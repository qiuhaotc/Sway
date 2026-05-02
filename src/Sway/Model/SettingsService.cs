using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Sway;

public class SettingsService
{
    private readonly string _jsonFilePath;
    private SwaySettings _current;

    public SettingsService(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
        _current = Load();
    }

    public SwaySettings Current => _current;

    const string SettingsSectionName = "SwaySettings";

    public SwaySettings Load()
    {
        if (!File.Exists(_jsonFilePath))
        {
            return new SwaySettings
            {
                RunAfterSeconds = 20,
                SwayLength = 1
            };
        }

        var config = new ConfigurationBuilder()
            .AddJsonFile(_jsonFilePath, optional: true, reloadOnChange: false)
            .Build();

        var settings = new SwaySettings();
        config.GetSection(SettingsSectionName).Bind(settings);

        if (settings.RunAfterSeconds < 2 || settings.RunAfterSeconds > 1000)
            settings.RunAfterSeconds = 20;
        if (settings.SwayLength < 0 || settings.SwayLength > 50)
            settings.SwayLength = 1;

        _current = settings;
        return settings;
    }

    public void Save(SwaySettings settings)
    {
        settings.RunAfterSeconds = Math.Clamp(settings.RunAfterSeconds, 2, 1000);
        settings.SwayLength = Math.Clamp(settings.SwayLength, 0, 50);

        _current = settings;

        var json = JsonSerializer.Serialize(
            new { SwaySettings = settings }, // property name matches SettingsSectionName
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(_jsonFilePath, json);
    }
}
