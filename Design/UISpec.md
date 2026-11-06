# Naval: UI/UX review and design spec

**Status: proposal, waiting for the owner's approval.** Nothing here is built yet. Read it with `SliceScope.md` (what the slice contains) and `Status.md` (what exists now).

**What this covers:**

1. A review of every screen as it is today.
2. A proposed design for every screen, with layout sketches at 1920×1080.
3. A small design system: type, colours, spacing, buttons, panels, icons, accessibility.
4. How to build it in Unity so a designer can open, read and change it.
5. A phased plan, the placeholder art it needs, and open questions.

**Labels:** anything marked *(optional)* is nice to have and can be cut without hurting the slice.

---

## 0. Summary

**The problem in one sentence:** every screen shows the right *information*, but as sentences of plain text on grey buttons, so the player has to read and do arithmetic to make every decision, and the screen gives no sense of place, priority or progress.

**The ten biggest problems:**

1. **No visual hierarchy.** Date, days left, gold and goal are four equal lines of white Arial. Nothing says "this is what matters".
2. **Progress is invisible.** "8,500 gold (goal 500,000 gold)" asks the player to work out that they are at 1.7%.
3. **Hurricane season, the game's central tension, isn't on screen at all** until the player opens a send preview in season.
4. **Destructive buttons sit beside the most-clicked buttons.** Restart and Tutorial are directly under Pause/1×/2×/4×, guarded only by a label that changes to "Sure?".
5. **Time stops silently.** Opening any panel holds the clock, but the speed buttons still show 1× selected. The player can't tell "paused" from "running".
6. **The map has no geography.** A blue rectangle inside a blue rectangle. Coastal versus ocean routes, the core rule for sloops, can't be seen because there is no coast and no ocean.
7. **The route colours mean things nobody is told.** Cream vs blue, faint vs half-bright vs bright, thin vs thick, with no legend.
8. **Decisions need comparison, but the UI shows one thing at a time.** The send panel lists destinations as bare names; to compare arrival times or risks the player must click each one and remember the numbers.
9. **The port market is a wall of 56 identical buttons** (14 goods × Buy 1 / Buy 10 / Sell 1 / Sell all), with no hint of what is cheap here or what sells well elsewhere.
10. **Feedback is text-only and fleeting.** Notifications are coloured sentences that fade after 10 seconds, with no icon, no log and no way to jump to the ship.

**The direction:** a period chart table. Parchment panels with ink text over a painted sea-and-land chart; gold as the accent colour; numbers shown as bars and meters, with the exact figure beside them; one consistent top bar; panels that dock so the map stays visible when the map matters (route buying, sending); and every row built from a prefab a designer can open and edit.

---

## 1. Review of the current screens

The screenshot reviewed shows the opening at 1920×1080. Everything is drawn with Unity's default `Image` (grey) and legacy `Text` in the built-in Arial. All rows are made in code by `SliceUi.MakeButton` / `MakeLabel` / `MakeRow`, so none of them exist in the scene for a designer to look at.

### 1.1 Across every screen

| What you see | Why it's a problem |
|---|---|
| Default grey Unity buttons with dark grey text | Reads as a debug tool, not a game. Grey on grey also gives no idea which button is the main action. |
| Built-in Arial everywhere, white on blue | No period feel; Arial at 20 to 26 px on a saturated blue is tiring to read in long tables. |
| Information as sentences ("Each: 34 crates of hold, speed 1.3, hull 70, wages 18 gold/day, ocean-going") | Sentences are easy to generate and parse, but people scan; they look for a number in a known place, an icon or a bar. This is the main reason the UI "feels made for an AI". |
| Unitless numbers ("speed 1.15", "Distance 12") | The player can't tell if 1.15 is good. Speed only matters as days on a route; distance only matters as days. |
| No hover state, no tooltips, no icons | Nothing teaches by itself; every rule has to be written out in a sentence. |
| Results shown in a small coloured message line at the bottom of each panel | The feedback appears far from the button that caused it, often off the player's focus. |
| Colours hard-coded in `SliceUi` and copied by hand into `Slice.unity` | A designer can't change the look in one place; the two copies drift. |
| Rows built in code | A designer can't see, select or restyle a market row, a fleet card or a notification in the editor. |
| No sound | Expected; it's phase 5. Noted here so the spec reserves the hooks. |

### 1.2 HUD (top left)

**What you see:** four lines of text: `1 Jul 1770`, `730 days left`, `8,500 gold (goal 500,000 gold)`, and a status line used only for data errors.

**Problems:**
- All four are the same size and colour, so the eye has nowhere to land.
- The goal is a raw number beside the gold. The player must divide to know where they stand, and at the start the gap (8,500 vs 500,000) looks hopeless instead of motivating.
- Days left is a big number with no context (is 300 a lot?).
- Hurricane season, which changes prices and risk across the whole map, is not shown at all.
- Gold changes (a sale, wages, dues, a repair) happen with no feedback: the number just changes.

### 1.3 Speed, Restart and Tutorial buttons (top right)

**What you see:** four grey buttons, Pause, 1×, 2×, 4×, with the selected one tinted yellow. Directly under 4× is Restart; under 2×, Tutorial.

**Problems:**
- Restart and Tutorial both throw the current game away, and they sit directly under buttons the player clicks constantly. The "click again within 3 seconds, label says Sure?" guard is easy to miss: a player who misses 4× and clicks again in frustration restarts the game.
- The speed buttons are four separate wide buttons, not one control. They take a lot of space for little information.
- When a panel is open, the clock is held, but 1× still looks selected. The player can't see that time has stopped, or why.
- There is no Esc menu and no keyboard shortcut (Space to pause is a genre standard).

### 1.4 Map

**What you see:** a dark blue background, a lighter blue rectangle (the "sea panel"), cream dots for ports with white names above, bigger gold dots for shipyards, straight lines between ports, and white triangles for ships.

**Problems:**
- **No land.** The game is about coasts, oceans, the Gulf and the Caribbean, but none of that can be seen. "Coastal route" and "ocean route" are words, not places. The rectangle-in-rectangle reads as an unfinished frame.
- **Ports all look the same** apart from shipyards. A port the player can reach, one they could reach by buying a route, and one far off the network look identical.
- **The shipyard is just a bigger, gold dot.** Nothing says "shipyard" or explains why it is gold.
- **Port names are white Arial TextMesh** with no backing, so on the light blue they have weak contrast. New York and Boston sit close enough that their names may touch.
- **Route styling is invisible logic.** Owned routes are thick and bright; routes that connect to the network are half-bright; others faint; ocean routes are blue, coastal routes cream. There is no legend, and faint lines on blue are close to invisible.
- **Straight lines** will cross land once land art exists (Havana to New York cuts across Florida).
- **Ships** are small triangles with no selected state, no hint of which ship is which, and several ships in one port pile up beside it.
- **The free-routes banner** says "Click a route on the map that touches your network". "Touches your network" is jargon, and only the brightness of the line shows which routes qualify.

### 1.5 Fleet choice (the opening)

*The empty grey box in the screenshot is a runtime bug being fixed separately; this reviews the intended screen.*

**What the code builds:** a title, "Start gold: 8,500 gold   Start port: Havana", and three 1000×130 grey buttons, each with three lines: "2 Brigs / Each: 34 crates of hold, speed 1.3, hull 70, wages 18 gold/day, ocean-going / Total cost 6,000 gold".

**Problems:**
- This is the most important decision in the game, and it's presented as three paragraphs on three identical buttons. The whole button is the "choose" action, so a curious click commits the player.
- The real trade-off is hidden: "coastal only" means *sloops can never reach Europe*, the best markets. That consequence isn't said.
- Stats are per ship ("Each:") while the choice is per fleet. The player has to multiply: 3 sloops × 16 crates = 48 crates, against 90 for the Merchantman.
- No picture, no sense of what these ships are.
- All three cost the same 6,000 gold, so "Total cost" on each card repeats information; what the player needs is "you'll have 2,500 gold left for cargo".

### 1.6 Route panel

**What you see:** a title "Havana – New Orleans", then a text block: "Coastal route / Distance 4 / Chance per day at sea: storm 4% pirates 4% / hurricane 4% at the peak of the season (Aug to Oct) / Cost: 500 gold", an "Open route" button and Close.

**Problems:**
- "Open route" is ambiguous (open the panel? the route?). It means *buy*.
- Per-day chances are honest but meaningless to a player: 4% a day sounds small, yet over a 10-day trip it's a third.
- Distance is unitless.
- It doesn't say what buying the route *gives*: which new ports it brings into reach.
- When a route can't be bought (not connected), the player only learns it after clicking Open.

### 1.7 Port panel

**What you see:** a large panel. Left column: "Gold: …", a ship stepper `< Ship 1 of 3 here >`, a text block (name, hull, hold), Repair and Send, and at shipyards a list of buttons ("Buy Brig – 3,000 gold", "Sails: … – … gold"). Right column: the market, a header row (Good, Buy, Sell, Aboard) in yellow, and one row per good with four buttons. A message line at the bottom; Close.

