# Contributing

Thanks for taking the time. This repository holds both SwiftlyS2 plugins, and the rules are the same for both.

## What lives where

| Folder | What it is |
| --- | --- |
| `PMM_WeaponPaints/` | the panorama locker |
| `WeaponPaints/` | the skin plugin that stores and applies the loadout |

The two plugins are separate on purpose. The locker never calls into the skin plugin directly, it only saves the
WeaponPaints tables and runs `css_wp` so the skin plugin picks the change up.

## Before you open a pull request

1. Build the solution. Both projects have to build with zero warnings.
   ```bash
   dotnet build SwiftlyS2.sln --configuration Release
   ```
2. Keep the code style that is already there: four spaces, one type per file, folders follow the job of the code
   (`Catalog`, `Data`, `View`, and so on).
3. Comments explain why something is done, not what the next line does.
4. One change per pull request.
5. Do not add a dependency the framework already has. The database comes from `Core.Database`, the scheduler from
   `Core.Scheduler`, the HUD from `Core.EntitySystem`. If a package is truly needed, explain it in the pull request.
6. Database statements must work on MySQL, MariaDB, PostgreSQL and SQLite. That means no `ON DUPLICATE KEY UPDATE`,
   no `ON CONFLICT` and no driver specific types in `resources/sql/schema.sql`.
7. New text goes into every file in `resources/translations` (en, ru, da, de, fr, fi, es, hu). English is the fallback,
   but a half translated page looks broken.

## Testing

There is no test project, so a change is checked on a real server:

- the plugin loads and prints its start line,
- `!pws` opens the window on a T or a CT,
- the page you touched draws, its cards show the right picture and the right sub line,
- a click saves into the database and the row is what you expected,
- a fresh database gets its tables from `schema.sql`.

Say in the pull request which of these you ran.

## Credits and license

Every contribution lands under the GNU GPL v3, the license of both original projects. Do not remove the credit block in
the README, and do not relicense the code. The original authors are:

- **Rimmer** for PMM WeaponPaints, https://github.com/RRimmer/PMM-WeaponPaints
- **Nereziel** for WeaponPaints, https://github.com/Nereziel/cs2-WeaponPaints

The SwiftlyS2 port is maintained by **SyntX34**.
