/* 
 * author : jiankaiwang
 */

using UnityEngine;

public class CharacterController : MonoBehaviour {

    public  float speed = 10.0f;
    private float _translation;
    private float _straffe;

    // Use this for initialization
    void Start () {
        // turn off the cursor
        Cursor.lockState = CursorLockMode.Locked;		
    }
	
    // Update is called once per frame
    void Update () {
        // Input.GetAxis() is used to get the user's input
        // You can further set it on Unity. (Edit, Project Settings, Input)
        _translation = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        _straffe     = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        transform.Translate(_straffe, 0, _translation);

        if (Input.GetKeyDown("escape")) {
            // turn on the cursor
            Cursor.lockState = CursorLockMode.None;
        }
    }
}