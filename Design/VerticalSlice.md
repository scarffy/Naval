# Naval: vertical slice design

> **Superseded:** what's being built now is defined in `Design/SliceScope.md`. This doc is kept for its research and balance history. The goods, time scale and port rules in it are economy version 1 (12 goods, a 90-day mission). Version 2 (14 goods, 2-year missions, 5-level ports, buildings) is described in `Design/EconomySpec.md`. This doc will be updated once version 2's balance settles.

This is the plan for the vertical slice: one complete mission of the trading game at near-final quality. It's Chapter 1, Mission 1 of the campaign in `Design/Campaign.md`. The numbers live in `Design/data/*.csv`, and `Design/sim/simulate.py` plays thousands of sessions with bot players to check them. When this doc and the CSVs disagree, the CSVs win.

## The slice in one paragraph

You start in Havana with one sloop and 2,500 gold. You have **90 days to reach 50,000 gold**, trading across 12 ports in the Gulf of Mexico, the Caribbean, North America and Europe:
- **Routes:** you choose your first 2 sea routes for free. Every later route costs gold, so the map opens up as your company grows.
- **Trade:** each port sells some goods cheaply and pays well for others. Buy low, sail, sell high.
- **Grow:** use the profit to buy ships and upgrade them at shipyards. Sloops are cheap coastal ships. Crossing the Atlantic to the rich European markets takes a Brig or a Fluyt.
- **The reactive market:** every crate you trade moves that port's price, and prices drift back over a few days. A fleet on one route ruins its own prices, so growing means spreading out.
- **Market news:** shortages and gluts create opportunities worth chasing.
- **Hazards:** storms and pirates make some routes risky. From about day 25 hurricane season builds in the Gulf and Caribbean, so plans have to change partway through.

**Done means:** someone who has never seen the game plays the mission start to finish in a standalone build without help, wins or loses, and the build contains no placeholder or borrowed art.

## Scope

| | In the slice | Left out for now |
|---|---|---|
| Ports | All 12 (assets exist in `Datas/Ports/`) | Asia and the rest of the world (later chapters) |
| Goods | All 12 (assets exist in `Datas/Goods/`) | |
| Ships | Sloop, Brig, Fluyt | |
| Hazards | Storms, pirates, hurricanes (seasonal) | Wars, blockades |
| Upgrades | Sails, Hull, Guns, Hold, two levels each | Crew skills, captains |
| Systems | Clock, routes you buy, reactive market, market news, shipyards, repairs, wages, win/lose | Save/load, rival companies, the other mission types |

## Rules

Money is called **gold** everywhere: UI, docs and data.

### Time
- Time is continuous. Ships arrive whenever their voyage ends, and the player acts then. Wages, price recovery and market news happen once a day.
- Suggested pacing: about 8 seconds per day at 1× speed, with pause, 2× and 4×, which makes a 90-day mission about 12 minutes at 1×.
- Ships cost wages for every day at sea; docked ships pay nothing (`docked_wage_fraction` in `rules.csv`, 0 since the first playtest). Gold can go negative. You can't buy anything while it is.

### Routes
- The map has 17 sea routes, each linking two neighbouring ports (`lanes.csv`). **Your ships can only sail on routes you own.** A voyage follows the shortest path along your routes and passes through ports on the way without stopping.
- You start with none. **Your first `free_routes` (2) routes are free**, and you choose them. After that, each route costs its `price`, from 250 gold for a short Gulf hop to 2,000 for an Atlantic crossing.
- A new route must connect to your network, which starts at Havana. Routes can be bought at any time from the map; no ship needs to be there.
- Four routes are **ocean crossings** (`ocean` = yes): Boston–Liverpool, New York–Liverpool, Seville–Bridgetown and Seville–Havana. Only ocean-going ships can sail them.
- The opening choice matters, but there's no single right answer. The three best openings all include Havana–Port-au-Prince, plus New Orleans, Campeche or Veracruz. They finish within 4% of each other.

### Ports and prices
- `market.csv` sets what each port trades. A number below 1 means the port produces the good (cheap), above 1 means it wants it (dear), and a blank means the port doesn't trade it.
- For a good `g` that port `p` trades:

  ```
  normal price = base_price[g] × market[p][g]
  current mid  = normal price × (1 + pressure[p][g] + news[p][g])     (clamped to pressure_min..pressure_max)
  buy price    = current mid × (1 + spread / 2)
  sell price   = current mid × (1 − spread / 2)
  ```

