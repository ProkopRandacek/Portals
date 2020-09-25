using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
    public Vector3 prevOffsetFromPortal;

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
    }

    public virtual void EnterPortalTreshold() { }
    public virtual void ExitPortalTreshold() { }
}