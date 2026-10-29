using UnityEngine;

// Mueve la cámara en primera persona para dar sensación de:
//  - Rebote/suspensión (al aterrizar o al pasar una loma)
//  - Cabeceo al acelerar/frenar
//  - Inclinación en las curvas y derrapes
// Va en un objeto vacío "CabinPivot" (hijo del carro) que es el PADRE de la cámara.
public class CabinMotion : MonoBehaviour
{
    [SerializeField] private CarController car;

    [Header("Rebote vertical (suspensión)")]
    [Tooltip("Rigidez del resorte: más alto = rebota más rápido")]
    [SerializeField] private float springStiffness = 90f;
    [Tooltip("Amortiguación: más alto = el rebote se apaga antes")]
    [SerializeField] private float springDamping = 6f;
    [Tooltip("Cuánto afecta un golpe vertical (1 = normal)")]
    [SerializeField] private float bounceStrength = 0.5f;
    [SerializeField] private float maxOffset = 0.25f;     // metros

    [Header("Inclinación")]
    [SerializeField] private float pitchFromAccel = 0.3f;   // grados por m/s²
    [SerializeField] private float rollFromLateral = 1.2f;  // grados por m/s lateral
    [SerializeField] private float maxPitch = 6f;
    [SerializeField] private float maxRoll = 8f;
    [SerializeField] private float tiltSmoothing = 8f;

    private Vector3 basePos;
    private Quaternion baseRot;

    private float offset, offsetVel, displayedOffset;
    private float prevVy, prevFwdSpeed;
    private float pitchTarget, rollTarget, pitch, roll;

    void Start()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        if (car != null) prevVy = car.Velocity.y;
    }

    void FixedUpdate()
    {
        if (car == null) return;
        float dt = Time.fixedDeltaTime;

        // Cambio de velocidad vertical = golpe (aterrizar) o "vacío" (pasar una cresta)
        float vy = car.Velocity.y;
        float dvy = vy - prevVy;
        prevVy = vy;

        if (Mathf.Abs(dvy) > 0.05f)
            offsetVel -= Mathf.Clamp(dvy, -10f, 10f) * bounceStrength;

        // Resorte con amortiguación
        float force = -springStiffness * offset - springDamping * offsetVel;
        offsetVel += force * dt;
        offset += offsetVel * dt;
        offset = Mathf.Clamp(offset, -maxOffset, maxOffset);

        // Aceleración hacia adelante (para el cabeceo)
        float accel = (car.ForwardSpeed - prevFwdSpeed) / dt;
        prevFwdSpeed = car.ForwardSpeed;

        pitchTarget = Mathf.Clamp(-accel * pitchFromAccel, -maxPitch, maxPitch);
        rollTarget = Mathf.Clamp(car.LateralSpeed * rollFromLateral, -maxRoll, maxRoll);
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        displayedOffset = Mathf.Lerp(displayedOffset, offset, 25f * dt);
        pitch = Mathf.Lerp(pitch, pitchTarget, tiltSmoothing * dt);
        roll = Mathf.Lerp(roll, rollTarget, tiltSmoothing * dt);

        transform.localPosition = basePos + Vector3.up * displayedOffset;
        transform.localRotation = baseRot * Quaternion.Euler(pitch, 0f, roll);
    }
}