- **Reactive market:** buying one crate adds `pressure_per_crate_bought` to that good's pressure at that port, and selling one subtracts `pressure_per_crate_sold`. Prices change crate by crate, so the UI should show the total for the amount you're about to trade. Each day the pressure fades by `pressure_recovery_per_day` and drifts randomly by `market_noise_per_day`.
- **Market news:** each day there's a `market_event_chance_per_day` chance of a shortage (+`market_event_size`) or a glut (−`market_event_size`) of one good at one port. It fades by only `market_event_recovery_per_day`, so it lasts a couple of weeks. Show it as a news line ("Shortage of Iron in Veracruz").
- Each arrival costs `port_fee` in harbour dues and `port_time_days` of unloading and loading. This makes very short hops less attractive than they'd otherwise be.

### Hazards
Each route has its own chance per day at sea of each hazard (`lanes.csv`).

| Hazard | Where | Effect |
|---|---|---|
| Storm | Everywhere; worst in the North Atlantic and Bay of Biscay | 1–2 days' delay, 6–15 hull damage |
| Pirates | Worst in the Caribbean (Havana–Port-au-Prince is the most dangerous) | Take 25–50% of each good aboard and do 5–12 hull damage. If the ship's guns drive them off, no cargo is lost and damage is halved. |
| Hurricane | Gulf and Caribbean, plus a little on the southern routes | 3–5 days' delay, 18–35 hull damage, 10–25% of cargo washed overboard |

Hurricanes are seasonal. There are none before day `hurricane_season_start` (25). Their chance then rises to each route's full value by day `hurricane_season_peak` (55) and stays there. Good players move north and to Europe: their time on hurricane routes drops from 96% before the season to 28% at its peak.

If a ship's hull reaches 0, the ship and its cargo are lost. Repairs happen in any port and cost `repair_cost_per_hull` per point.

### Ships and shipyards
**Havana, Boston, Liverpool and Bordeaux** are shipyards. That's where you buy ships and upgrades. Ships sell back for `ship_resale_fraction` of their price.

| Ship | Price | Crates | Speed | Hull | Wage/day | Ocean-going | Role |
|---|---|---|---|---|---|---|---|
| Sloop | 1,000 | 15 | 1.50 | 40 | 13 | No | Cheap, fast, coastal. Fragile in hurricane season. |
| Brig | 1,500 | 32 | 1.30 | 70 | 16 | Yes | The all-rounder |
| Fluyt | 1,800 | 64 | 1.20 | 100 | 18 | Yes | A slow, sturdy bulk carrier for long hauls |

### Ship upgrades
Each ship can be upgraded along four tracks, two levels each, at shipyards only. A level's cost is a fraction of that ship type's price, so upgrading a Fluyt costs more than upgrading a Sloop. Level 2 replaces level 1's effects, and you must buy the levels in order. The numbers are in `upgrades.csv`.

| Track | Level 1 | Level 2 |
|---|---|---|
| **Sails** | **Extra canvas** (15% of ship price): 8% faster | **Studding sails** (35%): 16% faster |
| **Hull** | **Thicker planking** (10%): 40% more hull | **Copper sheathing** (30%): keeps the extra hull, 10% faster, repairs cost 40% less |
| **Guns** | **Swivel guns** (10%): drive off half of pirate attacks | **Carriage guns** (30%): drive off 80% of attacks, but 3% slower and 15% higher wages |
| **Hold** | **Rearranged hold** (8%): 25% more cargo | **Enlarged hold** (22%): 50% more cargo |

What these are, for the item descriptions:
- **Studding sails:** extra sails set on poles that slide out from the ends of the yards, used in fair weather to catch more wind.
- **Copper sheathing:** thin copper plates nailed over the hull below the waterline. Introduced in the 1760s–80s, they stopped shipworm from eating the planks and weed and barnacles from slowing the ship.
- **Swivel guns:** small cannon on a pivot, mounted on the ship's rail.
- **Carriage guns:** full-size cannon on wheeled carriages, firing through gun ports. They need gun crews, hence the higher wages.

### Winning and losing
- **Win:** gold on hand reaches `goal_gold` (50,000).
- **Lose:** day `day_limit` (90) ends without reaching the goal, or you have no ships and can't afford one.

## The world at a glance

Generated from `market.csv`: "cheap" means a multiplier of 0.6 or less, "pays well" means 1.6 or more.

