using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using PMM_WeaponPaints.Catalog;
using PMM_WeaponPaints.Core;
using PMM_WeaponPaints.Locker;
using PMM_WeaponPaints.Services;

namespace PMM_WeaponPaints.View;

/*
  Draws one player's page: the root classes, the left column, the cards and
  the pager. Every value goes through HudWriter, so a redraw only sends what
  actually changed on the layout.
*/
public sealed class GridRenderer
{
    private readonly LayoutFactory _layout;
    private readonly HudWriter _hud;
    private readonly CardFactory _cards;
    private readonly HomeSummary _summary;
    private readonly TriggerBook _triggers;
    private readonly LockerText _text;
    private readonly ItemLibrary _items;
    private readonly DebugLog _debug;

    public GridRenderer(LayoutFactory layout, HudWriter hud, CardFactory cards, HomeSummary summary,
        TriggerBook triggers, LockerText text, ItemLibrary items, DebugLog debug)
    {
        _layout = layout;
        _hud = hud;
        _cards = cards;
        _summary = summary;
        _triggers = triggers;
        _text = text;
        _items = items;
        _debug = debug;
    }

    public void Draw(IPlayer player, SlotState state)
    {
        CCSCustomHudLayout? layout = _layout.Layout;
        if (layout == null || !layout.IsValid)
        {
            return;
        }

        int slot = player.Slot;
        state.TeamIndex = PlayerTools.SideOf(player);
        int view = state.View;

        List<LockerCard> cards = _cards.Build(player, state);
        state.Cards = cards;

        int pages = Pages(state);
        int page = Math.Clamp(state.Page, 0, pages - 1);
        state.Page = page;

        for (int i = 0; i < LockerConstants.Views.Length; i++)
        {
            _hud.Class(state, layout, slot, "wp-root", LockerConstants.Views[i], i == view);
        }

        int cat = Category(state);
        for (int i = 0; i < LockerConstants.Cats.Length; i++)
        {
            _hud.Class(state, layout, slot, "wp-root", LockerConstants.Cats[i], i == cat);
        }

        bool sideFirst = state.SidePage == 0;
        _hud.Class(state, layout, slot, "wp-root", "side-p0", sideFirst);
        _hud.Class(state, layout, slot, "wp-root", "side-p1", !sideFirst);
        _hud.Class(state, layout, slot, "wp-side-prev", "wp-dim", sideFirst);
        _hud.Class(state, layout, slot, "wp-side-next", "wp-dim", !sideFirst);
        _hud.Class(state, layout, slot, "wp-side-num-0", "is-cur", sideFirst);
        _hud.Class(state, layout, slot, "wp-side-num-1", "is-cur", !sideFirst);
        _hud.Class(state, layout, slot, "wp-root", "single-page", pages <= 1);
        _hud.Class(state, layout, slot, "wp-root", "multi-page", pages > 1);
        _hud.Class(state, layout, slot, "wp-root", "team-t", state.TeamIndex == WeaponCatalog.TeamT);
        _hud.Class(state, layout, slot, "wp-root", "team-ct", state.TeamIndex == WeaponCatalog.TeamCt);
        _hud.Class(state, layout, slot, "wp-root", "lang-ru", _text.Russian);

        foreach ((string id, string action) in _triggers.SectionButtons)
        {
            _hud.Class(state, layout, slot, id, "is-cur", string.Equals(action, state.Active, StringComparison.OrdinalIgnoreCase));
        }

        for (int c = LockerConstants.CatKnife; c <= LockerConstants.CatPins; c++)
        {
            (string label, string? art) = _summary.For(state, c);
            string key = LockerConstants.CatKeys[c];
            _hud.Var(state, layout, slot, "wp-side-" + key, "side_" + key, label);
            _hud.Var(state, layout, slot, "wp-home-" + key, "side_" + key, label);
            _hud.Art(state, layout, slot, "wp-sw-" + key, ref state.SideArt[c], art);
            _hud.Art(state, layout, slot, "wp-tile-art-" + key, ref state.TileArt[c], art);
        }

        Header(state, layout, slot, cards.Count);

        // Five cards fill a row. The empty cells of the last row stay as ghosts so the row keeps its widths.
        int onPage = Math.Clamp(cards.Count - page * LockerConstants.LivePage, 0, LockerConstants.LivePage);
        int lastRow = onPage > 0 ? (onPage - 1) / LockerConstants.RowSize : -1;
        for (int i = 0; i < LockerConstants.LivePage; i++)
        {
            int index = page * LockerConstants.LivePage + i;
            LockerCard? card = index < cards.Count ? cards[index] : null;
            Card(state, layout, slot, i, card, card == null && i / LockerConstants.RowSize == lastRow);
        }

        Pager(state, layout, slot, page, pages);
        _debug.Write(player, $"draw view={LockerConstants.Views[view]} cat={LockerConstants.Cats[cat]} menu='{state.Menu?.Title}' cards={cards.Count} page={page + 1}/{pages}");
    }

