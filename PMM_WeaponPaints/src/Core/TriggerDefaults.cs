namespace PMM_WeaponPaints.Core;

/*
  What every button of the shipped wp.xml does, and the weapon families the
  left column knows. A wp_triggers.json next to the dll can replace any of
  this without touching the layout.
*/
public static class TriggerDefaults
{
    public const string Pistols = "pistols";
    public const string Rifles = "rifles";
    public const string Snipers = "snipers";
    public const string Smg = "smg";
    public const string Heavy = "heavy";

    public static readonly Dictionary<string, string> Buttons = new(StringComparer.Ordinal)
    {
        ["wp-close"] = "close",
        ["wp-back"] = "back",
        ["wp-cat-loadout"] = "home",
        ["wp-cat-knife"] = "open:knife",
        ["wp-tile-knife"] = "open:knife",
        ["wp-cat-gloves"] = "open:gloves",
        ["wp-tile-gloves"] = "open:gloves",
        ["wp-cat-pistols"] = "weapons:pistols",
        ["wp-tile-pistols"] = "weapons:pistols",
        ["wp-cat-rifles"] = "weapons:rifles",
        ["wp-tile-rifles"] = "weapons:rifles",
        ["wp-cat-snipers"] = "weapons:snipers",
        ["wp-tile-snipers"] = "weapons:snipers",
        ["wp-cat-smg"] = "weapons:smg",
        ["wp-tile-smg"] = "weapons:smg",
        ["wp-cat-heavy"] = "weapons:heavy",
        ["wp-tile-heavy"] = "weapons:heavy",
        ["wp-cat-agents"] = "open:agents",
        ["wp-tile-agents"] = "open:agents",
        ["wp-cat-music"] = "open:music",
        ["wp-tile-music"] = "open:music",
        ["wp-cat-pins"] = "open:pins",
        ["wp-tile-pins"] = "open:pins",
        ["wp-prev"] = "page:prev",
        ["wp-next"] = "page:next"
    };

    public static readonly Dictionary<string, WeaponGroup> Groups = new(StringComparer.OrdinalIgnoreCase)
    {
        [Pistols] = new WeaponGroup(Pistols, "PISTOLS", new[]
        {
            "Glock-18", "USP-S", "P2000", "P250", "Dual Berettas", "Five-SeveN", "Tec-9", "CZ75-Auto", "Desert Eagle", "R8 Revolver",
            "Zeus x27"
        }),
        [Rifles] = new WeaponGroup(Rifles, "RIFLES", new[] { "AK-47", "M4A4", "M4A1-S", "Galil AR", "FAMAS", "SG 553", "AUG" }),
        [Snipers] = new WeaponGroup(Snipers, "SNIPERS", new[] { "AWP", "SSG 08", "SCAR-20", "G3SG1" }),
        [Smg] = new WeaponGroup(Smg, "SMG", new[] { "MAC-10", "MP9", "MP7", "MP5-SD", "UMP-45", "P90", "PP-Bizon" }),
        [Heavy] = new WeaponGroup(Heavy, "HEAVY", new[] { "Nova", "XM1014", "Sawed-Off", "MAG-7", "M249", "Negev" })
    };
}
