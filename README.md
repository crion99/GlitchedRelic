# MysteriousMap / 房间里的尖塔

An experimental Slay the Spire 2 mod that combines an Isaac-style outer floor
with short branching Slay the Spire routes inside every room.

Current test build: [MysteriousMap 0.17.29](releases/MysteriousMap-v0.17.29.zip).
Unzip it into the game's `mods` directory so the final folder is
`mods/MysteriousMap` and contains the DLL, JSON and PCK files. Quit the game
before replacing an older version. The GitHub repository is named
`GlitchedRelic`; the in-game mod ID remains `MysteriousMap` for compatibility.

Target: Slay the Spire 2 `0.111.x`, .NET 9.

## Map modes

In solo play, disabling the room-grid map keeps the game's standard paths but
adds a selectable chest to the opening row and places 3–4 treasure rooms on
one complete route to the boss. The red pickaxe remains available in treasure
rooms, shops, and elite reward screens. Standard
maps have no bombs. The first three normal fights of each solo act use the weak
encounter pool; elite and boss fights retain their own pools.

With the room-grid map enabled, act one has an opening chest plus 2–3 other
treasure rooms; later solo acts have 3–4 treasure rooms total. Multiplayer
keeps the earlier map layout.

## Nested-map preview

- Each act uses a deterministic 5×5 outer coordinate space.
- Eleven visible outer rooms form a connected cardinal layout: start, normal
  rooms, two or three treasure rooms, and one boss room.
- Every non-boss outer room contains an independent `1 → 2 → 3 → 1 → exit`
  route. Route nodes use normal combat, elite, unknown, rest, shop, and treasure
  encounters. Reaching the unique exit clears the outer room.
- The boss room uses `rest site → boss`, followed by the second boss on
  applicable runs.
- Cleared adjacent outer rooms can be crossed without replaying their route or
  receiving their reward again.
- A regular secret room prefers positions touching several visible rooms and
  never touches the boss. A super-secret room touches exactly one ordinary room
  and prefers a position far from the start.
- Two or three ordinary rooms per act drop one bomb when cleared. A bomb can be
  bought once per shop for 30 gold.
- In any occupied room, four visible wall buttons spend bombs to probe neighboring empty
  cells. A correct wall reveals its hidden room; a failed probe still consumes
  the bomb.
- During combat a bomb costs two energy and schedules 40 unpowered damage to
  every enemy after three rounds.
- Bomb inventory, claimed drops, shop purchases, hidden-room identities, and
  discoveries are retained in the run save.
- Map generation and all runtime patches have deterministic and installed-game
  compatibility tests.

The other custom Isaac relics have been removed from new reward pools. Their
model definitions and artwork remain only so runs that already own them can
still load. The Mind and Treasure Map remain optional reveal settings.

