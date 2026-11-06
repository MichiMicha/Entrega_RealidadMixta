using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Para las escenas de menú (1_Inicio y 4_Final). Los botones llaman a LoadScene / QuitGame desde el OnClick.
// Siempre deja un botón seleccionado: así el volante (ArduinoController del profe) puede navegar
// el menú: toque corto en CICLO = siguiente botón, mantener = presionarlo.
public class SceneLoader : MonoBehaviour
{
    [Tooltip("Botón que queda seleccionado al abrir la escena. Vacío = el primero que encuentre.")]
    [SerializeField] private Selectable firstSelected;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SelectFirst();
    }

    void Update()
    {
        // Si se perdió la selección (ej: clic en el vacío), volver a seleccionar algo.
        if (EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy) SelectFirst();
    }

    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
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

    private void SelectFirst()
    {
        if (EventSystem.current == null) return;
        Selectable target = firstSelected != null ? firstSelected : FindAnyObjectByType<Selectable>();
        EventSystem.current.SetSelectedGameObject(target != null ? target.gameObject : null);
    }
}
