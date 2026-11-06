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

**Phase 2: the map** (code only, not yet run in Unity).
- **Data:** `ports.csv` has `map_x,map_y` (x west to east 0 to 100, y south to north 0 to 60). `Voyage` exposes its `Legs` and `PositionAt(progress)` for drawing ships.
- **Scene:** menu **Naval → Build Slice Scene** builds `Assets/_NavalTradeMinigame/Scenes/Slice.unity` (it asks before replacing it). It isn't added to Build Settings.
- **Presentation** (`Scripts/Slice/`):
  - `SliceClock`: 1.5 s per day at 1×, with pause, 2× and 4×
  - `SliceMap`: placeholder sea, port dots and names (shipyards are bigger and gold), and route lines (owned routes bright, the rest faint; ocean routes blue)
  - `SliceShips`: ship markers that glide along their routes and sit beside ports
  - `SliceHud`: date, days left, gold and goal, speed buttons, and a placeholder Won/Lost text. It shows the data error if the CSVs fail to load.
- **`SliceDemo` is temporary.** Until phase 3's screens exist, it opens the two free routes (Havana to New Orleans and Havana to Port-au-Prince), buys two Brigs and keeps sending them to random ports. The ships carry no cargo, so wages and dues push gold below zero. That's expected. Turn it off with its `Run Demo` box, and delete it in phase 3.
- **Tests:** 98 edit-mode tests (3 new) pass in a stand-in runner.

## To check in Unity (designer)

1. Open the project and check the console for import or compile errors.
2. Run **Naval → Build Slice Scene**, open `Slice.unity` and press Play. Check that:
   - the whole map fits the Game view, including at other aspect ratios
   - the port names are readable (the size is `Label Size` on `SliceMap`)
   - two Brigs start beside Havana and sail along the owned routes, smoothly and facing their direction of travel
   - they pass through Havana between New Orleans and Port-au-Prince, hold still during storm delays, and settle beside the port on arrival
   - the date moves on about every 1.5 s, and Pause, 1×, 2× and 4× work and highlight
   - days left and gold update
3. The EventSystem gets the Input System's UI module, because the project uses the new Input System only. If the buttons don't respond to clicks, check that module.
4. Window → General → Test Runner → EditMode → Run All. Expect 98 passing.
5. Report anything that looks wrong, plus how the map and the ships' speed feel.

## Next

**Phase 3: screens.** Fleet choice, port market, shipyard and upgrades, buying routes, route preview, notifications, win and lose. These replace `SliceDemo`.

## Open items

- **`goal_gold` (500,000) is a placeholder.** Tune it by playing, or with a quick bot, once phase 3 makes the game playable.
- The current map texture (`Arts/Prototype/Map.png`) is a screenshot of another game's map. It must be replaced with original art before the slice is shown to anyone (phase 5).
- **Art and sound for phase 5** need sourcing by the designer: an original map, ship sprites, UI style, sounds and music.
