using UnityEngine;

/// <summary>
/// Convierte UN solo VehiclePawn en 4 carros distintos: cambia sus números (motor, agarre, peso...)
/// y muestra el modelo 3D que corresponde. Va en el mismo objeto que el VehiclePawn.
/// Al cargar la escena aplica el carro guardado en CarSelection (así 3_Juego usa el elegido en el tutorial).
/// Los números se pueden ajustar en el Inspector; "Reset" del componente los devuelve a los de fábrica.
/// Pensado para el modo "classicDriving" del VehiclePawn (el que viene activo).
///
/// DERRAPE: cada carro guarda su propio derrape en su perfil. El VehiclePawn NO se configura a mano,
/// porque este script lo reescribe al cargar la escena y cada vez que se elige un carro.
///   - Derrape básico (siempre se aplica): normalGrip, tireFriction, slideFriction, driftSpeedKmh, driftRearGrip, frontDriveShare.
///   - Derrape avanzado (llantas, transferencia de carga, contravolante...): se aplica si el perfil tiene
///     "Advanced Drift" marcado; si no, se dejan los valores que tenga el VehiclePawn.
///   - Para llevar lo que ya tienes en el VehiclePawn a un perfil: clic derecho en este componente →
///     "Guardar derrape del VehiclePawn en perfil N".
///   - En Play Mode, cambiar un valor del perfil en el Inspector se aplica al instante.
/// </summary>
[RequireComponent(typeof(VehiclePawn))]
public class CarProfileApplier : MonoBehaviour
{
    [System.Serializable]
    public class Profile
    {
        public string name = "Carro";
        [TextArea(1, 3)] public string description;

        [Header("Solo para mostrar en pantalla (1-5)")]
        [Range(1, 5)] public int speedStars = 3;
        [Range(1, 5)] public int accelerationStars = 3;
        [Range(1, 5)] public int controlStars = 3;

        [Header("Modelo 3D (hijo del carro; se activa solo si está elegido)")]
        public GameObject model;

        [Header("Motor y frenos")]
        [Tooltip("Velocidad máx. ≈ √((aceleración - rodadura) / resistencia del aire).")]
        public float engineAcceleration = 8f;
        public float airDrag = 0.02f;
        public float throttleRise = 2.5f;
        public float throttleFall = 3f;
        public float brakeDeceleration = 14f;
        public float maxReverseSpeed = 5f;

        [Header("Dirección")]
        public float maxSteeringRate = 70f;
        public float steeringSmoothing = 6f;
        [Range(0f, 0.9f)] public float highSpeedSteerReduction = 0.25f;

        [Header("Agarre y derrape")]
        public float normalGrip = 8f;
        public float tireFriction = 1.3f;
        [Range(0.2f, 1f)] public float slideFriction = 0.8f;
        [Tooltip("Velocidad (km/h) desde la que el eje trasero se va de lado al girar.")]
        public float driftSpeedKmh = 60f;
        [Tooltip("1 = no derrapa por velocidad. Más bajo = se va más de lado.")]
        [Range(0.3f, 1f)] public float driftRearGrip = 0.65f;
        [Tooltip("0 = tracción trasera (derrapa más), 0.5 = 4x4 (estable), 1 = delantera.")]
        [Range(0f, 1f)] public float frontDriveShare = 0f;

