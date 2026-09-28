using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform target;              // el personaje (eje central)
    public float targetHeight = 1.5f;     // altura relativa al target donde mirar
    public float distance = 3f;           // distancia al target
    public float sensitivity = 150f;      // sensibilidad del ratón
    public float minPitch = -30f;         // tope inferior de la cámara
    public float maxPitch = 60f;          // tope superior de la cámara
    public bool lockCursor = true;        // bloquear cursor al jugar
    public bool invertY = false;

    // Colisión de cámara
    public LayerMask collisionMask = ~0;  // capas con las que chocar (por defecto todo)
    public float cameraRadius = 0.2f;     // radio para el SphereCast (evita que la cámara "atraviese" esquinas)
    public float collisionOffset = 0.1f;  // separación mínima desde la pared
    public float minDistance = 0.5f;      // distancia mínima al target cuando la cámara se empuja hacia adelante

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

        // Leer movimiento del ratón (Input System)
        Vector2 delta = Vector2.zero;
        if (Mouse.current != null)
            delta = Mouse.current.delta.ReadValue();

        // Actualizar ángulos
        yaw += delta.x * sensitivity * Time.deltaTime;
        pitch += (invertY ? 1 : -1) * delta.y * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Calcular posición y orientación deseada
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPos = target.position + Vector3.up * targetHeight;
        Vector3 desiredPos = targetPos + rot * new Vector3(0f, 0f, -distance);

        // Gestión de colisiones: spherecast desde el target hacia la posición deseada
        Vector3 dir = (desiredPos - targetPos).normalized;
        float maxDist = Vector3.Distance(targetPos, desiredPos);

        RaycastHit hit;
        Vector3 finalPos = desiredPos;

        if (Physics.SphereCast(targetPos, cameraRadius, dir, out hit, maxDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // Colisión detectada: colocar la cámara justo delante del obstáculo, con un pequeño offset
            float hitDistance = hit.distance;
            float adjustedDistance = Mathf.Max(hitDistance - collisionOffset, minDistance);
            finalPos = targetPos + dir * adjustedDistance;
        }

        transform.position = finalPos;
        transform.rotation = Quaternion.LookRotation((targetPos - transform.position).normalized, Vector3.up);
    }
}