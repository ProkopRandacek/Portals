using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    public float maxDistance    = 10.0f;
    public bool  debugRay       = true;
    public float duration       = 0.0f;
    public float renderDistance = 3.0f;
    
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
            if (IsVisible(portal))
            {
                //Debug.Log("Render");
                portal.Render();
            }
            else
            {
                //Debug.Log("Dont Render");
                portal.RenderColor(Color.red);
            }
}

    bool IsVisible(PortalController portal)
    {
        if (Vector3.Distance(transform.position, portal.transform.position) < renderDistance)
            return true;
        return false;
        //RaycastHit hit = RayCast(transform.position, portal.transform.position);
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