using PMM_WeaponPaints.Catalog;
using PMM_WeaponPaints.Core;

namespace PMM_WeaponPaints.View;

/*
  The line and the picture each category shows on the left column and on the
  home tiles. It always describes what is equipped right now.
*/
public sealed class HomeSummary
{
    private readonly ItemLibrary _items;
    private readonly IconLibrary _icons;
    private readonly LockerText _text;
    private readonly MenuBook _menus;

    public HomeSummary(ItemLibrary items, IconLibrary icons, LockerText text, MenuBook menus)
    {
        _items = items;
        _icons = icons;
        _text = text;
        _menus = menus;
    }

    public (string Label, string? Art) For(SlotState state, int cat)
    {
        TeamLoadout[] teams = state.Teams ??= new[] { new TeamLoadout(), new TeamLoadout() };
        int side = state.TeamIndex;
        TeamLoadout data = teams[side];

        switch (cat)
        {
            case LockerConstants.CatKnife:
            {
                string name = data.Knife ?? "Default Knife";
                int index = Array.IndexOf(LockerConstants.KnifeNames, name);
                int def = WeaponCatalog.DefOf(name);
                int paint = index == LockerConstants.StockKnife ? 0 : data.PaintOf(def);
                string? finish = paint > 0 ? _items.PaintName(paint, _text.Russian) : null;
                string art = (paint > 0 ? _icons.PictureOf(def, paint) : null) ?? "knife-" + Math.Max(0, index);
                return (finish != null ? name + "  |  " + finish : name, art);
            }

            case LockerConstants.CatGloves:
            {
                if (data.GloveDef == 0)
                {
                    return (_text.Get("default"), side == WeaponCatalog.TeamCt ? "gl-def-ct" : "gl-def-t");
                }

                GloveType? type = _items.FindGloveType(data.GloveDef);
                string name = type != null
                    ? _menus.TypeName(type)
                    : WeaponCatalog.NameByDef.TryGetValue(data.GloveDef, out string? known) ? known : _text.Get("cat.gloves");
                int paint = data.PaintOf(data.GloveDef);
                string? finish = paint > 0 ? _items.PaintName(paint, _text.Russian) : null;
                string? art = (paint > 0 ? _icons.ClassOf(_items.GloveName(data.GloveDef, paint)) : null) ?? type?.Art;
                return (finish != null ? name + "  |  " + finish : name, art);
            }

            case LockerConstants.CatAgents:
            {
                AgentEntry? agent = _items.Agent(data.Agent);
                if (agent == null)
                {
                    return data.Agent == null
                        ? (_text.Get("default"), side == WeaponCatalog.TeamCt ? "ag-def-ct" : "ag-def-t")
                        : (data.Agent[(data.Agent.LastIndexOf('/') + 1)..], null);
                }

                string full = (_text.Russian ? agent.Ru : agent.En) ?? agent.En ?? agent.Model;
                TextTools.SplitBar(full, out string head, out _);
                return (head, agent.Art);
            }

            case LockerConstants.CatMusic:
            {
                if (data.Music <= 0)
                {
                    return (_text.Get("default"), null);
                }

                if (_items.Music.TryGetValue(data.Music, out NamedEntry? entry))
                {
                    return ((_text.Russian ? entry.Ru : entry.En) ?? entry.En ?? "#" + data.Music, entry.Art);
                }

                return ("#" + data.Music, null);
            }

            case LockerConstants.CatPins:
            {
                if (data.Pin <= 0)
                {
                    return (_text.Get("none"), null);
                }

                if (_items.Pins.TryGetValue(data.Pin, out NamedEntry? entry))
                {
                    return ((_text.Russian ? entry.Ru : entry.En) ?? entry.En ?? "#" + data.Pin, entry.Art);
                }

                return ("#" + data.Pin, null);
            }

            default:
            {
                if (cat < 3 || cat >= LockerConstants.CatGroup.Length
                    || !TriggerDefaults.Groups.TryGetValue(LockerConstants.CatGroup[cat], out WeaponGroup? group))
                {
                    return (_text.Get("default"), null);
                }

                string? best = null;
                foreach (string weapon in group.Items)
                {
                    if (data.PaintOf(WeaponCatalog.DefOf(weapon)) > 0 && (best == null || weapon == LockerConstants.CatPreferred[cat]))
                    {
                        best = weapon;
                    }
                }

                if (best == null)
                {
                    return (_text.Get("default"), null);
                }

                int def = WeaponCatalog.DefOf(best);
                int paint = data.PaintOf(def);
                string? finish = _items.PaintName(paint, _text.Russian);
                return (finish != null ? best + "  |  " + finish : best, _icons.PictureOf(def, paint) ?? _icons.ClassOf(best));
            }
        }
    }

    public string GroupTitle(WeaponGroup group)
    {
        bool known = group.Key.Length > 0
            && TriggerDefaults.Groups.TryGetValue(group.Key, out WeaponGroup? standard)
            && string.Equals(group.Title, standard.Title, StringComparison.Ordinal);

        return known ? _text.Get("group." + group.Key.ToLowerInvariant()) : group.Title;
    }

    public string GroupTitleOf(string weapon)
    {
        foreach (WeaponGroup group in TriggerDefaults.Groups.Values)
        {
            if (group.Items.Contains(weapon, StringComparer.OrdinalIgnoreCase))
            {
                return _text.Get("group." + group.Key);
            }
        }

        return _text.Get("cat.skins");
    }
}
