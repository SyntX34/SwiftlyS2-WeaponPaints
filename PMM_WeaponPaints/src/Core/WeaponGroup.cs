namespace PMM_WeaponPaints.Core;

/*
  One weapon family the locker can show on its own: pistols, rifles and so on.
  The key is also the trigger argument, for example weapons:pistols.
  A trigger may also carry its own list of weapon names instead of a key.
*/
public sealed class WeaponGroup
{
    public WeaponGroup(string key, string title, string[] items)
    {
        Key = key;
        Title = title;
        Items = items;
    }

    public string Key { get; }

    public string Title { get; }

    public string[] Items { get; }
}
