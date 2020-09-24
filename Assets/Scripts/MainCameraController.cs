using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    private PortalController[] _portals;
    void Awake()
    {
        _portals = FindObjectsOfType<PortalController>();
    }

    // Update is called once per frame
    void OnPreCull()
    {
        foreach (PortalController portal in _portals)
        {
            portal.Render();
        }
    }
}