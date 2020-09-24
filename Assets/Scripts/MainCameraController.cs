using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    private PortalCameraController[] _portals;
    void Awake()
    {
        _portals = FindObjectsOfType<PortalCameraController>();
    }

    // Update is called once per frame
    void OnPreCull()
    {
        foreach (PortalCameraController portal in _portals)
        {
            portal.Render();
        }
    }
}