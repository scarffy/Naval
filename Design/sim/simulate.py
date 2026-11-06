#!/usr/bin/env python3
"""Balance simulator for the Naval vertical slice.

Reads the CSV files in Design/data, plays many sessions with simple bot
players, and checks the results against the balance targets listed in
Design/VerticalSlice.md. Standard library only.

    python3 Design/sim/simulate.py              # 200 sessions per bot
    python3 Design/sim/simulate.py --runs 1000  # steadier numbers
    python3 Design/sim/simulate.py --seed 7     # a different random sequence
"""
import argparse
import csv
import heapq
import math
import os
import random
import statistics
from collections import Counter, defaultdict

DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "data")
TRACK_ORDER = ["Hold", "Sails", "Hull", "Guns"]


# ---------------------------------------------------------------- data ----

def read_csv(name):
    with open(os.path.join(DATA, name), newline="") as f:
        return list(csv.DictReader(f))


def as_value(v):
    try:
        return float(v)
    except ValueError:
        return v


class Data:
    def __init__(self, mode="veteran"):
        self.mode = mode
        self.goods = {r["good"]: float(r["base_price"]) for r in read_csv("goods.csv")}
        self.ports = {r["port"]: {"region": r["region"], "shipyard": r["shipyard"].strip().lower() == "yes"}
                      for r in read_csv("ports.csv")}
        self.market = {}
        for r in read_csv("market.csv"):
            self.market[r["port"]] = {g: float(r[g]) for g in self.goods if (r.get(g) or "").strip()}
        self.ships = {r["ship"]: {"price": float(r["price"]), "capacity": int(r["capacity"]),
                                  "speed": float(r["speed"]), "hull": int(r["hull"]),
                                  "wage": float(r["wage_per_day"]),
                                  "ocean": r.get("ocean_going", "yes").strip().lower() == "yes"}
                      for r in read_csv("ships.csv")}
        self.upgrades = defaultdict(list)
        for r in sorted(read_csv("upgrades.csv"), key=lambda r: (r["track"], int(r["level"]))):
            self.upgrades[r["track"]].append({k: as_value(v) for k, v in r.items()})
        self.rules = {r["key"]: as_value(r["value"]) for r in read_csv("rules.csv")}
        for r in read_csv("modes.csv"):
            if r["mode"].strip().startswith("#") or r["mode"].strip() != mode:
                continue
            if r["key"] not in self.rules:
                raise SystemExit(f"modes.csv overrides unknown rule {r['key']}")
            self.rules[r["key"]] = as_value(r["value"])
        self.lanes = [{"id": i, "a": r["from"], "b": r["to"], "distance": float(r["distance"]),
                       "storm": float(r["storm_per_day"]), "pirates": float(r["pirates_per_day"]),
                       "hurricanes": float(r["hurricanes_per_day"]), "price": float(r["price"]),
                       "ocean": r.get("ocean", "no").strip().lower() == "yes"}
                      for i, r in enumerate(read_csv("lanes.csv"))]
        self.fixed_market = (self.rules["pressure_per_crate_bought"] == 0
                             and self.rules["pressure_per_crate_sold"] == 0)
        if set(self.market) != set(self.ports):
            raise SystemExit("market.csv and ports.csv list different ports")
        self.routes = self._routes(self.lanes)            # every route open: used for reports
        self._route_cache, self._score_cache = {}, {}
        # Best single-crate margin from each port to each other port, before any trading.
        self.pair_margin = {}
        for a in self.market:
            for b in self.market:
                if a != b:
                    self.pair_margin[(a, b)] = max(
                        [self.goods[g] * (self.market[b][g] - self.market[a][g] * (1 + self.rules["spread"]))
                         for g in self.market[a] if g in self.market[b]] + [0.0])

    def routes_for(self, owned, ocean=True):
        """Shortest paths using only the routes the player owns (a frozenset of lane ids).
        Coastal ships (ocean=False) can't use ocean crossings."""
        key = (owned, ocean)
        if key not in self._route_cache:
            lanes = [self.lanes[i] for i in owned if ocean or not self.lanes[i]["ocean"]]
            self._route_cache[key] = self._routes(lanes, require_all=False)
        return self._route_cache[key]

    def trade_rates(self, owned, ocean=True):
        """Best gold per crate per day (at speed 1) for every trip the routes allow, best first."""
        key = (owned, ocean)
        if key not in self._score_cache:
            routes = self.routes_for(owned, ocean)
            home = self.rules["start_port"]
            self._score_cache[key] = sorted(
                (self.pair_margin[(a, b)] / (route["distance"] + self.rules["port_time_days"])
                 for (a, b), route in routes.items() if a == home or (home, a) in routes), reverse=True)
        return self._score_cache[key]

    def network_rate(self, owned, ships, ocean=True):
        """Average of the best 2-per-ship trades: roughly what a fleet of this size earns per crate per day."""
        k = max(2, 2 * ships)
        if self.fixed_market:
            k = 1          # nothing saturates: every ship can run the single best trade
        return sum(self.trade_rates(owned, ocean)[:k]) / k

    def _routes(self, lanes, require_all=True):
        """Shortest sea path between every pair of ports the lanes connect, as a list of lanes."""
        adj = defaultdict(list)
        for lane in lanes:
            for x, y in ((lane["a"], lane["b"]), (lane["b"], lane["a"])):
                if x not in self.ports or y not in self.ports:
                    raise SystemExit(f"lanes.csv mentions unknown port {x!r}")
                adj[x].append((y, lane))
        routes = {}
        sources = list(self.ports) if require_all else sorted({p for l in lanes for p in (l["a"], l["b"])})
        for src in sources:
            dist, prev = {src: 0.0}, {}
            heap = [(0.0, src)]
            while heap:
                d, u = heapq.heappop(heap)
                if d > dist[u]:
                    continue
                for v, lane in adj[u]:
                    if d + lane["distance"] < dist.get(v, math.inf):
                        dist[v], prev[v] = d + lane["distance"], (u, lane)
                        heapq.heappush(heap, (dist[v], v))
            for dst in self.ports:
                if dst == src:
                    continue
                if dst not in dist:
                    if require_all:
                        raise SystemExit(f"No sea path from {src} to {dst}")
                    continue
                legs, node = [], dst
                while node != src:
                    node, lane = prev[node]
                    legs.append(lane)
                legs.reverse()
                total = dist[dst]
                avg = {k: sum(l[k] * l["distance"] for l in legs) / total
                       for k in ("storm", "pirates", "hurricanes")}
                europe = {self.ports[src]["region"] == "Europe", self.ports[dst]["region"] == "Europe"}
                routes[(src, dst)] = {"distance": total, "legs": legs, "atlantic": len(europe) == 2, **avg}
        return routes


