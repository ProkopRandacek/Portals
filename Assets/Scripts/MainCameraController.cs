using System.Collections.Generic;
using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    public bool debugRay;
    public float duration = 0.1f;
    public int res = 10;
    public float renderDistance = 3.0f; //should be +- length of the maze's diagonal; maze pieces should be at least this far away from each other

    private Portal[]     _portals;
    private List<Portal> _directlyVisiblePortals;
    
    void Awake()
    {
        _portals = FindObjectsOfType<Portal>();
    }

    void OnPreCull()
    {
        // 1. Find directly visible portals (raycast from player camera to near portals)
        // 2. For every directly visible portal
        //    1. Get the planes that align with the portal's frame
        //    2. Use that to get portals that are possibly trough from this portal
        //    3. Calculate position of the camera relative to the portal and apply this position to the portal.otherPortal
        //       1. For every possibly visible portal cast rays from that portal to the calculated position and see if it hit the portal.otherPortal
        //          If yes, the raycasting portal is definitely visible.


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

            Plane[] planes = GetPlanes(portal.myCam, portal);
            
            List<Portal> possiblePortals = new List<Portal>();

            foreach (Portal p in _portals)
            {
                if (p.gameObject == raycastingPortal.gameObject) continue;
                if (GeometryUtility.TestPlanesAABB(planes, p.Bounds)) // Test if portal is within the camera vision planes
                    if (Vector3.Distance(raycastOrigin, p.transform.position) < renderDistance) // And if they are not too far away
                        if (!_directlyVisiblePortals.Contains(p)) // The portal isn't directly visible
                        {
                            possiblePortals.Add(p);
                            p.RenderColor(Color.magenta);
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
                    // Cast rays from points to the direction of raycastOrigin and see if they hit the portal
                    Vector3 direction = raycastOrigin - point;
                    // Move the point a tiny bit in the direction to prevent from colliding with the raycasting portal's screen
                    Vector3 p = point + (direction / direction.magnitude) / 10;
                    Physics.Raycast(p, direction, out RaycastHit hit2, renderDistance);
                    if (hit2.collider.gameObject == raycastingPortal.screen.gameObject)
                    {
                        //if (debugRay) Debug.DrawLine(p, hit2.point, Color.yellow, duration);
                        portals2.Add((possiblePortal, portal.offset));
                        break;
                    }
                    if (debugRay) Debug.DrawLine(p, hit2.point, Color.magenta, duration);
                }
            }
        }

        foreach ((Portal portal, Vector3 offset) in portals2) // First, render the 1 level deep portals
            portal.Render(offset);
        
        foreach (Portal portal in _directlyVisiblePortals)  // Last, render the directly visible portals
            portal.Render(Vector3.zero);
    }

    bool IsVisible(Portal portal)
    {
        Vector3   a      = portal.points[0].transform.position;
        Vector3   b      = portal.points[1].transform.position;
        Vector3[] points = new Vector3[res];
        
        // Test if the portal is in main camera's frustum planes
        if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), portal.Bounds))
            return false;

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

    Plane[] GetPlanes(Camera cam, Portal portal)
    {
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
        Vector3 camPos    = cam.transform.position;
        
        Plane[] planes      = new Plane[6];
        Vector3 topLeft     = portal.corners[0];
        Vector3 topRight    = portal.corners[1];
        Vector3 bottomRight = portal.corners[2];
        Vector3 bottomLeft  = portal.corners[3];
        
        planes[0] = new Plane(camPos, topLeft,    bottomLeft);  // Left
        planes[1] = new Plane(camPos, topRight,   bottomRight); // Right
        planes[2] = new Plane(camPos, bottomLeft, bottomRight); // Bottom
        planes[3] = new Plane(camPos, topLeft,    topRight);    // Top
        planes[4] = camPlanes[4];                               // Near
        planes[5] = camPlanes[5];                               // Far
        
        Debug.DrawLine(camPos, topLeft, Color.yellow);
        Debug.DrawLine(camPos, topRight, Color.yellow);
        Debug.DrawLine(camPos, bottomLeft, Color.yellow);
        Debug.DrawLine(camPos, bottomRight, Color.yellow);

        return planes;
    }
}