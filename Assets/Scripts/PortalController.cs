using UnityEngine;

public class PortalController : MonoBehaviour
{
    public PortalController otherPortal;
    public MeshRenderer           screen;
    public Vector3                portalOffset;
    public GameObject             mePortal;

    private Camera        _portalCam;
    private Transform     _playerPos;
    private Transform     _mePortalPos;
    private Transform     _otherPortalPos;
    private GameObject    _otherPortalScreen;
    private RenderTexture _viewTexture;

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    void Awake()
    {
        // ReSharper disable once PossibleNullReferenceException
        _playerPos      = Camera.main.GetComponent<Transform>();
        _portalCam      = GetComponentInChildren<Camera>();
        _mePortalPos    = transform.parent;
        _otherPortalPos = otherPortal.mePortal.transform;
        mePortal        = _mePortalPos.gameObject;

        //portalOffset = transform.position - _otherPortalPos.transform.position; // Vector pointing from the other portal to me
        
        _portalCam.enabled = false;
    }

    private void CreateViewTexture()
    {
        if (_viewTexture == null || _viewTexture.width != Screen.width || _viewTexture.height != Screen.height)
        {
            if (_viewTexture != null)
                _viewTexture.Release();
            _viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            _portalCam.targetTexture = _viewTexture;
            otherPortal.screen.material.SetTexture(MainTex, _viewTexture);
        }
    }
    
    // Camera position and rotation calculation
    private void Move()
    {
        transform.position = _mePortalPos.position + (_playerPos.position - _otherPortalPos.position);
        transform.rotation = _playerPos.rotation; //TODO this might not be right when multiple portals are behind with different rotations?
    }
    
    public void Render()
    {
        screen.enabled = false;
        
        CreateViewTexture();
        Move(); //FIXME
        _portalCam.Render();

        screen.enabled = true;
    }
}