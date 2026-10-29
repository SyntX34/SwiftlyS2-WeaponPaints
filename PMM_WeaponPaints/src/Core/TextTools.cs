namespace PMM_WeaponPaints.Core;

/* Small helpers for the text the game and WeaponPaints put on a menu line. */
public static class TextTools
{
    /* Removes the color tags the game allows in a menu title. */
    public static string Plain(string? text)
    {
        string result = text ?? "";
        foreach (string color in LockerConstants.Colors)
        {
            result = result.Replace("{ " + color + "}", "", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{" + color + "}", "", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("[color:" + color + "]", "", StringComparison.OrdinalIgnoreCase);
        }

        return result.Replace("</font>", "", StringComparison.OrdinalIgnoreCase).Trim();
    }

    public static bool HasAny(string? text, string first, string second)
    {
        string plain = Plain(text);
        return plain.Contains(first, StringComparison.OrdinalIgnoreCase) || plain.Contains(second, StringComparison.OrdinalIgnoreCase);
    }

    /* "Osiris | Elite Crew" becomes head "Osiris" and tail "Elite Crew". */
    public static void SplitBar(string text, out string head, out string tail)
    {
        int bar = text.IndexOf('|');
        if (bar < 0)
        {
            head = text.Trim();
            tail = "";
            return;
        }

        head = text[..bar].Trim();
        tail = text[(bar + 1)..].Trim();
    }

    public static string SplitBarName(string text)
    {
        SplitBar(text, out string head, out _);
        return head;
    }

    /* "Music Kit | Daniel Sadowski, Crimson Assault" gives the artist and the song. */
    public static void SplitMusic(string text, out string artist, out string song)
    {
        string body = text.Contains('|') ? text[(text.IndexOf('|') + 1)..].Trim() : text.Trim();
        foreach (string separator in new[] { " — ", " – ", " - ", ", " })
        {
            int at = body.IndexOf(separator, StringComparison.Ordinal);
            if (at > 0)
            {
                artist = body[..at].Trim();
                song = body[(at + separator.Length)..].Trim();
                return;
            }
        }

        artist = "";
        song = body;
    }

    public static bool IsNone(string text)
    {
        string plain = text.Trim();
        return plain.Equals("None", StringComparison.OrdinalIgnoreCase) || plain.Equals("Нет", StringComparison.OrdinalIgnoreCase);
    }

    public static string TeamName(int team)
    {
        return team == Catalog.WeaponCatalog.TeamCt ? "CT" : "T";
    }

    /* Keeps a command inside what the console can run: css_ names only. */
    public static bool IsSafeCommand(string command)
    {
        if (!command.StartsWith("css_", StringComparison.OrdinalIgnoreCase) || command.Length > 64)
        {
            return false;
        }

        foreach (char c in command)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != ' ')
            {
                return false;
            }
        }

        return true;
    }
}
