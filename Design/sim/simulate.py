#!/usr/bin/env python3
"""Balance simulator for the Naval vertical slice.

Reads the CSV files in Design/data, plays many 60-day sessions with simple
bot players, and checks the results against the targets in
Design/VerticalSlice.md. Standard library only.

    python3 Design/sim/simulate.py              # 500 sessions per bot
    python3 Design/sim/simulate.py --runs 2000  # more sessions, steadier numbers
    python3 Design/sim/simulate.py --seed 7     # different random sequence
"""
import argparse
import csv
import math
import os
import random
import statistics
from collections import Counter

DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "data")


# ---------------------------------------------------------------- data ----

def read_csv(name):
    with open(os.path.join(DATA, name), newline="") as f:
        return list(csv.DictReader(f))


def load():
    goods = {r["good"]: float(r["base_price"]) for r in read_csv("goods.csv")}
    market = {}
    for r in read_csv("market.csv"):
        market[r["port"]] = {g: float(r[g]) for g in goods}
    routes = {}
    for r in read_csv("routes.csv"):
        info = {"distance": float(r["distance"]),
                "storm": float(r["storm_per_day"]),
                "pirates": float(r["pirates_per_day"])}
        routes[(r["from"], r["to"])] = info
        routes[(r["to"], r["from"])] = info
    ships = {r["ship"]: {"price": float(r["price"]), "capacity": int(r["capacity"]),
                         "speed": float(r["speed"]), "hull": int(r["hull"]),
                         "wage": float(r["wage_per_day"])}
             for r in read_csv("ships.csv")}
    rules = {}
    for r in read_csv("rules.csv"):
        v = r["value"]
        try:
            rules[r["key"]] = float(v)
        except ValueError:
            rules[r["key"]] = v
    ports = list(market)
    for a in ports:
        for b in ports:
            if a != b and (a, b) not in routes:
                raise SystemExit(f"routes.csv has no route between {a} and {b}")
    return goods, market, routes, ships, rules


# ---------------------------------------------------------------- game ----

class Ship:
    def __init__(self, kind, spec, port):
        self.kind, self.spec = kind, spec
        self.port = port            # current port, or destination while sailing
        self.hull = spec["hull"]
        self.cargo = Counter()
        self.arrive_day = 0         # day the ship is next free to act
        self.origin = None


