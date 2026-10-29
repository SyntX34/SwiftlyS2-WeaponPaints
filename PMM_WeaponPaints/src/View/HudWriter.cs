using SwiftlyS2.Shared.SchemaDefinitions;
using PMM_WeaponPaints.Core;

namespace PMM_WeaponPaints.View;

/*
  Sends classes and dialog variables to one client and remembers what was
  already sent. The layout keeps its state between draws, so sending the same
  class again is wasted traffic on every frame the window is open.
*/
public sealed class HudWriter
{
    public void Class(SlotState state, CCSCustomHudLayout layout, int slot, string panel, string cls, bool on)
    {
        Dictionary<string, bool> sent = state.SentClass ??= new Dictionary<string, bool>(StringComparer.Ordinal);
        string key = panel + "\u0001" + cls;
        if (sent.TryGetValue(key, out bool current) && current == on)
        {
            return;
        }

        sent[key] = on;
        layout.SetHasClassForPlayer(slot, panel, cls, on
            ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
            : EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass);
    }

    public void Var(SlotState state, CCSCustomHudLayout layout, int slot, string panel, string name, string value)
    {
        Dictionary<string, string> sent = state.SentVar ??= new Dictionary<string, string>(StringComparer.Ordinal);
        string key = panel + "\u0001" + name;
        if (sent.TryGetValue(key, out string? current) && current == value)
        {
            return;
        }

        sent[key] = value;
        layout.SetDialogVariableStringForPlayer(slot, panel, name, value);
    }

    /*
    A picture class replaces the one before it. The old class has to come off
    the panel, otherwise the last picture stays behind the new one.
    */
    public void Art(SlotState state, CCSCustomHudLayout layout, int slot, string panel, ref string? previous, string? cls)
    {
        if (!string.IsNullOrEmpty(previous) && previous != cls)
        {
            Class(state, layout, slot, panel, previous, false);
        }

        if (!string.IsNullOrEmpty(cls))
        {
            Class(state, layout, slot, panel, cls, true);
        }

        previous = cls;
    }
}
