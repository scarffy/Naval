# Naval: vertical slice design

This is the plan for the vertical slice: a short, complete session of the trading game at near-final quality. The numbers live in `Design/data/*.csv`, and `Design/sim/simulate.py` plays thousands of sessions with bot players to check them. When this doc and the CSVs disagree, the CSVs win.

## The slice in one paragraph

You start in Havana with one sloop and 1,500 gold. You have **90 days to reach 50,000 gold**, trading across 12 ports in the Gulf of Mexico, the Caribbean, North America and Europe:
- **Trade:** each port sells some goods cheaply and pays well for others. Buy low, sail, sell high.
- **Grow:** use the profit to buy ships and upgrade them at shipyards.
- **The reactive market:** every crate you trade moves that port's price, and prices drift back over a few days. A fleet on one route ruins its own prices, so growing means spreading out across goods, ports and oceans.
- **Market news:** shortages and gluts create opportunities worth chasing.
- **Hazards:** storms and pirates make some lanes risky. From about day 25 hurricane season builds in the Gulf and Caribbean, so plans have to change partway through.

**Done means:** someone who has never seen the game plays a session start to finish in a standalone build without help, wins or loses, and the build contains no placeholder or borrowed art.

## Scope

| | In the slice | Left out for now |
|---|---|---|
| Ports | All 12 (assets exist in `Datas/Ports/`) | |
| Goods | All 12 (assets exist in `Datas/Goods/`) | |
| Ships | Sloop, Brig, Fluyt | |
| Hazards | Storms, pirates, hurricanes (seasonal) | |
| Upgrades | Sails, Hull, Guns, Hold, two levels each | Crew skills, captains |
| Systems | Clock, sea lanes, reactive market, market news, shipyards, repairs, wages, win/lose | Save/load, contracts, insurance, escorts |

## Rules

### Time
- Time is continuous. Ships arrive whenever their voyage ends, and the player acts then. Wages, price recovery and market news happen once a day.
- Suggested pacing: about 8 seconds per day at 1× speed, with pause, 2× and 4×, which makes a 90-day session about 12 minutes at 1×.
- Every ship costs wages every day, sailing or docked. Gold can go negative. You can't buy anything while it is.

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
- **Market news:** each day there's a `market_event_chance_per_day` chance of a shortage (+`market_event_size`) or a glut (−`market_event_size`) of one good at one port. It fades by only `market_event_recovery_per_day`, so it lasts a couple of weeks, long enough to sail there. Show it as a news line ("Shortage of Iron in Veracruz").

### Sea lanes and voyages
- Ports are linked by the sea lanes in `lanes.csv`. A ship sails the shortest path along the lanes and passes through ports on the way without stopping. Voyage time = path distance ÷ ship speed.
- Each arrival costs `port_fee` in harbour dues and `port_time_days` of unloading and loading. This makes very short hops less attractive than they'd otherwise be.
- Before you send a ship, the route preview shows the arrival time and the risks on its lanes.

### Hazards
Each lane has its own chance per day at sea of each hazard. The lanes split into regions: Gulf, Caribbean, the American coast, the Atlantic crossings and European waters.

| Hazard | Where | Effect |
|---|---|---|
| Storm | Everywhere; worst in the North Atlantic and Bay of Biscay | 1–2 days' delay, 6–15 hull damage |
| Pirates | Worst in the Caribbean (Havana–Port-au-Prince is the most dangerous) | Take 25–50% of each good aboard and do 5–12 hull damage. If the ship's guns drive them off, no cargo is lost and damage is halved. |
| Hurricane | Gulf and Caribbean, plus a little on the southern crossings | 3–5 days' delay, 18–35 hull damage, 10–25% of cargo washed overboard |

Hurricanes are seasonal. There are none before day `hurricane_season_start` (25). Their chance then rises to the lanes' full value by day `hurricane_season_peak` (55) and stays there. In the simulations, good players spend 74% of their sea time on hurricane lanes before the season and 23% at its peak: they move north and across the Atlantic.

If a ship's hull reaches 0, the ship and its cargo are lost. Repairs happen in any port and cost `repair_cost_per_hull` per point.

### Ships and shipyards
**Havana, Boston, Liverpool and Bordeaux** are shipyards. That's where you buy ships and upgrades. Ships sell back for `ship_resale_fraction` of their price.

| Ship | Price | Crates | Speed | Hull | Wage/day | Role |
|---|---|---|---|---|---|---|
| Sloop | 1,000 | 15 | 1.50 | 40 | 13 | Cheap and fast. Fragile in hurricane season. |
| Brig | 1,500 | 32 | 1.30 | 70 | 16 | The all-rounder |
| Fluyt | 1,800 | 64 | 1.20 | 100 | 18 | A slow, sturdy bulk carrier for long hauls |

