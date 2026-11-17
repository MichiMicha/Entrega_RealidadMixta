using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Convierte los potenciómetros del Arduino en los controles del juego, igual que los gatillos y el
/// stick de un control: acelerador (0..1), freno (0..1), timón (-1..1).
///
/// El Arduino solo manda cuando el jugador lo está USANDO (ver InputScheme): si toca el teclado o el
/// mando, vuelven a mandar ellos, y si toca un pedal, el volante o un botón, manda el Arduino.
///
/// En el carro:   acelerador / freno / timón = acelerar / frenar / girar.   CICLO = cambiar vista.
/// A pie:         pedal de acelerar = adelante, pedal de freno = atrás, volante = girar.   CICLO = interactuar (subirse).
/// En los menús:  pedal de acelerar = subir (en un slider: más), pedal de freno = bajar (en un slider: menos).
///                CICLO: toque = siguiente, mantener = seleccionar.  ATRÁS = volver / pausa.
/// </summary>
public class ArduinoController : MonoBehaviour
{
    [System.Serializable]
    public class Axis
    {
        [Tooltip("Posición del número en la línea del Arduino (0 = el primero).")]
        public int index;

        [Tooltip("Valor crudo en reposo / extremo izquierdo. Calibrar mirando Raw en Play.")]
        public int min = 0;

        [Tooltip("Valor crudo a fondo / extremo derecho.")]
        public int max = 1023;

        public bool invert;

        [Tooltip("Zona muerta: cerca del reposo (o del centro, en el timón) cuenta como 0.")]
        [Range(0f, 0.3f)] public float deadZone = 0.05f;

        [Header("Solo lectura")]
        public int raw;
        public float value;
    }

    [Tooltip("Si se deja vacío, lo busca solo.")]
    public PlayerController player;

    public Axis throttle = new Axis { index = 0 };
    public Axis steering = new Axis { index = 1 };
    [Tooltip("Si el control no tiene freno, el Arduino debe mandar 0 en esa posición.")]
    public Axis brake = new Axis { index = 2 };

    [Header("Botones (0 = suelto, 1 = presionado)")]
    [Tooltip("Toque corto: siguiente botón del menú. En el juego: cambiar vista (en el carro) o interactuar (a pie). Mantener: seleccionar.")]
    public int cycleButtonIndex = 3;

    [Tooltip("Atrás en los menús. En el juego abre el menú de pausa.")]
    public int backButtonIndex = 4;

    [Tooltip("Segundos manteniendo el botón de ciclar para seleccionar.")]
    public float holdToSelect = 0.6f;

    [Header("Detección de uso")]
    [Tooltip("Cuánto se debe mover un pedal o el volante (0..1) para contar como que el jugador lo está usando. " +
             "Sirve para pasar de teclado/mando al Arduino sin que el ruido del potenciómetro lo active.")]
    [Range(0.02f, 0.5f)] public float activityThreshold = 0.1f;

    [Header("A pie (sin carro)")]
    [Tooltip("Velocidad de giro con el volante cuando se camina (grados por segundo).")]
    public float walkTurnSpeed = 120f;

    [Header("Pedales en los menús")]
    [Tooltip("Qué tanto hay que pisar el pedal (0..1) para contar como pulsación.")]
    [Range(0.1f, 0.9f)] public float pedalPressAt = 0.6f;
    [Tooltip("El pedal debe bajar de aquí para poder contar otra vez.")]
    [Range(0.05f, 0.8f)] public float pedalReleaseAt = 0.3f;
    [Tooltip("En un slider: segundos manteniendo el pedal antes de que se repita.")]
    public float sliderRepeatDelay = 0.4f;
    [Tooltip("En un slider: segundos entre cada repetición mientras se mantiene el pedal.")]
    public float sliderRepeatRate = 0.12f;

    private bool cycleWasDown;
    private bool backWasDown;
    private bool holdDone;
    private bool suppressCycle;   // el toque empezó eligiendo carro: que al soltar no haga nada más
    private float cycleDownTime;

    // Referencia de los ejes para saber si el jugador los mueve (así el reposo no cuenta como uso).
    private bool baselineSet;
    private float baseThrottle, baseBrake, baseSteering;

