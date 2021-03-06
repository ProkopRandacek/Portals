﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;

public class Portal : MonoBehaviour
{
    public Portal       otherPortal; // Link to the other portal that is connected to this portal
    public MeshRenderer screen; // This portal's screen Mesh Renderer. The rendered texture is applied here
    public GameObject[] points = new GameObject[2]; // Two points and n generated points between them are used as raycasting targets to test if this portal is visible
    public Transform    myCamPos; // This portal's camera position. This camera is rendering what is supposed to be put on this portal screen (So its near the otherPortal)
    public Camera       myCam; // This portal's camera
    public Bounds       Bounds; // This portal's Bounds. Used to test if the portal is in cameras view frustum
    public Vector3      offset; // Vector pointing from this portal to the otherPortal. Not used rn
    public Vector3[]    corners; // The 4 corners of this portal's screen. Used to calculate planes that align with camera and these corners
                                 // to get view frustum and test whether a portal is visible from a camera trough this portal (See MainCameraController.cs' GetPlanes() method)

    // Self explanatory variables IMO
    private Transform             _playerPos;
    private Transform             _otherPortalPos;
    private GameObject            _otherPortalScreen; 
    private RenderTexture         _viewTexture;
    private List<PortalTraveller> _trackedTravellers; // Travellers that are near any portal

    public float something = 0.05f; // Dont know
    public float someotherthing = 0.05f; // Dont know neither

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    
    public void Render(Vector3 offset)
    {
        otherPortal.screen.enabled = false; // Disable the other portals screen because we need to see trough and render whats behind
        CreateViewTexture(); // Reset the texture or something
        Move(offset); // Move the camera to correct position with all the offset when rendering portal visible trough another portal and all that stuff
        SetNearClipPlane(); // Clip plane magic. Avoids rendering something between the portal's camera and otherPortals's screen
        ProtectScreenFromClipping(_playerPos.position); // Idk
        myCam.Render(); // Render what camera sees
        otherPortal.screen.enabled = true; // Enable the screen again
    }

    public void RenderColor(Color clr) // Same thing as Render() but makes the portal's screen a solid color. Used to debug
    {
        myCamPos.position = transform.position;
        myCamPos.rotation = transform.rotation;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, clr);
        tex.Apply();
        screen.material.SetTexture(MainTex, tex);
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

        if (screen.material.mainTexture.width == 1) // The debug 1x1 one color texture is set from previous frame
            screen.material.SetTexture(MainTex, _viewTexture);
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
            _trackedTravellers.Add(traveller);
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
        float   camSpaceDst    = -Vector3.Dot(camSpacePos, camSpaceNormal) + something;
        if (Mathf.Abs(camSpaceDst) > someotherthing)
        {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);
            myCam.projectionMatrix = myCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
        else
            myCam.projectionMatrix = myCam.projectionMatrix;
    }
    #endregion
}