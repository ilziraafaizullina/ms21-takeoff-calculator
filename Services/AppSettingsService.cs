using System.IO;
using System.Text.Json;

namespace MS21TakeoffCalculator.Services;

public sealed class AppSettingsService
{
    private const string SettingsFileName = "appsettings.json";

    public AppSettings Load()
    {
        var path = ResolveSettingsPath();
        if (path is null)
        {
            return new AppSettings();
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<AppSettings>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AppSettings();
    }

    private static string? ResolveSettingsPath()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
        if (File.Exists(basePath))
        {
            return basePath;
        }

        var currentPath = Path.Combine(Environment.CurrentDirectory, SettingsFileName);
        return File.Exists(currentPath) ? currentPath : null;
    }
}
