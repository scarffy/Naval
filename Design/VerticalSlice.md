# Naval: vertical slice design

This is the plan for the vertical slice: a short, complete session of the trading game at near-final quality. The numbers live in `Design/data/*.csv`, and `Design/sim/simulate.py` plays thousands of sessions with bot players to check them. When this doc and the CSVs disagree, the CSVs win.

## The slice in one paragraph

You start in Havana with one sloop and 1,500 gold. You have **60 days to reach 25,000 gold**. Each port sells some goods cheaply and pays well for others. Buy low, sail, sell high, and use the profit to buy more ships. The twist is the **reactive market**: every crate you sell pushes that port's price down, and every crate you buy pushes the price up. Prices drift back over a few days. One ship on one route does fine, but a fleet on one route ruins its own prices, so growing means spreading out across goods and ports. Storms and pirates make some routes riskier than others.

**Done means:** someone who has never seen the game plays a session start to finish in a standalone build without help, wins or loses, and the build contains no placeholder or borrowed art.

## Scope

| | In the slice | Left out for now |
|---|---|---|
| Ports | New Orleans, Havana, Veracruz, Port-au-Prince, Bridgetown | The other 7 port assets |
| Goods | Sugar, Cotton, Tobacco, Rum, Iron, Coffee | Furs, Guns, Orange, Pitch, Spices, Steel |
| Ships | Sloop, Brig, Fluyt | Ship upgrades |
| Events | Storm, Pirates | Hurricane (can come back as a rare, stronger storm) |
| Systems | Day clock, reactive market, voyages, events, repairs, wages, win/lose | Save/load, XP perks, escorts, insurance, contracts |

The five ports and six goods already exist as assets (`Datas/Ports/Port 1, 3, 5, 6, 9` and `Datas/Goods/`).

## Rules

### Time
- The game runs in days. Ships act when they're in port. Everything else happens at the end of each day.
- Suggested pacing: about 10 seconds per day at 1× speed, with pause, 2× and 4×, which makes a 60-day session about 10 minutes at 1×.
- Each ship's crew costs wages every day, whether the ship is sailing or docked (`ships.csv`, `wage_per_day`). Gold can go negative. You can't buy anything while it is.

### Prices
Every port trades every good. For good `g` at port `p`:

```
normal price = base_price[g] × market[p][g]                   (goods.csv, market.csv)
current mid  = normal price × (1 + pressure[p][g])
buy price    = current mid × (1 + spread / 2)
sell price   = current mid × (1 − spread / 2)
```

`market.csv` sets the character of each port. Values below 1 mean the port produces the good (cheap), and values above 1 mean it wants it (dear).

### Reactive market
- Buying one crate adds `pressure_per_crate_bought` to that good's pressure at that port. Selling one crate subtracts `pressure_per_crate_sold`.
- Prices change crate by crate, so a big sale earns less per crate than a small one. The UI should show the total for the amount you're about to trade, not only the price of the first crate.
- Pressure stays between `pressure_min` and `pressure_max`.
- At the end of each day, every pressure fades toward zero by `pressure_recovery_per_day` and drifts randomly by `market_noise_per_day`.

### Voyages
- Voyage length in days = ceil(route distance ÷ ship speed) (`routes.csv`, `ships.csv`). Faster ships arrive sooner; bigger ships carry more.
- For each day at sea, roll the route's storm and pirate chances:
  - **Storm:** the voyage takes `storm_delay_min`–`storm_delay_max` more days, and the hull takes `storm_damage_min`–`storm_damage_max` damage.
  - **Pirates:** they take `pirate_loss_min`–`pirate_loss_max` of each good aboard, and the hull takes `pirate_damage_min`–`pirate_damage_max` damage.
  - If the hull reaches 0, the ship and its cargo are lost.
- Before you send a ship, the route preview shows the arrival day and the storm and pirate risk.
- Repairs happen in port and cost `repair_cost_per_hull` per hull point.

### Ships
- You can buy ships at any port. You can sell them for `ship_resale_fraction` of their price.
- The bigger the ship, the cheaper its capacity per crate. The costs are slower voyages and the way large cargoes push prices down at the port you sell them in.

### Winning and losing
- **Win:** gold on hand reaches `goal_gold`.
- **Lose:** day `day_limit` ends without reaching the goal, or you have no ships and can't afford one.

## The numbers

See the CSVs for exact values. In summary:

- **Goods** (base price): Sugar 45, Cotton 60, Tobacco 90, Rum 105, Iron 120, Coffee 135.
- **Ships:**

  | Ship | Price | Crates | Speed | Hull | Wage/day |
  |---|---|---|---|---|---|
  | Sloop | 1,000 | 15 | 1.50 | 40 | 8 |
  | Brig | 1,700 | 30 | 1.20 | 70 | 14 |
  | Fluyt | 2,400 | 50 | 1.05 | 100 | 18 |