### Ship upgrades
Each ship can be upgraded along four tracks, two levels each, at shipyards only. A level's cost is a fraction of that ship type's price, so upgrading a Fluyt costs more than upgrading a Sloop. Level 2 replaces level 1's effects, and you must buy the levels in order. The numbers are in `upgrades.csv`.

| Track | Level 1 | Level 2 |
|---|---|---|
| **Sails** | **Extra canvas** (15% of ship price): 10% faster | **Studding sails** (35%): 20% faster |
| **Hull** | **Thicker planking** (10%): 40% more hull | **Copper sheathing** (30%): keeps the extra hull, 10% faster, repairs cost 40% less |
| **Guns** | **Swivel guns** (10%): drive off half of pirate attacks | **Carriage guns** (30%): drive off 80% of attacks, but 3% slower and 15% higher wages |
| **Hold** | **Rearranged hold** (10%): 30% more cargo | **Enlarged hold** (30%): 60% more cargo, 5% slower |

What these are, for the item descriptions:
- **Studding sails:** extra sails set on poles that slide out from the ends of the yards, used in fair weather to catch more wind.
- **Copper sheathing:** thin copper plates nailed over the hull below the waterline. Introduced in the 1760s–80s, they stopped shipworm from eating the planks and weed and barnacles from slowing the ship.
- **Swivel guns:** small cannon on a pivot, mounted on the ship's rail.
- **Carriage guns:** full-size cannon on wheeled carriages, firing through gun ports. They need gun crews, hence the higher wages.

### Winning and losing
- **Win:** gold on hand reaches `goal_gold` (50,000).
- **Lose:** day `day_limit` (90) ends without reaching the goal, or you have no ships and can't afford one.

## The world at a glance

| Port | Region | Cheap here | Pays well for |
|---|---|---|---|
| Natchez | Gulf (upriver) | Cotton, Tobacco | Guns, Iron |
| New Orleans | Gulf | Sugar, Cotton | Guns, Rum |
| Campeche | Gulf | Sugar, Tobacco | Iron, Guns, Steel |
| Veracruz | Gulf | Coffee | Iron, Steel, Guns |
| Havana | Caribbean (shipyard) | Tobacco, Sugar | Pitch, Steel, Iron |
| Port-au-Prince | Caribbean | Coffee, Sugar | Iron, Tobacco, Spices, Steel |
| Bridgetown | Caribbean | Rum, Sugar | Tobacco, Pitch, Iron, Cotton |
| New York | North America | Iron, Furs | Sugar, Coffee, Spices |
| Boston | North America (shipyard) | Pitch, Furs | Rum, Sugar, Oranges |
| Liverpool | Europe (shipyard) | Steel, Guns | Cotton, Sugar, Tobacco, Oranges, Pitch |
| Bordeaux | Europe (shipyard) | Guns, Spices | Coffee, Sugar, Furs |
| Seville | Europe | Oranges, Spices | Tobacco, Furs, Cotton |

The broad shape: colonial goods (sugar, tobacco, coffee, cotton, furs) sell well in Europe, and European manufactures (steel, guns, spices) sell well in the Americas. Each region also has short local trades, which matter early on when you have one small ship. Lane distances run from 3 (Natchez–New Orleans, Campeche–Veracruz, New York–Boston) to 12 (Havana–Seville). The simulator prints the ten strongest single trades when it starts.

## Balance targets and current results

The simulator plays 200 sessions with each bot:

- **Random:** sails anywhere with random cargo.
- **Fixed route:** picks the best-looking pair of ports (one with a shipyard) on day 1 and puts every ship on it, adding sloops as it can.
- **Greedy, 1 ship:** each time it's in port, picks the most profitable trip per day and never buys another ship.
- **Fleet of Sloops / Brigs / Fluyts:** the same, but it grows a fleet of one ship type, keeping enough gold to fill its ships.
- **Mixed fleet:** grows a fleet by always buying the biggest ship it can afford.
- **+ upgrades:** the best fleet bot, also buying all upgrades or only one track.

Bots keep trading after they reach the goal, so gold on day 90 can be compared across all of them. Results with seed 1:

| Bot | Win % | Median win day | Average gold, day 90 | Ships lost per session |
|---|---|---|---|---|
| Random | 0 | – | −339 | 0.7 |
| Fixed route (fleet) | 0 | – | 8,969 | 0.0 |
| Greedy, 1 ship | 0 | – | 21,222 | 0.1 |
| Fleet of Sloops | 99 | 77 | 84,843 | 2.2 |
| Fleet of Brigs | 94.5 | 80 | 72,347 | 0.2 |
| Fleet of Fluyts | 60.5 | 85 | 52,933 | 0.1 |
| Mixed fleet | 96.5 | 80 | 77,016 | 0.7 |
| Sloops + all upgrades | 100 | 74 | 106,946 | 1.5 |
| Sloops + Hold only | 100 | 76 | 91,122 | 1.9 |
| Sloops + Sails only | 99.5 | 76 | 93,105 | 2.4 |
| Sloops + Hull only | 100 | 77 | 89,521 | 0.8 |
| Sloops + Guns only | 100 | 77 | 88,470 | 2.2 |

