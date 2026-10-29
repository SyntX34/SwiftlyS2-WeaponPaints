namespace PMM_WeaponPaints.Catalog;

/*
  The defindex numbers CS2 uses for weapons, knives and gloves, plus the names
  the rest of the plugin shows. WeaponPaints stores numbers, the layout shows
  names, so these tables are the bridge.
*/
public static class WeaponCatalog
{
    public const int TeamT = 0;
    public const int TeamCt = 1;

    public static readonly Dictionary<string, int> DefByClass = new(StringComparer.OrdinalIgnoreCase)
    {
        ["weapon_deagle"] = 1, ["weapon_elite"] = 2, ["weapon_fiveseven"] = 3, ["weapon_glock"] = 4,
        ["weapon_ak47"] = 7, ["weapon_aug"] = 8, ["weapon_awp"] = 9, ["weapon_famas"] = 10,
        ["weapon_g3sg1"] = 11, ["weapon_galilar"] = 13, ["weapon_m249"] = 14, ["weapon_m4a1"] = 16,
        ["weapon_mac10"] = 17, ["weapon_p90"] = 19, ["weapon_mp5sd"] = 23, ["weapon_ump45"] = 24,
        ["weapon_xm1014"] = 25, ["weapon_bizon"] = 26, ["weapon_mag7"] = 27, ["weapon_negev"] = 28,
        ["weapon_sawedoff"] = 29, ["weapon_tec9"] = 30, ["weapon_taser"] = 31, ["weapon_hkp2000"] = 32,
        ["weapon_mp7"] = 33, ["weapon_mp9"] = 34, ["weapon_nova"] = 35, ["weapon_p250"] = 36,
        ["weapon_scar20"] = 38, ["weapon_sg556"] = 39, ["weapon_ssg08"] = 40, ["weapon_m4a1_silencer"] = 60,
        ["weapon_usp_silencer"] = 61, ["weapon_cz75a"] = 63, ["weapon_revolver"] = 64,
        ["weapon_knife"] = 42, ["weapon_knife_t"] = 59,
        ["weapon_bayonet"] = 500, ["weapon_knife_css"] = 503, ["weapon_knife_flip"] = 505,
        ["weapon_knife_gut"] = 506, ["weapon_knife_karambit"] = 507, ["weapon_knife_m9_bayonet"] = 508,
        ["weapon_knife_tactical"] = 509, ["weapon_knife_falchion"] = 512, ["weapon_knife_survival_bowie"] = 514,
        ["weapon_knife_butterfly"] = 515, ["weapon_knife_push"] = 516, ["weapon_knife_cord"] = 517,
        ["weapon_knife_canis"] = 518, ["weapon_knife_ursus"] = 519, ["weapon_knife_gypsy_jackknife"] = 520,
        ["weapon_knife_outdoor"] = 521, ["weapon_knife_stiletto"] = 522, ["weapon_knife_widowmaker"] = 523,
        ["weapon_knife_skeleton"] = 525, ["weapon_knife_kukri"] = 526
    };

    public static readonly Dictionary<int, string> NameByDef = new()
    {
        [1] = "Desert Eagle", [2] = "Dual Berettas", [3] = "Five-SeveN", [4] = "Glock-18",
        [7] = "AK-47", [8] = "AUG", [9] = "AWP", [10] = "FAMAS", [11] = "G3SG1", [13] = "Galil AR",
        [14] = "M249", [16] = "M4A4", [17] = "MAC-10", [19] = "P90", [23] = "MP5-SD", [24] = "UMP-45",
        [25] = "XM1014", [26] = "PP-Bizon", [27] = "MAG-7", [28] = "Negev", [29] = "Sawed-Off",
        [30] = "Tec-9", [31] = "Zeus x27", [32] = "P2000", [33] = "MP7", [34] = "MP9", [35] = "Nova",
        [36] = "P250", [38] = "SCAR-20", [39] = "SG 553", [40] = "SSG 08", [60] = "M4A1-S",
        [61] = "USP-S", [63] = "CZ75-Auto", [64] = "R8 Revolver",
        [42] = "Default Knife", [59] = "Default Knife",
        [500] = "Bayonet", [503] = "Classic Knife", [505] = "Flip Knife", [506] = "Gut Knife",
        [507] = "Karambit", [508] = "M9 Bayonet", [509] = "Huntsman Knife", [512] = "Falchion Knife",
        [514] = "Bowie Knife", [515] = "Butterfly Knife", [516] = "Shadow Daggers", [517] = "Paracord Knife",
        [518] = "Survival Knife", [519] = "Ursus Knife", [520] = "Navaja Knife", [521] = "Nomad Knife",
        [522] = "Stiletto Knife", [523] = "Talon Knife", [525] = "Skeleton Knife", [526] = "Kukri Knife",
        [4725] = "Broken Fang Gloves", [5027] = "Bloodhound Gloves", [5030] = "Sport Gloves", [5031] = "Driver Gloves",
        [5032] = "Hand Wraps", [5033] = "Moto Gloves", [5034] = "Specialist Gloves", [5035] = "Hydra Gloves"
    };

    private static readonly Dictionary<string, int> DefByName = BuildDefByName();

    public static int DefOf(string? name)
    {
        return name != null && DefByName.TryGetValue(name.Trim(), out int def) ? def : 0;
    }

    public static string NameOf(int def)
    {
        return NameByDef.TryGetValue(def, out string? name) ? name : "";
    }

    public static string ClassOf(string? name)
    {
        int def = DefOf(name);
        if (def <= 0)
        {
            return "";
        }

        foreach ((string cls, int value) in DefByClass)
        {
            if (value == def)
            {
                return cls;
            }
        }

        return "";
    }

    private static Dictionary<string, int> BuildDefByName()
    {
        Dictionary<string, int> map = new(StringComparer.OrdinalIgnoreCase);
        foreach ((int def, string name) in NameByDef)
        {
            map.TryAdd(name, def);
        }

        return map;
    }
}
