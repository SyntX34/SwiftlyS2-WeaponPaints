namespace PMM_WeaponPaints.Catalog;

/*
  What one side of a player has equipped in WeaponPaints.
  Index 0 of the pair is T, index 1 is CT, the same way the tables store it.
*/
public sealed class TeamLoadout
{
    // Knife display name such as Karambit. Null means the default knife.
    public string? Knife { get; set; }

    // defindex of a gun, knife or glove to the paint id.
    public Dictionary<int, int> Paints { get; } = new();

    public int GloveDef { get; set; }

    public int Music { get; set; }

    public int Pin { get; set; }

    // Agent model as WeaponPaints stores it, for example tm_leet/tm_leet_variantg.
    public string? Agent { get; set; }

    public int PaintOf(int def)
    {
        return Paints.TryGetValue(def, out int paint) ? paint : 0;
    }
}