class Game:
    def __init__(self, data, rng):
        self.goods, self.market, self.routes, self.ships_spec, self.r = data
        self.rng = rng
        self.day = 0
        self.gold = self.r["start_gold"]
        self.pressure = {p: {g: 0.0 for g in self.goods} for p in self.market}
        self.inbound = {p: Counter() for p in self.market}
        self.fleet = [Ship(self.r["start_ship"], self.ships_spec[self.r["start_ship"]],
                           self.r["start_port"])]
        self.win_day = None
        self.sunk = 0
        self.events = Counter()
        self.trips = Counter()
        self.bought = Counter()

    # --- prices
    def mid(self, port, good, extra=0.0):
        p = min(self.r["pressure_max"], max(self.r["pressure_min"],
                                            self.pressure[port][good] + extra))
        return self.goods[good] * self.market[port][good] * (1 + p)

    def buy_price(self, port, good, extra=0.0):
        return self.mid(port, good, extra) * (1 + self.r["spread"] / 2)

    def sell_price(self, port, good, extra=0.0):
        return self.mid(port, good, extra) * (1 - self.r["spread"] / 2)

    def push(self, port, good, amount):
        p = self.pressure[port]
        p[good] = min(self.r["pressure_max"], max(self.r["pressure_min"], p[good] + amount))

    def buy(self, ship, good):
        price = self.buy_price(ship.port, good)
        self.gold -= price
        ship.cargo[good] += 1
        self.push(ship.port, good, self.r["pressure_per_crate_bought"])

    def sell_all(self, ship):
        for good, n in list(ship.cargo.items()):
            for _ in range(n):
                self.gold += self.sell_price(ship.port, good)
                self.push(ship.port, good, -self.r["pressure_per_crate_sold"])
        ship.cargo.clear()

    def repair(self, ship, below=1.0):
        missing = ship.spec["hull"] - ship.hull
        if missing <= 0 or ship.hull > ship.spec["hull"] * below:
            return
        afford = int(max(0, self.gold) // self.r["repair_cost_per_hull"])
        fix = min(missing, afford)
        ship.hull += fix
        self.gold -= fix * self.r["repair_cost_per_hull"]

    def voyage_days(self, ship, dest):
        return math.ceil(self.routes[(ship.port, dest)]["distance"] / ship.spec["speed"])

    def sail(self, ship, dest):
        route = self.routes[(ship.port, dest)]
        days = self.voyage_days(ship, dest)
        self.trips[tuple(sorted((ship.port, dest)))] += 1
        for _ in range(days):
            if self.rng.random() < route["storm"]:
                self.events["storm"] += 1
                days += self.rng.randint(int(self.r["storm_delay_min"]), int(self.r["storm_delay_max"]))
                ship.hull -= self.rng.randint(int(self.r["storm_damage_min"]), int(self.r["storm_damage_max"]))
            if self.rng.random() < route["pirates"]:
                self.events["pirates"] += 1
                loss = self.rng.uniform(self.r["pirate_loss_min"], self.r["pirate_loss_max"])
                for g in ship.cargo:
                    ship.cargo[g] -= round(ship.cargo[g] * loss)
                ship.hull -= self.rng.randint(int(self.r["pirate_damage_min"]), int(self.r["pirate_damage_max"]))
            if ship.hull <= 0:
                break
        for g, n in ship.cargo.items():
            self.inbound[dest][g] += n
        ship.origin, ship.port, ship.arrive_day = ship.port, dest, self.day + days

    def arrive(self, ship):
        for g, n in ship.cargo.items():
            self.inbound[ship.port][g] -= n
        if ship.hull <= 0:
            self.sunk += 1
            self.events["sunk"] += 1
            self.fleet.remove(ship)
            return False
        self.sell_all(ship)
        return True

    def end_of_day(self):
        self.gold -= sum(s.spec["wage"] for s in self.fleet)
        keep = 1 - self.r["pressure_recovery_per_day"]
        noise = self.r["market_noise_per_day"]
        for port in self.pressure:
            for g in self.pressure[port]:
                self.pressure[port][g] = self.pressure[port][g] * keep + self.rng.gauss(0, noise)
        if self.win_day is None and self.gold >= self.r["goal_gold"]:
            self.win_day = self.day

    def bankrupt(self):
        cheapest = min(s["price"] for s in self.ships_spec.values())
        return not self.fleet and self.gold < cheapest

    def run(self, bot):
        limit = int(self.r["day_limit"])
        for self.day in range(1, limit + 1):
            for ship in [s for s in self.fleet if s.arrive_day <= self.day]:
                if ship.arrive_day == self.day and ship.origin is not None:
                    if not self.arrive(ship):
                        continue
                    ship.origin = None
                if self.gold >= self.r["goal_gold"]:
                    continue
                bot.act(self, ship)
            self.end_of_day()
            if self.win_day is not None or self.bankrupt():
                break
        return self


# ---------------------------------------------------------------- bots ----

def plan_cargo(game, ship, dest, days):
    """Pick crates one at a time by best expected margin. Returns (plan, expected profit)."""
    route = game.routes[(ship.port, dest)]
    p_pirates = 1 - (1 - route["pirates"]) ** days
    keep_frac = 1 - p_pirates * (game.r["pirate_loss_min"] + game.r["pirate_loss_max"]) / 2
    decay = (1 - game.r["pressure_recovery_per_day"]) ** days
    bought, sold = Counter(), Counter()
    budget = game.gold - 200          # keep a little for wages and repairs
    plan, profit = Counter(), 0.0
    for _ in range(ship.spec["capacity"]):
        best, best_margin, best_cost = None, 2.0, 0.0
        for g in game.goods:
            cost = game.buy_price(ship.port, g, bought[g] * game.r["pressure_per_crate_bought"])
            expected_pressure = (game.pressure[dest][g] * decay
                                 - (game.inbound[dest][g] + sold[g]) * game.r["pressure_per_crate_sold"])
            value = game.sell_price(dest, g, expected_pressure - game.pressure[dest][g])
            margin = value * keep_frac - cost
            if margin > best_margin and cost <= budget:
                best, best_margin, best_cost = g, margin, cost
        if best is None:
            break
        plan[best] += 1
        bought[best] += 1
        sold[best] += 1
        budget -= best_cost
        profit += best_margin
    expected_damage = days * (route["storm"] * (game.r["storm_damage_min"] + game.r["storm_damage_max"]) / 2
                              + route["pirates"] * (game.r["pirate_damage_min"] + game.r["pirate_damage_max"]) / 2)
    profit -= days * ship.spec["wage"] + expected_damage * game.r["repair_cost_per_hull"]
    return plan, profit


def execute(game, ship, dest, plan):
    for g, n in plan.items():
        for _ in range(n):
            game.buy(ship, g)
    game.sail(ship, dest)


class Greedy:
    """Picks the most profitable trip per day from where the ship is. Optionally grows the fleet."""
    def __init__(self, expand_with=None, allowed=None):
        self.expand_with = expand_with    # ship type to buy, or None for no fleet growth
        self.allowed = allowed            # restrict to these port pairs (fixed-route bot)

    def act(self, game, ship):
        game.repair(ship, below=0.6)
        if self.expand_with and game.day <= game.r["day_limit"] - 20:
            spec = game.ships_spec[self.expand_with]
            # Keep enough gold back to fill the new ship and the ones already sailing.
            reserve = 50 * (spec["capacity"] + sum(s.spec["capacity"] for s in game.fleet)) + 300
            if game.gold >= spec["price"] + reserve:
                game.gold -= spec["price"]
                game.bought[self.expand_with] += 1
                new = Ship(self.expand_with, spec, ship.port)
                new.arrive_day = game.day
                game.fleet.append(new)
                self.act(game, new)
        best = None
        for dest in game.market:
            if dest == ship.port or (self.allowed and tuple(sorted((ship.port, dest))) not in self.allowed):
                continue
            days = game.voyage_days(ship, dest)
            plan, profit = plan_cargo(game, ship, dest, days)
            score = profit / days
            if best is None or score > best[0]:
                best = (score, dest, plan)
        if best and best[0] > 0:
            execute(game, ship, best[1], best[2])
        elif self.allowed and best:
            execute(game, ship, best[1], Counter())    # head back along the route empty
        else:
            ship.arrive_day = game.day + 1             # wait a day for prices to recover


class FixedRoute(Greedy):
    """Finds the best-looking pair of ports on day 1 and puts every ship on it, adding sloops as it can."""
    def __init__(self, expand_with="Sloop"):
        super().__init__(expand_with=expand_with)

    def act(self, game, ship):
        if self.allowed is None:
            best = None
            for (a, b) in game.routes:
                if a < b:
                    days = game.routes[(a, b)]["distance"]
                    margin = sum(max(game.goods[g] * (game.market[y][g] - game.market[x][g]) for g in game.goods)
                                 for x, y in ((a, b), (b, a)))
                    if best is None or margin / days > best[0]:
                        best = (margin / days, (a, b))
            self.allowed = {best[1]}
            self.pair = best[1]
        if ship.port not in self.pair:
            game.sail(ship, self.pair[0])
            return
        super().act(game, ship)


class RandomPlayer:
    """Sails to a random port with random cargo. A stand-in for someone ignoring prices."""
    def act(self, game, ship):
        game.repair(ship, below=0.5)
        dest = game.rng.choice([p for p in game.market if p != ship.port])
        for _ in range(ship.spec["capacity"]):
            g = game.rng.choice(list(game.goods))
            if game.buy_price(ship.port, g) <= game.gold - 200:
                game.buy(ship, g)
        game.sail(ship, dest)


# -------------------------------------------------------------- report ----

def pct(xs, q):
    xs = sorted(xs)
    return xs[min(len(xs) - 1, int(q * len(xs)))] if xs else float("nan")


def simulate(data, make_bot, runs, seed):
    games = []
    for i in range(runs):
        g = Game(data, random.Random(seed * 100003 + i))
        games.append(g.run(make_bot()))
    return games


def summarize(name, games, goal):
    wins = [g.win_day for g in games if g.win_day is not None]
    final = [g.gold for g in games]
    trips = Counter()
    for g in games:
        trips.update(g.trips)
    total_trips = sum(trips.values()) or 1
    top_route, top_n = trips.most_common(1)[0] if trips else (("-", "-"), 0)
    voyages = total_trips / len(games)
    return {
        "bot": name,
        "win %": 100 * len(wins) / len(games),
        "median win day": statistics.median(wins) if wins else None,
        "median gold": statistics.median(final),
        "p10 gold": pct(final, 0.10),
        "p90 gold": pct(final, 0.90),
        "voyages": voyages,
        "sinkings/session": sum(g.sunk for g in games) / len(games),
        "top route share %": 100 * top_n / total_trips,
        "top route": " - ".join(top_route),
        "bankrupt %": 100 * sum(1 for g in games if g.bankrupt()) / len(games),
    }


def static_margins(data):
    goods, market, routes, ships, rules = data
    rows = []
    for (a, b), info in routes.items():
        for g in goods:
            m = goods[g] * (market[b][g] - market[a][g]) - goods[g] * (market[a][g] + market[b][g]) / 2 * rules["spread"]
            if m > 0:
                rows.append((m / info["distance"], m, g, a, b, info["distance"]))
    rows.sort(reverse=True)
    return rows


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--runs", type=int, default=500)
    ap.add_argument("--seed", type=int, default=1)
    args = ap.parse_args()
    data = load()
    rules = data[4]

    print("Best single-crate margins before any trading (gold per crate, per distance-day):")
    for per_day, m, g, a, b, d in static_margins(data)[:8]:
        print(f"  {g:8} {a:>15} -> {b:<15} +{m:5.0f}/crate  ({per_day:4.1f} per distance-day)")
    print()

    bots = [
        ("Random", RandomPlayer),
        ("Fixed route", FixedRoute),
        ("Greedy, 1 ship", lambda: Greedy()),
    ] + [(f"Greedy + {k}s", (lambda k=k: lambda: Greedy(expand_with=k))()) for k in data[3]]

    results = [summarize(n, simulate(data, b, args.runs, args.seed), rules["goal_gold"]) for n, b in bots]
    cols = ["bot", "win %", "median win day", "median gold", "p10 gold", "p90 gold",
            "voyages", "sinkings/session", "top route share %", "bankrupt %"]
    widths = [max(len(c), 15) for c in cols]
    print("  ".join(c.rjust(w) for c, w in zip(cols, widths)))
    for r in results:
        cells = []
        for c, w in zip(cols, widths):
            v = r[c]
            cells.append((f"{v:,.1f}" if isinstance(v, float) else str("-" if v is None else v)).rjust(w))
        print("  ".join(cells))
    print()
    print("Most-used route per bot:")
    for r in results:
        print(f"  {r['bot']:15} {r['top route']}  ({r['top route share %']:.0f}% of voyages)")
    print()
    check(results, rules)


def check(results, rules):
    by = {r["bot"]: r for r in results}
    fleet = [r for r in results if r["bot"].startswith("Greedy + ")]
    best_fleet = max(fleet, key=lambda r: r["win %"])
    checks = [
        ("A good player usually wins (best fleet bot wins >= 85%)",
         best_fleet["win %"] >= 85),
        ("...but not too early (its median win day is between 40 and 55)",
         best_fleet["median win day"] is not None and 40 <= best_fleet["median win day"] <= 55),
        ("Growing the fleet matters (1-ship greedy wins <= 50%)",
         by["Greedy, 1 ship"]["win %"] <= 50),
        ("Spamming one route fails (fixed-route bot wins <= 20%)",
         by["Fixed route"]["win %"] <= 20),
        ("Ignoring prices fails (random bot wins <= 2%)",
         by["Random"]["win %"] <= 2),
        ("No single route dominates (best fleet bot's top route <= 35% of voyages)",
         best_fleet["top route share %"] <= 35),
        ("Every ship type is viable (each fleet bot within 25 points of the best win %)",
         all(best_fleet["win %"] - r["win %"] <= 25 for r in fleet)),
        ("Losing a ship is rare for careful players (<= 0.3 sinkings per session)",
         best_fleet["sinkings/session"] <= 0.3),
    ]
    print("Balance targets:")
    for text, ok in checks:
        print(f"  [{'PASS' if ok else 'FAIL'}] {text}")


if __name__ == "__main__":
    main()
