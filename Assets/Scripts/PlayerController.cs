using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 5f;
    public Vector2 mouseSensitivity;
    public new Transform camera;

    public float rotationSpeed = 10f;

    private float cameraPitch = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;

        if (camera != null)
        {
            cameraPitch = camera.localEulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
        }
    }

    void Update()
    {
        Updatemovement();
        Updatemouselook();
    }

    void FixedUpdate()
    {
        if (camera == null || rb == null) return;

        Vector3 flatForward = new(camera.forward.x, 0f, camera.forward.z);
        if (flatForward.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(flatForward);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
    }

    private void Updatemovement()
    {         
        Vector2 move = Vector2.zero;

        if (Gamepad.current != null)
        {
            move = Gamepad.current.leftStick.ReadValue();
        }
        else if (Keyboard.current != null)
        {
            float hor = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) hor -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) hor += 1f;

            float ver = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) ver += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) ver -= 1f;

            move = new Vector2(hor, ver);
        }

        Vector3 velocity = Vector3.zero;
        if (move.x != 0 || move.y != 0)
        {
            Vector3 direction = (transform.forward * move.y + transform.right * move.x).normalized;
            velocity = direction * speed;
        }
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void Updatemouselook()
    {
        if (camera == null) return;
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }
        else if (Gamepad.current != null)
        {
            mouseDelta = Gamepad.current.rightStick.ReadValue() * 10f;
        }

        float hor = mouseDelta.x * mouseSensitivity.x;
        float ver = mouseDelta.y * mouseSensitivity.y;

        if (hor != 0f)
        {
            camera.Rotate(Vector3.up, hor, Space.World);
        }

        if (ver != 0f)
        {
            cameraPitch -= ver;
            cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);
            Vector3 current = camera.localEulerAngles;
            camera.localEulerAngles = new Vector3(cameraPitch, current.y, current.z);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Perdiste");
        }
    }
}
