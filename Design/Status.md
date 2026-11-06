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
- **Starting gold is 8,500** (was 6,000; the owner confirmed it). The fleet costs 6,000, so before this change the player had no gold left for cargo, and wages kept them in debt for good. The design simulator gave the player 2,500 gold for trading, so that's what is left over now. `GameState.ChooseFleet` makes the opening purchase, once: one Merchantman, two Brigs or three Sloops for the price of the most expensive ship (`GameData.FleetBudget`).
- **The flow** (`SliceScreens`): choose the fleet, then the 2 free routes, then play, then the win or lose screen. The clock is held while the fleet or free routes are being chosen, and while a route, port or send panel is open. Only one of those panels is open at a time.
- **Screens** (`Scripts/Slice/`):
  - `SliceFleetChoice`: the three openings with each ship's hold, speed, hull, wages and whether it can cross the ocean.
  - Clicking a route on the map opens `SliceRoutePanel`: distance, the storm, pirate and hurricane chances, the cost (or "Free"), and Open. Routes that could connect now are drawn brighter. A banner counts down the free routes.
  - Clicking a port opens `SlicePortPanel`: your ships there (step through them with < and >), hull and hold space, gold, and the market, with Buy 1, Buy 10, Sell 1 and Sell all at fixed prices. It also has Repair in any port. Shipyards add new ships and the Sails, Hull and Hold upgrades.
  - `SliceSendPanel`: pick a destination and read the preview (stops, arrival date, pirate risk, hurricane risk in season, chance of losing the ship), then Set sail.
  - `SliceNotifications`: a feed of arrivals, pirates, storms, hurricanes and sinkings at the bottom left.
  - `SliceFleetList`: one line per ship at the bottom right, clear of the port markers. Clicking a ship in port opens that port.
  - `SliceEndScreen`: goal reached, or mission failed with the reason. It replaces the HUD's placeholder text.
  - Every refused command shows its reason on the panel.
- **Map clicks** use the EventSystem: the camera has a `Physics2DRaycaster`, and ports and routes have 2D colliders (`SliceMapClick`).
- **Widgets:** `SliceUi` makes the widgets for both the scene builder and the rows built at runtime.
- **Tools:**
  - `Tools/test-runner/run.sh` runs the edit-mode tests on Mono without Unity.
  - The compile check has a small stub for the pointer event types (`stubs/EventSystems.cs`).
- **Tests:** 107 edit-mode tests pass in the Mono runner (it now runs `[TestCase]` rows too).
- **Review:** 10 findings plus the author's open questions. Fixed: the test runner skipped `[TestCase]` rows; buying a ship before `ChooseFleet` left `FleetChosen` false; the port panel overflowed with a 14-good market or 4+ ships (ships are now stepped through with < and >, Repair and Send share a row, market rows are shorter); the fleet list covered Natchez and New Orleans (moved to the bottom right); the end screen hard-coded "two years" and guessed the loss reason (`GameState.LostBecause` now says why); the clock could tick one frame after a panel opened; the fleet count was worked out in two places (`GameData.FleetCount`); duplicated date, colour and font helpers; the goal tests used a goal below the start gold.

**Phase 4: restart and the tutorial** (code only, not yet run in Unity).
- **Restart** reloads the scene. It's on the end screen, plus a small Restart button under 4× on the HUD, which asks "Sure?" and restarts on a second click within 3 seconds. A reload resets everything, so no component needs its own reset code. **Naval → Build Slice Scene** now adds `Slice.unity` to Build Settings (or enables it), because the reload needs a build index. If the scene isn't in Build Settings, Restart logs an error saying to run the menu command.
- **Sheltered voyages (Core):** while `GameState.ShelterNewVoyages` is true, voyages started by `Send` meet no hazards for their whole trip, even if it's turned off on the way. The preview still shows the route's real risks. 3 new tests.
- **Tutorial** (`SliceTutorial`): a guided first voyage. It shows one objective at a time in a box at the top centre, with a pulsing outline on the target. The steps are:
  1. open the start port
  2. buy goods (with a tip naming the best-paying good and where it sells)
  3. load and click Send
  4. pick a destination and read the preview
  5. set sail
  6. watch the ship arrive
  7. sell
  8. a wrap-up naming the goal and the date
  - **Skip** works at any step. It's a short ordered list of steps, each with text, a completion check and a target.
  - **When it starts:** after the opening (fleet choice and the 2 free routes). The voyage needs a ship and owned routes, and the opening already guides itself with its panel and banner.
  - **It can't be lost:** it turns `ShelterNewVoyages` on when it starts and off as soon as the first ship sets sail (or when it's skipped), so only that one voyage is sheltered.
  - **It doesn't get stuck:** if the player sets sail before the text asks, it jumps ahead. If the player opens another port, the outline points at Close. After Back on the send panel, it points at Send again. The sell step completes when the player sells from the tutorial ship, when that ship leaves the port, or when nothing aboard sells there.
  - **The box** stretches between the gold text and the Pause button, so it fits on 16:10 and 4:3 screens too (the text shrinks to fit). Only Skip takes clicks.
  - It runs only on the first play (see the follow-up below).
