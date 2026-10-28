using UnityEngine;
using UnityEngine.SceneManagement;

// Pon este script en un objeto vacío de cada escena de menú.
// Los botones llaman a LoadScene / QuitGame desde el OnClick.
public class SceneLoader : MonoBehaviour
{
    [Header("Botón CONFIRMAR del volante (opcional)")]
    [SerializeField] private string sceneOnConfirm = "";
    [SerializeField] private bool quitOnConfirm = false;

    void Update()
    {
        if (!GameInput.ConfirmPressed()) return;

        if (quitOnConfirm) QuitGame();
        else if (!string.IsNullOrEmpty(sceneOnConfirm)) LoadScene(sceneOnConfirm);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
