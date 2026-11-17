using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

/// <summary>
/// Muestra la tecla o el botón de una acción: "E" con teclado, "Y" / "Triángulo" con mando (según la marca),
/// o el texto de Arduino. Cambia solo según el dispositivo que se esté usando (ver InputScheme).
/// USO: ponerlo en un texto y arrastrar la acción (ej: Player/Interact) desde InputSystem_Actions.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class InputHint : MonoBehaviour
{
    /// <summary>True si se está usando un mando.</summary>
    public static bool UsingGamepad => InputScheme.Current == ControlScheme.Gamepad;

    /// <summary>True si se está usando el Arduino.</summary>
    public static bool UsingArduino => InputScheme.Current == ControlScheme.Arduino;

    [Tooltip("La acción, arrastrada desde Assets/InputSystem_Actions (despliega la flecha del archivo).")]
    public InputActionReference action;

    [Tooltip("{0} se reemplaza por la tecla. Ej: \"{0}\" o \"[{0}] Vista\".")]
    public string format = "{0}";

    [Tooltip("Opcional: qué mostrar si se está usando el Arduino. Ej: \"BOTÓN 1\". Vacío = no cambia.")]
    public string arduinoKey = "";

    private TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        string key;
        if (UsingArduino && arduinoKey != "") key = arduinoKey;
        else if (action == null || action.action == null) key = null; // sin acción asignada: no hay nada que mostrar
        else key = KeyName(action.action, UsingGamepad ? "Gamepad" : "Keyboard&Mouse");

        text.text = key == null ? "" : string.Format(format, key);
    }

    /// <summary>Nombre corto del primer botón de la acción en ese grupo ("E", "Y", "Triángulo", "Start", "RB").</summary>
    static string KeyName(InputAction action, string group)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite || !binding.groups.Contains(group)) continue;

            // En el mando, usar el nombre según la marca (Xbox, PlayStation...).
            const string padPrefix = "<Gamepad>/";
            if (group == "Gamepad" && binding.path.StartsWith(padPrefix))
            {
                string padName = ControlText.PadControl(binding.path.Substring(padPrefix.Length));
                if (padName != null) return padName;
            }
            return action.GetBindingDisplayString(i);
        }
        return "?";
    }
}
