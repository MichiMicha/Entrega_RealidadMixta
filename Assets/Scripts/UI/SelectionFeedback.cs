using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Resalta de forma bien visible el botón (o slider) que está seleccionado, para que quien juega con mando,
/// teclado o volante Arduino sepa en qué opción está parado. Muestra:
///   - un borde amarillo alrededor,
///   - el botón crece un poco (solo botones),
///   - y, si el color de "Selected" del botón era casi igual al normal (como en los botones por defecto
///     de Unity), le pone un tinte amarillo suave.
/// Además, al pasar el mouse por encima también se selecciona, así nunca hay dos opciones resaltadas a la vez.
///
/// NO hay que ponerlo en ningún objeto: el SelectionFeedbackInstaller de abajo se lo agrega solo a todos los
/// Button / Slider / Toggle... de cada escena al cargarla (también a los de paneles ocultos, como el menú de pausa).
/// </summary>
[DisallowMultipleComponent]
public class SelectionFeedback : MonoBehaviour, IPointerEnterHandler
{
    // ---- Ajusta aquí el estilo ----
    static readonly Color HighlightColor = new Color(1f, 0.84f, 0.29f, 1f); // amarillo #FFD54A
    const float SelectedScale = 1.08f;   // cuánto crece el botón seleccionado (1 = no crece)
    const float OutlineSize = 1f;        // grosor del borde en píxeles
    const float ScaleSpeed = 1f;        // qué tan rápido crece/encoge

    private Selectable selectable;
    private Outline outline;
    private Vector3 baseScale;
    private bool isButton;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        isButton = selectable is Button;
        baseScale = transform.localScale;

        // Borde: se crea apagado y se enciende solo cuando está seleccionado.
        Graphic graphic = selectable.targetGraphic;
        if (graphic != null)
        {
            outline = graphic.GetComponent<Outline>();
            if (outline == null) outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = HighlightColor;
            outline.effectDistance = new Vector2(OutlineSize, -OutlineSize);
            outline.useGraphicAlpha = false; // borde sólido aunque el botón sea translúcido
            outline.enabled = false;
        }

        // Si "Selected" casi no se distingue de "Normal", le damos un tinte amarillo suave.
        if (selectable.transition == Selectable.Transition.ColorTint)
        {
            ColorBlock colors = selectable.colors;
            Color n = colors.normalColor, s = colors.selectedColor;
            float difference = Mathf.Abs(n.r - s.r) + Mathf.Abs(n.g - s.g) + Mathf.Abs(n.b - s.b) + Mathf.Abs(n.a - s.a);
            if (difference < 0.2f)
            {
                colors.selectedColor = Color.Lerp(n, HighlightColor, 0.6f);
                selectable.colors = colors;
            }
        }
    }

    void Update()
    {
        bool isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;

        if (outline != null && outline.enabled != isSelected) outline.enabled = isSelected;

        if (isButton)
        {
            // Tiempo real: en el menú de pausa Time.timeScale vale 0.
            Vector3 target = baseScale * (isSelected ? SelectedScale : 1f);
            float t = 1f - Mathf.Exp(-ScaleSpeed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, target, t);
        }
    }

    void OnDisable()
    {
        // Al ocultarse el panel, que no quede crecido ni con borde.
        transform.localScale = baseScale;
        if (outline != null) outline.enabled = false;
    }

    // Mouse encima = seleccionar, para que mouse y mando compartan el mismo resaltado.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && selectable.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }
}

/// <summary>Le agrega SelectionFeedback a todos los elementos de menú de cada escena, sin tocar las escenas.</summary>
public static class SelectionFeedbackInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Install(); // escenas siguientes
        Install();                                              // la primera escena
    }

    private static void Install()
    {
        // Include: también los de paneles apagados (menú de pausa, opciones...).
        foreach (Selectable item in Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.GetComponent<SelectionFeedback>() == null)
                item.gameObject.AddComponent<SelectionFeedback>();
        }
    }
}
