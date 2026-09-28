using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(AudioSource))]
public class ComportamientoEnemigo : MonoBehaviour
{
    public enum Estado { Patrulla, Alerta, Persecucion }

    [Header("Patrulla")]
    public Transform[] puntosPatrulla;
    public float velocidadPatrulla = 2f;
    public float esperaEnPunto = 1.2f;

    [Header("Detección (raycast)")]
    public float distanciaVision = 12f;
    [Range(0, 180)] public float anguloVision = 60f;
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

    [Header("Audio")]
    public AudioClip sonidoAlerta;
    public AudioClip sonidoPersecucion;
    public AudioClip sonidoFinAlerta; // Sonido al volver de Alerta -> Patrulla
    [Range(0f, 1f)] public float volumenSonido = 1f;

    [Header("Feedback visual")]
    public bool crearBarraRuntime = true; // activar/desactivar barra en tiempo de ejecución
    public float offsetAlturaBarra = 1.2f; // altura relativa sobre la cabeza
    public float anchoBarra = 1.6f; // ancho total de la barra completa

    // Estado interno
    private Estado estadoActual = Estado.Patrulla;
    private Estado estadoPrevio = Estado.Patrulla;
    private int indicePunto = 0;
    private float contadorEspera = 0f;
    private float progresoDeteccion = 0f; // 0..1
    private Transform jugadorTransform;
    private Renderer rend;
    private AudioSource fuenteAudio;
    private bool yaPerdido = false;

    // Barra runtime
    private GameObject barraVisual;
    private Material matBarra;

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
        else Debug.LogWarning($"Enemigo: No se ha encontrado ningún GameObject con tag '{tagJugador}'.");

        if (crearBarraRuntime)
        {
            CrearBarraRuntime();
        }
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
                // Comprobar línea de visión: raycast que evalúa el primer hit
                RaycastHit hit;
                if (Physics.Raycast(origen, dirJugador, out hit, distanciaJugador))
                {
                    // Si el primer collider alcanzado es el jugador -> visible
                    if (hit.collider.CompareTag(tagJugador))
                    {
                        visiblePorRaycast = true;
                    }
                    else
                    {
                        // Golpeó algo antes que el jugador => obstáculo que bloquea la visión
                        visiblePorRaycast = false;
                    }
                }
                else
                {
                    // No golpeó nada dentro de la distancia de visión → considerar no visible (o visible según tu lógica)
                    visiblePorRaycast = false;
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
        if (progresoDeteccion >= umbralDeteccion)
        {
            estadoActual = Estado.Persecucion;
            if (!yaPerdido)
            {
                Debug.Log("Jugador detectado!");
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

        // Detectar cambio de estado para emitir sonido
        if (estadoActual != estadoPrevio)
        {
            OnEstadoCambiado(estadoPrevio, estadoActual);
            estadoPrevio = estadoActual;
        }

        // Visual feedback (color)
        if (rend != null)
        {
            switch (estadoActual)
            {
                case Estado.Patrulla: rend.material.color = colorPatrulla; break;
                case Estado.Alerta: rend.material.color = colorAlerta; break;
                case Estado.Persecucion: rend.material.color = colorPersecucion; break;
            }
        }

        // Actualizar barra runtime si existe
        if (barraVisual != null && matBarra != null)
        {
            // Color según progreso (verde->rojo)
            matBarra.color = Color.Lerp(colorPatrulla, colorPersecucion, progresoDeteccion);

            // Escala X proporcional al progreso
            float currentWidth = anchoBarra * progresoDeteccion;
            currentWidth = Mathf.Max(0.01f, currentWidth); // evitar escala cero absoluta
            barraVisual.transform.localScale = new Vector3(currentWidth, 0.12f, 1f);

            // Alinear izquierda: centro = -ancho/2 + currentWidth/2
            float centerX = -anchoBarra * 0.5f + currentWidth * 0.5f;
            barraVisual.transform.localPosition = new Vector3(centerX, alturaOjos + offsetAlturaBarra, 0f);

            // Billboard hacia cámara principal (si existe)
            if (Camera.main != null)
            {
                barraVisual.transform.rotation = Quaternion.LookRotation(barraVisual.transform.position - Camera.main.transform.position);
            }

            // Opcional: ocultar cuando 0
            barraVisual.SetActive(progresoDeteccion > 0.001f);
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
    }

    private void OnEstadoCambiado(Estado anterior, Estado actual)
    {
        if (fuenteAudio == null) return;

        fuenteAudio.volume = volumenSonido;

        // Reproducción específica para la transición Alerta -> Patrulla
        if (anterior == Estado.Alerta && actual == Estado.Patrulla)
        {
            if (sonidoFinAlerta != null)
            {
                fuenteAudio.loop = false;
                fuenteAudio.clip = sonidoFinAlerta;
                fuenteAudio.Play();
            }
            else
            {
                // Si no hay clip asignado, detener audio por si quedaba algo sonando
                fuenteAudio.Stop();
            }
            return;
        }

        // Comportamiento por estado de llegada
        switch (actual)
        {
            case Estado.Alerta:
                if (sonidoAlerta != null)
                {
                    fuenteAudio.loop = false;
                    fuenteAudio.clip = sonidoAlerta;
                    fuenteAudio.Play();
                }
                break;
            case Estado.Persecucion:
                if (sonidoPersecucion != null)
                {
                    fuenteAudio.loop = false;
                    fuenteAudio.clip = sonidoPersecucion;
                    fuenteAudio.Play();
                }
                break;
            case Estado.Patrulla:

                fuenteAudio.Stop();
                break;
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

    // --- Métodos auxiliares para la barra runtime ---
    private void CrearBarraRuntime()
    {
        // Crear quad simple como barra
        barraVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        barraVisual.name = $"{name}_BarraDeteccion";
        barraVisual.transform.SetParent(transform, false);

        // Quitar collider
        Collider col = barraVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Material sencillo (no afectado por iluminación)
        matBarra = new Material(Shader.Find("Unlit/Color"));
        matBarra.color = Color.green;
        var rendBarra = barraVisual.GetComponent<Renderer>();
        if (rendBarra != null) rendBarra.sharedMaterial = matBarra;

        // Ajuste inicial: ancho mínimo
        barraVisual.transform.localScale = new Vector3(0.01f, 0.12f, 1f);
        barraVisual.transform.localPosition = new Vector3(-anchoBarra * 0.5f, alturaOjos + offsetAlturaBarra, 0f);
    }

    private void OnDestroy()
    {
        if (matBarra != null)
        {
            Destroy(matBarra);
        }
        if (barraVisual != null)
        {
            Destroy(barraVisual);
        }
    }
}
