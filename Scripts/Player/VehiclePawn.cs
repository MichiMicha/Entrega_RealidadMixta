using UnityEngine;

using UnityEngine.XR;



[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]

public class VehiclePawn : Pawn, ISimulable, IInteractable

{

    [Header("Parámetros")]

    [Tooltip("Masa m (kg).")]

    public float mass = 1200f;



    [Tooltip("Fuerza máxima del motor (N).")]

    public float engineForce = 6000f;



    [Tooltip("Fuerza máxima del freno (N). Detenido, frenar da reversa con la mitad de engineForce.")]

    public float brakeForce = 9000f;



    [Tooltip("Distancia entre ejes L (m).")]

    public float wheelBase = 2.5f;



    [Tooltip("Ángulo máximo de las llantas delanteras δ (grados).")]

    public float maxSteerAngle = 30f;



    [Header("Fenómenos físicos")]

    [Tooltip("Resistencia del aire k (N·s²/m²): F = -k·|v|·v. Define la velocidad máxima ≈ √(engineForce / k). " +

             "El valor real (½·ρ·Cd·A) sería ~0.4; aquí es mayor para que se note en el juego.")]

    public float airDragCoefficient = 8f;



    [Tooltip("Coeficiente de rodadura Crr: F = Crr·m·g. Es lo que lo detiene solo al soltar el acelerador.")]

    public float rollingResistance = 0.05f;



    [Tooltip("Fricción de las llantas μ. El agarre lateral máximo es μ·g; si se pide más, derrapa.")]

    public float tireFriction = 1.3f;



    [Tooltip("Rigidez del agarre lateral (1/s): qué tan rápido las llantas quitan el deslizamiento.")]

    public float gripStiffness = 6f;



    [Tooltip("Al derrapar, la fricción baja a este porcentaje (fricción cinética < estática).")]

    [Range(0.2f, 1f)] public float slideFriction = 0.8f;



    [Tooltip("Velocidad (m/s) a la que el giro de las llantas se reduce a la cuarta parte. Evita giros imposibles a alta velocidad.")]

    public float steerFalloffSpeed = 12f;



    [Header("Derrape al frenar (modo clásico)")]

    [Tooltip("Sin freno de mano: frenar fuerte en una curva, a buena velocidad, suelta las llantas y el carro derrapa " +

             "mientras sigue desacelerando. Velocidad mínima (m/s) para que pase.")]

    public float brakeDriftMinSpeed = 4f;



    [Tooltip("Cuánto hay que pisar el freno (0..1) para que derrape.")]

    [Range(0.05f, 1f)] public float brakeDriftMinBrake = 0.3f;



    [Tooltip("Cuánto hay que girar el timón (0..1) mientras se frena para que derrape. Frenando en recta no derrapa.")]

    [Range(0.05f, 1f)] public float brakeDriftMinSteer = 0.25f;



    [Tooltip("Al derrapar frenando el carro gira más (se le va la cola). Multiplica el giro.")]

    [Range(1f, 2f)] public float brakeDriftYawBoost = 1.25f;



    [Header("Suspensión y cabeceo (chasis)")]

    [Tooltip("Longitud natural del resorte (m): hasta qué distancia 'alcanzan' las llantas hacia el suelo.")]

    public float suspensionLength = 0.5f;



    [Tooltip("Frecuencia propia f (Hz). Rigidez de cada resorte: k = (m/4)·(2π·f)². Más alta = más dura.")]

    public float suspensionFrequency = 1.8f;



    [Tooltip("Amortiguamiento ζ al comprimirse. ~0.9 = el chasis se asienta sin rebotar como pelota; " +

             "el rebote que se siente al bajar una loma lo hace la cabina (ver 'Rebote dentro del carro').")]

    [Range(0.05f, 1f)] public float suspensionDamping = 0.9f;



    [Tooltip("Multiplica el amortiguamiento cuando el resorte se extiende. >1 = el carro vuelve a subir despacio, sin salir disparado.")]

    [Range(1f, 3f)] public float reboundDamping = 1.2f;



    [Tooltip("Tope del resorte (m): cuánto se puede hundir de más antes de que el tope lo frene en seco.")]

    public float bumpStop = 0.08f;



    [Tooltip("Pendiente máxima (grados) que cuenta como suelo. Más empinado se trata como pared.")]

    public float maxGroundAngle = 50f;



    [Tooltip("Altura del centro de masa sobre el suelo (m). Más alto = cabecea y se balancea más.")]

    public float centerOfMassHeight = 0.5f;



    [Tooltip("Roce con el aire del giro (1/s). En el aire el cabeceo se mantiene: si la nariz empezó a bajar, sigue bajando hasta tocar el suelo.")]

    public float airAngularDamping = 0.3f;



    [Tooltip("Cabeceo máximo (grados, nariz arriba o abajo).")]

    public float maxPitch = 40f;



    [Tooltip("Balanceo máximo (grados, hacia un lado u otro).")]

    public float maxRoll = 30f;



    [Header("Aterrizaje (solo avisa con OnLanded, no rebota)")]

    [Tooltip("Impacto mínimo (m/s, velocidad vertical al tocar el suelo) para que cuente como aterrizaje.")]

