using System;
using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;

public class PortalController : MonoBehaviour
{
    public PortalController otherPortal;
    public MeshRenderer     screen;
    public Vector3          portalOffset;

    private Camera                _myCam;
    private Transform             _myCamPos;
    private Transform             _playerPos;
    private Transform             _otherPortalPos;
    private GameObject            _otherPortalScreen;
    private RenderTexture         _viewTexture;
    private List<PortalTraveller> _trackedTravellers;

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

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

    private void CreateViewTexture()
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
    
    // Camera position and rotation calculation
    private void Move()
    {
        _myCamPos.position = transform.position + (_playerPos.position - _otherPortalPos.position);
        _myCamPos.rotation = _playerPos.rotation; //TODO this might not be right when multiple portals are behind with different rotations?
    }
    
    public void Render()
    {
        screen.enabled = false;
        CreateViewTexture();
        Move(); //FIXME
        _myCam.Render();
        screen.enabled = true;
    }

    void OnTravellerEnterPortal(PortalTraveller traveller)
    {
        Debug.Log("OnTravellerEnter");
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
        Debug.Log("OnTriggerExit");
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller && _trackedTravellers.Contains(traveller))
        {
            traveller.ExitPortalTreshold();
            _trackedTravellers.Remove(traveller);
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < _trackedTravellers.Count; i++)
        {
            PortalTraveller traveller = _trackedTravellers[i];
            Transform travellerT = traveller.transform;
            var m = otherPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            int portalSide = Math.Sign (Vector3.Dot (offsetFromPortal, transform.forward));
            int portalSideOld = Math.Sign (Vector3.Dot (traveller.prevOffsetFromPortal, transform.forward));
            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld) {
                var positionOld = travellerT.position;
                var rotOld      = travellerT.rotation;
                traveller.Teleport (transform, otherPortal.transform, m.GetColumn (3), m.rotation);
                // Can't rely on OnTriggerEnter/Exit to be called next frame since it depends on when FixedUpdate runs
                otherPortal.OnTravellerEnterPortal (traveller);
                _trackedTravellers.RemoveAt (i);
                i--;

            } else {
                traveller.prevOffsetFromPortal = offsetFromPortal;
            }
        }
    }
}