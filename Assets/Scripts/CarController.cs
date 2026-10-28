using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float maxSteeringAngle = 90f; // grados por segundo
    [SerializeField] private float inputSmoothing = 5f;

    [Header("Calibration")]
    [SerializeField, Range(0f, 0.3f)] private float deadZone = 0.05f;
    [SerializeField] private bool invertSteering = false;

    private Rigidbody rb;
    private float currentSpeed;
    private float currentSteering;

    public float CurrentSpeed => currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        GameInput.Read(out float throttle, out float steering);
        if (invertSteering) steering = -steering;

        float targetSpeed = throttle * maxSpeed;
        float targetSteering = ApplyDeadZone(steering) * maxSteeringAngle;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * inputSmoothing);
        currentSteering = Mathf.Lerp(currentSteering, targetSteering, Time.deltaTime * inputSmoothing);
    }

    void FixedUpdate()
    {
        // Solo gira cuando se mueve (y al retroceder invierte el giro, como un carro real)
        float steeringFactor = Mathf.Sign(currentSpeed) * Mathf.Clamp01(Mathf.Abs(currentSpeed) / 2f);

        Quaternion turn = Quaternion.Euler(0f, currentSteering * steeringFactor * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);

        Vector3 velocity = transform.forward * currentSpeed;
        velocity.y = rb.linearVelocity.y; // conserva la gravedad
        rb.linearVelocity = velocity;
    }

    // Al desactivar el script (ej. durante el tutorial) el carro se detiene
    void OnDisable()
    {
        currentSpeed = 0f;
        currentSteering = 0f;
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private float ApplyDeadZone(float value)
    {
        if (Mathf.Abs(value) < deadZone) return 0f;
        float sign = Mathf.Sign(value);
        return sign * (Mathf.Abs(value) - deadZone) / (1f - deadZone);
    }
}
