# Stanley v2

A Discord bot that answers questions about Skullgirls Mobile fighters. Slash commands only, with
the fighter data in PostgreSQL so editors can correct it from inside Discord.

This is the rebuild of the Python prefix bot in `.old/`. The requirements are in
[`docs/Stanley-v2-FRD.docx`](docs/Stanley-v2-FRD.docx) (the master), the schema in
[`docs/Stanley-v2-DB-Design.docx`](docs/Stanley-v2-DB-Design.docx), the plan in
[`docs/migration-plan.md`](docs/migration-plan.md), and the architecture decisions in

## What is here so far

Phases 1 to 3 of the migration plan — the skeleton, the core logic, and the schema with its
import — and the commands `/ping`, `/fighter`, `/wiki`, `/alias list` and `/credits` for
everyone, plus the editor commands `/alias-admin` (`add`, `remove`) and `/fighter-admin`
(`add`, `edit`, `ability`, `remove`). Still to write: the Terms of Service and Privacy Policy
links FR-18 asks `/credits` for, which need the pages hosted first (phase 7), and the undo of
FR-16, which is a "could".

## Layout

| Path | What it holds |
|---|---|
| `src/Stanley.Core` | Domain logic: `StatCalculator`, `NameNormalizer`. No Discord, no database. |
| `src/Stanley.Data` | `StanleyDbContext`, the entities, the EF Core migrations, the seed rows. |
| `src/Stanley.Bot` | The host, the Discord wiring, and `Modules/` — one class per feature. |
| `tools/Stanley.Importer` | One-off: loads v1's `fighters.json` into the database. |
| `tools/golden` | Regenerates the stat fixture from v1's Python formula. |
| `scripts/up.sh` | Build, start and verify the stack: the deploy command. |
| `tests/Stanley.Core.Tests` | xUnit. The golden stat test over all 309 fighters, and the normalizer. |
| `tests/Stanley.Data.Tests` | The migration and its constraints, against a real PostgreSQL. |

Discord handlers stay thin: a module method reads its options, calls a service, and renders the
reply. Anything worth testing lives in `Stanley.Core` or `Stanley.Data`, which need neither a
Discord connection nor a token.

## Setting up

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Docker.

Two things to check before you start:

- **On Windows, Docker Desktop needs WSL2.** Windows Home has no Hyper-V backend to fall
  back on, so without it the daemon never comes up and `docker` fails with
  `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`. Run
  `wsl --install` from an admin prompt and reboot.
