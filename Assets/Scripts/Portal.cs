using System;
using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;

public class Portal : MonoBehaviour
{
    public Portal       otherPortal;
    public MeshRenderer screen;
    public GameObject[] points = new GameObject[2];
    public Transform    myCamPos;
    public Camera       myCam;
    public Bounds       Bounds;
    public Vector3      offset;
    public Vector3[]    corners;

    private Transform             _playerPos;
    private Transform             _otherPortalPos;
    private GameObject            _otherPortalScreen;
    private RenderTexture         _viewTexture;
    private List<PortalTraveller> _trackedTravellers;

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    
    public void Render(Vector3 offset)
    {
        otherPortal.screen.enabled = false;
        CreateViewTexture();
        Move(offset);
        SetNearClipPlane();
        ProtectScreenFromClipping(_playerPos.position);
        myCam.Render();
        otherPortal.screen.enabled = true;
    }

    public void RenderColor(Color clr)
    {
        /*myCamPos.position = transform.position;
        myCamPos.rotation = transform.rotation;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, clr);
        tex.Apply();
        screen.material.SetTexture(MainTex, tex);*/
    }

    void Awake()
    {
        // ReSharper disable once PossibleNullReferenceException
        _playerPos         = Camera.main.GetComponent<Transform>();
        myCam              = GetComponentInChildren<Camera>();
        myCamPos           = myCam.transform;
        _otherPortalPos    = otherPortal.transform;
        _trackedTravellers = new List<PortalTraveller>();
        myCam.enabled      = false;
        Bounds             = screen.bounds;
        offset             = transform.position - _otherPortalPos.position;
        //portalOffset = transform.position - _otherPortalPos.transform.position; // Vector pointing from the other portal to me

        //_myCam.nearClipPlane
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
            myCam.targetTexture = _viewTexture;
            screen.material.SetTexture(MainTex, _viewTexture);
        }

        //if (screen.material.mainTexture.width == 1) // The debug 1x1 one color texture is set from previous frame
        //    screen.material.SetTexture(MainTex, _viewTexture);
    }
    
    void Move(Vector3 offset) // Camera position and rotation calculation
    {
        myCamPos.position =  _otherPortalPos.position + (_playerPos.position - transform.position);
        myCamPos.position -= offset;
        myCamPos.rotation =  _playerPos.rotation;
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
        Transform clipPlane = otherPortal.transform;
        int dot = Math.Sign(Vector3.Dot(clipPlane.forward, otherPortal.transform.position - myCam.transform.position));
        Vector3 camSpacePos    = myCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = myCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float   camSpaceDst    = -Vector3.Dot(camSpacePos, camSpaceNormal) + 0.00f;
        if (Mathf.Abs(camSpaceDst) > 0.00f)
        {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);
            myCam.projectionMatrix = myCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
        else
            myCam.projectionMatrix = myCam.projectionMatrix;
    }
    #endregion
}