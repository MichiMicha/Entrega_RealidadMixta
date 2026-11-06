using UnityEngine;
using TMPro;

// Muestra los textos del tutorial uno por uno. Cada paso avanza SOLO cuando el jugador
// hace lo que el texto pide: mirar, caminar, subirse al carro, girar, acelerar, frenar...
// Lee los valores del FirstPersonPawn (a pie) y del VehiclePawn (en el carro), así que
// funciona igual con teclado, control o volante de Arduino.
public class TutorialSteps : MonoBehaviour
{
    public enum Condition
    {
        Wait,        // solo esperar "value" segundos
        Look,        // girar la vista "value" grados (a pie)
        Walk,        // caminar "value" metros (a pie)
        EnterCar,    // subirse al carro (E mirándolo)
        Steer,       // girar el timón (más de la mitad) durante "value" segundos
        Throttle,    // acelerar (más de la mitad) durante "value" segundos
        Brake,       // frenar (más de la mitad) durante "value" segundos
        ChangeView   // cambiar la vista de la cámara del carro
    }

    [System.Serializable]
    public class Step
    {
        [TextArea(2, 4)] public string text;
        public Condition condition = Condition.Wait;
        [Tooltip("Wait/Steer/Throttle/Brake: segundos. Walk: metros. Look: grados. Los demás lo ignoran.")]
        public float value = 3f;
    }

    [Tooltip("Si se deja vacío, lo busca solo.")]
    [SerializeField] private FirstPersonPawn player;
    [Tooltip("Si se deja vacío, lo busca solo.")]
    [SerializeField] private VehiclePawn vehicle;
    [SerializeField] private TMP_Text stepText;

    [SerializeField]
    private Step[] steps =
    {
        new Step { text = "¡Bienvenido! Unas frutas se escaparon y debes atraparlas con el carro.", condition = Condition.Wait, value = 4f },
        new Step { text = "Mueve el mouse para mirar a tu alrededor.", condition = Condition.Look, value = 45f },
        new Step { text = "Camina con W A S D.", condition = Condition.Walk, value = 2f },
        new Step { text = "Acércate al carro, míralo y presiona E para subirte.", condition = Condition.EnterCar },
        new Step { text = "Gira el timón para dirigir el carro a los lados.\n(Teclado: A / D)", condition = Condition.Steer, value = 0.5f },
        new Step { text = "Usa el acelerador para avanzar.\n(Teclado: W)", condition = Condition.Throttle, value = 1f },
        new Step { text = "Usa el freno para detenerte. Si ya estás detenido, el freno te da reversa.\n(Teclado: S)", condition = Condition.Brake, value = 1f },
        new Step { text = "Cambia la vista de la cámara con el botón CICLO.\n(Teclado: V)", condition = Condition.ChangeView }
    };

    [TextArea(2, 4)]
    [SerializeField] private string finalMessage = "¡Ahora inténtalo! Atrapa la fruta que está en el escenario.";

    private int index;
    private float progress;
    private int viewAtStepStart;
    private Vector3 playerStartPos;
    private float playerStartYaw;
    private bool finished;

    void Start()
    {
        FindReferences();
        if (steps.Length == 0) { Finish(); return; }
        ShowStep();
    }

    void FindReferences()
    {
        if (player == null) player = FindAnyObjectByType<FirstPersonPawn>();
        if (vehicle == null) vehicle = FindAnyObjectByType<VehiclePawn>();
    }

    void Update()
    {
        if (finished) return;

        // Si ya atrapó la fruta, no pisar el mensaje de victoria del LevelManager.
        if (LevelManager.Instance != null && LevelManager.Instance.IsComplete) { finished = true; return; }

        FindReferences();

        Step step = steps[index];
        bool done = false;

        switch (step.condition)
        {
            case Condition.Wait:
                progress += Time.deltaTime;
                done = progress >= step.value;
                break;

            case Condition.Look:
                // Sin jugador a pie (ej: la escena empieza dentro del carro) el paso se salta solo.
                done = player == null ||
                       Mathf.Abs(Mathf.DeltaAngle(playerStartYaw, player.transform.eulerAngles.y)) >= step.value;
                break;

            case Condition.Walk:
                if (player == null) { done = true; break; }
                Vector3 moved = player.transform.position - playerStartPos;
                moved.y = 0f;
                done = moved.magnitude >= step.value;
                break;

            case Condition.EnterCar:
                // El Pawn.Controller es null mientras nadie maneja el carro.
                done = vehicle == null || vehicle.Controller != null;
                break;

            case Condition.Steer:
                done = vehicle != null && Hold(Mathf.Abs(vehicle.steer) > 0.5f, step.value);
                break;

            case Condition.Throttle:
                done = vehicle != null && Hold(vehicle.throttle > 0.5f, step.value);
                break;

            case Condition.Brake:
                done = vehicle != null && Hold(vehicle.brake > 0.5f, step.value);
                break;

            case Condition.ChangeView:
                done = vehicle != null && vehicle.CurrentView != viewAtStepStart;
                break;
        }

        if (!done) return;

        index++;
        if (index >= steps.Length) Finish();
        else ShowStep();
    }

    // Suma tiempo mientras la condición se cumple.
    private bool Hold(bool active, float seconds)
    {
        if (active) progress += Time.deltaTime;
        return progress >= seconds;
    }

    private void ShowStep()
    {
        progress = 0f;
        if (vehicle != null) viewAtStepStart = vehicle.CurrentView;
        if (player != null)
        {
            playerStartPos = player.transform.position;
            playerStartYaw = player.transform.eulerAngles.y;
        }
        if (stepText != null) stepText.text = steps[index].text;
    }

    private void Finish()
    {
        finished = true;
        if (stepText != null) stepText.text = finalMessage;
    }
}
