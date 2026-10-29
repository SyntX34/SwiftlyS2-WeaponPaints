using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using WeaponPaints.Config;
using WeaponPaints.Skin;

namespace WeaponPaints.Hooks;

/*
  Two moments matter: a player got a body again, so the loadout goes back on,
  and a player left, so the cached loadout can go.
*/
public sealed class GameHooks
{
    private readonly ISwiftlyCore _core;
    private readonly LoadoutService _loadout;
    private readonly PluginSettings _settings;
    private readonly List<Guid> _handles = new();

    public GameHooks(ISwiftlyCore core, LoadoutService loadout, PluginSettings settings)
    {
        _core = core;
        _loadout = loadout;
        _settings = settings;
    }

    public void Attach()
    {
        _handles.Add(_core.GameEvent.HookPost<EventPlayerSpawn>(OnSpawn));
        _handles.Add(_core.GameEvent.HookPre<EventPlayerDisconnect>(OnDisconnect));
    }

    public void Detach()
    {
        foreach (Guid handle in _handles)
        {
            _core.GameEvent.Unhook(handle);
        }

        _handles.Clear();
    }

    private HookResult OnSpawn(EventPlayerSpawn @event)
    {
        if (!_settings.ApplyOnSpawn)
        {
            return HookResult.Continue;
        }

        IPlayer? player = @event.UserIdPlayer;
        if (player == null || player.IsFakeClient)
        {
            return HookResult.Continue;
        }

        int slot = player.Slot;
        // The weapons of the new body are handed out a moment after the event.
        _core.Scheduler.DelayBySeconds(0.2f, () =>
        {
            IPlayer? current = _core.PlayerManager.GetPlayer(slot);
            if (current != null && current.IsValid)
            {
                _loadout.Refresh(current);
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnDisconnect(EventPlayerDisconnect @event)
    {
        IPlayer? player = @event.UserIdPlayer;
        if (player != null)
        {
            _loadout.Forget(player.Slot);
        }

        return HookResult.Continue;
    }
}
