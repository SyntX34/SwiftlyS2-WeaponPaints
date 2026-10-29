using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using PMM_WeaponPaints.Catalog;
using PMM_WeaponPaints.Config;
using PMM_WeaponPaints.Core;
using PMM_WeaponPaints.Data;
using PMM_WeaponPaints.Equip;
using PMM_WeaponPaints.Services;
using PMM_WeaponPaints.View;

namespace PMM_WeaponPaints.Locker;

/*
  The window itself. It opens and closes, keeps the back stack, runs the
  trigger actions the layout sends and saves what the player picks.

  Saving always happens on a background thread, because the database call can
  take a moment and the game thread must not wait for it.
*/
public sealed class LockerWindow
{
    private readonly ISwiftlyCore _core;
    private readonly LockerStore _store;
    private readonly PluginSettings _settings;
    private readonly DebugLog _debug;
    private readonly TriggerBook _triggers;
    private readonly MenuBook _menus;
    private readonly ItemLibrary _items;
    private readonly LockerText _text;
    private readonly WpDatabase _database;
    private readonly EquipService _equip;
    private readonly LayoutFactory _layout;
    private readonly GridRenderer _grid;

    public LockerWindow(ISwiftlyCore core, LockerStore store, PluginSettings settings, DebugLog debug,
        TriggerBook triggers, MenuBook menus, ItemLibrary items, LockerText text, WpDatabase database,
        EquipService equip, LayoutFactory layout, GridRenderer grid)
    {
        _core = core;
        _store = store;
        _settings = settings;
        _debug = debug;
        _triggers = triggers;
        _menus = menus;
        _items = items;
        _text = text;
        _database = database;
        _equip = equip;
        _layout = layout;
        _grid = grid;
    }

    public bool IsOpen(IPlayer player)
    {
        return _store.IsOpen(player.Slot);
    }

    /* The command entry point. A second call while the window is open closes it. */
    public void Toggle(IPlayer player)
    {
        if (!PlayerTools.Fits(player))
        {
            return;
        }

        SlotState state = _store.Get(player.Slot);
        if (state.Open)
        {
            Hide(player);
            return;
        }

        if (!PlayerTools.OnASide(player))
        {
            PlayerTools.Say(player, _text.Get("chat.team"));
            return;
        }

        if (!_database.Gate.Ready && !_database.Gate.TryPing())
        {
            PlayerTools.Say(player, _text.Get("chat.nodb"));
            return;
        }

        if (!_layout.Ensure())
        {
            PlayerTools.Say(player, _text.Get("chat.layout"));
            return;
        }

        state.Clear();
        state.Open = true;
        int token = ++state.Token;
        PlayerTools.Say(player, _text.Get("chat.opened"));
        _debug.Write(player, "locker opened, reading the loadout");

        string steam = PlayerTools.SteamOf(player);
        Task.Run(() =>
        {
            TeamLoadout[]? teams = null;
            string? failure = null;
            try
            {
                teams = _database.Load(steam);
            }
            catch (Exception ex)
            {
                failure = ex.Message;
            }

            _core.Scheduler.NextTick(() =>
            {
                SlotState current = _store.Get(player.Slot);
                if (!player.IsValid || current.Token != token || !current.Open)
                {
                    return;
                }

                if (teams == null)
                {
                    current.Open = false;
                    _debug.Write(player, "loadout was not read: " + failure);
                    PlayerTools.Say(player, _text.Get("chat.nodb"));
                    return;
                }

                current.Teams = teams;
                _debug.Write(player, $"loadout ready: T knife={teams[WeaponCatalog.TeamT].Knife ?? "default"} CT knife={teams[WeaponCatalog.TeamCt].Knife ?? "default"}");

                Later(_settings.OpenDelay, () =>
                {
                    if (!player.IsValid || current.Token != token || !current.Open)
                    {
                        return;
                    }

                    Show(player);
                    CaptureInput(player, token);
                });
            });
        });
    }

    public void Hide(IPlayer player)
    {
        if (!PlayerTools.Fits(player))
        {
            return;
        }

        SlotState state = _store.Get(player.Slot);
        state.Open = false;
        state.Token++;
        state.Menu = null;
        state.Filter = null;
        state.Cards = null;
        state.Back.Clear();
        _debug.Write(player, "locker hidden");

        CCSCustomHudLayout? layout = _layout.Layout;
        if (layout == null || !layout.IsValid)
        {
            return;
        }

        layout.SetInputCaptureEnabledForPlayer(player.Slot, false);
        layout.SetHasClassForPlayer(player.Slot, "wp-root", "wp-hidden", EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass);
    }

