using PMM_WeaponPaints.Catalog;

namespace PMM_WeaponPaints.Core;

/* One step of the back button: where the player was before the current page. */
public sealed class NavFrame
{
    public int View { get; init; }

    public LockerMenu? Menu { get; init; }

    public int Page { get; init; }

    public int GloveType { get; init; } = -1;

    public int SidePage { get; init; }

    public string Active { get; init; } = "home";
}

/*
  Everything the locker remembers about one player. A slot object lives as
  long as the server runs; it is reset when the player disconnects so the next
  player in that slot starts clean.

  The icon, rarity and art fields hold what was last sent to this client. The
  layout is not cleared when the window is hidden, so a class has to be
  removed from the panel it sits on, which means the plugin must know it.
*/
public sealed class SlotState
{
    public bool Open { get; set; }

    // Changed on every open and close, so late callbacks can tell they are stale.
    public int Token { get; set; }

    public int View { get; set; } = LockerConstants.ViewHome;

    public int Page { get; set; }

    public int PagerStart { get; set; }

    public int TeamIndex { get; set; }

    public LockerMenu? Menu { get; set; }

    public TeamLoadout[]? Teams { get; set; }

    public List<LockerCard>? Cards { get; set; }

    public readonly string?[] Icon = new string?[LockerConstants.LivePage];

    public readonly string?[] Rarity = new string?[LockerConstants.LivePage];

    public readonly string?[] SideArt = new string?[LockerConstants.CatCount];

    public readonly string?[] TileArt = new string?[LockerConstants.CatCount];

    public Dictionary<string, bool>? SentClass { get; set; }

    public Dictionary<string, string>? SentVar { get; set; }

    public string Active { get; set; } = "home";

    public int SidePage { get; set; }

    public WeaponGroup? Filter { get; set; }

    public int GloveType { get; set; } = -1;

    public bool Loading { get; set; }

    public List<NavFrame> Back { get; } = new();

    public void DropSent()
    {
        SentClass = null;
        SentVar = null;

        for (int i = 0; i < LockerConstants.LivePage; i++)
        {
            Icon[i] = null;
            Rarity[i] = null;
        }

        for (int i = 0; i < LockerConstants.CatCount; i++)
        {
            SideArt[i] = null;
            TileArt[i] = null;
        }
    }

    public void Clear()
    {
        Open = false;
        Token++;
        Menu = null;
        Filter = null;
        Cards = null;
        Teams = null;
        GloveType = -1;
        Page = 0;
        PagerStart = 0;
        SidePage = 0;
        View = LockerConstants.ViewHome;
        Active = "home";
        Loading = false;
        Back.Clear();
        DropSent();
    }
}
