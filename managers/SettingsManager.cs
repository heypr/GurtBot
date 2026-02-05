using Newtonsoft.Json;

public class SettingsManager
{
    private const string SettingsPath = "../../../settings.json";
    private static int _maxWords = 20;
    private static int _chanceToTalk = 5;

    public static void LoadSettings()
    {
        if (File.Exists(SettingsPath))
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonConvert.DeserializeObject<SettingsData>(json);
            if (settings != null)
            {
                _maxWords = settings.MaxWords;
                _chanceToTalk = settings.ChanceToTalk;
            }
        }
    }

    public static async Task SaveSettings(int newMaxWords, int newChance)
    {
        _maxWords = newMaxWords;
        _chanceToTalk = newChance;
        var data = new SettingsData { MaxWords = _maxWords, ChanceToTalk = _chanceToTalk };
        await File.WriteAllTextAsync(SettingsPath, JsonConvert.SerializeObject(data, Formatting.Indented));
    }

    public static int GetMaxWords()
    {
        return _maxWords;
    }

    public static int GetChanceToTalk()
    {
        return _chanceToTalk;
    }

    private class SettingsData
    {
        public int MaxWords { get; set; }
        public int ChanceToTalk { get; set; }
    }
}
