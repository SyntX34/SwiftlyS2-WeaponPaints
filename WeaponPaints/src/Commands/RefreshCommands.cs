using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using WeaponPaints.Config;
using WeaponPaints.Skin;

namespace WeaponPaints.Commands;

/*
  The refresh command. The locker runs it for a player right after a choice is
  saved, and a player can type it to pull the loadout from the database again.
*/
public sealed class RefreshCommands
{
    private readonly ISwiftlyCore _core;
    private readonly LoadoutService _loadout;
    private readonly PluginSettings _settings;
    private readonly List<Guid> _registered = new();

    public RefreshCommands(ISwiftlyCore core, LoadoutService loadout, PluginSettings settings)
    {
        _core = core;
        _loadout = loadout;
        _settings = settings;
    }

    /* The locker always calls css_wp, so that name is fixed. The config can add a second one. */
    public const string FixedCommand = "css_wp";

    public void Register()
    {
        _registered.Add(_core.Command.RegisterCommand(FixedCommand, Run, registerRaw: true, permission: "",
            helpText: "Read the WeaponPaints loadout again and put it on"));

        if (!string.Equals(_settings.RefreshCommand, FixedCommand, StringComparison.OrdinalIgnoreCase))
        {
            _registered.Add(_core.Command.RegisterCommand(_settings.RefreshCommand, Run, registerRaw: true, permission: "",
                helpText: "Read the WeaponPaints loadout again and put it on"));
        }
    }

    public void Unregister()
    {
        foreach (Guid id in _registered)
        {
            _core.Command.UnregisterCommand(id);
        }

        _registered.Clear();
    }

    private void Run(ICommandContext context)
    {
        IPlayer? player = context.Sender;
        if (player == null)
        {
            context.Reply("This command only works for a player.");
            return;
        }

        _core.Logger.LogDebug("{Player} asks for a loadout refresh", player.Name);
        _loadout.Refresh(player);
    }
}
