using PMM_WeaponPaints.Catalog;
using PMM_WeaponPaints.Core;
using SwiftlyS2.Shared.Players;

namespace PMM_WeaponPaints.Locker;

/*
  Turns the open page into cards. Every card knows the numbers it stands for,
  so the click handler can save the choice without reading the text again.
*/
public sealed class CardFactory
{
    private readonly ItemLibrary _items;
    private readonly IconLibrary _icons;
    private readonly LockerText _text;
    private readonly MenuBook _menus;

    public CardFactory(ItemLibrary items, IconLibrary icons, LockerText text, MenuBook menus)
    {
        _items = items;
        _icons = icons;
        _text = text;
        _menus = menus;
    }

    public List<LockerCard> Build(IPlayer player, SlotState state)
    {
        return state.View switch
        {
            LockerConstants.ViewKnife => KnifeCards(player, state),
            LockerConstants.ViewAgents => AgentCards(player, state),
            LockerConstants.ViewGloves => GloveTypeCards(player, state),
            LockerConstants.ViewLive => LiveCards(player, state),
            _ => new List<LockerCard>()
        };
    }

    private List<LockerCard> KnifeCards(IPlayer player, SlotState state)
    {
        TeamLoadout[] teams = Teams(state);
        int side = state.TeamIndex;
        List<LockerCard> cards = new();

        foreach (int k in KnifeOrder(state))
        {
            string name = LockerConstants.KnifeNames[k];
            bool stock = k == LockerConstants.StockKnife;
            LockerCard card = new()
            {
                Name = name,
                Stock = stock,
                Rarity = stock ? 0 : LockerConstants.CovertRarity,
                EquippedT = KnifeOf(teams[WeaponCatalog.TeamT]) == name,
                EquippedCt = KnifeOf(teams[WeaponCatalog.TeamCt]) == name,
                Art = "knife-" + k,
                Do = new CardAction { Kind = CardActionKind.PickKnife, Name = name, Stock = stock }
            };

            bool equipped = side == WeaponCatalog.TeamCt ? card.EquippedCt : card.EquippedT;
            if (stock)
            {
                card.Sub = _text.Get("stock");
            }
            else if (equipped)
            {
                int def = WeaponCatalog.DefOf(name);
                int paint = teams[side].PaintOf(def);
                card.Sub = paint > 0 ? (_items.PaintName(paint, _text.Russian) ?? _text.Get("vanilla")) : _text.Get("vanilla");
                card.Art = paint > 0 ? (_icons.PictureOf(def, paint) ?? card.Art) : card.Art;
            }
            else
            {
                card.Sub = _text.Get("finishes", LockerConstants.KnifeFinishes[k]);
            }

            cards.Add(card);
        }

        return cards;
    }

    private List<LockerCard> AgentCards(IPlayer player, SlotState state)
    {
        TeamLoadout[] teams = Teams(state);
        int teamNumber = state.TeamIndex == WeaponCatalog.TeamCt ? 3 : 2;

        List<LockerCard> cards = new()
        {
            new LockerCard
            {
                Name = _text.Get("default"),
                Sub = TextTools.TeamName(state.TeamIndex),
                Art = state.TeamIndex == WeaponCatalog.TeamCt ? "ag-def-ct" : "ag-def-t",
                Stock = true,
                EquippedT = teams[WeaponCatalog.TeamT].Agent == null,
                EquippedCt = teams[WeaponCatalog.TeamCt].Agent == null,
                Do = new CardAction { Kind = CardActionKind.WearAgent, Model = null, Stock = true }
            }
        };

        foreach (AgentEntry agent in _items.Agents.Where(a => a.Team == teamNumber))
        {
            string full = (_text.Russian ? agent.Ru : agent.En) ?? agent.En ?? agent.Model;
            TextTools.SplitBar(full, out string head, out string tail);

            cards.Add(new LockerCard
            {
                Name = head,
                Sub = tail.Length > 0 ? tail : _text.RarityName(RarityKind.Agent, agent.Rarity),
                Art = agent.Art,
                Rarity = agent.Rarity,
                EquippedT = string.Equals(teams[WeaponCatalog.TeamT].Agent, agent.Model, StringComparison.OrdinalIgnoreCase),
                EquippedCt = string.Equals(teams[WeaponCatalog.TeamCt].Agent, agent.Model, StringComparison.OrdinalIgnoreCase),
                Do = new CardAction { Kind = CardActionKind.WearAgent, Model = agent.Model }
            });
        }

        return cards;
    }

