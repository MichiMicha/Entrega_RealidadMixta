using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class ScoreEntry
{
    public string name;   // "Jugador 3"
    public float time;    // segundos que tardó en el juego principal
    public string date;
}

[Serializable]
public class ScoreData
{
    public int nextPlayer = 1;
    public List<ScoreEntry> entries = new List<ScoreEntry>();
}

/// <summary>
/// Guarda los tiempos de todas las partidas en un archivo (ranking.json, dentro de
/// Application.persistentDataPath), así la lista sigue ahí aunque se cierre el juego.
/// Es estática: se puede usar desde cualquier escena sin ponerla en ningún objeto.
/// </summary>
public static class ScoreBoard
{
    private const string PlayerPrefix = "Jugador";

    private static ScoreData data;

    /// <summary>La partida que acaba de terminar (null si todavía no se ha jugado ninguna).</summary>
    public static ScoreEntry LastEntry { get; private set; }

    private static string FilePath => Path.Combine(Application.persistentDataPath, "ranking.json");

    private static ScoreData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                data = JsonUtility.FromJson<ScoreData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ScoreBoard] No se pudo leer el ranking: {e.Message}");
        }

        if (data == null) data = new ScoreData();
        if (data.entries == null) data.entries = new List<ScoreEntry>();
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ScoreBoard] No se pudo guardar el ranking: {e.Message}");
        }
    }

    /// <summary>Registra una partida nueva con el siguiente nombre ("Jugador 1", "Jugador 2"...).</summary>
    public static ScoreEntry AddRun(float seconds)
    {
        ScoreEntry entry = new ScoreEntry
        {
            name = $"{PlayerPrefix} {Data.nextPlayer}",
            time = seconds,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
        Data.nextPlayer++;
        Data.entries.Add(entry);
        Save();

        LastEntry = entry;
        return entry;
    }

    /// <summary>Todas las partidas, de la más rápida a la más lenta.</summary>
    public static List<ScoreEntry> GetSorted()
    {
        return Data.entries.OrderBy(e => e.time).ToList();
    }

    /// <summary>Olvida cuál fue "la última partida" (se llama al empezar una nueva).</summary>
    public static void ForgetLast()
    {
        LastEntry = null;
    }

    /// <summary>Borra todo el ranking.</summary>
    public static void Clear()
    {
        data = new ScoreData();
        LastEntry = null;
        Save();
    }

    /// <summary>Segundos → "mm:ss.cc" (ej: 83.456 → "01:23.45").</summary>
    public static string FormatTime(float seconds)
    {
        int centiseconds = Mathf.FloorToInt(Mathf.Max(0f, seconds) * 100f);
        int minutes = centiseconds / 6000;
        int rest = centiseconds % 6000;
        return $"{minutes:00}:{rest / 100:00}.{rest % 100:00}";
    }
}