**Problems:**
- **56 buttons** in the market at a 14-good port, all identical grey, at 100×40.
- **No trading guidance.** Prices are fixed per port, so the whole game is "buy where cheap, sell where dear", yet nothing marks a cheap good or a good that pays well somewhere else.
- **Hold and hull are text** ("Hold 12/90 crates"), so the player can't see at a glance how full the ship is or how damaged.
- **The stepper hides the fleet**: "Ship 1 of 3 here" doesn't say *which* ships are here until you step through them.
- **Shipyard and market are mixed in one view**, and buying a ship and upgrading the selected ship are styled the same.
- **Upgrade levels aren't visible**: the button shows only the next level's name and price; "complete" appears as plain text.
- **Feedback is far away**: the result line sits at the bottom of the panel, away from the row that was clicked.
- **Gold is shown inside the panel** while the HUD gold is hidden behind it, so there are two places to look.

### 1.8 Send panel

**What you see:** "Send Brig 2 from Havana", a column of destination buttons (names only), a text block with the preview ("Havana → New Orleans / Arrives 12 Jul 1770 (11 days) / Pirate risk: 15% / Hurricane season risk: 7% / Chance of losing this ship: 2% (hull 70/70)"), Set sail (appears only once a destination is chosen) and Back.

**Problems:**
- **Destinations can't be compared.** A bare list of names; days, risk and the money to be made are only shown after clicking each.
- **The route isn't shown on the map.** Stops are a text chain ("A → B → C"), and the panel covers much of the map anyway.
- **Risk is three percentages in a sentence**, with nothing saying whether 15% is high.
- **No link from risk to action**: the loss chance depends on hull, but there's no Repair button here.
- **Set sail pops in and out** instead of being visibly disabled with a reason.
- **What's aboard isn't shown**, so the player can send an empty ship without noticing.

### 1.9 Notifications

**What you see:** up to six white or orange or red lines at the bottom left, newest at the bottom, each fading after 10 seconds.

**Problems:**
- Colour is the only difference between "arrived", "pirates" and "sank". That fails for colour-blind players and is weak for everyone.
- Ten seconds at 4× is about 27 game days; a sinking can scroll past while the player is looking elsewhere.
- No log to read what was missed.
- No way to click a message to find the ship or the port.
- Arrivals (good news, and a prompt to go and sell) look the same as routine text.

### 1.10 Fleet list

**What you see:** bottom right, one line per ship: grey buttons for ships in port ("Brig 2 – Havana – hull 70/70"), plain white text for ships at sea ("Brig 3 – at sea to Boston").

**Problems:**
- Two different looks (button vs label) for the same kind of item.
- Missing what the player wants at a glance: cargo, arrival date, and whether the ship is idle (an idle ship is lost money).
- No link to the map: hovering or clicking a ship doesn't highlight its marker.

### 1.11 Tutorial

**What you see:** a box across the top centre, stretched between the gold text and Pause, with one sentence of objective and a Skip button. A pulsing yellow outline marks the target.

**Strengths worth keeping:** one objective at a time, a pulsing highlight on the real widget, Skip at any time, and it never blocks clicks.

**Problems:**
- The box is squeezed into the gap between HUD elements and the text shrinks to fit (best fit), so on narrow screens it can get small.
- No step counter ("3 of 8"), so the player doesn't know how long it lasts.
- It looks like the rest of the UI (no "advisor" identity), so it's easy to read as part of the HUD.
- The outline is a plain rectangle; on the map it's a fixed-size square around a port dot.

### 1.12 End screen

**What you see:** a full-screen panel with "Goal reached" or "Mission failed", a text block (gold vs goal, date, and the reason for a loss), and Restart.

**Problems:**
- Win and loss look identical apart from the title text.
- No summary of the run (voyages, best trade, ships lost), which is the reward moment of a strategy game.
- Restart is the only action; there's no "Main menu" or "Quit".

---

## 2. Layout of the screen

All sizes are for the Canvas reference resolution, **1920×1080** (Canvas Scaler: Scale With Screen Size, match 0.5, as now).

**Zones:**

```
 0                                                                                               1920
 ┌──────────────────────────────────────────────────────────────────────────────────────────────┐ 0
 │ TOP BAR (72 px): date + season   ·   goal progress   ·   days left   ·   speed   ·   menu       │
 ├──────────────────────────────────────────────────────────────────────────────────────────────┤ 72
 │                                                                         │                      │
 │                     MAP (whole screen behind the UI)                    │  RIGHT DRAWER        │
 │                                                                         │  560 px, only while  │
 │   objective card (tutorial / free routes) top centre, under the bar     │  the route or send   │
 │                                                                         │  panel is open; the  │
 │                                                                         │  map camera shrinks  │
 │                                                                         │  to the space left   │
 │                                                                         │                      │
 │ ┌──────────┐            ┌───────────────────┐               ┌──────────┐│                      │
 │ │ LEGEND   │            │ TOASTS (max 3)    │               │ FLEET    ││                      │
 │ │ + LOG    │            │ bottom centre     │               │ LIST     ││                      │
 │ └──────────┘            └───────────────────┘               └──────────┘│                      │
 └──────────────────────────────────────────────────────────────────────────────────────────────┘ 1080
```

**Why these zones:** with the current `ports.csv` coordinates, the bottom-left (open Gulf south of Veracruz), the bottom centre (Caribbean south of Bridgetown) and the bottom right (open Atlantic) are empty sea, so the legend, toasts and fleet list never cover a port. The top bar holds everything the player checks often; the bottom holds things the player glances at.

**Panels:**
- **Modal, centred:** fleet choice, port panel, pause menu, confirm dialogs, end screen. The map behind is dimmed (Ink at 55%) and doesn't take clicks.
- **Docked right drawer:** route panel and send panel. These are about the map, so the map stays visible and the map camera fits itself into the remaining 1360 px (a `Camera.rect` change in `SliceMap.FitCamera`; see §5.6).
- **Esc** closes the top panel; with nothing open it opens the pause menu.

---

## 3. Screen designs

Sketches use ASCII. `[ Button ]` is a button; `[*Button*]` is the primary action; `(icon)` is a placeholder icon from §4.6; `▓▓▓░░` is a bar or meter.

### 3.1 Top bar (HUD)

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ (cal) 1 Jul 1770    (hurricane) Calm      │ (coin) 8,500  ▕▓░░░░░░░░░░░░░░░░░░░▏ 500,000  │ (glass) 730 days │  [❚❚|▶|▶▶|▶▶▶]     (≡) │
│       Thursday      season from 1 Jun     │        gold      goal: 1.7%                    │  left            │                   Menu │
└──────────────────────────────────────────────────────────────────────────────────────────────────────┘
  ← 24 →  left group ~520 px                   centre group ~640 px                            right group ~560 px
```

- **Bar:** full width, 72 px tall, ParchmentDark with a 2 px Ink line along the bottom. Groups separated by thin vertical rules.
- **Date:** Heading size. *(optional)* weekday in Caption under it.
- **Hurricane-season indicator:** an icon plus a word and a five-pip meter, driven by `GameState.Season` (0 to 1, already in Core):

  | Season value | Label | Pips | Colour |
  |---|---|---|---|
  | 0 (Dec to May) | Calm, "season from 1 Jun" | ○○○○○ | InkSoft |
  | 0 to 0.99, rising (Jun to mid-Aug) | Rising | ●●○○○ … | Warning |
  | 1 (mid-Aug to mid-Oct) | Peak | ●●●●● | Bad |
  | below 1, falling (mid-Oct to Nov) | Easing | ●●●○○ … | Warning |

  Hover tooltip: "Hurricane season. Ports outside the Gulf and Caribbean pay +25% for Gulf and Caribbean goods; those ports pay +20% for imports. Storms at sea are worse." (figures from `rules.csv`). This is the only place that tells the player *why* braving the season pays.
- **Goal progress:** gold on hand as a large number, a 360×14 bar of gold against `goal_gold`, the goal at the right end, and the percentage as a caption. If gold is negative (debt), the number turns Bad and the bar shows empty with a small "in debt" caption.
- **Gold change feedback:** each change floats a short "+1,240" (Good) or "−40 dues" (Bad) up from the number for one second. *(optional, but cheap and very satisfying)*
- **Days left:** number in Heading, "days left" in Caption. Under 90 days it turns Warning; under 30, Bad.
- **Speed control:** see 3.2.
- **Menu button:** a 44×44 icon button at the far right, at least 48 px from the speed control and behind a divider. It opens the pause menu (3.3).

### 3.2 Speed control

```
 normal                       held by a panel                   paused by the player
 ┌────┬────┬─────┬──────┐     ┌────┬────┬─────┬──────┐          ┌────┬────┬─────┬──────┐
 │ ❚❚ │ ▶  │ ▶▶  │ ▶▶▶  │     │ ❚❚ │ ▶  │ ▶▶  │ ▶▶▶  │          │▓❚❚▓│ ▶  │ ▶▶  │ ▶▶▶  │
 └────┴▓▓▓▓┴─────┴──────┘     └────┴────┴─────┴──────┘          └────┴────┴─────┴──────┘
   1× selected                 "Paused while you trade"            "Paused" (Space)
