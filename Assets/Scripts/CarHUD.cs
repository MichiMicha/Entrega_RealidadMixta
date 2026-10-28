using UnityEngine;
using TMPro;

public class CarHUD : MonoBehaviour
{
    [SerializeField] private CarController car;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text rawValuesText; // opcional

    void Update()
    {
        if (speedText != null && car != null)
            speedText.text = $"{Mathf.Abs(car.CurrentSpeed):F1} m/s";

        if (rawValuesText != null)
        {
            var reader = ArduinoSerialReader.Instance;
            if (reader != null && reader.IsConnected)
            {
                reader.GetState(out float steer, out bool acc, out bool rev, out bool ok);
                rawValuesText.text =
                    $"Volante: conectado ({reader.ConnectedPort})\n" +
                    $"Dirección: {steer:F2}\nAcelerar: {acc}  Retroceder: {rev}  Confirmar: {ok}";
            }
            else
            {
                rawValuesText.text = "Volante: NO conectado (usa el teclado)";
            }
        }
    }
}
