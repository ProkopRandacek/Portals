using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public  float               gravity = 18;
    private float               _verticalVelocity;
    private Vector3             _velocity;
    private Vector3             _smoothV;
    private CharacterController _controller;

    // Use this for initialization
    void Start ()
    {
        // turn off the cursor
        Cursor.lockState = CursorLockMode.Locked;		
        _controller      = GetComponent<CharacterController>();
    }
	
    // Update is called once per frame
    void Update ()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        Vector3 inputDir      = new Vector3(input.x, 0, input.y).normalized;
        Vector3 worldInputDir = transform.TransformDirection(inputDir);

        float   currentSpeed   = 3;
        Vector3 targetVelocity = worldInputDir * currentSpeed;
        _velocity = Vector3.SmoothDamp(_velocity, targetVelocity, ref _smoothV, 0.3f);

        _verticalVelocity -= gravity * Time.deltaTime;
        _velocity         =  new Vector3(_velocity.x, _verticalVelocity, _velocity.z);

        var flags = _controller.Move(_velocity * Time.deltaTime);
        
        if (flags == CollisionFlags.Below)
            _verticalVelocity = 0;
        
        if (Input.GetKeyDown("escape")) {
            // turn on the cursor
            Cursor.lockState = CursorLockMode.None;
        }
    }
}