    public float landingMinImpact = 1.5f;



    [Tooltip("Tiempo mínimo en el aire (s) para que cuente como aterrizaje. Evita avisos por baches pequeños.")]

    public float landingMinAirTime = 0.1f;



    [Header("Parqueo en pendientes")]

    [Tooltip("Sin acelerador ni freno, la fricción estática lo sostiene en pendientes menores a esto (grados).")]

    public float parkMaxAngle = 25f;



    [Tooltip("Por debajo de esta velocidad (m/s) el carro queda quieto (fricción estática).")]

    public float parkSpeed = 0.3f;



    [Header("Choques")]

    [Tooltip("Coeficiente de restitución e del choque contra obstáculos. 0 = se queda pegado, 1 = rebote perfecto.")]

    [Range(0f, 1f)] public float collisionRestitution = 0.25f;



    [Tooltip("Fricción al rozar una pared: porcentaje de la velocidad paralela a la pared que se pierde en cada contacto.")]

    [Range(0f, 1f)] public float collisionFriction = 0.2f;



    [Header("Estilo de conducción")]

    [Tooltip("Activo: la conducción de antes (aceleración suave, derrape por deslizamiento lateral, giro por velocidad " +

             "angular). Apagado: el modelo de fuerzas con círculo de fricción y modelo de bicicleta.")]

    public bool classicDriving = true;



    [Header("Clásico: motor y frenos (aceleraciones, m/s²)")]

    [Tooltip("Qué tan fuerte acelera (m/s²).")]

    public float engineAcceleration = 8f;

    [Tooltip("Qué tan rápido sube el acelerador al presionar (por segundo). Da la aceleración progresiva.")]

    public float throttleRise = 2.5f;

    [Tooltip("Qué tan rápido baja al soltar (por segundo). Da la desaceleración progresiva.")]

    public float throttleFall = 3f;

    [Tooltip("Frenada al presionar el freno mientras avanza (m/s²).")]

    public float brakeDeceleration = 14f;

    public float reverseAcceleration = 4f;

    public float maxReverseSpeed = 5f;

    [Tooltip("Desaceleración natural al soltar el acelerador (m/s²).")]

    public float classicRolling = 1.2f;

    [Tooltip("Resistencia del aire: a = -k·|v|·v. Velocidad máxima ≈ √((aceleración - rodadura) / k) ≈ 18 m/s.")]

    public float classicAirDrag = 0.02f;

    [Tooltip("Presiona el carro contra el suelo según la velocidad (m/s² por m/s). Valores altos lo pegan al suelo " +

             "y no deja que se 'despegue' en las crestas de las lomas.")]

    public float classicDownforce = 0.1f;

    [Tooltip("Sin acelerar y por debajo de esta velocidad, no se desliza por la loma (m/s).")]

    public float classicParkingSpeed = 1f;



    [Header("Clásico: dirección")]

    [Tooltip("Giro máximo (grados por segundo).")]

    public float maxSteeringRate = 70f;

    public float steeringSmoothing = 6f;

    [Range(0f, 0.9f)]

    [Tooltip("A alta velocidad gira menos.")]

    public float highSpeedSteerReduction = 0.25f;

    [Range(0f, 0.3f)] public float steerDeadZone = 0.05f;



    [Header("Clásico: agarre y derrape")]

    [Tooltip("Agarre normal: más alto = el carro se pega más a la dirección.")]

    public float normalGrip = 8f;

    [Tooltip("Agarre mientras derrapa: más bajo = se desliza más.")]

    public float driftGrip = 2.2f;

    [Tooltip("Si el deslizamiento lateral supera esto (m/s), empieza el derrape.")]

    public float driftStartLateralSpeed = 2f;

    [Tooltip("Cuando baja de esto (m/s), el derrape termina.")]

    public float driftEndLateralSpeed = 0.8f;

    [Tooltip("Cuánta velocidad pierde al derrapar.")]

    public float driftSpeedLoss = 0.5f;



    [Header("Rebote dentro del carro (lomas hacia abajo a gran velocidad)")]

    [Tooltip("Frecuencia propia de la cabina sobre el chasis (Hz). Más alta = el rebote es más rápido y seco.")]

    public float cabinFrequency = 2.2f;

    [Tooltip("Amortiguamiento ζ de la cabina. Bajo = rebota varias veces; alto = un solo golpe.")]

    [Range(0.05f, 1f)] public float cabinDamping = 0.2f;

    [Tooltip("Qué tanto de la aceleración vertical del chasis le llega a la cabina. 1 = tal cual; menos = más suave; " +

             "más = exagerado.")]

    public float cabinBounceStrength = 0.3f;

    [Tooltip("Máximo desplazamiento vertical de la cabina (m).")]

    public float cabinBounceMax = 0.15f;



    [Header("Inclinación de la cabina (acelerar, frenar, curvas)")]

    [Tooltip("Cabeceo al acelerar/frenar (grados por m/s²).")]

    public float pitchFromAccel = 0.3f;

    [Tooltip("Inclinación en curvas y derrapes (grados por m/s de deslizamiento lateral).")]

    public float rollFromLateral = 1.2f;

    public float maxCabinPitch = 6f;

