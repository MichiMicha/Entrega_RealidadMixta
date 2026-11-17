using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Pantalla para elegir carro al empezar el tutorial (el juego queda en pausa mientras tanto).
///   Arduino: pedal de freno = carro anterior, pedal de acelerar = carro siguiente, botón CICLO = aceptar.
///   Teclado: flechas ← → y Enter.
///   Mando (Xbox, PlayStation...): cruceta o stick izquierdo ← →, botón A / Cruz = aceptar.
/// El texto de ayuda cambia solo según el dispositivo que se esté usando.
/// </summary>
public class CarSelector : MonoBehaviour
{
    [Tooltip("Un objeto por carro, EN ORDEN (0 Carreras, 1 Común, 2 Achatado, 3 Minivan). Cada uno con su propio " +
             "VehiclePawn, cámara y vistas, y un CarInfo. El selector solo activa el elegido y apaga los demás: " +
             "no toca ningún valor de manejo.")]
    [SerializeField] private GameObject[] cars;
    [Tooltip("Al cambiar de carro, el nuevo aparece en la misma posición y giro del anterior.")]
    [SerializeField] private bool keepPose = true;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text infoText;

    [Header("Pedales")]
    [Tooltip("Qué tanto hay que pisar el pedal (0..1) para contar como pulsación.")]
    [SerializeField] private float pressAt = 0.6f;
    [Tooltip("El pedal debe bajar de aquí para poder contar otra vez.")]
    [SerializeField] private float releaseAt = 0.3f;
    [Tooltip("Actívalo si tus pedales quedan al revés (izquierdo = siguiente).")]
    [SerializeField] private bool swapPedals = false;

    /// <summary>True mientras se está eligiendo carro. Lo usa el ArduinoController para no actuar a la vez.</summary>
    public static bool IsChoosing => active || Time.frameCount == lastAcceptFrame;
    private static bool active;
    private static int lastAcceptFrame = -1;

    private ArduinoController arduino;
    private PauseMenu pauseMenu;
    private bool padStickLeftDown, padStickRightDown;
    private ControlScheme shownScheme;
    private PadBrand shownBrand;
    private int index;
    private int shownCar = -1;
    private bool frozen;
    // true al inicio: primero hay que soltar pedales y botón antes de que cuenten.
    private bool leftDown = true, rightDown = true, buttonDown = true;

    void Start()
    {
        pauseMenu = FindAnyObjectByType<PauseMenu>();
        index = Mathf.Clamp(CarSelection.Index, 0, Mathf.Max(0, cars.Length - 1));
        if (panel != null) panel.SetActive(true);
        frozen = true;
        active = true;
        Time.timeScale = 0f; // el juego espera a que elijas
        Show();
    }

    void OnDisable()
    {
        // Si se cambia de escena en plena elección, que la siguiente no arranque congelada.
        if (frozen) Time.timeScale = 1f;
        active = false;
    }

