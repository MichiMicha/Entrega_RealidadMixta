using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Tutorial interactivo: el jugador PRUEBA cada control en vez de memorizarlo.
///   1. Se muestra una instrucción y el jugador es libre de moverse.
///   2. Cuando hace lo que se pide (caminar, acelerar, girar...), el juego se CONGELA con una felicitación
///      y aparece la siguiente instrucción.
///   3. El juego sigue congelado hasta que el jugador se MUEVE o toca cualquier control (teclado, mando o
///      volante Arduino): ahí se descongela y la instrucción se queda igual hasta que la cumpla.
/// Los textos se adaptan solos al dispositivo que se esté usando (teclado, mando Xbox / PlayStation /
/// Nintendo, o volante Arduino), ver InputScheme y ControlText.
/// Solo el paso "Wait" usa el texto escrito en el Inspector; los demás se generan según la condición.
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
        [Tooltip("Solo se usa en los pasos Wait. Los demás pasos escriben su texto solos según el dispositivo.")]
        [TextArea(2, 4)] public string text;
        public Condition condition = Condition.Wait;

        [Tooltip("Wait: segundos que se muestra. Steer/Throttle/Brake: segundos que debe mantener la acción. \n" +
                 "Walk: metros. Look: grados. Los demás lo ignoran.")]
        public float value = 3f;

        [Tooltip("Steer/Throttle/Brake: el carro debe ir al menos a esta velocidad (m/s) para que cuente. \n" +
                 "El timón solo gira el carro si se mueve, así que en Steer conviene 2.")]
        public float minSpeed = 0f;

        [Tooltip("Al lograrlo, el juego se congela con una felicitación y pasa al siguiente texto.")]
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
        new Step { condition = Condition.Look, value = 45f },
        new Step { condition = Condition.Walk, value = 2f },
        new Step { condition = Condition.EnterCar },
        new Step { condition = Condition.Steer, value = 0.7f, minSpeed = 2f },
        new Step { condition = Condition.Throttle, value = 1f },
        new Step { condition = Condition.Brake, value = 0.8f },
        new Step { condition = Condition.ChangeView }
    };

    [TextArea(2, 4)]
    [SerializeField] private string finalMessage = "¡Listo! Ahora atrapa la fruta que está en el escenario.";

    [Header("Pausa entre pasos")]
    [SerializeField] private string praise = "¡Muy bien!";
    [SerializeField] private string praiseColor = "#7CFC00";

    [Tooltip("Segundos que el juego queda congelado mostrando la felicitación.")]
    [SerializeField] private float praiseSeconds = 0.8f;

    [Tooltip("Segundos mínimos que se muestra la siguiente instrucción antes de aceptar que el jugador se mueva. " +
             "Así alcanza a leerla y un control que sigue apretado del paso anterior no la salta.")]
    [SerializeField] private float readSeconds = 1f;

    [Tooltip("Activo: tras cada paso el juego sigue congelado hasta que el jugador se mueva o toque un control. " +
             "Apagado: se descongela solo después de 'Read Seconds'.")]
    [SerializeField] private bool waitForInput = true;

    private int index;
    private float progress;
    private int viewAtStepStart;
    private Vector3 playerStartPos;
    private float playerStartYaw;
    private bool finished;
    private bool busy;           // mientras dura la pausa entre pasos
    private bool frozen;
    private bool showingPraise;  // la felicitación no se debe pisar al cambiar de dispositivo
    private PauseMenu pauseMenu;
    private ArduinoController arduino;

    // Paso "Walk": direcciones distintas que el jugador ya usó (1 = adelante/W, 2 = atrás/S, 4 = izquierda/A, 8 = derecha/D).
    private int walkDirections;

    // Con qué dispositivo se escribió el texto que se ve ahora (para reescribirlo si cambia).
    private ControlScheme shownScheme;
    private PadBrand shownBrand;

    void Start()
    {
        FindReferences();
        pauseMenu = FindAnyObjectByType<PauseMenu>();
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
        if (vehicle == null || !vehicle.isActiveAndEnabled) vehicle = FindAnyObjectByType<VehiclePawn>();

        // Si hay más de un VehiclePawn (ej: uno duplicado o desactivado en el carro), usa el que el jugador
        // realmente está manejando. Así el tutorial no se queda mirando un componente que nadie posee.
        VehiclePawn driven = FindDrivenVehicle();
        if (driven != null) vehicle = driven;
    }

    // Devuelve el VehiclePawn que tiene un controlador (el que se está conduciendo), o null si ninguno.
    private static VehiclePawn FindDrivenVehicle()
    {
        VehiclePawn[] all = FindObjectsByType<VehiclePawn>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (VehiclePawn v in all)
            if (v.Controller != null) return v;
        return null;
    }

    void Update()
    {
        RefreshTextIfDeviceChanged();
        TrackWalkInput();

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
                // Basta con usar dos direcciones distintas (ej: W y A), sin esperar a recorrer la distancia.
                if ((walkDirections & (walkDirections - 1)) != 0) { done = true; break; }
                Vector3 moved = player.transform.position - playerStartPos;
                moved.y = 0f;
                done = moved.magnitude >= step.value;
                break;

            case Condition.EnterCar:
                // El Pawn.Controller es null mientras nadie maneja el carro. Se revisa cualquier VehiclePawn
                // y también si el jugador a pie ya se desactivó (al subirse, VehiclePawn.Interact lo apaga).
                done = vehicle == null || FindDrivenVehicle() != null ||
                       (player != null && !player.gameObject.activeInHierarchy);
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
    /// 1) Congela el juego y felicita.
    /// 2) Cambia al texto del siguiente paso.
    /// 3) Sigue congelado hasta que el jugador se mueva o toque un control (con teclado, mando o volante).
    /// 4) Descongela: la instrucción no cambia hasta que el jugador la cumpla.
    /// Usa tiempo real (WaitForSecondsRealtime) porque con Time.timeScale = 0 el tiempo normal no avanza.
    /// </summary>
    private IEnumerator FreezeAndAdvance()
    {
        busy = true;
        Freeze(true);

        showingPraise = true;
        if (stepText != null) stepText.text = $"<color={praiseColor}>{praise}</color>";
        yield return new WaitForSecondsRealtime(praiseSeconds);
        showingPraise = false;

        Advance(); // pone el texto de la siguiente instrucción (o el mensaje final)
        yield return new WaitForSecondsRealtime(readSeconds);

        if (waitForInput && !finished)
        {
            float since = Time.unscaledTime;
            while (InputScheme.LastActivityTime <= since)
            {
                if (pauseMenu != null && pauseMenu.IsOpen)
                {
                    // Con el menú de pausa abierto, tocar botones no cuenta como "moverse".
                    since = Time.unscaledTime;
                }
                else if (Time.timeScale != 0f)
                {
                    Time.timeScale = 0f; // si el menú se cerró, seguir congelado hasta que se mueva
                }
                yield return null;
            }
        }

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
            if (pauseMenu == null || !pauseMenu.IsOpen) Time.timeScale = 1f;
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
        walkDirections = 0;
        if (vehicle != null) viewAtStepStart = vehicle.CurrentView;
        if (player != null)
        {
            playerStartPos = player.transform.position;
            playerStartYaw = player.transform.eulerAngles.y;
        }
        WriteStepText();
    }

    private void Finish()
    {
        finished = true;
        if (stepText != null) stepText.text = finalMessage;
    }

    /// <summary>
    /// Paso Walk: anota qué direcciones usó el jugador (teclado W A S D o flechas, stick izquierdo del mando,
    /// o pedales y volante del Arduino). Se anota también mientras el juego está congelado esperando al
    /// jugador, así la primera tecla que lo descongela ya cuenta.
    /// </summary>
    private void TrackWalkInput()
    {
        if (showingPraise || index >= steps.Length || steps[index].condition != Condition.Walk) return;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) walkDirections |= 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) walkDirections |= 2;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) walkDirections |= 4;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) walkDirections |= 8;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.y > 0.5f) walkDirections |= 1;
            if (stick.y < -0.5f) walkDirections |= 2;
            if (stick.x < -0.5f) walkDirections |= 4;
            if (stick.x > 0.5f) walkDirections |= 8;
        }

        if (InputScheme.Current == ControlScheme.Arduino)
        {
            if (arduino == null) arduino = FindAnyObjectByType<ArduinoController>();
            if (arduino != null)
            {
                if (arduino.throttle.value > 0.3f) walkDirections |= 1;   // pedal de acelerar = adelante
                if (arduino.brake.value > 0.3f) walkDirections |= 2;      // pedal de freno = atrás
                if (arduino.steering.value < -0.3f) walkDirections |= 4;  // volante a la izquierda
                if (arduino.steering.value > 0.3f) walkDirections |= 8;   // volante a la derecha
            }
        }
    }

    // ---------- Textos según el dispositivo ----------

    private void WriteStepText()
    {
        if (stepText == null || index >= steps.Length) return;
        shownScheme = InputScheme.Current;
        shownBrand = InputScheme.Brand;
        stepText.text = TextFor(steps[index]);
    }

    // Si el jugador cambia de teclado a mando o a volante, la instrucción se reescribe al momento.
    private void RefreshTextIfDeviceChanged()
    {
        if (finished || showingPraise || stepText == null || index >= steps.Length) return;
        if (InputScheme.Current == shownScheme && InputScheme.Brand == shownBrand) return;
        WriteStepText();
    }

    private static string TextFor(Step step)
    {
        ControlScheme scheme = InputScheme.Current;
        bool keyboard = scheme == ControlScheme.Keyboard;
        bool arduino = scheme == ControlScheme.Arduino;

        switch (step.condition)
        {
            case Condition.Look:
                if (keyboard) return "Mueve el mouse para mirar a tu alrededor.";
                if (arduino) return "Gira el volante para mirar a tu alrededor.";
                return "Mueve el stick derecho para mirar a tu alrededor.";

            case Condition.Walk:
                if (keyboard) return "Camina con W A S D (prueba con dos teclas).";
                if (arduino) return "Camina: pedal de acelerar para ir adelante y pedal de freno para ir atrás.";
                return "Camina con el stick izquierdo.";

            case Condition.EnterCar:
                return $"Acércate al carro, míralo y usa {ControlText.Name(GameAction.Interact)} para subirte.";

            case Condition.Steer:
                return "Gira el timón para dirigir el carro a los lados.\n" + Hint(GameAction.Steer);

            case Condition.Throttle:
                return "Usa el acelerador para avanzar.\n" + Hint(GameAction.Throttle);

            case Condition.Brake:
                return "Usa el freno para detenerte. Si ya estás detenido, el freno te da reversa.\n" + Hint(GameAction.Brake);

            case Condition.ChangeView:
                return "Cambia la vista de la cámara.\n" + Hint(GameAction.ChangeView);

            default: // Wait
                return step.text;
        }
    }

    // "(Teclado: W)", "(Mando Xbox: RT)", "(Volante: el pedal de acelerar)"
    private static string Hint(GameAction action)
    {
        return $"({ControlText.DeviceName()}: {ControlText.Name(action)})";
    }
}
