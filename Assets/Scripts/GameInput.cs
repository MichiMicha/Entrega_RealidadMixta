using UnityEngine;

// Une el Arduino y el teclado en un solo lugar.
// TECLADO:  W/S o flechas = acelerar / frenar-retroceder
//           A/D o flechas = girar
//           Espacio o Shift = freno de mano (derrape)
//           Enter = confirmar
public static class GameInput
{
    private static bool prevConfirm;
    private static bool confirmDown;
    private static int lastFrame = -1;

    // throttle: 1 = acelerar, -1 = frenar/retroceder | steering: -1 izquierda, 1 derecha
    public static void Read(out float throttle, out float steering)
    {
        throttle = Input.GetAxisRaw("Vertical");
        steering = Input.GetAxisRaw("Horizontal");

        var r = ArduinoSerialReader.Instance;
        if (r != null && r.IsConnected)
        {
            r.GetState(out float st, out bool acc, out bool rev, out _);
            throttle += (acc ? 1f : 0f) - (rev ? 1f : 0f);
            steering += st;
        }

        throttle = Mathf.Clamp(throttle, -1f, 1f);
        steering = Mathf.Clamp(steering, -1f, 1f);
    }

    // Freno de mano (por ahora solo teclado)
    public static bool Handbrake()
    {
        return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);
    }

    // true SOLO en el momento en que se presiona el botón confirmar
    public static bool ConfirmPressed()
    {
        if (lastFrame != Time.frameCount)
        {
            lastFrame = Time.frameCount;

            bool c = Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);
            var r = ArduinoSerialReader.Instance;
            if (r != null && r.IsConnected)
            {
                r.GetState(out _, out _, out _, out bool ok);
                c |= ok;
            }

            confirmDown = c && !prevConfirm;
            prevConfirm = c;
        }
        return confirmDown;
    }
}