    private List<LockerCard> GloveTypeCards(IPlayer player, SlotState state)
    {
        TeamLoadout[] teams = Teams(state);
        int side = state.TeamIndex;
        List<LockerCard> cards = new()
        {
            new LockerCard
            {
                Name = _text.Get("default"),
                Sub = _text.Get("stock"),
                Art = side == WeaponCatalog.TeamCt ? "gl-def-ct" : "gl-def-t",
                Stock = true,
                EquippedT = teams[WeaponCatalog.TeamT].GloveDef == 0,
                EquippedCt = teams[WeaponCatalog.TeamCt].GloveDef == 0,
                Do = new CardAction { Kind = CardActionKind.WearGlove, Def = 0, Paint = 0, Stock = true }
            }
        };

        foreach (GloveType type in _items.GloveTypes)
        {
            int def = type.Def;
            int count = _items.GlovesOf(def).Count;
            if (count == 0)
            {
                continue;
            }

            LockerCard card = new()
            {
                Name = _menus.TypeName(type),
                Rarity = LockerConstants.CovertRarity,
                Art = type.Art,
                EquippedT = teams[WeaponCatalog.TeamT].GloveDef == def,
                EquippedCt = teams[WeaponCatalog.TeamCt].GloveDef == def,
                Do = new CardAction { Kind = CardActionKind.OpenGloveFinishes, Def = def }
            };

            bool equipped = side == WeaponCatalog.TeamCt ? card.EquippedCt : card.EquippedT;
            int paint = teams[side].PaintOf(def);
            if (equipped && paint > 0)
            {
                card.Sub = _items.PaintName(paint, _text.Russian) ?? _text.Get("finishes", count);
                card.Art = _icons.ClassOf(_items.GloveName(def, paint)) ?? card.Art;
            }
            else
            {
                card.Sub = _text.Get("finishes", count);
            }

            cards.Add(card);
        }

        return cards;
    }