    public float maxCabinRoll = 8f;

    public float tiltSmoothing = 8f;



    [Header("Cámara y cabina")]

    [Tooltip("Puntos donde se pone la cámara; V cambia entre ellos. El primero es el inicial.")]

    public Transform[] views;



    [Tooltip("La vista que orbita con mouse / stick (3ª persona). Debe ser hija directa del vehículo.")]

    public Transform orbitView;



    [Tooltip("Opcional: este objeto gira con el timón (se ve desde la vista en 1ª persona).")]

    public Transform steeringWheel;



    [Header("Estado (solo lectura)")]

    public Vector3 x;

    public Vector3 v;

    public float heading;

    public float throttle;

    public float brake;

    public float steer;

    public bool sliding;       // true si las llantas pierden agarre (derrape)

    public bool grounded;      // true si al menos una llanta toca el suelo (false = en el aire)

    public float impactSpeed;  // velocidad (m/s) del último choque contra un obstáculo en este paso (0 = sin choque)

    public float pitchAngle;   // cabeceo del chasis (grados). Positivo = nariz hacia abajo

    public float rollAngle;    // balanceo del chasis (grados). Positivo = se inclina hacia la izquierda

    public float cabinOffset;  // desplazamiento vertical de la cabina (m). Negativo = se hunde



    public event System.Action<float> OnLanded;



    public Vector3 Forward => Vector3.ProjectOnPlane(Quaternion.Euler(0f, heading, 0f) * Vector3.forward, groundNormal).normalized;



    public Vector3 Right => Vector3.Cross(groundNormal, Forward);



    public float Speed => Vector3.Dot(v, Forward);



    public int CurrentView { get; private set; }



    private Rigidbody rb;

    private BoxCollider box;

    private Vector3 x0;

    private float heading0;

    private Pawn driver; // quien se subió, para bajarlo después

    private float lookYaw;

    private float lookPitch;



    // Suelo y orientación del cuerpo.

    private Vector3[] wheelOffsets;               // posición de cada llanta respecto al centro del carro (m)

    private Vector3 groundNormal = Vector3.up;    // normal promedio del suelo bajo las llantas

    private Quaternion bodyRotation = Quaternion.identity;

    private Vector3 suspensionForce;              // suma de los 4 resortes en este paso

    private Vector3 suspensionTorque;             // par de los 4 resortes respecto al centro de masa

    private float pitch, roll;                    // inclinación del chasis (rad)

    private float pitchRate, rollRate;            // velocidad de giro del cabeceo y del balanceo (rad/s)

    private Vector3 bodySize;                     // largo, alto y ancho del carro (m), para la inercia

    private Vector3 comLocal;                     // centro de masa en coordenadas del carro (m)

    private readonly RaycastHit[] rayHits = new RaycastHit[16];

    private readonly Collider[] overlaps = new Collider[16];



    // Aterrizaje.

    private float airTime;                 // segundos seguidos sin tocar el suelo

    private Vector3 airVelocity;           // velocidad justo antes de tocar el suelo



    // Rebote de la cabina (resorte sobre el chasis) e inclinación.

    private float bounceY, bounceVel;      // desplazamiento vertical de la cabina (m) y su velocidad

    private float prevVy, prevSpeed;

    private float tiltPitchTarget, tiltRollTarget, tiltPitch, tiltRoll;

    private Vector3[] viewBasePos;

    private Quaternion[] viewBaseRot;



    // Modo clásico.

    private float classicThrottle;         // acelerador suavizado (-1..1)

    private float classicSteering;         // giro suavizado (grados/s)

    private bool classicDrifting;

    private bool brakeDrifting;            // derrapando mientras frena



    void Awake()

    {

        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        box = GetComponent<BoxCollider>();

        x0 = transform.position;

        heading0 = transform.eulerAngles.y;



        // Pose original de cada vista de cámara: el rebote y la inclinación de la cabina se suman encima de esta.

        int viewCount = views != null ? views.Length : 0;

        viewBasePos = new Vector3[viewCount];

        viewBaseRot = new Quaternion[viewCount];

        for (int i = 0; i < viewCount; i++)

        {

            if (views[i] == null) continue;

            viewBasePos[i] = views[i].localPosition;

            viewBaseRot[i] = views[i].localRotation;

        }



        // Las 4 llantas se ponen en las esquinas del collider (delanteras primero), a la altura del centro del carro.

        Vector3 s = transform.lossyScale;

        float halfWidth = box.size.x * s.x * 0.5f * 0.85f;

        float halfLength = box.size.z * s.z * 0.5f * 0.75f;

        float centerZ = box.center.z * s.z;

        bodySize = Vector3.Scale(box.size, s);

        comLocal = new Vector3(0f, centerOfMassHeight, centerZ);

        wheelOffsets = new[]

        {

            new Vector3(-halfWidth, 0f, centerZ + halfLength), // delantera izquierda

            new Vector3( halfWidth, 0f, centerZ + halfLength), // delantera derecha

            new Vector3(-halfWidth, 0f, centerZ - halfLength), // trasera izquierda

            new Vector3( halfWidth, 0f, centerZ - halfLength), // trasera derecha

        };

    }



    public override void Drive(float throttle, float brake, float steer)

