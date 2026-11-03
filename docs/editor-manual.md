# Stanley editor manual

For wiki editors: the people who hold the Wiki Editor role and keep Stanley's fighter data,
and the wiki pages that are not fighters, right. The commands everyone can run are in [the user manual](user-manual.md).

Editing happens in Discord. There is no deploy, no restart and no developer in the loop: a
fighter you add is on `/fighter` straight away, and a stat you correct is on the next card.

## Before you start

- **The editor commands live in one server.** `/alias-admin`, `/fighter-admin` and
  `/wiki-admin` are registered to Stanley's home server alone, so they exist in no other
  server, whoever you are.
- **They are refused for anyone without the Wiki Editor role**, and the role only counts in the
  home server.
- **Every reply is private.** Confirmations, refusals and the forms are shown to you and nobody
  else, so nothing you type while editing lands in the channel.
- **Every change is recorded** with your Discord ID, what was changed, and the values before and
  after.
- **There is no undo.** A mistake is put right by editing again, which is why the commands that
  destroy anything ask first.

If a command is missing or refuses you, see [the last
section](#when-a-command-does-not-appear-or-refuses-you).

## Names and aliases share one pool

Anything that finds a fighter — a fighter's own name and every alias — has to be unique across
all of them. So a fighter cannot be given a name an alias already uses, and an alias cannot be
one that already finds something else. Both refusals name whatever holds it.

This is also why a name or an alias has to contain at least one letter or digit: matching
ignores case, spaces, punctuation and accents, so a name of punctuation alone would be a name
nothing could ever find.

## Other names: `/alias-admin`

| Command | What it does |
|---|---|
| `/alias-admin add fighter:<fighter> alias:<name>` | Gives that fighter another name players can use. |
| `/alias-admin remove alias:<alias>` | Takes one alias away. Pick it from the suggestions — they read `alias → fighter`. |

Aliases are how players actually type: short forms, nicknames and the names used in chat. Pick
the fighter from the suggestions and the alias is attached to the right one; `/alias list` shows
a fighter's aliases to everyone.

## Wiki pages: `/wiki-admin`

`/wiki` links a fighter's page by building the address from its name. The wiki also has pages
that are no fighter at all — the Critless Skill Tree, Fortune's Favor — and those have no name
to build from, so each is a row you maintain here.

| Command | What it does |
|---|---|
| `/wiki-admin add key:<what users type> url:<full address>` | Adds a page. |
| `/wiki-admin edit page:<page> [key:<new key>] [url:<new address>]` | Changes its key, its address, or both. Leave an option out to keep what it has. |
| `/wiki-admin remove page:<page>` | Takes the page away. |

Pick the page from the suggestions for `edit` and `remove` — they read `key → page`, with the
wiki's own prefix dropped so the part that differs is the part you see.

**A page with two names is two rows.** There is no list of synonyms: if players type both
`critless` and `Critless Skill Tree`, add both, each with the same address. Either can then be
corrected or dropped without touching the other, and `/wiki` answers the same for both.

**A key cannot be a fighter's name or alias.** `/wiki` looks for a page before it looks for a
fighter, so a key that already finds a fighter would hide that fighter's own wiki link. The
refusal names the fighter. Keys otherwise live in a pool of their own, separate from the names
and aliases pool above: a key only has to be unique among keys.

**The address is the whole address**, starting `http://` or `https://`. Stanley sends it to the
channel exactly as stored, so a page that moves is fixed with `/wiki-admin edit`.

## Fighters: `/fighter-admin`

### `add`

| Option | What goes in it |
|---|---|
| `name` | The fighter's name, as the card should show it. |
| `character` | The character it is a variant of. Pick a suggestion. |
| `rarity` | Bronze, Silver, Gold or Diamond. |
| `element` | Fire, Water, Air, Light, Dark or Neutral. |
| `ability-name` | The signature ability's name, for example `RIFF WRATH`. Stored in capitals whichever way you type it. |
| `atk`, `hp`, `fs` | The base stats, as whole numbers above zero. |
| `portrait-url` | Web address of the portrait. Stanley links it and never hosts it, so the address has to keep working. |
| `quote` | Optional. Shown on the card in italics between quotation marks, above the abilities. |

Base stats only: maximum stats are never typed in and never stored. Stanley works them out from
the base stats and the rarity for every card, so correcting a base stat corrects the maximum too.

A new fighter has **no abilities yet** — write them with `ability`, below.

### `edit`

```text
/fighter-admin edit fighter:<fighter> atk:650 quote:Say hello to my little friend.
```

Give only the options you are changing; everything you leave out stays as it is. The
confirmation lists each field that actually changed, with its old and its new value, and says so
plainly when the values you gave were already what was stored.

`edit` takes every option `add` does, and two more:

| Option | What it is for |
|---|---|
| `wiki-url` | A fighter whose wiki page is not titled exactly like its name. Stanley otherwise builds the link from the name. |
| `raid-boss` | Marks a non-playable Raid Boss. Raid Bosses have cards and are searchable like anyone else, but are left out of the fighter counts. |

### `ability`

```text
/fighter-admin ability fighter:<fighter> action:Set one ability sa:2
```

**Set one ability** opens a form with room for 1,024 characters — the most a card can display,
and far more than a command option would let you type. The form arrives pre-filled when that SA
already exists, so an edit is a correction of the text in front of you rather than a retype.

- Leave `sa` out to add an ability at the end of the block.
- Give `sa` to rewrite an existing one. It has to be between 1 and one past the last ability:
  the numbers run from SA1 upwards and never have a gap.
- One ability per form. Discord forms hold five fields at most and Raid Bosses have no fixed
  number of abilities, so one at a time is the only shape that fits every fighter.

**Remove all abilities** drops the whole block after a confirmation, and you then enter them
again from SA1. There is deliberately no way to remove a single ability: removing SA2 of three
would leave a gap, and the numbers are also the display order. (`sa` belongs to
`Set one ability`; giving it here is refused rather than ignored.)

### `remove`

Removes a fighter with its abilities and its aliases, after a confirmation that names what goes
with it. Nothing is removed until you press the button, and nothing comes back afterwards.

## Why a write is refused

| The refusal | What it means |
|---|---|
| No fighter matches that | The name reached no fighter. Pick from the suggestions instead of typing it out. |
| That name is already in use, naming another fighter | A fighter's name or an alias already uses it. See [Names and aliases share one pool](#names-and-aliases-share-one-pool). |
| It has no letters or digits | Punctuation and symbols alone cannot be matched, so the name would never find anything. |
| That is not one of Stanley's rarities / elements | Pick one of the offered values. |
| That character is not one Stanley holds | Characters come from the database. Pick a suggestion; if the character is genuinely new, a developer adds it. |
| A stat has to be a whole number above zero | Zero and negative stats are refused, by the command and by the database. |
| That is not a web address | `portrait-url`, `wiki-url` and a wiki page's `url` take an absolute `http`/`https` address. |
| The field was left blank | A value of spaces alone is refused rather than treated as "clear this field", so nothing is emptied by accident. |
| Nothing to change | Every value you gave already matched what was stored. |
| No wiki page matches that | The `page` option reached no wiki page. Pick from the suggestions. |
| That key already finds the fighter … | A wiki page under that key would hide that fighter from `/wiki`. Pick a key players would not use for a fighter. |
| That key already finds the wiki page … | Another page answers to it. Edit that page instead, or pick another key. |
| Give a new key, a new address, or both | `/wiki-admin edit` was run with neither, so there was nothing to change. |

## When a command does not appear or refuses you

| What you see | What to do |
|---|---|
| `/alias-admin`, `/fighter-admin` or `/wiki-admin` is not offered at all, not even in the suggestions | Discord hides them from everyone except administrators until the Wiki Editor role is given access, in the home server, under **Server Settings → Integrations → Stanley → Manage**. A server admin does that once. |
| "Only Wiki Editors can change Stanley's data", although you hold the role | The role has to be the one in the home server, and Stanley has to be a member of that server. Whoever runs Stanley can check both. |
| "Editing is not set up yet" | Stanley does not know which server and role to trust yet. That is configuration, not something an editor can fix — see [Letting editors change data](../README.md#letting-editors-change-data). |
| "That command does not exist any more" | Discord offered you a command from an older version of Stanley. It clears itself; typing `/` again after a minute or two shows the current ones. |

---

What Stanley is required to do is written in [`Stanley-v2-FRD.docx`](Stanley-v2-FRD.docx), and
the tables behind these commands in
[`Stanley-v2-DB-Design.docx`](Stanley-v2-DB-Design.docx). Installing and running Stanley is the
[README](../README.md).
