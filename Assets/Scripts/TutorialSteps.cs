using UnityEngine;
using TMPro;

// Muestra los textos del tutorial uno por uno. Se avanza con el botón CONFIRMAR.
// Durante los textos el carro está bloqueado; al terminar, puede moverse.
public class TutorialSteps : MonoBehaviour
{
    [SerializeField] private CarController car;
    [SerializeField] private TMP_Text stepText;

    [TextArea(2, 5)]
    [SerializeField] private string[] steps =
    {
        "¡Bienvenido! Eres un carro y unas frutas se escaparon. ¡Debes atraparlas!\n\n(Presiona CONFIRMAR para continuar)",
        "Gira el volante para DIRIGIR el carro a la izquierda o a la derecha.\n\n(CONFIRMAR para continuar)",
        "Presiona el botón ACELERAR para avanzar y el botón RETROCEDER para ir hacia atrás.\n\n(CONFIRMAR para continuar)",
        "El botón CONFIRMAR sirve para avanzar en los textos y los menús.\n\n(CONFIRMAR para continuar)"
    };
    [TextArea(2, 5)]
    [SerializeField] private string finalMessage = "¡Ahora inténtalo! Atrapa la fruta que está en el escenario.";

    private int index;
    private bool finished;

    void Start()
    {
        if (car != null) car.enabled = false;
        ShowStep();
    }

    void Update()
    {
        if (finished) return;

        if (GameInput.ConfirmPressed())
        {
            index++;
            if (index >= steps.Length) Finish();
            else ShowStep();
        }
    }

    private void ShowStep()
    {
        if (stepText != null) stepText.text = steps[index];
    }

    private void Finish()
    {
        finished = true;
        if (car != null) car.enabled = true;
        if (stepText != null) stepText.text = finalMessage;
    }
}
