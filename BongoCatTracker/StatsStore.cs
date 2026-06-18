using System.IO;
using System.Text.Json;

namespace BongoCatTracker;

public static class StatsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BongoCatTracker");

    public static string StatsPath => Path.Combine(AppDataDirectory, "stats.json");

    public static InputStats Load()
    {
        try
        {
            if (!File.Exists(StatsPath))
            {
                return new InputStats();
            }

            return JsonSerializer.Deserialize<InputStats>(File.ReadAllText(StatsPath)) ?? new InputStats();
        }
        catch
        {
            return new InputStats();
        }
    }

    public static void Save(InputStats stats)
    {
        Directory.CreateDirectory(AppDataDirectory);
        File.WriteAllText(StatsPath, JsonSerializer.Serialize(stats, JsonOptions));
    }
}
