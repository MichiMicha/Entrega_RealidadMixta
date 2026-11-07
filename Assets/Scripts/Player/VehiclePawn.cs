using UnityEngine;
using UnityEngine.XR;

public class VehiclePawn : Pawn, ISimulable, IInteractable
{
    [Header("Parámetros")]
    [Tooltip("Masa m (kg).")]
    public float mass = 1200f;

    [Header("Motor y frenos (aceleraciones, m/s²)")]
    [Tooltip("Qué tan fuerte acelera (m/s²).")]
    public float engineAcceleration = 8f;

    [Tooltip("Qué tan rápido sube el acelerador al presionar (por segundo). Da la aceleración progresiva.")]
    public float throttleRise = 2.5f;

    [Tooltip("Qué tan rápido baja al soltar (por segundo). Da la desaceleración progresiva.")]
    public float throttleFall = 3f;

    [Tooltip("Frenada al presionar el freno mientras avanza (m/s²). Si supera la adherencia, las llantas patinan.")]
    public float brakeDeceleration = 14f;

    [Tooltip("Aceleración en reversa (m/s²). Detenido, el freno da reversa.")]
    public float reverseAcceleration = 4f;

    public float maxReverseSpeed = 5f;

    [Tooltip("Desaceleración natural al soltar el acelerador (m/s²).")]
    public float rollingDeceleration = 1.2f;

    [Header("Resistencia del aire")]
    [Tooltip("a = -k·|v|·v. Velocidad máxima ≈ √((aceleración - rodadura) / k) ≈ 18 m/s (66 km/h).")]
    public float airDrag = 0.02f;

    [Header("Dirección")]
    [Tooltip("Giro máximo (grados por segundo).")]
    public float maxSteeringRate = 70f;

    public float steeringSmoothing = 6f;

    [Tooltip("A alta velocidad gira menos.")]
    [Range(0f, 0.9f)] public float highSpeedSteerReduction = 0.25f;

    [Range(0f, 0.3f)] public float steerDeadZone = 0.05f;

    [Tooltip("Mientras las llantas patinan, las delanteras giran el carro solo este porcentaje (no agarran).")]
    [Range(0.2f, 1f)] public float driftSteerFactor = 0.6f;

    [Header("Llantas y derrape")]
    [Tooltip("Coeficiente de fricción ESTÁTICA μs. Adherencia máxima = μs·N (N = peso sobre las llantas). " +
             "Más alto = agarra más y cuesta más derrapar.")]
    public float tireFriction = 1.2f;

    [Tooltip("Fricción CINÉTICA μk como fracción de μs (siempre menor). Al perder el agarre solo queda μk·N, " +
             "por eso el derrape no se recupera de inmediato. Más bajo = se desliza más.")]
    [Range(0.3f, 1f)] public float kineticFrictionRatio = 0.7f;

    [Tooltip("Ángulo de deslizamiento (grados) con el que la llanta llega al límite de adherencia. " +
             "Menor = agarre más brusco; mayor = más progresivo.")]
    public float slipAngleAtLimit = 8f;

    [Tooltip("Velocidad mínima (m/s) usada para calcular el ángulo de deslizamiento (evita dividir por ~0 yendo muy lento).")]
    public float minSlipSpeed = 2f;

    [Tooltip("Velocidad que se pierde al patinar (m/s² por cada m/s de velocidad transversal).")]
    public float driftSpeedLoss = 0.5f;

    [Tooltip("Presiona el carro contra el suelo según la velocidad (m/s² por m/s). También sube el agarre.")]
    public float downforce = 0.1f;

    [Header("Freno de mano (Espacio) - opcional")]
    [Tooltip("Apagado: el derrape sale solo de la física (curva rápida, frenar en la curva), sin tecla. " +
             "Encendido: Espacio activa un freno de mano que suelta las llantas traseras.")]
    public bool handbrakeEnabled = false;

    [Tooltip("Con el freno de mano la adherencia baja a este porcentaje (llantas traseras bloqueadas).")]
    [Range(0.05f, 1f)] public float handbrakeGrip = 0.3f;

    [Tooltip("Frenada extra del freno de mano (m/s²).")]
    public float handbrakeDeceleration = 5f;

    [Tooltip("Cada vez que se presiona Espacio, el freno de mano dura esto (s).")]
    public float handbrakeDuration = 0.8f;

