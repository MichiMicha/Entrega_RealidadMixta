using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>Con qué se está jugando ahora mismo.</summary>
public enum ControlScheme { Keyboard, Gamepad, Arduino }

/// <summary>Marca del mando. Solo cambia el nombre de los botones en los textos.</summary>
public enum PadBrand { Generic, Xbox, PlayStation, Nintendo }

/// <summary>
/// Detecta qué dispositivo está usando el jugador: teclado/mouse, mando (Xbox, PlayStation, Switch...
/// cualquiera que Unity reconozca como Gamepad) o el volante Arduino.
/// Gana el último que se tocó. Se crea solo al iniciar el juego y sobrevive a los cambios de escena,
/// no hay que ponerlo en ninguna escena.
///   - InputScheme.Current  : Keyboard / Gamepad / Arduino
///   - InputScheme.Brand    : Xbox / PlayStation / Nintendo / Generic (solo con mando)
///   - InputScheme.LastActivityTime : última vez (tiempo real) que el jugador hizo algo nuevo en cualquier dispositivo
/// El ArduinoController avisa de su actividad con InputScheme.ReportActivity(ControlScheme.Arduino).
/// </summary>
[DefaultExecutionOrder(-100)]
public class InputScheme : MonoBehaviour
{
    public static ControlScheme Current { get; private set; } = ControlScheme.Keyboard;
    public static PadBrand Brand { get; private set; } = PadBrand.Generic;

    /// <summary>Time.unscaledTime de la última acción nueva del jugador (funciona aunque el juego esté pausado).</summary>
    public static float LastActivityTime { get; private set; }

    const float MouseMovePixels = 3f;   // píxeles por frame para contar el mouse como "se movió"
    const float AnalogOn = 0.5f;        // stick o gatillo: se considera "usado" al pasar de aquí
    const float AnalogOff = 0.3f;       // y vuelve a "soltado" al bajar de aquí (evita parpadeos)

    private bool leftStickOn, rightStickOn, leftTriggerOn, rightTriggerOn;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        Current = ControlScheme.Keyboard;
        Brand = PadBrand.Generic;
        LastActivityTime = 0f;

        GameObject go = new GameObject("[InputScheme]");
        DontDestroyOnLoad(go);
        go.AddComponent<InputScheme>();
    }

    /// <summary>El jugador hizo algo con este dispositivo: pasa a ser el que se usa.</summary>
    public static void ReportActivity(ControlScheme scheme)
    {
        Current = scheme;
        LastActivityTime = Time.unscaledTime;
    }

    void Update()
    {
        // ---- Teclado y mouse ----
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool keyboardActive =
            (keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
            (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > MouseMovePixels * MouseMovePixels ||
                               mouse.leftButton.wasPressedThisFrame ||
                               mouse.rightButton.wasPressedThisFrame));
        if (keyboardActive) ReportActivity(ControlScheme.Keyboard);

        // ---- Mando (cualquier marca) ----
        if (PadActive()) ReportActivity(ControlScheme.Gamepad);
    }

    bool PadActive()
    {
        bool buttonPressed = false;
        float leftStick = 0f, rightStick = 0f, leftTrigger = 0f, rightTrigger = 0f;

        foreach (Gamepad pad in Gamepad.all)
        {
            leftStick = Mathf.Max(leftStick, pad.leftStick.ReadValue().magnitude);
            rightStick = Mathf.Max(rightStick, pad.rightStick.ReadValue().magnitude);
            leftTrigger = Mathf.Max(leftTrigger, pad.leftTrigger.ReadValue());
            rightTrigger = Mathf.Max(rightTrigger, pad.rightTrigger.ReadValue());

            if (buttonPressed) continue;
            foreach (InputControl control in pad.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame) { buttonPressed = true; break; }
            }
        }

        // Sticks y gatillos: cuenta el momento en que se empiezan a usar, no mientras se mantienen.
        bool leftStickNow = leftStick > (leftStickOn ? AnalogOff : AnalogOn);
        bool rightStickNow = rightStick > (rightStickOn ? AnalogOff : AnalogOn);
        bool leftTriggerNow = leftTrigger > (leftTriggerOn ? AnalogOff : AnalogOn);
        bool rightTriggerNow = rightTrigger > (rightTriggerOn ? AnalogOff : AnalogOn);

        bool active = buttonPressed ||
                      (leftStickNow && !leftStickOn) || (rightStickNow && !rightStickOn) ||
                      (leftTriggerNow && !leftTriggerOn) || (rightTriggerNow && !rightTriggerOn);

        leftStickOn = leftStickNow;
        rightStickOn = rightStickNow;
        leftTriggerOn = leftTriggerNow;
        rightTriggerOn = rightTriggerNow;

        if (active) Brand = DetectBrand(Gamepad.current ?? (Gamepad.all.Count > 0 ? Gamepad.all[0] : null));
        return active;
    }

    /// <summary>Adivina la marca por el nombre del layout y del producto que reporta el mando.</summary>
    static PadBrand DetectBrand(Gamepad pad)
    {
        if (pad == null) return PadBrand.Generic;

        string id = (pad.layout + " " + pad.description.product + " " + pad.description.manufacturer + " " + pad.displayName)
            .ToLowerInvariant();

        if (id.Contains("dualshock") || id.Contains("dualsense") || id.Contains("playstation") ||
            id.Contains("sony") || id.Contains("ps4") || id.Contains("ps5") || id.Contains("wireless controller"))
            return PadBrand.PlayStation;

        if (id.Contains("xbox") || id.Contains("xinput") || id.Contains("microsoft"))
            return PadBrand.Xbox;

        if (id.Contains("switch") || id.Contains("nintendo") || id.Contains("pro controller"))
            return PadBrand.Nintendo;

        return PadBrand.Generic;
    }
}