Upgrade gains over no upgrades: Hold +7%, Sails +10%, Hull +6%, Guns +4%, all four +26%.

All 14 targets pass, and they also pass with seed 2:
1. A good player usually wins: the best fleet bot wins at least 85% of sessions.
2. It doesn't win too early: its median win day is between day 58 and day 81.
3. Growing the fleet matters: the 1-ship bot wins at most 50%.
4. Putting a fleet on one route fails: the fixed-route bot wins at most 20%.
5. Ignoring prices fails: the random bot wins at most 2%.
6. No route dominates: the best fleet bot's most-used route carries at most 30% of voyages.
7. Big ships aren't a trap: the mixed fleet earns at least 90% of the all-sloop fleet.
8. Hull upgrades make sinking rare: at most 1 loss per session, and at most half the unprotected fleet's rate.
9. All 12 ports matter: each gets at least 2% of the best bot's arrivals.
10. The Atlantic matters: at least 15% of the best bot's voyages cross it.
11. Hurricane season changes plans: time on hurricane lanes drops at least 20% at the season's peak.
12. Upgrades pay off: all four together add at least 10%.
13. Every upgrade track is worth buying on its own (at least +3%).
14. No track is a must-have: the best track gains at most 3× the worst.

## What to watch in playtests

The bots do exact price math every time they're in port, which a person won't do. They are also short-sighted: they never plan two voyages ahead. Real players will be worse at the first and better at the second. These are the points the numbers can't settle:

- **The late-game snowball.** The best bot has about 15,000 gold on day 60, 31,000 on day 70 and 60,000 on day 80. Part of that is the bots stopping ship purchases at day 65, so profits pile up. Still, the first half could feel slow. If it does, raise `start_gold` or lower `goal_gold`.
- **Sloop swarms are still the top raw strategy.** The mixed fleet is at about 91% of the sloop fleet, the tightest of the 14 targets. Managing 20 sloops may not be fun for a person, even though the bot doesn't mind. If playtesters agree, give bigger ships more advantages.
- **An all-Fluyt fleet is weak** (wins about 60%). Fluyts work as late-game Atlantic haulers inside a mixed fleet, not as a whole strategy.
- **Unprotected sloop fleets lose about 2 ships per session**, mostly to hurricanes. That's intended (sloops are fragile in hurricane season), and hull upgrades cut it to under 1. If losses feel unfair, lower `hurricane_damage_max`.

## Tuning

1. Edit a CSV in any spreadsheet app and keep the column names. A blank cell in `market.csv` means "not traded here".
2. Run `python3 Design/sim/simulate.py` from the repo root (Python 3, no packages needed). It takes about 3 minutes. Use `--runs 50` for a quick look and `--seed N` for a different random sequence.
3. Check the PASS/FAIL list at the end.

Changes that move results the most:

| Change | Effect |
|---|---|
| `goal_gold` and `start_gold` | Difficulty and pacing, without changing how the game plays |
| Values in `market.csv` further from 1.0 | Bigger margins and faster growth |
| `pressure_per_crate_sold` | How much big cargoes hurt, and so how good big ships are |
| Ship prices and wages | When expanding pays for itself, and which ship sizes win |
| `upgrades.csv` costs | Upgrades against more ships |
| Lane hazards and `hurricane_season_*` | How much the map changes over the session |

## For step 2 (building it in Unity)

- Load these numbers from data rather than hard-coding them: generate ScriptableObjects from the CSVs or read the CSVs at startup. The simulator stays the reference for the formulas.
- Keep the game state (market, fleet, clock, gold) in plain C# classes, separate from MonoBehaviours, so the formulas can be checked with edit-mode tests against the simulator.
- Ships follow the lane graph (shortest path). Voyage time and hazards come from lane data, not from NavMesh pathing or trigger colliders. The ship can still be animated along its lanes.
- Time is continuous, as in the simulator, so speed upgrades always shorten voyages.

## Still to decide

- **Map art.** The current texture (`Arts/Prototype/Map.png`) is a screenshot of another game's map. It needs to be replaced with an original map before the slice is shown to anyone.
- **Ship names.** The existing ship assets use the names Jupiter, Neptune and Marikh, which could become names for individual ships.
