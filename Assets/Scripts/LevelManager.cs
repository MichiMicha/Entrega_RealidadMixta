using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Un LevelManager por escena de juego (Tutorial y Juego).
// Cuenta solo cuántas frutas hay en la escena y cambia de escena al atrapar todas.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private string nextScene = "3_Juego";
    [SerializeField] private float delayBeforeNextScene = 2f;
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string winMessage = "¡Atrapaste todas las frutas!";

    private int total;
    private int collected;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        total = FindObjectsByType<Fruit>(FindObjectsSortMode.None).Length;
        UpdateUI();
    }

    public void FruitCollected()
    {
        collected++;
        UpdateUI();

        if (collected >= total)
        {
            if (messageText != null) messageText.text = winMessage;
            Invoke(nameof(LoadNext), delayBeforeNextScene);
        }
    }

    private void UpdateUI()
    {
        if (counterText != null)
            counterText.text = $"Frutas: {collected}/{total}";
    }

    private void LoadNext()
    {
        SceneManager.LoadScene(nextScene);
    }
}
