using UnityEngine;

// Une el Arduino y el teclado en un solo lugar.
// Así puedes probar el juego sin el volante (W/S/A/D o flechas, Enter o Espacio).
public static class GameInput
{
    private static bool prevConfirm;
    private static bool confirmDown;
    private static int lastFrame = -1;

    // throttle: 1 = acelerar, -1 = retroceder | steering: -1 izquierda, 1 derecha
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

    // true SOLO en el momento en que se presiona el botón confirmar
    public static bool ConfirmPressed()
    {
        if (lastFrame != Time.frameCount)
        {
            lastFrame = Time.frameCount;

            bool c = Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space);
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
