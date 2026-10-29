using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PMM_WeaponPaints.Core;

public enum RarityKind
{
    Weapon,
    Agent,
    Item
}

/*
  Reads resources/translations/<language>.jsonc, the folder SwiftlyS2 also
  loads for the plugin. English is always kept as the fallback, so a key that
  one language file is missing still shows a readable line.

  The rarity lines are single strings with eight entries split by a bar and
  read by rarity number. Entry zero stays empty because rarity starts at one.
*/
public sealed class LockerText
{
    public static readonly string[] Languages = { "en", "ru", "da", "de", "fr", "fi", "es", "hu" };

    private readonly Dictionary<string, string> _active = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _fallback = new(StringComparer.Ordinal);
    private readonly ILogger _logger;

    public LockerText(ILogger logger)
    {
        _logger = logger;
    }

    public string Language { get; private set; } = "en";

    public bool Russian => Language == "ru";

    public void Load(string translationFolder, string language)
    {
        Language = Normalize(language);

        Dictionary<string, string> english = ReadFile(Path.Combine(translationFolder, "en.jsonc"));
        if (english.Count == 0)
        {
            _logger.LogWarning("en.jsonc was not found in {Folder}. The locker shows raw keys.", translationFolder);
        }

        _fallback.Clear();
        foreach ((string key, string value) in english)
        {
            _fallback[key] = value;
        }

        _active.Clear();
        Dictionary<string, string> chosen = Language == "en" ? english : ReadFile(Path.Combine(translationFolder, Language + ".jsonc"));
        foreach ((string key, string value) in chosen)
        {
            _active[key] = value;
        }
    }

    public static string Normalize(string? language)
    {
        string text = (language ?? "").Trim().ToLowerInvariant();
        return Languages.Contains(text) ? text : "en";
    }

    public string Get(string key, params object[] args)
    {
        string text = _active.TryGetValue(key, out string? value) ? value
            : _fallback.TryGetValue(key, out string? english) ? english
            : key;

        try
        {
            return args.Length == 0 ? text : string.Format(text, args);
        }
        catch (FormatException)
        {
            return text;
        }
    }

    public string RarityName(RarityKind kind, int rarity)
    {
        if (rarity < 1 || rarity > 7)
        {
            return "";
        }

        string key = kind switch
        {
            RarityKind.Agent => "rarity.agent",
            RarityKind.Item => "rarity.item",
            _ => "rarity.weapon"
        };

        string? line = null;
        if (_active.TryGetValue(key, out string? active)) line = active;
        else if (_fallback.TryGetValue(key, out string? english)) line = english;

        if (line != null)
        {
            string[] parts = line.Split('|');
            if (rarity < parts.Length && parts[rarity].Length > 0)
            {
                return parts[rarity];
            }
        }

        return kind switch
        {
            RarityKind.Agent => AgentEn[rarity],
            RarityKind.Item => ItemEn[rarity],
            _ => WeaponEn[rarity]
        };
    }

    // English fallback, index 1 to 7.
    private static readonly string[] WeaponEn = { "", "CONSUMER GRADE", "INDUSTRIAL GRADE", "MIL-SPEC", "RESTRICTED", "CLASSIFIED", "COVERT", "CONTRABAND" };
    private static readonly string[] AgentEn = { "", "", "", "DISTINGUISHED", "EXCEPTIONAL", "SUPERIOR", "MASTER", "MASTER" };
    private static readonly string[] ItemEn = { "", "BASE GRADE", "BASE GRADE", "HIGH GRADE", "REMARKABLE", "EXOTIC", "EXTRAORDINARY", "CONTRABAND" };

    private Dictionary<string, string> ReadFile(string path)
    {
        Dictionary<string, string> table = new(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return table;
        }

        try
        {
            JsonSerializerOptions options = new()
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            Dictionary<string, string>? read = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path), options);
            if (read != null)
            {
                foreach ((string key, string value) in read)
                {
                    table[key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{File} could not be read: {Message}", Path.GetFileName(path), ex.Message);
        }

        return table;
    }
}
