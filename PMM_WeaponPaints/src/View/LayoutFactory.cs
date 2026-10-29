using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;
using PMM_WeaponPaints.Core;
using PMM_WeaponPaints.Services;

namespace PMM_WeaponPaints.View;

/*
  Owns the custom hud entity the locker draws into. There is one per server,
  so every player shares the same layout and only the values differ.
*/
public sealed class LayoutFactory
{
    private readonly ISwiftlyCore _core;
    private readonly DebugLog _debug;

    public LayoutFactory(ISwiftlyCore core, DebugLog debug)
    {
        _core = core;
        _debug = debug;
    }

    public CCSCustomHudLayout? Layout { get; private set; }

    public bool Ensure()
    {
        if (Layout != null && Layout.IsValid)
        {
            return true;
        }

        try
        {
            CCSCustomHudLayout? created = _core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
            if (created == null || !created.IsValid)
            {
                return false;
            }

            created.StrLayout = LockerConstants.LayoutPath;
            created.StrLayoutUpdated();
            created.DispatchSpawn();

            if (!created.IsValid)
            {
                return false;
            }

            Layout = created;
            _debug.Write(null, "layout created");
            return true;
        }
        catch (Exception ex)
        {
            _debug.Write(null, "layout was not created: " + ex.Message);
            return false;
        }
    }

    /* A dead handle is dropped so the next draw makes a new layout. */
    public bool Alive()
    {
        if (Layout != null && Layout.IsValid)
        {
            return true;
        }

        Layout = null;
        return false;
    }

    public void Forget()
    {
        Layout = null;
    }
}