    private List<LockerCard> LiveCards(IPlayer player, SlotState state)
    {
        LockerMenu? menu = state.Menu;
        List<LockerCard> cards = new();
        if (menu == null)
        {
            return cards;
        }

        TeamLoadout[] teams = Teams(state);
        int side = state.TeamIndex;

        switch (menu.Kind)
        {
            case MenuKind.Skins:
                foreach (LockerOption option in menu.Options)
                {
                    bool stock = option.Paint <= 0;
                    int rarity = stock ? 0 : menu.Knife != null ? LockerConstants.CovertRarity : _items.SkinRarity(option.Def, option.Paint);
                    if (!stock && rarity == 0)
                    {
                        rarity = 3;
                    }

                    cards.Add(new LockerCard
                    {
                        Name = option.Text,
                        Sub = stock
                            ? _text.Get("stock")
                            : (menu.Knife != null ? "★ " : "") + _text.RarityName(RarityKind.Weapon, rarity),
                        Art = stock ? null : _icons.PictureOf(option.Def, option.Paint),
                        Rarity = rarity,
                        Stock = stock,
                        EquippedT = SkinOn(teams[WeaponCatalog.TeamT], menu.Knife, option.Def, option.Paint),
                        EquippedCt = SkinOn(teams[WeaponCatalog.TeamCt], menu.Knife, option.Def, option.Paint),
                        Do = new CardAction
                        {
                            Kind = CardActionKind.WearSkin,
                            Name = menu.Knife ?? "",
                            Def = option.Def,
                            Paint = option.Paint,
                            Stock = stock
                        }
                    });
                }

                return cards;

            case MenuKind.Weapons:
                foreach (LockerOption option in menu.Options)
                {
                    int paint = teams[side].PaintOf(option.Def);
                    bool skinned = paint > 0;
                    int rarity = skinned ? Math.Max(1, _items.SkinRarity(option.Def, paint)) : 0;

                    cards.Add(new LockerCard
                    {
                        Name = option.Text,
                        Sub = skinned
                            ? _items.PaintName(paint, _text.Russian) ?? _text.RarityName(RarityKind.Weapon, rarity)
                            : _text.Get("default"),
                        Art = (skinned ? _icons.PictureOf(option.Def, paint) : null) ?? _icons.ClassOf(option.Text),
                        Rarity = rarity,
                        Stock = !skinned,
                        EquippedT = teams[WeaponCatalog.TeamT].PaintOf(option.Def) > 0,
                        EquippedCt = teams[WeaponCatalog.TeamCt].PaintOf(option.Def) > 0,
                        Do = new CardAction { Kind = CardActionKind.OpenFinishes, Name = option.Text }
                    });
                }

                return cards;

            case MenuKind.Gloves:
                foreach (LockerOption option in menu.Options)
                {
                    if (option.Stock)
                    {
                        cards.Add(new LockerCard
                        {
                            Name = option.Text,
                            Sub = _text.Get("stock"),
                            Art = side == WeaponCatalog.TeamCt ? "gl-def-ct" : "gl-def-t",
                            Stock = true,
                            EquippedT = teams[WeaponCatalog.TeamT].GloveDef == 0,
                            EquippedCt = teams[WeaponCatalog.TeamCt].GloveDef == 0,
                            Do = new CardAction { Kind = CardActionKind.WearGlove, Def = 0, Paint = 0, Stock = true }
                        });
                        continue;
                    }

                    TextTools.SplitBar(option.Text.Replace("★", "").Trim(), out string head, out string tail);
                    cards.Add(new LockerCard
                    {
                        Name = tail.Length > 0 ? tail : head,
                        Sub = "★ " + _text.RarityName(RarityKind.Item, LockerConstants.CovertRarity),
                        Art = _icons.ClassOf(option.Text) ?? _icons.ClassOf(_items.GloveName(option.Def, option.Paint)),
                        Rarity = LockerConstants.CovertRarity,
                        EquippedT = teams[WeaponCatalog.TeamT].GloveDef == option.Def && teams[WeaponCatalog.TeamT].PaintOf(option.Def) == option.Paint,
                        EquippedCt = teams[WeaponCatalog.TeamCt].GloveDef == option.Def && teams[WeaponCatalog.TeamCt].PaintOf(option.Def) == option.Paint,
                        Do = new CardAction { Kind = CardActionKind.WearGlove, Def = option.Def, Paint = option.Paint }
                    });
                }

                return cards;

            case MenuKind.Music:
                foreach (LockerOption option in menu.Options)
                {
                    bool none = option.ItemId <= 0;
                    _items.Music.TryGetValue(option.ItemId, out NamedEntry? entry);
                    TextTools.SplitMusic(option.Text, out string artist, out string song);

                    cards.Add(new LockerCard
                    {
                        Name = none ? _text.Get("none") : song,
                        Sub = none ? _text.Get("default") : artist,
                        Art = none ? "mk-none" : entry?.Art,
                        Rarity = none ? 0 : 3,
                        Stock = none,
                        EquippedT = teams[WeaponCatalog.TeamT].Music == option.ItemId,
                        EquippedCt = teams[WeaponCatalog.TeamCt].Music == option.ItemId,
                        Do = new CardAction { Kind = CardActionKind.WearMusic, ItemId = option.ItemId, Stock = none }
                    });
                }

                return cards;

            case MenuKind.Pins:
                foreach (LockerOption option in menu.Options)
                {
                    bool none = option.ItemId <= 0;
                    _items.Pins.TryGetValue(option.ItemId, out NamedEntry? entry);
                    int rarity = none ? 0 : Math.Max(1, entry?.Rarity ?? 3);

                    cards.Add(new LockerCard
                    {
                        Name = none ? _text.Get("none") : option.Text,
                        Sub = none ? _text.Get("default") : _text.RarityName(RarityKind.Item, rarity),
                        Art = none ? "pn-none" : entry?.Art,
                        Rarity = rarity,
                        Stock = none,
                        EquippedT = teams[WeaponCatalog.TeamT].Pin == option.ItemId,
                        EquippedCt = teams[WeaponCatalog.TeamCt].Pin == option.ItemId,
                        Do = new CardAction { Kind = CardActionKind.WearPin, ItemId = option.ItemId, Stock = none }
                    });
                }

                return cards;

            default:
                foreach (LockerOption option in menu.Options)
                {
                    cards.Add(new LockerCard
                    {
                        Name = option.Text,
                        Art = _icons.ClassOf(option.Text),
                        Do = new CardAction { Kind = CardActionKind.None }
                    });
                }

                return cards;
        }
    }

    private static bool SkinOn(TeamLoadout team, string? knife, int def, int paint)
    {
        if (knife != null && KnifeOf(team) != knife)
        {
            return false;
        }

        int current = team.PaintOf(def);
        return paint > 0 ? current == paint : current <= 0;
    }

    private static TeamLoadout[] Teams(SlotState state)
    {
        return state.Teams ??= new[] { new TeamLoadout(), new TeamLoadout() };
    }

    private static string KnifeOf(TeamLoadout team)
    {
        return team.Knife ?? "Default Knife";
    }

    /* The equipped knife stands first, the rest follow in the table order. */
    private static int[] KnifeOrder(SlotState state)
    {
        string equipped = KnifeOf(Teams(state)[state.TeamIndex]);
        int first = Math.Max(0, Array.IndexOf(LockerConstants.KnifeNames, equipped));

        List<int> order = new(LockerConstants.KnifeBase.Length) { first };
        order.AddRange(LockerConstants.KnifeBase.Where(k => k != first));
        return order.ToArray();
    }
}
