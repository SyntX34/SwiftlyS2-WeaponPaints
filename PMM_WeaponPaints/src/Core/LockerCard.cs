namespace PMM_WeaponPaints.Core;

/*
  One cell of the grid. Cards are built fresh on every draw, so they can carry
  the current loadout without any cache to keep in sync.
*/
public sealed class LockerCard
{
    public string Name { get; set; } = "";

    public string Sub { get; set; } = "";

    public string? Art { get; set; }

    public int Rarity { get; set; }

    public bool Stock { get; set; }

    public bool EquippedT { get; set; }

    public bool EquippedCt { get; set; }

    public CardAction Do { get; set; } = new();
}