    void Update()
    {
        // Si se cierra el menú de pausa (que pone timeScale = 1), seguir congelado hasta aceptar.
        if (Time.timeScale != 0f && (pauseMenu == null || !pauseMenu.IsOpen)) Time.timeScale = 0f;

        int move = 0;
        bool accept = false;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame) move = -1;
            if (kb.rightArrowKey.wasPressedThisFrame) move = 1;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) accept = true;
        }

        ReadGamepad(ref move, ref accept);
        ReadArduino(ref move, ref accept);

        // Si el jugador cambia de dispositivo, la ayuda de abajo cambia al momento.
        if (InputScheme.Current != shownScheme || InputScheme.Brand != shownBrand) ShowText();

        if (move != 0)
        {
            index = (index + move + cars.Length) % cars.Length;
            Show();
        }
        if (accept) Accept();
    }

    void ReadArduino(ref int move, ref bool accept)
    {
        ArduinoSerial serial = ArduinoSerial.Instance;
        if (serial == null || !serial.IsConnected) return;
        if (arduino == null) arduino = FindAnyObjectByType<ArduinoController>();
        if (arduino == null) return;
        if (arduino.MenuIsOpen) return; // con el menú de pausa abierto, los pedales mueven el menú, no el carro

        float left = swapPedals ? arduino.throttle.value : arduino.brake.value;
        float right = swapPedals ? arduino.brake.value : arduino.throttle.value;
        float button = serial.GetRaw(arduino.cycleButtonIndex) > 0 ? 1f : 0f;

        if (Pressed(left, ref leftDown)) move = -1;
        if (Pressed(right, ref rightDown)) move = 1;
        if (Pressed(button, ref buttonDown)) accept = true;
    }

    void ReadGamepad(ref int move, ref bool accept)
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;
        if (pauseMenu != null && pauseMenu.IsOpen) return;

        float x = pad.leftStick.x.ReadValue();
        bool left = x < -0.6f, right = x > 0.6f;
        if (pad.dpad.left.wasPressedThisFrame || (left && !padStickLeftDown)) move = -1;
        if (pad.dpad.right.wasPressedThisFrame || (right && !padStickRightDown)) move = 1;
        padStickLeftDown = left;
        padStickRightDown = right;

        if (pad.buttonSouth.wasPressedThisFrame) accept = true; // A / Cruz
    }

    // Detecta el momento en que se pisa (una vez por pisada, no mientras se mantiene).
    bool Pressed(float value, ref bool isDown)
    {
        if (!isDown && value > pressAt) { isDown = true; return true; }
        if (isDown && value < releaseAt) isDown = false;
        return false;
    }

    void Show()
    {
        ActivateCar(index); // enciende el carro elegido (con SU VehiclePawn y SU cámara) y apaga los demás
        ShowText();
    }

    void ActivateCar(int i)
    {
        if (cars == null || cars.Length == 0) return;
        i = Mathf.Clamp(i, 0, cars.Length - 1);

        // Pose del carro que se estaba viendo, para que el nuevo salga en el mismo lugar.
        GameObject previous = shownCar >= 0 && shownCar < cars.Length ? cars[shownCar] : null;
        Vector3 pos = default; Quaternion rot = default;
        bool hasPose = keepPose && previous != null;
        if (hasPose) { pos = previous.transform.position; rot = previous.transform.rotation; }

        for (int j = 0; j < cars.Length; j++)
            if (cars[j] != null && j != i) cars[j].SetActive(false);

        GameObject chosen = cars[i];
        if (chosen != null)
        {
            if (hasPose && chosen != previous)
            {
                chosen.transform.SetPositionAndRotation(pos, rot);
                Rigidbody rb = chosen.GetComponent<Rigidbody>();
                if (rb != null) { rb.position = pos; rb.rotation = rot; }
            }
            chosen.SetActive(true);
        }
        shownCar = i;
    }

    // Solo el texto: se reescribe también cuando el jugador cambia de dispositivo.
    void ShowText()
    {
        shownScheme = InputScheme.Current;
        shownBrand = InputScheme.Brand;
        CarInfo p = cars.Length > 0 && cars[index] != null ? cars[index].GetComponent<CarInfo>() : null;
        if (p == null)
        {
            if (infoText != null) infoText.text = $"Carro {index + 1}/{cars.Length}\n(falta el componente CarInfo)";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<size=130%>«  {p.carName}  »</size>   ({index + 1}/{cars.Length})");
        sb.AppendLine(p.description);
        sb.AppendLine();
        sb.AppendLine($"Velocidad     {Bar(p.speedStars)}");
        sb.AppendLine($"Aceleración   {Bar(p.accelerationStars)}");
        sb.AppendLine($"Control       {Bar(p.controlStars)}");
        sb.AppendLine();
        sb.AppendLine($"{Cap(ControlText.Name(GameAction.PrevCar))}: anterior  ·  {Cap(ControlText.Name(GameAction.NextCar))}: siguiente  ·  {Cap(ControlText.Name(GameAction.Confirm))}: elegir");
        if (infoText != null) infoText.text = sb.ToString();
    }

    // Barras con "|" (así no depende de símbolos que la fuente de TMP podría no tener).
    string Bar(int n)
    {
        return "<color=#FFD54A>" + new string('|', n) + "</color><color=#555555>" + new string('|', 5 - n) + "</color>";
    }

    void Accept()
    {
        CarSelection.Index = index;
        if (panel != null) panel.SetActive(false);
        frozen = false;
        active = false;
        lastAcceptFrame = Time.frameCount;
        Time.timeScale = 1f;
        enabled = false;
    }

    // "el pedal de freno" → "El pedal de freno" (empieza la frase).
    static string Cap(string s)
    {
        return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
