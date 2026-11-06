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
- `SliceDemo` was a temporary stand-in for the phase 3 screens and has been removed.
- **Tests:** 99 edit-mode tests (4 new) pass in a stand-in runner.

**Phase 3: screens** (code only, not yet run in Unity). The game is playable start to finish.
- **Starting gold is now 8,500** (was 6,000). The fleet costs 6,000, so before this change the player had no gold left for cargo, and wages kept them in debt for good. The design simulator gave the player 2,500 gold for trading, so that's what is left over now. `GameState.ChooseFleet` makes the opening purchase, once: one Merchantman, two Brigs or three Sloops for the price of the most expensive ship (`GameData.FleetBudget`).
- **The flow** (`SliceScreens`): choose the fleet, then the 2 free routes, then play, then the win or lose screen. The clock is held while the fleet or free routes are being chosen, and while a route, port or send panel is open. Only one of those panels is open at a time.
- **Screens** (`Scripts/Slice/`):
  - `SliceFleetChoice`: the three openings with each ship's hold, speed, hull, wages and whether it can cross the ocean.
  - Clicking a route on the map opens `SliceRoutePanel`: distance, the storm, pirate and hurricane chances, the cost (or "Free"), and Open. Routes that could connect now are drawn brighter. A banner counts down the free routes.
  - Clicking a port opens `SlicePortPanel`: your ships there (pick one), hull and hold space, gold, and the market, with Buy 1, Buy 10, Sell 1 and Sell all at fixed prices. It also has Repair in any port. Shipyards add new ships and the Sails, Hull and Hold upgrades.
  - `SliceSendPanel`: pick a destination and read the preview (stops, arrival date, pirate risk, hurricane risk in season, chance of losing the ship), then Set sail.
  - `SliceNotifications`: a feed of arrivals, pirates, storms, hurricanes and sinkings at the bottom left.
  - `SliceFleetList`: one line per ship under the gold. Clicking a ship in port opens that port.
  - `SliceEndScreen`: goal reached, or mission failed with the reason. It replaces the HUD's placeholder text.
  - Every refused command shows its reason on the panel.
- **Map clicks** use the EventSystem: the camera has a `Physics2DRaycaster`, and ports and routes have 2D colliders (`SliceMapClick`).
- **Widgets:** `SliceUi` makes the widgets for both the scene builder and the rows built at runtime.
- **Tools:**
  - `Tools/test-runner/run.sh` runs the edit-mode tests on Mono without Unity.
  - The compile check has a small stub for the pointer event types (`stubs/EventSystems.cs`).
- **Tests:** 102 edit-mode tests (3 new, plus 3 rows of a TestCase) pass in the Mono runner.

## To check in Unity (designer)

1. Open the project and check the console for import or compile errors.
2. Run **Naval → Build Slice Scene** again (the scene changed), open `Slice.unity` and press Play. Play a game through:
   - **Fleet choice:** the panel appears, choosing a fleet puts the ships beside Havana, and 2,500 gold is left.
   - **Free routes:**
     - the banner counts down
     - clicking a route opens its panel
     - routes that touch the network are brighter
     - a route that doesn't touch the network is refused with a message
     - the date doesn't move until both free routes are open
   - **Clicks:** ports and routes are easy to hit, the near ends of routes don't steal clicks from ports, and clicks on a panel never reach the map behind it.
   - **Port panel:**
     - buying and selling update gold, hold space and the aboard column
     - a full hold and too little gold are refused with a message
     - the layout fits, also with several ships in port and at a shipyard
   - **Shipyard:** buying a ship, each upgrade (two levels, then "complete") and repairing a damaged ship.
   - **Send:** the destination list, the preview numbers (hurricane risk only shows in season), "Only an ocean-going ship can sail there" for a sloop, and Set sail.
   - **Time:** it stops while a panel is open and runs again when it's closed.
   - **Notifications:** lines appear for arrivals and hazards and fade after about 10 seconds.
   - **Phase 2 checks:** the map fits the view, port names are readable, and ships glide along their routes and settle beside ports.
   - **Win and lose:** lower `goal_gold` in `rules.csv` (to 10,000, say) to see the win screen. Use 4× speed, or lower `mission_days`, to see the lose screen.
3. If clicks on the map don't register, check the Main Camera's `Physics2DRaycaster` and the EventSystem's Input System UI module.
4. Window → General → Test Runner → EditMode → Run All. Expect 102 passing.
5. Report anything that looks wrong or is hard to read, and how the opening and the first voyages feel.

## Next

**Phase 4: the tutorial.** A short guided first voyage: buy, load, preview, send, arrive, sell.

## Open items

- **`goal_gold` (500,000) is a placeholder.** Tune it by playing, or with a quick bot, once phase 3 makes the game playable.
- The current map texture (`Arts/Prototype/Map.png`) is a screenshot of another game's map. It must be replaced with original art before the slice is shown to anyone (phase 5).
- **Art and sound for phase 5** need sourcing by the designer: an original map, ship sprites, UI style, sounds and music.
