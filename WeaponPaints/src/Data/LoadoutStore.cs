using System.Data;
using WeaponPaints.Catalog;

namespace WeaponPaints.Data;

/*
  Everything that touches the WeaponPaints tables lives here. Reads are one
  statement per table so a build without one of them still gives the rest,
  and writes update first and insert only when nothing was there yet.
*/
public sealed class LoadoutStore
{
    private readonly DatabaseGate _gate;

    public LoadoutStore(DatabaseGate gate)
    {
        _gate = gate;
    }

    public DatabaseGate Gate => _gate;

    public TeamLoadout[] Load(string steam)
    {
        TeamLoadout[] loadout = { new TeamLoadout(), new TeamLoadout() };

        _gate.Each(
            "SELECT weapon_team, weapon_defindex, weapon_paint_id, weapon_wear, weapon_seed, weapon_name FROM wp_player_skins WHERE steamid = @steam",
            reader =>
            {
                int side = Side(reader, 0);
                int def = Number(reader, 1);
                if (side < 0 || def <= 0)
                {
                    return;
                }

                loadout[side].Paints[def] = new PaintInfo(
                    Number(reader, 2),
                    reader.IsDBNull(3) ? 0f : Convert.ToSingle(reader.GetValue(3)),
                    Number(reader, 4),
                    Number(reader, 5),
                    reader.IsDBNull(6) ? "" : Convert.ToString(reader.GetValue(6)) ?? "");
            },
            new QueryArg("@steam", steam));

        _gate.Each(
            "SELECT weapon_team, knife FROM wp_player_knife WHERE steamid = @steam",
            reader =>
            {
                int side = Side(reader, 0);
                string? cls = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1));
                if (side < 0 || string.IsNullOrWhiteSpace(cls))
                {
                    return;
                }

                if (WeaponCatalog.DefByClass.TryGetValue(cls.Trim(), out int def) && WeaponCatalog.NameByDef.TryGetValue(def, out string? name))
                {
                    loadout[side].Knife = name;
                }
            },
            new QueryArg("@steam", steam));

        _gate.Each(
            "SELECT weapon_team, weapon_defindex FROM wp_player_gloves WHERE steamid = @steam",
            reader =>
            {
                int side = Side(reader, 0);
                if (side >= 0 && !reader.IsDBNull(1))
                {
                    loadout[side].GloveDef = Number(reader, 1);
                }
            },
            new QueryArg("@steam", steam));

        _gate.Each(
            "SELECT weapon_team, music_id FROM wp_player_music WHERE steamid = @steam",
            reader =>
            {
                int side = Side(reader, 0);
                if (side >= 0 && !reader.IsDBNull(1))
                {
                    loadout[side].Music = Number(reader, 1);
                }
            },
            new QueryArg("@steam", steam));

        _gate.Each(
            "SELECT weapon_team, id FROM wp_player_pins WHERE steamid = @steam",
            reader =>
            {
                int side = Side(reader, 0);
                if (side >= 0 && !reader.IsDBNull(1))
                {
                    loadout[side].Pin = Number(reader, 1);
                }
            },
            new QueryArg("@steam", steam));

        _gate.Each(
            "SELECT agent_ct, agent_t FROM wp_player_agents WHERE steamid = @steam",
            reader =>
            {
                loadout[WeaponCatalog.TeamCt].Agent = Agent(reader, 0);
                loadout[WeaponCatalog.TeamT].Agent = Agent(reader, 1);
            },
            new QueryArg("@steam", steam));

        return loadout;
    }

    public bool SaveSkin(string steam, int side, int def, int paint, float wear, int seed, string name)
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
            "UPDATE wp_player_skins SET weapon_paint_id = @paint, weapon_wear = @wear, weapon_seed = @seed, weapon_name = @name WHERE steamid = @steam AND weapon_team = @team AND weapon_defindex = @def",
            "INSERT INTO wp_player_skins (steamid, weapon_team, weapon_defindex, weapon_paint_id, weapon_wear, weapon_seed, weapon_name) VALUES (@steam, @team, @def, @paint, @wear, @seed, @name)",
            new QueryArg("@steam", steam), new QueryArg("@team", team), new QueryArg("@def", def),
            new QueryArg("@paint", paint), new QueryArg("@wear", wear), new QueryArg("@seed", seed), new QueryArg("@name", name ?? ""));
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

        // The finish of the gloves is a normal skin row under the glove defindex.
        return SaveSkin(steam, side, def, paint, 0f, 0, "") && saved;
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

    private static int Side(IDataReader reader, int column)
    {
        if (reader.IsDBNull(column))
        {
            return -1;
        }

        return Number(reader, column) switch
        {
            2 => WeaponCatalog.TeamT,
            3 => WeaponCatalog.TeamCt,
            _ => -1
        };
    }

    private static int Number(IDataReader reader, int column)
    {
        return reader.IsDBNull(column) ? 0 : Convert.ToInt32(reader.GetValue(column));
    }

    private static string? Agent(IDataReader reader, int column)
    {
        if (reader.IsDBNull(column))
        {
            return null;
        }

        string? text = Convert.ToString(reader.GetValue(column));
        return string.IsNullOrWhiteSpace(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : text.Trim();
    }
}
