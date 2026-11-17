using UnityEngine;

/// <summary>Acciones del juego que se nombran en los textos de ayuda.</summary>
public enum GameAction
{
    Look,       // mirar a los lados (a pie)
    Walk,       // caminar (a pie)
    Interact,   // subirse al carro
    Throttle,   // acelerar
    Brake,      // frenar / reversa
    Steer,      // girar el carro
    ChangeView, // cambiar la cámara
    Pause,      // abrir el menú de pausa
    PrevCar,    // carro anterior (selector)
    NextCar,    // carro siguiente (selector)
    Confirm     // aceptar / elegir
}

/// <summary>
/// Nombres de teclas, botones y pedales según el dispositivo que se esté usando
/// (InputScheme.Current) y la marca del mando (InputScheme.Brand).
/// Así los textos del tutorial y del selector de carro dicen "W", "RT" o "pedal de acelerar" según toque.
/// </summary>
public static class ControlText
{
    /// <summary>Nombre del dispositivo para los textos: "Teclado", "Mando Xbox", "Arduino".</summary>
    public static string DeviceName()
    {
        switch (InputScheme.Current)
        {
            case ControlScheme.Arduino: return "Arduino";
            case ControlScheme.Gamepad:
                switch (InputScheme.Brand)
                {
                    case PadBrand.Xbox: return "Mando Xbox";
                    case PadBrand.PlayStation: return "Mando PlayStation";
                    case PadBrand.Nintendo: return "Mando Nintendo";
                    default: return "Mando";
                }
            default: return "Teclado";
        }
    }

    /// <summary>Cómo se hace esa acción con el dispositivo actual. Ej: Throttle → "W", "RT", "pedal de acelerar".</summary>
    public static string Name(GameAction action)
    {
        switch (InputScheme.Current)
        {
            case ControlScheme.Arduino: return ArduinoName(action);
            case ControlScheme.Gamepad: return PadActionName(action);
            default: return KeyboardName(action);
        }
    }

    static string KeyboardName(GameAction action)
    {
        switch (action)
        {
            case GameAction.Look: return "el mouse";
            case GameAction.Walk: return "W A S D";
            case GameAction.Interact: return "E";
            case GameAction.Throttle: return "W";
            case GameAction.Brake: return "S";
            case GameAction.Steer: return "A / D";
            case GameAction.ChangeView: return "V";
            case GameAction.Pause: return "Esc";
            case GameAction.PrevCar: return "flecha izquierda";
            case GameAction.NextCar: return "flecha derecha";
            case GameAction.Confirm: return "Enter";
        }
        return "?";
    }

    static string PadActionName(GameAction action)
    {
        switch (action)
        {
            case GameAction.Look: return "el stick derecho";
            case GameAction.Walk: return "el stick izquierdo";
            case GameAction.Interact: return PadControl("buttonNorth");
            case GameAction.Throttle: return PadControl("rightTrigger");
            case GameAction.Brake: return PadControl("leftTrigger");
            case GameAction.Steer: return "el stick izquierdo";
            case GameAction.ChangeView: return PadControl("dpad/right");
            case GameAction.Pause: return PadControl("start");
            case GameAction.PrevCar: return PadControl("dpad/left");
            case GameAction.NextCar: return PadControl("dpad/right");
            case GameAction.Confirm: return PadControl("buttonSouth");
        }
        return "?";
    }

    static string ArduinoName(GameAction action)
    {
        switch (action)
        {
            case GameAction.Look: return "el volante";
            case GameAction.Walk: return "los pedales";
            case GameAction.Interact: return "el botón CICLO";
            case GameAction.Throttle: return "el pedal de acelerar";
            case GameAction.Brake: return "el pedal de freno";
            case GameAction.Steer: return "el volante";
            case GameAction.ChangeView: return "el botón CICLO";
            case GameAction.Pause: return "el botón ATRÁS";
            case GameAction.PrevCar: return "el pedal de freno";
            case GameAction.NextCar: return "el pedal de acelerar";
            case GameAction.Confirm: return "el botón CICLO";
        }
        return "?";
    }

    /// <summary>
    /// Nombre de un control del mando según la marca. Recibe el final de la ruta del Input System:
    /// "buttonNorth", "rightTrigger", "dpad/left"... Devuelve null si no lo conoce.
    /// </summary>
    public static string PadControl(string leaf)
    {
        PadBrand b = InputScheme.Brand;
        bool xbox = b == PadBrand.Xbox, ps = b == PadBrand.PlayStation, ns = b == PadBrand.Nintendo;

        switch (leaf)
        {
            case "buttonSouth": return xbox ? "A" : ps ? "Cruz" : ns ? "B" : "el botón de abajo";
            case "buttonEast": return xbox ? "B" : ps ? "Círculo" : ns ? "A" : "el botón de la derecha";
            case "buttonWest": return xbox ? "X" : ps ? "Cuadrado" : ns ? "Y" : "el botón de la izquierda";
            case "buttonNorth": return xbox ? "Y" : ps ? "Triángulo" : ns ? "X" : "el botón de arriba";
            case "leftTrigger": return xbox ? "LT" : ps ? "L2" : ns ? "ZL" : "el gatillo izquierdo";
            case "rightTrigger": return xbox ? "RT" : ps ? "R2" : ns ? "ZR" : "el gatillo derecho";
            case "leftShoulder": return xbox ? "LB" : ps ? "L1" : ns ? "L" : "el bumper izquierdo";
            case "rightShoulder": return xbox ? "RB" : ps ? "R1" : ns ? "R" : "el bumper derecho";
            case "start": return xbox ? "Menú" : ps ? "Options" : ns ? "+" : "Start";
            case "select": return xbox ? "Vista" : ps ? "Share" : ns ? "-" : "Select";
            case "dpad/left": return "la cruceta izquierda";
            case "dpad/right": return "la cruceta derecha";
            case "dpad/up": return "la cruceta arriba";
            case "dpad/down": return "la cruceta abajo";
        }
        return null;
    }
}
