/* 
 * author : jiankaiwang
 */

using UnityEngine;

public class MouseCamLook : MonoBehaviour {

    [SerializeField]
    public float sensitivity = 5.0f;
    [SerializeField]
    public float smoothing = 2.0f;
    // the chacter is the capsule
    public GameObject character;
    // get the incremental value of mouse moving
    private Vector2 _mouseLook;
    // smooth the mouse moving
    private Vector2 _smoothV;

    // Use this for initialization
    void Start () {
        character = this.transform.parent.gameObject;
    }
	
    // Update is called once per frame
    void Update () {
        // md is mouse delta
        var md = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        md = Vector2.Scale(md, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        // the interpolated float result between the two float values
        _smoothV.x = Mathf.Lerp(_smoothV.x, md.x, 1f / smoothing);
        _smoothV.y = Mathf.Lerp(_smoothV.y, md.y, 1f / smoothing);
        // incrementally add to the camera look
        _mouseLook += _smoothV;

        // vector3.right means the x-axis
        transform.localRotation           = Quaternion.AngleAxis(-_mouseLook.y, Vector3.right);
        character.transform.localRotation = Quaternion.AngleAxis(_mouseLook.x,  character.transform.up);
    }
}