# ---------------------------------------------------------------- game ----

class Ship:
    def __init__(self, data, kind, port):
        self.data, self.kind, self.base = data, kind, data.ships[kind]
        self.levels = Counter()          # upgrade track -> level bought
        self.invested = self.base["price"]
        self.port = port                 # current port, or destination while sailing
        self.cargo = Counter()
        self.free_at = 0.0               # time (in days) the ship can act again
        self.origin = None               # set while sailing
        self.hull = self.max_hull()

    def _mult(self, field):
        m = 1.0
        for track, level in self.levels.items():
            if level:
                m *= self.data.upgrades[track][level - 1][field]
        return m

    def speed(self):
        return self.base["speed"] * self._mult("speed")

    def capacity(self):
        return int(round(self.base["capacity"] * self._mult("capacity")))

    def max_hull(self):
        return int(round(self.base["hull"] * self._mult("hull")))

    def wage(self):
        return self.base["wage"] * self._mult("wage")

    def repair_mult(self):
        return self._mult("repair_cost")

    def repel(self):
        return max([self.data.upgrades[t][l - 1]["pirate_repel"] for t, l in self.levels.items() if l] or [0.0])


def frac_at_least(hull, lo, hi):
    """Chance that damage drawn uniformly from lo..hi is at least `hull`."""
    return min(1.0, max(0.0, (hi - hull + 1) / (hi - lo + 1)))