```

- One grouped control, 4 segments of 52×40, shared border, no gaps. The selected segment is filled Ink with a Parchment icon.
- Icons, not "1×/2×/4×" text; the tooltip gives the name and key: Pause (Space), Normal (1), Fast (2), Fastest (3).
- **When the clock is held by a panel**, all segments dim and a caption under the control reads "Paused while you trade". This makes the existing hold rule visible instead of mysterious.
- Keyboard: Space toggles pause and the last speed; 1, 2, 3 pick a speed.

### 3.3 Pause menu and confirm dialog

```
            ┌──────────────────────────────────┐
            │            Paused                │   Title
            │  ──────────────────────────────  │
            │      [*      Resume       *]     │   primary, Esc also resumes
            │      [  Replay tutorial     ]    │   secondary
            │      [  Restart voyage      ]    │   secondary
            │  ──────────────────────────────  │
            │      [  Quit to desktop     ]    │   danger (optional: builds only)
            └──────────────────────────────────┘
                        480 × ~460, centred

            ┌──────────────────────────────────────────┐
            │  Restart the voyage?                     │
            │  Your fleet, gold and routes will be     │
            │  lost. The game starts again on          │
            │  1 Jul 1770.                             │
            │                                          │
            │              [ Cancel ]  [!Restart!]     │   Cancel is the default (Enter/Esc)
            └──────────────────────────────────────────┘
```

- Opens from the Menu button or Esc. Pauses the game while open.
- Restart and Replay tutorial each open the confirm dialog, which replaces the current "Sure?" label trick. One reusable `ConfirmDialog` (title, body, confirm label, danger or not).
- Quit is only shown in a player build (`Application.isEditor` false) *(optional)*.
- *(optional)* Settings: music and sound volume once phase 5 adds sound.

### 3.4 Fleet choice

```
┌────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                     Choose your fleet                                                  │ Title
│                     You have 8,500 gold in Havana. A fleet costs 6,000; the rest buys your first cargo.│ Body, InkSoft
│                                                                                                        │
│  ┌────────────────────────────┐   ┌────────────────────────────┐   ┌────────────────────────────┐      │
│  │ ┌────────────────────────┐ │   │ ┌────────────────────────┐ │   │ ┌────────────────────────┐ │      │
│  │ │                        │ │   │ │                        │ │   │ │                        │ │      │
│  │ │   [ship picture 1:1]   │ │   │ │  [two ships picture]   │ │   │ │ [three ships picture]  │ │      │
│  │ │                        │ │   │ │                        │ │   │ │                        │ │      │
│  │ └────────────────────────┘ │   │ └────────────────────────┘ │   │ └────────────────────────┘ │      │
│  │  1 × Merchantman           │   │  2 × Brig                  │   │  3 × Sloop                 │      │ Heading
│  │  (globe) OCEAN-GOING       │   │  (globe) OCEAN-GOING       │   │  (coast) COASTAL ONLY      │      │ badge
│  │                            │   │                            │   │                            │      │
│  │  "One big hold. Slow, but  │   │  "Two ships, two trades at │   │  "Fast and nimble, but     │      │ pitch,
│  │   built for the Atlantic." │   │   once."                   │   │   can never reach Europe." │      │ italic
│  │                            │   │                            │   │                            │      │
│  │  (crate) Cargo   90 crates │   │  (crate) Cargo   68 crates │   │  (crate) Cargo   48 crates │      │ fleet
│  │  (sail)  Speed   ▓▓▓░░     │   │  (sail)  Speed   ▓▓▓▓░     │   │  (sail)  Speed   ▓▓▓▓▓     │      │ totals,
│  │  (hull)  Hull    130 each  │   │  (hull)  Hull     70 each  │   │  (hull)  Hull     40 each  │      │ bars
│  │  (coin)  Wages   32 /day   │   │  (coin)  Wages    36 /day  │   │  (coin)  Wages    36 /day  │      │ compare
│  │  ───────────────────────── │   │  ───────────────────────── │   │  ───────────────────────── │      │ across
│  │  Cost 6,000 · 2,500 left   │   │  Cost 6,000 · 2,500 left   │   │  Cost 6,000 · 2,500 left   │      │ cards
│  │                            │   │                            │   │                            │      │
│  │     [*     Choose     *]   │   │     [*     Choose     *]   │   │     [*     Choose     *]   │      │
│  └────────────────────────────┘   └────────────────────────────┘   └────────────────────────────┘      │
│     460 × 720 each, 48 px apart                                                                        │
└────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

- A full-screen modal over the dimmed map (so Havana and the routes are visible behind it).
- **Three cards** from one `FleetCard` prefab. Card content comes from `ships.csv` and `GameData.FleetCount`:
  - picture placeholder (a sprite field per ship type on the card data, see §5.3)
  - "count × name"
  - an **ocean-going / coastal badge**: icon + word + colour (Ocean blue / InkSoft)
  - a one-line pitch (written by the designer; see §5.3 for where it lives)
  - **cargo as the fleet total** (count × capacity), speed as a five-step bar scaled across the three types plus the number on hover, hull per ship, wages as the fleet total per day
  - **cost and gold left**, which is what the player actually needs
  - a **Choose** button. Only the button commits, not the whole card. Hover lifts the card (border turns Gold).
- The coastal badge on Sloops is Warning-coloured with a tooltip: "Sloops can't sail ocean routes, so they can't reach Liverpool, Bordeaux or Seville."
- Order left to right: Merchantman, Brigs, Sloops (as now, most expensive first).
- *(optional)* After Choose, a short confirmation: the card stays highlighted for 0.4 s and the ships slide into Havana.

### 3.5 The map

```
 ┌──────────────────────────────────────────────────────────────────────────────────────────────┐
 │▒▒▒▒▒▒▒▒▒▒ land ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒   ● Boston (anchor+crane)                                     │
 │▒▒▒ North America ▒▒▒▒▒▒▒▒▒▒▒▒▒   ● New York               ═══════ ocean route ═══════  ▒▒▒▒▒▒│
 │▒▒▒ ◉ Natchez ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒╲                                              ● Liverpool ▒▒▒▒│
 │▒▒▒   ┃ owned (solid, bright)  ╲                                              ● Bordeaux ▒▒▒▒│
 │▒▒▒ ◉ New Orleans ▒▒▒▒▒▒▒▒▒▒▒▒▒ ╲  coast                                       ○ Seville  ▒▒▒│
 │▒▒▒▒▒▒   ┃                ▒▒▒▒▒▒▒ ╲                                            Europe  ▒▒▒▒▒▒▒│
 │    ◉ Havana (shipyard) ━━━━━━━━━━━━ ◉ Port-au-Prince                                         │
 │  ○ Campeche  ┄┄ buyable (dashed) ┄┄       ┄┄ ○ Bridgetown                                    │
 │ ○ Veracruz                                                                                   │
 │                                                                                              │
 └──────────────────────────────────────────────────────────────────────────────────────────────┘
```

**Sea and land:**
- **Sea:** a single painted texture (or a simple gradient with a faint paper grain) covering the whole camera view, so there is no "rectangle inside a rectangle". Colours: Sea / SeaDeep tokens.
- **Land:** one placeholder sprite, `MapLand.png`, on top of the sea, in the same 100×60 space as `ports.csv` (so 4096×2458 px fits exactly). Flat Land colour with a darker coast line and a faint inner shade. Region names (Gulf of Mexico, Caribbean Sea, North America, Europe, Atlantic Ocean) as large, light, letter-spaced text in the art or as world-space TMP labels.
- **The map art and the port positions must agree.** The art is drawn over a guide image of the port dots, or ports are moved to fit the art; see open question Q2.
- *(optional)* A compass rose and a cartouche with "1770" in an empty corner; a soft vignette at the edges.

**Port markers** (one `PortMarker` prefab):

```
     ┌──────────────┐
     │  New Orleans │   label: Body size, Ink on a Parchment tag with 1 px Ink border
     └──────┬───────┘
            ◉          marker: 28 px circle (screen size); shipyards add a crane/anchor badge
```

| State | Marker | Label | Meaning |
|---|---|---|---|
| **Reachable** (on the owned network) | filled Parchment circle, Ink ring | Ink on Parchment tag | Ships can sail here now |
| **Buyable next** (one route from the network) | hollow circle, Ink ring | InkSoft on Parchment tag | A route you can buy would bring it in |
| **Unreachable** | small hollow circle, InkSoft ring, 60% opacity | InkSoft, no tag | Not connected yet |
| **Has your ships** | small count badge "2" at the top right of the marker | | Ships waiting in port |
| **Shipyard** | adds a (shipyard) icon badge at the bottom right | | Can buy ships and upgrades |
| **Hover** | ring turns Gold, marker scales 110% | | Clickable |

