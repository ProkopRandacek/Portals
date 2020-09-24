using UnityEngine;

public class PortalCameraController : MonoBehaviour
{
    private Transform _playerCam;
    public  Transform mePortal;
    public  Transform otherPortal;
    
    void Start()
    {
        _playerCam = Camera.main.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = otherPortal.position - (mePortal.position - _playerCam.position);
        transform.rotation = _playerCam.rotation;
    }
}