| Port | Region | Cheap here | Pays well for |
|---|---|---|---|
| Natchez | Gulf (upriver) | Cotton, Tobacco | Guns, Iron |
| New Orleans | Gulf | Sugar, Cotton | Guns, Rum |
| Campeche | Gulf | Sugar, Tobacco | Guns, Steel, Iron, Pitch |
| Veracruz | Gulf | Coffee | Steel, Guns, Iron |
| Havana | Caribbean (shipyard) | Tobacco, Sugar | Steel, Pitch |
| Port-au-Prince | Caribbean | Coffee, Sugar | Spices, Iron, Tobacco |
| Bridgetown | Caribbean | Rum, Sugar | Tobacco, Pitch, Iron |
| New York | North America | Iron, Furs | Spices, Sugar, Coffee |
| Boston | North America (shipyard) | Pitch, Furs | Rum, Sugar |
| Liverpool | Europe (shipyard) | Steel, Guns | Cotton, Sugar, Tobacco, Oranges |
| Bordeaux | Europe (shipyard) | Guns, Spices | Coffee, Sugar, Furs |
| Seville | Europe | Oranges, Spices | Tobacco, Furs, Cotton |

The broad shape: colonial goods sell for about double in Europe, and European manufactures (steel, guns, spices) sell well in the Americas. Each region also has short local trades, which matter early on when you have one small ship. Europe's own internal trades are the richest on the map, but you need an ocean-going ship and an Atlantic route to reach them. The simulator prints the ten strongest single trades when it starts.

## Balance targets and current results

The simulator plays 200 sessions with each bot:

- **Random:** opens random routes and sails anywhere with random cargo.
- **Fixed route:** takes its free routes, picks the best-looking pair of ports, and puts every ship on it, adding sloops as it can. It never buys more routes.
- **Greedy, 1 ship:** each time it's in port, picks the most profitable trip per day. It opens routes but never buys another ship.
- **Fleet of Sloops / Brigs / Fluyts:** the same, but it grows a fleet of one ship type, keeping enough gold to fill its ships.
- **Mixed fleet:** grows a fleet by always buying the biggest ship it can afford.
- **Free routes only:** the best fleet bot, but it never buys a route beyond its free ones.
- **Opening 1–3:** the best fleet bot, forced to take each of the three best pairs of free routes.
- **+ upgrades:** the best fleet bot, also buying all upgrades or only one track.

Bots buy a route when its extra income would repay the price within about 12 days, looking two purchases ahead. They keep trading after they reach the goal, so gold on day 90 can be compared across all of them. Results with seed 1:

