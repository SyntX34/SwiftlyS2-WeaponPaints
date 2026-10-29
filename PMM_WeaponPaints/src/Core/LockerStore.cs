namespace PMM_WeaponPaints.Core;

/* Holds one SlotState per server slot. Nothing is created until a player asks. */
public sealed class LockerStore
{
    private readonly SlotState?[] _slots = new SlotState?[LockerConstants.SlotCount];

    public SlotState? Peek(int slot)
    {
        return slot >= 0 && slot < LockerConstants.SlotCount ? _slots[slot] : null;
    }

    public SlotState Get(int slot)
    {
        SlotState? state = _slots[slot];
        if (state == null)
        {
            state = new SlotState();
            _slots[slot] = state;
        }

        return state;
    }

    public bool IsOpen(int slot)
    {
        return slot >= 0 && slot < LockerConstants.SlotCount && _slots[slot] != null && _slots[slot]!.Open;
    }

    public void Reset(int slot)
    {
        _slots[slot]?.Clear();
    }

    public void CloseAll()
    {
        for (int slot = 0; slot < _slots.Length; slot++)
        {
            if (_slots[slot] != null)
            {
                _slots[slot]!.Token++;
                _slots[slot]!.Open = false;
                _slots[slot]!.Menu = null;
            }
        }
    }

    /* Drops what was sent to a client, so the next draw sends every class again. */
    public void ForgetSentEverywhere()
    {
        foreach (SlotState? state in _slots)
        {
            state?.DropSent();
        }
    }
}
