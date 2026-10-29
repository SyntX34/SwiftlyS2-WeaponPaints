using System.Text.Json.Serialization;

namespace PMM_WeaponPaints.Catalog;

/* One agent of a side, with the model string WeaponPaints saves. */
public sealed class AgentEntry
{
    [JsonPropertyName("def")] public int Def { get; set; }

    // 2 for T, 3 for CT.
    [JsonPropertyName("team")] public int Team { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; } = "";

    [JsonPropertyName("en")] public string? En { get; set; }

    [JsonPropertyName("ru")] public string? Ru { get; set; }

    [JsonPropertyName("rar")] public int Rarity { get; set; }

    [JsonPropertyName("art")] public string? Art { get; set; }

    [JsonPropertyName("names")] public List<string>? Names { get; set; }
}

/* A music kit or a coin. Both only carry an id and a name. */
public sealed class NamedEntry
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("en")] public string? En { get; set; }

    [JsonPropertyName("ru")] public string? Ru { get; set; }

    [JsonPropertyName("rar")] public int Rarity { get; set; } = 3;

    [JsonPropertyName("art")] public string? Art { get; set; }

    [JsonPropertyName("names")] public List<string>? Names { get; set; }
}

/* One glove finish. The name is the full line the game shows. */
public sealed class GloveEntry
{
    [JsonPropertyName("def")] public int Def { get; set; }

    [JsonPropertyName("paint")] public int Paint { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

/* A glove family such as Sport Gloves, with the picture the home page uses. */
public sealed class GloveType
{
    [JsonPropertyName("def")] public int Def { get; set; }

    [JsonPropertyName("en")] public string? En { get; set; }

    [JsonPropertyName("ru")] public string? Ru { get; set; }

    [JsonPropertyName("art")] public string? Art { get; set; }
}

public sealed class ItemFile
{
    [JsonPropertyName("skinRarity")] public Dictionary<string, int>? SkinRarity { get; set; }

    [JsonPropertyName("paints")] public Dictionary<string, string[]>? Paints { get; set; }

    [JsonPropertyName("agents")] public List<AgentEntry>? Agents { get; set; }

    [JsonPropertyName("music")] public List<NamedEntry>? Music { get; set; }

    [JsonPropertyName("pins")] public List<NamedEntry>? Pins { get; set; }

    [JsonPropertyName("gloves")] public List<GloveEntry>? Gloves { get; set; }

    [JsonPropertyName("gloveTypes")] public List<GloveType>? GloveTypes { get; set; }
}
