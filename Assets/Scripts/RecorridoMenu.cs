using UnityEngine;

public class RecorridoMenu : MonoBehaviour
{
    public Transform[] puntos;      // puntos por donde pasa la cámara, en orden
    public float velocidad = 8f;    // unidades por segundo
    public bool mirarAdelante = true;

    float t;                        // progreso total (0 a puntos.Length)

    void Start()
    {
        if (puntos.Length < 4)
        {
            Debug.LogError("Pon al menos 4 puntos");
            enabled = false;
        }
    }

    void Update()
    {
        int n = puntos.Length;

        // Avance a velocidad constante aproximada
        int i = Mathf.FloorToInt(t) % n;
        float distTramo = Vector3.Distance(puntos[i].position, puntos[(i + 1) % n].position);
        t += (velocidad / Mathf.Max(distTramo, 0.01f)) * Time.deltaTime;
        if (t >= n) t -= n;

        i = Mathf.FloorToInt(t) % n;
        float f = t - Mathf.Floor(t);

        Vector3 p0 = puntos[(i - 1 + n) % n].position;
        Vector3 p1 = puntos[i].position;
        Vector3 p2 = puntos[(i + 1) % n].position;
        Vector3 p3 = puntos[(i + 2) % n].position;

        Vector3 pos = CatmullRom(p0, p1, p2, p3, f);
        transform.position = pos;

        if (mirarAdelante)
        {
            Vector3 adelante = CatmullRom(p0, p1, p2, p3, Mathf.Min(f + 0.05f, 1f)) - pos;
            if (adelante.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(adelante),
                    5f * Time.deltaTime);
            }
        }
    }

    static Vector3 CatmullRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * b) +
            (-a + c) * t +
            (2f * a - 5f * b + 4f * c - d) * t2 +
            (-a + 3f * b - 3f * c + d) * t3);
    }

    // Dibuja la ruta en la ventana Scene para que veas el recorrido
    void OnDrawGizmos()
    {
        if (puntos == null) return;
        Gizmos.color = Color.yellow;
        for (int k = 0; k < puntos.Length; k++)
        {
            if (puntos[k] != null && puntos[(k + 1) % puntos.Length] != null)
            {
                Gizmos.DrawLine(puntos[k].position, puntos[(k + 1) % puntos.Length].position);
            }
        }
    }
}