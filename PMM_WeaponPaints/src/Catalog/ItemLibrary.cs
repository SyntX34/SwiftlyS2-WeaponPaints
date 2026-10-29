using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PMM_WeaponPaints.Catalog;

/*
  Item tables built from the CS2 item schema: rarities, paint names, agents,
  music kits, coins and gloves. Everything is read once from pmm_items.json
  that sits in the resources folder of the plugin.
*/
public sealed class ItemLibrary
{
    private Dictionary<long, int> _skinRarity = new();
    private Dictionary<int, string[]> _paints = new();
    private Dictionary<int, List<int>> _finishes = new();
    private readonly Dictionary<string, (int Def, int Paint)> _gloveByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(int Def, int Paint), string> _gloveName = new();
    private readonly Dictionary<string, int> _musicByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _pinByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<GloveEntry>> _glovesByDef = new();

    public List<AgentEntry> Agents { get; private set; } = new();

    public Dictionary<int, NamedEntry> Music { get; private set; } = new();

    public Dictionary<int, NamedEntry> Pins { get; private set; } = new();

    public List<GloveType> GloveTypes { get; private set; } = new();

    public void Load(string path, ILogger logger)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("pmm_items.json was not found at {Path}. No rarities, agents, music kits or coins.", path);
            return;
        }

        try
        {
            ItemFile? file = JsonSerializer.Deserialize<ItemFile>(File.ReadAllText(path));
            if (file == null)
            {
                logger.LogWarning("pmm_items.json is empty.");
                return;
            }

            _skinRarity = new Dictionary<long, int>();
            _finishes = new Dictionary<int, List<int>>();
            foreach ((string key, int rarity) in file.SkinRarity ?? new())
            {
                string[] parts = key.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int def) || !int.TryParse(parts[1], out int paint))
                {
                    continue;
                }

                _skinRarity[Key(def, paint)] = rarity;
                if (!_finishes.TryGetValue(def, out List<int>? list))
                {
                    list = new List<int>();
                    _finishes[def] = list;
                }

                list.Add(paint);
            }

            foreach (List<int> list in _finishes.Values)
            {
                list.Sort();
            }

            _paints = new Dictionary<int, string[]>();
            foreach ((string key, string[] names) in file.Paints ?? new())
            {
                if (int.TryParse(key, out int paint) && names.Length > 0)
                {
                    _paints[paint] = names;
                }
            }

            Agents = file.Agents ?? new List<AgentEntry>();
            Music = (file.Music ?? new List<NamedEntry>()).GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.First());
            Pins = (file.Pins ?? new List<NamedEntry>()).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            GloveTypes = file.GloveTypes ?? new List<GloveType>();

            _gloveByName.Clear();
            _gloveName.Clear();
            _glovesByDef.Clear();
            foreach (GloveEntry glove in file.Gloves ?? new List<GloveEntry>())
            {
                AddGlove(glove.Name, glove.Def, glove.Paint);
            }

            _musicByName.Clear();
            foreach (NamedEntry music in Music.Values)
            {
                AddNames(_musicByName, music);
            }

            _pinByName.Clear();
            foreach (NamedEntry pin in Pins.Values)
            {
                AddNames(_pinByName, pin);
            }

            logger.LogInformation("pmm_items.json: {Skins} skins, {Agents} agents, {Music} music kits, {Pins} coins, {Gloves} glove families.",
                _skinRarity.Count, Agents.Count, Music.Count, Pins.Count, GloveTypes.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning("pmm_items.json could not be read: {Message}", ex.Message);
        }
    }

    public int SkinRarity(int def, int paint)
    {
        return _skinRarity.TryGetValue(Key(def, paint), out int rarity) ? rarity : 0;
    }

    /* Every paint id the game has for one defindex, in ascending order. */
    public List<int> Finishes(int def)
    {
        return _finishes.TryGetValue(def, out List<int>? list) ? list : new List<int>();
    }

    public string? PaintName(int paint, bool russian)
    {
        if (!_paints.TryGetValue(paint, out string[]? names))
        {
            return null;
        }

        return russian && names.Length > 1 && !string.IsNullOrEmpty(names[1]) ? names[1] : names[0];
    }

    public bool TryGlove(string text, out int def, out int paint)
    {
        if (_gloveByName.TryGetValue(Norm(text), out (int Def, int Paint) found))
        {
            def = found.Def;
            paint = found.Paint;
            return true;
        }

        def = 0;
        paint = -1;
        return false;
    }

    public string? GloveName(int def, int paint)
    {
        return _gloveName.TryGetValue((def, paint), out string? name) ? name : null;
    }

    public List<GloveEntry> GlovesOf(int def)
    {
        return _glovesByDef.TryGetValue(def, out List<GloveEntry>? list) ? list : new List<GloveEntry>();
    }

    public int MusicId(string text)
    {
        return _musicByName.TryGetValue(Norm(text), out int id) ? id : -1;
    }

    public int PinId(string text)
    {
        return _pinByName.TryGetValue(Norm(text), out int id) ? id : -1;
    }

    public AgentEntry? Agent(string? model)
    {
        if (string.IsNullOrEmpty(model))
        {
            return null;
        }

        return Agents.FirstOrDefault(a => a.Model.Equals(model, StringComparison.OrdinalIgnoreCase));
    }

    public GloveType? FindGloveType(int def)
    {
        return GloveTypes.FirstOrDefault(t => t.Def == def);
    }

    public static string Norm(string? text)
    {
        return Regex.Replace(text ?? "", @"\s+", " ").Trim();
    }

    private void AddGlove(string name, int def, int paint)
    {
        string text = Norm(name);
        if (text.Length == 0)
        {
            return;
        }

        _gloveByName.TryAdd(text, (def, paint));
        _gloveName.TryAdd((def, paint), name);

        if (!_glovesByDef.TryGetValue(def, out List<GloveEntry>? list))
        {
            list = new List<GloveEntry>();
            _glovesByDef[def] = list;
        }

        list.Add(new GloveEntry { Def = def, Paint = paint, Name = name });
    }

    private static void AddNames(Dictionary<string, int> map, NamedEntry entry)
    {
        foreach (string name in (entry.Names ?? new List<string>()).Append(entry.En ?? "").Append(entry.Ru ?? ""))
        {
            string text = Norm(name);
            if (text.Length > 0)
            {
                map.TryAdd(text, entry.Id);
            }
        }
    }

    private static long Key(int def, int paint)
    {
        return ((long)def << 32) | (uint)paint;
    }
}
