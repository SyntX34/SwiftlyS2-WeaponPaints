using PMM_WeaponPaints.Catalog;
using SwiftlyS2.Shared.Players;

namespace PMM_WeaponPaints.Services;

/* Small checks that many parts of the locker need before they touch a player. */
public static class PlayerTools
{
    public const byte TeamTerrorist = 2;
    public const byte TeamCounterTerrorist = 3;

    public static bool Fits(IPlayer? player)
    {
        return player != null
            && player.IsValid
            && !player.IsFakeClient
            && player.Slot >= 0
            && player.Slot < Core.LockerConstants.SlotCount;
    }

    public static bool OnASide(IPlayer? player)
    {
        return Fits(player) && (TeamOf(player) == TeamTerrorist || TeamOf(player) == TeamCounterTerrorist);
    }

    public static byte TeamOf(IPlayer player)
    {
        return player.Controller.TeamNum;
    }

    public static int SideOf(IPlayer player)
    {
        return TeamOf(player) == TeamCounterTerrorist ? WeaponCatalog.TeamCt : WeaponCatalog.TeamT;
    }

    public static string SteamOf(IPlayer player)
    {
        return player.SteamID.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static void Say(IPlayer player, string message)
    {
        if (Fits(player))
        {
            player.SendChat(message);
        }
    }
}
