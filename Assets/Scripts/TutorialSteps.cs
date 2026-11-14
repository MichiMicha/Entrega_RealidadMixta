using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Tutorial interactivo: el jugador PRUEBA cada control en vez de memorizarlo.
///   1. Se muestra una instrucción y el jugador es libre de moverse.
///   2. Cuando hace lo que se pide (caminar, acelerar, girar...), el juego se CONGELA un momento
///      con una felicitación, el texto cambia a la siguiente instrucción y el juego continúa.
/// Lee los valores del FirstPersonPawn (a pie) y del VehiclePawn (en el carro), así que funciona
/// igual con teclado, control o volante de Arduino. No hay que presionar Enter para avanzar.
/// </summary>
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

        [Tooltip("Wait: segundos que se muestra. Steer/Throttle/Brake: segundos que debe mantener la acción. " +
                 "Walk: metros. Look: grados. Los demás lo ignoran.")]
        public float value = 3f;

        [Tooltip("Steer/Throttle/Brake: el carro debe ir al menos a esta velocidad (m/s) para que cuente. " +
                 "El timón solo gira el carro si se mueve, así que en Steer conviene 2.")]
        public float minSpeed = 0f;

        [Tooltip("Al lograrlo, el juego se congela un momento con una felicitación y pasa al siguiente texto.")]
        public bool freezeAfter = true;
    }

    [Tooltip("Si se deja vacío, lo busca solo.")]
    [SerializeField] private FirstPersonPawn player;
    [Tooltip("Si se deja vacío, lo busca solo.")]
    [SerializeField] private VehiclePawn vehicle;
    [SerializeField] private TMP_Text stepText;

    [SerializeField]
    private Step[] steps =
    {
        new Step { text = "¡Bienvenido! Unas frutas se escaparon y debes atraparlas con el carro.\nPrimero vas a practicar cada control.", condition = Condition.Wait, value = 4f, freezeAfter = false },
        new Step { text = "Mueve el mouse para mirar a tu alrededor.", condition = Condition.Look, value = 45f },
        new Step { text = "Camina con W A S D.", condition = Condition.Walk, value = 2f },
        new Step { text = "Acércate al carro, míralo y presiona E para subirte.", condition = Condition.EnterCar },
        new Step { text = "Acelera para avanzar.\n(Teclado: W  ·  Volante: palanca hacia adelante)", condition = Condition.Throttle, value = 1f },
        new Step { text = "Con el carro en movimiento, gira para dirigirlo.\n(Teclado: A / D  ·  Volante: gira el timón)", condition = Condition.Steer, value = 0.7f, minSpeed = 2f },
        new Step { text = "Frena para detenerte. Si ya estás detenido, el freno te da reversa.\n(Teclado: S  ·  Volante: palanca hacia atrás)", condition = Condition.Brake, value = 0.8f },
        new Step { text = "Cambia la vista de la cámara.\n(Teclado: V  ·  Volante: botón CICLO)", condition = Condition.ChangeView }
    };

    [TextArea(2, 4)]
    [SerializeField] private string finalMessage = "¡Listo! Ahora atrapa la fruta que está en el escenario.";

    [Header("Pausa entre pasos")]
    [SerializeField] private string praise = "¡Muy bien!";
    [SerializeField] private string praiseColor = "#7CFC00";

    [Tooltip("Segundos que el juego queda congelado mostrando la felicitación.")]
    [SerializeField] private float praiseSeconds = 0.8f;

    [Tooltip("Segundos que sigue congelado mostrando la siguiente instrucción, para leerla antes de continuar.")]
    [SerializeField] private float readSeconds = 1f;

    private int index;
    private float progress;
    private int viewAtStepStart;
    private Vector3 playerStartPos;
    private float playerStartYaw;
    private bool finished;
    private bool busy;      // mientras dura la pausa entre pasos
    private bool frozen;

    void Start()
    {
        FindReferences();
        if (steps.Length == 0) { Finish(); return; }
        ShowStep();
    }

    void OnDisable()
    {
        // Si se cambia de escena en plena pausa, que la siguiente no arranque congelada.
        if (frozen) Time.timeScale = 1f;
    }

    void FindReferences()
    {
        if (player == null) player = FindAnyObjectByType<FirstPersonPawn>();
        if (vehicle == null) vehicle = FindAnyObjectByType<VehiclePawn>();
    }

    void Update()
    {
        if (finished || busy) return;

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
                done = vehicle != null &&
                       Hold(Mathf.Abs(vehicle.steer) > 0.5f && Mathf.Abs(vehicle.Speed) >= step.minSpeed, step.value);
                break;

            case Condition.Throttle:
                done = vehicle != null &&
                       Hold(vehicle.throttle > 0.5f && Mathf.Abs(vehicle.Speed) >= step.minSpeed, step.value);
                break;

            case Condition.Brake:
                done = vehicle != null &&
                       Hold(vehicle.brake > 0.5f && Mathf.Abs(vehicle.Speed) >= step.minSpeed, step.value);
                break;

            case Condition.ChangeView:
                done = vehicle != null && vehicle.CurrentView != viewAtStepStart;
                break;
        }

        if (!done) return;

        if (step.freezeAfter) StartCoroutine(FreezeAndAdvance());
        else Advance();
    }

    // Suma tiempo mientras la condición se cumple (con el juego pausado, Time.deltaTime vale 0).
    private bool Hold(bool active, float seconds)
    {
        if (active) progress += Time.deltaTime;
        return progress >= seconds;
    }

    /// <summary>
    /// Congela el juego, felicita, cambia al siguiente texto, deja un momento para leerlo y continúa.
    /// Usa tiempo real (WaitForSecondsRealtime) porque con Time.timeScale = 0 el tiempo normal no avanza.
    /// </summary>
    private IEnumerator FreezeAndAdvance()
    {
        busy = true;
        Freeze(true);

        if (stepText != null) stepText.text = $"<color={praiseColor}>{praise}</color>";
        yield return new WaitForSecondsRealtime(praiseSeconds);

        Advance(); // pone el texto de la siguiente función
        yield return new WaitForSecondsRealtime(readSeconds);

        Freeze(false);
        busy = false;
    }

    private void Freeze(bool on)
    {
        frozen = on;
        if (on)
        {
            Time.timeScale = 0f;
        }
        else
        {
            // Si el jugador abrió el menú de pausa en ese momento, no lo descongeles por él.
            PauseMenu menu = FindAnyObjectByType<PauseMenu>();
            if (menu == null || !menu.IsOpen) Time.timeScale = 1f;
        }
    }

    private void Advance()
    {
        index++;
        if (index >= steps.Length) Finish();
        else ShowStep();
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