    {

        this.throttle = throttle;

        this.brake = brake;

        this.steer = steer;

    }



    public override void Look(Vector2 input)

    {

        if (XRSettings.isDeviceActive) return;



        if (views.Length > 0 && views[CurrentView] == orbitView) Orbit(input);

        else HeadLook(input);

    }



    void HeadLook(Vector2 input)

    {

        lookYaw = Mathf.Clamp(lookYaw + input.x, -120f, 120f);

        lookPitch = Mathf.Clamp(lookPitch - input.y, -60f, 60f);

        pawnCamera.transform.localRotation = Quaternion.Euler(lookPitch, lookYaw, 0f);

    }



    void Orbit(Vector2 input)

    {

        lookYaw += input.x;

        lookPitch = Mathf.Clamp(lookPitch - input.y, -25f, 50f);

        Quaternion orbit = Quaternion.Euler(lookPitch, lookYaw, 0f);



        // En coordenadas del vehículo: rotar la posición de la vista alrededor de su centro (~1 m arriba).

        Vector3 center = transform.TransformPoint(Vector3.up);

        Vector3 wanted = transform.TransformPoint(Vector3.up + orbit * (orbitView.localPosition - Vector3.up));



        // Si algo queda entre el centro y la cámara (suelo, pared), ponerla justo antes, separada 30 cm.

        Vector3 dir = wanted - center;

        if (Physics.Raycast(center, dir.normalized, out RaycastHit hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))

            wanted = hit.point + hit.normal * 0.3f;



        pawnCamera.transform.SetPositionAndRotation(wanted, transform.rotation * orbit * orbitView.localRotation);

    }



    public override void ChangeView()

    {

        if (views.Length == 0) return;

        CurrentView = (CurrentView + 1) % views.Length;

        pawnCamera.transform.SetParent(views[CurrentView], false);

        pawnCamera.transform.localPosition = Vector3.zero;

        pawnCamera.transform.localRotation = Quaternion.identity;

        lookYaw = 0f;

        lookPitch = 0f;

        Look(Vector2.zero);

    }



    void Update()

    {

        // Volante visual: un timón real da ~1.5 vueltas hacia cada lado; aquí basta con 135°.

        if (steeringWheel != null) steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -steer * 135f);