    public int Pages(SlotState state)
    {
        int count = state.Cards?.Count ?? 0;
        return Math.Max(1, (count + LockerConstants.LivePage - 1) / LockerConstants.LivePage);
    }

    private void Card(SlotState state, CCSCustomHudLayout layout, int slot, int i, LockerCard? card, bool ghost)
    {
        string panel = "wp-live-" + i;
        _hud.Class(state, layout, slot, panel, "wp-off", card == null && !ghost);
        _hud.Class(state, layout, slot, panel, "wp-ghost", ghost);
        if (card == null)
        {
            return;
        }

        string artPanel = "wp-live-art-" + i;
        _hud.Art(state, layout, slot, artPanel, ref state.Icon[i], card.Art);
        _hud.Class(state, layout, slot, artPanel, "wp-noart", string.IsNullOrEmpty(card.Art));

        bool equipped = state.TeamIndex == WeaponCatalog.TeamCt ? card.EquippedCt : card.EquippedT;
        _hud.Class(state, layout, slot, panel, "is-eq", equipped);
        _hud.Class(state, layout, slot, panel, "is-stock", card.Stock);
        _hud.Class(state, layout, slot, panel, "on-t", card.EquippedT);
        _hud.Class(state, layout, slot, panel, "on-ct", card.EquippedCt);

        string? rarity = card.Rarity > 0 && !card.Stock ? "rar-" + card.Rarity : null;
        _hud.Art(state, layout, slot, panel, ref state.Rarity[i], rarity);

        _hud.Var(state, layout, slot, panel, "live" + i, card.Name);
        _hud.Var(state, layout, slot, panel, "livesub" + i, card.Sub);
    }

    private void Header(SlotState state, CCSCustomHudLayout layout, int slot, int count)
    {
        string title;
        string crumb;
        string hint;

        switch (state.View)
        {
            case LockerConstants.ViewHome:
                title = _text.Get("title.home");
                crumb = _text.Get("crumb.home");
                hint = _text.Get("hint.home");
                break;

            case LockerConstants.ViewKnife:
                title = _text.Get("cat.knife");
                crumb = _summary.For(state, LockerConstants.CatKnife).Label;
                hint = _text.Get("hint.knife", LockerConstants.KnifeNames.Length);
                break;

            case LockerConstants.ViewAgents:
                title = _text.Get("cat.agents");
                crumb = _text.Get("crumb.team", TextTools.TeamName(state.TeamIndex));
                hint = _text.Get("hint.agents", Math.Max(0, count - 1), TextTools.TeamName(state.TeamIndex));
                break;

            case LockerConstants.ViewGloves:
                title = _text.Get("cat.gloves");
                crumb = _summary.For(state, LockerConstants.CatGloves).Label;
                hint = _text.Get("hint.gloves", _items.GloveTypes.Count);
                break;

            default:
                LiveHeader(state, count, out title, out crumb, out hint);
                break;
        }

        _hud.Var(state, layout, slot, "wp-title", "wp_title", title);
        _hud.Var(state, layout, slot, "wp-crumb", "wp_crumb", crumb);
        _hud.Var(state, layout, slot, "wp-hint", "wp_hint", hint);
    }

