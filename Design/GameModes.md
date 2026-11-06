# Naval: game modes

There are three ways to play. **Tutorial** teaches the game. **Beginner** and **Veteran** are the two difficulties for every mission. You choose the difficulty when you start a mission and can't change it mid-mission.

| | Tutorial | Beginner | Veteran |
|---|---|---|---|
| Who it's for | First-time players | People learning the game, or who want a relaxed session | Players who want the full economic puzzle |
| Market | Fixed | **Fixed:** your trading never moves prices | **Reactive:** every crate you buy or sell moves that port's price, which recovers over a few days |
| Market news | One scripted shortage | Yes | Yes |
| Hazards | One scripted storm; pirates and hurricanes are only explained | See the Beginner settings below | Full |
| Can you lose? | No | Yes | Yes |
| What wins | Following the steps | Finding good routes and scaling up | Spreading across goods, ports and oceans as your fleet grows |

## Beginner and Veteran

**Veteran** is the game as designed and balanced in `Design/VerticalSlice.md`. The reactive market is its heart: a big cargo sells for less than a small one, and a fleet that crowds one route ruins its own prices.

**Beginner** removes that one layer of maths. The price you see is the price you get for every crate, so the player can learn routes, ships, upgrades and hazards first. The cost is that a fixed market has an obvious best answer: find the best route and put more ships on it. That's acceptable at this difficulty, and market news and hurricane season still change the best route over the mission.

Beginner's settings are rows in `Design/data/modes.csv`, which override `rules.csv`. The simulator checks Beginner against its own targets (`python3 Design/sim/simulate.py --mode beginner`):

**Current Beginner settings** (everything else is the same as Veteran: 50,000 gold goal, 90 days, 2,500 starting gold, the same hazards):
- `pressure_per_crate_bought` = 0 and `pressure_per_crate_sold` = 0: the fixed market.
- `spread` = 0.35 (Veteran: 0.10): a wider gap between the buy and sell price at a port. In a fixed market a ship can repeat its best trade forever, so profits compound very fast: a good player would win around day 42. The wider gap puts the median win near day 59.

**Beginner targets** (seed 1, 200 sessions per bot; seed 2 matches within about a point):

| Target | Result |
|---|---|
| B1: a good player (best fleet bot) wins at least 95% | 99%: pass |
| B2: their median win day is 50–75 | Day 59: pass |
| B3: a simple plan works (one route, keep adding ships) wins at least 50% | 100%, median day 51: pass |
| B4: ignoring prices still fails (random play) at most 5% | 0%: pass |
| B5: growing the fleet still matters (one ship wins at most 80%) | 0%: pass |
| B6: buying routes matters (free routes only earns at most 70% of an expanding fleet) | About 125%: **fail** |
| B7: upgrades pay off (at least +10%) | Pass, by a wide margin |
| B8: hull upgrades keep losses to at most 1 per session | Pass |

**Open problem: in a fixed market, buying routes doesn't pay.** With no price impact, every ship can repeat the best trade it already has. The free trades around Havana earn about 25 gold per crate-day, and the best trades anywhere about 36. Expansion can gain at most about 1.4×, and the cost of the routes eats that. The fix being considered is **port stock**: prices stay fixed, but each port holds a limited, visible number of crates of each good and restocks over a few days. Stock is easy to understand, it naturally caps how much one route can earn (so new routes pay off again), and it would replace both the wider gap and the bots' 30-ship cap.

**Simulator notes:**
- In Beginner the bots stop buying ships at 30, because nothing else limits a fleet in a fixed market.
- Gold figures after the win are not meaningful. In the game, the mission ends when you reach the goal.

**UI differences:**
- **Beginner:** show one price per crate.
- **Veteran:** show the total for the amount you're trading and how far the price will drop ("Selling 30 crates: 2,940 gold, price falls 24%").
- **Veteran's first time:** a short pop-up explains the reactive market with a live example the first time a player starts a Veteran mission.

## Tutorial

### Principles
- **One objective at a time,** the same as missions. The current objective is always on screen, and finishing it reveals the next one.
- **Learn by doing.** Every step is an action in the real UI, with the relevant button highlighted. No walls of text.
- **One new idea per step,** in the order a player needs them.
- **You can't lose.** There's no day limit and nothing random, and the one storm is scripted. The tutorial can be skipped at any time.
- **About 10 minutes.** Check this in playtests.

### Setting
It's July 1770. Your **factor** (a merchant's agent in a port, a real job title of the period) has written to you: you've inherited a sloop in Havana and 1,500 gold. The factor is the voice of the tutorial, a short line or two of advice for each step. Their name and portrait are still to be decided.

The tutorial uses four ports from Chapter 1: Havana, Port-au-Prince, Bridgetown and New Orleans. It uses Beginner rules, with pirates and hurricanes switched off.

### Steps

| # | Objective shown | What the player does | What it teaches |
|---|---|---|---|
| 1 | Open your first sea route | Choose Havana–Port-au-Prince on the map (free) | Ships only sail on routes you own; your first two routes are free |
| 2 | Buy Tobacco in Havana | Open Havana's market and fill the hold | The port panel. Green = cheap here. Cargo space. |
| 3 | Send your sloop to Port-au-Prince | Pick the destination and check the route preview, then speed up time | Arrival time, route risks, time controls |
| 4 | Sell your Tobacco | Sell at Port-au-Prince | Gold = profit. Gold marker = pays well here. |
| 5 | Bring Coffee back to Havana and sell it | Buy Coffee, sail back, sell | Round trips: carry something both ways |
| 6 | Open your second free route | Choose New Orleans *or* Bridgetown (both highlighted) | Your first real choice, and that the map grows |
| 7 | News: "Shortage of Tobacco in Bridgetown" | Deliver Tobacco there while the price is high (if the player picked New Orleans, the news is about Coffee in New Orleans instead) | Market news, and chasing opportunities |
| 8 | Buy a second ship in Havana | Use Havana's shipyard | Shipyards, and that every ship costs wages |
| 9 | Fit Extra canvas to a ship | Buy the Sails level 1 upgrade | Upgrades. Only at shipyards. |
| 10 | A storm damages a ship on its way. Repair it in port. | Pay for repairs on arrival | Hull damage and repairs. The factor mentions pirates, hurricane season and that sloops can't cross the Atlantic. |
| 11 | Buy a new route | Open a third route with gold | Routes cost gold after the free ones, and must join your network |
| 12 | Reach 6,000 gold | Trade freely | Putting it together |

At the end, the factor suggests Chapter 1, Mission 1 on Beginner and explains in one line what Veteran adds.

The tutorial's numbers (6,000 gold, the scripted storm's damage, which trades are on offer) need a quick check once it's built. They aren't simulated.

## Building it

- **One objective system for everything.** A mission is a list of objectives with a completion check each: "gold ≥ X", "own route A–B", "deliver N of good G to port P", "own N ships", "repair a ship" and so on. Missions have one objective; the tutorial has twelve in sequence. Building the system once serves the campaign's mission types too (`Design/Campaign.md`).
- **Each tutorial step is data:** objective text, the factor's line, the UI element to highlight, the completion check, and an optional scripted event (the news in step 7, the storm in step 10).
- **Difficulty is data too:** Beginner is the rule overrides in `modes.csv`, and the game applies them at mission start, as the simulator does.