    [Header("Rebote de la cabina (bajar una loma a gran velocidad)")]
    [Tooltip("Frecuencia propia (Hz) del resorte de la cabina. Más alto = vaivén más rápido.")]
    public float cabinFrequency = 2.2f;

    [Tooltip("Amortiguación ζ. Bajo = rebota varias veces; alto = se asienta rápido.")]
    [Range(0.05f, 1f)] public float cabinDamping = 0.25f;

    [Tooltip("Cuánto de la aceleración vertical del carro le llega a la cabina. 1 = como un resorte real. " +
             "Súbelo para exagerar el efecto, bájalo para suavizarlo.")]
    public float cabinGain = 1f;

    [Tooltip("Aceleraciones verticales menores a esto (m/s²) se ignoran (ruido y baches pequeños).")]
    public float cabinDeadAcceleration = 1f;

    [Tooltip("Máximo desplazamiento vertical de la cabina (m).")]
    public float cabinMaxOffset = 0.2f;

    [Tooltip("La cabina cabecea con el vaivén (grados por metro de desplazamiento). 0 = sin cabeceo.")]
    public float cabinPitchPerMeter = 20f;

    [Header("Suspensión")]
    [Tooltip("Longitud natural del resorte (m): hasta qué distancia 'alcanzan' las llantas hacia el suelo.")]
    public float suspensionLength = 0.5f;

    [Tooltip("Frecuencia propia f (Hz). Rigidez de cada resorte: k = (m/4)·(2π·f)². Más alta = más dura.")]
    public float suspensionFrequency = 1.8f;

    [Tooltip("Amortiguamiento ζ al comprimirse. ~0.9 = el chasis se asienta sin rebotar (el vaivén que se ve al caer " +
             "lo hace la cabina, ver 'Rebote de la cabina'). Bajo = el carro entero rebota como pelota.")]
    [Range(0.05f, 1f)] public float suspensionDamping = 0.9f;

    [Tooltip("Multiplica el amortiguamiento cuando el resorte se extiende. >1 = el carro vuelve a subir despacio, sin salir disparado.")]
    [Range(1f, 3f)] public float reboundDamping = 1.2f;

    [Tooltip("Tope del resorte (m): cuánto se puede hundir de más antes de que el tope lo frene en seco.")]
    public float bumpStop = 0.08f;

    [Tooltip("Pendiente máxima (grados) que cuenta como suelo. Más empinado se trata como pared.")]
    public float maxGroundAngle = 50f;

    [Tooltip("Qué tan rápido el carro se alinea con la pendiente (1/s). En el aire se alinea 7 veces más lento.")]
    public float bodyAlignSpeed = 8f;

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
    public bool sliding;       // true si las llantas perdieron la adherencia (derrape)
    public float lateralSpeed; // velocidad transversal v⊥ (m/s): hacia los lados del carro
    public float slipAngle;    // ángulo de deslizamiento α (grados) = atan(v⊥ / |v∥|)
    public bool handbrakeOn;
    public bool grounded;      // true si al menos una llanta toca el suelo (false = en el aire)
    public float impactSpeed;  // velocidad (m/s) del último choque contra un obstáculo en este paso (0 = sin choque)

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
    private float handbrakeTimer;

    // Suelo y orientación del cuerpo.
    private Vector3[] wheelOffsets;               // posición de cada llanta respecto al centro del carro (m)
    private Vector3 groundNormal = Vector3.up;    // normal promedio del suelo bajo las llantas
    private Vector3 bodyNormal = Vector3.up;      // normal suavizada con la que se inclina el carro
    private Quaternion bodyRotation = Quaternion.identity;
    private Vector3 suspensionForce;              // suma de los 4 resortes en este paso
    private readonly RaycastHit[] rayHits = new RaycastHit[16];
    private readonly Collider[] overlaps = new Collider[16];

    // Conducción.
    private float currentThrottle;         // acelerador suavizado (-1..1)
    private float currentSteering;         // giro suavizado (grados/s)

    // Rebote de la cabina.
    private float cabinY, cabinVel;        // desplazamiento vertical de la cabina (m) y su velocidad
    private float cabinShown;              // el desplazamiento suavizado que se ve en pantalla
    private float prevVy;                  // v_y del paso anterior (para la aceleración vertical)
    private Vector3[] viewBasePos;
    private Quaternion[] viewBaseRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        box = GetComponent<BoxCollider>();
        x0 = transform.position;
        heading0 = transform.eulerAngles.y;

