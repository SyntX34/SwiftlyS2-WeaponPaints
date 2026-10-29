/*
  Tables this locker reads and writes. They are the WeaponPaints tables, so a
  server that already runs WeaponPaints keeps its rows and nothing is lost.
  Every statement is safe to run again and again.

  The plugin runs this file on start. It can also be imported by hand with
  mysql or psql when the database user is not allowed to create tables.
*/

CREATE TABLE IF NOT EXISTS wp_player_skins (
    steamid VARCHAR(64) NOT NULL,
    weapon_team INT NOT NULL,
    weapon_defindex INT NOT NULL,
    weapon_paint_id INT NOT NULL DEFAULT 0,
    weapon_wear FLOAT NOT NULL DEFAULT 0,
    weapon_seed INT NOT NULL DEFAULT 0,
    weapon_name VARCHAR(64) NOT NULL DEFAULT '',
    PRIMARY KEY (steamid, weapon_team, weapon_defindex)
);

CREATE TABLE IF NOT EXISTS wp_player_knife (
    steamid VARCHAR(64) NOT NULL,
    weapon_team INT NOT NULL,
    knife VARCHAR(64) NOT NULL DEFAULT '',
    PRIMARY KEY (steamid, weapon_team)
);

CREATE TABLE IF NOT EXISTS wp_player_gloves (
    steamid VARCHAR(64) NOT NULL,
    weapon_team INT NOT NULL,
    weapon_defindex INT NOT NULL DEFAULT 0,
    PRIMARY KEY (steamid, weapon_team)
);

CREATE TABLE IF NOT EXISTS wp_player_music (
    steamid VARCHAR(64) NOT NULL,
    weapon_team INT NOT NULL,
    music_id INT NOT NULL DEFAULT 0,
    PRIMARY KEY (steamid, weapon_team)
);

CREATE TABLE IF NOT EXISTS wp_player_pins (
    steamid VARCHAR(64) NOT NULL,
    weapon_team INT NOT NULL,
    id INT NOT NULL DEFAULT 0,
    PRIMARY KEY (steamid, weapon_team)
);

CREATE TABLE IF NOT EXISTS wp_player_agents (
    steamid VARCHAR(64) NOT NULL,
    agent_ct VARCHAR(255) NULL,
    agent_t VARCHAR(255) NULL,
    PRIMARY KEY (steamid)
);
