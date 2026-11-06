# Naval: economy, version 2 (spec)

This spec combines the decisions made after the first vertical-slice balance:
- 14 historically flavoured goods
- missions that span years
- ports with 5 levels that grow, and buildings the player can add to them
- port stock limits in Veteran only (Beginner has fixed prices and no stock limits)
- bulky and perishable cargo in Veteran only

It's the brief for the next simulator pass. When it's built and balanced, `VerticalSlice.md` will be updated to match. Background: `Research/TradeGoods-1770s.md` and `Research/Mechanics-References.md`.

## 1. Time

- **Chapter 1, Mission 1 runs 2 years:** July 1, 1770 to June 30, 1772 (730 days). The goal is still one objective: reach a gold target, to be tuned.
- **Pacing:** about 1.5 real seconds per in-game day at 1× (about 18 minutes for the mission), plus pause, 2× and 4×.
- **Seasons repeat every year, by calendar date:**
  - Hurricanes: from June 1 the chance rises, reaching full strength on August 15. It stays at full strength until October 15, then falls to zero by November 30.
  - The rest of the year is calm.
  - So the slice has two hurricane seasons: one starting about 6 weeks in, and one in its second summer.
- Wages and harbour dues work as now, per day and per arrival.

## 2. Goods (14)

| Good | Base price | Hold space per crate | Perishable (Veteran only) |
|---|---|---|---|
| Lumber & Naval Stores | 30 | 2 (bulky) | |
| Salt Cod | 35 | 1 | |
| Flour | 40 | 2 (bulky) | Yes |
| Logwood | 45 | 2 (bulky) | |
| Sugar | 60 | 2 (bulky) | |
| Tobacco | 85 | 1 | |
| Rum | 90 | 1 | |
| Wine | 100 | 1 | |
| Hardware | 110 | 1 | |
| Furs & Deerskins | 120 | 1 | |
| Cloth | 130 | 1 | |
| Coffee | 140 | 1 | |
| Indigo | 180 | 0.5 (compact) | |
| Cochineal | 300 | 0.5 (compact) | |

- **Hold space (Veteran only):** a ship's capacity is measured in hold space. A bulky crate takes 2 spaces and a compact crate half a space. In Beginner every crate takes 1. The effect: big ships suit bulk goods, and small, fast ships suit valuable compact goods.
- **Perishable (Veteran only):** after `spoil_after_days` (start at 14) aboard, a perishable cargo loses `spoil_per_day` (start at 5%) of its crates each day. Flour is the only perishable good. It rewards fast ships and short routes.
- **Base prices are starting points for tuning.**
- **Which port sells and wants what:** start from the table in `Research/TradeGoods-1770s.md`, section 3, and adjust for balance. Keep these principles from version 1:
  - each port has 2–3 cheap goods and 2–3 wanted goods
  - every good has at least one cheap source and at least two buyers
  - Europe pays well for colonial goods and the colonies pay well for European goods
  - every region has some short local trades for the early game
  - blank means not traded

## 3. Port levels and capacity

Every port has a **level from 1 to 5**.
- The big hubs (Havana, New York, Liverpool, Bordeaux) start at level 3. Other ports start at level 1.
- Level multipliers grow steadily to about ×2.5–3 at level 5.

**Beginner (fixed prices, no stock limits).**
- Ports buy and sell any number of crates at fixed prices.
- Beginner has a wider gap between a port's buy and sell price (`spread` around 0.35).
- **Each port level narrows that gap at that port,** down to about Veteran's 0.10 at level 5. Growing a port makes its trade more profitable.

**Veteran (reactive prices plus stock).**
- Prices move per crate as before.
- Each port also has **stock and demand with a daily restock**:
  - For each good a port sells, it holds a stock of crates and restocks a set amount each day.
  - For each good it wants, it buys up to a set number of crates, and that number refills each day.
  - Starting values to tune: cheap goods stock 80 and restock 8 a day; wanted goods buy 80 and refill 8 a day; other traded goods 40 and 4.
- Port level multiplies stock, demand and restock, and divides the price movement per crate.

**Port growth (both modes):**
- A port grows a level when its wanted goods have been well supplied over a season. Start with: at least 60% of its demand delivered over the last 90 days.
- Each level needs more supply than the last.
- **Veteran only:** a port drops a level if it's badly undersupplied for a whole season. In Beginner, levels never drop.

## 4. Port buildings (both modes)

- The player can build in any port they've reached.
- Building slots: **1 at levels 1–2, 2 at levels 3–4, 3 at level 5.**
- Each building has a cost and a monthly **upkeep**, so empty or unused buildings lose money. Starting values to tune:

