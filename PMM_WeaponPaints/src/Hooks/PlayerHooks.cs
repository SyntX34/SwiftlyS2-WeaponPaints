using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using PMM_WeaponPaints.Core;
using PMM_WeaponPaints.Locker;
using PMM_WeaponPaints.Services;

namespace PMM_WeaponPaints.Hooks;

/*
  The three things the locker listens to: a click on the layout, a player
  leaving and a player switching sides. The first one drives the window, the
  other two make sure nothing is left open for the next player.
*/
public sealed class PlayerHooks
{
    private readonly ISwiftlyCore _core;
    private readonly LockerStore _store;
    private readonly LockerWindow _window;
    private readonly DebugLog _debug;
    private readonly List<Guid> _handles = new();
    private EventDelegates.OnCustomHudClicked? _clicked;

    public PlayerHooks(ISwiftlyCore core, LockerStore store, LockerWindow window, DebugLog debug)
    {
        _core = core;
        _store = store;
        _window = window;
        _debug = debug;
    }

    public void Attach()
    {
        _clicked = OnCustomHudClicked;
        _core.Event.OnCustomHudClicked += _clicked;

        _handles.Add(_core.GameEvent.HookPre<EventPlayerDisconnect>(OnDisconnect));
        _handles.Add(_core.GameEvent.HookPre<EventPlayerTeam>(OnTeamChange));
    }

    public void Detach()
    {
        if (_clicked != null)
        {
            _core.Event.OnCustomHudClicked -= _clicked;
            _clicked = null;
        }

        foreach (Guid handle in _handles)
        {
            _core.GameEvent.Unhook(handle);
        }

        _handles.Clear();
    }

    private void OnCustomHudClicked(IOnCustomHudClickedEvent @event)
    {
        IPlayer? player = _core.PlayerManager.GetPlayer(@event.PlayerId);
        if (player == null || !_store.IsOpen(player.Slot))
        {
            return;
        }

        CCSCustomHudLayout layout = @event.CustomHudLayout;
        _window.OnClick(player, layout, @event.ButtonId);
    }

    private HookResult OnDisconnect(EventPlayerDisconnect @event)
    {
        IPlayer? player = @event.UserIdPlayer;
        if (player != null && player.Slot >= 0 && player.Slot < LockerConstants.SlotCount)
        {
            _window.Hide(player);
            _store.Reset(player.Slot);
            _debug.Write(null, $"slot {player.Slot} reset after a disconnect");
        }

        return HookResult.Continue;
    }

    private HookResult OnTeamChange(EventPlayerTeam @event)
    {
        IPlayer? player = @event.UserIdPlayer;
        if (player == null || player.IsFakeClient || !_store.IsOpen(player.Slot))
        {
            return HookResult.Continue;
        }

        _debug.Write(player, $"team event {(@event.OldTeam)} to {(@event.Team)}, the locker closes");
        _core.Scheduler.NextTick(() =>
        {
            if (player.IsValid)
            {
                _window.Hide(player);
            }
        });

        return HookResult.Continue;
    }
}