    // Empiezan en true: primero hay que soltar los pedales antes de que cuenten en un menú.
    private bool forwardHeld = true, backwardHeld = true;
    private float forwardNextRepeat, backwardNextRepeat;

    /// <summary>True si hay un botón o slider de un menú seleccionado (menú abierto).</summary>
    public bool MenuIsOpen => MenuSelection() != null;

    // LateUpdate corre después del Update del PlayerController, así el Arduino reemplaza al teclado.
    void LateUpdate()
    {
        ArduinoSerial arduino = ArduinoSerial.Instance;
        if (arduino == null || !arduino.IsConnected)
        {
            baselineSet = false;
            return;
        }

        // El Arduino pasa de escena en escena: buscar el jugador de la escena actual.
        if (player == null) player = FindAnyObjectByType<PlayerController>();

        Read(arduino, throttle, false);
        Read(arduino, brake, false);
        Read(arduino, steering, true);

        DetectAxisActivity();
        ReadButtons(arduino);
        ReadPedalsForMenus();

        // Si el jugador está usando teclado o mando, ellos manejan.
        if (InputScheme.Current != ControlScheme.Arduino) return;
        if (player == null || player.pawn == null || !player.enabled) return;

        if (player.pawn is FirstPersonPawn)
        {
            // A pie: acelerador = adelante, freno = atrás, volante = girar el cuerpo.
            player.pawn.Move(new Vector2(0f, throttle.value - brake.value));
            player.pawn.Look(new Vector2(steering.value * walkTurnSpeed * Time.deltaTime, 0f));
            return;
        }

        player.pawn.Drive(throttle.value, brake.value, steering.value);
    }

    /// <summary>Avisa a InputScheme cuando un pedal o el volante se mueve de verdad.</summary>
    void DetectAxisActivity()
    {
        if (!baselineSet)
        {
            baseThrottle = throttle.value;
            baseBrake = brake.value;
            baseSteering = steering.value;
            baselineSet = true;
            return;
        }

        if (Mathf.Abs(throttle.value - baseThrottle) > activityThreshold ||
            Mathf.Abs(brake.value - baseBrake) > activityThreshold ||
            Mathf.Abs(steering.value - baseSteering) > activityThreshold)
        {
            baseThrottle = throttle.value;
            baseBrake = brake.value;
            baseSteering = steering.value;
            InputScheme.ReportActivity(ControlScheme.Arduino);
        }
    }

    void ReadButtons(ArduinoSerial arduino)
    {
        bool cycle = arduino.GetRaw(cycleButtonIndex) > 0;
        bool back = arduino.GetRaw(backButtonIndex) > 0;
        if ((cycle && !cycleWasDown) || (back && !backWasDown)) InputScheme.ReportActivity(ControlScheme.Arduino);

        // Tiempo real (unscaled): en el menú de pausa Time.timeScale es 0.
        if (cycle && !cycleWasDown)
        {
            cycleDownTime = Time.unscaledTime;
            holdDone = false;
            // Si con este toque se está eligiendo carro, no debe además "interactuar" al soltarlo.
            suppressCycle = CarSelector.IsChoosing && !MenuIsOpen;
        }
        if (cycle && !holdDone && Time.unscaledTime - cycleDownTime >= holdToSelect)
        {
            holdDone = true;
            if (!suppressCycle) Submit();
        }
        if (!cycle && cycleWasDown && !holdDone && !suppressCycle) Cycle(); // se soltó antes de tiempo: toque corto
        if (back && !backWasDown) Back();

        cycleWasDown = cycle;
        backWasDown = back;
    }