Dataminer (the red pickaxe) is an active relic that needs no
recharge. In a treasure room, shop, or elite reward screen, its button
arms the next relic pickup, transforming it into a glitched relic with 3–8
trigger/outcome links (3 is most common, 8 is rare). Links can branch to
another outcome or another compatible trigger, so one event can activate multiple effects
and one effect can have multiple triggers, with at most two links per condition
or result. Later uses add another 3–8 links without a total cap; repeated
links stack,
including branches from existing pairs. Every matched trigger activates with
no extra proc chance. Common combat events are favored as triggers while rare
events remain in the pool. The MysteriousMap options include an off-by-default "Start with
Dataminer" switch for testing; it grants one Dataminer to each player in a new
run when enabled by the solo player or multiplayer host. The nested map starts with a
treasure room; the standard solo map instead offers one among its first choices.
The chest's Dataminer button is checked again when opened, so
it remains available before the relic is claimed. A separate off-by-default
option shows a no-cost instant-kill button in new single-player combats.
"Show glitched relic effects" is enabled by default and displays this run's
rolled combinations on the relic tooltip; disabling it hides only the details.
The remaining Dataminer relics use original transparent artwork in `assets/relics`.
The starting-relic and instant-kill switches do not change an existing save.
In multiplayer, each player can find and arm their own Dataminer. Its arming
decision and rolled combinations are sent to peers, as are shared bomb
purchases, wall blasts and combat fuses. The host's starting-Dataminer choice
is carried in the multiplayer start payload. All players need the same mod version.
Eighty-eight generic events cover combat, cards, powers, potions,
gold, purchases, rewards, relic acquisition, rest sites, rooms and bombs.
Eighty built-in results plus 180 vanilla-relic fragments include friendly and hostile powers, character-specific
card generation, potions, block, damage, healing, gold, max HP, bombs,
secret-room discovery and an exceptionally rare instant combat win.
Apotheosis and Wish fragments generate exact temporary cards in combat instead
of permanently filling the deck with off-character cards. Twelve curse-derived
drawbacks add self-debuffs, damage, or curse/status cards; a relic-granting result
is limited to once per room.
Multiplayer-only rolls observe teammates taking HP damage, damaging enemies,
gaining Block or gaining a buff. They can damage or heal a random living teammate,
put an Injury in that teammate's draw pile, generate a card from that teammate's
character pool into their hand, grant Strength, Block or a random buff, or
force an active teammate to end their turn through the game's ready signal.
The latter does nothing outside the player turn. These rolls are excluded in solo play.
Random enemy buffs include Strength, Soar's attack-damage reduction and Intangible;
the self-debuff pool includes Vulnerable, Slow, Constrict, Mind Rot and Doom.
Relic-like fragments include first-block doubling, Intangible, next-turn energy/draw,
Barricade, Afterimage, Double Damage, Vigor and Ritual. Feedback pairs such as
generated-card to generated-card, Block to Block, and Gold to Gold can chain.
One feedback chain permits at most five nested triggers and five generated cards;
there is also a 20-effect cascade budget and 40-effect per-turn ceiling. Each
combination activates whenever its condition is met; older saves' chance
fields are ignored. Incompatible out-of-combat combinations are excluded.
Poison results and Snecko Skull/Twisted Funnel fragments are Silent-only;
Focus from Data Disk is Defect-only. Existing saved combinations from other
characters are hidden and disabled, but not deleted. All
effects belong to the current player, and effects they cause do not recursively
trigger themselves without a safety limit.
The random-buff result separately draws from 28 vetted vanilla powers, including
card-granted buffs from other characters such as Demon Form, Echo Form,
Afterimage, Envenom and Dark Embrace. These are applied directly as powers;
randomly generated cards and potions still use the current character's pools.
Two separate random-debuff results apply one of eight enemy-safe or fourteen
player-safe vanilla debuffs, including cross-character card effects. They use
the same five-trigger feedback safety limit.

Other mods can register stable, namespaced custom triggers and outcomes via
`GlitchInteractionRegistry.RegisterTrigger`, `RegisterOutcome` and `EmitAsync`.
The random roll mixes these with built-in triggers/outcomes, and saves IDs
rather than load-order indices. Existing numeric combinations still load.
Generic events and the character card/potion pools already observe other
mods' content without explicit registration. Arbitrary relic code is not
automatically reverse-engineered into a safe standalone effect; custom
mechanics need a small adapter using this API.

Vanilla relic coverage is tracked individually in `RELIC_COVERAGE.md` for the
installed game build (299 relic classes). The first 180 mapped fragments have
independent condition aliases and exact-value results in the random pool.
Their source values were read from each relic's game data. Remaining relics stay marked Pending until
their behavior is checked and ported; a catalog entry alone is not an effect.

## Build

Set `STS2_GAME_DIR` to a Slay the Spire 2 installation, then run:

```powershell
.\build.ps1
```

The installable folder is written to `build\MysteriousMap`.

Run deterministic generation tests with:

```powershell
dotnet run --project .\tests\MysteriousMap.Tests\MysteriousMap.Tests.csproj -c Release
```

Resolve and temporarily apply every Harmony patch against the installed game:

```powershell
dotnet run --project .\tests\MysteriousMap.CompatibilityTests\MysteriousMap.CompatibilityTests.csproj -c Release
```

## Project layout

- `Generation/` contains the outer-floor and legacy grid generators.
- `Map/` contains the nested coordinate encoding and persistent run state.
- `Patches/` contains map UI, travel, bomb, room, save, and multiplayer hooks.
- `Rooms/` contains encounter and reusable-shop support.
- `assets/localization/` contains English, Simplified Chinese, and Traditional
  Chinese localization.
