using UnityEngine;

// Carro con física simple basada en Rigidbody:
//  - Aceleración y desaceleración progresivas
//  - Resistencia del aire (cuadrática: a más velocidad, más frena)
//  - Derrape (el carro pierde agarre lateral en curvas fuertes o con freno de mano)
//  - Se adapta a lomas y puede quedar en el aire; avisa cuando aterriza (para el rebote)
// Requiere: Rigidbody + un Collider en el carro.
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Motor (aceleración / desaceleración)")]
    [Tooltip("Qué tan fuerte acelera (m/s²)")]
    [SerializeField] private float engineAcceleration = 8f;
    [Tooltip("Qué tan rápido sube el acelerador al presionar el botón (por segundo)")]
    [SerializeField] private float throttleRise = 2.5f;
    [Tooltip("Qué tan rápido baja al soltar el botón (por segundo)")]
    [SerializeField] private float throttleFall = 3f;
    [Tooltip("Frenada al presionar retroceder mientras avanza (m/s²)")]
    [SerializeField] private float brakeDeceleration = 14f;
    [SerializeField] private float reverseAcceleration = 4f;
    [SerializeField] private float maxReverseSpeed = 5f;
    [Tooltip("Desaceleración natural al soltar el acelerador (m/s²)")]
    [SerializeField] private float rollingResistance = 1.2f;

    [Header("Resistencia del aire")]
    [Tooltip("Velocidad máxima ≈ raíz( (aceleración - rodadura) / resistencia ). Con 8 y 0.02 ≈ 18 m/s")]
    [SerializeField] private float airDrag = 0.02f;

    [Header("Dirección")]
    [SerializeField] private float maxSteeringRate = 70f;       // grados por segundo
    [SerializeField] private float inputSmoothing = 6f;
    [Range(0f, 0.9f)]
    [SerializeField] private float highSpeedSteerReduction = 0.25f; // gira menos a alta velocidad
    [SerializeField, Range(0f, 0.3f)] private float deadZone = 0.05f;
    [SerializeField] private bool invertSteering = false;

    [Header("Agarre y derrape")]
    [Tooltip("Agarre normal: más alto = el carro se pega más a la dirección")]
    [SerializeField] private float normalGrip = 8f;
    [Tooltip("Agarre mientras derrapa: más bajo = se desliza más")]
    [SerializeField] private float driftGrip = 2.2f;
    [Tooltip("Si el deslizamiento lateral supera esto (m/s), empieza el derrape")]
    [SerializeField] private float driftStartLateralSpeed = 2f;
    [Tooltip("Cuando baja de esto (m/s), el derrape termina")]
    [SerializeField] private float driftEndLateralSpeed = 0.8f;
    [SerializeField] private float handbrakeDeceleration = 5f;
    [Tooltip("Cuánta velocidad pierde al derrapar")]
    [SerializeField] private float driftSpeedLoss = 0.5f;

    [Header("Suelo / Lomas")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float maxGroundAngle = 60f;
    [SerializeField] private float alignSpeed = 8f;   // qué tan rápido se inclina siguiendo la loma
    [Tooltip("Presiona el carro contra el suelo según la velocidad (evita despegues con baches pequeños)")]
    [SerializeField] private float downforce = 0.1f;
    [SerializeField] private float parkingSpeed = 1f;

    private Rigidbody rb;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    private float throttleInput;
    private bool handbrakeInput;
    private float currentThrottle;
    private float currentSteering;

    private bool wasGrounded;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 airVelocity;

    // ---- Datos públicos (los usan el HUD y la cabina) ----
    public bool IsGrounded { get; private set; }
    public bool IsDrifting { get; private set; }
    public float ForwardSpeed { get; private set; }
    public float LateralSpeed { get; private set; }
    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public float CurrentSpeed => ForwardSpeed;                 // compatible con tu HUD anterior
    public float SpeedKmh => Mathf.Abs(ForwardSpeed) * 3.6f;
    public float TopSpeed => Mathf.Sqrt(Mathf.Max(0.01f, engineAcceleration - rollingResistance) / Mathf.Max(0.0001f, airDrag));

    // Se dispara al aterrizar. El valor es qué tan fuerte fue el golpe (m/s)
    public event System.Action<float> OnLanded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;                 // la rotación la controlamos nosotros
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;                    // la resistencia del aire la calculamos nosotros
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Material sin fricción: así el suelo no frena al carro "gratis"
        var mat = new PhysicsMaterial("CarNoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        foreach (var col in GetComponentsInChildren<Collider>())
            if (!col.isTrigger) col.sharedMaterial = mat;
    }

    // ---------------- Entrada (cada frame) ----------------
    void Update()
    {
        GameInput.Read(out float throttle, out float steering);
        handbrakeInput = GameInput.Handbrake();
        throttleInput = throttle;

        if (invertSteering) steering = -steering;

        // Acelerador progresivo
        bool rising = Mathf.Abs(throttle) > Mathf.Abs(currentThrottle) && (throttle * currentThrottle >= 0f);
        float rate = rising ? throttleRise : throttleFall;
        currentThrottle = Mathf.MoveTowards(currentThrottle, throttle, rate * Time.deltaTime);

        // Dirección suavizada
        float targetSteering = ApplyDeadZone(steering) * maxSteeringRate;
        currentSteering = Mathf.Lerp(currentSteering, targetSteering, Time.deltaTime * inputSmoothing);
    }

    // ---------------- Física (cada paso de física) ----------------
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        CheckGround();

        Vector3 v = rb.linearVelocity;
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;
        ForwardSpeed = Vector3.Dot(v, fwd);
        LateralSpeed = Vector3.Dot(v, right);

        // 1) RESISTENCIA DEL AIRE: siempre, incluso en el aire. Crece con el cuadrado de la velocidad.
        rb.AddForce(-v * v.magnitude * airDrag, ForceMode.Acceleration);

        if (!IsGrounded)
        {
            airVelocity = v;                       // guardamos la velocidad para medir el aterrizaje
            rb.angularVelocity = Vector3.zero;
            IsDrifting = false;
            return;
        }

        float absFwd = Mathf.Abs(ForwardSpeed);
        float dirSign = ForwardSpeed >= 0f ? 1f : -1f;

        // 2) ACELERACIÓN / FRENADO / DESACELERACIÓN
        float accel = 0f;
        if (throttleInput < -0.01f && ForwardSpeed > 0.5f)
        {
            accel = throttleInput * brakeDeceleration;                    // frenando
        }
        else if (currentThrottle > 0.01f)
        {
            accel = currentThrottle * engineAcceleration;                 // acelerando
        }
        else if (currentThrottle < -0.01f)
        {
            if (ForwardSpeed > -maxReverseSpeed)
                accel = currentThrottle * reverseAcceleration;            // reversa
        }

        // Rodadura (desaceleración natural). Nunca invierte el sentido del movimiento.
        accel -= dirSign * Mathf.Min(rollingResistance, absFwd / dt);

        if (handbrakeInput)
            accel -= dirSign * Mathf.Min(handbrakeDeceleration, absFwd / dt);

        // 3) DERRAPE
        bool wantsSlide = handbrakeInput && absFwd > 3f;
        if (!IsDrifting && (wantsSlide || Mathf.Abs(LateralSpeed) > driftStartLateralSpeed))
            IsDrifting = true;
        else if (IsDrifting && !handbrakeInput && Mathf.Abs(LateralSpeed) < driftEndLateralSpeed)
            IsDrifting = false;

        float grip = IsDrifting ? driftGrip : normalGrip;
        if (handbrakeInput) grip = Mathf.Min(grip, driftGrip);

        // El agarre elimina poco a poco la velocidad lateral
        float newLateral = LateralSpeed * Mathf.Exp(-grip * dt);
        rb.AddForce(right * (newLateral - LateralSpeed), ForceMode.VelocityChange);

        // Al derrapar se pierde velocidad
        if (IsDrifting)
            accel -= dirSign * Mathf.Abs(LateralSpeed) * driftSpeedLoss;

        rb.AddForce(fwd * accel, ForceMode.Acceleration);

        // 4) FUERZAS DEL SUELO
        rb.AddForce(-groundNormal * downforce * absFwd, ForceMode.Acceleration);

        // Si está casi quieto y sin acelerar, no se desliza por la loma
        if (Mathf.Abs(currentThrottle) < 0.05f && Mathf.Abs(throttleInput) < 0.05f && absFwd < parkingSpeed)
            rb.AddForce(-Vector3.ProjectOnPlane(Physics.gravity, groundNormal), ForceMode.Acceleration);

        // 5) DIRECCIÓN + inclinación siguiendo la loma
        float steerFactor = dirSign * Mathf.Clamp01(absFwd / 2f);   // solo gira si se mueve; en reversa invierte
        float speedReduction = 1f - highSpeedSteerReduction * Mathf.Clamp01(absFwd / TopSpeed);
        float yawRate = currentSteering * steerFactor * speedReduction;

        Quaternion rot = Quaternion.AngleAxis(yawRate * dt, groundNormal) * rb.rotation;
        Vector3 flatFwd = Vector3.ProjectOnPlane(rot * Vector3.forward, groundNormal);
        if (flatFwd.sqrMagnitude > 0.0001f)
        {
            Quaternion aligned = Quaternion.LookRotation(flatFwd, groundNormal);
            rot = Quaternion.Slerp(rot, aligned, alignSpeed * dt);
        }
        rb.MoveRotation(rot);
    }

    // Lanza un rayo hacia abajo para saber si está tocando el suelo
    private void CheckGround()
    {
        wasGrounded = IsGrounded;
        IsGrounded = false;

        Collider body = null;
        foreach (var c in GetComponentsInChildren<Collider>())
            if (!c.isTrigger) { body = c; break; }
        if (body == null) return;

        Bounds b = body.bounds;
        float dist = b.extents.y + groundCheckDistance;
        int count = Physics.RaycastNonAlloc(b.center, -transform.up, hits, dist, groundLayers, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.attachedRigidbody == rb) continue;   // ignora al propio carro
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                groundNormal = hits[i].normal;
                IsGrounded = true;
            }
        }

        if (IsGrounded && Vector3.Angle(Vector3.up, groundNormal) > maxGroundAngle)
            IsGrounded = false;   // es una pared, no suelo

        if (IsGrounded && !wasGrounded)
        {
            float impact = Mathf.Max(0f, -Vector3.Dot(airVelocity, groundNormal));
            OnLanded?.Invoke(impact);
        }
    }

    // Al desactivar el script (ej. durante el tutorial) el carro se detiene
    void OnDisable()
    {
        currentThrottle = 0f;
        currentSteering = 0f;
        throttleInput = 0f;
        if (rb != null) rb.linearVelocity = Vector3.Project(rb.linearVelocity, Vector3.up);
    }

    private float ApplyDeadZone(float value)
    {
        if (Mathf.Abs(value) < deadZone) return 0f;
        float sign = Mathf.Sign(value);
        return sign * (Mathf.Abs(value) - deadZone) / (1f - deadZone);
    }
}
