using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float targetHeight = 1.5f;
    public float distance = 3f;
    public float sensitivity = 150f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public bool lockCursor = true;
    public bool invertY = false;
    public LayerMask collisionMask = ~0;
    public float cameraRadius = 0.2f;
    public float collisionOffset = 0.1f;
    public float minDistance = 0.5f;

    private float yaw = 0f;
    private float pitch = 10f;

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector2 delta = Vector2.zero;
        if (Mouse.current != null)
            delta = Mouse.current.delta.ReadValue();

        yaw += delta.x * sensitivity * Time.deltaTime;
        pitch += (invertY ? 1 : -1) * delta.y * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPos = target.position + Vector3.up * targetHeight;
        Vector3 desiredPos = targetPos + rot * new Vector3(0f, 0f, -distance);

        Vector3 dir = (desiredPos - targetPos).normalized;
        float maxDist = Vector3.Distance(targetPos, desiredPos);

        RaycastHit hit;
        Vector3 finalPos = desiredPos;

        if (Physics.SphereCast(targetPos, cameraRadius, dir, out hit, maxDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float hitDistance = hit.distance;
            float adjustedDistance = Mathf.Max(hitDistance - collisionOffset, minDistance);
            finalPos = targetPos + dir * adjustedDistance;
        }

        transform.position = finalPos;
        transform.rotation = Quaternion.LookRotation((targetPos - transform.position).normalized, Vector3.up);
    }
}