- Labels sit above the marker by default. Each `PortMarker` has a **label offset** field so the designer can nudge crowded names (New York and Boston, Natchez and New Orleans) without code.
- Labels are screen-readable at all supported aspects: at least 20 px tall at 1080p.
- *(optional)* Hover shows a tooltip with "Cheap here: Sugar, Tobacco · Pays well: Hardware, Cloth" from `market.csv`.

**Routes** (one `RouteLine` prefab with a `LineRenderer`):

| Style | Colour | Line | Width | Meaning |
|---|---|---|---|---|
| **Owned, coastal** | RouteCoast (cream) | solid | 6 px | Your ships can sail it |
| **Owned, ocean** | RouteOcean (pale blue) | solid, with small wave ticks *(optional)* | 6 px | Your ocean-going ships can sail it |
| **Buyable** (connects to your network) | same colour as its kind | long dashes | 4 px | Click to buy |
| **Not connected** | same colour as its kind, 35% opacity | dots | 3 px | Can't be bought yet |
| **Highlighted** (send preview, hover) | Gold | solid, animated dashes moving in the direction of travel | 8 px | The path you're looking at |

- **Colour and pattern both carry meaning**, so the styles still read for colour-blind players and in a screenshot.
- **Routes follow the sea:** each route can have bend points so it goes around Florida, the Yucatán and so on. The `RouteLine` prefab instance holds its points (see §5.6); ships follow the drawn line.
- During the free-routes phase, buyable routes pulse gently.

**Legend** (bottom left, collapsible, 280 px wide):

```
 ┌ Legend ───────────── (–) ┐
 │ ━━━  Owned route          │
 │ ┅┅┅  Can buy              │
 │ ┄┄┄  Not connected        │
 │ cream coastal · blue ocean│
 │ ◉ Reachable  ○ Not yet    │
 │ (crane) Shipyard          │
 ├───────────────────────────┤
 │ (scroll) Log (3 new)      │   opens the notification log
 └───────────────────────────┘
```

Each line is a `LegendEntry` prefab (a swatch image plus a label) placed in the scene by hand: the legend is fixed content, so it's built in the editor, not from code.

**Ships on the map** (one `ShipMarker` prefab):
- A small top-down ship sprite (Parchment hull, Ink outline) pointing along its heading, 32 px.
- **Selected:** a Gold ring under it that pulses slowly, and its path to its destination drawn in Gold dashes.
- **Hover:** tooltip "Brig 2 → Boston, arrives 3 Aug · 34 crates · hull 52/70".
- In port, up to four ships fan out in a small arc beside the marker; beyond four, a "+2" badge.
- Clicking a ship at sea selects it; clicking a ship in port opens that port with the ship selected.
- *(optional)* A short wake trail behind moving ships.

### 3.6 Route panel (right drawer)

```
 ┌──────────────────────────────────────────┐
 │ Havana — New Orleans               (×)   │  Title + close
 │ (coast) Coastal route                    │  badge; ocean: "(globe) Ocean route · not for sloops"
 │──────────────────────────────────────────│
 │ About 3 days by Brig                     │  from the route's distance and the player's ships
 │                                          │
 │ Risk on one crossing                     │  Heading
 │ (pirate)    Pirates    ▓▓░░░  Moderate   │  meters: chance over one trip,
 │ (storm)     Storms     ▓░░░░  Low        │  with "per day: 4%" in the tooltip
 │ (hurricane) Hurricanes ▓▓▓░░  High in    │
 │                               season     │
 │──────────────────────────────────────────│
 │ Brings into reach                        │
 │   ◉ New Orleans                          │  new ports this route adds
 │   ○ Natchez (one more route)             │
 │──────────────────────────────────────────│
 │                                          │
 │  Cost  (coin) 500                        │
 │  [*       Buy route — 500 gold      *]   │  primary
 │                                          │
 │  or, when it can't be bought:            │
 │  (lock) Not connected to your routes.    │  shown instead of the button,
 │  Buy a route that reaches Havana first.  │  before the player tries
 └──────────────────────────────────────────┘
   560 × full height under the top bar
```

- **States:** Owned (a "(check) Owned" badge, no button), Free (during the opening: "Take as free route (1 of 2 left)"), Buyable (price on the button), Not connected (lock + reason), Too expensive (button disabled, caption "You have 320 gold").
- **Risk per crossing**, not per day: the panel turns the per-day chance into a chance over one crossing for the player's fastest ship that can use it (same maths as the preview). Bands come from the theme asset (§4.2). The per-day figures stay in the tooltip for players who want them.
- **"Brings into reach"** lists ports that become reachable if bought (`Network.Reaches` before vs after). This is what makes buying a route feel like a decision.
- The route is highlighted Gold on the map while the drawer is open.

### 3.7 Free-routes phase

The text banner becomes the **objective card** (the same prefab as the tutorial card, §3.12):

```
            ┌────────────────────────────────────────────────────────┐
            │ (compass)  Choose 2 free routes                0 / 2   │
            │ Dashed routes start at Havana. Click one to see where  │
            │ it leads.                                              │
            └────────────────────────────────────────────────────────┘
```

Buyable routes pulse. The route drawer's button reads "Take as free route".

### 3.8 Port panel

