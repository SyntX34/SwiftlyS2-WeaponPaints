namespace WeaponPaints.Catalog;

/* One row of wp_player_skins: which finish, how worn and how it is named. */
public readonly record struct PaintInfo(int Paint, float Wear, int Seed, int StatTrak, string Name);

/*
  What one side of a player has equipped. Index 0 is T and index 1 is CT,
  which is the order the weapon_team column uses after the mapping is done.
*/
public sealed class TeamLoadout
{
    public string? Knife { get; set; }

    public Dictionary<int, PaintInfo> Paints { get; } = new();

    public int GloveDef { get; set; }

    public int Music { get; set; }

    public int Pin { get; set; }

    public string? Agent { get; set; }

    public int PaintOf(int def)
    {
        return Paints.TryGetValue(def, out PaintInfo info) ? info.Paint : 0;
    }

    public PaintInfo? InfoOf(int def)
    {
        return Paints.TryGetValue(def, out PaintInfo info) ? info : null;
    }
}
