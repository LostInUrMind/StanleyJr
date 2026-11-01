# Stanley user manual

For everyone in a server Stanley is in. If you hold the Wiki Editor role and maintain the
fighter data, read [the editor manual](editor-manual.md) as well.

Stanley answers questions about Skullgirls Mobile fighters. Everything is a slash command: type
`/` in any channel Stanley can see, pick a command, and Discord prompts you for its options.
Stanley only ever sees the commands you send it, not the channel's messages.

## The commands

| Command | What you get |
|---|---|
| `/fighter name:<fighter>` | The fighter's card: rarity, element, character, portrait, stats and signature abilities. |
| `/wiki name:<fighter or page>` | A link to that page on the Skullgirls Mobile wiki. |
| `/alias list fighter:<fighter>` | The other names that fighter answers to. |
| `/credits` | Who makes Stanley. |
| `/ping` | Whether Stanley is awake, and how slow its connection is. |

## Typing a fighter's name

Suggestions appear as you type — up to 25 of them — and picking one is always the surest way to
fill the option in. A suggestion that reads `alias → fighter` is one of a fighter's other names,
so you can see which fighter you are about to get.

Typing it out yourself works too, because the name is matched loosely:

- Case, spaces, punctuation and accents are ignored. `très chic`, `Tres Chic` and `treschic` all
  mean the same fighter.
- Aliases count as names.
- A near miss still lands: a typo or a dropped letter usually finds the fighter anyway.

When nothing matches, Stanley tells you in a reply only you can see, so a mistyped name leaves
nothing in the channel.

## What the card shows

`/fighter` answers with the fighter's name, then a card holding:

- **The heading:** the fighter's rarity, element and character, each with its emoji once those
  have been uploaded to the bot, and text alone until then.
- **The portrait**, as the card's thumbnail. Stanley links the image from the wiki; it hosts
  nothing itself, so a portrait can only be as good as the address an editor gave it.
- **The signature abilities**, one block each, named after the ability with its number —
  `RIFF WRATH (SA1)` — in order.
- **STATS**, as two lines: `Min` is the fighter at level 1 in the rarity it comes in, `Max` is
  the same fighter evolved to Diamond and levelled to the top. The maximum is worked out for
  each request from the base stats and the rarity rather than stored, so it is never out of step
  with a correction.
- **The stripe down the side**, coloured by the fighter's element.

A Raid Boss with more abilities than one Discord message holds continues in a second message
titled `<name> (continued)`.

## Wiki links

`/wiki` takes any fighter, and these pages that are not fighters:

| Page | Also answers to |
|---|---|
| Critless Skill Tree | `critless` |
| Fortune's Favor | — |

A fighter's page is its name with underscores for spaces, so `Vintage Virtuoso` is at
`/wiki/Vintage_Virtuoso`. A fighter whose wiki page is titled something else has the correct
address stored by an editor, and `/wiki` sends that instead.

## Other names

Players rarely use a fighter's full name, so editors give fighters aliases, and anything that
takes a fighter name accepts them — `/fighter`, `/wiki` and `/alias list` alike.
`/alias list` shows the ones a fighter has, or says it has none. The list started empty when
Stanley launched and grows as editors add to it, so an alias you expect may simply not be there
yet; ask an editor for it.

## Replies only you can see

Most answers are public, so the channel can read a card together. These are private:

- `/ping`.
- Every "no fighter matches that" reply.
- Everything the editor commands answer.

## Something wrong in the data?

Tell a wiki editor. They correct fighters, stats, abilities and aliases from inside Discord — no
developer, no deploy, no restart — and the correction is on the next `/fighter` straight away.

## For server admins

Stanley has no channel whitelist command of its own. Restrict it per channel and per role under
**Server Settings → Integrations → Stanley**, which is Discord's own command-permissions UI.

Stanley uses slash commands only and requests none of Discord's privileged intents, so it cannot
read your channels' messages, your member list or anyone's presence.

## Terms and privacy

[Terms of Service](StanleyTerms.txt) and [Privacy Policy](StanleyPrivacyPolicy.txt).

---

What Stanley is required to do is written in [`Stanley-v2-FRD.docx`](Stanley-v2-FRD.docx).
Installing and running Stanley is the [README](../README.md).