        UpdateCabinView(Time.deltaTime);

    }



    public void Simulate(float h)

    {

        // 0. Suelo: un rayo por llanta para saber si toca, qué tan comprimido está el resorte y la normal.

        ProbeGround(h);

        if (classicDriving) UpdateClassicInputs(h);



        // 1. Fuerzas.

        Vector3 F = classicDriving ? ComputeClassicForces(h) : ComputeForces(h);



        // 2. Integrar.

        Vector3 a = F / mass;

        v = v + a * h;

        x = x + v * h;



        // 2b. Fricción estática: quieto y sin pedales en una pendiente suave, se queda parqueado.

        ParkOnSlopes();



        // 3. Girar: ω = v / L · tan(δ). Quieto no gira, igual que un carro real.

        //    A mayor velocidad las llantas giran menos (si no, el carro daría vueltas imposibles).

        //    En el aire las llantas no agarran: no gira.

        if (grounded)

        {

            if (classicDriving)

            {

                heading += ClassicYawRate() * h; // grados/s

            }

            else

            {

                float falloff = 1f + (Speed / steerFalloffSpeed) * (Speed / steerFalloffSpeed);

                float delta = steer * maxSteerAngle * Mathf.Deg2Rad / falloff;

                float omega = Speed / wheelBase * Mathf.Tan(delta); // rad/s

                heading += omega * Mathf.Rad2Deg * h;

            }

        }



        // 4. Choques.

        CollideWithScene(h);



        // 5. Cabeceo y balanceo del chasis, rebote de la cabina, y mover el objeto de Unity.

        IntegrateAttitude(h);

        CabinInput(h);

        rb.MovePosition(x);

        rb.MoveRotation(bodyRotation);

    }



    /// <summary>

    /// Suspensión. Por cada llanta un rayo baja desde un punto sobre el carro.

    ///   compresión:  x_i = L - d_i          (d_i = distancia hasta el suelo, L = longitud natural)

    ///   velocidad:   x'_i = -(v_llanta · arriba),  v_llanta = v + ω × r

    ///   fuerza:      F_i = k·x_i + c·x'_i   (un resorte solo empuja, nunca jala: F_i ≥ 0)

    ///   par:         τ = Σ r_i × F_i        (hace cabecear y balancear al carro)

    /// con k = (m/4)·(2πf)² y c = 2ζ·√(k·m/4). Cada resorte soporta m/4, así que en reposo se

    /// comprime sag = m·g/(4k) y el rayo parte sag por debajo de L para que el carro quede a ras del suelo.

    /// </summary>

    void ProbeGround(float h)

    {

        float g = Physics.gravity.magnitude;

        float k = (mass / 4f) * Mathf.Pow(2f * Mathf.PI * suspensionFrequency, 2f);

        float c = 2f * suspensionDamping * Mathf.Sqrt(k * mass / 4f);

        float sag = mass * g / (4f * k);

        float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);



        Vector3 up = bodyRotation * Vector3.up;



        // Giro del cuerpo (cabeceo alrededor del eje derecho, balanceo alrededor del eje frontal):

        // sirve para saber qué tan rápido se mueve cada llanta: v_llanta = v + ω × r.

        Quaternion yawRotation = Quaternion.Euler(0f, heading, 0f);

        Vector3 omega = pitchRate * (yawRotation * Vector3.right) + rollRate * (yawRotation * Vector3.forward);

        comLocal.y = centerOfMassHeight;



        suspensionForce = Vector3.zero;

        suspensionTorque = Vector3.zero;

        Vector3 normalSum = Vector3.zero;

        int contacts = 0;



        for (int i = 0; i < wheelOffsets.Length; i++)

        {

            Vector3 r = bodyRotation * (wheelOffsets[i] - comLocal); // del centro de masa a la llanta

            Vector3 origin = x + bodyRotation * wheelOffsets[i] + up * (suspensionLength - sag);

            if (!RaycastGround(origin, -up, suspensionLength, minNormalY, out RaycastHit hit)) continue;



            float compression = suspensionLength - hit.distance;

            float compressionRate = -Vector3.Dot(v + Vector3.Cross(omega, r), up);

            // Amortiguador asimétrico: más fuerte al extenderse (compressionRate < 0) que al comprimirse.

            float damper = compressionRate < 0f ? c * reboundDamping : c;

            float f = k * compression + damper * compressionRate;



            // Tope: si se hunde más de lo normal, el tope lo frena en seco (evita atravesar el suelo).

            float overTravel = compression - (sag + bumpStop);

            if (overTravel > 0f) f += 6f * k * overTravel;



            if (f <= 0f) continue; // el resorte no jala

            suspensionForce += f * hit.normal;

            suspensionTorque += Vector3.Cross(r, f * hit.normal); // el resorte que empuja más hace girar el carro

            normalSum += hit.normal;

            contacts++;

        }



        bool wasGrounded = grounded;

        grounded = contacts > 0;

        groundNormal = grounded ? normalSum.normalized : Vector3.up;



        // Aterrizaje: toca el suelo después de haber estado en el aire. El impacto es la velocidad

        // que traía hacia el suelo: impacto = -(v · normal).

        if (!grounded)

        {

            airTime += h;

            airVelocity = v;

        }

        else

        {

            if (!wasGrounded && airTime >= landingMinAirTime)

            {

                float impact = Mathf.Max(0f, -Vector3.Dot(airVelocity, groundNormal));

                if (impact >= landingMinImpact) Land(impact);

            }

            airTime = 0f;

        }

    }



    bool RaycastGround(Vector3 origin, Vector3 dir, float length, float minNormalY, out RaycastHit best)

    {

        best = default;

        bool found = false;

        float bestDistance = float.MaxValue;



        int n = Physics.RaycastNonAlloc(origin, dir, rayHits, length, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)

        {

            RaycastHit hit = rayHits[i];

            if (hit.collider.transform.IsChildOf(transform)) continue; // el propio carro

            if (hit.normal.y < minNormalY) continue;                   // muy empinado: es pared, no suelo

            if (hit.distance < bestDistance)

            {

                bestDistance = hit.distance;

                best = hit;

                found = true;

            }

        }

        return found;

    }



    Vector3 ComputeForces(float h)

    {

        Vector3 F = Vector3.zero;

        float g = Physics.gravity.magnitude;

        float speed = v.magnitude;



        // Gravedad: F = m·g. Es la que hace caer el carro por una bajada.

        F += mass * Physics.gravity;



        // Suspensión: los 4 resortes (ya calculados en ProbeGround). Sostienen el carro.

        F += suspensionForce;



        // Arrastre del aire: F = -k·|v|·v. Crece con el cuadrado de la velocidad. Actúa también en el aire.

        F -= airDragCoefficient * speed * v;



        sliding = false;



        // Todo lo que sigue depende de las llantas: en el aire no hay motor, freno ni agarre.

        if (!grounded) return F;



        // Motor y freno, a lo largo del vehículo. Casi detenido, el mismo pedal de freno da reversa.

        float brakeAmount = Speed > 0.5f ? brake * brakeForce : brake * engineForce * 0.5f;

        float fLong = throttle * engineForce - brakeAmount;

        F += fLong * Forward;



        // Rodadura: F = -Crr·m·g, opuesta a la velocidad. Se limita para que

        // nunca invierta el sentido (así el carro se detiene del todo, sin temblar).

        if (speed > 0.001f)

        {

            float rolling = Mathf.Min(rollingResistance * mass * g, mass * speed / h);

            F -= rolling * (v / speed);

        }



        // Agarre lateral (llantas): quita la velocidad de lado (v · derecha).

        //   aceleración necesaria: a = -k·vLat

        //   límite de las llantas:  a_max = μ·g

        // Si se pide más de lo que aguantan, la llanta se suelta (fricción cinética) y el carro derrapa.

        float vLat = Vector3.Dot(v, Right);

        // Círculo de fricción: la llanta tiene un agarre total μ·g que reparte entre avanzar/frenar y

        // aguantar de lado: a_lat² + a_long² ≤ (μ·g)². Si frenas (o aceleras) fuerte en plena curva,

        // queda menos agarre lateral y el carro derrapa, sin necesidad de ninguna tecla.

        float mu = tireFriction;

        float aLong = Mathf.Abs(fLong) / mass;

        float aMax = Mathf.Sqrt(Mathf.Max(0f, mu * g * mu * g - aLong * aLong));

        float aNeeded = -gripStiffness * vLat;



        sliding = Mathf.Abs(aNeeded) > aMax;

        float aLat = sliding ? -Mathf.Sign(vLat) * aMax * slideFriction : aNeeded;

        F += mass * aLat * Right;



        return F;

    }





    float ClassicTopSpeed => Mathf.Sqrt(Mathf.Max(0.01f, engineAcceleration - classicRolling) / Mathf.Max(0.0001f, classicAirDrag));



    static float ApplyDeadZone(float value, float deadZone)

    {

        if (Mathf.Abs(value) < deadZone) return 0f;

        return Mathf.Sign(value) * (Mathf.Abs(value) - deadZone) / (1f - deadZone);

    }



    void UpdateClassicInputs(float h)

    {

        float input = Mathf.Clamp(throttle - brake, -1f, 1f);

        bool rising = Mathf.Abs(input) > Mathf.Abs(classicThrottle) && input * classicThrottle >= 0f;

        classicThrottle = Mathf.MoveTowards(classicThrottle, input, (rising ? throttleRise : throttleFall) * h);



        float targetSteering = ApplyDeadZone(steer, steerDeadZone) * maxSteeringRate;

        classicSteering = Mathf.Lerp(classicSteering, targetSteering, 1f - Mathf.Exp(-steeringSmoothing * h));

    }



    float ClassicYawRate()

    {

        float absFwd = Mathf.Abs(Speed);

        float dirSign = Speed >= 0f ? 1f : -1f;

        float steerFactor = dirSign * Mathf.Clamp01(absFwd / 2f);

        float speedReduction = 1f - highSpeedSteerReduction * Mathf.Clamp01(absFwd / ClassicTopSpeed);

        float yawRate = classicSteering * steerFactor * speedReduction;

        return brakeDrifting ? yawRate * brakeDriftYawBoost : yawRate; // al derrapar frenando se le va la cola

    }



    Vector3 ComputeClassicForces(float h)

    {

        float speed = v.magnitude;



        // Peso + los 4 resortes de la suspensión (sostienen el carro y siguen las lomas).

        Vector3 F = mass * Physics.gravity + suspensionForce;



        // 1) RESISTENCIA DEL AIRE: siempre, incluso en el aire. a = -k·|v|·v

        F -= mass * classicAirDrag * speed * v;



        sliding = false;

        if (!grounded)

        {

            classicDrifting = false; // en el aire no hay motor, ni freno, ni agarre

            brakeDrifting = false;

            return F;

        }



        Vector3 fwd = Forward;

        Vector3 right = Right;

        float fwdSpeed = Speed;

        float lateral = Vector3.Dot(v, right);

        float absFwd = Mathf.Abs(fwdSpeed);

        float dirSign = fwdSpeed >= 0f ? 1f : -1f;

        float input = Mathf.Clamp(throttle - brake, -1f, 1f);



        // 2) ACELERACIÓN / FRENADO / DESACELERACIÓN

        float accel = 0f;

        if (input < -0.01f && fwdSpeed > 0.5f)

            accel = input * brakeDeceleration;                          // frenando

        else if (classicThrottle > 0.01f)

            accel = classicThrottle * engineAcceleration;               // acelerando

        else if (classicThrottle < -0.01f && fwdSpeed > -maxReverseSpeed)

            accel = classicThrottle * reverseAcceleration;              // reversa



        // Rodadura (desaceleración natural). Nunca invierte el sentido del movimiento.

        accel -= dirSign * Mathf.Min(classicRolling, absFwd / h);



        // 3) DERRAPE (con histéresis: empieza con un deslizamiento y termina con otro menor).

        //    Frenar fuerte, a buena velocidad y con el timón girado suelta las llantas: el freno hace el papel

        //    que tenía el freno de mano. Mientras derrapa sigue desacelerando (la frenada de arriba no se quita).

        //    También empieza solo si el deslizamiento lateral es grande (una curva tomada muy rápido).

        bool braking = input < -0.01f && fwdSpeed > 0.5f;

        bool wantsSlide = braking && -input >= brakeDriftMinBrake && absFwd > brakeDriftMinSpeed

                          && Mathf.Abs(steer) > brakeDriftMinSteer;

        if (!classicDrifting && (wantsSlide || Mathf.Abs(lateral) > driftStartLateralSpeed))

            classicDrifting = true;

        else if (classicDrifting && !wantsSlide && Mathf.Abs(lateral) < driftEndLateralSpeed)

            classicDrifting = false;

        sliding = classicDrifting;

        brakeDrifting = classicDrifting && braking;



        float grip = classicDrifting ? driftGrip : normalGrip;



        // El agarre elimina poco a poco la velocidad lateral: v_lat' = v_lat·e^(-grip·h)

        float newLateral = lateral * Mathf.Exp(-grip * h);

        F += mass * (newLateral - lateral) / h * right;



        // Al derrapar se pierde velocidad

        if (classicDrifting)

            accel -= dirSign * Mathf.Abs(lateral) * driftSpeedLoss;



        F += mass * accel * fwd;



        // 4) FUERZAS DEL SUELO

        F -= mass * classicDownforce * absFwd * groundNormal;



        // Casi quieto y sin acelerar: no se desliza por la loma

        if (Mathf.Abs(classicThrottle) < 0.05f && Mathf.Abs(input) < 0.05f && absFwd < classicParkingSpeed)

            F -= mass * Vector3.ProjectOnPlane(Physics.gravity, groundNormal);



        return F;

    }



    void ParkOnSlopes()

    {

        if (!grounded || throttle > 0.01f || brake > 0.01f) return;

        if (Vector3.Angle(groundNormal, Vector3.up) > parkMaxAngle) return;



        Vector3 vNormal = Vector3.Project(v, groundNormal);

        Vector3 vTangent = v - vNormal;

        if (vTangent.magnitude < parkSpeed) v = vNormal;

    }



    void CollideWithScene(float h)

    {

        impactSpeed = 0f;



        Vector3 s = transform.lossyScale;

        Vector3 center = x + bodyRotation * Vector3.Scale(box.center, s);

        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, s);

        float groundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);



        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, overlaps, bodyRotation, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)

        {

            Collider other = overlaps[i];

            if (other.transform.IsChildOf(transform)) continue;                                  // el propio carro

            if (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic) continue; // Unity lo empuja

            if (other is MeshCollider mesh && !mesh.convex) continue;                            // no soportado



            if (!Physics.ComputePenetration(

                    box, x, bodyRotation,

                    other, other.transform.position, other.transform.rotation,

                    out Vector3 n, out float depth)) continue;



            if (n.y > groundNormalY) continue; // suelo o rampa suave: lo maneja la suspensión



            x += n * depth;



            float vn = Vector3.Dot(v, n);

            if (vn >= 0f) continue; // ya se está alejando



            impactSpeed = Mathf.Max(impactSpeed, -vn);



            // Si llega muy despacio, no rebota (queda recostado contra la pared).

            float e = -vn < 0.5f ? 0f : collisionRestitution;

            Vector3 vNormal = vn * n;

            Vector3 vTangent = v - vNormal;

            v = vTangent * (1f - collisionFriction) - e * vNormal; // = v - (1 + e)(v·n)n, con fricción

        }

    }



    /// <summary>

    /// Cabeceo y balanceo del chasis como cuerpo rígido. Cada resorte empuja en la posición de su llanta,

    /// así que además de levantar el carro le da un par: τ = Σ rᵢ × Fᵢ (calculado en ProbeGround).

    ///   α = τ / I          I_cabeceo = m·(largo² + alto²)/12,   I_balanceo = m·(ancho² + alto²)/12

    ///   ω[n+1] = ω[n] + α·h,   ángulo[n+1] = ángulo[n] + ω[n+1]·h

    /// Por eso, al pasar la cresta de una bajada, las llantas delanteras pierden el suelo primero, solo las

    /// traseras empujan y la nariz baja; en el aire el giro se mantiene y el carro cae de frente, no todo parejo.

    /// </summary>

    void IntegrateAttitude(float h)

    {

        Quaternion yawRotation = Quaternion.Euler(0f, heading, 0f);

        float pitchInertia = mass * (bodySize.z * bodySize.z + bodySize.y * bodySize.y) / 12f;

        float rollInertia = mass * (bodySize.x * bodySize.x + bodySize.y * bodySize.y) / 12f;



        float pitchTorque = Vector3.Dot(suspensionTorque, yawRotation * Vector3.right);

        float rollTorque = Vector3.Dot(suspensionTorque, yawRotation * Vector3.forward);



        pitchRate += pitchTorque / pitchInertia * h;

        rollRate += rollTorque / rollInertia * h;



        // En el aire solo hay un poco de roce: el cabeceo que traía se mantiene.

        if (!grounded)

        {

            float damping = Mathf.Exp(-airAngularDamping * h);

            pitchRate *= damping;

            rollRate *= damping;

        }



        pitch += pitchRate * h;

        roll += rollRate * h;



        // Límites (si choca con el límite, deja de girar hacia ese lado).

        float maxP = maxPitch * Mathf.Deg2Rad;

        if (Mathf.Abs(pitch) > maxP)

        {

            pitch = Mathf.Sign(pitch) * maxP;

            if (pitchRate * pitch > 0f) pitchRate = 0f;

        }

        float maxR = maxRoll * Mathf.Deg2Rad;

        if (Mathf.Abs(roll) > maxR)

        {

            roll = Mathf.Sign(roll) * maxR;

            if (rollRate * roll > 0f) rollRate = 0f;

        }



        pitchAngle = pitch * Mathf.Rad2Deg;

        rollAngle = roll * Mathf.Rad2Deg;



        // Euler(x, y, z) aplica primero z (balanceo), luego x (cabeceo) y al final y (rumbo).

        bodyRotation = Quaternion.Euler(pitchAngle, heading, rollAngle);

    }



    void Land(float impact)

    {

        OnLanded?.Invoke(impact);

    }



    /// <summary>

    /// Rebote dentro del carro. La cabina es una masa sobre un resorte amortiguado montado en el chasis.

    /// Lo que la mueve es la aceleración vertical del chasis a_b (cambio de v.y en cada paso):

    ///   z'' = -ω²·z - 2ζω·z' - a_b          (z = desplazamiento de la cabina respecto al chasis, ω = 2π·f)

    /// Sobre terreno plano a_b = 0 y la cabina no se mueve. En la cresta de una bajada el chasis cae libre

    /// (a_b ≈ -g) y la cabina "flota" un poco; en el fondo o al aterrizar el chasis frena de golpe

    /// (a_b grande y positiva) y la cabina se hunde y rebota varias veces hasta apagarse.

    /// A mayor velocidad el cambio de pendiente y la caída son más fuertes, y el rebote también.

    /// </summary>

    void CabinInput(float h)

    {

        float chassisAccelY = Mathf.Clamp((v.y - prevVy) / h, -40f, 40f);

        prevVy = v.y;



        float w = 2f * Mathf.PI * cabinFrequency;

        bounceVel += (-w * w * bounceY - 2f * cabinDamping * w * bounceVel - cabinBounceStrength * chassisAccelY) * h;

        bounceY += bounceVel * h;

        if (Mathf.Abs(bounceY) > cabinBounceMax)

        {

            bounceY = Mathf.Sign(bounceY) * cabinBounceMax;

            if (bounceVel * bounceY > 0f) bounceVel = 0f; // tope del recorrido

        }

        cabinOffset = bounceY;



        // Inclinación suave de la cabina al acelerar, frenar y tomar curvas.

        float forwardAccel = (Speed - prevSpeed) / h;

        prevSpeed = Speed;

        tiltPitchTarget = Mathf.Clamp(-forwardAccel * pitchFromAccel, -maxCabinPitch, maxCabinPitch);

        tiltRollTarget = Mathf.Clamp(Vector3.Dot(v, Right) * rollFromLateral, -maxCabinRoll, maxCabinRoll);

    }



    /// <summary>Aplica el rebote y la inclinación a las vistas de cámara (la cámara va dentro de ellas).</summary>

    void UpdateCabinView(float dt)

    {

        if (views == null || views.Length == 0 || viewBasePos == null) return;



        float a = 1f - Mathf.Exp(-tiltSmoothing * Mathf.Min(dt, 0.033f));

        tiltPitch = Mathf.Lerp(tiltPitch, tiltPitchTarget, a);

        tiltRoll = Mathf.Lerp(tiltRoll, tiltRollTarget, a);



        for (int i = 0; i < views.Length; i++)

        {

            if (views[i] == null) continue;

            // El desplazamiento está en metros del mundo; se pasa a unidades locales (el carro tiene escala).

            float scaleY = views[i].parent != null ? views[i].parent.lossyScale.y : 1f;

            views[i].localPosition = viewBasePos[i] + new Vector3(0f, bounceY / Mathf.Max(0.0001f, scaleY), 0f);

            views[i].localRotation = viewBaseRot[i] * Quaternion.Euler(tiltPitch, 0f, tiltRoll);

        }

    }



    void RestoreViews()

    {

        if (views == null || viewBasePos == null) return;

        for (int i = 0; i < views.Length && i < viewBasePos.Length; i++)

        {

            if (views[i] == null) continue;

            views[i].localPosition = viewBasePos[i];

            views[i].localRotation = viewBaseRot[i];

        }

    }



    public void ResetState()

    {

        x = x0;

        v = Vector3.zero;

        heading = heading0;

        sliding = false;

        grounded = false;

        impactSpeed = 0f;

        groundNormal = Vector3.up;

        pitch = roll = pitchRate = rollRate = 0f;

        pitchAngle = rollAngle = 0f;

        bodyRotation = Quaternion.Euler(0f, heading, 0f);

        suspensionForce = Vector3.zero;

        suspensionTorque = Vector3.zero;

        airTime = 0f;

        classicThrottle = classicSteering = 0f;

        classicDrifting = false;

        brakeDrifting = false;

        prevVy = prevSpeed = 0f;

        tiltPitchTarget = tiltRollTarget = tiltPitch = tiltRoll = 0f;

        bounceY = bounceVel = cabinOffset = 0f;

        RestoreViews();

        rb.position = x;

        transform.SetPositionAndRotation(x, bodyRotation);

    }



    // E mirando el vehículo desde afuera: subirse.

    public void Interact(GameObject interactor)

    {

        Pawn pawn = interactor.GetComponent<Pawn>();

        if (pawn == null || pawn.Controller == null) return;



        driver = pawn;

        pawn.Controller.Possess(this);

        pawn.gameObject.SetActive(false);

    }



    public string GetInteractText()

    {

        return "Conducir";

    }



    // E estando adentro: bajarse por el lado izquierdo.

    public override void Interact()

    {

        if (driver == null) return;



        driver.transform.position = transform.position - transform.right * 2.5f + Vector3.up;

        driver.gameObject.SetActive(true);

        Controller.Possess(driver);

        driver = null;

        Drive(0f, 0f, 0f);

    }

}

