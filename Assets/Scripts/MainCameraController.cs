using System.Collections.Generic;
using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    public bool  debugRay       = true;
    public float duration       = 0.1f;
    public int res = 10;
    public float renderDistance = 3.0f; //should be +- length of the maze's diagonal; maze pieces should be at least this far away from each other
    
    private Portal[]     _portals;
    private List<Portal> _directlyVisiblePortals;

    private Vector3 da;
    private Vector3 db;
    private Vector3 dc;
    
    void Awake()
    {
        _portals = FindObjectsOfType<Portal>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(da, 0.4f);
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(db, 0.4f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(dc, 0.4f);
    }

    void OnPreCull()
    {
        // Look for directly visible portals
        _directlyVisiblePortals = new List<Portal>();
        foreach (Portal portal in _portals)
        {
            bool       visible;
            visible = IsVisible(portal);
            
            if (visible)
                _directlyVisiblePortals.Add(portal);
            else
                portal.RenderColor(Color.red);
        }

        // Look for 1 level deep portals
        List<(Portal, Vector3)> portals2 = new List<(Portal, Vector3)>();
        foreach (Portal portal in _directlyVisiblePortals)
        {
            // portal = the portal that was hit with the ray directly from player

            Portal  raycastingPortal = portal.otherPortal; // The portal that is gonna cast rays
            Vector3 raycastOrigin    = portal.otherPortal.transform.position + (transform.position - portal.transform.position); // The point that the rays were cast from the point of view of the raycasting portal;
            //Debug.Log("origin " + raycastOrigin);
            da = raycastOrigin;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(portal.myCam); // The camera planes
            
            List<Portal> possiblePortals = new List<Portal>();

            foreach (Portal p in _portals)
            {
                if (p.gameObject == raycastingPortal.gameObject) continue;
                if (GeometryUtility.TestPlanesAABB(planes, p.Bounds)) // Test if portal is within the camera vision planes
                    if (Vector3.Distance(raycastOrigin, p.transform.position) < renderDistance) // And if they are not too far away
                    {
                        possiblePortals.Add(p);
                        p.RenderColor(Color.black);
                    }
            }


            foreach (Portal possiblePortal in possiblePortals)
            {
                Vector3   a      = possiblePortal.points[0].transform.position;
                Vector3   b      = possiblePortal.points[1].transform.position;
                Vector3[] points = new Vector3[res];
                
                // Calculate the points between a and b
                for (int i = 0; i < res; i++)
                    points[i] = a + ((b - a) * ((Vector3.Distance(a, b) / res) * i));

                foreach (Vector3 point in points)
                {
                    db = point;
                    // Cast rays from points to the direction of raycarsOrigin and see if they hit the portal
                    Physics.Raycast(point, raycastOrigin - point, out RaycastHit hit2, renderDistance);
                    dc = (raycastOrigin - point);
                    if (hit2.collider.gameObject == raycastingPortal.screen.gameObject)
                    {
                        if (debugRay) Debug.DrawRay(point, raycastOrigin - point, Color.yellow, duration);
                        portals2.Add((possiblePortal, portal.offset));
                        break;
                    }
                    if (debugRay) Debug.DrawRay(point, raycastOrigin - point, Color.magenta, duration);
                }
            }
        }

        foreach ((Portal portal, Vector3 offset) in portals2)
            portal.Render(offset);
        
        foreach (Portal portal in _directlyVisiblePortals)
            portal.Render(Vector3.zero);
    }

    bool IsVisible(Portal portal)
    {
        Vector3   a      = portal.points[0].transform.position;
        Vector3   b      = portal.points[1].transform.position;
        Vector3[] points = new Vector3[res];

        // Calculate the points between a and b
        for (int i = 0; i < res; i++)
            points[i] = a + ((b - a) * ((Vector3.Distance(a, b) / res) * i));

        // Cast rays from camera to the points
        foreach (Vector3 prPos in points)
        {
            Vector3 direc = prPos - transform.position;
            if (Vector3.Distance(transform.position, prPos) < renderDistance)
            {
                if (!Physics.Raycast(transform.position, direc, out RaycastHit hit, renderDistance))
                    return true;

                if (hit.collider.gameObject == portal.screen.gameObject)
                {
                    if (debugRay) Debug.DrawLine(transform.position, hit.point, Color.green, duration);
                    return true;
                }

                if (debugRay) Debug.DrawLine(transform.position, hit.point, Color.red, duration);
            }
        }

        return false;
    }

    List<Portal> GetNearPortals(Vector3 position, float dist)
    {
        List<Portal> near = new List<Portal>();
        foreach (Portal portal in _portals)
            if (Vector3.Distance(position, portal.transform.position) < dist)
                near.Add(portal);
        return near;
    }
}