using System.Text.Json.Serialization;

namespace PMM_WeaponPaints.Config;

/*
  wp_triggers.json from CSCreatePanorama. It sits next to the dll and says
  what each button id of a custom layout does.
*/
public sealed class TriggerFile
{
    [JsonPropertyName("version")] public int Version { get; set; }

    [JsonPropertyName("layout")] public string? Layout { get; set; }

    [JsonPropertyName("buttons")] public Dictionary<string, string>? Buttons { get; set; }

    [JsonPropertyName("groups")] public Dictionary<string, TriggerGroupFile>? Groups { get; set; }
}

public sealed class TriggerGroupFile
{
    [JsonPropertyName("title")] public string? Title { get; set; }

    [JsonPropertyName("items")] public string[]? Items { get; set; }
}
