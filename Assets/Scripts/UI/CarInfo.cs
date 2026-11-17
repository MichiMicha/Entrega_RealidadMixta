using UnityEngine;

/// <summary>
/// Datos que el selector muestra en pantalla para ESTE carro (nombre, descripción y estrellas).
/// Va en la raíz de cada carro, junto a su propio VehiclePawn. No cambia ningún número de manejo:
/// los valores de motor, agarre, cámaras, etc. se ajustan en el VehiclePawn de cada carro.
/// </summary>
public class CarInfo : MonoBehaviour
{
    public string carName = "Carro";
    [TextArea(1, 3)] public string description;

    [Header("Solo para mostrar en pantalla (1-5)")]
    [Range(1, 5)] public int speedStars = 3;
    [Range(1, 5)] public int accelerationStars = 3;
    [Range(1, 5)] public int controlStars = 3;
}
