# Naval: status and how we work

Read this together with `Design/SliceScope.md`, which defines what's being built.

## Working agreements

- **Goal:** a playable, polished vertical slice inspired by Assassin's Creed III's trade convoys (only AC3; Black Flag is background). Build only what `SliceScope.md` lists. Don't over-engineer.
- **No AI attribution anywhere:** commit messages, trailers, authors, code comments and files. Commit as `scarffy` / `34879344+scarffy@users.noreply.github.com`.
- **Branch:** `vertical-slice-design` only.
- **Every code change is reviewed before it's committed** (`.claude/skills/review-after-code`): correctness first, then pragmatism, then style. `Tools/compile-check/check.sh` must pass.
- **Coding is done by Sonnet subagents.** The main session reviews their work.
- **Unity can't run in the cloud session.** Code is compile-checked against Unity 2021.3's real libraries. Playing and the Test Runner happen on the designer's machine, and they report what's wrong.
- **Money is "gold" everywhere.** The setting is inspired by history, not faithful to it.

## Done

**Design and research** are in `Design/`: the slice scope, research on 1770s ships and goods, Asia, Railway Empire and other references, and AC3's minigames. Parked designs: economy v2, game modes, campaign.

**Phase 1: game rules and data.**
- **Data:** `Assets/_NavalTradeMinigame/Resources/SliceData/*.csv` (goods, market, ports, routes, ships, upgrades, rules).
- **Rules:** `Assets/_NavalTradeMinigame/Scripts/Core/` (assembly `Naval.Core`, no Unity dependencies). Covers:
  - data loading with clear errors
  - calendar and hurricane season
  - fixed prices with the season premium
  - the fleet choice
  - routes (2 free, the rest bought, must connect, sloops can't cross oceans) and shortest paths
  - voyages with day-by-day hazards
  - sinking, repairs, wages and dues
  - upgrades (Sails, Hull, Hold)
  - the route preview (arrival, pirate and hurricane risk, chance of losing the ship)
  - win and lose
- **Commands:** the UI talks to `GameState` through commands that return a result with a reason.
- **Startup:** `Assets/_NavalTradeMinigame/Scripts/Slice/SliceBootstrap.cs` loads the CSVs and creates the game. It runs early (`DefaultExecutionOrder(-100)`) and exposes `LoadError` if the data is bad.
- **Tests:** 95 edit-mode tests in `Assets/_NavalTradeMinigame/Tests/EditMode/`. They pass in a stand-in runner, but not yet in Unity's Test Runner.
- **Review:** 10 findings were all fixed: a ship soft-lock, startup order, missing `.meta` files, a null crash, the debt message, duplicated sailing rules, a hard-coded track count, confusing method names, preview speed, and an invisible character.

## To check in Unity (designer)

1. Open the project. The new `.meta` files are committed, so nothing should be regenerated.
2. Window → General → Test Runner → EditMode → Run All. Expect 95 passing.
3. Report any import errors or failing tests.

## Next

**Phase 2: the map.**
- A new `Slice.unity` scene built by an Editor menu command.
- Ports placed on a map, routes drawn between them, ships moving along their routes.
- The clock and calendar, with pause, 1×, 2× and 4×.
- Gold and the goal shown on screen.

Placeholder map art is fine for now; the final map comes in phase 5. Ports need map positions. Add them to `ports.csv`.

## Open items

- **`goal_gold` (500,000) is a placeholder.** Tune it by playing, or with a quick bot, once phase 3 makes the game playable.
- The current map texture (`Arts/Prototype/Map.png`) is a screenshot of another game's map. It must be replaced with original art before the slice is shown to anyone (phase 5).
- **Art and sound for phase 5** need sourcing by the designer: an original map, ship sprites, UI style, sounds and music.
