namespace PMM_WeaponPaints.Core;

public enum CardActionKind
{
    None,

    // Equip this knife and go on to its finishes.
    PickKnife,

    // Open the finish list of a weapon or a knife.
    OpenFinishes,

    // Open the finish list of a glove family.
    OpenGloveFinishes,

    // Equip a finish of a gun or a knife.
    WearSkin,

    // Equip a finish of the gloves.
    WearGlove,

    WearMusic,

    WearPin,

    WearAgent
}

/*
  What a card does when it is clicked. Cards carry data, never a callback, so
  the view code stays free of navigation and the click handler decides alone
  how a choice is saved.
*/
public sealed class CardAction
{
    public CardActionKind Kind { get; init; } = CardActionKind.None;

    // Weapon or knife name for the pick actions.
    public string Name { get; init; } = "";

    // Glove family or item defindex.
    public int Def { get; init; }

    public int Paint { get; init; }

    public int ItemId { get; init; } = -1;

    public string? Model { get; init; }

    public bool Stock { get; init; }
}
