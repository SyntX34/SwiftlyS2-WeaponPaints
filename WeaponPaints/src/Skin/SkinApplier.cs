using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using WeaponPaints.Catalog;
using WeaponPaints.Config;

namespace WeaponPaints.Skin;

/*
  Puts the loadout onto the player that is alive right now.

  Guns and knives use the fallback fields of the econ entity, which is the
  same path the game uses for a skin that is not a real inventory item.
  Gloves use the econ glove item of the pawn and the agent is a model change.
*/
public sealed class SkinApplier
{
    private readonly ISwiftlyCore _core;
    private readonly ILogger _logger;
    private readonly PluginSettings _settings;

    public SkinApplier(ISwiftlyCore core, ILogger logger, PluginSettings settings)
    {
        _core = core;
        _logger = logger;
        _settings = settings;
    }

    public void Apply(IPlayer player, TeamLoadout loadout)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || !player.IsAlive)
        {
            return;
        }

        try
        {
            PaintGuns(pawn, loadout);
            DressKnife(player, pawn, loadout);
            DressGloves(pawn, loadout);
            DressAgent(pawn, loadout);
        }
        catch (Exception ex)
        {
            _logger.LogError("The loadout of {Player} was not applied: {Message}", player.Name, ex.Message);
        }
    }

    private void PaintGuns(CCSPlayerPawn pawn, TeamLoadout loadout)
    {
        if (pawn.WeaponServices == null)
        {
            return;
        }

        foreach (CHandle<CBasePlayerWeapon> handle in pawn.WeaponServices.MyWeapons.ToList())
        {
            if (!handle.IsValid || handle.Value == null)
            {
                continue;
            }

            CCSWeaponBase? gun = handle.Value.As<CCSWeaponBase>();
            if (gun == null)
            {
                continue;
            }

            int def = gun.AttributeManager.Item.ItemDefinitionIndex;
            if (def >= 500)
            {
                continue;
            }

            PaintInfo? info = loadout.InfoOf(def);
            if (info == null || info.Value.Paint <= 0)
            {
                continue;
            }

            Paint(gun, info.Value);
        }
    }

    private void DressKnife(IPlayer player, CCSPlayerPawn pawn, TeamLoadout loadout)
    {
        if (loadout.Knife == null)
        {
            return;
        }

        int wanted = WeaponCatalog.DefOf(loadout.Knife);
        string cls = WeaponCatalog.ClassOf(loadout.Knife);
        if (wanted <= 0 || cls.Length == 0 || pawn.WeaponServices == null)
        {
            return;
        }

        List<CCSWeaponBase> knives = new();
        foreach (CHandle<CBasePlayerWeapon> handle in pawn.WeaponServices.MyWeapons.ToList())
        {
            if (!handle.IsValid || handle.Value == null)
            {
                continue;
            }

            CCSWeaponBase? weapon = handle.Value.As<CCSWeaponBase>();
            if (weapon == null)
            {
                continue;
            }

            int def = weapon.AttributeManager.Item.ItemDefinitionIndex;
            if (def == 42 || def == 59 || def >= 500)
            {
                knives.Add(weapon);
            }
        }

        bool matches = knives.Count == 1 && knives[0].AttributeManager.Item.ItemDefinitionIndex == wanted;
        if (!matches)
        {
            // The old knife has to go first, otherwise the player carries two.
            foreach (CCSWeaponBase knife in knives)
            {
                knife.AddEntityIOEvent("Kill", string.Empty);
            }

            int id = player.PlayerID;
            _core.Scheduler.NextWorldUpdate(() =>
            {
                IPlayer? current = _core.PlayerManager.GetPlayer(id);
                CCSPlayerPawn? live = current?.PlayerPawn;
                if (current == null || live == null || !live.IsValid || !current.IsAlive)
                {
                    return;
                }

                CCSWeaponBase? given = live.ItemServices?.GiveItem<CCSWeaponBase>(cls);
                if (given == null || !given.IsValid)
                {
                    return;
                }

                PaintInfo? info = loadout.InfoOf(wanted);
                if (info != null && info.Value.Paint > 0)
                {
                    Paint(given, info.Value);
                }
            });

            return;
        }

        PaintInfo? paint = loadout.InfoOf(wanted);
        if (paint != null && paint.Value.Paint > 0)
        {
            Paint(knives[0], paint.Value);
        }
    }

    private void Paint(CCSWeaponBase weapon, PaintInfo info)
    {
        weapon.FallbackPaintKit = info.Paint;
        weapon.FallbackWear = info.Wear > 0f ? info.Wear : _settings.DefaultWear;
        weapon.FallbackSeed = info.Seed;
        weapon.FallbackStatTrak = info.StatTrak;

        CEconItemView item = weapon.AttributeManager.Item;
        item.ItemIDHigh = uint.MaxValue;
        item.ItemIDLow = uint.MaxValue;
        item.Initialized = true;
        if (!string.IsNullOrWhiteSpace(info.Name))
        {
            item.CustomName = info.Name;
        }

        weapon.FallbackPaintKitUpdated();
        weapon.FallbackWearUpdated();
        weapon.FallbackSeedUpdated();
        weapon.FallbackStatTrakUpdated();
        weapon.AttributeManager.ItemUpdated();
    }

    private void DressGloves(CCSPlayerPawn pawn, TeamLoadout loadout)
    {
        if (loadout.GloveDef <= 0)
        {
            return;
        }

        CEconItemView gloves = pawn.EconGloves;
        gloves.ItemDefinitionIndex = (ushort)loadout.GloveDef;
        gloves.ItemIDHigh = uint.MaxValue;
        gloves.ItemIDLow = uint.MaxValue;
        gloves.Initialized = true;
        gloves.ItemDefinitionIndexUpdated();

        pawn.EconGlovesChanged = 1;
        pawn.EconGlovesUpdated();
    }

    private void DressAgent(CCSPlayerPawn pawn, TeamLoadout loadout)
    {
        if (string.IsNullOrWhiteSpace(loadout.Agent) || loadout.Agent.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pawn.SetModel(loadout.Agent);
    }
}