- **Ports at a glance** (from `market.csv`):

  | Port | Cheap here | Pays well for |
  |---|---|---|
  | New Orleans | Cotton, Iron | Rum, Coffee |
  | Havana | Tobacco, Sugar | Cotton, Iron |
  | Veracruz | Coffee | Iron, Sugar, Rum |
  | Port-au-Prince | Sugar, Coffee | Iron, Tobacco |
  | Bridgetown | Rum, Sugar | Cotton, Tobacco |

- **Routes:** distances from 4 (New Orleans–Havana, Havana–Port-au-Prince) to 10 (Bridgetown to New Orleans or Veracruz). The Florida Straits and the Gulf are stormy. The Havana–Port-au-Prince passage is the most pirate-heavy.

## Balance targets and current results

The simulator plays 500 sessions with each of these bots:

- **Random:** sails anywhere with random cargo.
- **Fixed route:** picks the best-looking pair of ports on day 1 and puts every ship on it, adding sloops as it can.
- **Greedy, 1 ship:** each time it's in port, picks the most profitable trip per day and never buys another ship.
- **Greedy + Sloops / Brigs / Fluyts:** the same, but it grows a fleet of one ship type while keeping enough gold to fill the ships.

Results from `python3 Design/sim/simulate.py` (seed 1, 500 sessions per bot):

| Bot | Win % | Median win day | Median gold on day 60 or win | Most-used route |
|---|---|---|---|---|
| Random | 0 | – | 168 | 12% of voyages |
| Fixed route (fleet) | 0 | – | 11,443 | 99% |
| Greedy, 1 ship | 0 | – | 16,809 | 29% |
| Greedy + Sloops | 99.8 | 49 | 27,185 | 16% |
| Greedy + Brigs | 98.2 | 53 | 26,339 | 19% |
| Greedy + Fluyts | 84.8 | 55 | 26,401 | 23% |

All eight targets in the script pass, and they also pass with a different random seed:
1. A good player usually wins: the best fleet bot wins at least 85% of sessions.
2. It doesn't win too early: its median win day is between 40 and 55.
3. Growing the fleet matters: the 1-ship greedy bot wins at most 50%.
4. Putting a fleet on one route fails: the fixed-route bot wins at most 20%.
5. Ignoring prices fails: the random bot wins at most 2%.
6. No route dominates: the best fleet bot's most-used route carries at most 35% of voyages.
7. Every ship type is viable: each fleet bot is within 25 points of the best win rate.
8. Careful players rarely lose a ship: at most 0.3 sinkings per session.

The fixed-route result is the reactive market working. A fleet of sloops shuttling Coffee and Iron between New Orleans and Veracruz pushes both prices so far that it earns less than a single well-played ship.

**What the bots don't tell us.** The greedy bot does exact price math every time it's in port, which a person won't do. It's also short-sighted: it never plans two legs ahead. Real players will be worse at the first and better at the second, so the playtests in step 6 decide the final numbers. If first-time players struggle, lower `goal_gold` first; it's the cleanest single knob.

## Tuning

1. Edit a CSV in any spreadsheet app and keep the column names.
2. Run `python3 Design/sim/simulate.py` from the repo root (Python 3, no packages needed). Use `--runs 2000` for steadier numbers and `--seed N` for a different random sequence.
3. Check the PASS/FAIL list at the end.

Changes that move results the most:

| Change | Effect |
|---|---|
| `goal_gold` | Difficulty, without changing how the game plays |
| Values in `market.csv` further from 1.0 | Bigger margins and faster growth |
| `pressure_per_crate_sold` and `pressure_recovery_per_day` | How much room the market has, and so how big a fleet pays off |
| Ship prices | How soon expanding pays for itself |

## For step 2 (building it in Unity)

- Load these numbers from data rather than hard-coding them. Either generate the ScriptableObjects from the CSVs or read the CSVs at startup. The simulator stays the reference for the formulas.
- Keep the game state (market, fleet, clock, gold) in plain C# classes, separate from MonoBehaviours, so the formulas can be checked with edit-mode tests against the simulator.
- Ships sail fixed routes between ports. Voyage time and events come from route data, not from NavMesh pathing or trigger colliders on the map. The ship can still be animated along the route.

## Still to decide

- **Ports and setting.** The five ports and the 18th-century Caribbean setting are real history. The current map texture (`Arts/Prototype/Map.png`) is a screenshot of another game's map and needs replacing with original art before the slice is shown to anyone.
- **Ship names.** Sloop, Brig and Fluyt are generic types. The existing ship assets use the names Jupiter, Neptune and Marikh, which could become the names of individual ships.
- **Hurricane.** It's left out to keep the slice small. It could return as a rare, stronger storm.
