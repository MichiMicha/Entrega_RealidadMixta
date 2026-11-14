using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Va en la última escena (4_Final). Escribe la lista de tiempos en un solo texto:
///     #   Jugador     Tiempo
///     1.  Jugador 2   01:12.30
///     2.  Jugador 1   01:45.10   (tú)
/// Ordenada de la partida más rápida a la más lenta. La partida que acaba de terminar sale resaltada.
/// </summary>
public class ScoreBoardUI : MonoBehaviour
{
    [Tooltip("El texto donde se escribe la lista. Alineación: izquierda; Wrapping: desactivado.")]
    [SerializeField] private TMP_Text listText;

    [Tooltip("Opcional: un texto aparte con 'Tu tiempo: ...'.")]
    [SerializeField] private TMP_Text yourTimeText;

    [Tooltip("Cuántas filas se muestran como máximo.")]
    [SerializeField] private int maxRows = 8;

    [Tooltip("Color de la fila de la partida actual.")]
    [SerializeField] private string highlightColor = "#FFD54A";

    [Tooltip("Color de la fila de títulos.")]
    [SerializeField] private string headerColor = "#B0B0B0";

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        List<ScoreEntry> sorted = ScoreBoard.GetSorted();
        ScoreEntry mine = ScoreBoard.LastEntry;

        if (yourTimeText != null)
            yourTimeText.text = mine != null ? $"Tu tiempo ({mine.name}): {ScoreBoard.FormatTime(mine.time)}" : "";

        if (listText == null) return;

        if (sorted.Count == 0)
        {
            listText.text = "Aún no hay partidas.";
            return;
        }

        // Filas a mostrar: las primeras maxRows. Si la partida actual quedó más abajo,
        // ocupa la última fila (con su puesto real) para que siempre se vea.
        int shown = Mathf.Min(Mathf.Max(1, maxRows), sorted.Count);
        int myIndex = mine != null ? sorted.IndexOf(mine) : -1;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color={headerColor}>#<pos=14%>Jugador<pos=68%>Tiempo</color>");

        for (int row = 0; row < shown; row++)
        {
            int i = (row == shown - 1 && myIndex >= shown) ? myIndex : row;
            ScoreEntry e = sorted[i];
            bool isMine = i == myIndex;

            string line = $"{i + 1}.<pos=14%>{e.name}<pos=68%>{ScoreBoard.FormatTime(e.time)}";
            if (isMine) line = $"<color={highlightColor}>{line}  (tú)</color>";
            sb.AppendLine(line);
        }

        listText.text = sb.ToString();
    }

    /// <summary>Para un botón opcional "Borrar ranking".</summary>
    public void ClearRanking()
    {
        ScoreBoard.Clear();
        Refresh();
    }
}
