using System.Data;
using PMM_WeaponPaints.Catalog;

namespace PMM_WeaponPaints.Data;

/*
  Reads what a player has equipped. One statement per table, so a WeaponPaints
  install that is missing a table still gives the rest of the loadout.
*/
public sealed class WpDatabase
{
    private readonly DatabaseGate _gate;

    public WpDatabase(DatabaseGate gate)
    {
        _gate = gate;
    }

    public DatabaseGate Gate => _gate;

    /* Index 0 is T, index 1 is CT. */
    public TeamLoadout[] Load(string steam)
    {
        TeamLoadout[] loadout = { new TeamLoadout(), new TeamLoadout() };

        _gate.Each(
            "SELECT weapon_team, weapon_defindex, weapon_paint_id FROM wp_player_skins WHERE steamid = @steam",
            reader => loadout[Side(reader, 0)].Paints[Number(reader, 1)] = Number(reader, 2),
            new QueryArg("@steam", steam));

        _gate.Each(
            "SELECT weapon_team, knife FROM wp_player_knife WHERE steamid = @steam",
            reader =>
            {
                int knife = Side(reader, 0);
                string? cls = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1));
                if (knife < 0 || string.IsNullOrWhiteSpace(cls))
                {
                    return;
                }

                if (WeaponCatalog.DefByClass.TryGetValue(cls.Trim(), out int def) && WeaponCatalog.NameByDef.TryGetValue(def, out string? name))
                {
                    loadout[knife].Knife = name;
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

    /* WeaponPaints writes the word null into the agent columns when a player resets the agent. */
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
