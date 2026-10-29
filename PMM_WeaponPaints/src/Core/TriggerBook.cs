namespace PMM_WeaponPaints.Core;

/*
  The button map the locker runs on. It starts as a copy of the defaults and
  the loader overwrites it with wp_triggers.json when that file exists.
*/
public sealed class TriggerBook
{
    public Dictionary<string, string> Buttons { get; private set; } = new(TriggerDefaults.Buttons, StringComparer.Ordinal);

    public Dictionary<string, WeaponGroup> Groups { get; private set; } = new(TriggerDefaults.Groups, StringComparer.OrdinalIgnoreCase);

    /* Buttons that open a section. They get the is-cur class while their section is open. */
    public List<KeyValuePair<string, string>> SectionButtons { get; private set; } = new();

    public void Reset()
    {
        Buttons = new Dictionary<string, string>(TriggerDefaults.Buttons, StringComparer.Ordinal);
        Groups = new Dictionary<string, WeaponGroup>(TriggerDefaults.Groups, StringComparer.OrdinalIgnoreCase);
        SectionButtons = new List<KeyValuePair<string, string>>();
    }

    public void SetButton(string id, string action)
    {
        Buttons[id] = action;
        if (IsSection(action))
        {
            SectionButtons.Add(new KeyValuePair<string, string>(id, action));
        }
    }

    public void SetGroup(string key, WeaponGroup group)
    {
        Groups[key] = group;
    }

    public bool TryAction(string buttonId, out string action)
    {
        return Buttons.TryGetValue(buttonId, out action!);
    }

    /* Looks a weapon group up by key, or builds one from a comma list of weapon names. */
    public WeaponGroup? GroupFor(string arg)
    {
        if (Groups.TryGetValue(arg, out WeaponGroup? group))
        {
            return group;
        }

        string[] items = arg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return items.Length == 0 ? null : new WeaponGroup("", "WEAPONS", items);
    }

    public string GroupKeyOf(string weapon)
    {
        foreach (WeaponGroup group in TriggerDefaults.Groups.Values)
        {
            if (group.Items.Contains(weapon, StringComparer.OrdinalIgnoreCase))
            {
                return group.Key;
            }
        }

        return "";
    }

    private static bool IsSection(string action)
    {
        return action.StartsWith("open:", StringComparison.OrdinalIgnoreCase)
            || action.StartsWith("weapons:", StringComparison.OrdinalIgnoreCase)
            || action.Equals("home", StringComparison.OrdinalIgnoreCase);
    }
}
