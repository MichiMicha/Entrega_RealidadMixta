using UnityEngine;
using TMPro;

/// <summary>
/// Cronómetro del juego principal (escena 3_Juego; NO va en el tutorial).
///   - Empieza a correr cuando el jugador SE SUBE AL CARRO (o desde que carga la escena, si se desmarca
///     "Start When In Car").
///   - Se detiene en pausa: el menú de pausa pone Time.timeScale = 0, y aquí se suma Time.deltaTime,
///     que vale 0 mientras está pausado.
///   - Termina cuando el LevelManager dice que se atraparon todas las frutas, y guarda el tiempo
///     en el ranking (ScoreBoard) para mostrarlo en la última escena.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Tooltip("Texto donde se muestra el tiempo (en el Canvas del juego).")]
    [SerializeField] private TMP_Text timerText;

    [Tooltip("Cómo se escribe. {0} se reemplaza por el tiempo.")]
    [SerializeField] private string format = "Tiempo: {0}";

    [Tooltip("Activo: el tiempo empieza cuando el jugador se sube al carro. " +
             "Apagado: empieza al cargar la escena.")]
    [SerializeField] private bool startWhenInCar = true;

    [Tooltip("El carro. Si se deja vacío, lo busca solo.")]
    [SerializeField] private VehiclePawn vehicle;

    /// <summary>Segundos jugados (sin contar las pausas).</summary>
    public float Elapsed { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ScoreBoard.ForgetLast(); // empieza una partida nueva
        Elapsed = 0f;
        IsFinished = false;
        IsRunning = !startWhenInCar;
        UpdateText();
    }

    void Update()
    {
        if (IsFinished) return;

        // ¿Ya se atraparon todas las frutas? Entonces termina y guarda el tiempo.
        if (LevelManager.Instance != null && LevelManager.Instance.IsComplete)
        {
            Finish();
            return;
        }

        // Todavía no arranca: espera a que el jugador se suba al carro.
        if (!IsRunning)
        {
            // Se busca el VehiclePawn que SÍ está siendo conducido (el prefab del carro trae un duplicado apagado,
            // y tomar "el primero que aparezca" podía agarrar ese y el cronómetro nunca arrancaba).
            VehiclePawn driven = FindDrivenVehicle();
            if (driven != null)
            {
                vehicle = driven;
                IsRunning = true;
            }
            else return;
        }

        // En pausa no corre.
        if (Time.timeScale <= 0f) return;

        Elapsed += Time.deltaTime;
        UpdateText();
    }

    // Devuelve el VehiclePawn que tiene un controlador (el que se está conduciendo), o null si ninguno.
    private static VehiclePawn FindDrivenVehicle()
    {
        foreach (VehiclePawn v in FindObjectsByType<VehiclePawn>(FindObjectsSortMode.None))
            if (v.Controller != null) return v;
        return null;
    }

    public void StartTimer() => IsRunning = true;
    public void StopTimer() => IsRunning = false;

    private void Finish()
    {
        IsFinished = true;
        IsRunning = false;
        ScoreBoard.AddRun(Elapsed);
        UpdateText();
    }

    private void UpdateText()
    {
        if (timerText != null)
            timerText.text = string.Format(format, ScoreBoard.FormatTime(Elapsed));
    }
}
