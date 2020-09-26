using System;
using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;

public class PortalController : MonoBehaviour
{
    public PortalController otherPortal;
    public MeshRenderer     screen;

    private Camera                _myCam;
    private Transform             _myCamPos;
    private Transform             _playerPos;
    private Transform             _otherPortalPos;
    private GameObject            _otherPortalScreen;
    private RenderTexture         _viewTexture;
    private List<PortalTraveller> _trackedTravellers;

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    
    public void Render()
    {
        screen.enabled = false;
        CreateViewTexture();
        Move(); //FIXME
        SetNearClipPlane();
        _myCam.Render();
        screen.enabled = true;
        ProtectScreenFromClipping(_playerPos.position);
    }

    void Awake()
    {
        // ReSharper disable once PossibleNullReferenceException
        _playerPos         = Camera.main.GetComponent<Transform>();
        _myCam             = GetComponentInChildren<Camera>();
        _myCamPos          = _myCam.transform;
        _otherPortalPos    = otherPortal.transform;
        _trackedTravellers = new List<PortalTraveller>();
        _myCam.enabled     = false;
        //portalOffset = transform.position - _otherPortalPos.transform.position; // Vector pointing from the other portal to me
    }
    
    void LateUpdate()
    {
        for (int i = 0; i < _trackedTravellers.Count; i++)
        {
            PortalTraveller traveller = _trackedTravellers[i];
            Transform travellerT = traveller.transform;
            Matrix4x4 m = otherPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            int portalSide = Math.Sign(Vector3.Dot(offsetFromPortal, transform.forward));
            int portalSideOld = Math.Sign(Vector3.Dot(traveller.prevOffsetFromPortal, transform.forward));
            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld)
            {
                traveller.Teleport(transform, otherPortal.transform, m.GetColumn (3), m.rotation);
                // Can't rely on OnTriggerEnter/Exit to be called next frame since it depends on when FixedUpdate runs
                otherPortal.OnTravellerEnterPortal(traveller);
                _trackedTravellers.RemoveAt(i);
                i--;
            }
            else
                traveller.prevOffsetFromPortal = offsetFromPortal;
        }
    }

    void CreateViewTexture()
    {
        if (_viewTexture == null || _viewTexture.width != Screen.width || _viewTexture.height != Screen.height)
        {
            if (_viewTexture != null)
                _viewTexture.Release();
            _viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            _myCam.targetTexture = _viewTexture;
            otherPortal.screen.material.SetTexture(MainTex, _viewTexture);
        }
    }
    
    void Move() // Camera position and rotation calculation
    {
        _myCamPos.position = transform.position + (_playerPos.position - _otherPortalPos.position);
        _myCamPos.rotation = _playerPos.rotation; //TODO this might not be right when multiple portals are behind with different rotations?
    }

    #region Events
    void OnTravellerEnterPortal(PortalTraveller traveller)
    {
        if (!_trackedTravellers.Contains(traveller))
        {
            traveller.EnterPortalTreshold();
            traveller.prevOffsetFromPortal = traveller.transform.position - transform.position;
            _trackedTravellers.Add (traveller);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller)
            OnTravellerEnterPortal(traveller);
    }
    
    void OnTriggerExit(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller && _trackedTravellers.Contains(traveller))
        {
            traveller.ExitPortalTreshold();
            _trackedTravellers.Remove(traveller);
        }
    }
    #endregion
    
    #region Some camera magic
    // Sets the thickness of the portal screen so as not to clip with camera near plane when player goes through
    void ProtectScreenFromClipping(Vector3 viewPoint)
    {
        float halfHeight = Camera.main.nearClipPlane * Mathf.Tan (Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * Camera.main.aspect;
        float dstToNearClipPlaneCorner = new Vector3 (halfWidth, halfHeight, Camera.main.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner;

        Transform screenT                  = screen.transform;
        bool      camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;
        screenT.localScale    = new Vector3(screenT.localScale.x, screenT.localScale.y, screenThickness);
        screenT.localPosition = Vector3.forward * screenThickness * (camFacingSameDirAsPortal ? 0.5f : -0.5f);
    }
    
    // Calculate and set the clip plane to align with the portal screen
    void SetNearClipPlane()
    {
        Transform clipPlane = transform;
        int dot = Math.Sign(Vector3.Dot (clipPlane.forward, transform.position - _myCam.transform.position));
        Vector3 camSpacePos    = _myCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = _myCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float   camSpaceDst    = -Vector3.Dot(camSpacePos, camSpaceNormal) + 0.05f;
        if (Mathf.Abs(camSpaceDst) > 0.2f)
        {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);
            _myCam.projectionMatrix = _myCam.CalculateObliqueMatrix (clipPlaneCameraSpace);
        }
        else
            _myCam.projectionMatrix = _myCam.projectionMatrix;
    }
    #endregion
}