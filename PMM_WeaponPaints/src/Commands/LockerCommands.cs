using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using PMM_WeaponPaints.Config;
using PMM_WeaponPaints.Locker;
using PMM_WeaponPaints.Services;

namespace PMM_WeaponPaints.Commands;

/*
  Registers one css_ command per entry of the Commands list and takes them all
  back on unload. The names are raw, so no sw_ prefix is added.
*/
public sealed class LockerCommands
{
    private readonly ISwiftlyCore _core;
    private readonly LockerWindow _window;
    private readonly PluginSettings _settings;
    private readonly DebugLog _debug;
    private readonly List<Guid> _registered = new();

    public LockerCommands(ISwiftlyCore core, LockerWindow window, PluginSettings settings, DebugLog debug)
    {
        _core = core;
        _window = window;
        _settings = settings;
        _debug = debug;
    }

    public void Register()
    {
        foreach (string name in _settings.Commands)
        {
            string command = "css_" + name;
            _registered.Add(_core.Command.RegisterCommand(command, OpenLocker, registerRaw: true, permission: "",
                helpText: "Open the panorama locker"));
            _debug.Write(null, "command registered: " + command);
        }

        _core.Logger.LogInformation("The locker opens with {Commands}.", string.Join(", ", _settings.Commands.Select(c => "css_" + c)));
    }

    public void Unregister()
    {
        foreach (Guid id in _registered)
        {
            _core.Command.UnregisterCommand(id);
        }

        _registered.Clear();
    }

    private void OpenLocker(ICommandContext context)
    {
        IPlayer? player = context.Sender;
        if (player == null)
        {
            context.Reply("This command only works for a player.");
            return;
        }

        _debug.Write(player, $"command '{context.CommandName}'");
        _window.Toggle(player);
    }
}
