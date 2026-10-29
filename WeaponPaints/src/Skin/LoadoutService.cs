using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using WeaponPaints.Catalog;
using WeaponPaints.Data;

namespace WeaponPaints.Skin;

/*
  Keeps the loadout of every player in memory and puts it on the pawn when
  asked. The database call runs in the background and comes back on the game
  thread, so a slow database never stalls a tick.
*/
public sealed class LoadoutService
{
    private readonly ISwiftlyCore _core;
    private readonly ILogger _logger;
    private readonly LoadoutStore _store;
    private readonly SkinApplier _applier;
    private readonly Dictionary<int, TeamLoadout[]> _cache = new();

    public LoadoutService(ISwiftlyCore core, ILogger logger, LoadoutStore store, SkinApplier applier)
    {
        _core = core;
        _logger = logger;
        _store = store;
        _applier = applier;
    }

    public TeamLoadout[]? Cached(IPlayer player)
    {
        return _cache.TryGetValue(player.Slot, out TeamLoadout[]? loadout) ? loadout : null;
    }

    /* Reads the loadout again and hands it to the applier. */
    public void Refresh(IPlayer player)
    {
        int slot = player.Slot;
        string steam = player.SteamID.ToString(System.Globalization.CultureInfo.InvariantCulture);

        Task.Run(() =>
        {
            TeamLoadout[]? loadout = null;
            try
            {
                loadout = _store.Load(steam);
            }
            catch (Exception ex)
            {
                _logger.LogError("The loadout of {Player} was not read: {Message}", steam, ex.Message);
            }

            _core.Scheduler.NextTick(() =>
            {
                IPlayer? current = _core.PlayerManager.GetPlayer(slot);
                if (loadout == null || current == null)
                {
                    return;
                }

                _cache[slot] = loadout;
                Apply(current);
            });
        });
    }

    /* Puts the cached loadout back on the player. */
    public void Apply(IPlayer player)
    {
        TeamLoadout[]? loadout = Cached(player);
        if (loadout == null)
        {
            return;
        }

        int side = player.Controller.TeamNum == 3 ? WeaponCatalog.TeamCt : WeaponCatalog.TeamT;
        _applier.Apply(player, loadout[side]);
    }

    public void Forget(int slot)
    {
        _cache.Remove(slot);
    }

    public void ForgetAll()
    {
        _cache.Clear();
    }
}
