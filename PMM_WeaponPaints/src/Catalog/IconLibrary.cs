using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PMM_WeaponPaints.Catalog;

/*
  icons.json maps a full item line such as "AK-47 | Redline (282)" to a css
  class the layout knows. The line is written the way the game writes it, so
  the plugin also indexes every picture by defindex and paint id and uses that
  index whenever it has the numbers at hand.
*/
public sealed class IconLibrary
{
    private static readonly Regex TrailingPaint = new(@"\((\d+)\)\s*$", RegexOptions.Compiled);

    private readonly Dictionary<string, string> _byLine = new(StringComparer.Ordinal);
    private readonly Dictionary<(int Def, int Paint), string> _byNumber = new();

    public int Count => _byLine.Count;

    public void Load(string path, ILogger logger)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("icons.json was not found at {Path}. Cards get no pictures.", path);
            return;
        }

        try
        {
            Dictionary<string, string>? icons = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
            if (icons == null)
            {
                return;
            }

            _byLine.Clear();
            foreach ((string line, string css) in icons)
            {
                _byLine[line] = css;
            }

            IndexByNumber();
            logger.LogInformation("icons.json: {Count} pictures indexed.", _byNumber.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning("icons.json could not be read: {Message}", ex.Message);
        }
    }

    public string? ClassOf(string? line)
    {
        return line != null && _byLine.TryGetValue(line, out string? css) ? css : null;
    }

    /* The css class for a defindex and a paint id, or null when the file has no picture for it. */
    public string? PictureOf(int def, int paint)
    {
        return paint > 0 && _byNumber.TryGetValue((def, paint), out string? line) ? ClassOf(line) : null;
    }

    private void IndexByNumber()
    {
        _byNumber.Clear();
        foreach ((string line, string css) in _byLine)
        {
            Match tail = TrailingPaint.Match(line);
            if (!tail.Success || !int.TryParse(tail.Groups[1].Value, out int paint))
            {
                continue;
            }

            int bar = line.IndexOf('|');
            string head = (bar >= 0 ? line[..bar] : line).Replace("★", "").Replace("(", " ").Replace(")", " ").Trim();

            foreach ((int def, string name) in WeaponCatalog.NameByDef)
            {
                // The default knives share a name and their pictures come from the knife table instead.
                if (def == 42 || def == 59)
                {
                    continue;
                }

                if (!head.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!_byNumber.TryGetValue((def, paint), out string? current) || line.Length < current.Length)
                {
                    _byNumber[(def, paint)] = line;
                }
            }
        }
    }
}
