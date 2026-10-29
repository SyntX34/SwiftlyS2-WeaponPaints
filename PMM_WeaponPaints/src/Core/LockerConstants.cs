namespace PMM_WeaponPaints.Core;

/*
  Fixed numbers and tables that the panorama layout and this plugin share.
  The layout has one panel per view, per category, per weapon group and per
  card cell, so these names have to match wp.xml exactly.
*/
public static class LockerConstants
{
    public const string PluginVersion = "0.1.1";

    public const string LayoutPath = "panorama/layout/custom_game/wp.xml";

    public const string TriggerFileName = "wp_triggers.json";

    public const int ViewHome = 0;
    public const int ViewKnife = 1;
    public const int ViewLive = 2;
    public const int ViewAgents = 3;
    public const int ViewGloves = 4;

    public const int LivePage = 120;
    public const int RowSize = 5;
    public const int PagerButtons = 7;

    public const int SlotCount = 64;

    public const int CatLoadout = 0;
    public const int CatKnife = 1;
    public const int CatGloves = 2;
    public const int CatPistols = 3;
    public const int CatRifles = 4;
    public const int CatSnipers = 5;
    public const int CatSmg = 6;
    public const int CatHeavy = 7;
    public const int CatAgents = 8;
    public const int CatMusic = 9;
    public const int CatPins = 10;
    public const int CatNone = 11;
    public const int CatCount = 12;

    public const int StockKnife = 1;
    public const int CovertRarity = 6;

    // Index of the view class the root panel gets: wp-root view-home, view-knife and so on.
    public static readonly string[] Views = { "view-home", "view-knife", "view-live", "view-agents", "view-gloves" };

    // Category classes, one per entry of the left column.
    public static readonly string[] Cats =
    {
        "cat-loadout", "cat-knife", "cat-gloves", "cat-pistols", "cat-rifles", "cat-snipers", "cat-smg", "cat-heavy", "cat-agents", "cat-music",
        "cat-pins", "cat-none"
    };

    // Category index to panel suffix: wp-side-knife, wp-home-knife, wp-sw-knife, wp-tile-art-knife.
    public static readonly string[] CatKeys = { "loadout", "knife", "gloves", "pistols", "rifles", "snipers", "smg", "heavy", "agents", "music", "pins" };

    // Index into KnifeNames. The order is the one the layout shows.
    public static readonly string[] KnifeNames =
    {
        "Shadow Daggers", "Default Knife", "Bayonet", "Bowie Knife", "Butterfly Knife", "Classic Knife", "Falchion Knife", "Flip Knife", "Gut Knife", "Huntsman Knife",
        "Karambit", "Kukri Knife", "M9 Bayonet", "Navaja Knife", "Nomad Knife", "Paracord Knife", "Skeleton Knife", "Stiletto Knife", "Survival Knife", "Talon Knife",
        "Ursus Knife"
    };

    // How many finishes each knife has, used for the small line under a knife card.
    public static readonly int[] KnifeFinishes =
    {
        34, 0, 34, 34, 34, 12, 34, 34, 34, 34,
        34, 12, 34, 24, 24, 24, 24, 24, 24, 24,
        24
    };

    // The order the knife page starts with. The equipped knife is moved to the front at draw time.
    public static readonly int[] KnifeBase =
    {
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
        11, 12, 13, 14, 15, 0, 16, 17, 18, 19,
        20
    };

    // Colors that may sit in a WeaponPaints menu title. They are removed before any text is compared.
    public static readonly string[] Colors =
    {
        "Default", "White", "Darkred", "Green", "Lightyellow", "Lightblue", "Olive", "Lime", "Red", "Lightpurple",
        "Purple", "Grey", "Yellow", "Gold", "Silver", "Blue", "Darkblue", "Bluegrey", "Magenta", "Lightred",
        "Orange"
    };

    // Category index to weapon family, for the five gun rows of the left column.
    public static readonly string[] CatGroup = { "", "", "", "pistols", "rifles", "snipers", "smg", "heavy" };

    // The weapon that stands for a family on the home page when several carry a skin.
    public static readonly string[] CatPreferred = { "", "", "", "Desert Eagle", "AK-47", "AWP", "MAC-10", "Nova" };

    public static int CategoryOf(string? command)
    {
        return command switch
        {
            "css_knife" => CatKnife,
            "css_gloves" => CatGloves,
            "css_skins" => CatPistols,
            "css_agents" => CatAgents,
            "css_music" => CatMusic,
            "css_pins" => CatPins,
            _ => CatNone
        };
    }
}