    public void Close(IPlayer player)
    {
        Hide(player);
    }

    /* A click on the layout. Buttons first, then cards. */
    public void OnClick(IPlayer player, CCSCustomHudLayout layout, string buttonId)
    {
        if (!PlayerTools.Fits(player))
        {
            return;
        }

        SlotState state = _store.Get(player.Slot);
        if (!state.Open || _layout.Layout == null || layout.Address != _layout.Layout.Address)
        {
            return;
        }

        _debug.Write(player, $"click '{buttonId}' view={state.View} page={state.Page}");

        if (buttonId == "wp-side-prev" || buttonId == "wp-side-num-0")
        {
            state.SidePage = 0;
            Draw(player, state);
            return;
        }

        if (buttonId == "wp-side-next" || buttonId == "wp-side-num-1")
        {
            state.SidePage = 1;
            Draw(player, state);
            return;
        }

        if (_triggers.TryAction(buttonId, out string? action))
        {
            RunAction(player, state, action);
            return;
        }

        if (buttonId.StartsWith("wp-live-", StringComparison.Ordinal) && int.TryParse(buttonId["wp-live-".Length..], out int cell))
        {
            List<LockerCard>? cards = state.Cards;
            int index = state.Page * LockerConstants.LivePage + cell;
            if (cards != null && index >= 0 && index < cards.Count)
            {
                TakeCard(player, state, cards[index]);
            }

            return;
        }

        if (buttonId.StartsWith("wp-num-", StringComparison.Ordinal) && int.TryParse(buttonId["wp-num-".Length..], out int number))
        {
            Turn(player, state, state.PagerStart + number);
        }
    }

    /* Runs the action a button carries, for example open:knife or weapons:pistols. */
    public void RunAction(IPlayer player, SlotState state, string action)
    {
        int colon = action.IndexOf(':');
        string verb = (colon < 0 ? action : action[..colon]).Trim().ToLowerInvariant();
        string arg = colon >= 0 ? action[(colon + 1)..].Trim() : "";
        _debug.Write(player, $"action '{action}'");

        switch (verb)
        {
            case "close":
                Hide(player);
                break;

            case "back":
                Back(player, state);
                break;

            case "home":
                state.Filter = null;
                state.SidePage = 0;
                GoHome(player, state);
                break;

            case "side":
                state.SidePage = arg is "next" or "1" ? 1 : 0;
                Draw(player, state);
                break;

            case "open":
                OpenSection(player, state, arg.ToLowerInvariant());
                break;

            case "weapons":
            {
                WeaponGroup? group = _triggers.GroupFor(arg);
                if (group == null)
                {
                    _debug.Write(player, $"unknown weapon group '{arg}'");
                    break;
                }

                state.Filter = group;
                OpenMenu(player, state, _menus.WeaponList(group), action);
                break;
            }

            case "page":
                if (state.View != LockerConstants.ViewHome)
                {
                    Turn(player, state, state.Page + (arg.Equals("prev", StringComparison.OrdinalIgnoreCase) ? -1 : 1));
                }

                break;

            case "cmd":
                RunPlayerCommand(player, arg);
                break;

            default:
                _debug.Write(player, $"unknown action '{action}'");
                break;
        }
    }

    public void Back(IPlayer player, SlotState state)
    {
        if (state.Back.Count == 0)
        {
            Hide(player);
            return;
        }

        NavFrame frame = state.Back[^1];
        state.Back.RemoveAt(state.Back.Count - 1);
        state.View = frame.View;
        state.Menu = frame.Menu;
        state.Page = frame.Page;
        state.GloveType = frame.GloveType;
        state.SidePage = frame.SidePage;
        state.Active = frame.Active;
        Draw(player, state);
    }

    public void GoHome(IPlayer player, SlotState state)
    {
        state.Back.Clear();
        state.View = LockerConstants.ViewHome;
        state.Menu = null;
        state.Filter = null;
        state.GloveType = -1;
        state.Page = 0;
        state.Active = "home";
        state.SidePage = 0;
        Draw(player, state);
    }

    public void Turn(IPlayer player, SlotState state, int target)
    {
        if (state.View == LockerConstants.ViewHome)
        {
            return;
        }

        state.Page = Math.Clamp(target, 0, Math.Max(0, _grid.Pages(state) - 1));
        Draw(player, state);
    }