- **Tests:** 110 edit-mode tests pass in the Mono runner.
- **Review:** 7 findings. Fixed: every voyage stayed sheltered until the player clicked Done (now only the first); the sell step stuck if the ship left or bought more first; the box covered Restart on 16:10 and 4:3 screens (Restart moved under 4×, the box stretches and doesn't block clicks); no way back after Back on the send panel; steps counted any open port as the start port; the Skip label was looked up every frame. Left: a restart uses the same seed (reproducible on purpose), and the buying tip uses the first ship and today's prices (all opening ships are the same type).

**Phase 4 follow-up: the tutorial runs once.**
- **Decided:** starting gold stays at 8,500.
- When the tutorial is completed or skipped (Skip or Done), `SliceTutorial` saves the PlayerPrefs key `Naval.Slice.TutorialSeen` = 1, and it doesn't run again after a restart or on later launches. The flag lives only in the Slice layer; `Naval.Core` and the edit-mode tests never touch it.
- **If the game ends mid-tutorial** (or before it starts), it doesn't count as seen. If the tutorial ship is gone after it set sail, it counts as seen. Quitting mid-tutorial means it shows again next launch.
- **HUD Tutorial button** (under 2×, left of Restart): forgets the flag and restarts, so the tutorial runs in the new game. Like Restart, it asks "Sure?" and needs a second click within 3 seconds. Clicking the other button first moves "Sure?" to it and does nothing else.
- **Tests:** still 110 edit-mode tests.

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
     - the layout fits, also at New York, Liverpool or Bordeaux (14 goods) and with 4 or more ships at a shipyard
     - < and > step through the ships in port; Repair and Send sit side by side
   - **Shipyard:** buying a ship, each upgrade (two levels, then "complete") and repairing a damaged ship.
   - **Send:** the destination list, the preview numbers (hurricane risk only shows in season), "Only an ocean-going ship can sail there" for a sloop, and Set sail.
   - **Time:** it stops while a panel is open and runs again when it's closed.
   - **Notifications:** lines appear for arrivals and hazards and fade after about 10 seconds.
   - **Fleet list:** it sits at the bottom right, its lines fit, and it doesn't cover a port or the free-routes banner.
   - **Phase 2 checks:** the map fits the view, port names are readable, and ships glide along their routes and settle beside ports.
   - **Win and lose:** lower `goal_gold` in `rules.csv` (to 10,000, say) to see the win screen. Use 4× speed, or lower `mission_days`, to see the lose screen; it should give the number of days from `rules.csv`.
3. If clicks on the map don't register, check the Main Camera's `Physics2DRaycaster` and the EventSystem's Input System UI module.
4. **Phase 4** (build the scene again first):
   - **Build Settings:** after the menu command, `Slice.unity` is listed and enabled, and the scenes that were already there are unchanged.
   - **Restart:** the end screen's Restart starts a fresh game: fleet choice, 8,500 gold, the date back to 1 Jul 1770, no old notifications, markers or panels, speed 1×. The HUD Restart (under 4×) shows "Sure?", goes back to "Restart" after about 3 seconds, and restarts on a second click.
   - **Tutorial:**
     - it appears after the second free route, one objective at a time
     - the outline sits on the right thing at each step: the start port on the map, the market, Send, the destinations, Set sail, Close or 4×, the destination port, then the market there
     - the outline never blocks a click
     - the box doesn't cover the date, gold, speed or Restart buttons, or anything you need on the port and send panels
     - the first voyage meets no storms, pirates or hurricanes, and a second ship sent during the tutorial does meet them
     - try 16:10 and 4:3 in the Game view: the box clears the gold text, the speed buttons and Restart, and its text fits
     - open a different port, press Back on the send panel, or sail away from the destination without selling: the outline and text still point somewhere sensible
     - Skip hides it at any step, and voyages after that meet hazards again
     - doing things out of order (for example, setting sail straight away) doesn't leave it stuck
   - **Tutorial runs once** (to test from scratch, delete the `Naval.Slice.TutorialSeen` PlayerPrefs key, e.g. Edit → Clear All PlayerPrefs):
     - finish or skip it, then Restart: the new game has no tutorial; stop and press Play again: still none
     - let the game end mid-tutorial (lower `mission_days`), then Restart: the tutorial runs again
     - the HUD Tutorial button (under 2×, left of Restart) shows "Sure?", goes back after about 3 seconds, and on a second click starts a new game with the tutorial
     - click Restart then Tutorial (or the reverse): only the second shows "Sure?", and nothing restarts
     - at 16:9, 16:10 and 4:3 the Tutorial button doesn't overlap the tutorial box, the gold text, the speed buttons or Restart
5. Window → General → Test Runner → EditMode → Run All. Expect 110 passing.
6. Report anything that looks wrong or is hard to read, and how the opening and the first voyages feel.

## Next

**Phase 5: polish.**
- original map art (it replaces `Map.png`)
- ships moving along their routes
- the UI style
- sound and music
- arrival and sale feedback

## Decided

- **Starting gold is 8,500** (the owner confirmed it). At 6,000 the opening fleet took every coin, nothing could be bought and wages kept the player in debt (the only way out was selling a ship at half price).
- **The tutorial runs only on the first play.** Completing or skipping it is remembered; the HUD Tutorial button replays it.

## Open items

- **The clock stops while the port panel is open.** That keeps trading calm, but ships at sea also wait. Decide by playing.
- **The tutorial's preview shows the real risks** while the voyage itself is sheltered. The step text says it sails under escort. The alternative is showing 0%, which teaches less.
- **"Buy" and "load" are one action** in this game: bought crates go straight into the selected ship's hold. The tutorial covers them as two steps (buy, then fill the hold and click Send).
- **`goal_gold` (500,000) is a placeholder.** Tune it by playing, or with a quick bot, once phase 3 makes the game playable.
- The current map texture (`Arts/Prototype/Map.png`) is a screenshot of another game's map. It must be replaced with original art before the slice is shown to anyone (phase 5).
- **Art and sound for phase 5** need sourcing by the designer: an original map, ship sprites, UI style, sounds and music.