| Bot | Win % | Median win day | Average gold, day 90 | Ships lost per session |
|---|---|---|---|---|
| Random | 0 | – | −624 | 0.9 |
| Fixed route (fleet) | 0 | – | 6,966 | 1.8 |
| Greedy, 1 ship | 0 | – | 22,204 | 0.2 |
| Fleet of Sloops (can't cross) | 97.5 | 81 | 67,969 | 3.3 |
| Fleet of Brigs | 98.5 | 77 | 90,135 | 0.2 |
| Fleet of Fluyts | 77.5 | 82 | 64,360 | 0.2 |
| Mixed fleet | 99 | 78 | 88,465 | 0.6 |
| Brigs, free routes only | 0 | – | 23,218 | 0.2 |
| Brigs + all upgrades | 100 | 75 | 115,236 | 0.1 |
| Brigs + Hold only | 99 | 77 | 94,747 | 0.3 |
| Brigs + Sails only | 99.5 | 76 | 102,287 | 0.3 |
| Brigs + Hull only | 97 | 77 | 94,803 | 0.1 |
| Brigs + Guns only | 100 | 76 | 100,799 | 0.3 |

- **Upgrade gains** over no upgrades: Hold +5%, Sails +13%, Hull +5%, Guns +12%, all four +28%.
- **Openings:** Havana–Port-au-Prince with New Orleans–Havana 90,135; with Campeche–Havana 91,069; with Veracruz–Havana 87,986.
- **Routes owned** by the Brig fleet: 6.3 of 17 by day 30, 10.5 by day 60, 11.6 by day 90.

All 16 targets pass, and they also pass with seed 2:
1. A good player usually wins: the best fleet bot wins at least 85% of sessions.
2. It doesn't win too early: its median win day is between day 58 and day 81.
3. Growing the fleet matters: the 1-ship bot wins at most 50%.
4. Putting a fleet on one route fails: the fixed-route bot wins at most 20%.
5. Ignoring prices fails: the random bot wins at most 2%.
6. No route dominates: the best bot's most-used route carries at most 30% of voyages.
7. Big ships aren't a trap: the mixed fleet earns at least 90% of the all-sloop fleet.
8. Hull upgrades make sinking rare: at most 1 loss per session, and at most half the unprotected rate.
9. All 12 ports matter: each gets at least 2% of the best bot's arrivals.
10. The Atlantic matters: at least 15% of the best bot's voyages cross it.
11. Hurricane season changes plans: time on hurricane routes drops at least 20% at the peak.
12. Opening routes matters: a fleet that keeps only its free routes earns at most 60% of one that expands. (It earns about 26%.)
13. The opening isn't solved: the second-best free pick earns within 10% of the best.
14. Upgrades pay off: all four together add at least 10%.
15. Every upgrade track is worth buying on its own (at least +3%).
16. No track is a must-have: the best track gains at most 3× the worst.

## What to watch in playtests

The bots do exact price math every time they're in port, which a person won't do. They are also short-sighted: they never plan two voyages ahead. Real players will be worse at the first and better at the second. These are the points the numbers can't settle:

- **A slow first half.** The Brig fleet has about 11,000 gold on day 60, 28,000 on day 70 and 60,000 on day 80. Part of that is the bots stopping purchases at day 65, so profits pile up. If the first half drags, raise `start_gold` or lower `goal_gold`.
- **Europe is optional, but rich.** An all-sloop fleet never leaves the Americas and still usually wins, ending with about 68,000 against 90,000 for a fleet that crosses. That seems right for a first mission. A later mission could require Europe.
- **Sloops sink often**: about 3 per session for an all-sloop fleet, mostly in hurricane season. That's intended. Bigger ships and hull upgrades make losses rare. If it feels unfair, lower `hurricane_damage_max`.
- **The tightest targets:** Bridgetown gets 2.1% of visits (the target is 2%), and the mixed fleet sits close to its 90% line. Small changes to Bridgetown's prices or the Brig's stats can tip them.

## Tuning

1. Edit a CSV in any spreadsheet app and keep the column names. A blank cell in `market.csv` means "not traded here".
2. Run `python3 Design/sim/simulate.py` from the repo root (Python 3, no packages needed). It takes about 3 minutes. Use `--runs 50` for a quick look and `--seed N` for a different random sequence.
3. Check the PASS/FAIL list at the end.

Changes that move results the most:

| Change | Effect |
|---|---|
| `goal_gold` and `start_gold` | Difficulty and pacing, without changing how the game plays |
| `free_routes` and route prices (`lanes.csv`) | How fast the map opens up |
| `ocean_going` (`ships.csv`) | Whether sloops can cross to Europe. Setting it to "yes" makes Europe a day-1 option and breaks several targets. |
| Values in `market.csv` further from 1.0 | Bigger margins and faster growth |
| `pressure_per_crate_sold` | How much big cargoes hurt, and so how good big ships are |
| Ship prices and wages | When expanding pays for itself, and which ship sizes win |
| `upgrades.csv` costs | Upgrades against more ships |
| Route hazards and `hurricane_season_*` | How much the map changes over the session |

## For step 2 (building it in Unity)

- Load these numbers from data rather than hard-coding them: generate ScriptableObjects from the CSVs or read the CSVs at startup. The simulator stays the reference for the formulas.
- Keep the game state (owned routes, market, fleet, clock, gold) in plain C# classes, separate from MonoBehaviours, so the formulas can be checked with edit-mode tests against the simulator.
- Ships follow owned routes (shortest path, with ocean crossings only for ocean-going ships). Voyage time and hazards come from route data, not from NavMesh pathing or trigger colliders. The ship can still be animated along its routes.
- Time is continuous, as in the simulator, so speed upgrades always shorten voyages.
- Keep the mission's goal separate from the game rules (a "mission" record with a goal type, target and day limit), so the other mission types in `Campaign.md` can reuse everything.

## Still to decide

- **Map art.** The current texture (`Arts/Prototype/Map.png`) is a screenshot of another game's map. It needs to be replaced with an original map before the slice is shown to anyone.
- **Setting and year.** See `Design/Campaign.md`. The proposal is about 1770, with the mission running from July 1 to late September, so hurricane season lines up with real history.
- **Ship names.** The existing ship assets use the names Jupiter, Neptune and Marikh, which could become names for individual ships.
- **The Seville port asset is spelled "Sevile"** (`Datas/Ports/Port 12.asset`).
