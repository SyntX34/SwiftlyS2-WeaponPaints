using PMM_WeaponPaints.Core;

namespace PMM_WeaponPaints.Catalog;

/*
  Builds the lists the locker draws. WeaponPaints opens these as chat menus;
  here they are plain objects, so a page can be rebuilt at any time without
  waiting for another plugin to answer.
*/
public sealed class MenuBook
{
    private readonly ItemLibrary _items;
    private readonly LockerText _text;

    public MenuBook(ItemLibrary items, LockerText text)
    {
        _items = items;
        _text = text;
    }

    /* Without a group the list covers every family, which is what the main weapon button opens. */
    public LockerMenu WeaponList(WeaponGroup? group)
    {
        LockerMenu menu = new() { Kind = MenuKind.Weapons, Title = group?.Title ?? _text.Get("cat.weapons") };
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        IEnumerable<string> names = group != null
            ? group.Items
            : TriggerDefaults.Groups.Values.SelectMany(g => g.Items);

        foreach (string weapon in names)
        {
            int def = WeaponCatalog.DefOf(weapon);
            if (def <= 0 || !seen.Add(weapon))
            {
                continue;
            }

            menu.Options.Add(new LockerOption { Text = weapon, Def = def });
        }

        return menu;
    }

    public LockerMenu WeaponFinishes(string weapon)
    {
        int def = WeaponCatalog.DefOf(weapon);
        return FinishMenu(weapon, def, null);
    }

    public LockerMenu KnifeFinishes(string knife)
    {
        int def = WeaponCatalog.DefOf(knife);
        return FinishMenu(knife, def, knife);
    }

    public LockerMenu GloveFinishes(GloveType type)
    {
        LockerMenu menu = new() { Kind = MenuKind.Gloves, Title = TypeName(type) };
        menu.Options.Add(new LockerOption { Text = _text.Get("default"), Stock = true });

        foreach (GloveEntry glove in _items.GlovesOf(type.Def))
        {
            menu.Options.Add(new LockerOption { Text = glove.Name, Def = glove.Def, Paint = glove.Paint });
        }

        return menu;
    }

    public LockerMenu MusicList()
    {
        LockerMenu menu = new() { Kind = MenuKind.Music, Title = _text.Get("cat.music") };
        menu.Options.Add(new LockerOption { Text = _text.Get("none"), ItemId = 0, Stock = true });

        foreach (NamedEntry entry in _items.Music.Values.OrderBy(e => e.Id))
        {
            menu.Options.Add(new LockerOption
            {
                Text = _text.Russian ? entry.Ru ?? entry.En ?? "" : entry.En ?? entry.Ru ?? "",
                ItemId = entry.Id
            });
        }

        return menu;
    }

    public LockerMenu PinList()
    {
        LockerMenu menu = new() { Kind = MenuKind.Pins, Title = _text.Get("cat.pins") };
        menu.Options.Add(new LockerOption { Text = _text.Get("none"), ItemId = 0, Stock = true });

        foreach (NamedEntry entry in _items.Pins.Values.OrderBy(e => e.Id))
        {
            menu.Options.Add(new LockerOption
            {
                Text = _text.Russian ? entry.Ru ?? entry.En ?? "" : entry.En ?? entry.Ru ?? "",
                ItemId = entry.Id
            });
        }

        return menu;
    }

    public string TypeName(GloveType type)
    {
        return (_text.Russian ? type.Ru : type.En) ?? type.En ?? "";
    }

    private LockerMenu FinishMenu(string title, int def, string? knife)
    {
        LockerMenu menu = new() { Kind = MenuKind.Skins, Title = title, Knife = knife };
        menu.Options.Add(new LockerOption
        {
            Text = _text.Get("default"),
            Def = def,
            Paint = 0,
            Stock = true
        });

        foreach (int paint in _items.Finishes(def))
        {
            if (paint <= 0)
            {
                continue;
            }

            string? name = _items.PaintName(paint, _text.Russian);
            menu.Options.Add(new LockerOption
            {
                Text = name ?? "#" + paint,
                Def = def,
                Paint = paint
            });
        }

        return menu;
    }
}
