/// <summary>
/// Recuerda qué carro se eligió en el tutorial para que 3_Juego use el mismo.
/// Es estática: vive mientras el juego esté abierto, sin ponerla en ningún objeto.
/// 0 = Carreras, 1 = Común, 2 = Achatado, 3 = Minivan. Por defecto, el Común.
/// </summary>
public static class CarSelection
{
    public static int Index = 1;
}