    public void Show(IPlayer player)
    {
        CCSCustomHudLayout? layout = _layout.Layout;
        if (layout == null || !layout.IsValid)
        {
            return;
        }

        Draw(player, _store.Get(player.Slot));
        layout.SetHasClassForPlayer(player.Slot, "wp-root", "wp-hidden", EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass);
    }

    private void Draw(IPlayer player, SlotState state)
    {
        if (!state.Open)
        {
            return;
        }

        _grid.Draw(player, state);
    }

    private void CaptureInput(IPlayer player, int token)
    {
        Later(_settings.InputDelay, () =>
        {
            CCSCustomHudLayout? layout = _layout.Layout;
            SlotState state = _store.Get(player.Slot);
            if (layout != null && layout.IsValid && player.IsValid && state.Open && state.Token == token)
            {
                layout.SetInputCaptureEnabledForPlayer(player.Slot, true);
                _debug.Write(player, "input capture on");
            }
        });
    }

    private void OpenSection(IPlayer player, SlotState state, string section)
    {
        state.Filter = null;

        switch (section)
        {
            case "knife":
                Push(state);
                state.View = LockerConstants.ViewKnife;
                state.Menu = null;
                state.Page = 0;
                state.Active = "open:knife";
                state.SidePage = 0;
                Draw(player, state);
                break;

            case "gloves":
                Push(state);
                state.View = LockerConstants.ViewGloves;
                state.Menu = null;
                state.Page = 0;
                state.GloveType = -1;
                state.Active = "open:gloves";
                state.SidePage = 0;
                Draw(player, state);
                break;

            case "agents":
                Push(state);
                state.View = LockerConstants.ViewAgents;
                state.Menu = null;
                state.Page = 0;
                state.Active = "open:agents";
                state.SidePage = 0;
                Draw(player, state);
                break;

            case "music":
                OpenMenu(player, state, _menus.MusicList(), "open:music");
                break;

            case "pins":
            case "coins":
                OpenMenu(player, state, _menus.PinList(), "open:pins");
                break;

            case "skins":
            case "weapons":
            case "pistols":
                OpenMenu(player, state, _menus.WeaponList(null), "open:skins");
                break;

            default:
                _debug.Write(player, $"unknown section '{section}'");
                break;
        }
    }

    private void OpenMenu(IPlayer player, SlotState state, LockerMenu menu, string active)
    {
        Push(state);
        state.Menu = menu;
        state.View = LockerConstants.ViewLive;
        state.Page = 0;
        state.Active = active;
        state.SidePage = SideFor(active);
        Draw(player, state);
    }

    private void Push(SlotState state)
    {
        state.Back.Add(new NavFrame
        {
            View = state.View,
            Menu = state.Menu,
            Page = state.Page,
            GloveType = state.GloveType,
            SidePage = state.SidePage,
            Active = state.Active
        });
    }

    private static int SideFor(string active)
    {
        return active is "weapons:smg" or "weapons:heavy" or "open:agents" or "open:music" or "open:pins" ? 1 : 0;
    }

    private void RunPlayerCommand(IPlayer player, string command)
    {
        if (!TextTools.IsSafeCommand(command))
        {
            return;
        }

        _core.Scheduler.NextTick(() =>
        {
            if (player.IsValid)
            {
                player.ExecuteCommand(command);
                _debug.Write(player, $"ran '{command}' for the player");
            }
        });
    }

    /* A card click turns into the one thing that card stands for. */
    private void TakeCard(IPlayer player, SlotState state, LockerCard card)
    {
        CardAction action = card.Do;
        _debug.Write(player, $"card '{card.Name}' {action.Kind}");

        switch (action.Kind)
        {
            case CardActionKind.PickKnife:
                WearKnife(player, state, action.Name, action.Stock);
                break;

            case CardActionKind.OpenFinishes:
                OpenMenu(player, state, _menus.WeaponFinishes(action.Name), state.Active);
                break;

            case CardActionKind.OpenGloveFinishes:
            {
                GloveType? type = _items.FindGloveType(action.Def);
                if (type == null)
                {
                    break;
                }

                state.GloveType = action.Def;
                OpenMenu(player, state, _menus.GloveFinishes(type), "open:gloves");
                break;
            }

            case CardActionKind.WearSkin:
                WearSkin(player, state, action);
                break;

            case CardActionKind.WearGlove:
                WearGlove(player, state, action.Def, action.Paint);
                break;

            case CardActionKind.WearMusic:
                WearMusic(player, state, action.ItemId);
                break;

            case CardActionKind.WearPin:
                WearPin(player, state, action.ItemId);
                break;

            case CardActionKind.WearAgent:
                WearAgent(player, state, action.Model);
                break;
        }
    }