        [Header("Derrape avanzado (llantas del VehiclePawn)")]
        [Tooltip("Marcado: este carro aplica TODOS los valores de abajo al VehiclePawn. " +
                 "Desmarcado: se dejan los que tenga el VehiclePawn.")]
        public bool advancedDrift = false;
        [Tooltip("Rango (km/h) por debajo de 'Drift Speed Kmh' en el que la pérdida de agarre entra poco a poco.")]
        public float driftRampKmh = 9f;
        [Tooltip("Rigidez de curva de las llantas (1/rad). Más alto = el agarre llega con menos ángulo y el pico es más brusco.")]
        public float corneringStiffness = 20f;
        [Tooltip("Cuánto baja el agarre al cargar mucho una llanta. Más alto = pierde agarre más fácil en curva.")]
        [Range(0f, 0.4f)] public float loadSensitivity = 0.15f;
        [Tooltip("Más alta = el eje delantero pierde agarre primero (subviraje); más baja = el trasero (sobreviraje).")]
        [Range(0.2f, 0.8f)] public float rollStiffnessFront = 0.55f;
        [Tooltip("Más alto = el carro tarda más en rotar y en dejar de rotar.")]
        [Range(0.3f, 0.8f)] public float yawGyrationRatio = 0.5f;
        [Tooltip("Fracción del peso sobre el eje delantero.")]
        [Range(0.3f, 0.7f)] public float frontWeightFraction = 0.5f;
        [Tooltip("Ancho de vía (m). Más angosto = más transferencia de peso en curva.")]
        public float trackWidth = 1.6f;
        [Tooltip("Máximo uso del agarre por frenar/acelerar. Menos de 1 deja algo de agarre lateral al frenar a fondo.")]
        [Range(0.5f, 1f)] public float absMaxUsage = 0.95f;
        [Tooltip("Segundos que tarda el peso en pasar de un lado a otro.")]
        public float loadTransferTime = 0.08f;
        [Tooltip("Velocidad (m/s) por debajo de la cual se usa la conducción simple.")]
        public float dynamicsBlendStart = 2.5f;
        [Tooltip("Velocidad (m/s) por encima de la cual manda el modelo de llantas al 100 %.")]
        public float dynamicsBlendEnd = 6f;
        [Tooltip("Ángulo de deslizamiento (grados) donde empieza a recuperarse el giro para contravolantear.")]
        public float counterSteerStart = 5f;
        [Tooltip("Ángulo (grados) donde las llantas ya tienen su giro máximo disponible.")]
        public float counterSteerFull = 15f;
        [Tooltip("Cuánto de la fuerza del motor le quita agarre lateral al eje trasero (0 = nada, 1 = todo).")]
        [Range(0f, 1f)] public float classicDriveGripUse = 0.9f;

        [Header("Chasis")]
        public float mass = 1200f;
        [Tooltip("Más bajo = más estable en curvas. Más alto = se balancea más.")]
        public float centerOfMassHeight = 0.5f;
        [Tooltip("Más alta = suspensión más dura.")]
        public float suspensionFrequency = 1.8f;
    }

    [Tooltip("Si se deja vacío, usa el VehiclePawn de este mismo objeto.")]
    [SerializeField] private VehiclePawn vehicle;

    [Tooltip("Apagado (recomendado cuando cada carro tiene su propio VehiclePawn): este componente NO escribe " +
             "ningún valor al cargar la escena. Úsalo solo si quieres volver a un VehiclePawn compartido.")]
    [SerializeField] private bool applyOnStart = false;

    private int currentIndex;

    [Tooltip("Orden: 0 Carreras, 1 Común, 2 Achatado, 3 Minivan. Arrastra el modelo 3D de cada uno.")]
    public Profile[] profiles = DefaultProfiles();

    public int Count => profiles.Length;
    public Profile Get(int i) => profiles[Mathf.Clamp(i, 0, profiles.Length - 1)];

    void Reset()
    {
        profiles = DefaultProfiles();
    }

    void Awake()
    {
        if (vehicle == null) vehicle = GetComponent<VehiclePawn>();
        if (applyOnStart) Apply(CarSelection.Index);
    }

    // En Play Mode, al cambiar un valor del perfil en el Inspector se aplica al carro que se está viendo.
    void OnValidate()
    {
        if (applyOnStart && Application.isPlaying && profiles != null && profiles.Length > 0) Apply(currentIndex);
    }

