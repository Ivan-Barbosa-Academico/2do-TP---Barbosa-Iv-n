csharp Assets/Scripts/Enemigo.cs
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Enemigo : MonoBehaviour
{
    public enum Estado { Patrulla, Alerta, Persecucion }

    [Header("Patrulla")]
    public Transform[] puntosPatrulla;
    public float velocidadPatrulla = 2f;
    public float esperaEnPunto = 1.2f;

    [Header("Detección (raycast)")]
    public float distanciaVision = 12f;
    [Range(0, 180)] public float anguloVision = 60f;
    public LayerMask capaObstaculos;     // capas que bloquean la visión (paredes, etc.)
    public string tagJugador = "Player";

    [Header("Zona de detección (esfera)")]
    public float radioAlerta = 6f;       // radio de la esfera de segunda detección

    [Header("Dinámica de detección progresiva")]
    public float tasaIncremento = 0.6f;  // cuánto sube la barra de detección por segundo cuando hay visibilidad
    public float tasaIncrementoCercania = 1.0f; // multiplicador si está dentro de la esfera
    public float tasaDecrecimiento = 0.4f; // cuánto decrece por segundo cuando no hay contacto
    [Range(0f, 1f)] public float umbralAlerta = 0.25f;   // a partir de aquí -> estado Alerta
    [Range(0f, 1f)] public float umbralDeteccion = 1f;   // detección completa (perdiste)

    [Header("Movimiento en persecución")]
    public float velocidadPersecucion = 4f;

    [Header("Visual")]
    public Color colorPatrulla = Color.green;
    public Color colorAlerta = Color.yellow;
    public Color colorPersecucion = Color.red;
    public float alturaOjos = 1.0f;

    // Estado interno
    private Estado estadoActual = Estado.Patrulla;
    private int indicePunto = 0;
    private float contadorEspera = 0f;
    private float progresoDeteccion = 0f; // 0..1
    private Transform jugadorTransform;
    private Renderer rend;
    private bool yaPerdido = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            Debug.LogWarning("Enemigo: No hay puntos de patrulla asignados.");
        }

        GameObject jugadorObj = GameObject.FindGameObjectWithTag(tagJugador);
        if (jugadorObj != null) jugadorTransform = jugadorObj.transform;
        else Debug.LogWarning($"Enemigo: No se ha encontrado ningún GameObject con tag '{tagJugador}'.");
    }

    void Update()
    {
        // Actualizar detección progresiva
        bool visiblePorRaycast = false;
        bool dentroEsfera = false;

        if (jugadorTransform != null)
        {
            Vector3 origen = transform.position + Vector3.up * alturaOjos;
            Vector3 dirJugador = (jugadorTransform.position - origen).normalized;
            float distanciaJugador = Vector3.Distance(origen, jugadorTransform.position);

            // Comprueba ángulo de visión
            float angulo = Vector3.Angle(transform.forward, dirJugador);
            if (angulo <= anguloVision * 0.5f && distanciaJugador <= distanciaVision)
            {
                // Raycast para comprobar obstáculos
                if (!Physics.Raycast(origen, dirJugador, distanciaJugador, capaObstaculos))
                {
                    visiblePorRaycast = true;
                }
            }

            // Comprobar esfera de proximidad (segunda zona)
            if (Vector3.Distance(transform.position, jugadorTransform.position) <= radioAlerta)
            {
                dentroEsfera = true;
            }
        }

        // Ajuste progresivo
        float incremento = 0f;
        if (visiblePorRaycast)
        {
            incremento += tasaIncremento * Time.deltaTime;
            if (dentroEsfera) incremento += tasaIncrementoCercania * Time.deltaTime;
        }
        else if (dentroEsfera)
        {
            // Si no hay línea de visión pero está dentro de la esfera, subir ligeramente
            incremento += (tasaIncremento * 0.4f) * Time.deltaTime;
        }
        else
        {
            // Decrementar si no detecta
            progresoDeteccion -= tasaDecrecimiento * Time.deltaTime;
        }

        progresoDeteccion += incremento;
        progresoDeteccion = Mathf.Clamp01(progresoDeteccion);

        // Estado según progreso
        Estado estadoPrevio = estadoActual;
        if (progresoDeteccion >= umbralDeteccion)
        {
            estadoActual = Estado.Persecucion;
            if (!yaPerdido)
            {
                Debug.Log("perdiste");
                yaPerdido = true;
            }
        }
        else if (progresoDeteccion >= umbralAlerta)
        {
            estadoActual = Estado.Alerta;
            yaPerdido = false;
        }
        else
        {
            estadoActual = Estado.Patrulla;
            yaPerdido = false;
        }

        // Visual feedback (color)
        switch (estadoActual)
        {
            case Estado.Patrulla: rend.material.color = colorPatrulla; break;
            case Estado.Alerta: rend.material.color = colorAlerta; break;
            case Estado.Persecucion: rend.material.color = colorPersecucion; break;
        }

        // Lógica de comportamiento según estado
        switch (estadoActual)
        {
            case Estado.Patrulla:
                EjecutarPatrulla();
                break;
            case Estado.Alerta:
                EjecutarAlerta();
                break;
            case Estado.Persecucion:
                EjecutarPersecucion();
                break;
        }

        // Opcional: mostrar información por Debug (puedes quitar si molesta)
        // Debug.DrawLine(transform.position + Vector3.up * alturaOjos, (jugadorTransform ? jugadorTransform.position : transform.position), Color.cyan);
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
            // Llegó al punto
            contadorEspera += Time.deltaTime;
            if (contadorEspera >= esperaEnPunto)
            {
                indicePunto = (indicePunto + 1) % puntosPatrulla.Length;
                contadorEspera = 0f;
            }
            return;
        }

        // Rotación suave hacia objetivo
        if (dirPlano != Vector3.zero)
        {
            Quaternion rotDeseada = Quaternion.LookRotation(dirPlano);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotDeseada, Time.deltaTime * 4f);
        }

        // Movimiento hacia el punto
        transform.position += transform.forward * velocidadPatrulla * Time.deltaTime;
    }

    void EjecutarAlerta()
    {
        // Comportamiento simple de alerta: mirar hacia el jugador si lo conoce, pero no perseguir fuertemente.
        if (jugadorTransform != null)
        {
            Vector3 direccion = jugadorTransform.position - transform.position;
            direccion.y = 0;
            if (direccion != Vector3.zero)
            {
                Quaternion rotDeseada = Quaternion.LookRotation(direccion);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotDeseada, Time.deltaTime * 5f);
            }
        }

        // Pequeño movimiento de aproximación lento si está dentro de la esfera
        if (jugadorTransform != null && Vector3.Distance(transform.position, jugadorTransform.position) <= radioAlerta)
        {
            transform.position = Vector3.MoveTowards(transform.position, jugadorTransform.position, velocidadPatrulla * 0.6f * Time.deltaTime);
        }
    }

    void EjecutarPersecucion()
    {
        if (jugadorTransform == null) return;

        // Mirar al jugador y moverse rápido hacia él
        Vector3 direccion = jugadorTransform.position - transform.position;
        direccion.y = 0;
        if (direccion != Vector3.zero)
        {
            Quaternion rotDeseada = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotDeseada, Time.deltaTime * 8f);
        }

        transform.position = Vector3.MoveTowards(transform.position, jugadorTransform.position, velocidadPersecucion * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        // Dibuja esfera de detección
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, radioAlerta);

        // Dibuja campo de visión (aproximado con líneas)
        Gizmos.color = Color.cyan;
        Vector3 origen = transform.position + Vector3.up * alturaOjos;
        float halfAngle = anguloVision * 0.5f;
        Vector3 dirIzq = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        Vector3 dirDer = Quaternion.Euler(0, halfAngle, 0) * transform.forward;
        Gizmos.DrawLine(origen, origen + dirIzq.normalized * distanciaVision);
        Gizmos.DrawLine(origen, origen + dirDer.normalized * distanciaVision);

        // Barra de progreso de detección (visualmente en escena, encima del enemigo)
        Vector3 barraBase = transform.position + Vector3.up * (alturaOjos + 1.2f);
        Vector3 dir = Vector3.right * 0.75f;
        Gizmos.color = Color.black;
        Gizmos.DrawCube(barraBase + Vector3.up * 0.01f, new Vector3(1.6f, 0.12f, 0.01f));
        Gizmos.color = Color.Lerp(Color.green, Color.red, progresoDeteccion);
        Gizmos.DrawCube(barraBase - dir * 0.8f + Vector3.right * progresoDeteccion * 1.6f, new Vector3(progresoDeteccion * 1.6f, 0.1f, 0.01f));
    }
}