    private void WearKnife(IPlayer player, SlotState state, string name, bool stock)
    {
        TeamLoadout loadout = Teams(state)[state.TeamIndex];
        loadout.Knife = stock ? null : name;
        Save(PlayerTools.SteamOf(player), () => _equip.SaveKnife(PlayerTools.SteamOf(player), state.TeamIndex, name), player);

        if (stock)
        {
            Draw(player, state);
            return;
        }

        OpenMenu(player, state, _menus.KnifeFinishes(name), "open:knife");
    }

    private void WearSkin(IPlayer player, SlotState state, CardAction action)
    {
        TeamLoadout loadout = Teams(state)[state.TeamIndex];
        if (action.Name.Length > 0)
        {
            loadout.Knife = action.Name;
        }

        if (action.Paint > 0)
        {
            loadout.Paints[action.Def] = action.Paint;
        }
        else
        {
            loadout.Paints.Remove(action.Def);
        }

        int side = state.TeamIndex;
        int def = action.Def;
        int paint = action.Paint;
        string steam = PlayerTools.SteamOf(player);
        Save(steam, () => _equip.SaveSkin(steam, side, def, paint), player);
        Draw(player, state);
    }

    private void WearGlove(IPlayer player, SlotState state, int def, int paint)
    {
        TeamLoadout loadout = Teams(state)[state.TeamIndex];
        int side = state.TeamIndex;
        string steam = PlayerTools.SteamOf(player);

        if (def <= 0)
        {
            loadout.GloveDef = 0;
            loadout.Paints.Remove(0);
            Save(steam, () => _equip.SaveGloves(steam, side, 0, 0), player);
            Draw(player, state);
            return;
        }

        loadout.GloveDef = def;
        loadout.Paints[def] = paint;
        Save(steam, () => _equip.SaveGloves(steam, side, def, paint), player);
        Draw(player, state);
    }

    private void WearMusic(IPlayer player, SlotState state, int id)
    {
        Teams(state)[state.TeamIndex].Music = Math.Max(0, id);
        int side = state.TeamIndex;
        string steam = PlayerTools.SteamOf(player);
        Save(steam, () => _equip.SaveMusic(steam, side, id), player);
        Draw(player, state);
    }

    private void WearPin(IPlayer player, SlotState state, int id)
    {
        Teams(state)[state.TeamIndex].Pin = Math.Max(0, id);
        int side = state.TeamIndex;
        string steam = PlayerTools.SteamOf(player);
        Save(steam, () => _equip.SavePin(steam, side, id), player);
        Draw(player, state);
    }

    private void WearAgent(IPlayer player, SlotState state, string? model)
    {
        if (state.TeamIndex == WeaponCatalog.TeamT && model != null && !model.StartsWith("tm_", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Teams(state)[state.TeamIndex].Agent = model;

        AgentEntry? agent = _items.Agent(model);
        string name = agent == null
            ? _text.Get("default")
            : TextTools.SplitBarName((_text.Russian ? agent.Ru : agent.En) ?? agent.En ?? agent.Model);
        PlayerTools.Say(player, _text.Get("chat.agent", name));

        int side = state.TeamIndex;
        string steam = PlayerTools.SteamOf(player);
        Save(steam, () => _equip.SaveAgent(steam, side, model), player, _text.Get("chat.agent.fail"));
        Draw(player, state);
    }

    private void Save(string steam, Func<bool> write, IPlayer player, string? failure = null)
    {
        Task.Run(() =>
        {
            bool saved;
            try
            {
                saved = write();
            }
            catch (Exception ex)
            {
                _core.Logger.LogError("The choice was not saved: {Message}", ex.Message);
                saved = false;
            }

            if (saved)
            {
                // WeaponPaints puts the new skin on the player, if it is installed.
                _core.Scheduler.NextTick(() =>
                {
                    if (player.IsValid)
                    {
                        player.ExecuteCommand("css_wp");
                    }
                });

                return;
            }

            _core.Scheduler.NextTick(() =>
            {
                if (player.IsValid)
                {
                    PlayerTools.Say(player, failure ?? _text.Get("chat.savefail"));
                }
            });
        });
    }

    private static TeamLoadout[] Teams(SlotState state)
    {
        return state.Teams ??= new[] { new TeamLoadout(), new TeamLoadout() };
    }

    private void Later(float seconds, Action callback)
    {
        if (seconds <= 0f)
        {
            _core.Scheduler.NextTick(callback);
        }
        else
        {
            _core.Scheduler.DelayBySeconds(seconds, callback);
        }
    }
}
