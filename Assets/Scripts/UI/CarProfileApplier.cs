using UnityEngine;

/// <summary>
/// Convierte UN solo VehiclePawn en 4 carros distintos: cambia sus números (motor, agarre, peso...)
/// y muestra el modelo 3D que corresponde. Va en el mismo objeto que el VehiclePawn.
/// Al cargar la escena aplica el carro guardado en CarSelection (así 3_Juego usa el elegido en el tutorial).
/// Los números se pueden ajustar en el Inspector; "Reset" del componente los devuelve a los de fábrica.
/// Pensado para el modo "classicDriving" del VehiclePawn (el que viene activo).
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

        [Header("Chasis")]
        public float mass = 1200f;
        [Tooltip("Más bajo = más estable en curvas. Más alto = se balancea más.")]
        public float centerOfMassHeight = 0.5f;
        [Tooltip("Más alta = suspensión más dura.")]
        public float suspensionFrequency = 1.8f;
    }

    [Tooltip("Si se deja vacío, usa el VehiclePawn de este mismo objeto.")]
    [SerializeField] private VehiclePawn vehicle;

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
        Apply(CarSelection.Index);
    }

    /// <summary>Pone los números y el modelo del carro i.</summary>
    public void Apply(int i)
    {
        if (profiles == null || profiles.Length == 0) return;
        if (vehicle == null) vehicle = GetComponent<VehiclePawn>();
        i = Mathf.Clamp(i, 0, profiles.Length - 1);
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

        vehicle.mass = p.mass;
        vehicle.centerOfMassHeight = p.centerOfMassHeight;
        vehicle.suspensionFrequency = p.suspensionFrequency;

        for (int j = 0; j < profiles.Length; j++)
            if (profiles[j].model != null) profiles[j].model.SetActive(j == i);
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