    private void LiveHeader(SlotState state, int count, out string title, out string crumb, out string hint)
    {
        LockerMenu? menu = state.Menu;
        if (menu == null)
        {
            title = "";
            crumb = "";
            hint = "";
            return;
        }

        switch (menu.Kind)
        {
            case MenuKind.Skins:
                title = menu.Title;
                crumb = _text.Get("crumb.finishes", menu.Knife != null ? _text.Get("cat.knife") : _summary.GroupTitleOf(menu.Title));
                hint = _text.Get("hint.finishes", count);
                break;

            case MenuKind.Weapons:
            {
                string group = state.Filter != null ? _summary.GroupTitle(state.Filter) : _text.Get("cat.weapons");
                title = group;
                crumb = _text.Get("crumb.weapons", group);
                hint = _text.Get("hint.weapons");
                break;
            }

            case MenuKind.Gloves:
                title = menu.Title;
                crumb = _text.Get("crumb.gloves", title);
                hint = _text.Get("hint.finishes", count);
                break;

            case MenuKind.Music:
                title = _text.Get("cat.music");
                crumb = _summary.For(state, LockerConstants.CatMusic).Label;
                hint = _text.Get("hint.equip", count);
                break;

            case MenuKind.Pins:
                title = _text.Get("cat.pins");
                crumb = _summary.For(state, LockerConstants.CatPins).Label;
                hint = _text.Get("hint.pins", Math.Max(0, count - 1));
                break;

            default:
                title = menu.Title;
                crumb = _text.Get("cat.locker");
                hint = _text.Get("hint.equip", count);
                break;
        }
    }

    private void Pager(SlotState state, CCSCustomHudLayout layout, int slot, int page, int pages)
    {
        int start = pages > LockerConstants.PagerButtons
            ? Math.Clamp(page - LockerConstants.PagerButtons / 2, 0, pages - LockerConstants.PagerButtons)
            : 0;

        state.PagerStart = start;
        for (int i = 0; i < LockerConstants.PagerButtons; i++)
        {
            int number = start + i;
            bool shown = pages > 1 && number < pages;
            string panel = "wp-num-" + i;
            _hud.Class(state, layout, slot, panel, "wp-off", !shown);
            _hud.Class(state, layout, slot, panel, "is-cur", shown && number == page);
            _hud.Var(state, layout, slot, panel, "num" + i, (number + 1).ToString());
        }

        _hud.Class(state, layout, slot, "wp-prev", "wp-dim", page <= 0);
        _hud.Class(state, layout, slot, "wp-next", "wp-dim", page >= pages - 1);
    }

    private int Category(SlotState state)
    {
        switch (state.View)
        {
            case LockerConstants.ViewHome:
                return LockerConstants.CatLoadout;
            case LockerConstants.ViewKnife:
                return LockerConstants.CatKnife;
            case LockerConstants.ViewAgents:
                return LockerConstants.CatAgents;
            case LockerConstants.ViewGloves:
                return LockerConstants.CatGloves;
        }

        return state.Active switch
        {
            "weapons:pistols" => LockerConstants.CatPistols,
            "weapons:rifles" => LockerConstants.CatRifles,
            "weapons:snipers" => LockerConstants.CatSnipers,
            "weapons:smg" => LockerConstants.CatSmg,
            "weapons:heavy" => LockerConstants.CatHeavy,
            "open:music" => LockerConstants.CatMusic,
            "open:agents" => LockerConstants.CatAgents,
            "open:pins" => LockerConstants.CatPins,
            "open:knife" => LockerConstants.CatKnife,
            "open:gloves" => LockerConstants.CatGloves,
            _ => LockerConstants.CatNone
        };
    }
}
