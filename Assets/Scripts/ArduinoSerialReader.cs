using UnityEngine;
using System.IO.Ports;
using System.Threading;

// Lee del Arduino líneas con este formato:  direccion,acelerar,retroceder,confirmar
// Ejemplo:  512,1,0,0   (direccion 0-1023, botones 0 o 1)
public class ArduinoSerialReader : MonoBehaviour
{
    public static ArduinoSerialReader Instance { get; private set; }

    [Header("Serial Configuration")]
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int readTimeoutMs = 100;
    [SerializeField] private bool autoConnectOnStart = true;

    [Header("Auto-Detect Settings")]
    [Tooltip("Si está activo, ignora portName y busca el Arduino solo.")]
    [SerializeField] private bool autoDetectPort = true;
    [SerializeField] private float reconnectInterval = 3f;
    [Tooltip("El Arduino tarda ~2 s en reiniciarse al abrir el puerto. No lo bajes mucho.")]
    [SerializeField] private int scanTimeoutMs = 2500;

    public bool IsConnected { get; private set; }
    public string ConnectedPort { get; private set; }

    // Valores crudos (para mostrar en el HUD)
    public int RawSteering { get; private set; }
    public bool Accelerate { get; private set; }
    public bool Reverse { get; private set; }
    public bool Confirm { get; private set; }

    public System.Action<string> OnConnected;
    public System.Action OnDisconnected;

    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool running;
    private volatile bool scanning;
    private readonly object dataLock = new object();

    private float reconnectTimer;
    private bool intentionalDisconnect;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (autoConnectOnStart) Connect();
    }

    void Update()
    {
        // Intenta reconectar cada cierto tiempo si no hay conexión
        if (!IsConnected && !intentionalDisconnect && !scanning)
        {
            reconnectTimer -= Time.deltaTime;
            if (reconnectTimer <= 0f)
            {
                reconnectTimer = reconnectInterval;
                AttemptConnect();
            }
        }
    }

    public void Connect()
    {
        intentionalDisconnect = false;
        AttemptConnect();
    }

    private void AttemptConnect()
    {
        if (autoDetectPort || string.IsNullOrEmpty(portName))
            TryAutoConnect();
        else
            ConnectTo(portName);
    }

    public void ConnectTo(string port)
    {
        if (IsConnected) Disconnect(intentional: false);
        intentionalDisconnect = false;

        try
        {
            serialPort = new SerialPort(port, baudRate)
            {
                ReadTimeout = readTimeoutMs,
                NewLine = "\n"
            };
            serialPort.Open();

            running = true;
            readThread = new Thread(ReadLoop) { IsBackground = true };
            readThread.Start();

            IsConnected = true;
            ConnectedPort = port;

            Debug.Log($"[Arduino] Conectado a {port}");
            OnConnected?.Invoke(port);
        }
        catch (System.Exception e)
        {
            IsConnected = false;
            Debug.LogWarning($"[Arduino] No se pudo abrir {port}: {e.Message}");
            CleanupSerial();
        }
    }

    public void Disconnect() => Disconnect(intentional: true);

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

    // Devuelve la dirección normalizada (-1 a 1) y el estado de los botones
    public void GetState(out float steering, out bool accelerate, out bool reverse, out bool confirm)
    {
        int raw;
        lock (dataLock)
        {
            raw = RawSteering;
            accelerate = Accelerate;
            reverse = Reverse;
            confirm = Confirm;
        }
        steering = Mathf.Clamp((raw - 512) / 512f, -1f, 1f);
    }

    // ---------- Búsqueda automática (en otro hilo para no congelar el juego) ----------
    private void TryAutoConnect()
    {
        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0)
        {
            Debug.Log("[Arduino] No hay puertos serial disponibles.");
            return;
        }

        scanning = true;
        Debug.Log($"[Arduino] Buscando en {ports.Length} puerto(s): {string.Join(", ", ports)}");

        var scanThread = new Thread(() =>
        {
            string found = null;
            foreach (string port in ports)
            {
                if (TryHandshake(port)) { found = port; break; }
            }
            scanning = false;

            if (found != null)
                UnityMainThreadDispatcher.Enqueue(() => ConnectTo(found));
            else
                Debug.Log("[Arduino] No se encontró el Arduino. Reintentando...");
        })
        { IsBackground = true };
        scanThread.Start();
    }

    private bool TryHandshake(string port)
    {
        SerialPort probe = null;
        try
        {
            probe = new SerialPort(port, baudRate)
            {
                ReadTimeout = 500,
                NewLine = "\n"
            };
            probe.Open();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < scanTimeoutMs)
            {
                try
                {
                    string line = probe.ReadLine();
                    if (IsValidArduinoLine(line))
                    {
                        Debug.Log($"[Arduino] Dispositivo encontrado en {port}");
                        return true;
                    }
                }
                catch (System.TimeoutException) { }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"[Arduino] Saltando {port}: {e.Message}");
        }
        finally
        {
            try { probe?.Close(); } catch { }
        }
        return false;
    }

    private bool IsValidArduinoLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        string[] p = line.Trim().Split(',');
        return p.Length == 4
            && int.TryParse(p[0], out _)
            && int.TryParse(p[1], out _)
            && int.TryParse(p[2], out _)
            && int.TryParse(p[3], out _);
    }

    // ---------- Hilo de lectura ----------
    private void ReadLoop()
    {
        while (running)
        {
            try
            {
                string line = serialPort.ReadLine();
                ParseLine(line);
            }
            catch (System.TimeoutException)
            {
                // Normal: no llegaron datos todavía
            }
            catch (System.Exception e)
            {
                if (!running) break; // se está cerrando a propósito

                Debug.LogWarning($"[Arduino] Error de lectura: {e.Message}");
                running = false;
                IsConnected = false;

                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    CleanupSerial();
                    ConnectedPort = null;
                    OnDisconnected?.Invoke();
                    Debug.Log("[Arduino] Dispositivo desconectado. Se intentará reconectar...");
                });
            }
        }
    }

    private void ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        string[] p = line.Trim().Split(',');
        if (p.Length != 4) return;

        if (int.TryParse(p[0], out int steer) &&
            int.TryParse(p[1], out int acc) &&
            int.TryParse(p[2], out int rev) &&
            int.TryParse(p[3], out int ok))
        {
            lock (dataLock)
            {
                RawSteering = steer;
                Accelerate = acc == 1;
                Reverse = rev == 1;
                Confirm = ok == 1;
            }
        }
    }

    private void Disconnect(bool intentional)
    {
        intentionalDisconnect = intentional;
        running = false;

        if (readThread != null && readThread.IsAlive)
            readThread.Join(500);

        CleanupSerial();
        IsConnected = false;
        ConnectedPort = null;

        OnDisconnected?.Invoke();
        Debug.Log($"[Arduino] Desconectado (intencional: {intentional}).");
    }

    private void CleanupSerial()
    {
        try { if (serialPort != null && serialPort.IsOpen) serialPort.Close(); }
        catch { }
        serialPort = null;
    }

    void OnApplicationQuit() => Disconnect(intentional: true);

    void OnDestroy()
    {
        if (Instance == this)
        {
            Disconnect(intentional: true);
            Instance = null;
        }
    }
}