class Game:
    def __init__(self, data, rng):
        self.d, self.r, self.rng = data, data.rules, rng
        self.day = 0
        self.now = 0.0                   # exact time of the decision being made
        self.gold = self.r["start_gold"]
        self.pressure = {p: defaultdict(float) for p in data.market}   # pushed by the player's trading
        self.trend = {p: defaultdict(float) for p in data.market}      # shortages and gluts
        self.inbound = {p: Counter() for p in data.market}
        self.fleet = [Ship(data, self.r["start_ship"], self.r["start_port"])]
        self.win_day = None
        self.sunk = 0
        self.events = Counter()
        self.trips = Counter()
        self.arrivals = Counter()
        self.atlantic_trips = 0
        self.sea_days = Counter()        # (period, on_hurricane_lane) -> ship-days at sea
        self.ships_bought = Counter()
        self.upgrades_bought = Counter()
        self.history = []                # gold at the end of each day
        self.owned = frozenset()         # ids of the routes the player has opened
        self.route_tables = {True: {}, False: {}}   # shortest paths over owned routes: ocean-going / coastal
        self.free_routes = int(self.r["free_routes"])
        self.route_log = []              # (day, lane) for each route opened

    # --- prices
    def price(self, port, good, pressure):
        p = min(self.r["pressure_max"], max(self.r["pressure_min"], pressure))
        return self.d.goods[good] * self.d.market[port][good] * (1 + p)

    def total_pressure(self, port, good):
        return self.pressure[port][good] + self.trend[port][good]

    def buy_price(self, port, good, extra=0.0):
        return self.price(port, good, self.total_pressure(port, good) + extra) * (1 + self.r["spread"] / 2)

    def sell_price(self, port, good, pressure):
        return self.price(port, good, pressure) * (1 - self.r["spread"] / 2)

    def push(self, port, good, amount):
        p = self.pressure[port]
        p[good] = min(self.r["pressure_max"], max(self.r["pressure_min"], p[good] + amount))

    def buy(self, ship, good):
        self.gold -= self.buy_price(ship.port, good)
        ship.cargo[good] += 1
        self.push(ship.port, good, self.r["pressure_per_crate_bought"])

    def sell_all(self, ship):
        for good, n in list(ship.cargo.items()):
            if good not in self.d.market[ship.port]:
                continue                                  # not traded here: keep it aboard
            for _ in range(n):
                self.gold += self.sell_price(ship.port, good, self.total_pressure(ship.port, good))
                self.push(ship.port, good, -self.r["pressure_per_crate_sold"])
            del ship.cargo[good]

    # --- ships
    def repair(self, ship, below=1.0):
        missing = ship.max_hull() - ship.hull
        if missing <= 0 or ship.hull > ship.max_hull() * below:
            return
        cost = self.r["repair_cost_per_hull"] * ship.repair_mult()
        fix = min(missing, int(max(0, self.gold) // cost))
        ship.hull += fix
        self.gold -= fix * cost

    def buy_ship(self, kind, port):
        spec = self.d.ships[kind]
        self.gold -= spec["price"]
        self.ships_bought[kind] += 1
        ship = Ship(self.d, kind, port)
        ship.free_at = self.now
        self.fleet.append(ship)
        return ship

    def upgrade_cost(self, ship, track):
        level = ship.levels[track]
        if level >= len(self.d.upgrades[track]):
            return None
        return self.d.upgrades[track][level]["cost_fraction"] * ship.base["price"]

    def buy_upgrade(self, ship, track):
        cost = self.upgrade_cost(ship, track)
        before = ship.max_hull()
        ship.levels[track] += 1
        ship.hull += ship.max_hull() - before
        ship.invested += cost
        self.gold -= cost
        self.upgrades_bought[f"{track} {ship.levels[track]}"] += 1

    # --- routes
    def routes(self, ship):
        return self.route_tables[ship.base["ocean"]]

    def network(self):
        ports = {self.r["start_port"]}
        for i in self.owned:
            ports |= {self.d.lanes[i]["a"], self.d.lanes[i]["b"]}
        return ports

    def route_candidates(self):
        """Routes that can be opened next: not owned yet and touching the network."""
        net = self.network()
        return [l for l in self.d.lanes if l["id"] not in self.owned and (l["a"] in net or l["b"] in net)]

    def route_price(self, lane):
        return 0.0 if self.free_routes > 0 else lane["price"]

    def buy_route(self, lane):
        self.gold -= self.route_price(lane)
        if self.free_routes > 0:
            self.free_routes -= 1
        self.owned = self.owned | {lane["id"]}
        self.route_tables = {ocean: self.d.routes_for(self.owned, ocean) for ocean in (True, False)}
        self.route_log.append((self.day, lane))

    # --- voyages
    def season(self, day):
        start, peak = self.r["hurricane_season_start"], self.r["hurricane_season_peak"]
        if day < start:
            return 0.0
        return min(1.0, (day - start) / max(1.0, peak - start))

    def voyage_days(self, ship, dest):
        return self.routes(ship)[(ship.port, dest)]["distance"] / ship.speed()

    def roll(self, lo, hi):
        return self.rng.randint(int(self.r[lo]), int(self.r[hi]))

    def lose_cargo(self, ship, lo, hi):
        frac = self.rng.uniform(self.r[lo], self.r[hi])
        for g in ship.cargo:
            ship.cargo[g] -= round(ship.cargo[g] * frac)

    def sail(self, ship, dest):
        route = self.routes(ship)[(ship.port, dest)]
        days = self.voyage_days(ship, dest)
        self.trips[tuple(sorted((ship.port, dest)))] += 1
        self.atlantic_trips += route["atlantic"]
        bounds, total = [], 0.0
        for lane in route["legs"]:
            total += lane["distance"]
            bounds.append((total, lane))
        delay = 0
        for i in range(math.ceil(days)):
            share = min(1.0, days - i)           # the last day at sea may be partial
            pos = min(i + 0.5, days) / days * route["distance"]
            lane = next(l for b, l in bounds if pos <= b)
            sea_day = int(self.now) + i
            period = "early" if sea_day <= self.r["hurricane_season_start"] else (
                "late" if sea_day > self.r["hurricane_season_peak"] else "mid")
            self.sea_days[(period, lane["hurricanes"] > 0.025)] += share
            if self.rng.random() < lane["storm"] * share:
                self.events["storm"] += 1
                delay += self.roll("storm_delay_min", "storm_delay_max")
                ship.hull -= self.roll("storm_damage_min", "storm_damage_max")
            if self.rng.random() < lane["pirates"] * share:
                if self.rng.random() < ship.repel():
                    self.events["pirates driven off"] += 1
                    ship.hull -= self.roll("pirate_damage_min", "pirate_damage_max") // 2
                else:
                    self.events["pirates"] += 1
                    self.lose_cargo(ship, "pirate_loss_min", "pirate_loss_max")
                    ship.hull -= self.roll("pirate_damage_min", "pirate_damage_max")
            if self.rng.random() < lane["hurricanes"] * self.season(sea_day) * share:
                self.events["hurricane"] += 1
                delay += self.roll("hurricane_delay_min", "hurricane_delay_max")
                self.lose_cargo(ship, "hurricane_loss_min", "hurricane_loss_max")
                ship.hull -= self.roll("hurricane_damage_min", "hurricane_damage_max")
            if ship.hull <= 0:
                break
        for g, n in ship.cargo.items():
            self.inbound[dest][g] += n
        ship.origin, ship.port = ship.port, dest
        ship.free_at = self.now + days + delay + self.r["port_time_days"]

    def arrive(self, ship):
        for g, n in ship.cargo.items():
            self.inbound[ship.port][g] -= n
        ship.origin = None
        if ship.hull <= 0:
            self.sunk += 1
            self.fleet.remove(ship)
            return False
        self.arrivals[ship.port] += 1
        self.gold -= self.r["port_fee"]
        self.sell_all(ship)
        return True

    def end_of_day(self):
        self.gold -= sum(s.wage() for s in self.fleet)
        keep = 1 - self.r["pressure_recovery_per_day"]
        trend_keep = 1 - self.r["market_event_recovery_per_day"]
        noise = self.r["market_noise_per_day"]
        for port in self.d.market:
            for g in self.d.market[port]:
                self.pressure[port][g] = self.pressure[port][g] * keep + self.rng.gauss(0, noise)
                self.trend[port][g] *= trend_keep
        if self.rng.random() < self.r["market_event_chance_per_day"]:
            port = self.rng.choice(list(self.d.market))
            good = self.rng.choice(list(self.d.market[port]))
            self.trend[port][good] += self.rng.choice((-1, 1)) * self.r["market_event_size"]
            self.events["market news"] += 1
        self.history.append(self.gold)
        if self.win_day is None and self.gold >= self.r["goal_gold"]:
            self.win_day = self.day

    def bankrupt(self):
        return not self.fleet and self.gold < min(s["price"] for s in self.d.ships.values())

    def run(self, bot):
        for self.day in range(1, int(self.r["day_limit"]) + 1):
            for ship in sorted((s for s in self.fleet if s.free_at <= self.day), key=lambda s: s.free_at):
                self.now = max(ship.free_at, self.day - 1)
                if ship.origin is not None and not self.arrive(ship):
                    continue
                bot.act(self, ship)
            self.end_of_day()
            if self.bankrupt():
                break
        return self


# ---------------------------------------------------------------- bots ----

def plan_cargo(game, ship, dest, days):
    """Choose crates one at a time by best expected margin. Returns (plan, expected profit)."""
    r = game.r
    route = game.routes(ship)[(ship.port, dest)]
    here, there = game.d.market[ship.port], game.d.market[dest]
    season = game.season(int(game.now + days / 2))
    p_robbed = 1 - (1 - route["pirates"] * (1 - ship.repel())) ** days
    p_hurricane = 1 - (1 - route["hurricanes"] * season) ** days
    keep = ((1 - p_robbed * (r["pirate_loss_min"] + r["pirate_loss_max"]) / 2)
            * (1 - p_hurricane * (r["hurricane_loss_min"] + r["hurricane_loss_max"]) / 2))
    decay = (1 - r["pressure_recovery_per_day"]) ** days
    trend_decay = (1 - r["market_event_recovery_per_day"]) ** days
    kb, ks = r["pressure_per_crate_bought"], r["pressure_per_crate_sold"]

    def margin(g, k):
        cost = game.buy_price(ship.port, g, k * kb)
        expected = (game.pressure[dest][g] * decay + game.trend[dest][g] * trend_decay
                    - (game.inbound[dest][g] + k) * ks)
        return game.sell_price(dest, g, expected) * keep - cost, cost

    heap = []
    for g in here:
        if g in there:
            m, c = margin(g, 0)
            heap.append((-m, g, 0, c))
    heapq.heapify(heap)
    budget = game.gold - 200                  # keep a little for wages and repairs
    room = ship.capacity() - sum(ship.cargo.values())
    plan, profit, spent = Counter(), 0.0, 0.0
    while heap and room > 0:
        neg, g, k, cost = heapq.heappop(heap)
        if -neg <= 2:
            break
        if cost > budget:
            continue
        plan[g] += 1
        room -= 1
        budget -= cost
        spent += cost
        profit -= neg
        m, c = margin(g, k + 1)
        heapq.heappush(heap, (-m, g, k + 1, c))

    mean = lambda lo, hi: (r[lo] + r[hi]) / 2
    damage = days * (route["storm"] * mean("storm_damage_min", "storm_damage_max")
                     + route["pirates"] * mean("pirate_damage_min", "pirate_damage_max")
                     + route["hurricanes"] * season * mean("hurricane_damage_min", "hurricane_damage_max"))
    profit -= ((days + r["port_time_days"]) * ship.wage() + r["port_fee"]
               + damage * r["repair_cost_per_hull"] * ship.repair_mult())
    p_sink = min(1.0, p_hurricane * frac_at_least(ship.hull, r["hurricane_damage_min"], r["hurricane_damage_max"])
                 + (1 - (1 - route["storm"]) ** days)
                 * frac_at_least(ship.hull, r["storm_damage_min"], r["storm_damage_max"]))
    profit -= p_sink * (ship.invested + spent + max(0.0, profit))
    return plan, profit


def reserve(game, extra_capacity=0):
    """Gold to keep back so every ship can still fill its hold."""
    return 50 * (extra_capacity + sum(s.base["capacity"] for s in game.fleet)) + 300


ROUTE_PAYBACK_DAYS = 12   # bots open a route if the extra income repays it within this many days


def best_new_route(game, owned=None, free=None):
    """Best next route to open, looking one purchase further ahead.
    Returns (lane to open now, extra gold per day for the fleet, price of the plan)."""
    owned = game.owned if owned is None else owned
    free = game.free_routes if free is None else free
    ships = len(game.fleet) or 1
    capacity = sum(s.capacity() * s.speed() for s in game.fleet) or 22.5
    ocean = any(s.base["ocean"] for s in game.fleet)
    base = game.d.network_rate(owned, ships, ocean)
    net = {game.r["start_port"]} | {p for i in owned for p in (game.d.lanes[i]["a"], game.d.lanes[i]["b"])}
    price = lambda lane, nth: 0.0 if nth < free else lane["price"]
    best = None
    for lane in game.d.lanes:
        if lane["id"] in owned or not (lane["a"] in net or lane["b"] in net):
            continue
        one = owned | {lane["id"]}
        plans = [(one, price(lane, 0))]
        net2 = net | {lane["a"], lane["b"]}
        for lane2 in game.d.lanes:
            if lane2["id"] not in one and (lane2["a"] in net2 or lane2["b"] in net2):
                plans.append((one | {lane2["id"]}, price(lane, 0) + price(lane2, 1)))
        for routes, cost in plans:
            gain = (game.d.network_rate(routes, ships, ocean) - base) * capacity
            if gain <= 0:
                continue
            value = gain / max(1.0, cost) if cost > 0 else gain * 1e6
            if best is None or value > best[0]:
                best = (value, lane, gain, cost)
    return (None, 0.0, 0.0) if best is None else best[1:]


MAX_FLEET = 30     # in a fixed market nothing else limits the fleet, so bots stop buying ships here


class Greedy:
    """Picks the most profitable trip per day from where the ship is.
    Takes its free routes, then buys more when they pay back quickly (unless expand_routes is off).
    Optionally grows a fleet of one ship type and buys upgrades at shipyards."""
    def __init__(self, expand_with=None, upgrades=(), allowed=None, expand_routes=True, opening=None):
        self.expand_with = expand_with
        self.upgrades = list(upgrades)
        self.allowed = allowed            # restrict to these port pairs (fixed-route bot)
        self.expand_routes = expand_routes
        self.opening = list(opening or [])   # lane ids to take as the free routes, in order
        self.checked_day = None

    def manage_routes(self, game):
        while game.free_routes > 0:
            lane = game.d.lanes[self.opening.pop(0)] if self.opening else best_new_route(game)[0]
            if lane is None:
                break
            game.buy_route(lane)
        if not self.expand_routes or self.checked_day == game.day or game.day > game.r["day_limit"] - 20:
            return
        self.checked_day = game.day
        while True:
            lane, gain, cost = best_new_route(game)
            if (lane is None or cost > gain * ROUTE_PAYBACK_DAYS
                    or game.gold < game.route_price(lane) + reserve(game)):
                return
            game.buy_route(lane)

    def shop(self, game, ship):
        if not game.d.ports[ship.port]["shipyard"] or game.day > game.r["day_limit"] - 25:
            return
        if self.expand_with and (len(game.fleet) < MAX_FLEET or not game.d.fixed_market):
            kinds = list(game.d.ships) if self.expand_with == "Mixed" else [self.expand_with]
            affordable = [k for k in kinds
                          if game.gold >= game.d.ships[k]["price"] + reserve(game, game.d.ships[k]["capacity"])]
            if affordable:
                biggest = max(affordable, key=lambda k: game.d.ships[k]["capacity"])
                new = game.buy_ship(biggest, ship.port)
                self.act(game, new)
        while self.upgrades:
            options = [(ship.levels[t], TRACK_ORDER.index(t), t) for t in self.upgrades
                       if game.upgrade_cost(ship, t) is not None]
            if not options:
                break
            _, _, track = min(options)
            if game.gold < game.upgrade_cost(ship, track) + reserve(game):
                break
            game.buy_upgrade(ship, track)

    def act(self, game, ship):
        self.manage_routes(game)
        game.repair(ship, below=0.8)
        self.shop(game, ship)
        best = None
        for dest in game.d.ports:
            if (ship.port, dest) not in game.routes(ship) or (
                    self.allowed and tuple(sorted((ship.port, dest))) not in self.allowed):
                continue
            days = game.voyage_days(ship, dest)
            plan, profit = plan_cargo(game, ship, dest, days)
            score = profit / (days + game.r["port_time_days"])
            if best is None or score > best[0]:
                best = (score, dest, plan)
        if best and best[0] > 0:
            for g, n in best[2].items():
                for _ in range(n):
                    game.buy(ship, g)
            game.sail(ship, best[1])
        elif self.allowed and best:
            game.sail(ship, best[1])             # head back along the route empty
        else:
            ship.free_at = game.now + 1          # wait a day for prices to move


class FixedRoute(Greedy):
    """Takes its free routes, picks the best-looking pair of ports (one with a shipyard) on day 1,
    and keeps every ship on it, adding sloops as it can. Never buys more routes."""
    def __init__(self):
        super().__init__(expand_with="Sloop", expand_routes=False)
        self.pair = None

    def act(self, game, ship):
        self.manage_routes(game)
        if self.pair is None:
            best = None
            for (a, b), route in game.routes(ship).items():
                if a > b or not (game.d.ports[a]["shipyard"] or game.d.ports[b]["shipyard"]):
                    continue
                margin = 0.0
                for x, y in ((a, b), (b, a)):
                    shared = [g for g in game.d.market[x] if g in game.d.market[y]]
                    margin += max([game.d.goods[g] * (game.d.market[y][g] - game.d.market[x][g]) for g in shared] or [0])
                if best is None or margin / route["distance"] > best[0]:
                    best = (margin / route["distance"], (a, b))
            self.pair = best[1]
            self.allowed = {self.pair}
        if ship.port not in self.pair:
            game.sail(ship, self.pair[0])
            return
        super().act(game, ship)


class RandomPlayer:
    """Opens random routes and sails to random ports with random cargo. A stand-in for someone ignoring prices."""
    def act(self, game, ship):
        while game.free_routes > 0 or (game.route_candidates() and game.gold > 500 + min(
                l["price"] for l in game.route_candidates())):
            options = [l for l in game.route_candidates() if game.route_price(l) <= game.gold - 500]
            if not options:
                break
            game.buy_route(game.rng.choice(options))
        game.repair(ship, below=0.5)
        reachable = [b for (a, b) in game.routes(ship) if a == ship.port]
        if not reachable:
            ship.free_at = game.now + 1
            return
        dest = game.rng.choice(reachable)
        shared = [g for g in game.d.market[ship.port] if g in game.d.market[dest]]
        for _ in range(ship.capacity()):
            if not shared:
                break
            g = game.rng.choice(shared)
            if game.buy_price(ship.port, g) <= game.gold - 200:
                game.buy(ship, g)
        game.sail(ship, dest)


# -------------------------------------------------------------- report ----

def rank_openings(data):
    """Every valid pair of free routes from the start port, best (for one sloop) first."""
    start = data.rules["start_port"]
    sloop = 1
    options = {}
    for a in data.lanes:
        if start not in (a["a"], a["b"]):
            continue
        for b in data.lanes:
            if b["id"] != a["id"] and ({b["a"], b["b"]} & {start, a["a"], a["b"]}):
                key = frozenset((a["id"], b["id"]))
                options[key] = (data.network_rate(key, sloop, data.ships[data.rules["start_ship"]]["ocean"]),
                                [a["id"], b["id"]])
    return sorted(options.values(), key=lambda o: -o[0])


def lane_name(data, i):
    return f"{data.lanes[i]['a']}-{data.lanes[i]['b']}"


def simulate(data, make_bot, runs, seed):
    return [Game(data, random.Random(seed * 100003 + i)).run(make_bot()) for i in range(runs)]


def summarize(name, games):
    wins = [g.win_day for g in games if g.win_day is not None]
    final = sorted(g.gold for g in games)
    trips, arrivals, sea = Counter(), Counter(), Counter()
    for g in games:
        trips.update(g.trips)
        arrivals.update(g.arrivals)
        sea.update(g.sea_days)
    total_trips = sum(trips.values()) or 1
    total_arrivals = sum(arrivals.values()) or 1
    top_route, top_n = trips.most_common(1)[0] if trips else (("-", "-"), 0)

    def hurricane_share(period):
        on, off = sea[(period, True)], sea[(period, False)]
        return on / (on + off) if on + off else 0.0

    upgrades = Counter()
    for g in games:
        upgrades.update(g.upgrades_bought)
    return {
        "bot": name,
        "win %": 100 * len(wins) / len(games),
        "win day": statistics.median(wins) if wins else None,
        "gold day 90": statistics.median(final),
        "mean gold": statistics.mean(final),
        "p10 gold": final[len(final) // 10],
        "voyages": total_trips / len(games),
        "sunk": sum(g.sunk for g in games) / len(games),
        "top route %": 100 * top_n / total_trips,
        "atlantic %": 100 * sum(g.atlantic_trips for g in games) / total_trips,
        "min port %": 100 * min(arrivals.get(p, 0) for p in games[0].d.ports) / total_arrivals,
        "quiet ports": sorted((100 * arrivals.get(p, 0) / total_arrivals, p) for p in games[0].d.ports)[:3],
        "top route": " - ".join(top_route),
        "hurricane lanes early": hurricane_share("early"),
        "hurricane lanes late": hurricane_share("late"),
        "upgrades": {k: v / len(games) for k, v in upgrades.items()},
        "gold by day": {d: statistics.median(g.history[min(d, len(g.history)) - 1] for g in games)
                        for d in (50, 60, 70, 80, 90)},
        "routes by day": {d: statistics.mean(sum(1 for day, _ in g.route_log if day <= d) for g in games)
                          for d in (1, 30, 60, 90)},
        "first routes": Counter(" + ".join(sorted(f"{l['a']}-{l['b']}" for _, l in g.route_log[:2]))
                                for g in games).most_common(3),
        "ships bought": {k: sum(g.ships_bought[k] for g in games) / len(games) for k in games[0].d.ships},
    }


def print_table(results):
    cols = ["bot", "win %", "win day", "gold day 90", "mean gold", "p10 gold", "voyages", "sunk",
            "top route %", "atlantic %", "min port %"]
    widths = [max(len(c), 9) if c != "bot" else 22 for c in cols]
    print("  ".join(c.rjust(w) for c, w in zip(cols, widths)))
    for r in results:
        cells = []
        for c, w in zip(cols, widths):
            v = r[c]
            text = "-" if v is None else (f"{v:,.1f}" if isinstance(v, float) else str(v))
            cells.append(text.rjust(w))
        print("  ".join(cells))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--runs", type=int, default=200)
    ap.add_argument("--seed", type=int, default=1)
    ap.add_argument("--mode", choices=["veteran", "beginner"], default="veteran")
    args = ap.parse_args()
    data = Data(args.mode)
    print(f"Mode: {args.mode}  (goal {data.rules['goal_gold']:,.0f} gold, {data.rules['day_limit']} days, "
          f"start gold {data.rules['start_gold']:,.0f})")
    print()
    run = lambda name, make: summarize(name, simulate(data, make, args.runs, args.seed))

    print("Strongest trades before any trading (gold per crate, and per day for a ship of speed 1):")
    trades = []
    for (a, b), route in data.routes.items():
        for g in data.market[a]:
            if g in data.market[b]:
                m = data.goods[g] * (data.market[b][g] - data.market[a][g] * (1 + data.rules["spread"]))
                if m > 0:
                    trades.append((m / (route["distance"] + data.rules["port_time_days"]), m, g, a, b))
    for per_day, m, g, a, b in sorted(trades, reverse=True)[:10]:
        print(f"  {g:8} {a:>15} -> {b:<15} +{m:4.0f}/crate  {per_day:5.1f}/day")
    print()

    results = [run("Random", RandomPlayer), run("Fixed route", FixedRoute), run("Greedy, 1 ship", Greedy)]
    fleet = [run(f"Fleet of {k}s", (lambda k=k: lambda: Greedy(expand_with=k))()) for k in data.ships]
    fleet.append(run("Mixed fleet", lambda: Greedy(expand_with="Mixed")))
    results += fleet
    best = max(fleet, key=lambda r: r["mean gold"])
    kind = "Mixed" if best["bot"] == "Mixed fleet" else best["bot"].split()[-1][:-1]
    label = "Mixed" if kind == "Mixed" else f"{kind}s"
    stay = run(f"{label}, free routes only", lambda: Greedy(expand_with=kind, expand_routes=False))
    results.append(stay)
    openings = rank_openings(data)[:3]
    opening_results = [run(f"{label}, opening {i + 1}", (lambda o=o: lambda: Greedy(expand_with=kind, opening=o))())
                       for i, (_, o) in enumerate(openings)]
    results += opening_results
    upgraded = run(f"{label} + all upgrades", lambda: Greedy(expand_with=kind, upgrades=TRACK_ORDER))
    singles = [run(f"{label} + {t} only", (lambda t=t: lambda: Greedy(expand_with=kind, upgrades=[t]))())
               for t in TRACK_ORDER]
    results += [upgraded] + singles

    print_table(results)
    print()
    print(f"Hurricane-lane share of sea days for '{best['bot']}': "
          f"{100 * best['hurricane lanes early']:.0f}% before the season, "
          f"{100 * best['hurricane lanes late']:.0f}% at its peak")
    print(f"Most-used route for '{best['bot']}': {best['top route']}")
    print(f"Routes owned by '{best['bot']}' (of {len(data.lanes)}): " +
          ", ".join(f"day {d} {v:.1f}" for d, v in best["routes by day"].items()))
    print(f"Most common free picks for '{best['bot']}': " +
          "; ".join(f"{pick} ({100 * n / args.runs:.0f}%)" for pick, n in best["first routes"]))
    print("Openings tried (free routes): " + "; ".join(
        f"{i + 1}: {' + '.join(lane_name(data, l) for l in o)} -> average gold {r['mean gold']:,.0f}"
        for i, ((_, o), r) in enumerate(zip(openings, opening_results))))
    print(f"Median gold for '{best['bot']}' by day: " +
          ", ".join(f"day {d} {v:,.0f}" for d, v in best["gold by day"].items()))
    print(f"Least-visited ports for '{best['bot']}': " +
          ", ".join(f"{p} {share:.1f}%" for share, p in best["quiet ports"]))
    mixed = next(r for r in results if r["bot"] == "Mixed fleet")
    print("Ships bought per session by 'Mixed fleet': " +
          ", ".join(f"{k} {v:.1f}" for k, v in mixed["ships bought"].items()))
    print(f"Upgrades bought per session by '{upgraded['bot']}': " +
          ", ".join(f"{k} {v:.1f}" for k, v in sorted(upgraded["upgrades"].items())))
    print()
    (check if args.mode == "veteran" else check_beginner)(results, best, upgraded, singles, data.rules, opening_results)


def check(results, best, upgraded, singles, rules, opening_results):
    by = {r["bot"]: r for r in results}
    hull = next(r for r in singles if r["bot"].endswith("Hull only"))
    stay = next(r for r in results if r["bot"].endswith("free routes only"))
    fleet = [r for r in results if r["bot"].startswith("Fleet of ")]
    limit = rules["day_limit"]
    base = best["mean gold"]
    gains = [r["mean gold"] / base - 1 for r in singles]
    early, late = best["hurricane lanes early"], best["hurricane lanes late"]
    checks = [
        ("A good player usually wins (best fleet bot wins >= 85%)", best["win %"] >= 85),
        (f"...but not too early (its median win day is {0.65 * limit:.0f}-{0.9 * limit:.0f})",
         best["win day"] is not None and 0.65 * limit <= best["win day"] <= 0.9 * limit),
        ("Growing the fleet matters (1-ship greedy wins <= 50%)", by["Greedy, 1 ship"]["win %"] <= 50),
        ("Putting a fleet on one route fails (fixed-route bot wins <= 20%)", by["Fixed route"]["win %"] <= 20),
        ("Ignoring prices fails (random bot wins <= 2%)", by["Random"]["win %"] <= 2),
        ("No route dominates (best fleet bot's top route <= 30% of voyages)", best["top route %"] <= 30),
        ("Big ships aren't a trap (a fleet that moves up to big ships earns >= 90% of an all-sloop fleet)",
         by["Mixed fleet"]["mean gold"] >= 0.9 * by["Fleet of Sloops"]["mean gold"]),
        ("Hull upgrades make sinking rare (<= 1 per session, at most half the unprotected fleet's rate)",
         hull["sunk"] <= 1.0 and hull["sunk"] <= 0.5 * best["sunk"]),
        ("All 12 ports matter (each gets >= 2% of the best bot's arrivals)", best["min port %"] >= 2),
        ("The Atlantic matters (>= 15% of the best bot's voyages cross it)", best["atlantic %"] >= 15),
        ("Hurricane season changes plans (time on hurricane lanes drops >= 20% at the peak)",
         early > 0 and late <= 0.8 * early),
        ("Opening routes matters (a fleet that keeps only its free routes earns <= 60% of one that expands)",
         stay["mean gold"] <= 0.6 * base),
        ("The opening isn't solved (the 2nd-best free pick earns within 10% of the best)",
         sorted((r["mean gold"] for r in opening_results), reverse=True)[1]
         >= 0.9 * max(r["mean gold"] for r in opening_results)),
        ("Upgrades pay off (all upgrades: >= 10% more average gold on day 90)", upgraded["mean gold"] >= 1.10 * base),
        ("Every upgrade track is worth buying (each alone: >= 3% more average gold)", min(gains) >= 0.03),
        ("No track is a must-have (best single track gains <= 3x the worst)",
         min(gains) > 0 and max(gains) <= 3 * min(gains)),
    ]
    print("Balance targets:")
    for text, ok in checks:
        print(f"  [{'PASS' if ok else 'FAIL'}] {text}")
    print("Upgrade gains vs no upgrades: " +
          ", ".join(f"{r['bot'].split('+ ')[1]} {100 * g:+.0f}%" for r, g in zip(singles, gains)) +
          f", all {100 * (upgraded['mean gold'] / base - 1):+.0f}%")


def check_beginner(results, best, upgraded, singles, rules, opening_results):
    by = {r["bot"]: r for r in results}
    hull = next(r for r in singles if r["bot"].endswith("Hull only"))
    stay = next(r for r in results if r["bot"].endswith("free routes only"))
    base = best["mean gold"]
    checks = [
        ("B1: best fleet bot wins >= 95%", best["win %"] >= 95),
        ("B2: its median win day is 50-75", best["win day"] is not None and 50 <= best["win day"] <= 75),
        ("B3: fixed-route bot wins >= 50%", by["Fixed route"]["win %"] >= 50),
        ("B4: random bot wins <= 5%", by["Random"]["win %"] <= 5),
        ("B5: 1-ship greedy wins <= 80%", by["Greedy, 1 ship"]["win %"] <= 80),
        ("B6: free-routes-only bot earns <= 70% of the best fleet bot's gold", stay["mean gold"] <= 0.7 * base),
        ("B7: all upgrades give >= 10% more average gold", upgraded["mean gold"] >= 1.10 * base),
        ("B8: Hull-only bot loses <= 1 ship per session", hull["sunk"] <= 1.0),
    ]
    print("Beginner balance targets:")
    for text, ok in checks:
        print(f"  [{'PASS' if ok else 'FAIL'}] {text}")
    print("(Veteran targets not checked in beginner mode.)")


if __name__ == "__main__":
    main()
