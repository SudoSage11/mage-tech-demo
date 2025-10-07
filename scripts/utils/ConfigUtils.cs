using System;
using Godot;
using Newtonsoft.Json;

namespace Config
{
  public class ConfigSettings
  {
    private ConfigSettings() { }

    private static Lazy<ConfigSettings> lazy = new(() => new ConfigSettings());

    public static ConfigSettings Instance { get { return lazy.Value; } }

    public bool DrawGeneratedMeshes { get; set; } = true;
    public bool DrawDebugPoints { get; set; } = false;
    public bool DrawDebugLines { get; set; } = false;
  }

  public static class ConfigManager
  {
    public static ConfigSettings? latestConfig;

    public static ConfigSettings? GetConfig()
    {
      try
      {
        using var saveFile = FileAccess.Open("user://config.cfg", FileAccess.ModeFlags.Read);

        string jsonString = saveFile.GetLine();

        ConfigSettings? config = JsonConvert.DeserializeObject<ConfigSettings?>(jsonString);

        latestConfig = config;
        return config;
      }
      catch (Exception ex)
      {
        GD.Print($"Failed to get config: {ex}");
        return null;
      }
    }

    public static bool SaveConfig(ConfigSettings config)
    {
      try
      {
        using var saveFile = FileAccess.Open("user://config.cfg", FileAccess.ModeFlags.Write);

        string configJson = JsonConvert.SerializeObject(config);

        saveFile.StoreLine(configJson);

        latestConfig = config;
        return true;
      }
      catch (Exception ex)
      {
        GD.Print($"Failed to save config: {ex}");
        return false;
      }
    }
  }
}
