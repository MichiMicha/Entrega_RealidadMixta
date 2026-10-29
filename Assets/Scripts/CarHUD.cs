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
        {
            speedText.text = $"{car.SpeedKmh:F0} km/h";
            if (car.IsDrifting) speedText.text += "\nDERRAPE";
            if (!car.IsGrounded) speedText.text += "\nEN EL AIRE";
        }

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
