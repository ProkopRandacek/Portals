using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    public float maxDistance = 10.0f;
    public bool  debugRay    = true;
    public float duration    = 0.0f;
    
    private PortalController[] _portals;
    private Vector3            _pos;
    
    void Awake()
    {
        _portals = FindObjectsOfType<PortalController>();
        _pos     = transform.position;
    }

    void OnPreCull()
    {
        foreach (PortalController portal in _portals)
        {
            RaycastHit hit = RayCast(transform.position, portal.transform.position);
            
            Debug.Log(transform.position + "  " +  portal.transform.position);
            Debug.Log(hit);

            portal.Render();
        }
    }

    void isVisible(PortalController portal)
    {
        RaycastHit hit = RayCast(transform.position, portal.transform.position);
    }

    RaycastHit RayCast(Vector3 from, Vector3 to)
    {
        Vector3 direction = transform.TransformDirection(to);
        if (Physics.Raycast(from, direction, out RaycastHit hit, maxDistance))
            if (debugRay) Debug.DrawRay(from, direction * hit.distance, Color.green, duration);
        else
            if (debugRay) Debug.DrawRay(from, direction * maxDistance,  Color.red,   duration);
        return hit;
    }
}