- **Port 5432 has to be free.** The database publishes on `127.0.0.1:5432`, so a PostgreSQL
  installed natively on the same machine will take the port first. Stop that service
  (`net stop postgresql-x64-18` from an admin prompt on Windows) or see
  [If it does not start](#if-it-does-not-start) for how to move the container's port.

1. **Make yourself a bot.** In the [Discord Developer Portal](https://discord.com/developers),
   create an application — call it something like `Stanley Dev` — and copy its token. Never run
   development code with the production token (NFR-06). Stanley needs no privileged intents;
   leave all three switched off.
2. **Make yourself a test server.** A private Discord server you own, with a second account or a
   friend in it, so you can check that a command refuses someone without the editor role.
3. **Configure.** Copy `.env.example` to `.env`. `.env` is git-ignored.

   `POSTGRES_PASSWORD` is always required — it is the only value the database container
   needs. `DISCORD_TOKEN` and `DISCORD_TEST_GUILD_IDS` (your test servers' IDs,
   comma-separated) are needed only to run the bot **inside** Docker; if you run it on the host
   instead, they come from user secrets and can stay empty here.

   `POSTGRES_PASSWORD` takes effect only when the `postgres-data` volume is first created.
   Changing it later needs the volume recreated:
   `docker compose down -v` (this deletes the data).
4. **Run it.**

   ```sh
   ./scripts/up.sh
   ```

   That starts PostgreSQL and the bot, applies the migrations, and registers the commands to
   your test server, where they appear immediately. Type `/ping` there.

   A good startup ends with these four lines:

   ```text
   Stanley.Bot.Hosting.DatabaseMigrationService[0] Migrations applied.
   Stanley.Bot.Hosting.DiscordBotService[0] [Gateway] Connected
   Stanley.Bot.Hosting.DiscordBotService[0] Registered 1 command(s) to test guild <id>.
   Stanley.Bot.Hosting.DiscordBotService[0] [Gateway] Ready
   ```

   If it says `Registered 1 command(s) globally` instead, `Discord:TestGuildIds` did not
   reach the bot — see [If it does not start](#if-it-does-not-start).

### Running the bot from your IDE instead

Start the database on its own and run the bot on the host:

```sh
docker compose up -d postgres
dotnet user-secrets --project src/Stanley.Bot set "Discord:Token" "<your token>"
dotnet user-secrets --project src/Stanley.Bot set "Discord:TestGuildIds:0" "<your test server id>"
dotnet run --project src/Stanley.Bot
```

User secrets are stored outside the repository, so a token cannot be committed. The bot connects
out to Discord, so no public URL or tunnel is needed. The connection string for this path comes
from `appsettings.json`, which already points at `localhost:5432`.

Two names have to match exactly or the setting is silently ignored, because configuration
binding skips keys it does not recognise rather than failing:

- the secret key is `Discord:TestGuildIds`, spelled as the property in
  `src/Stanley.Bot/Configuration/DiscordOptions.cs`;
- the environment variable equivalent is `Discord__TestGuildIds`, with a double underscore.

The setting is a list, so each shape writes it differently:

| Where | How to write two servers |
|---|---|
| `appsettings.Development.json` | `"TestGuildIds": [ 111, 222 ]` |
| User secrets | one key per entry: `Discord:TestGuildIds:0`, `Discord:TestGuildIds:1` |
| Environment or `.env` | one comma-separated value: `DISCORD_TEST_GUILD_IDS=111,222` |

The comma-separated form is expanded into the indexed keys before binding
(`src/Stanley.Bot/Configuration/GuildIdListConfiguration.cs`). An empty variable changes
nothing, so the list in `appsettings.Development.json` still applies; a variable that does list
servers replaces that list rather than merging with it. Commands are registered to every server
in the list, and one unreachable server is logged and skipped rather than stopping the rest.

#### User secrets only load in the Development environment

`HostApplicationBuilder` adds the user-secrets provider only when the environment is
`Development`, and a console host defaults to `Production`. `dotnet run` is fine because
`src/Stanley.Bot/Properties/launchSettings.json` sets `DOTNET_ENVIRONMENT=Development` for it.
Anything that bypasses launch profiles does not get that, so set it yourself:

```sh
DOTNET_ENVIRONMENT=Development dotnet src/Stanley.Bot/bin/Debug/net10.0/Stanley.Bot.dll
```

Without it the host stops before touching Discord and prints what is missing:

```text
DataAnnotation validation failed for 'DiscordOptions' members: 'Token' with the error:
'Set Discord:Token (dotnet user-secrets, or the Discord__Token environment variable).'
```

### If it does not start

| Symptom | Cause and fix |
|---|---|
| `DataAnnotation validation failed ... 'Token'` even though the secret is set | The environment is not `Development`, so user secrets never loaded. See above. |
| `Registered N command(s) globally` when you expected a test server | `Discord:TestGuildIds` is empty or misspelled. Check it with `dotnet user-secrets --project src/Stanley.Bot list \| grep TestGuildIds` — a bare `list` prints your token too. |
| `ports are not available: exposing port TCP 127.0.0.1:5432` | Something else holds 5432, usually a native PostgreSQL service. Stop it, or publish the container elsewhere with an override file (below) and pass a matching `ConnectionStrings__Stanley`. |
| `failed to connect to the docker API at npipe:...` | The Docker daemon is not running. On Windows Home it also needs WSL2; see the top of this section. |
| `/alias-admin add` answers "Only Wiki Editors can change Stanley's data" although the role is held | Stanley is probably not a *member* of that server — commands can be registered to a server the bot never joined. `GET /users/@me/guilds` with the bot token returning `[]` confirms it; re-invite with the `bot` scope (above). |
| `/alias-admin add` answers "Editing is not set up yet", or the write commands appear in no server at all | `Discord:HomeGuildId`, `Discord:WikiEditorRoleId` or `Discord:TestGuildIds` is not reaching this run. `.env` is read by Docker Compose only; a host-side `dotnet run` reads user secrets and the environment. The bot logs a warning at startup when the first two are missing, and another naming the editor commands it registered nowhere. |
| `/alias-admin` or `/fighter-admin` does not appear for a Wiki Editor, not even in the suggestions | Discord hides them: they are limited to Administrator, so the Wiki Editor role has to be given access under Server Settings, Integrations, Stanley, Manage in the home server. Check the startup log for the line naming the server they were registered to. |
| A command Discord offers answers "That command does not exist any more" | A registration from an earlier run that this build no longer has, which Discord keeps until it is overwritten. Restart the bot: it now rewrites every scope it uses, and logs what it registered and what it removed. The picker can go on showing the old one for a few minutes from the client's own cache. |
| `password authentication failed for user "<your_user_name>"` | The database is a different server than the container, or the volume was created with another `POSTGRES_PASSWORD`. |

To keep a native PostgreSQL on 5432 and put the container on 5433, write this override
somewhere outside the repository and point `-f` at it. `!override` matters: without it
Compose appends to the port list and still tries to bind 5432.

```yaml
services:
  postgres:
    ports: !override
      - "127.0.0.1:5433:5432"
```

```sh
docker compose -f docker-compose.yml -f ../pg-5433.yml up -d postgres
ConnectionStrings__Stanley="Host=localhost;Port=5433;Database=<your_database_name>;Username=<your_user_name>;Password=<yours>" \n  dotnet run --project src/Stanley.Bot
```

### Loading the fighter data

```sh
dotnet run --project tools/Stanley.Importer -- .old/fighters.json
```

It applies any pending migrations, then imports. It matches fighters on their slug and updates
in place, so running it twice changes nothing (FR-21). It prints what it did and the resulting
totals; at launch those are 18 characters, 309 fighters (306 collectible, 3 raid bosses) and 627
signature abilities. Aliases start empty on purpose — editors add them with `/alias-admin add` after
launch (FR-22).

## Tests

```sh
dotnet test
```

- `Stanley.Core.Tests` needs nothing. Its main job is the golden test: v2's maximum stats must
  equal v1's for every one of the 309 fighters (FR-06). If it fails, the formula changed.
- `Stanley.Data.Tests` needs Docker. Testcontainers starts a real PostgreSQL, applies the
  migration and checks the seeded rows, the views and every constraint. Without Docker, skip
  them:

  ```sh
  dotnet test --filter "RequiresDocker!=true"
  ```

The projects use xUnit v2 on VSTest rather than xUnit v3, because `dotnet test` on the .NET 10
SDK drives it without extra configuration.

## Adding a command

1. Add a method to a class in `src/Stanley.Bot/Modules/`, or add a module class if the command
   belongs to a new feature. `PingModule` is the smallest example.

   ```csharp
   [SlashCommand("credits", "Who made Stanley.")]
   public Task CreditsAsync() => RespondAsync(embed: _credits.Build());
   ```

2. Keep the handler thin. Put the logic in a service in `Stanley.Core` (or a repository in
   `Stanley.Data`) and inject it through the module's constructor; that is the part you unit-test.
3. Answer within three seconds, or `await DeferAsync()` first and follow up (NFR-01).
4. Make error and confirmation replies `ephemeral: true`, and never let a reply mention anyone
   (FRD section 6.2).
5. Nothing needs registering by hand: modules are discovered at startup, and while
   `Discord:TestGuildIds` lists a server the command appears there right away.

Write commands additionally get the `[RequireWikiEditor]` precondition
(`src/Stanley.Bot/Preconditions/`), which allows only members holding the Wiki Editor role in
the home server, both matched by ID. It fails closed: while `Discord:HomeGuildId` or
`Discord:WikiEditorRoleId` is unset, every write command refuses everyone.

**Write commands live in a top-level command of their own**, `/alias-admin` or
`/fighter-admin`, which is what lets them be registered to the home server alone so they do not
appear anywhere else (migration plan section 6). Discord registers a top-level command with all
of its subcommands, and keeps a server's commands apart from the global ones, so a write
*subcommand* of a command everyone can run cannot be narrowed: it is registered wherever its
siblings are, and a guild copy carrying it would show up as a second `/alias` next to the global
one. `DefaultMemberPermissions` is per top-level command for the same reason. So a new write
command belongs on an `-admin` module (marked `[DontAutoRegister]`, which is what
`DiscordBotService` registers to the home server), not next to a read command.

## Letting editors change data

`/alias-admin` and `/fighter-admin` are refused for everyone until both of these are set,
because the role is matched by ID and there is no default. Without `Discord:HomeGuildId` they
are not registered anywhere either, so they appear in no server at all:

| Setting | What it is |
|---|---|
| `Discord:HomeGuildId` | The server whose Wiki Editor role counts. A role of that ID in any other server does not. |
| `Discord:WikiEditorRoleId` | The Wiki Editor role itself. Right-click the role in Server Settings → Roles → Copy Role ID, with Developer Mode on. |

**Stanley has to be a member of the home server, invited with the `bot` scope.** Registering
commands to a server needs only `applications.commands`, so an app installed without `bot` has
working commands there and no member: Discord then sends no member data with an interaction and
the gateway never caches the server, so there is no role to check and no way to read one. The
editor commands refuse everyone in that state, and the bot logs a warning naming the server at
startup. Check the app's install link in the Developer Portal under **Installation → Guild
Install**, and re-invite with:

```text
https://discord.com/oauth2/authorize?client_id=<app id>&scope=bot+applications.commands&permissions=0
```

Set them like the token — user secrets locally, `Discord__HomeGuildId` and
`Discord__WikiEditorRoleId` in Docker (`.env.example` lists them). Filling them into `.env`
alone is the easy mistake: Compose passes that file to the container, and a host-side
`dotnet run` never reads it. Who should hold the role is
FRD FR-17, still deferred; until it is decided, leaving them unset is the safe state rather than
a broken one.

**Then grant the role access inside Discord.** The editor commands are registered with
`DefaultMemberPermissions` of Administrator, so Discord shows them to administrators only —
a Wiki Editor who is not an administrator does not see them in the picker at all, which looks
exactly like a command that was never registered. In the home server, open **Server Settings →
Integrations → Stanley → Manage** and give the Wiki Editor role access to `/alias-admin` and
`/fighter-admin`. The bot prints a reminder naming those commands and the server at every
startup.

## Adding or correcting a fighter

This is done in Discord, with no deploy and no developer:

| Command | What it does |
|---|---|
| `/fighter-admin add` | Adds a fighter from its name, character, rarity, element, ability name, base stats and portrait URL (FR-12). It has no abilities yet. |
| `/fighter-admin ability` | `Set one ability` opens a form for one SA — pre-filled when that SA exists — and `Remove all abilities` drops the block after a confirmation (FR-13). Leave `sa` out to add one at the end. |
| `/fighter-admin edit` | Changes only the options you give, and confirms with the old and new value of each (FR-13). |
| `/fighter-admin remove` | Removes a fighter, its abilities and its aliases, after a confirmation (FR-14). |

`/fighter-admin` is registered to the home server alone, so it appears nowhere else, and every
write is recorded with the editor's Discord ID in `change_records` (FR-15). A fighter added this
way is on `/fighter` straight away; nothing is cached and nothing needs a restart.

Two rules worth knowing before touching fighter data:

- **Maximum stats are never stored.** They are computed per request from the base stats and the
  tier (`StatCalculator`). Correcting a base stat corrects the card.
- **Signature abilities are all or nothing.** A single ability is never removed on its own:
  removal takes the whole block, which is then re-entered from SA1, so the SA numbers always run
  from 1 with no gap (FR-13).

## Changing the schema

```sh
docker compose up -d postgres
dotnet ef migrations add <Name> --project src/Stanley.Data
```

An applied migration is never edited — a correction is a new migration. Views, the `pg_trgm`
extension and the tier and element seed rows are part of the first migration, so a fresh
database and a restored backup agree. CI fails if an entity changed without a migration.

Migrations are applied by the bot at startup. A deployment that would rather apply them as its
own step sets `Database:ApplyMigrationsOnStartup` to `false` and runs
`dotnet ef database update` itself.

## Deploying

One small VPS, the same Compose file, without the development override:

```sh
git pull
./scripts/up.sh
```

`scripts/up.sh` is `docker compose up -d --build --force-recreate bot` plus the check that
command does not do. The bot's container is always replaced, never reused: `up -d` on its own
leaves a container whose image and configuration have not changed running as it is, so the
previous run would survive the deploy. PostgreSQL is left alone, holding no code of ours. Then
it waits for the bot to report `[Gateway] Ready` and prints the migration and command-registration
lines, so a deploy that leaves the bot in a crash loop exits non-zero with its last 30 log lines
instead of looking successful. Pass a service name (`./scripts/up.sh postgres`) to start one
only, or set `STANLEY_UP_TIMEOUT` for a slow first build. `docker compose up -d --build` still
works if you want the raw command.

- `restart: unless-stopped` brings the bot back after a crash or a reboot (NFR-04).
- Clear `DISCORD_TEST_GUILD_IDS` in the server's `.env`, and make sure the production image is
  not built with a `TestGuildIds` list in `appsettings.Development.json`, so commands register
  globally.
- Still to set up in phase 6: the nightly `pg_dump` to off-box storage **with one restore
  tested** (NFR-05), and an alert when the bot has been offline for five minutes.

## For server admins

Stanley has no channel whitelist command. Restrict it per channel and per role in
**Server Settings → Integrations → Stanley**, which is Discord's own command-permissions UI
(FR-19).