A centred modal, 1440 × 860. Two tabs at shipyards (**Market**, **Shipyard**), one view elsewhere.

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ ◉ Havana   (crane) Shipyard     Caribbean · hurricane zone                                       (coin) 2,500 │ Title row
│ ┌─[ Market ]─┬─[ Shipyard ]─┐                                                                          (×)    │ tabs
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Ships here:  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐                                  │ ship chips
│              │▓Brig 1  ▓▓▓▓░ ▓▓░│ │ Brig 2  ▓▓▓▓▓ ░░░│ │ Sloop 4 ▓▓░░░ ▓▓▓│                                  │ (name, hull,
│              └──────────────────┘ └──────────────────┘ └──────────────────┘                                  │  hold mini-bars)
├───────────────────────────────────────┬──────────────────────────────────────────────────────────────────────┤
│ BRIG 1                                │  Quantity  [ 1 | 5 |▓10▓| Max ]                                      │
│                                       │ ┌──────────────────┬───────┬───────┬────────┬──────────────────────┐ │
│ (crate) Hold   ▓▓▓▓▓▓▓▓░░░░  22 / 34  │ │ Good             │  Buy  │  Sell │ Aboard │ Actions              │ │ header
│ (hull)  Hull   ▓▓▓▓▓▓▓▓▓▓░░  52 / 70  │ ├──────────────────┼───────┼───────┼────────┼──────────────────────┤ │
│         [ (hammer) Repair · 180 ]     │ │ (i) Sugar  CHEAP │    41 │    37 │     12 │ [ Buy 10 ] [ Sell 10]│ │ row
│                                       │ │ (i) Tobacco CHEAP│    38 │    34 │     10 │ [ Buy 10 ] [ Sell 10]│ │
│ Cargo                                 │ │ (i) Coffee       │   118 │   106 │      — │ [ Buy 10 ] [  Sell  ]│ │ Sell disabled
│   Sugar ×12  · Tobacco ×10            │ │ (i) Hardware PAYS│   190 │   171 │      — │ [ Buy 10 ] [  Sell  ]│ │
│                                       │ │ (i) Cloth   PAYS │   176 │   158 │      — │ [ Buy 10 ] [  Sell  ]│ │
│ Aboard, not traded here:              │ │ …                │       │       │        │                      │ │
│   Furs ×4                             │ │                  │       │       │        │                      │ │ scrolls if
│                                       │ └──────────────────┴───────┴───────┴────────┴──────────────────────┘ │ more than 10
│                                       │  CHEAP = sells below the usual price here.                           │
│                                       │  PAYS  = this port pays above the usual price. (season) +25% in season │ caption
├───────────────────────────────────────┴──────────────────────────────────────────────────────────────────────┤
│  Bought 10 Sugar for 410 gold.                                  [ Close ]   [*  Send Brig 1…  *]             │ result + actions
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
    left column 400 px                      market table ~1000 px; row height 44
    (prices in the sketches are illustrative; tags follow Havana's real multipliers in market.csv)
```

- **Ship chips** replace the `< Ship 1 of 3 >` stepper: every ship in port is visible with its name and two mini-bars (hull, hold). The selected chip is filled Ink. More than five ships scroll sideways.
- **Left column, the selected ship:**
  - **Hold bar** (crates / capacity), segmented every 10 crates *(optional)*. While hovering a Buy button, the bar shows a ghost of the crates that would be added.
  - **Hull bar**, coloured Good above 60%, Warning 30 to 60%, Bad below 30%, plus the number.
  - **Repair** with the cost on the button; disabled (not hidden) at full hull, with "Hull is full" as tooltip, so the layout doesn't jump.
  - A plain cargo list and, if any, "Aboard, not traded here".
- **Market table** (one `MarketRow` prefab per good):
  - Columns: **Good | Buy | Sell | Aboard | Actions**, as asked.
  - **Quantity is chosen once** in the table header with a segmented control: 1, 5, 10, Max (Max = as many as the hold and gold allow when buying, everything aboard when selling). Each row then needs only two buttons, **Buy N** and **Sell N**, instead of four. That halves the buttons and keeps the numbers in the labels honest.
  - *(optional)* Shift-click on Buy or Sell uses Max for that one click.
  - **Tags** from data the game already has: CHEAP when this port's buy multiplier is at or below `zone_export_max_multiplier` (0.65); PAYS when its sell multiplier is high (threshold in the theme asset, e.g. 1.5); a small (season) icon when the season premium applies. Tag = word + colour, never colour alone.
  - Numbers right-aligned, tabular figures, no "gold" suffix in cells (the column header has the coin icon).
  - Buttons disabled when they can't work (no gold, no space, nothing aboard), with the reason in a tooltip. A refused command still shows its reason in the result line.
  - **Feedback:** the clicked row flashes Good (or Bad if refused) for 0.3 s, and the result line at the bottom repeats the message next to the action buttons.
- **Footer:** the result line (left), Close (secondary) and **Send** (primary, named after the ship).
- **Gold** is in the title row so it stays visible while the HUD is dimmed.

**Shipyard tab:**

```
├───────────────────────────────────────┬──────────────────────────────────────────────────────────────────────┤
│ BRIG 1   (same left column)           │ Buy a ship                                                           │
│                                       │ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                   │
│                                       │ │ [pic] Sloop  │ │ [pic] Brig   │ │ [pic] Merch. │                   │ compact
│                                       │ │ 16 crates    │ │ 34 crates    │ │ 90 crates    │                   │ ship cards
│                                       │ │ coastal      │ │ ocean        │ │ ocean        │                   │
│                                       │ │[ Buy 2,000 ] │ │[ Buy 3,000 ] │ │[ Buy 6,000 ] │                   │
│                                       │ └──────────────┘ └──────────────┘ └──────────────┘                   │
│                                       │                                                                      │
│                                       │ Upgrade Brig 1                                                       │
│                                       │ (sail)  Sails   ●○  Faster sailing    next: Studding sails [ 1,050 ] │ upgrade rows,
│                                       │ (hull)  Hull    ●●  Complete (check)                                 │ level pips
│                                       │ (crate) Hold    ○○  More cargo        next: Rearranged hold [ 240 ]  │
├───────────────────────────────────────┴──────────────────────────────────────────────────────────────────────┤
```

- Buying a ship and upgrading the selected ship are visibly separate sections.
- Upgrade rows show **level pips** (two levels each), what the track does in plain words, the next level's name, and its price. "Complete" gets a check icon.
- *(optional)* The upgrade row shows the stat change ("Speed 1.30 → 1.43").

### 3.9 Send panel (right drawer) and the route preview

```
 ┌──────────────────────────────────────────┐
 │ Send Brig 1 from Havana            (×)   │
 │ (crate) 22 / 34 crates · (hull) 52 / 70  │  what's being sent
 │──────────────────────────────────────────│
 │ Destination          Days  Risk          │  header
 │ ┌──────────────────────────────────────┐ │
 │ │ New Orleans          4    ●○○        │ │  DestinationRow prefabs,
 │ │ Natchez              7    ●●○        │ │  sorted by days.
 │ │▓Port-au-Prince▓▓▓▓▓▓▓5▓▓▓▓●○○▓▓▓▓▓▓▓▓│ │  Risk = worst of the
 │ │ Boston              14    ●●●        │ │  three, as pips + word
 │ │ (lock) Liverpool  — needs an ocean   │ │  on hover.
 │ │        route                         │ │
 │ └──────────────────────────────────────┘ │
 │──────────────────────────────────────────│
 │ Havana → Port-au-Prince                  │  stops; also drawn on the map
 │                                          │
 │ Arrives  14 Jul 1770       (5 days)      │  Heading size
 │                                          │
 │ (pirate)    Pirates     ▓▓▓░░░░░░░  15%  │  risk meters, 0–100% scale,
 │ (hurricane) Hurricane   ▓░░░░░░░░░   7%  │  coloured by band
 │             in season                    │  "Out of season" greyed when 0
 │ (sink)      Loss of     ▓░░░░░░░░░   2%  │  loss chance, from hull
 │             this ship                    │
 │   Hull 52/70. [ Repair first · 180 ]     │  shown when repair would help
 │                                          │
 │ (i) Sugar and Tobacco sell well here.        │  optional sale hint
 │──────────────────────────────────────────│
 │ [ Back ]           [*   Set sail   *]    │  disabled until a destination
 └──────────────────────────────────────────┘  is picked, never hidden
```

- **The destination list is comparable:** each `DestinationRow` shows the port, days, and a three-pip risk summary for *this ship*, from `GameState.Preview` per destination (12 ports, cheap to compute when the panel opens). Rows the ship can't use (a sloop and an ocean route) are shown locked with the reason instead of hidden, so the player learns the rule.
- **Route on the map:** the chosen path is drawn Gold with numbered stop badges (1, 2, …) and the destination marker pulses. The map camera shrinks to the area left of the drawer so every port stays visible.
- **Risk meters** (one `RiskMeter` prefab, used here and in the route drawer): icon, label, bar, percentage. Band colours: Low (Good), Moderate (Warning), High (Bad), with the band word in the tooltip. Thresholds live in the theme asset (open question Q5).
- **Repair first** appears when the ship is damaged and repairing would cut the loss chance; it repairs in place and refreshes the preview.
- **Empty ship warning:** if nothing is aboard, a Warning line "The hold is empty." above Set sail (sending is still allowed).
- **Set sail** is the primary button; disabled with a tooltip until a destination is chosen.
- *(optional)* **Sale hint:** the best one or two goods aboard by sell price at the destination.

### 3.10 Notifications: toasts and log

**Toasts** (bottom centre, 560 px wide, at most 3 visible, newest at the bottom):

```
   ┌──────────────────────────────────────────────────────┐
   │▌(anchor) Brig 1 arrived in New Orleans · dues 40     │  Info: Ink bar
   └──────────────────────────────────────────────────────┘
   ┌──────────────────────────────────────────────────────┐
   │▌(pirate) Pirates attacked Sloop 4 · 6 crates taken,  │  Warning: Warning bar + icon
   │          hull −9                                     │
   └──────────────────────────────────────────────────────┘
   ┌──────────────────────────────────────────────────────┐
   │▌(sink) Sloop 3 sank with its cargo           [ View ]│  Bad: Bad bar, stays until
   └──────────────────────────────────────────────────────┘  dismissed or viewed
```

| Event | Icon | Severity | Stays |
|---|---|---|---|
| Ship arrived | anchor | Good (Good bar) | 6 s real time |
| Storm | storm | Warning | 8 s |
| Pirates | pirate | Warning | 8 s |
| Hurricane | hurricane | Bad | until dismissed |
| Ship sunk | sink | Bad | until dismissed |
| *(optional)* Hurricane season starts / peaks | hurricane | Warning | 8 s |

- One `Toast` prefab: severity bar (colour), icon, text, optional View button, close on click.
- Clicking a toast (or View) selects the ship, or opens the port for an arrival.
- Toast timers pause while a panel holds the clock, so nothing is missed while trading.
- When more than three are queued, the oldest Info toast goes first; Bad toasts are never dropped.
- **Log:** the Log button under the legend opens a small scrolling list of the last 50 events with the date, icon and text (one `LogEntry` prefab). The button shows "(3 new)" since it was last opened.

### 3.11 Fleet list (bottom right)

```
 ┌ Fleet (4) ─────────────────────────── (–) ┐
 │ (anchor) Brig 1     Havana     ▓▓▓▓░ IDLE │  in port, idle: Warning tag
 │ (ship)   Brig 2  → Boston      3 Aug      │  at sea: destination + arrival
 │ (ship)   Sloop 3 → Havana     29 Jul      │
 │ (anchor) Sloop 4    New Orl.   ▓▓░░░      │  hull bar in Bad when low
 └───────────────────────────────────────────┘
   420 px wide, 40 px rows, scrolls after 6 rows
```

- One `FleetListEntry` prefab for every ship, in port or at sea: same look, the icon and text change.
- Columns: state icon, name, place (port, or "→ destination"), then hull mini-bar or arrival date.
- **IDLE** tag on ships sitting in port with an empty hold, since an idle ship costs wages and earns nothing.
- Hover highlights the ship on the map; click selects it (and opens the port if it's in one).
- The selected ship's row is filled Parchment with a Gold left bar, matching the ring on the map.
- Collapsible to a header.

### 3.12 Tutorial: objective card and highlight

```
            ┌────────────────────────────────────────────────────────────────┐
            │ (compass) First voyage · step 3 of 8                    [Skip] │  Caption, InkSoft
            │ Fill the hold, then click Send.                                │  Heading
            │ The crates go straight into Brig 1's hold. Buy more while your │  Body
            │ gold allows.                                                   │
            └────────────────────────────────────────────────────────────────┘
              640 px wide, fixed; under the top bar, centred; never covers the speed control
```

- A fixed-size card (no best-fit shrinking): a one-line **objective** in Heading, an optional line of **explanation** in Body, a **step counter**, and **Skip** (Done on the last step).
- When the port or send panel is open, the card moves to sit just above that panel's top edge, so it never covers a button the step needs. (Two anchor positions set in the scene, not computed.)
- **Highlight:** keep the current pulsing frame, restyled: a 3 px Gold rounded outline with a soft glow, plus a small arrow pointing at it from the card's side. On the map, the frame becomes a ring sized to the port marker.
- *(optional)* Dim everything except the target with a 40% Ink overlay that has a hole for the target. Clicks still pass through as now.
- The text of each step stays in `SliceTutorial`, split into objective and explanation.

### 3.13 Win and lose screens

```
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │                                                                              │
 │                         [ illustration placeholder ]                         │  win: ships in harbour at dusk
 │                                                                              │  lose: a wreck / empty quay
 │                              Fortune made                                    │  Display (64); lose: "Voyage's end"
 │             You reached 500,000 gold on 14 March 1772.                       │  Body; lose: the reason
 │                                                                              │
 │     ┌────────────────┬────────────────┬────────────────┬────────────────┐    │
 │     │ (coin) Gold    │ (ship) Voyages │ (crate) Best   │ (sink) Ships   │    │  stat tiles
 │     │ 512,340        │ 41             │ trade +8,210   │ lost 2         │    │
 │     └────────────────┴────────────────┴────────────────┴────────────────┘    │
 │                                                                              │
 │                    [ Main menu ]   [*  Play again  *]                        │  Main menu only if one exists
 └──────────────────────────────────────────────────────────────────────────────┘
```

- **Win:** warm palette, Gold title rule, short fanfare (phase 5).
- **Lose:** cooler palette (SeaDeep backdrop), Ink title, the reason in plain words: "The two years are up. You ended with 212,400 of 500,000 gold." or "Your last ship is gone and you can't afford another."
- **Stat tiles:** gold, days taken (win) or gold short (lose), voyages completed, ships lost. *(optional)* best single trade, needs a counter in Core or the presentation.
- Play again is the existing Restart; no confirm needed here.

---

## 4. Design system

All values below are first proposals; they live in the theme asset (§5.2) so the designer can change them in one place.

### 4.1 Type

Two fonts, both free for commercial use (SIL Open Font License), imported as TextMeshPro font assets:

- **Display / headings:** a period serif with small caps, e.g. *IM Fell English SC* or *Cormorant SC*. Used only at 28 px and above.
- **Body / numbers:** a sturdy, readable serif or humanist sans with **lining, tabular figures**, e.g. *Alegreya Sans* or *Source Serif 4*. Tables need digits of equal width so columns line up.

Final choice is open question Q3.

| Style | Size (at 1080p) | Font | Use |
|---|---|---|---|
| Display | 64 | Display | End screen title |
| Title | 40 | Display | Panel titles, fleet choice title |
| Heading | 28 | Display | Section headings, card names, HUD numbers |
| Body | 22 | Body | Most text, table cells, buttons |
| Caption | 18 | Body | Labels under numbers, tooltips, legend. **Nothing smaller than 18.** |
| Map label | 20 (screen-equivalent) | Body, semibold | Port names |

Line height 1.25. Buttons use Body in semibold, no ALL CAPS except short tags (CHEAP, PAYS, IDLE), which use Caption with +8% letter spacing.

### 4.2 Colour tokens

| Token | Hex | Use |
|---|---|---|
| `Parchment` | `#EFE2C4` | Panel background |
| `ParchmentDark` | `#DCC89E` | Top bar, table header, alternate rows, secondary button |
| `Ink` | `#2B1F14` | Main text, borders, selected segments |
| `InkSoft` | `#5C4A36` | Secondary text, captions |
| `Gold` | `#C8962E` | Accent: primary button fill, goal bar, selection ring, highlights (fill only; Ink text on it) |
| `GoldText` | `#7A5A12` | Gold-coloured text on Parchment |
| `Good` | `#2F5E2C` | Good text and icons (profit, arrival, healthy hull) |
| `GoodFill` | `#4E8A45` | Good bars and flashes |
| `Bad` | `#8E2A22` | Bad text, danger button fill (Parchment text on it) |
| `BadFill` | `#B8443A` | Bad bars and flashes |
| `Warning` | `#7A4E0A` | Warning text and icons |
| `WarningFill` | `#D9912B` | Warning bars and meters |
| `Ocean` | `#1F5873` | Ocean badge and text |
| `Disabled` | `#8F8270` | Disabled text (always paired with a lock or a tooltip reason) |
| `Overlay` | `#2B1F14` at 55% | Dim behind modals |
| `SeaDeep` | `#16303F` | Map edges, lose screen |
| `Sea` | `#2E5A6E` | Open sea |
| `Land` | `#CDB98C` | Land fill |
| `Coast` | `#6B5638` | Coastline |
| `RouteCoast` | `#F2E3B3` | Coastal routes |
| `RouteOcean` | `#9FD8EA` | Ocean routes |

**Checked contrast** (WCAG ratio):
- Ink on Parchment 12.5 : 1; InkSoft 6.6; Good 5.9; Bad 6.5; Warning 5.6; GoldText 5.0; Ocean 6.1. All pass 4.5 : 1 for body text **on Parchment**.
- On ParchmentDark, Warning (4.4) and GoldText (3.9) fall short, so on ParchmentDark use them only at Heading size or larger, or use Ink.
- Ink on Gold 6.0; Parchment on Bad 6.5; Ink on WarningFill 6.1.
- On the map: Parchment on SeaDeep 10.7; RouteCoast on Sea 5.9; RouteOcean on Sea 4.8; Land against Sea 3.9 (enough for a large shape).

**Theme thresholds** (also in the theme asset): risk bands (Low below 5%, Moderate 5 to 20%, High above 20%; loss chance Low below 2%, Moderate 2 to 10%, High above 10%), hull colour bands (60%, 30%), the PAYS sell-multiplier threshold, days-left warning (90, 30).

### 4.3 Spacing and shape

- **Base unit 4 px.** Steps: 4, 8, 12, 16, 24, 32, 48.
- Panel padding 24; gap between sections 16; between related items 8.
- Row height: tables 44, lists 40. Every click target is at least 40×40 (36 minimum for icon buttons inside dense rows).
- Corner radius 6 on buttons and panels (from the 9-slice sprite). Borders 2 px Ink on panels, 1 px on inner boxes.
- Panel shadow: Ink at 35%, 8 px offset down *(optional)*.

### 4.4 Buttons

| Style | Fill | Text | Border | Use |
|---|---|---|---|---|
| **Primary** | Gold | Ink, semibold | 2 px Ink | The one main action on a screen: Choose, Set sail, Buy route, Send, Resume, Play again |
| **Secondary** | ParchmentDark | Ink | 1 px Ink | Everything else: Close, Back, Buy/Sell in rows, Repair |
| **Danger** | Bad | Parchment, semibold | 2 px Ink | Restart (in the confirm dialog), Quit |
| **Icon** | none (hover: ParchmentDark) | Ink icon | none | Close ×, menu, collapse |
| **Segmented** | ParchmentDark; selected Ink | Ink; selected Parchment | shared 1 px Ink | Speed control, quantity, tabs |

- **States:** normal; hover (fill 8% lighter, border Gold); pressed (fill 8% darker, content 1 px down); disabled (60% opacity, Disabled text, no hover, tooltip says why); selected (segmented only).
- Heights: 52 primary, 44 secondary, 36 small (rows), 40 segmented.
- **One primary button per panel.** Destructive actions are never next to frequent actions, and always confirm.
- Built as button prefabs (§5.1) using Unity's `Button` with Color Tint transitions and a 9-slice sprite.

### 4.5 Panels

- **Modal panel:** Parchment 9-slice with a 2 px Ink border and radius 6. Title row 64 px (Title text, close × at the top right), 24 px padding. The overlay behind blocks clicks.
- **Drawer:** same style, docked right, full height under the top bar, left edge only bordered. Slides in over 0.15 s *(optional)*.
- **Card:** Parchment, 1 px Ink border, 16 px padding; hover border Gold.
- **Tooltip:** Ink background, Parchment Caption text, 8 px padding, max 320 px wide, appears after 0.4 s.
- *(optional)* A faint paper-grain texture on panels and a thin double rule under titles for the period feel.

### 4.6 Icons

One set of single-colour icons, 64×64 PNG, white on transparent so Unity can tint them. Placeholder art is fine; sources are open question Q4.

| Group | Icons |
|---|---|
| HUD | calendar, coin, hourglass, hurricane, menu, pause, play, fast, fastest |
| Ships and ports | ship (at sea), anchor (in port), shipyard (crane), hull (shield), hold (crate), sails, repair (hammer) |
| Routes | coastal, ocean (globe or waves), lock, check |
| Hazards and events | pirate (flag), storm (cloud and bolt), sink, season |
| UI | close, collapse, arrow, info, warning, log (scroll), compass (objective) |

About 30 icons in total.

### 4.7 Sound (placeholder for phase 5)

Not built now; the spec only reserves where sounds go so the hooks exist. Suggested set: button click, tab or segment switch, buy (coins), sell (bigger coins), set sail (rope and sail), arrival (ship's bell), pirates (distant cannon), storm and hurricane (thunder, wind), sinking (low creak), season starts (wind rising), win (fanfare), lose (low strings), ambient sea loop, one music track. A single `SliceAudio` component with one serialized `AudioClip` field per sound is enough.

### 4.8 Accessibility

- **Contrast:** body text at least 4.5 : 1, large text and UI shapes at least 3 : 1 (checked in §4.2).
- **Readable sizes:** nothing under 18 px at 1080p; tables at 22.
- **Colour is never the only signal:** route styles also differ in line pattern; port states in fill and size; toasts and meters have icons and words; tags are words; hull bars carry the number.
- **Hover is never the only way to learn something important:** tooltips add detail, but the decision-critical figures are on screen.
- **Keyboard:** Esc (close or menu), Space (pause), 1/2/3 (speed). *(optional)* Enter for the primary button in dialogs.
- **Resolution and aspect:** the Canvas Scaler stays at 1920×1080 with match 0.5. Every element is anchored to its zone (top bar stretches; legend bottom-left; toasts bottom-centre; fleet list bottom-right; drawer right; modals centred). Test 16:9, 16:10, 21:9 and 4:3 in the Game view. At 4:3 the port panel shrinks to 90% width with the market table scrolling; the fleet choice cards keep their size and the gap between them shrinks.
- **Motion:** pulsing and floating numbers are short and subtle. *(optional)* a "reduce motion" toggle.
- *(optional)* A UI scale setting (90 to 120%).

---

## 5. How it's built in Unity

The rule: **what a designer sees in the editor is what the player sees.** Every screen, row and marker exists as a scene object or a prefab that can be opened, selected and restyled. Scripts fill in data; they never create widgets from scratch.

### 5.1 Scene objects and prefabs

**In `Slice.unity`** (as now, rearranged):
- `Main Camera`, `EventSystem`, `Game` (bootstrap, clock, map, ships).
- `Map` under `Game`: the sea and land sprites, 12 `PortMarker` instances, 17 `RouteLine` instances and the region labels, placed by hand on the art (see Q2).
- `Canvas` with children: `Top Bar`, `Legend`, `Toasts`, `Fleet List`, `Objective Card`, `Tutorial Frame`, and the panels: `Fleet Choice`, `Route Drawer`, `Send Drawer`, `Port Panel`, `Pause Menu`, `Confirm Dialog`, `Log`, `End Screen`.
- **Each panel is a prefab instance** (`Prefabs/UI/Panels/PortPanel.prefab` and so on). The designer edits it in Prefab Mode, and two people can change different panels without scene merge conflicts.

**Prefabs** (new `Assets/_NavalTradeMinigame/Prefabs/UI/` and `Prefabs/Map/`):

| Prefab | Script on it | Where it's used |
|---|---|---|
| `Button Primary`, `Button Secondary`, `Button Danger`, `Button Icon` | none (Unity `Button`) | Everywhere; variants of one base button prefab |
| `Segmented Control` | `SegmentedControl` | Speed, quantity, port tabs |
| `Tooltip` | `TooltipView` | One instance on the Canvas |
| `Fleet Card` | `FleetCardView` | Fleet choice (3) |
| `Ship Chip` | `ShipChipView` | Port panel |
| `Market Row` | `MarketRowView` | Port panel |
| `Ship For Sale` | `ShipForSaleView` | Shipyard tab |
| `Upgrade Row` | `UpgradeRowView` | Shipyard tab (3, placed in the prefab) |
| `Destination Row` | `DestinationRowView` | Send drawer |
| `Risk Meter` | `RiskMeterView` | Send and route drawers |
| `Toast` | `ToastView` | Toasts |
| `Log Entry` | `LogEntryView` | Log |
| `Fleet List Entry` | `FleetListEntryView` | Fleet list |
| `Legend Entry` | none (an Image and a TMP text) | Legend, placed by hand |
| `Stat Tile` | none | End screen, placed by hand |
| `Port Marker` | `PortMarkerView` | Map (12 instances) |
| `Route Line` | `RouteLineView` | Map (17 instances) |
| `Ship Marker` | `ShipMarkerView` | Map, spawned per ship |

### 5.2 The theme asset

`UiTheme` is a ScriptableObject (`Create → Naval → UI Theme`, one asset, e.g. `Assets/_NavalTradeMinigame/Datas/UI/SliceTheme.asset`). That menu only creates an empty asset the designer fills in; it doesn't generate content.

It holds:
- **Colours:** every token from §4.2, as named `Color` fields with tooltips.
- **Fonts:** `TMP_FontAsset displayFont`, `TMP_FontAsset bodyFont`.
- **Type sizes:** display, title, heading, body, caption, map label.
- **Thresholds:** risk bands, hull bands, the PAYS threshold, days-left warnings.
- **Timings:** toast durations, pulse speed.

Two tiny components apply it, so a designer can recolour or re-font the whole UI from one asset:
- `ThemedText` (on a TMP text): a style dropdown (Display, Title, Heading, Body, Caption) and a colour token dropdown.
- `ThemedGraphic` (on an Image): a colour token dropdown.

Both apply in `OnEnable` and `OnValidate` (marked `[ExecuteAlways]`), so the editor shows the themed look; after editing the theme asset, colours refresh when the scene or prefab is reopened. Code that picks a colour at runtime (route styles, toast severity, risk bands, hull bands) reads it from the theme through a serialized `UiTheme` reference, never from a hard-coded `Color`.

Prefabs that are already their own single source (the button variants) can carry their look directly; `ThemedText`/`ThemedGraphic` are for the places where a token is reused widely.

### 5.3 Data-driven rows

Each row prefab has a small view script with **serialized references to its own children** and one `Bind` method. The panel that owns the rows has a serialized prefab reference and a container.

```csharp
public sealed class MarketRowView : MonoBehaviour
{
    [SerializeField] Image goodIcon;
    [SerializeField] TMP_Text goodName;
    [SerializeField] TMP_Text tag;
    [SerializeField] TMP_Text buyPrice;
    [SerializeField] TMP_Text sellPrice;
    [SerializeField] TMP_Text aboard;
    [SerializeField] Button buyButton;
    [SerializeField] TMP_Text buyLabel;
    [SerializeField] Button sellButton;
    [SerializeField] TMP_Text sellLabel;

    public event Action<Good> BuyClicked;
    public event Action<Good> SellClicked;

    public void Bind(Good good, int buy, int sell, int aboardCount, int quantity, MarketTag marketTag, bool canBuy, bool canSell) { … }
}
```

- The panel keeps a list of row instances and **reuses them** on refresh (activate, bind, deactivate the spare ones) instead of destroying and recreating every row after every click, as `SliceUi.Clear` does now.
- **Fleet card data:** the picture and the one-line pitch for each ship type don't belong in the rules CSV. They go in a small `ShipPresentation` ScriptableObject (sprite and pitch per ship name), one asset the designer edits. A missing entry logs a clear error and shows the placeholder.
- Fixed content (legend, upgrade rows, stat tiles, the three fleet cards) can be placed in the prefab by hand rather than instantiated. Only rows whose count depends on data (market goods, ships, destinations, toasts, log, fleet list) are instantiated.

### 5.4 TextMeshPro

- TMP Essentials are already imported (`Assets/TextMesh Pro`). The two fonts are added under `Art/Fonts/`, and their TMP font assets are made once with the Font Asset Creator by the designer and committed.
- The font assets are assigned in the theme asset and on the prefabs' TMP components in the Inspector.
- All UI text moves from legacy `Text` to `TextMeshProUGUI`; port and region labels on the map use world-space `TextMeshPro`, replacing `TextMesh`.
- **Note:** the cloud compile check only has small stand-ins for TextMeshPro, so TMP code is checked by hand and needs a compile in the Unity editor.

### 5.5 No runtime widget factories

- `SliceUi`'s widget-making methods (`MakeText`, `MakeLabel`, `MakeButton`, `MakeRow`, `Clear`) are removed. Its formatting helpers (`Percent`, `Date`, `Count`, `ShipName`) stay, renamed `SliceFormat`.
- `SliceSprites` (circle, triangle and square textures made in code) is replaced by placeholder sprite files.
- Nothing builds a GameObject from `new GameObject` plus `AddComponent` for UI or map content. Instantiating a designer-made prefab is the only way rows appear.

### 5.6 Script changes

| Script | Change |
|---|---|
| `SliceUi` | Becomes `SliceFormat` (text helpers only). Widget methods and colours removed. |
| `SliceHud` | Top bar: date, season indicator from `GameState.Season`, goal bar, days left with warning bands, speed segmented control with a "held" state from `SliceClock.Held`, keyboard shortcuts, Menu button. Restart and Tutorial buttons and the "Sure?" logic move out. |
| **new** `SlicePauseMenu` | Esc handling (closes the top panel via `SliceScreens`, else opens the menu), Resume, Replay tutorial, Restart, Quit. |
| **new** `SliceConfirmDialog` | Reusable confirm: title, body, confirm label, danger flag, callback. |
| `SliceScreens` | Knows the drawer vs modal panels; tells `SliceMap` how much screen the drawer takes; owns the selected ship; routes Esc. |
| `SliceFleetChoice` | Binds three `FleetCardView`s (placed in its prefab); reads pictures and pitches from `ShipPresentation`. |
| `SliceMap` | Uses the scene's `PortMarkerView` and `RouteLineView` instances instead of making sprites and lines; sets port states (reachable, buyable next, unreachable, ships here) and route styles from the theme; highlights a path; fits the camera to the free area (`Camera.rect`) when a drawer is open. |
| `SliceShips` | Spawns `ShipMarker` prefabs; follows the route's drawn points (from `RouteLineView`) instead of a straight line; shows selection; fans out ships in port. |
| `SliceRoutePanel` | Becomes a drawer: badge, days, per-crossing risk meters, "brings into reach", state-specific button. |
| `SlicePortPanel` | Ship chips, hold and hull bars, quantity control, `MarketRowView` rows with tags, tabs, shipyard cards and upgrade rows with level pips, reused row instances. |
| `SliceSendPanel` | Drawer with `DestinationRowView`s (days and risk per destination), `RiskMeterView`s, path drawn on the map, Repair first, empty-hold warning, Set sail disabled rather than hidden. |
| `SliceNotifications` | `ToastView`s with severity and icons, sticky Bad toasts, click to select or open, timers paused while held; keeps the last 50 events for the log. |
| **new** `SliceLog` | The log panel and the "(n new)" count. |
| `SliceFleetList` | `FleetListEntryView` per ship, IDLE tag, hover and click linked to the map selection. |
| `SliceTutorial` | Objective card prefab (objective, explanation, step counter); two card anchor positions; restyled frame; legacy `Text` to TMP. Step logic unchanged. |
| `SliceEndScreen` | Win and lose variants, stat tiles, Play again. |
| **new** `UiTheme`, `ThemedText`, `ThemedGraphic`, `ShipPresentation`, `SegmentedControl`, `TooltipView`, and the row view scripts in §5.1 | As described above. |
| `Naval.Core` | No rule changes. *(optional)* counters for the end screen (voyages completed, ships lost, best trade) on `GameState`, with tests. |

---

## 6. Implementation plan (after approval)

Small steps, each one reviewable on its own and each ending with the usual review (`.claude/skills/review-after-code`) and a check in the Unity editor by the owner.

| Step | What | Done when |
|---|---|---|
| **1. Foundations** | `UiTheme` asset, `ThemedText`/`ThemedGraphic`, the two TMP font assets, the button prefabs, `SegmentedControl`, `TooltipView`, the placeholder icon set. `SliceUi` split into `SliceFormat`. | The theme asset exists and recolours a test button in the editor. |
| **2. Top bar and menu** | `SliceHud` rebuilt as the top bar with season indicator, goal bar and speed control (with the held state); `SlicePauseMenu`; `SliceConfirmDialog`; Restart and Tutorial moved into the menu; keyboard shortcuts. | Esc opens the menu, Restart asks in a dialog, and opening a port shows "Paused while you trade". |
| **3. Fleet choice** | `Fleet Card` prefab, `ShipPresentation` asset, `SliceFleetChoice` rebuilt. | The opening shows three cards; only Choose commits. |
| **4. Map art and markers** | Placeholder sea and land, `Port Marker`, `Route Line` and `Ship Marker` prefabs placed in the scene; port states; route styles; legend; ships following route points; selection. `SliceSprites` removed. | The map reads as a chart; every route style is in the legend; ships go around land. |
| **5. Route drawer and free routes** | Drawer layout, camera fit, risk meters, "brings into reach", objective card for the free routes. | Buying a route shows what it gives before clicking. |
| **6. Port panel** | Ship chips, bars, quantity control, `Market Row` with tags, tabs, shipyard cards and upgrade rows. | A 14-good port fits; buying 10 Sugar is one click; upgrades show their level. |
| **7. Send drawer** | Destination rows, preview meters, path on the map, Repair first, empty-hold warning. | Destinations can be compared without clicking each. |
| **8. Toasts, log, fleet list** | `Toast`, `Log Entry`, `Fleet List Entry` prefabs and their scripts. | A sinking stays until dismissed; clicking it selects the ship. |
| **9. Tutorial and end screen** | Objective card, restyled frame, step counter; win and lose screens with stat tiles. | The tutorial reads as an advisor card; the end screen summarises the run. |
| **10. Accessibility pass** | Check every screen at 16:9, 16:10, 21:9 and 4:3; contrast; minimum sizes; colour-blind check (a simulator on screenshots). | All four aspects look right; nothing relies on colour alone. |

Steps 2 to 9 are mostly independent once step 1 is in; steps 4 and 5 go together because the drawer needs the camera fit.

### 6.1 Placeholder art needed

All placeholders can be simple flat shapes; final art comes in phase 5.

| Asset | Size | Notes |
|---|---|---|
| `MapSea.png` | 2048×1229 (or a gradient material) | Fills the camera; tiles or stretches |
| `MapLand.png` | 4096×2458 | Land masses for the Gulf, Caribbean, eastern North America and western Europe, aligned to `ports.csv` (x 0–100, y 0–60) |
| Region labels | text | Gulf of Mexico, Caribbean Sea, Atlantic Ocean, North America, Europe |
| Port marker sprites | 64×64 | Filled circle, hollow circle, shipyard badge, count badge |
| Ship marker sprite | 64×64 | Top-down hull pointing up; selection ring |
| Route dash and dot textures | 64×16 | For `LineRenderer` texture mode Tile |
| Panel 9-slice | 64×64 | Parchment with Ink border, radius 6 |
| Button 9-slices | 64×64 | One white sprite tinted per style |
| Icons | 64×64 each, ~30 | §4.6; white on transparent |
| Fleet card pictures | 512×512, 3 | Merchantman, two Brigs, three Sloops (silhouettes are fine) |
| Shipyard card pictures | 256×256, 3 | Can reuse the above, cropped |
| End screen illustrations | 1280×480, 2 | Win and lose |
| Fonts | 2 TTF/OTF | OFL-licensed, §4.1 |

### 6.2 Open questions for the owner

1. **Scope check:** is everything not marked *(optional)* the right amount for the slice, or should some of it move to *(optional)*? The biggest items are the route drawer's "brings into reach", the destination comparison and the route bend points.
2. **Map positions:** should the 12 port markers and route lines be placed by hand in the scene on top of the art (the usual way, and it lets routes bend around land), with `ports.csv` `map_x`/`map_y` dropped, or kept only as a check? Or should the CSV stay the source and the art be drawn to fit it?
3. **Fonts:** happy with an OFL period serif for headings (e.g. IM Fell English SC) and a clean serif or sans with tabular figures for body text (e.g. Alegreya Sans)? Any fonts you already own?
4. **Icons and placeholder art:** draw simple placeholders in-house, or use a free set (for example game-icons.net, which is CC BY 3.0 and needs a credit line)?
5. **Risk bands:** are Low below 5%, Moderate 5 to 20%, High above 20% (and for loss of the ship: 2% and 10%) the right cut-offs?
6. **Market tags:** is showing CHEAP and PAYS on the market rows too much help? It makes the core trading loop easier to read; without it the player has to remember prices across 12 ports.
7. **Port panel and time:** the clock still stops while the port panel is open (the existing open item). This spec makes the hold visible ("Paused while you trade"); does that settle it, or should time keep running?
8. **Quit and main menu:** does the slice need a Quit button and a main menu, or is the pause menu (Resume, Replay tutorial, Restart) enough?
9. **End-screen stats:** OK to add small counters to `GameState` (voyages completed, ships lost, best trade) for the end screen? It's the only change this spec suggests in `Naval.Core`.