    /// <summary>Pone los números y el modelo del carro i.</summary>
    public void Apply(int i)
    {
        if (profiles == null || profiles.Length == 0) return;
        if (vehicle == null) vehicle = GetComponent<VehiclePawn>();
        i = Mathf.Clamp(i, 0, profiles.Length - 1);
        currentIndex = i;
        Profile p = profiles[i];

        vehicle.classicDriving = true;
        vehicle.engineAcceleration = p.engineAcceleration;
        vehicle.classicAirDrag = p.airDrag;
        vehicle.throttleRise = p.throttleRise;
        vehicle.throttleFall = p.throttleFall;
        vehicle.brakeDeceleration = p.brakeDeceleration;
        vehicle.maxReverseSpeed = p.maxReverseSpeed;

        vehicle.maxSteeringRate = p.maxSteeringRate;
        vehicle.steeringSmoothing = p.steeringSmoothing;
        vehicle.highSpeedSteerReduction = p.highSpeedSteerReduction;

        vehicle.normalGrip = p.normalGrip;
        vehicle.tireFriction = p.tireFriction;
        vehicle.slideFriction = p.slideFriction;
        vehicle.driftSpeedKmh = p.driftSpeedKmh;
        vehicle.driftRearGrip = p.driftRearGrip;
        vehicle.frontDriveShare = p.frontDriveShare;

        if (p.advancedDrift)
        {
            vehicle.driftRampKmh = p.driftRampKmh;
            vehicle.corneringStiffness = p.corneringStiffness;
            vehicle.loadSensitivity = p.loadSensitivity;
            vehicle.rollStiffnessFront = p.rollStiffnessFront;
            vehicle.yawGyrationRatio = p.yawGyrationRatio;
            vehicle.frontWeightFraction = p.frontWeightFraction;
            vehicle.trackWidth = p.trackWidth;
            vehicle.absMaxUsage = p.absMaxUsage;
            vehicle.loadTransferTime = p.loadTransferTime;
            vehicle.dynamicsBlendStart = p.dynamicsBlendStart;
            vehicle.dynamicsBlendEnd = p.dynamicsBlendEnd;
            vehicle.counterSteerStart = p.counterSteerStart;
            vehicle.counterSteerFull = p.counterSteerFull;
            vehicle.classicDriveGripUse = p.classicDriveGripUse;
        }

        vehicle.mass = p.mass;
        vehicle.centerOfMassHeight = p.centerOfMassHeight;
        vehicle.suspensionFrequency = p.suspensionFrequency;

        for (int j = 0; j < profiles.Length; j++)
            if (profiles[j].model != null) profiles[j].model.SetActive(j == i);
    }

    // ---------- Llevar el derrape del VehiclePawn a un perfil ----------

    [ContextMenu("Guardar derrape del VehiclePawn en perfil 1")] void CaptureDrift0() => CaptureDrift(0);
    [ContextMenu("Guardar derrape del VehiclePawn en perfil 2")] void CaptureDrift1() => CaptureDrift(1);
    [ContextMenu("Guardar derrape del VehiclePawn en perfil 3")] void CaptureDrift2() => CaptureDrift(2);
    [ContextMenu("Guardar derrape del VehiclePawn en perfil 4")] void CaptureDrift3() => CaptureDrift(3);

    /// <summary>Copia el derrape que tiene ahora el VehiclePawn al perfil i y marca "Advanced Drift".</summary>
    void CaptureDrift(int i)
    {
        if (vehicle == null) vehicle = GetComponent<VehiclePawn>();
        if (vehicle == null || profiles == null || i < 0 || i >= profiles.Length)
        {
            Debug.LogWarning($"[CarProfileApplier] No hay perfil {i + 1} o falta el VehiclePawn.", this);
            return;
        }
        Profile p = profiles[i];

        p.normalGrip = vehicle.normalGrip;
        p.tireFriction = vehicle.tireFriction;
        p.slideFriction = vehicle.slideFriction;
        p.driftSpeedKmh = vehicle.driftSpeedKmh;
        p.driftRearGrip = vehicle.driftRearGrip;
        p.frontDriveShare = vehicle.frontDriveShare;

        p.advancedDrift = true;
        p.driftRampKmh = vehicle.driftRampKmh;
        p.corneringStiffness = vehicle.corneringStiffness;
        p.loadSensitivity = vehicle.loadSensitivity;
        p.rollStiffnessFront = vehicle.rollStiffnessFront;
        p.yawGyrationRatio = vehicle.yawGyrationRatio;
        p.frontWeightFraction = vehicle.frontWeightFraction;
        p.trackWidth = vehicle.trackWidth;
        p.absMaxUsage = vehicle.absMaxUsage;
        p.loadTransferTime = vehicle.loadTransferTime;
        p.dynamicsBlendStart = vehicle.dynamicsBlendStart;
        p.dynamicsBlendEnd = vehicle.dynamicsBlendEnd;
        p.counterSteerStart = vehicle.counterSteerStart;
        p.counterSteerFull = vehicle.counterSteerFull;
        p.classicDriveGripUse = vehicle.classicDriveGripUse;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[CarProfileApplier] Derrape del VehiclePawn guardado en el perfil {i + 1} ({p.name}).", this);
    }

