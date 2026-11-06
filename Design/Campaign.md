# Naval: campaign and missions (proposal)

A proposal for how the game grows beyond the vertical slice. Nothing here is built or balanced yet, except Chapter 1, Mission 1 (`Design/VerticalSlice.md`).

## The shape

- **A campaign of chapters.** Each chapter is one region of the world in one period, with its own map, ports, goods and one new mechanic.
- **Each chapter is a short series of missions.** A mission has exactly one objective, shown at the top of the screen ("Reach 50,000 gold by day 90"). You play the missions in order, and finishing one unlocks the next.
- **Each mission starts fresh,** from a designed starting state (some gold, some ships, maybe some routes already open). It doesn't carry over your company from the last mission. Fresh starts keep each mission short, and each one can be balanced on its own with the simulator.
- **After the campaign comes World mode.** All regions are joined by long ocean routes, and you can start anywhere and play freely. It's the natural DLC or post-campaign unlock.

This is a familiar structure for management games (Railway Empire's campaign works much the same way: regional chapters, then free play). The point is that every mission teaches or tests one thing, and the world opens up a chapter at a time.

## Setting and year

**Nothing in the repo sets a year.** The README, code and data have no dates. The ports are real cities and the goods are real colonial trade goods, so it's already based on the real world, just not pinned to a time.

**Decided: inspired by history, not a faithful recreation.** Chapters move forward in time from about 1770 to 1800.
- Real places, goods, ships and seasons give it flavour. Events, companies, people and prices are invented whenever that makes a better game.
- **Money is always "gold"**, in every region and chapter. There are no historical currencies or exchange rates. In code, all money is shown through `MoneyManager.Format()` (for example "1,500 gold").
- Show the date on the in-game calendar. Chapter 1, Mission 1 would start on July 1, 1770. The hurricane season (day 25 to the day-55 peak) then falls in late July to late August, close to the real peak from mid-August to mid-October.
- Historical notes are flavour, not rules. These are worth knowing when writing port descriptions:
  - **Seville** had lost the official Spanish Americas trade to **Cádiz** by 1717. Renaming the port to Cádiz is more accurate; keeping Seville is fine for a loose setting. (The port asset is also misspelled "Sevile".)
  - **Natchez cotton** really boomed only after the cotton gin (1793). In 1770 Natchez was a small British outpost; tobacco fits it better then. Natchez was Spanish from 1779, and New Orleans from 1763 to 1801.
  - **Port-au-Prince** was in the French colony of Saint-Domingue, the world's largest sugar and coffee producer until the revolution of 1791.
  - **Copper sheathing** was new in this period: the Royal Navy first tested it in 1761 and used it widely by about 1780. Merchants followed.
- **One thing to decide on purpose:** the real 18th-century trade in sugar, coffee, cotton, tobacco and rum ran on enslaved labour. Liverpool and Bordeaux were leading slave-trading ports, and the later China trade involved opium. The game doesn't depict any of this, and doesn't need to. But a real-world setting means some players will notice, so choose deliberately how the game treats it. Options range from leaving it out entirely, to a line in an in-game almanac, to a more fictionalized world.

## Mission types

Each mission uses exactly one of these objectives. They all reuse the same game rules; only the goal check and the starting state change.

| Type | Objective | Example | What it tests |
|---|---|---|---|
| **Fortune** | Reach X gold by day D | "Reach 50,000 gold by day 90" (the vertical slice) | Everything at once |
| **Contract** | Deliver N crates of a good to a port by day D | "Deliver 300 crates of Iron to Veracruz by day 60" | Planning a supply line under hurricanes and pirates |
| **Pathfinder** | Connect two ports with your own routes by day D | "Open a route from Havana to Liverpool by day 40" | Spending on routes and ships before you can afford to |
| **Fleet** | Own N ships (or N ocean-going ships) by day D | "Own 4 ocean-going ships by day 50" | Growth and upgrades |
| **Supplier** | Deliver at least K crates of a good to a port every week for W weeks | "Keep Boston supplied with 30 crates of Rum a week for 8 weeks" | Steady logistics, and the reactive market working against you |
| **Survival** | End day D with at least N ships and gold at or above zero | "Get through hurricane season with your fleet intact" (or a war year with privateers) | Risk management, hull and guns upgrades |
| **Race** | Reach a target before a rival company | Later, once rival companies exist | Competition. Needs computer-controlled rival companies, a large feature on its own. |

Contract, Pathfinder, Fleet and Survival need almost no new systems: a new goal check, a starting state and a balance pass. That makes them the cheapest content after the slice.

## Campaign outline

Each chapter adds one new mechanic, and its seasonal hazard plays the same role hurricanes do in Chapter 1.

### Chapter 1: The Caribbean and the Atlantic (about 1770)
The slice's map (12 ports). New mechanics: buying routes, the reactive market, hurricane season.
1. **Fortune:** 50,000 gold in 90 days (the vertical slice).
2. **Contract:** supply Iron to Veracruz before hurricane season peaks.
3. **Pathfinder:** start with two sloops and open a route to Liverpool. You'll need an ocean-going ship.
4. **Survival:** a war year. Privateers make every Caribbean route dangerous. Keep your fleet afloat.

### Chapter 2: Around the Cape (Indian Ocean, about 1776)
Cape Town, Port Louis (Mauritius), Bombay, Colombo, Madras, Calcutta. Goods: pepper, cinnamon, calico, indigo, saltpetre, tea (re-export).
- **New mechanic: the monsoon.** Winds flip partway through the mission (southwest from about June to September, northeast from about November to March). Sailing with the wind is fast, and sailing against it is slow. It plays the role of hurricane season: plans that worked early stop working.
- **New ship:** the East Indiaman, big, armed and expensive.

### Chapter 3: The Spice Islands (Southeast Asia, about 1784)
Batavia, Bantam, Malacca, Penang (founded 1786), Manila, Ambon, Banda. Goods: nutmeg, cloves, mace, pepper, tin, rice, sugar.
- **New mechanic: trade licences.** The richest spice ports are closed until you buy a licence, which reflects real company monopolies. Pirates in the Strait of Malacca.

### Chapter 4: The China trade (East Asia, about 1792)
Canton, Macau, Manila, Nagasaki, Batavia. Goods: tea, porcelain, silk, lacquerware, silver.
- **New mechanic: silver and quotas.** Canton sells tea only for silver, so you must bring silver from Manila. (Historically, from 1757 to 1842 Western ships could trade in China only at Canton.) Nagasaki takes a limited number of ships per season. (Historically only Dutch and Chinese ships could trade there, at Dejima.)
- **New ship:** a Chinese junk, bought locally.

### World mode (DLC or post-campaign)
All four regions joined by long routes: round the Cape of Good Hope, and the Manila–Acapulco route across the Pacific (the real Manila galleons ran from 1565 to 1815). You pick a starting region and a goal, and contracts of every type appear as you play.

## How to build it

1. **Finish the slice** (Chapter 1, Mission 1). Keep the mission's goal separate from the game rules from the start, so new mission types are just new goal checks.
2. **Add the other mission types on the same map** (Chapter 1, Missions 2–4). This is the cheapest content: no new art or regions, and the simulator can check each one with a new goal check.
3. **Then add regions one at a time.** Each region is expensive: map art, port and goods assets, a new mechanic and a balance pass. Give each chapter its own data folder (`Design/data/chapter2/...`) with the same CSV files, plus a `missions.csv` listing each mission's type, target, day limit and starting state.
4. **World mode last.** It joins the chapters' data and needs long-route balancing of its own.
