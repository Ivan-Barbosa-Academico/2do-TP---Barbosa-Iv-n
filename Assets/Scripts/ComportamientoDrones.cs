using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(AudioSource))]
public class ComportamientoDrones : MonoBehaviour
{
    public enum Estado { Patrulla, Persecucion }

    [Header("Patrulla")]
    public Transform[] puntosPatrulla;
    public float velocidadPatrulla = 2f;
    public float esperaEnPunto = 1.2f;

    [Header("Detección (raycast)")]
    public float distanciaVision = 12f;
    [Range(0, 180)] public float anguloVision = 60f;
    public string tagJugador = "Player";

    [Header("Zona de detección (esfera)")]
    public float radioAlerta = 6f;

    [Header("Movimiento en persecución")]
    public float velocidadPersecucion = 4f;

    [Header("Visual")]
    public Color colorPatrulla = Color.green;
    public Color colorPersecucion = Color.red;
    public float alturaOjos = 1.0f;
    [Tooltip("Rotación en grados alrededor del eje Y que desplaza el eje central del cono de visión")]
    public float rotacionConoY = 90f;
    [Tooltip("Número de segmentos usados para dibujar el cono en los Gizmos")]
    [Range(4, 64)] public int segmentosCono = 24;

    [Header("Audio")]
    public AudioClip sonidoPersecucion;
    [Range(0f, 1f)] public float volumenSonido = 1f;


    private Estado estadoActual = Estado.Patrulla;
    private Estado estadoPrevio = Estado.Patrulla;
    private int indicePunto = 0;
    private float contadorEspera = 0f;
    private Transform jugadorTransform;
    private Renderer rend;
    private AudioSource fuenteAudio;

    private bool haDetectadoJugador = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        fuenteAudio = GetComponent<AudioSource>();
        if (fuenteAudio != null)
        {
            fuenteAudio.playOnAwake = false;
            fuenteAudio.loop = false;
            fuenteAudio.volume = volumenSonido;
        }

        GameObject jugadorObj = GameObject.FindGameObjectWithTag(tagJugador);
        if (jugadorObj != null) jugadorTransform = jugadorObj.transform;
        else Debug.LogWarning($"Enemigo binario: No se ha encontrado ningún GameObject con tag '{tagJugador}'.");
    }

    void Update()
    {
        bool detectadoAhora = DetectarJugadorBinario();
        haDetectadoJugador = haDetectadoJugador || detectadoAhora;

        estadoActual = haDetectadoJugador ? Estado.Persecucion : Estado.Patrulla;

        if (estadoActual != estadoPrevio)
        {
            OnEstadoCambiado(estadoPrevio, estadoActual);
            estadoPrevio = estadoActual;
        }

        if (rend != null)
        {
            rend.material.color = (estadoActual == Estado.Patrulla) ? colorPatrulla : colorPersecucion;
        }

        switch (estadoActual)
        {
            case Estado.Patrulla:
                EjecutarPatrulla();
                break;
            case Estado.Persecucion:
                EjecutarPersecucion();
                break;
        }
    }

    private bool DetectarJugadorBinario()
    {
        if (jugadorTransform == null) return false;

        Vector3 origen = transform.position + Vector3.up * alturaOjos;
        Vector3 dirJugador = (jugadorTransform.position - origen).normalized;
        float distanciaJugador = Vector3.Distance(origen, jugadorTransform.position);

        if (Vector3.Distance(transform.position, jugadorTransform.position) <= radioAlerta)
        {
            return true;
        }

        Vector3 downWorld = transform.TransformDirection(Vector3.down);
        Vector3 ejeCono = (Quaternion.Euler(0f, rotacionConoY, 0f) * downWorld).normalized;

        float anguloMedio = anguloVision * 0.5f;
        float angulo = Vector3.Angle(ejeCono, dirJugador);
        if (angulo <= anguloMedio && distanciaJugador <= distanciaVision)
        {
            RaycastHit hit;
            if (Physics.Raycast(origen, dirJugador, out hit, distanciaJugador))
            {
                if (hit.collider.CompareTag(tagJugador))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void OnEstadoCambiado(Estado anterior, Estado actual)
    {
        if (fuenteAudio == null) return;

        fuenteAudio.volume = volumenSonido;

        if (actual == Estado.Persecucion)
        {
            if (sonidoPersecucion != null)
            {
                fuenteAudio.loop = false;
                fuenteAudio.clip = sonidoPersecucion;
                fuenteAudio.Play();
            }
        }
        else
        {
            fuenteAudio.Stop();
        }
    }

    void EjecutarPatrulla()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;

        Transform objetivo = puntosPatrulla[indicePunto];
        Vector3 direccion = (objetivo.position - transform.position);
        Vector3 dirPlano = new Vector3(direccion.x, 0, direccion.z);
        float distancia = dirPlano.magnitude;

        if (distancia < 0.2f)
        {
            contadorEspera += Time.deltaTime;
            if (contadorEspera >= esperaEnPunto)
            {
                indicePunto = (indicePunto + 1) % puntosPatrulla.Length;
                contadorEspera = 0f;
            }
            return;
        }

        Vector3 objetivoPlano = new Vector3(objetivo.position.x, transform.position.y, objetivo.position.z);
        transform.position = Vector3.MoveTowards(transform.position, objetivoPlano, velocidadPatrulla * Time.deltaTime);
    }

    void EjecutarPersecucion()
    {
        if (jugadorTransform == null) return;
        transform.position = Vector3.MoveTowards(transform.position, jugadorTransform.position, velocidadPersecucion * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, radioAlerta);
        Gizmos.color = Color.cyan;
        Vector3 origen = transform.position + Vector3.up * alturaOjos;

        Vector3 downWorld = transform.TransformDirection(Vector3.down);
        Vector3 ejeCono = (Quaternion.Euler(0f, rotacionConoY, 0f) * downWorld).normalized;

        DrawConeGizmo(origen, ejeCono, anguloVision, distanciaVision, segmentosCono);
    }
    void DrawConeGizmo(Vector3 apex, Vector3 axis, float fovDegrees, float length, int segments)
    {
        if (segments < 4) segments = 4;
        float halfAngleRad = (fovDegrees * 0.5f) * Mathf.Deg2Rad;
        float radius = Mathf.Tan(halfAngleRad) * length;

        Vector3 center = apex + axis.normalized * length;
        Vector3 arbitrary = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.99f ? Vector3.up : Vector3.forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(axis, arbitrary));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, axis));

        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float ang = t * Mathf.PI * 2f;
            Vector3 circlePoint = center + (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * radius;

            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, circlePoint);
            }
            Gizmos.DrawLine(apex, circlePoint);
            prevPoint = circlePoint;
        }
    }
}