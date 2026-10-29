using System.Text.Json;
using Microsoft.Extensions.Logging;
using PMM_WeaponPaints.Config;
using PMM_WeaponPaints.Core;

namespace PMM_WeaponPaints.Services;

/* Reads wp_triggers.json into the trigger book. A missing file simply keeps the defaults. */
public sealed class TriggerLoader
{
    private readonly TriggerBook _book;
    private readonly ILogger _logger;

    public TriggerLoader(TriggerBook book, ILogger logger)
    {
        _book = book;
        _logger = logger;
    }

    public void Load(string folder)
    {
        _book.Reset();
        string path = Path.Combine(folder, LockerConstants.TriggerFileName);
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            TriggerFile? file = JsonSerializer.Deserialize<TriggerFile>(File.ReadAllText(path), options);
            if (file == null)
            {
                _logger.LogWarning("{File} is empty.", LockerConstants.TriggerFileName);
                return;
            }

            foreach ((string key, TriggerGroupFile value) in file.Groups ?? new Dictionary<string, TriggerGroupFile>())
            {
                if (value.Items is { Length: > 0 })
                {
                    string title = string.IsNullOrWhiteSpace(value.Title) ? key.ToUpperInvariant() : value.Title!;
                    _book.SetGroup(key, new WeaponGroup(key, title, value.Items));
                }
            }

            foreach ((string id, string action) in file.Buttons ?? new Dictionary<string, string>())
            {
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(action))
                {
                    continue;
                }

                _book.SetButton(id, action.Trim());
            }

            _logger.LogInformation("{File}: {Buttons} buttons and {Groups} weapon groups.", LockerConstants.TriggerFileName,
                file.Buttons?.Count ?? 0, _book.Groups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{File} could not be read: {Message}", LockerConstants.TriggerFileName, ex.Message);
        }
    }
}