| Building | Cost | Upkeep/month | Effect in Beginner | Effect in Veteran |
|---|---|---|---|---|
| Warehouse | 3,000 | 60 | To be settled in balancing (a fixed-price effect, or Veteran-only) | +50% stock and demand; price movement per crate ÷1.5 more |
| Market Hall | 2,500 | 50 | Narrows this port's buy/sell gap one more step | Restock and price recovery twice as fast |
| Customs House | 2,000 | 40 | Harbour dues here halved, and half the time in port | The same |

This is the "upgrade the port's limit" mechanic. It and port growth give the multi-year mission a long-term shape: supply a colony, it grows, you build there, and it becomes a hub.

## 5. Unchanged from version 1

These stay as they are:
- the 12 ports and 17 routes (prices, the 2 free routes, ocean crossings)
- ship types and upgrades (sails, hull, guns, hold)
- storms and pirates
- market news, wages, dues and repairs

The numbers may need rescaling for a 2-year mission.

## 6. Simulator and targets

**Bots must play the new rules sensibly:**
- respect stock, demand and hold space when planning cargo
- build buildings when they'd pay back within a set time
- only buy ships while extra ships still earn more than they cost
- keep a safety cap on fleet size so runs finish

**Keep each mode's run to a few minutes:** fewer bots (drop the single-track upgrade bots, or run them only with a flag) and about 100 sessions by default.

**Targets: carry over both modes' lists, adapted to 2 years, plus:**
- Veteran's win-timing target becomes a share of the mission: a good player's median win lands between 65% and 90% of the mission length.
- **Port development matters in Beginner:** a bot that builds and grows ports earns at least 5% more than one that doesn't. With fixed prices and no stock, nothing stops one route absorbing every ship, so Beginner's "buying routes matters" target is information only.
- **Port buildings pay off:** a bot that builds earns at least 5% more than one that doesn't.
- **Ports grow:** a good player's network has at least 4 ports at level 2 or above by the end. Informational to start with.
- **Seasons matter every year:** time on hurricane routes drops by at least 20% in each hurricane season compared with the calm months.
- **Veteran's cargo rules matter:** Flour is carried less by slow ships, and big ships carry more bulk goods than small ships. Informational.

## 7. Status after the first balance pass

Built in the simulator and balanced: all required targets pass on seeds 1 and 2 (Veteran 19 of 19, Beginner 9 required plus 2 information-only). Run `python3 Design/sim/simulate.py --mode veteran` (about 4 minutes) or `--mode beginner` (about 40 seconds). Add `--tracks` to Veteran to test each upgrade track on its own.

**Numbers that ended up far from the starting values above:**
- Goal: 900,000 gold in 730 days, the same in both modes. A good player (a Brig fleet in Veteran, a Fluyt fleet in Beginner) wins around day 530–570.
- Veteran stock at level 1: cheap goods 16 crates, restocking 2 a day (the spec said 80 and 8; at that size fleets never ran short).
- Port levels multiply capacity ×1.0 / 1.4 / 1.8 / 2.3 / 2.8. Supply needed to grow: 40% / 50% / 60% / 70% by level.
- Beginner buy/sell gap: 0.60 at level 1, narrowing to 0.10 at level 5.
- Buildings cost 10 times the spec's values (20,000–30,000 gold, 400–600 a month). Warehouse is Veteran-only.
- Flour starts spoiling after 3 days aboard, losing 12% a day (at 14 days nothing ever spoiled).
- Hurricanes are much harsher: 10–16 days' delay, 25–45 hull damage, 25–45% of cargo lost. Milder storms didn't change routes enough.

**Open issues for the next pass:**
1. **Beginner has no fleet limit in the game.** The simulator stops Beginner bots at 6 ships, and that cap is what sets Beginner's pace. A real player could keep buying ships and win far sooner. The game needs a rule that limits fleet size, or Beginner's pace is untested.
2. **Upgrades are too strong in Veteran.** All upgrades together add about 160%, so an upgrading player wins at about 46% of the mission. The targets measure a player who doesn't upgrade.
3. **Hurricanes may now be too punishing:** about 3 ships lost per session for a Brig fleet, and about 10 for a Sloop fleet.
4. **Beginner plays as a one-route game.** With fixed prices and no stock, every ship runs the single best pair (Havana–New York), so buying routes and seasons don't matter there.
5. Smaller: free-route openings barely matter (bots buy about 4 routes on day 1); hub ports often decline early in Veteran; most building value comes from Market Halls.
