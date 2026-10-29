namespace PMM_WeaponPaints.Core;

public enum MenuKind
{
    // Finishes of one weapon or one knife.
    Skins,

    // The weapon list a group trigger opens.
    Weapons,

    // Finishes of one glove family.
    Gloves,

    Music,

    Pins,

    // Anything that only needs a name and a callback.
    Generic
}

/*
  One entry of a menu. The numbers are already resolved here, so the card
  builder never has to read them out of the text again.
*/
public sealed class LockerOption
{
    public string Text { get; init; } = "";

    public int Def { get; init; }

    public int Paint { get; init; }

    // Music kit or coin id, minus one when the entry is not one of those.
    public int ItemId { get; init; } = -1;

    // Agent model string.
    public string? Model { get; init; }

    // True for the default entry that carries no finish.
    public bool Stock { get; init; }

    public bool Disabled { get; init; }
}

/*
  The list a page shows, built on demand and thrown away when the player
  leaves the page. WeaponPaints menus live in the same shape, which keeps the
  card builder small.
*/
public sealed class LockerMenu
{
    public MenuKind Kind { get; init; }

    public string Title { get; init; } = "";

    // Set when the finishes belong to a knife, so the cards get the knife look.
    public string? Knife { get; init; }

    public List<LockerOption> Options { get; } = new();

    public LockerOption? Find(string name)
    {
        foreach (LockerOption option in Options)
        {
            if (string.Equals(option.Text, name, StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }

        return null;
    }
}
