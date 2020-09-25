﻿using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    private PortalController[] _portals;
    
    void Awake()
    {
        _portals = FindObjectsOfType<PortalController>();
    }

    void OnPreCull()
    {
        foreach (PortalController portal in _portals)
        {
            portal.Render();
        }
    }
}