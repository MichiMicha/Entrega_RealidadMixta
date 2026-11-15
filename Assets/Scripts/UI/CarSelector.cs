using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Pantalla para elegir carro al empezar el tutorial (el juego queda en pausa mientras tanto).
///   Pedal izquierdo (freno)     = carro anterior
///   Pedal derecho (acelerador)  = carro siguiente
///   Botón del centro (CICLO)    = aceptar
/// Teclado para probar sin Arduino: flechas ← → y Enter.
/// </summary>
public class CarSelector : MonoBehaviour
{
    [SerializeField] private CarProfileApplier applier;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text infoText;

    [Header("Pedales")]
    [Tooltip("Qué tanto hay que pisar el pedal (0..1) para contar como pulsación.")]
    [SerializeField] private float pressAt = 0.6f;
    [Tooltip("El pedal debe bajar de aquí para poder contar otra vez.")]
    [SerializeField] private float releaseAt = 0.3f;
    [Tooltip("Actívalo si tus pedales quedan al revés (izquierdo = siguiente).")]
    [SerializeField] private bool swapPedals = false;

    private ArduinoController arduino;
    private PauseMenu pauseMenu;
    private int index;
    private bool frozen;
    // true al inicio: primero hay que soltar pedales y botón antes de que cuenten.
    private bool leftDown = true, rightDown = true, buttonDown = true;

    void Start()
    {
        pauseMenu = FindAnyObjectByType<PauseMenu>();
        index = Mathf.Clamp(CarSelection.Index, 0, applier.Count - 1);
        if (panel != null) panel.SetActive(true);
        frozen = true;
        Time.timeScale = 0f; // el juego espera a que elijas
        Show();
    }

    void OnDisable()
    {
        // Si se cambia de escena en plena elección, que la siguiente no arranque congelada.
        if (frozen) Time.timeScale = 1f;
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

        ReadArduino(ref move, ref accept);

        if (move != 0)
        {
            index = (index + move + applier.Count) % applier.Count;
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

        float left = swapPedals ? arduino.throttle.value : arduino.brake.value;
        float right = swapPedals ? arduino.brake.value : arduino.throttle.value;
        float button = serial.GetRaw(arduino.cycleButtonIndex) > 0 ? 1f : 0f;

        if (Pressed(left, ref leftDown)) move = -1;
        if (Pressed(right, ref rightDown)) move = 1;
        if (Pressed(button, ref buttonDown)) accept = true;
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
        applier.Apply(index); // cambia el modelo y los números del carro al instante
        CarProfileApplier.Profile p = applier.Get(index);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<size=130%>«  {p.name}  »</size>   ({index + 1}/{applier.Count})");
        sb.AppendLine(p.description);
        sb.AppendLine();
        sb.AppendLine($"Velocidad     {Bar(p.speedStars)}");
        sb.AppendLine($"Aceleración   {Bar(p.accelerationStars)}");
        sb.AppendLine($"Control       {Bar(p.controlStars)}");
        sb.AppendLine();
        sb.AppendLine("Pedal izquierdo: anterior  ·  Pedal derecho: siguiente  ·  Botón del centro: elegir");
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
        Time.timeScale = 1f;
        enabled = false;
    }
}
