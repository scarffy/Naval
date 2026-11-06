# Naval: vertical slice scope

**This is the source of truth for what's being built.** The goal is a playable, polished vertical slice inspired by Assassin's Creed III's trade convoys. Anything not listed under "In the slice" is parked. The other design docs (`EconomySpec.md`, `GameModes.md`, `Campaign.md`) describe parked ideas for later.

## In the slice

1. **One map, 12 ports:** Natchez, New Orleans, Campeche, Veracruz, Havana, Port-au-Prince, Bridgetown, New York, Boston, Liverpool, Bordeaux, Seville.
   - Each port sells some goods cheaply and pays well for others.
   - **Prices are fixed:** the player's trading never moves them. Prices rise only in hurricane season (item 7).
2. **14 goods:** Lumber & Naval Stores, Salt Cod, Flour, Logwood, Sugar, Tobacco, Rum, Wine, Hardware, Furs & Deerskins, Cloth, Coffee, Indigo, Cochineal. Every crate takes 1 space in the hold.
3. **Start with gold, choose your fleet.** Starting gold buys exactly one Merchantman (great), *or* two Brigs (good), *or* three Sloops (poor).
   - Sloops are coastal and can't cross the Atlantic.
   - Ships cost daily wages.
   - More ships can be bought at shipyards (Havana, Boston, Liverpool, Bordeaux), with no fleet limit.
4. **Routes:** ships sail only on sea routes the player owns.
   - The first 2 routes are free and the player picks them. Later routes cost gold and must connect to the network.
   - A voyage follows the shortest path along owned routes.
5. **Sending a ship:** load goods, pick a destination, and see the **route preview** first:
   - arrival date
   - pirate risk and, in season, hurricane risk (as percentages)
   - the chance of losing this ship given its current hull
6. **Hazards on the way:**
   - **Pirates** take part of the cargo and damage the hull.
   - **Storms** delay the ship and damage the hull.
   - **Hurricanes** delay the ship badly, damage the hull and wash cargo overboard.
   - Repairs in port cost gold.
   - **A ship whose hull reaches 0 is gone, with its cargo.** No insurance.
7. **Hurricane season every year,** by calendar date (June to November, peaking mid-August to mid-October). In season, Gulf and Caribbean goods sell for more elsewhere, and those ports pay more for imports. Braving the season pays.
8. **Ship upgrades** at shipyards:
   - **Sails:** faster.
   - **Hull:** more hull, then copper sheathing for cheaper repairs.
   - **Hold:** more cargo.

   Two levels each.
9. **Time:** a calendar starting July 1, 1770. About 1.5 real seconds per day at 1×, with pause, 2× and 4×.
10. **Goal:** reach a target amount of gold before the mission ends (2 years). Then a win or lose screen.
11. **Tutorial:** a short guided first voyage: buy, load, preview, send, arrive, sell.
12. **Polish:**
    - original map art (the current `Map.png` is another game's screenshot and must go)
    - ships moving along their routes
    - clear port and market screens
    - sound and music
    - satisfying arrival and sale feedback

Money is **gold** everywhere (`MoneyManager.Format`).

## Parked for after the slice
- Veteran difficulty and the reactive market; Beginner as a separate mode
- port levels, port growth and port buildings; stock limits
- spoiling and bulky cargo
- warships, the Guns upgrade, route clearing and patrols
- market news
- campaign chapters, missions and the Asia regions
- endless mode

## Data
The slice's numbers live in CSV files under `Assets/_NavalTradeMinigame/Resources/SliceData/`: goods, market, ports, routes, ships, upgrades and rules. The game loads them at startup. All numbers are first guesses and get tuned by playing.

## How it's built
- **Game rules** are plain C# with no Unity dependencies, in `Assets/_NavalTradeMinigame/Scripts/Core/`. They cover:
  - data loading
  - clock and calendar
  - market
  - fleet and ships
  - routes and pathfinding
  - voyages and hazards
  - repairs, wages, dues
  - goal and win/lose

  They use a seeded random source so bugs can be reproduced. Edit-mode tests cover the rules.
- **Presentation** (the map, ships, screens) is MonoBehaviours that read the game state.
- A new scene, `Slice.unity`, is built by an Editor menu command, so it can be set up without hand-editing scene files. The old prototype scene stays untouched until the slice works.
- **Every coding phase ends with a review** (`.claude/skills/review-after-code`): correctness first, then pragmatism, then style. `Tools/compile-check/check.sh` checks every change against Unity 2021.3's real libraries. Running and playing can only be done in the Unity editor.

## Phases

| Phase | What | Done when |
|---|---|---|
| 1 | Game rules and data, with tests | All rules work in code and tests pass in Unity |
| 2 | The map: ports, routes, ships sailing, clock and time controls, gold display | You can watch ships sail and time pass |
| 3 | Screens: fleet choice, port market, shipyard and upgrades, buying routes, route preview, notifications, win/lose | The game is playable start to finish |
| 4 | Tutorial | A new player gets through the first voyage unaided |
| 5 | Polish: art, ships, UI style, sound, animation | It looks and feels finished |

After each phase: review, fix, push, then you test in Unity and report what's wrong.
