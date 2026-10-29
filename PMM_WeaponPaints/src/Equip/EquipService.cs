using PMM_WeaponPaints.Catalog;
using PMM_WeaponPaints.Data;

namespace PMM_WeaponPaints.Equip;

/*
  Saves the choice into the WeaponPaints tables. WeaponPaints reads the same
  rows on spawn, so a click here is the same thing as a click in its own menu.
  An empty choice deletes the row, which is how WeaponPaints means "default".
*/
public sealed class EquipService
{
    private readonly DatabaseGate _gate;

    public EquipService(DatabaseGate gate)
    {
        _gate = gate;
    }

    public bool SaveSkin(string steam, int side, int def, int paint)
    {
        int team = WeaponTeam(side);
        if (def <= 0)
        {
            return false;
        }

        if (paint <= 0)
        {
            return _gate.Execute(
                "DELETE FROM wp_player_skins WHERE steamid = @steam AND weapon_team = @team AND weapon_defindex = @def",
                new QueryArg("@steam", steam), new QueryArg("@team", team), new QueryArg("@def", def)) >= 0;
        }

        return _gate.Upsert(
            "UPDATE wp_player_skins SET weapon_paint_id = @paint WHERE steamid = @steam AND weapon_team = @team AND weapon_defindex = @def",
            "INSERT INTO wp_player_skins (steamid, weapon_team, weapon_defindex, weapon_paint_id) VALUES (@steam, @team, @def, @paint)",
            new QueryArg("@steam", steam), new QueryArg("@team", team), new QueryArg("@def", def), new QueryArg("@paint", paint));
    }

    public bool SaveKnife(string steam, int side, string? knifeName)
    {
        int team = WeaponTeam(side);
        string cls = WeaponCatalog.ClassOf(knifeName);
        if (cls.Length == 0 || knifeName == null || knifeName.Equals("Default Knife", StringComparison.OrdinalIgnoreCase))
        {
            return _gate.Execute(
                "DELETE FROM wp_player_knife WHERE steamid = @steam AND weapon_team = @team",
                new QueryArg("@steam", steam), new QueryArg("@team", team)) >= 0;
        }

        return _gate.Upsert(
            "UPDATE wp_player_knife SET knife = @knife WHERE steamid = @steam AND weapon_team = @team",
            "INSERT INTO wp_player_knife (steamid, weapon_team, knife) VALUES (@steam, @team, @knife)",
            new QueryArg("@steam", steam), new QueryArg("@team", team), new QueryArg("@knife", cls));
    }

    public bool SaveGloves(string steam, int side, int def, int paint)
    {
        int team = WeaponTeam(side);
        if (def <= 0)
        {
            return _gate.Execute(
                "DELETE FROM wp_player_gloves WHERE steamid = @steam AND weapon_team = @team",
                new QueryArg("@steam", steam), new QueryArg("@team", team)) >= 0;
        }

        bool saved = _gate.Upsert(
            "UPDATE wp_player_gloves SET weapon_defindex = @def WHERE steamid = @steam AND weapon_team = @team",
            "INSERT INTO wp_player_gloves (steamid, weapon_team, weapon_defindex) VALUES (@steam, @team, @def)",
            new QueryArg("@steam", steam), new QueryArg("@team", team), new QueryArg("@def", def));

        // The glove finish lives in the skins table, under the glove defindex.
        return SaveSkin(steam, side, def, paint) && saved;
    }

    public bool SaveMusic(string steam, int side, int id)
    {
        int team = WeaponTeam(side);
        if (id <= 0)
        {
            return _gate.Execute(
                "DELETE FROM wp_player_music WHERE steamid = @steam AND weapon_team = @team",
                new QueryArg("@steam", steam), new QueryArg("@team", team)) >= 0;
        }

        return _gate.Upsert(
            "UPDATE wp_player_music SET music_id = @id WHERE steamid = @steam AND weapon_team = @team",
            "INSERT INTO wp_player_music (steamid, weapon_team, music_id) VALUES (@steam, @team, @id)",
            new QueryArg("@steam", steam), new QueryArg("@team", team), new QueryArg("@id", id));
    }

    public bool SavePin(string steam, int side, int id)
    {
        int team = WeaponTeam(side);
        if (id <= 0)
        {
            return _gate.Execute(
                "DELETE FROM wp_player_pins WHERE steamid = @steam AND weapon_team = @team",
                new QueryArg("@steam", steam), new QueryArg("@team", team)) >= 0;
        }

        return _gate.Upsert(
            "UPDATE wp_player_pins SET id = @id WHERE steamid = @steam AND weapon_team = @team",
            "INSERT INTO wp_player_pins (steamid, weapon_team, id) VALUES (@steam, @team, @id)",
            new QueryArg("@steam", steam), new QueryArg("@team", team), new QueryArg("@id", id));
    }

    public bool SaveAgent(string steam, int side, string? model)
    {
        string column = side == WeaponCatalog.TeamCt ? "agent_ct" : "agent_t";
        object? value = string.IsNullOrWhiteSpace(model) ? null : model.Trim();

        return _gate.Upsert(
            $"UPDATE wp_player_agents SET {column} = @model WHERE steamid = @steam",
            $"INSERT INTO wp_player_agents (steamid, {column}) VALUES (@steam, @model)",
            new QueryArg("@steam", steam), new QueryArg("@model", value));
    }

    private static int WeaponTeam(int side)
    {
        return side == WeaponCatalog.TeamCt ? 3 : 2;
    }
}