    static Profile[] DefaultProfiles()
    {
        return new[]
        {
            // CARRERAS: ~91 km/h. Muy rápido y nervioso: gira brusco, pierde agarre atrás y derrapa fácil.
            new Profile
            {
                name = "Carreras",
                description = "El más veloz. Acelera como un rayo, pero es nervioso y derrapa: exige manos firmes.",
                speedStars = 5, accelerationStars = 5, controlStars = 2,
                engineAcceleration = 14f, airDrag = 0.02f, throttleRise = 4f, throttleFall = 4f,
                brakeDeceleration = 18f, maxReverseSpeed = 5f,
                maxSteeringRate = 90f, steeringSmoothing = 9f, highSpeedSteerReduction = 0.45f,
                normalGrip = 7f, tireFriction = 1.5f, slideFriction = 0.7f,
                driftSpeedKmh = 70f, driftRearGrip = 0.5f, frontDriveShare = 0f,
                mass = 1000f, centerOfMassHeight = 0.4f, suspensionFrequency = 2.4f
            },
            // COMÚN: ~66 km/h. Los valores de siempre.
            new Profile
            {
                name = "Común",
                description = "El equilibrado. Ni muy rápido ni muy lento, fácil de llevar.",
                speedStars = 3, accelerationStars = 3, controlStars = 3,
                engineAcceleration = 8f, airDrag = 0.02f, throttleRise = 2.5f, throttleFall = 3f,
                brakeDeceleration = 14f, maxReverseSpeed = 5f,
                maxSteeringRate = 70f, steeringSmoothing = 6f, highSpeedSteerReduction = 0.25f,
                normalGrip = 8f, tireFriction = 1.3f, slideFriction = 0.8f,
                driftSpeedKmh = 60f, driftRearGrip = 0.65f, frontDriveShare = 0f,
                mass = 1200f, centerOfMassHeight = 0.5f, suspensionFrequency = 1.8f
            },
            // ACHATADO: ~75 km/h. Centro de masa bajo y 4x4: pega al suelo y casi no derrapa.
            new Profile
            {
                name = "Achatado",
                description = "Bajo y pegado al suelo. Bastante rápido y muy estable en las curvas.",
                speedStars = 4, accelerationStars = 4, controlStars = 5,
                engineAcceleration = 10f, airDrag = 0.02f, throttleRise = 3f, throttleFall = 3f,
                brakeDeceleration = 16f, maxReverseSpeed = 5f,
                maxSteeringRate = 75f, steeringSmoothing = 7f, highSpeedSteerReduction = 0.15f,
                normalGrip = 12f, tireFriction = 1.6f, slideFriction = 0.9f,
                driftSpeedKmh = 75f, driftRearGrip = 0.9f, frontDriveShare = 0.5f,
                mass = 1100f, centerOfMassHeight = 0.3f, suspensionFrequency = 2.2f
            },
            // MINIVAN: ~50 km/h. Pesada y lenta, pero nunca derrapa: la más fácil de manejar.
            new Profile
            {
                name = "Minivan",
                description = "Grande y pesada. Lenta para arrancar y girar, pero muy fácil y segura de manejar.",
                speedStars = 1, accelerationStars = 2, controlStars = 4,
                engineAcceleration = 5.5f, airDrag = 0.022f, throttleRise = 1.5f, throttleFall = 2f,
                brakeDeceleration = 10f, maxReverseSpeed = 4f,
                maxSteeringRate = 55f, steeringSmoothing = 4f, highSpeedSteerReduction = 0.1f,
                normalGrip = 7f, tireFriction = 1.1f, slideFriction = 0.9f,
                driftSpeedKmh = 90f, driftRearGrip = 1f, frontDriveShare = 1f,
                mass = 1800f, centerOfMassHeight = 0.75f, suspensionFrequency = 1.4f
            },
        };
    }
}