    /// <summary>
    /// Pedales en un menú abierto: acelerar = subir (o más en un slider), frenar = bajar (o menos).
    /// Fuera de un menú los pedales manejan el carro. En la pantalla de elegir carro los lee el CarSelector.
    /// </summary>
    void ReadPedalsForMenus()
    {
        // Con histéresis: se activa al pasar pedalPressAt y se suelta al bajar de pedalReleaseAt.
        bool forward = throttle.value > (forwardHeld ? pedalReleaseAt : pedalPressAt);
        bool backward = brake.value > (backwardHeld ? pedalReleaseAt : pedalPressAt);
        bool forwardDown = forward && !forwardHeld;
        bool backwardDown = backward && !backwardHeld;
        forwardHeld = forward;
        backwardHeld = backward;

        GameObject selected = MenuSelection();
        if (selected == null) return;

        float now = Time.unscaledTime;

        Slider slider = selected.GetComponent<Slider>();
        if (slider != null)
        {
            // Un slider se mueve mientras se mantiene el pedal.
            if (forwardDown) { forwardNextRepeat = now + sliderRepeatDelay; StepSlider(slider, +1); }
            else if (forward && now >= forwardNextRepeat) { forwardNextRepeat = now + sliderRepeatRate; StepSlider(slider, +1); }

            if (backwardDown) { backwardNextRepeat = now + sliderRepeatDelay; StepSlider(slider, -1); }
            else if (backward && now >= backwardNextRepeat) { backwardNextRepeat = now + sliderRepeatRate; StepSlider(slider, -1); }
            return;
        }

        if (forwardDown) MoveSelection(selected, true);
        if (backwardDown) MoveSelection(selected, false);
    }

    /// <summary>El botón o slider seleccionado en un menú abierto (null si no hay menú).</summary>
    GameObject MenuSelection()
    {
        if (EventSystem.current == null) return null;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected != null && selected.activeInHierarchy ? selected : null;
    }

    void Cycle()
    {
        GameObject selected = MenuSelection();
        if (selected == null)
        {
            if (player == null || player.pawn == null || !player.enabled) return;
            if (player.pawn is FirstPersonPawn) player.pawn.Interact(); // a pie: subirse al carro
            else player.pawn.ChangeView();                              // en el carro: cambiar vista
            return;
        }

        // Bajar al siguiente; al llegar al final, volver al primero de arriba.
        MoveSelection(selected, false);
    }

    /// <summary>Selecciona el botón de arriba o de abajo. Al llegar al borde, da la vuelta al otro extremo.</summary>
    void MoveSelection(GameObject selected, bool up)
    {
        Selectable current = selected.GetComponent<Selectable>();
        if (current == null) return;

        Selectable next = up ? current.FindSelectableOnUp() : current.FindSelectableOnDown();
        if (next == null)
        {
            next = current;
            for (int i = 0; i < 50; i++)
            {
                Selectable beyond = up ? next.FindSelectableOnDown() : next.FindSelectableOnUp();
                if (beyond == null) break;
                next = beyond;
            }
        }
        EventSystem.current.SetSelectedGameObject(next.gameObject);
    }

    void StepSlider(Slider slider, int direction)
    {
        float step = (slider.maxValue - slider.minValue) * 0.05f;
        if (slider.wholeNumbers) step = Mathf.Max(1f, step);
        slider.value = Mathf.Clamp(slider.value + direction * step, slider.minValue, slider.maxValue);
    }

    void Submit()
    {
        GameObject selected = MenuSelection();
        if (selected == null) return;

        // Un slider no se "presiona": cada vez que se mantiene sube 10 % y al pasar el máximo vuelve al mínimo.
        Slider slider = selected.GetComponent<Slider>();
        if (slider != null)
        {
            float step = (slider.maxValue - slider.minValue) * 0.1f;
            slider.value = slider.value + step > slider.maxValue + 0.001f ? slider.minValue : slider.value + step;
            return;
        }
        ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    void Back()
    {
        PauseMenu pause = FindAnyObjectByType<PauseMenu>();
        if (pause != null)
        {
            pause.OpenOrBack();
            return;
        }
        MainMenu main = FindAnyObjectByType<MainMenu>();
        if (main != null) main.Back();
    }

    /// <summary>Pedal: raw (min..max) → 0..1. Timón (centered): raw (min..max) → -1..1.</summary>
    void Read(ArduinoSerial arduino, Axis axis, bool centered)
    {
        axis.raw = arduino.GetRaw(axis.index);

        float t = Mathf.InverseLerp(axis.min, axis.max, axis.raw);
        if (axis.invert) t = 1f - t;
        if (centered) t = t * 2f - 1f;
        if (Mathf.Abs(t) < axis.deadZone) t = 0f;
        axis.value = t;
    }
}
