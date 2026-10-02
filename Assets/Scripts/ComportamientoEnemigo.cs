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
    public float radioAlerta = 6f;

    [Header("Dinámica de detección progresiva")]
    public float tasaIncremento = 0.6f;
    public float tasaIncrementoCercania = 1.0f;
    public float tasaDecrecimiento = 0.4f;
    [Range(0f, 1f)] public float umbralAlerta = 0.25f;
    [Range(0f, 1f)] public float umbralDeteccion = 1f;

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
    public AudioClip sonidoFinAlerta;
    [Range(0f, 1f)] public float volumenSonido = 1f;

    [Header("Feedback visual")]
    public bool crearBarraRuntime = true;
    public float offsetAlturaBarra = 1.2f;
    public float anchoBarra = 1.6f;
    public bool mostrarBarraSiempre = true;

    // Estado interno
    private Estado estadoActual = Estado.Patrulla;
    private Estado estadoPrevio = Estado.Patrulla;
    private int indicePunto = 0;
    private float contadorEspera = 0f;
    private float progresoDeteccion = 0f;
    private Transform jugadorTransform;
    private Renderer rend;
    private AudioSource fuenteAudio;
    private bool yaPerdido = false;
    private GameObject barraVisual;
    private Material matBarra;
    private bool deteccionSuprimida = false;
    private float cachedRadioAlerta;
    private float cachedTasaIncrementoCercania;

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
        InvPotion.OnInvisibilityStateChanged += HandleInvisibilityStateChanged;
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
        InvPotion.OnInvisibilityStateChanged -= HandleInvisibilityStateChanged;
    }
    private void HandleInvisibilityStateChanged(GameObject player, bool active)
    {
        if (jugadorTransform == null) return;
        if (player != jugadorTransform.gameObject) return;

        if (active)
        {
            if (!deteccionSuprimida)
            {
                cachedRadioAlerta = radioAlerta;
                cachedTasaIncrementoCercania = tasaIncrementoCercania;

                radioAlerta = 0f;
                tasaIncrementoCercania = 0f;

                deteccionSuprimida = true;
            }
        }
        else
        {
            if (deteccionSuprimida)
            {
                radioAlerta = cachedRadioAlerta;
                tasaIncrementoCercania = cachedTasaIncrementoCercania;

                deteccionSuprimida = false;
            }
        }
    }

    void Update()
    {
        bool visiblePorRaycast = false;
        bool dentroEsfera = false;

        if (jugadorTransform != null)
        {
            Vector3 origen = transform.position + Vector3.up * alturaOjos;
            Vector3 dirJugador = (jugadorTransform.position - origen).normalized;
            float distanciaJugador = Vector3.Distance(origen, jugadorTransform.position);

            float angulo = Vector3.Angle(transform.forward, dirJugador);
            if (angulo <= anguloVision * 0.5f && distanciaJugador <= distanciaVision)
            {
                RaycastHit hit;
                if (Physics.Raycast(origen, dirJugador, out hit, distanciaJugador))
                {
                    if (hit.collider.CompareTag(tagJugador))
                    {
                        visiblePorRaycast = true;
                    }
                    else
                    {
                        visiblePorRaycast = false;
                    }
                }
                else
                {
                    visiblePorRaycast = false;
                }
            }
            if (Vector3.Distance(transform.position, jugadorTransform.position) <= radioAlerta)
            {
                dentroEsfera = true;
            }
        }

        float incremento = 0f;
        if (visiblePorRaycast)
        {
            incremento += tasaIncremento * Time.deltaTime;
            if (dentroEsfera) incremento += tasaIncrementoCercania * Time.deltaTime;
        }
        else if (dentroEsfera)
        {
            incremento += (tasaIncremento * 0.4f) * Time.deltaTime;
        }
        else
        {
            progresoDeteccion -= tasaDecrecimiento * Time.deltaTime;
        }

        progresoDeteccion += incremento;
        progresoDeteccion = Mathf.Clamp01(progresoDeteccion);

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

        if (estadoActual != estadoPrevio)
        {
            OnEstadoCambiado(estadoPrevio, estadoActual);
            estadoPrevio = estadoActual;
        }

        if (rend != null)
        {
            switch (estadoActual)
            {
                case Estado.Patrulla: rend.material.color = colorPatrulla; break;
                case Estado.Alerta: rend.material.color = colorAlerta; break;
                case Estado.Persecucion: rend.material.color = colorPersecucion; break;
            }
        }

        if (barraVisual != null && matBarra != null)
        {
            matBarra.color = Color.Lerp(colorPatrulla, colorPersecucion, progresoDeteccion);

            float currentWidth = anchoBarra * progresoDeteccion;
            currentWidth = Mathf.Max(0.01f, currentWidth);
            barraVisual.transform.localScale = new Vector3(currentWidth, 0.12f, 1f);

            float centerX = -anchoBarra * 0.5f + currentWidth * 0.5f;
            barraVisual.transform.localPosition = new Vector3(centerX, alturaOjos + offsetAlturaBarra, 0f);

            if (Camera.main != null)
            {
                barraVisual.transform.rotation = Quaternion.LookRotation(barraVisual.transform.position - Camera.main.transform.position);
            }

            barraVisual.SetActive(progresoDeteccion > 0.001f);
        }

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
                fuenteAudio.Stop();
            }
            return;
        }

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
            contadorEspera += Time.deltaTime;
            if (contadorEspera >= esperaEnPunto)
            {
                indicePunto = (indicePunto + 1) % puntosPatrulla.Length;
                contadorEspera = 0f;
            }
            return;
        }

        if (dirPlano != Vector3.zero)
        {
            Quaternion rotDeseada = Quaternion.LookRotation(dirPlano);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotDeseada, Time.deltaTime * 4f);
        }

        transform.position += transform.forward * velocidadPatrulla * Time.deltaTime;
    }

    void EjecutarAlerta()
    {
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

        if (jugadorTransform != null && Vector3.Distance(transform.position, jugadorTransform.position) <= radioAlerta)
        {
            transform.position = Vector3.MoveTowards(transform.position, jugadorTransform.position, velocidadPatrulla * 0.6f * Time.deltaTime);
        }
    }

    void EjecutarPersecucion()
    {
        if (jugadorTransform == null) return;

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
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, radioAlerta);
        Gizmos.color = Color.cyan;
        Vector3 origen = transform.position + Vector3.up * alturaOjos;
        float halfAngle = anguloVision * 0.5f;
        Vector3 dirIzq = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        Vector3 dirDer = Quaternion.Euler(0, halfAngle, 0) * transform.forward;
        Gizmos.DrawLine(origen, origen + dirIzq.normalized * distanciaVision);
        Gizmos.DrawLine(origen, origen + dirDer.normalized * distanciaVision);
        Vector3 barraBase = transform.position + Vector3.up * (alturaOjos + 1.2f);
        Vector3 dir = Vector3.right * 0.75f;
        Gizmos.color = Color.black;
        Gizmos.DrawCube(barraBase + Vector3.up * 0.01f, new Vector3(1.6f, 0.12f, 0.01f));
        Gizmos.color = Color.Lerp(Color.green, Color.red, progresoDeteccion);
        Gizmos.DrawCube(barraBase - dir * 0.8f + Vector3.right * progresoDeteccion * 1.6f, new Vector3(progresoDeteccion * 1.6f, 0.1f, 0.01f));
    }

    private void CrearBarraRuntime()
    {
        barraVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        barraVisual.name = $"{name}_BarraDeteccion";
        barraVisual.transform.SetParent(transform, false);

        var meshCol = barraVisual.GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            DestroyImmediate(meshCol);
        }
        else
        {
            Collider col = barraVisual.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        matBarra = new Material(Shader.Find("Unlit/Color"));
        matBarra.color = Color.green;
        var rendBarra = barraVisual.GetComponent<Renderer>();
        if (rendBarra != null) rendBarra.sharedMaterial = matBarra;

        barraVisual.transform.localScale = new Vector3(0.01f, 0.12f, 1f);
        barraVisual.transform.localPosition = new Vector3(-anchoBarra * 0.5f, alturaOjos + offsetAlturaBarra, 0f);
    }
}