        // Pose original de cada vista de cámara: el rebote de la cabina se suma encima de esta.
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
        wheelOffsets = new[]
        {
            new Vector3(-halfWidth, 0f, centerZ + halfLength), // delantera izquierda
            new Vector3( halfWidth, 0f, centerZ + halfLength), // delantera derecha
            new Vector3(-halfWidth, 0f, centerZ - halfLength), // trasera izquierda
            new Vector3( halfWidth, 0f, centerZ - halfLength), // trasera derecha
        };
    }

    public override void Jump()
    {
        if (handbrakeEnabled) handbrakeTimer = handbrakeDuration;
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
        handbrakeTimer = Mathf.Max(0f, handbrakeTimer - h);
        handbrakeOn = handbrakeTimer > 0f;

        // 0. Suelo: un rayo por llanta para saber si toca, qué tan comprimido está el resorte y la normal.
        ProbeGround(h);
        UpdateInputs(h);

        // 1. Fuerzas.
        Vector3 F = ComputeForces(h);

        // 2. Integrar.
        Vector3 a = F / mass;
        v = v + a * h;
        x = x + v * h;

        // 2b. Fricción estática: quieto y sin pedales en una pendiente suave, se queda parqueado.
        ParkOnSlopes();

        // 3. Girar. En el aire las llantas no agarran: no gira.
        if (grounded) heading += YawRate() * h; // grados/s

        // 4. Choques.
        CollideWithScene(h);

        // 5. Cabina: la excita la aceleración vertical del carro (ya con choques resueltos).
        StepCabin(h);

        // 6. Inclinar el cuerpo según la pendiente y mover el objeto de Unity.
        AlignBody(h);
        rb.MovePosition(x);
        rb.MoveRotation(bodyRotation);
    }

    void ProbeGround(float h)
    {
        float g = Physics.gravity.magnitude;
        float k = (mass / 4f) * Mathf.Pow(2f * Mathf.PI * suspensionFrequency, 2f);
        float c = 2f * suspensionDamping * Mathf.Sqrt(k * mass / 4f);
        float sag = mass * g / (4f * k);
        float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        Vector3 up = bodyRotation * Vector3.up;
        float compressionRate = -Vector3.Dot(v, up);

        suspensionForce = Vector3.zero;
        Vector3 normalSum = Vector3.zero;
        int contacts = 0;

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            Vector3 origin = x + bodyRotation * wheelOffsets[i] + up * (suspensionLength - sag);
            if (!RaycastGround(origin, -up, suspensionLength, minNormalY, out RaycastHit hit)) continue;

            float compression = suspensionLength - hit.distance;
            // Amortiguador asimétrico: más fuerte al extenderse (compressionRate < 0) que al comprimirse.
            float damper = compressionRate < 0f ? c * reboundDamping : c;
            float f = k * compression + damper * compressionRate;

            // Tope: si se hunde más de lo normal, el tope lo frena en seco (evita atravesar el suelo).
            float overTravel = compression - (sag + bumpStop);
            if (overTravel > 0f) f += 6f * k * overTravel;

            if (f <= 0f) continue; // el resorte no jala
            suspensionForce += f * hit.normal;
            normalSum += hit.normal;
            contacts++;
        }

        grounded = contacts > 0;
        groundNormal = grounded ? normalSum.normalized : Vector3.up;
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

    public float TopSpeed => Mathf.Sqrt(Mathf.Max(0.01f, engineAcceleration - rollingDeceleration) / Mathf.Max(0.0001f, airDrag));

    static float ApplyDeadZone(float value, float deadZone)
    {
        if (Mathf.Abs(value) < deadZone) return 0f;
        return Mathf.Sign(value) * (Mathf.Abs(value) - deadZone) / (1f - deadZone);
    }

    void UpdateInputs(float h)
    {
        float input = Mathf.Clamp(throttle - brake, -1f, 1f);
        bool rising = Mathf.Abs(input) > Mathf.Abs(currentThrottle) && input * currentThrottle >= 0f;
        currentThrottle = Mathf.MoveTowards(currentThrottle, input, (rising ? throttleRise : throttleFall) * h);

        float targetSteering = ApplyDeadZone(steer, steerDeadZone) * maxSteeringRate;
        currentSteering = Mathf.Lerp(currentSteering, targetSteering, 1f - Mathf.Exp(-steeringSmoothing * h));
    }

    float YawRate()
    {
        float absLong = Mathf.Abs(Speed);
        float dirSign = Speed >= 0f ? 1f : -1f;
        float steerFactor = dirSign * Mathf.Clamp01(absLong / 2f);
        float speedReduction = 1f - highSpeedSteerReduction * Mathf.Clamp01(absLong / TopSpeed);
        float slideReduction = sliding ? driftSteerFactor : 1f;
        return currentSteering * steerFactor * speedReduction * slideReduction;
    }

    Vector3 ComputeForces(float h)
    {
        float g = Physics.gravity.magnitude;
        float speed = v.magnitude;

        // Peso + los 4 resortes de la suspensión (sostienen el carro y siguen las lomas).
        Vector3 F = mass * Physics.gravity + suspensionForce;

        // RESISTENCIA DEL AIRE: siempre, incluso en el aire. a = -k·|v|·v
        F -= mass * airDrag * speed * v;

        // Sin las llantas en el suelo no hay motor, ni freno, ni agarre: solo peso y aire.
        if (!grounded)
        {
            sliding = false;
            lateralSpeed = 0f;
            slipAngle = 0f;
            return F;
        }

        Vector3 fwd = Forward;
        Vector3 right = Right;

        // ---- Descomposición de la velocidad respecto al carro ----
        float vLong = Vector3.Dot(v, fwd);   // longitudinal: hacia adelante (+) o atrás (-) de la carrocería
        float vLat = Vector3.Dot(v, right);  // transversal: hacia los lados de la carrocería
        float absLong = Mathf.Abs(vLong);
        float dirSign = vLong >= 0f ? 1f : -1f;
        float input = Mathf.Clamp(throttle - brake, -1f, 1f);
        lateralSpeed = vLat;

        // ---- Longitudinal: lo que piden el motor y el freno (fuerza que las llantas deberían dar) ----
        float demand = 0f; // m/s²
        if (input < -0.01f && vLong > 0.5f) demand = input * brakeDeceleration;            // frenando
        else if (currentThrottle > 0.01f) demand = currentThrottle * engineAcceleration;   // acelerando
        else if (currentThrottle < -0.01f && vLong > -maxReverseSpeed) demand = currentThrottle * reverseAcceleration; // reversa
        float Fx = mass * demand;

        // ---- Adherencia: N (peso sobre las llantas), μs (estática) y μk (cinética) ----
        float N = mass * (g * Mathf.Max(0.1f, groundNormal.y) + downforce * absLong);
        float gripFactor = handbrakeOn ? handbrakeGrip : 1f;   // llantas traseras bloqueadas: agarran menos
        float limitStatic = tireFriction * gripFactor * N;     // μs·N
        float limitKinetic = limitStatic * kineticFrictionRatio; // μk·N

        // Círculo de fricción: |F_total| ≤ μ·N. Lo que gasta el motor o el freno no queda para la lateral.
        float FxStatic = Mathf.Clamp(Fx, -limitStatic, limitStatic);
        float FxKinetic = Mathf.Clamp(Fx, -limitKinetic, limitKinetic);
        float latCapStatic = Mathf.Sqrt(Mathf.Max(0f, limitStatic * limitStatic - FxStatic * FxStatic));
        float latCapKinetic = Mathf.Sqrt(Mathf.Max(0f, limitKinetic * limitKinetic - FxKinetic * FxKinetic));

        // ---- Transversal: ángulo de deslizamiento y fuerza lateral de la llanta ----
        //   α = atan(v⊥ / |v∥|)     la llanta pide Fy = -Cα·α para alinear v con la carrocería
        float slip = Mathf.Atan2(vLat, Mathf.Max(absLong, minSlipSpeed));
        slipAngle = slip * Mathf.Rad2Deg;
        float corneringStiffness = tireFriction * gripFactor * N / (slipAngleAtLimit * Mathf.Deg2Rad); // Cα (N/rad)
        float FyDemand = -corneringStiffness * slip;

        // Pierde la adherencia al pasar el límite ESTÁTICO. La recupera cuando lo que pide la llanta
        // baja del límite CINÉTICO (histéresis: μk < μs, por eso el derrape "se sostiene").
        if (!sliding && Mathf.Abs(FyDemand) > latCapStatic) sliding = true;
        else if (sliding && Mathf.Abs(FyDemand) < latCapKinetic) sliding = false;

        // Agarrando: la fuerza que pide la llanta. Patinando: solo fricción cinética, oponiéndose al deslizamiento.
        float Fy = sliding ? -Mathf.Sign(vLat) * latCapKinetic : FyDemand;
        float FxApplied = sliding ? FxKinetic : FxStatic;

        // La fuerza lateral nunca puede invertir v⊥ en un solo paso (evita temblor).
        float maxFy = mass * Mathf.Abs(vLat) / h;
        Fy = Mathf.Clamp(Fy, -maxFy, maxFy);

        // ---- Pérdidas longitudinales: rodadura, freno de mano y roce al patinar ----
        float decel = rollingDeceleration;
        if (handbrakeOn) decel += handbrakeDeceleration;
        if (sliding) decel += Mathf.Abs(vLat) * driftSpeedLoss;
        decel = Mathf.Min(decel, absLong / h); // nunca invierte el sentido del movimiento

        F += fwd * (FxApplied - dirSign * mass * decel);
        F += right * Fy;

        // Presiona el carro contra el suelo (más agarre a más velocidad).
        F -= mass * downforce * absLong * groundNormal;

        return F;
    }

    void StepCabin(float h)
    {
        float ay = (v.y - prevVy) / h;
        prevVy = v.y;

        // Ignora el ruido y los baches pequeños; limita los golpes enormes (choques).
        float magnitude = Mathf.Max(0f, Mathf.Abs(ay) - cabinDeadAcceleration);
        ay = Mathf.Sign(ay) * Mathf.Min(magnitude, 200f);

        float w = 2f * Mathf.PI * cabinFrequency;
        cabinVel += (-w * w * cabinY - 2f * cabinDamping * w * cabinVel - cabinGain * ay) * h;
        cabinY += cabinVel * h;

        // Tope de recorrido.
        if (cabinY > cabinMaxOffset) { cabinY = cabinMaxOffset; cabinVel = Mathf.Min(cabinVel, 0f); }
        else if (cabinY < -cabinMaxOffset) { cabinY = -cabinMaxOffset; cabinVel = Mathf.Max(cabinVel, 0f); }
    }

    void UpdateCabinView(float dt)
    {
        if (views == null || viewBasePos == null) return;

        // La simulación va a 50 Hz; aquí se suaviza a los cuadros reales para que no se vea escalonado.
        cabinShown = Mathf.Lerp(cabinShown, cabinY, 1f - Mathf.Exp(-40f * dt));
        float pitch = -cabinShown * cabinPitchPerMeter; // se hunde = la nariz cabecea hacia abajo

        for (int i = 0; i < views.Length && i < viewBasePos.Length; i++)
        {
            if (views[i] == null || views[i] == orbitView) continue;
            // El desplazamiento está en metros del mundo; se pasa a unidades locales (el carro tiene escala).
            float scaleY = views[i].parent != null ? views[i].parent.lossyScale.y : 1f;
            views[i].localPosition = viewBasePos[i] + new Vector3(0f, cabinShown / Mathf.Max(0.0001f, scaleY), 0f);
            views[i].localRotation = viewBaseRot[i] * Quaternion.Euler(pitch, 0f, 0f);
        }
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


    void AlignBody(float h)
    {
        Vector3 targetNormal = grounded ? groundNormal : Vector3.up;
        float rate = grounded ? bodyAlignSpeed : bodyAlignSpeed / 7f;
        bodyNormal = Vector3.Slerp(bodyNormal, targetNormal, 1f - Mathf.Exp(-rate * h)).normalized;

        Vector3 yawForward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
        Vector3 forward = Vector3.ProjectOnPlane(yawForward, bodyNormal);
        bodyRotation = Quaternion.LookRotation(forward, bodyNormal);
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
        handbrakeTimer = 0f;
        sliding = false;
        grounded = false;
        impactSpeed = 0f;
        groundNormal = Vector3.up;
        bodyNormal = Vector3.up;
        bodyRotation = Quaternion.Euler(0f, heading, 0f);
        suspensionForce = Vector3.zero;
        currentThrottle = currentSteering = 0f;
        lateralSpeed = slipAngle = 0f;
        prevVy = 0f;
        cabinY = cabinVel = cabinShown = 0f;
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
