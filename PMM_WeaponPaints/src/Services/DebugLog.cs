using System.Text;
using Microsoft.Extensions.Logging;
using PMM_WeaponPaints.Config;
using SwiftlyS2.Shared.Players;

namespace PMM_WeaponPaints.Services;

/*
  The debug channel. With Debug on, every step of the locker goes to a file in
  the logs folder of the plugin, to the server console and to the chat of the
  player the line is about.
*/
public sealed class DebugLog
{
    private readonly object _lock = new();
    private readonly ILogger _logger;
    private StreamWriter? _file;

    public DebugLog(ILogger logger, PluginSettings settings)
    {
        _logger = logger;
        Settings = settings;
    }

    public PluginSettings Settings { get; set; }

    public string? FilePath { get; private set; }

    public bool On => Settings.Debug;

    public void Open(string folder)
    {
        if (!Settings.Debug)
        {
            return;
        }

        try
        {
            string logs = Path.Combine(folder, "logs");
            Directory.CreateDirectory(logs);
            FilePath = Path.Combine(logs, $"log-PMM_WeaponPaints_debug_{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            FileStream stream = new(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _file = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            _logger.LogWarning("Debug is on. The locker log is {Path}", FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError("The debug log was not created: {Message}", ex.Message);
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            _file?.Dispose();
            _file = null;
        }
    }

    /* Player is optional. Pass it from the game thread, because the line also goes to the chat. */
    public void Write(IPlayer? player, string message)
    {
        if (!Settings.Debug)
        {
            return;
        }

        string who = "";
        bool chat = false;
        try
        {
            if (player != null && player.IsValid)
            {
                who = $"[{player.Slot}:{player.Name}:{player.Controller.TeamNum}] ";
                chat = !player.IsFakeClient;
            }
        }
        catch
        {
            // A player handle that is already gone. The line is still worth writing.
        }

        string line = $"{DateTime.Now:HH:mm:ss.fff} {who}{message}";
        lock (_lock)
        {
            try
            {
                _file?.WriteLine(line);
            }
            catch
            {
                // Disk full or the file is locked. The console still gets the line.
            }
        }

        Console.WriteLine("[PMM-DBG] " + line);
        if (chat)
        {
            string text = message.Length > 180 ? message[..180] + "..." : message;
            player!.SendChat(" \x08[PMM-DBG]\x01 " + text);
        }
    }
}
