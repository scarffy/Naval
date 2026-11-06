#!/usr/bin/env python3
"""Balance simulator for the Naval vertical slice (economy version 2).

Reads the CSV files in Design/data, plays many 730-day sessions with simple bot
players, and checks the results against the balance targets. Standard library only.

    python3 Design/sim/simulate.py --mode veteran     # 100 sessions per bot
    python3 Design/sim/simulate.py --mode beginner --runs 50 --seed 2
    python3 Design/sim/simulate.py --tracks           # also run the four single-track upgrade bots

Every number lives in a CSV. rules.csv holds the rules, modes.csv overrides them per mode,
and the bots' own thresholds are the rules whose names start with "bot_".
"""
import argparse
import csv
import heapq
import math
import multiprocessing
import os
import random
import statistics
import time
from collections import Counter, defaultdict, deque

DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "data")
TRACK_ORDER = ["Hold", "Sails", "Hull", "Guns"]
BIG = 10 ** 9


# ---------------------------------------------------------------- data ----

def read_csv(name):
    with open(os.path.join(DATA, name), newline="") as f:
        return list(csv.DictReader(f))


def as_value(v):
    try:
        return float(v)
    except ValueError:
        return v


def yes(v):
    return (v or "").strip().lower() == "yes"


class Data:
    def __init__(self, mode="veteran", hurricanes=True):
        self.mode = mode
        self.hurricanes = hurricanes      # False plays the same world with no hurricanes (for the seasons target)
        self.rules = {r["key"]: as_value(r["value"]) for r in read_csv("rules.csv")}
        for r in read_csv("modes.csv"):
            if r["mode"].strip().startswith("#") or r["mode"].strip() != mode:
                continue
            if r["key"] not in self.rules:
                raise SystemExit(f"modes.csv overrides unknown rule {r['key']}")
            self.rules[r["key"]] = as_value(r["value"])
        R = self.rules
        goods = read_csv("goods.csv")
        self.goods = {r["good"]: float(r["base_price"]) for r in goods}
        # Hold space per crate and perishability only apply in the mode that turns them on.
        self.space = {r["good"]: float(r["hold_space"]) if R["hold_space_matters"] else 1.0 for r in goods}
        self.perishable = {r["good"]: bool(R["perishable_goods"]) and yes(r["perishable"]) for r in goods}
        self.ports = {r["port"]: {"region": r["region"], "shipyard": yes(r["shipyard"]),
                                  "level": int(r["start_level"])} for r in read_csv("ports.csv")}
        self.market = {}
        for r in read_csv("market.csv"):
            self.market[r["port"]] = {g: float(r[g]) for g in self.goods if (r.get(g) or "").strip()}
        if set(self.market) != set(self.ports):
            raise SystemExit("market.csv and ports.csv list different ports")
        self.ships = {r["ship"]: {"price": float(r["price"]), "capacity": int(r["capacity"]),
                                  "speed": float(r["speed"]), "hull": int(r["hull"]),
                                  "wage": float(r["wage_per_day"]), "ocean": yes(r.get("ocean_going", "yes"))}
                      for r in read_csv("ships.csv")}
        self.upgrades = defaultdict(list)
        for r in sorted(read_csv("upgrades.csv"), key=lambda r: (r["track"], int(r["level"]))):
            self.upgrades[r["track"]].append({k: as_value(v) for k, v in r.items()})
        # Buildings available in this mode.
        self.buildings = {}
        for r in read_csv("buildings.csv"):
            if r["modes"].strip() in ("both", mode):
                self.buildings[r["building"]] = {k: as_value(v) for k, v in r.items()}
        self.lanes = [{"id": i, "a": r["from"], "b": r["to"], "distance": float(r["distance"]),
                       "storm": float(r["storm_per_day"]), "pirates": float(r["pirates_per_day"]),
                       "hurricanes": float(r["hurricanes_per_day"]), "price": float(r["price"]),
                       "ocean": yes(r.get("ocean", "no"))} for i, r in enumerate(read_csv("lanes.csv"))]
        self.limits = bool(R["port_stock_limits"])
        self.level_mult = {i: R[f"port_level_multiplier_{i}"] for i in range(1, 6)}
        self.max_level = 5
        self.season_table = self._season_table()
        # Per port and good: (stock cap, restock/day, demand cap, refill/day) at level 1, by what the port makes or wants.
        self.pool, self.wanted = {}, {}
        for p, goods_here in self.market.items():
            self.pool[p], self.wanted[p] = {}, []
            for g, m in goods_here.items():
                stock, restock = R["stock_other"], R["restock_other"]
                demand, refill = R["stock_other"], R["restock_other"]
                if m <= R["cheap_multiplier_max"]:
                    stock, restock = R["stock_cheap"], R["restock_cheap"]
                if m >= R["wanted_multiplier_min"]:
                    demand, refill = R["demand_wanted"], R["refill_wanted"]
                    self.wanted[p].append(g)
                self.pool[p][g] = (stock, restock, demand, refill)
        # Crates a level 1 port's wanted goods should take per day: the yardstick for port growth (both modes).
        self.nominal_demand = {p: len(self.wanted[p]) * R["growth_demand_per_good"] for p in self.ports}
        self.shared = {(a, b): [g for g in self.market[a] if g in self.market[b]]
                       for a in self.market for b in self.market if a != b}
        self.routes = self._routes(self.lanes)            # every route open: used for reports
        self._route_cache, self._trade_cache, self._risk_cache = {}, {}, {}
        self.pair_trades = {(a, b): self._pair_trades(a, b) for (a, b) in self.shared}

    # --- calendar
    def _season_table(self):
        R = self.rules
        rise, p0, p1, end = (R["hurricane_rise_day"], R["hurricane_peak_start_day"],
                             R["hurricane_peak_end_day"], R["hurricane_end_day"])
        table = [0.0]
        for day in range(1, int(R["day_limit"]) + 80):
            doy = (int(R["start_day_of_year"]) - 1 + day - 1) % int(R["year_days"]) + 1
            if doy < rise or doy >= end:
                s = 0.0
            elif doy < p0:
                s = (doy - rise) / (p0 - rise)
            elif doy <= p1:
                s = 1.0
            else:
                s = (end - doy) / (end - p1)
            table.append(s)
        return table

    def season(self, day):
        return self.season_table[min(max(int(day), 0), len(self.season_table) - 1)]

    def hazard(self, day):
        """Hurricane strength (0..1) that actually applies on a day."""
        return self.season(day) if self.hurricanes else 0.0

    def year_index(self, day):
        """Calendar year of a mission day: 0 for the first calendar year, 1 for the next."""
        return (int(self.rules["start_day_of_year"]) - 1 + int(day) - 1) // int(self.rules["year_days"])

    # --- port rules
    def spread_at(self, level, steps=0):
        """Buy/sell gap at a port of this level (Beginner narrows it with level; Veteran's step is 0)."""
        R = self.rules
        return max(R["spread_floor"], R["spread"] - R["spread_level_step"] * (level - 1 + steps)) \
            if R["spread_level_step"] else R["spread"]

    def slots(self, level):
        R = self.rules
        return int(R["building_slots_level_1"] if level <= 2 else
                   R["building_slots_level_3"] if level <= 4 else R["building_slots_level_5"])

    # --- route estimates used by bots
    def _pair_trades(self, a, b):
        """(margin per hold space, hold space per day the trade can absorb) for each good that pays a -> b."""
        R = self.rules
        out = []
        for g in self.shared[(a, b)]:
            sa = self.spread_at(self.ports[a]["level"])
            sb = self.spread_at(self.ports[b]["level"])
            buy = self.goods[g] * self.market[a][g] * (1 + sa / 2)
            sell = self.goods[g] * self.market[b][g] * (1 - sb / 2)
            m = sell - buy
            if m <= 0:
                continue
            flow = BIG
            if self.limits:
                la, lb = self.level_mult[self.ports[a]["level"]], self.level_mult[self.ports[b]["level"]]
                flow = min(self.pool[a][g][1] * la, self.pool[b][g][3] * lb)
                push = R["pressure_per_crate_bought"] / la + R["pressure_per_crate_sold"] / lb
                if push > 0:   # the flow at which price movement eats about half the margin
                    flow = min(flow, 0.5 * (m / sell) * R["pressure_recovery_per_day"] / push)
            out.append((m / self.space[g], flow * self.space[g]))
        return out

    def risk(self, route, season):
        """Mean hull damage and mean days of delay per day at sea on a route, at a hurricane strength."""
        key = (id(route), season)
        if key not in self._risk_cache:
            R = self.rules
            mean = lambda lo, hi: (R[lo] + R[hi]) / 2
            self._risk_cache[key] = (
                route["storm"] * mean("storm_damage_min", "storm_damage_max")
                + route["pirates"] * mean("pirate_damage_min", "pirate_damage_max")
                + route["hurricanes"] * season * mean("hurricane_damage_min", "hurricane_damage_max"),
                route["storm"] * mean("storm_delay_min", "storm_delay_max")
                + route["hurricanes"] * season * mean("hurricane_delay_min", "hurricane_delay_max"))
        return self._risk_cache[key]

    def expected_delay(self, route):
        """Days a typical trip on this route loses to storms and (in an average year) hurricanes."""
        R = self.rules
        mean = lambda lo, hi: (R[lo] + R[hi]) / 2
        mean_season = sum(self.season_table[1:int(R["year_days"]) + 1]) / R["year_days"] if self.hurricanes else 0.0
        days = route["distance"] / 1.3        # a Brig's speed
        return days * (route["storm"] * mean("storm_delay_min", "storm_delay_max")
                       + route["hurricanes"] * mean_season * mean("hurricane_delay_min", "hurricane_delay_max"))

    def routes_for(self, owned, ocean=True):
        """Shortest paths using only the routes the player owns (a frozenset of lane ids).
        Coastal ships (ocean=False) can't use ocean crossings."""
        key = (owned, ocean)
        if key not in self._route_cache:
            if len(self._route_cache) > 20000:
                self._route_cache.clear()
            lanes = [self.lanes[i] for i in owned if ocean or not self.lanes[i]["ocean"]]
            self._route_cache[key] = self._routes(lanes, require_all=False)
        return self._route_cache[key]

    def trade_list(self, owned, ocean=True):
        """Trades the routes allow from the home network, best gold per hold space per day first."""
        key = (owned, ocean)
        if key not in self._trade_cache:
            if len(self._trade_cache) > 20000:
                self._trade_cache.clear()
            routes = self.routes_for(owned, ocean)
            home, pt = self.rules["start_port"], self.rules["port_time_days"]
            out = []
            for (a, b), route in routes.items():
                if a != home and (home, a) not in routes:
                    continue
                span = route["distance"] + pt + self.expected_delay(route)
                for m_space, cap in self.pair_trades[(a, b)]:
                    out.append((m_space / span, span, cap))
            out.sort(reverse=True)
            self._trade_cache[key] = out[:150]
        return self._trade_cache[key]

    def network_income(self, owned, capacity, ocean=True):
        """Rough gold per day a fleet with this much hold-space x speed earns on the routes:
        fill the best trades first, each up to the hold space per day its ports can absorb."""
        left, income = capacity, 0.0
        for rate, span, cap in self.trade_list(owned, ocean):
            use = min(left, cap * span)
            income += use * rate
            left -= use
            if left <= 0:
                break
        return income

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
        self.refresh()
        self.hull = self.max_hull

    def _mult(self, field):
        m = 1.0
        for track, level in self.levels.items():
            if level:
                m *= self.data.upgrades[track][level - 1][field]
        return m

    def refresh(self):
        """Recompute the stats that upgrades change."""
        self.speed = self.base["speed"] * self._mult("speed")
        self.capacity = int(round(self.base["capacity"] * self._mult("capacity")))
        self.max_hull = int(round(self.base["hull"] * self._mult("hull")))
        self.wage = self.base["wage"] * self._mult("wage")
        self.repair_mult = self._mult("repair_cost")
        self.repel = max([self.data.upgrades[t][l - 1]["pirate_repel"] for t, l in self.levels.items() if l] or [0.0])

    def used(self):
        space = self.data.space
        return sum(n * space[g] for g, n in self.cargo.items())

    def room(self):
        return self.capacity - self.used()


def frac_at_least(hull, lo, hi):
    """Chance that damage drawn uniformly from lo..hi is at least `hull`."""
    return min(1.0, max(0.0, (hi - hull + 1) / (hi - lo + 1)))


class Game:
    def __init__(self, data, rng):
        self.d, self.r, self.rng = data, data.rules, rng
        r = self.r
        self.day = 0
        self.now = 0.0                   # exact time of the decision being made
        self.gold = r["start_gold"]
        self.level = {p: info["level"] for p, info in data.ports.items()}
        self.built = {p: [] for p in data.ports}              # buildings in each port, in build order
        self.visited = {r["start_port"]}
        self.pressure = {p: {g: 0.0 for g in m} for p, m in data.market.items()}   # pushed by the player's trading
        self.trend = {p: {g: 0.0 for g in m} for p, m in data.market.items()}      # shortages and gluts
        self.norm = {p: {g: data.goods[g] * m for g, m in ms.items()} for p, ms in data.market.items()}
        self.mid = {p: dict(ns) for p, ns in self.norm.items()}
        self.inbound = {p: Counter() for p in data.market}
        self.stock, self.demand = {}, {}
        for p in data.market:
            self.refresh_port(p)
        if data.limits:
            self.stock = {p: {g: data.pool[p][g][0] * self.cap_f[p] for g in data.market[p]} for p in data.market}
            self.demand = {p: {g: data.pool[p][g][2] * self.cap_f[p] for g in data.market[p]} for p in data.market}
        self.fleet = [Ship(data, r["start_ship"], r["start_port"])]
        self.win_day = None
        self.sunk = 0
        self.events = Counter()
        self.trips = Counter()
        self.arrivals = Counter()
        self.atlantic_trips = 0
        self.sea_days = Counter()        # (period, on_hurricane_lane) -> ship-days at sea
        self.cargo_log = Counter()       # (ship kind, good) -> hold space loaded
        self.ships_bought = Counter()
        self.upgrades_bought = Counter()
        self.history = []                # gold at the end of each day
        self.owned = frozenset()         # ids of the routes the player has opened
        self.reach_tables = {True: {}, False: {}}   # port -> [(destination, route)] over owned routes
        self.free_routes = int(r["free_routes"])
        self.route_log = []              # (day, lane) for each route opened
        # Port growth: deliveries of wanted goods over a rolling window.
        win = int(r["growth_window_days"])
        self.window = {p: deque(maxlen=win) for p in data.market}
        self.window_sum = dict.fromkeys(data.market, 0)
        self.wanted_sold = dict.fromkeys(data.market, 0)
        self.level_log = []              # (day, port, old level, new level)
        # What each port's limits cost the player per day (smoothed): bots use it to decide on buildings.
        self.strain = dict.fromkeys(data.market, 0.0)
        self.strain_today = dict.fromkeys(data.market, 0.0)
        self.turnover = dict.fromkeys(data.market, 0.0)       # gold traded per day at the port
        self.turnover_today = dict.fromkeys(data.market, 0.0)
        self.arrival_rate = dict.fromkeys(data.market, 0.0)
        self.arrivals_today = dict.fromkeys(data.market, 0)
        self.upkeep_paid = 0.0
        self.spoiled = 0
        self.capex = 0.0                 # gold spent today on ships, upgrades, routes and buildings
        self.operating = []              # gold earned per day before those purchases
        self.day_start_gold = self.gold

    # --- ports
    def refresh_port(self, p):
        """Recompute a port's multipliers from its level and buildings."""
        d, r = self.d, self.r
        scale = d.level_mult[self.level[p]]
        recover, steps, dues, time_f = 1.0, 0, 1.0, 1.0
        for name in self.built[p]:
            b = d.buildings[name]
            scale_b, recover = b["capacity_mult"], recover * b["recovery_mult"]
            steps += int(b["spread_steps"])
            dues, time_f = dues * b["dues_mult"], time_f * b["time_mult"]
            scale *= scale_b
        level_only = d.level_mult[self.level[p]]
        for attr, val in (("cap_f", scale), ("rate_f", level_only * recover), ("dues_f", dues), ("time_f", time_f),
                          ("recover_f", min(1.0, r["pressure_recovery_per_day"] * recover)),
                          ("spread_f", d.spread_at(self.level[p], steps if r["spread_level_step"] else 0))):
            if not hasattr(self, attr):
                setattr(self, attr, {})
            getattr(self, attr)[p] = val
        self.cap_f[p] = scale

    def slots(self, p):
        return self.d.slots(self.level[p])

    def build(self, p, name):
        self.gold -= self.d.buildings[name]["cost"]
        self.capex += self.d.buildings[name]["cost"]
        self.built[p].append(name)
        self.refresh_port(p)
        self.upgrades_bought["building " + name] += 1

    # --- prices
    def _price(self, p, g, pressure):
        x = min(self.r["pressure_max"], max(self.r["pressure_min"], pressure))
        return self.norm[p][g] * (1 + x)

    def remid(self, p, g):
        self.mid[p][g] = self._price(p, g, self.pressure[p][g] + self.trend[p][g])

    def buy_price(self, p, g, extra=0.0):
        return self._price(p, g, self.pressure[p][g] + self.trend[p][g] + extra) * (1 + self.spread_f[p] / 2)

    def sell_price(self, p, g, pressure):
        return self._price(p, g, pressure) * (1 - self.spread_f[p] / 2)

    def push(self, p, g, amount):
        x = self.pressure[p][g] + amount / self.cap_f[p]
        self.pressure[p][g] = min(self.r["pressure_max"], max(self.r["pressure_min"], x))
        self.remid(p, g)

    def buy(self, ship, g, n):
        """Buy up to n crates. Returns how many were bought."""
        p, r = ship.port, self.r
        if self.d.limits:
            n = min(n, int(self.stock[p][g]))
            self.stock[p][g] -= n
        ref = self._price(p, g, self.trend[p][g]) * (1 + self.spread_f[p] / 2)
        kb = r["pressure_per_crate_bought"]
        for _ in range(n):
            price = self.buy_price(p, g)
            self.gold -= price
            self.turnover_today[p] += price
            self.strain_today[p] += price - ref
            if kb:
                self.push(p, g, kb)
        ship.cargo[g] += n
        return n

    def sell_all(self, ship):
        p, r = ship.port, self.r
        ks = r["pressure_per_crate_sold"]
        for g, n in list(ship.cargo.items()):
            if g not in self.d.market[p]:
                continue                                  # not traded here: keep it aboard
            if self.d.limits:
                n = min(n, int(self.demand[p][g]))        # the port only buys so many
                self.demand[p][g] -= n
            ref = self._price(p, g, self.trend[p][g]) * (1 - self.spread_f[p] / 2)
            for _ in range(n):
                price = self.sell_price(p, g, self.pressure[p][g] + self.trend[p][g])
                self.gold += price
                self.turnover_today[p] += price
                self.strain_today[p] += ref - price
                if ks:
                    self.push(p, g, -ks)
            if g in self.d.wanted[p]:
                self.wanted_sold[p] += n
            ship.cargo[g] -= n
            if ship.cargo[g] <= 0:
                del ship.cargo[g]

    # --- ships
    def repair(self, ship, below=1.0):
        missing = ship.max_hull - ship.hull
        if missing <= 0 or ship.hull > ship.max_hull * below:
            return
        cost = self.r["repair_cost_per_hull"] * ship.repair_mult
        fix = min(missing, int(max(0, self.gold) // cost))
        ship.hull += fix
        self.gold -= fix * cost

    def buy_ship(self, kind, port):
        spec = self.d.ships[kind]
        self.gold -= spec["price"]
        self.capex += spec["price"]
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
        before = ship.max_hull
        ship.levels[track] += 1
        ship.refresh()
        ship.hull += ship.max_hull - before
        ship.invested += cost
        self.gold -= cost
        self.capex += cost
        self.upgrades_bought[f"{track} {ship.levels[track]}"] += 1

    # --- routes
    def routes(self, ship):
        return self.d.routes_for(self.owned, ship.base["ocean"])

    def reach(self, ship):
        return self.reach_tables[ship.base["ocean"]].get(ship.port, ())

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
        self.capex += self.route_price(lane)
        if self.free_routes > 0:
            self.free_routes -= 1
        self.owned = self.owned | {lane["id"]}
        for ocean in (True, False):
            table = defaultdict(list)
            for (a, b), route in self.d.routes_for(self.owned, ocean).items():
                table[a].append((b, route))
            self.reach_tables[ocean] = table
        self.route_log.append((self.day, lane))
        self.visited |= {lane["a"], lane["b"]}

    # --- voyages
    def voyage_days(self, ship, dest):
        return self.routes(ship)[(ship.port, dest)]["distance"] / ship.speed

    def roll(self, lo, hi):
        return self.rng.randint(int(self.r[lo]), int(self.r[hi]))

    def lose_cargo(self, ship, lo, hi):
        frac = self.rng.uniform(self.r[lo], self.r[hi])
        for g in ship.cargo:
            ship.cargo[g] -= round(ship.cargo[g] * frac)

    def period(self, day):
        """Hurricane-season bookkeeping: the peak of one year's season, or the calm months that follow it
        (December to May)."""
        day = max(int(day), 1)
        s = self.d.season(day)
        if s >= 1:
            return f"peak{self.d.year_index(day)}"
        if s > 0:
            return None
        doy = (int(self.r["start_day_of_year"]) - 1 + int(day) - 1) % int(self.r["year_days"]) + 1
        return f"calm{self.d.year_index(day) - (0 if doy >= self.r['hurricane_end_day'] else 1)}"

    def sail(self, ship, dest):
        r = self.r
        route = self.routes(ship)[(ship.port, dest)]
        days = self.voyage_days(ship, dest)
        self.trips[tuple(sorted((ship.port, dest)))] += 1
        self.atlantic_trips += route["atlantic"]
        for g, n in ship.cargo.items():
            self.cargo_log[(ship.kind, g)] += n * self.d.space[g]
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
            season = self.d.hazard(sea_day)
            period = self.period(sea_day)
            if period:
                self.sea_days[(period, lane["hurricanes"] > 0.025)] += share
            if self.rng.random() < lane["storm"] * share:
                self.events["storm"] += 1
                delay += self.roll("storm_delay_min", "storm_delay_max")
                ship.hull -= self.roll("storm_damage_min", "storm_damage_max")
            if self.rng.random() < lane["pirates"] * share:
                if self.rng.random() < ship.repel:
                    self.events["pirates driven off"] += 1
                    ship.hull -= self.roll("pirate_damage_min", "pirate_damage_max") // 2
                else:
                    self.events["pirates"] += 1
                    self.lose_cargo(ship, "pirate_loss_min", "pirate_loss_max")
                    ship.hull -= self.roll("pirate_damage_min", "pirate_damage_max")
            if self.rng.random() < lane["hurricanes"] * season * share:
                self.events["hurricane"] += 1
                delay += self.roll("hurricane_delay_min", "hurricane_delay_max")
                self.lose_cargo(ship, "hurricane_loss_min", "hurricane_loss_max")
                ship.hull -= self.roll("hurricane_damage_min", "hurricane_damage_max")
            if ship.hull <= 0:
                break
        late = days + delay - r["spoil_after_days"]       # days a perishable cargo spends spoiling
        for g in ship.cargo:
            if late > 0 and self.d.perishable[g]:
                kept = round(ship.cargo[g] * (1 - r["spoil_per_day"]) ** late)
                self.spoiled += ship.cargo[g] - kept
                ship.cargo[g] = kept
        for g, n in ship.cargo.items():
            self.inbound[dest][g] += n
        ship.origin, ship.port = ship.port, dest
        ship.free_at = self.now + days + delay + r["port_time_days"] * self.time_f[dest]

    def arrive(self, ship):
        for g, n in ship.cargo.items():
            self.inbound[ship.port][g] -= n
        ship.origin = None
        if ship.hull <= 0:
            self.sunk += 1
            self.fleet.remove(ship)
            return False
        p = ship.port
        self.arrivals[p] += 1
        self.arrivals_today[p] += 1
        self.visited.add(p)
        self.gold -= self.r["port_fee"] * self.dues_f[p]
        self.sell_all(ship)
        return True

    # --- the daily tick
    def end_of_day(self):
        d, r = self.d, self.r
        self.gold -= sum(s.wage for s in self.fleet)
        for p, names in self.built.items():
            for name in names:
                cost = d.buildings[name]["upkeep_per_month"] / r["days_per_month"]
                self.gold -= cost
                self.upkeep_paid += cost
        trend_keep = 1 - r["market_event_recovery_per_day"]
        noise = r["market_noise_per_day"]
        gauss = self.rng.gauss
        pmin, pmax = r["pressure_min"], r["pressure_max"]
        for p, goods in d.market.items():
            keep = 1 - self.recover_f[p]
            pres, trend, norm, mid = self.pressure[p], self.trend[p], self.norm[p], self.mid[p]
            for g in goods:
                pg = pres[g] = pres[g] * keep + gauss(0, noise)
                tg = trend[g] = trend[g] * trend_keep
                x = pg + tg
                mid[g] = norm[g] * (1 + (pmax if x > pmax else (pmin if x < pmin else x)))
            if d.limits:
                rate = self.rate_f[p]
                cap = self.cap_f[p]
                stock, demand = self.stock[p], self.demand[p]
                for g, (s_cap, s_rate, d_cap, d_rate) in d.pool[p].items():
                    stock[g] = min(s_cap * cap, stock[g] + s_rate * rate)
                    demand[g] = min(d_cap * cap, demand[g] + d_rate * rate)
        if self.rng.random() < r["market_event_chance_per_day"]:
            port = self.rng.choice(list(d.market))
            good = self.rng.choice(list(d.market[port]))
            self.trend[port][good] += self.rng.choice((-1, 1)) * r["market_event_size"]
            self.remid(port, good)
            self.events["market news"] += 1
        self.update_ports()
        self.operating.append(self.gold - self.day_start_gold + self.capex)
        self.capex = 0.0
        self.day_start_gold = self.gold
        self.history.append(self.gold)
        if self.win_day is None and self.gold >= r["goal_gold"]:
            self.win_day = self.day

    def update_ports(self):
        """Smooth the daily stats and grow or shrink ports."""
        d, r = self.d, self.r
        a = 1 / 30
        for p in d.market:
            self.strain[p] += a * (self.strain_today[p] - self.strain[p])
            self.turnover[p] += a * (self.turnover_today[p] - self.turnover[p])
            self.arrival_rate[p] += a * (self.arrivals_today[p] - self.arrival_rate[p])
            self.strain_today[p] = self.turnover_today[p] = self.arrivals_today[p] = 0
            w = self.window[p]
            if len(w) == w.maxlen:
                self.window_sum[p] -= w[0]
            w.append(self.wanted_sold[p])
            self.window_sum[p] += self.wanted_sold[p]
            self.wanted_sold[p] = 0
            if len(w) < w.maxlen:
                continue
            level = self.level[p]
            supply = self.window_sum[p] / (d.nominal_demand[p] * d.level_mult[level] * w.maxlen or 1)
            new = level
            if level < d.max_level and supply >= r[f"growth_supply_level_{level}"]:
                new = level + 1
            elif level > 1 and supply < r["port_decline_below"]:
                new = level - 1
            if new != level:
                self.level_log.append((self.day, p, level, new))
                self.level[p] = new
                del self.built[p][d.slots(new):]          # a smaller port loses its newest buildings
                self.refresh_port(p)
                w.clear()
                self.window_sum[p] = 0

    def bankrupt(self):
        return not self.fleet and self.gold < min(s["price"] for s in self.d.ships.values())

    def run(self, bot):
        for self.day in range(1, int(self.r["day_limit"]) + 1):
            for ship in sorted((s for s in self.fleet if s.free_at <= self.day), key=lambda s: s.free_at):
                self.now = max(ship.free_at, self.day - 1)
                if ship.origin is not None and not self.arrive(ship):
                    continue
                bot.act(self, ship)
            if not self.fleet and self.gold >= self.d.ships[self.r["start_ship"]]["price"]:
                self.buy_ship(self.r["start_ship"], self.r["start_port"])     # a player with gold can always restart
            self.end_of_day()
            if self.bankrupt():
                break
        return self

    def result(self):
        """The small summary of a finished session that the report needs."""
        return {"win_day": self.win_day, "gold": self.gold, "history": self.history, "trips": self.trips,
                "arrivals": self.arrivals, "sea_days": self.sea_days, "sunk": self.sunk,
                "atlantic": self.atlantic_trips, "upgrades": self.upgrades_bought, "cargo": self.cargo_log,
                "routes": [(day, f"{l['a']}-{l['b']}") for day, l in self.route_log],
                "ships_bought": self.ships_bought, "fleet": len(self.fleet), "levels": dict(self.level),
                "level_log": self.level_log, "upkeep": self.upkeep_paid, "spoiled": self.spoiled,
                "buildings": sum(len(b) for b in self.built.values())}


# ---------------------------------------------------------------- bots ----

def plan_cargo(game, ship, dest, days, floor=-math.inf):
    """Choose crates by best expected margin per hold space. Returns (plan, expected profit, lost, trip days),
    where trip days include the expected storm and hurricane delays, and lost maps a port to the gold the
    ship leaves behind because that port's stock or demand ran out.
    Returns None without planning in detail if even a full hold of the best crate couldn't beat `floor`
    (profit per day), which keeps the search fast."""
    r, d = game.r, game.d
    port = ship.port
    route = game.routes(ship)[(port, dest)]
    pt = r["port_time_days"] * game.time_f[dest]
    season = d.hazard(int(game.now + days / 2)) * r["bot_hurricane_caution"]
    p_robbed = 1 - (1 - route["pirates"] * (1 - ship.repel)) ** days
    p_hurricane = 1 - (1 - route["hurricanes"] * season) ** days
    keep = ((1 - p_robbed * (r["pirate_loss_min"] + r["pirate_loss_max"]) / 2)
            * (1 - p_hurricane * (r["hurricane_loss_min"] + r["hurricane_loss_max"]) / 2))
    risk = d.risk(route, season)
    damage, delay = days * risk[0], days * risk[1]
    trip_days = days + delay + pt
    fixed = (trip_days * ship.wage + r["port_fee"] * game.dues_f[dest]
             + damage * r["repair_cost_per_hull"] * ship.repair_mult)
    spoil = (1 - r["spoil_per_day"]) ** max(0.0, days - r["spoil_after_days"])
    decay = (1 - game.recover_f[dest]) ** days
    trend_decay = (1 - r["market_event_recovery_per_day"]) ** days
    kb = r["pressure_per_crate_bought"] / game.cap_f[port]
    ks = r["pressure_per_crate_sold"] / game.cap_f[dest]
    pres, trend, inbound = game.pressure[dest], game.trend[dest], game.inbound[dest]
    pres_a, trend_a = game.pressure[port], game.trend[port]
    norm_a, norm_b = game.norm[port], game.norm[dest]
    bm, sm = 1 + game.spread_f[port] / 2, 1 - game.spread_f[dest] / 2
    pmin, pmax = r["pressure_min"], r["pressure_max"]
    limits = d.limits
    space = d.space
    # Per good: buy-side pressure now, and the sell-side pressure expected on arrival.
    state = {}
    for g in d.shared[(port, dest)]:
        k_g = keep * (spoil if d.perishable[g] else 1.0)
        state[g] = (norm_a[g], pres_a[g] + trend_a[g], norm_b[g],
                    pres[g] * decay + trend[g] * trend_decay - inbound[g] * ks, k_g)

    def margin(g, k):
        na, pa, nb, pb, kg = state[g]
        x = pa + k * kb
        x = pmax if x > pmax else (pmin if x < pmin else x)
        cost = na * (1 + x) * bm
        y = pb - k * ks
        y = pmax if y > pmax else (pmin if y < pmin else y)
        return nb * (1 + y) * sm * kg - cost, cost

    def avail(g):
        """Crates the ports can supply at the start and take at the end of the trip."""
        if not limits:
            return BIG, "none"
        rate_dest = d.pool[dest][g][3] * game.rate_f[dest]
        want = min(d.pool[dest][g][2] * game.cap_f[dest], game.demand[dest][g] + rate_dest * days) - inbound[g]
        have = game.stock[port][g]
        return max(0, int(min(have, want))), (port if have <= want else dest)

    room = ship.room()
    profit = 0.0
    # Cargo still aboard (it wasn't traded at the last port) may sell here.
    for g, n in ship.cargo.items():
        if g in d.market[dest]:
            if limits:
                n = min(n, max(0, int(game.demand[dest][g] - inbound[g])))
            profit += n * game.sell_price(dest, g, pres[g]) * keep
    first, best_mps = [], 0.0
    for g in state:
        m, c = margin(g, 0)
        if m > 2:
            first.append((m, c, g))
            best_mps = max(best_mps, m / space[g])
    if (profit + room * best_mps - fixed) / trip_days <= floor:
        return None                               # can't beat the best trip found so far
    budget = game.gold - 200                      # keep a little for wages and repairs
    plan, spent, lost, capped = Counter(), 0.0, {}, []
    heap = []
    for m, c, g in first:
        lim, who = avail(g)
        if lim > 0:
            heap.append((-m / space[g], g, 0, c, m, lim, who))
    heapq.heapify(heap)
    while heap and room > 0:
        neg, g, k, cost, m, lim, who = heapq.heappop(heap)
        sp = space[g]
        if m <= 2 or sp > room or cost > budget:
            continue
        if k >= lim:
            capped.append((-neg, who))            # this good ran out of stock or demand
            continue
        plan[g] += 1
        room -= sp
        budget -= cost
        spent += cost
        profit += m
        if kb == 0 and ks == 0:
            # Fixed prices: the margin doesn't change, so take as many as fit in one step.
            n = min(lim - k - 1, int(room // sp), int(max(0, budget) // max(cost, 1e-9)))
            if n > 0:
                plan[g] += n
                room -= n * sp
                budget -= n * cost
                spent += n * cost
                profit += n * m
                k += n
        m2, c2 = margin(g, k + 1)
        heapq.heappush(heap, (-m2 / sp, g, k + 1, c2, m2, lim, who))
    if capped and room >= 0.5:
        mps, who = capped[0]
        lost[who] = room * mps
    profit -= fixed
    p_sink = min(1.0, p_hurricane * frac_at_least(ship.hull, r["hurricane_damage_min"], r["hurricane_damage_max"])
                 + (1 - (1 - route["storm"]) ** days)
                 * frac_at_least(ship.hull, r["storm_damage_min"], r["storm_damage_max"]))
    profit -= p_sink * (ship.invested + spent + max(0.0, profit))
    return plan, profit, lost, trip_days


def reserve(game, extra_capacity=0):
    """Gold to keep back so ships can still fill their holds."""
    return game.r["bot_reserve_per_space"] * (extra_capacity + sum(s.capacity for s in game.fleet)) + 300


def best_new_route(game, owned=None, free=None):
    """Best next route to open, looking one purchase further ahead.
    Returns (lane to open now, extra gold per day for the fleet, price of the plan)."""
    owned = game.owned if owned is None else owned
    free = game.free_routes if free is None else free
    capacity = sum(s.capacity * s.speed for s in game.fleet) or 22.5
    ocean = any(s.base["ocean"] for s in game.fleet)
    base = game.d.network_income(owned, capacity, ocean)
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
            gain = game.d.network_income(routes, capacity, ocean) - base
            if gain <= 0:
                continue
            value = gain / max(1.0, cost) if cost > 0 else gain * 1e6
            if best is None or value > best[0]:
                best = (value, lane, gain, cost)
    return (None, 0.0, 0.0) if best is None else best[1:]


class Greedy:
    """Picks the most profitable trip per day from where the ship is.
    Takes its free routes, then buys more when they pay back quickly (unless expand_routes is off).
    Optionally grows a fleet, buys upgrades at shipyards and builds port buildings.
    It buys a ship only while the recent marginal trip rate would repay it fast enough."""
    def __init__(self, expand_with=None, upgrades=(), allowed=None, expand_routes=True, opening=None, build=False):
        self.expand_with = expand_with
        self.upgrades = list(upgrades)
        self.allowed = allowed            # restrict to these port pairs (fixed-route bot)
        self.expand_routes = expand_routes
        self.opening = list(opening or [])   # lane ids to take as the free routes, in order
        self.build = build
        self.checked_day = -99
        self.checked_build = -99
        self.rate = None                  # smoothed profit per ship-day of the trips ships choose

    def ship_day_profit(self, game):
        """Profit per ship-day: the fleet's real operating profit over the last 30 days once there is a month
        of history, the smoothed trip estimate before that."""
        window = game.operating[-30:]
        if len(window) >= 30 and game.fleet:
            return sum(window) / 30 / len(game.fleet)
        return self.rate

    def days_left(self, game):
        return game.r["day_limit"] - game.day

    def manage_routes(self, game):
        while game.free_routes > 0:
            lane = game.d.lanes[self.opening.pop(0)] if self.opening else best_new_route(game)[0]
            if lane is None:
                break
            game.buy_route(lane)
        r = game.r
        if (not self.expand_routes or game.day - self.checked_day < r["bot_route_check_days"]
                or self.days_left(game) < r["bot_end_buy_days"]):
            return
        self.checked_day = game.day
        candidates = game.route_candidates()
        if not candidates or game.gold < min(l["price"] for l in candidates) + reserve(game):
            return
        horizon = min(r["bot_route_payback_days"], self.days_left(game) / 2)
        while True:
            lane, gain, cost = best_new_route(game)
            if lane is None or cost > gain * horizon or game.gold < game.route_price(lane) + reserve(game):
                return
            game.buy_route(lane)

    def building_gain(self, game, p, b):
        """Gold per day a building at port p should add, from what the port costs the fleet now."""
        gain = game.strain[p] * ((1 - 1 / b["capacity_mult"]) + (1 - 1 / b["recovery_mult"]))
        if b["spread_steps"]:
            # Beginner: a narrower gap saves half the step on everything traded there.
            step = game.d.rules["spread_level_step"]
            room = game.spread_f[p] - game.d.rules["spread_floor"]
            gain += game.turnover[p] * min(step * b["spread_steps"], max(0.0, room)) / 2
        if b["dues_mult"] < 1 or b["time_mult"] < 1:
            day_value = max(self.ship_day_profit(game) or 0.0, 0.0)
            gain += game.arrival_rate[p] * (game.r["port_fee"] * (1 - b["dues_mult"])
                                            + game.r["port_time_days"] * (1 - b["time_mult"]) * day_value)
        return gain

    def manage_buildings(self, game):
        r = game.r
        if not self.build or game.day == self.checked_build or self.days_left(game) < r["bot_end_buy_days"]:
            return
        self.checked_build = game.day
        horizon = min(r["bot_building_payback_days"], self.days_left(game) / 2)
        best = None
        for p in game.visited:
            if len(game.built[p]) >= game.slots(p):
                continue
            for name, b in game.d.buildings.items():
                if name in game.built[p]:
                    continue
                net = self.building_gain(game, p, b) - b["upkeep_per_month"] / r["days_per_month"]
                if net <= 0 or b["cost"] > net * horizon or game.gold < b["cost"] + reserve(game):
                    continue
                if best is None or net / b["cost"] > best[0]:
                    best = (net / b["cost"], p, name)
        if best:
            game.build(best[1], best[2])

    def shop(self, game, ship):
        r = game.r
        if not game.d.ports[ship.port]["shipyard"] or self.days_left(game) < r["bot_end_buy_days"]:
            return
        rate = self.ship_day_profit(game)
        if self.expand_with and rate and len(game.fleet) < r["bot_max_fleet"]:
            kinds = list(game.d.ships) if self.expand_with == "Mixed" else [self.expand_with]
            horizon = min(r["bot_ship_payback_days"], (self.days_left(game) - 20) / 2)
            affordable = [k for k in kinds
                          if game.gold >= game.d.ships[k]["price"] + reserve(game, game.d.ships[k]["capacity"])
                          and game.d.ships[k]["price"] <= rate * horizon]
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
        self.manage_buildings(game)
        game.repair(ship, below=0.8)
        self.shop(game, ship)
        best = None
        for dest, _ in game.reach(ship):
            if self.allowed and tuple(sorted((ship.port, dest))) not in self.allowed:
                continue
            days = game.voyage_days(ship, dest)
            found = plan_cargo(game, ship, dest, days, best[0] if best else -math.inf)
            if found is None:
                continue
            plan, profit, lost, trip_days = found
            score = profit / trip_days
            if best is None or score > best[0]:
                best = (score, dest, plan, lost)
        score = best[0] if best else 0.0
        self.rate = max(score, 0.0) if self.rate is None else self.rate + 0.02 * (max(score, 0.0) - self.rate)
        if best and best[0] > 0:
            for g, n in best[2].items():
                game.buy(ship, g, n)
            for p, gold in best[3].items():
                game.strain_today[p] += gold
            game.sail(ship, best[1])
        elif self.allowed and best:
            game.sail(ship, best[1])             # head back along the route empty
        else:
            ship.free_at = game.now + 1          # wait a day for prices to move


class FixedRoute(Greedy):
    """Takes its free routes, picks the best-looking pair of ports (one with a shipyard) on day 1,
    and keeps every ship on it, adding sloops as it can. Never buys more routes."""
    def __init__(self, expand_with="Sloop"):
        super().__init__(expand_with=expand_with, expand_routes=False)
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
                    shared = game.d.shared[(x, y)]
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
        reachable = [b for b, _ in game.reach(ship)]
        if not reachable:
            ship.free_at = game.now + 1
            return
        dest = game.rng.choice(reachable)
        shared = game.d.shared[(ship.port, dest)]
        for _ in range(int(ship.capacity * 2)):
            if not shared:
                break
            g = game.rng.choice(shared)
            if ship.room() >= game.d.space[g] and game.buy_price(ship.port, g) <= game.gold - 200:
                game.buy(ship, g, 1)
        game.sail(ship, dest)


# -------------------------------------------------------------- report ----

def rank_openings(data):
    """Every valid pair of free routes from the start port, best (for one sloop) first."""
    start = data.rules["start_port"]
    sloop = data.ships[data.rules["start_ship"]]
    options = {}
    for a in data.lanes:
        if start not in (a["a"], a["b"]):
            continue
        for b in data.lanes:
            if b["id"] != a["id"] and ({b["a"], b["b"]} & {start, a["a"], a["b"]}):
                key = frozenset((a["id"], b["id"]))
                options[key] = (data.network_income(key, sloop["capacity"] * sloop["speed"], sloop["ocean"]),
                                [a["id"], b["id"]])
    return sorted(options.values(), key=lambda o: -o[0])


def lane_name(data, i):
    return f"{data.lanes[i]['a']}-{data.lanes[i]['b']}"


def make_bot(spec):
    spec = dict(spec)
    kind = spec.pop("kind", "greedy")
    return {"greedy": Greedy, "fixed": FixedRoute, "random": RandomPlayer}[kind](**spec)


_DATA = {}


def _run_chunk(task):
    name, spec, seed, lo, hi = task
    spec = dict(spec)
    spec.pop("share", None)
    data = _DATA["calm" if spec.pop("no_hurricanes", False) else "normal"]
    return name, lo, [Game(data, random.Random(seed * 100003 + i)).run(make_bot(spec)).result()
                      for i in range(lo, hi)]


def simulate_all(data, specs, runs, seed, jobs):
    """Run every (name, spec) bot for `runs` sessions, spread over `jobs` processes. Returns name -> results.
    A spec with "share" below 1 runs only that share of the sessions (for comparisons that don't need many)."""
    _DATA["normal"] = data
    _DATA["calm"] = Data(data.mode, hurricanes=False)
    chunk = max(1, min(10, runs // (jobs * 3) or 1))
    tasks = []
    for name, spec in specs:
        n = max(1, round(runs * spec.get("share", 1.0)))
        tasks += [(name, spec, seed, lo, min(n, lo + chunk)) for lo in range(0, n, chunk)]
    if jobs > 1:
        with multiprocessing.get_context("fork").Pool(jobs) as pool:
            done = list(pool.imap_unordered(_run_chunk, tasks))
    else:
        done = [_run_chunk(t) for t in tasks]
    out = defaultdict(list)
    for name, lo, res in sorted(done, key=lambda x: (x[0], x[1])):
        out[name].extend(res)
    return out


def summarize(name, games, data):
    wins = [g["win_day"] for g in games if g["win_day"] is not None]
    final = sorted(g["gold"] for g in games)
    trips, arrivals, sea, cargo = Counter(), Counter(), Counter(), Counter()
    for g in games:
        trips.update(g["trips"])
        arrivals.update(g["arrivals"])
        sea.update(g["sea_days"])
        cargo.update(g["cargo"])
    total_trips = sum(trips.values()) or 1
    total_arrivals = sum(arrivals.values()) or 1
    top_route, top_n = trips.most_common(1)[0] if trips else (("-", "-"), 0)

    def hurricane_share(period):
        on, off = sea[(period, True)], sea[(period, False)]
        return on / (on + off) if on + off else None

    periods = sorted({k[0] for k in sea})
    upgrades = Counter()
    for g in games:
        upgrades.update(g["upgrades"])
    n = len(games)
    limit = int(data.rules["day_limit"])
    marks = [int(limit * f) for f in (0.25, 0.5, 0.75, 1.0)]
    return {
        "bot": name,
        "win %": 100 * len(wins) / n,
        "win day": statistics.median(wins) if wins else None,
        "gold end": statistics.median(final),
        "mean gold": statistics.mean(final),
        "p10 gold": final[n // 10],
        "voyages": total_trips / n,
        "sunk": sum(g["sunk"] for g in games) / n,
        "top route %": 100 * top_n / total_trips,
        "atlantic %": 100 * sum(g["atlantic"] for g in games) / total_trips,
        "min port %": 100 * min(arrivals.get(p, 0) for p in data.ports) / total_arrivals,
        "quiet ports": sorted((100 * arrivals.get(p, 0) / total_arrivals, p) for p in data.ports)[:3],
        "top route": " - ".join(top_route),
        "hurricane": {p: hurricane_share(p) for p in periods},
        "upgrades": {k: v / n for k, v in upgrades.items()},
        "gold by day": {d: statistics.median(g["history"][min(d, len(g["history"])) - 1] for g in games) for d in marks},
        "routes by day": {d: statistics.mean(sum(1 for day, _ in g["routes"] if day <= d) for g in games)
                          for d in (1, limit // 8, limit // 4, limit // 2, limit)},
        "first routes": Counter(" + ".join(sorted(l for _, l in g["routes"][:2])) for g in games).most_common(3),
        "ships bought": {k: sum(g["ships_bought"][k] for g in games) / n for k in data.ships},
        "fleet": statistics.mean(g["fleet"] for g in games),
        "buildings": statistics.mean(g["buildings"] for g in games),
        "upkeep": statistics.mean(g["upkeep"] for g in games),
        "ports lv2+": statistics.mean(sum(1 for v in g["levels"].values() if v >= 2) for g in games),
        "ports grown": statistics.mean(sum(1 for v in g["levels"].items() if v[1] > data.ports[v[0]]["level"])
                                       for g in games),
        "ports lv4+": statistics.mean(sum(1 for v in g["levels"].values() if v >= 4) for g in games),
        "ports declined": statistics.mean(sum(1 for lv in g["level_log"] if lv[3] < lv[2]) for g in games),
        "spoiled": statistics.mean(g["spoiled"] for g in games),
        "cargo": cargo,
    }


def print_table(results):
    cols = ["bot", "win %", "win day", "gold end", "mean gold", "p10 gold", "voyages", "sunk",
            "fleet", "top route %", "atlantic %", "min port %"]
    widths = [max(len(c), 9) if c != "bot" else 26 for c in cols]
    print("  ".join(c.rjust(w) for c, w in zip(cols, widths)))
    for r in results:
        cells = []
        for c, w in zip(cols, widths):
            v = r[c]
            text = "-" if v is None else (f"{v:,.1f}" if isinstance(v, float) else str(v))
            cells.append(text.rjust(w))
        print("  ".join(cells))


def share(res, kinds, goods):
    """Share of the hold space a fleet loaded that went to these goods (None if it loaded nothing)."""
    total = sum(v for (k, g), v in res["cargo"].items() if k in kinds)
    part = sum(v for (k, g), v in res["cargo"].items() if k in kinds and g in goods)
    return part / total if total else None


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--runs", type=int, default=100)
    ap.add_argument("--seed", type=int, default=1)
    ap.add_argument("--mode", choices=["veteran", "beginner"], default="veteran")
    ap.add_argument("--jobs", type=int, default=min(4, os.cpu_count() or 1), help="processes to use")
    ap.add_argument("--tracks", action="store_true", help="also run the single-track upgrade bots")
    args = ap.parse_args()
    t0 = time.time()
    data = Data(args.mode)
    R = data.rules
    veteran = args.mode == "veteran"
    print(f"Mode: {args.mode}  (goal {R['goal_gold']:,.0f} gold, {R['day_limit']:.0f} days, "
          f"start gold {R['start_gold']:,.0f}, {args.runs} sessions per bot, seed {args.seed})")
    print()
    print("Strongest trades before any trading (gold per crate, and per day for a ship of speed 1):")
    trades = []
    for (a, b), route in data.routes.items():
        for g in data.market[a]:
            if g in data.market[b]:
                m = (data.goods[g] * (data.market[b][g] * (1 - data.spread_at(data.ports[b]["level"]) / 2)
                                      - data.market[a][g] * (1 + data.spread_at(data.ports[a]["level"]) / 2)))
                if m > 0:
                    trades.append((m / (route["distance"] + R["port_time_days"]), m, g, a, b))
    for per_day, m, g, a, b in sorted(trades, reverse=True)[:10]:
        print(f"  {g:22} {a:>15} -> {b:<15} +{m:4.0f}/crate  {per_day:5.1f}/day")
    print()

    run = lambda specs: {n: summarize(n, g, data) for n, g in simulate_all(data, specs, args.runs, args.seed, args.jobs).items()}
    stage1 = [("Random", {"kind": "random"}), ("Fixed route", {"kind": "fixed"}), ("Greedy, 1 ship", {}),
              ("Fleet of Sloops", {"expand_with": "Sloop"}), ("Fleet of Brigs", {"expand_with": "Brig"}),
              ("Fleet of Fluyts", {"expand_with": "Fluyt"}), ("Mixed fleet", {"expand_with": "Mixed"})]
    first = run(stage1)
    fleet = [first[n] for n in ("Fleet of Sloops", "Fleet of Brigs", "Fleet of Fluyts", "Mixed fleet")]
    best = max(fleet, key=lambda r: r["mean gold"])
    kind = "Mixed" if best["bot"] == "Mixed fleet" else best["bot"].split()[-1][:-1]
    label = "Mixed" if kind == "Mixed" else f"{kind}s"
    openings = rank_openings(data)[:3] if veteran else []
    stage2 = [(f"{label}, free routes only", {"expand_with": kind, "expand_routes": False}),
              (f"{label} + all upgrades", {"expand_with": kind, "upgrades": TRACK_ORDER}),
              (f"{label} + buildings", {"expand_with": kind, "build": True}),
              (f"{label} + Hull only", {"expand_with": kind, "upgrades": ["Hull"], "share": R["sim_side_run_share"]}),
              (f"{label}, no hurricanes", {"expand_with": kind, "no_hurricanes": True, "share": R["sim_side_run_share"]})]
    if not veteran:
        # Beginner's "simple plan": one route, the best ship type, keep adding ships.
        stage2.append((f"Fixed route, {label}", {"kind": "fixed", "expand_with": kind}))
    if veteran:
        stage2 += [(f"{label}, opening {i + 1}", {"expand_with": kind, "opening": o, "share": R["sim_side_run_share"]}) for i, (_, o) in enumerate(openings)]
    if args.tracks:
        stage2 += [(f"{label} + {t} only", {"expand_with": kind, "upgrades": [t]}) for t in TRACK_ORDER if t != "Hull"]
    second = run(stage2)
    by = {**first, **second}
    results = [by[n] for n, _ in stage1 + stage2 if "no hurricanes" not in n]
    print_table(results)
    print()
    h = best["hurricane"]
    print(f"Share of sea days on hurricane lanes for '{best['bot']}': " +
          ", ".join(f"{p} {100 * v:.0f}%" for p, v in sorted(h.items()) if v is not None))
    print(f"Most-used route for '{best['bot']}': {best['top route']}")
    print(f"Routes owned by '{best['bot']}' (of {len(data.lanes)}): " +
          ", ".join(f"day {d} {v:.1f}" for d, v in best["routes by day"].items()))
    print(f"Most common free picks for '{best['bot']}': " +
          "; ".join(f"{pick} ({100 * n / args.runs:.0f}%)" for pick, n in best["first routes"]))
    print(f"Median gold for '{best['bot']}' by day: " +
          ", ".join(f"day {d} {v:,.0f}" for d, v in best["gold by day"].items()))
    print(f"Least-visited ports for '{best['bot']}': " +
          ", ".join(f"{p} {s:.1f}%" for s, p in best["quiet ports"]))
    mixed = by["Mixed fleet"]
    print("Ships bought per session by 'Mixed fleet': " +
          ", ".join(f"{k} {v:.1f}" for k, v in mixed["ships bought"].items()))
    up = by[f"{label} + all upgrades"]
    print(f"Upgrades bought per session by '{up['bot']}': " +
          ", ".join(f"{k} {v:.1f}" for k, v in sorted(up["upgrades"].items())))
    bld = by[f"{label} + buildings"]
    print(f"'{bld['bot']}' builds {bld['buildings']:.1f} buildings per session, paying {bld['upkeep']:,.0f} upkeep; "
          f"bought: " + ", ".join(f"{k.replace('building ', '')} {v:.1f}" for k, v in sorted(bld["upgrades"].items())
                                  if k.startswith("building")))
    print(f"Ports for '{best['bot']}' at the end: {best['ports lv2+']:.1f} at level 2+, {best['ports lv4+']:.1f} at "
          f"level 4+, {best['ports grown']:.1f} grew above their start level, {best['ports declined']:.1f} declines")
    print()
    (check_veteran if veteran else check_beginner)(by, best, label, R, args.tracks, data)
    print(f"\nRun time: {time.time() - t0:.0f} s")


def targets_common(by, best, label, R, data):
    """Targets both modes share. Returns a list of (text, ok, required)."""
    limit = R["day_limit"]
    top = best["mean gold"]
    bld = by[f"{label} + buildings"]
    h = best["hurricane"]
    calm_world = by[f"{label}, no hurricanes"]["hurricane"]
    peaks = {p[4:]: v for p, v in h.items() if p.startswith("peak") and v is not None}
    base = {p[4:]: v for p, v in calm_world.items() if p.startswith("peak") and v is not None}
    calms = {p[4:]: v for p, v in h.items() if p.startswith("calm") and v is not None}
    both = sorted(set(peaks) & set(base))
    testable = len(both) >= 2 and all(base[p] >= 0.05 for p in both)
    veteran = data.mode == "veteran"
    out = [
        (f"Port buildings pay off (a bot that builds earns >= 5% more than one that doesn't; "
         f"{100 * (bld['mean gold'] / top - 1):+.1f}%)", bld["mean gold"] >= 1.05 * top, True),
        (f"Ports grow (a good player's network ends with >= 4 ports at level 2+: {best['ports lv2+']:.1f}; "
         f"{best['ports grown']:.1f} grew above their start level)", best["ports lv2+"] >= 4, False),
        ("Seasons matter every year (in each season's peak the fleet spends >= 20% less time on hurricane lanes than "
         "the same fleet does on the same days with no hurricanes: " +
         ", ".join(f"season {p} {100 * peaks[p]:.0f}% vs {100 * base[p]:.0f}%" for p in both) +
         "; for reference, the calm months after each season: " +
         ", ".join(f"{100 * v:.0f}%" for _, v in sorted(calms.items())) + ")" +
         ("" if testable else " [the fleet never sails hurricane lanes in either world, so this can't show anything]"),
         testable and all(peaks[p] <= 0.8 * base[p] for p in both), veteran),
    ]
    return out


def show(title, checks):
    print(title)
    for text, ok, required in checks:
        print(f"  [{'PASS' if ok else 'FAIL'}]{'' if required else ' (info)'} {text}")
    req = [ok for _, ok, required in checks if required]
    print(f"{sum(req)} of {len(req)} required targets pass; {sum(1 for _, ok, r in checks if not r and ok)} of "
          f"{sum(1 for *_, r in checks if not r)} informational pass.")


def check_veteran(by, best, label, R, tracks, data):
    stay = by[f"{label}, free routes only"]
    hull = by[f"{label} + Hull only"]
    upgraded = by[f"{label} + all upgrades"]
    limit = R["day_limit"]
    base = best["mean gold"]
    openings = [by[n] for n in by if ", opening " in n]
    sunk_cap = 1.0 * limit / 90      # version 1 allowed 1 loss in 90 days
    checks = [
        ("A good player usually wins (best fleet bot wins >= 85%)", best["win %"] >= 85, True),
        (f"...but not too early (its median win day is {0.65 * limit:.0f}-{0.9 * limit:.0f}: 65-90% of the mission; "
         f"{best['win day']})", best["win day"] is not None and 0.65 * limit <= best["win day"] <= 0.9 * limit, True),
        ("Growing the fleet matters (1-ship greedy wins <= 50%)", by["Greedy, 1 ship"]["win %"] <= 50, True),
        ("Putting a fleet on one route fails (fixed-route bot wins <= 20%)", by["Fixed route"]["win %"] <= 20, True),
        ("Ignoring prices fails (random bot wins <= 2%)", by["Random"]["win %"] <= 2, True),
        ("No route dominates (best fleet bot's top route <= 30% of voyages)", best["top route %"] <= 30, True),
        ("Big ships aren't a trap (mixed fleet earns >= 90% of an all-sloop fleet)",
         by["Mixed fleet"]["mean gold"] >= 0.9 * by["Fleet of Sloops"]["mean gold"], True),
        (f"Hull upgrades make sinking rare (<= {sunk_cap:.0f} per session, at most half the unprotected rate; "
         f"{hull['sunk']:.1f} vs {best['sunk']:.1f})", hull["sunk"] <= sunk_cap and hull["sunk"] <= 0.5 * best["sunk"], True),
        ("All 12 ports matter (each gets >= 2% of the best bot's arrivals)", best["min port %"] >= 2, True),
        ("The Atlantic matters (>= 15% of the best bot's voyages cross it)", best["atlantic %"] >= 15, True),
        ("Opening routes matters (a fleet that keeps only its free routes earns <= 60% of one that expands; "
         f"{100 * stay['mean gold'] / base:.0f}%)", stay["mean gold"] <= 0.6 * base, True),
        ("The opening isn't solved (the 2nd-best free pick earns within 10% of the best)",
         len(openings) == 3 and sorted((r["mean gold"] for r in openings), reverse=True)[1]
         >= 0.9 * max(r["mean gold"] for r in openings), True),
        (f"Upgrades pay off (all upgrades: >= 10% more average gold; {100 * (upgraded['mean gold'] / base - 1):+.0f}%)",
         upgraded["mean gold"] >= 1.10 * base, True),
    ]
    if tracks:
        singles = [by[f"{label} + {t} only"] for t in TRACK_ORDER if t != "Hull"] + [hull]
        gains = [r["mean gold"] / base - 1 for r in singles]
        checks += [
            ("Every upgrade track is worth buying (each alone: >= 3% more average gold): " +
             ", ".join(f"{r['bot'].split('+ ')[1]} {100 * g:+.0f}%" for r, g in zip(singles, gains)), min(gains) >= 0.03, True),
            ("No track is a must-have (best single track gains <= 3x the worst)",
             min(gains) > 0 and max(gains) <= 3 * min(gains), True)]
    checks += targets_common(by, best, label, R, data)
    # Veteran's cargo rules: informational.
    sl, fl = ["Sloop"], ["Fluyt"]
    flour = [g for g in data.goods if data.perishable[g]]
    bulk = [g for g in data.goods if data.space[g] >= 2]
    fs, ff = share(by["Fleet of Sloops"], sl, flour), share(by["Fleet of Fluyts"], fl, flour)
    bs, bf = share(by["Fleet of Sloops"], sl, bulk), share(by["Fleet of Fluyts"], fl, bulk)
    ok = None not in (fs, ff, bs, bf) and ff <= fs and bf >= bs
    checks.append((f"Cargo rules matter (Flour is a smaller share of a Fluyt fleet's holds than a Sloop fleet's: "
                   f"{100 * (ff or 0):.1f}% vs {100 * (fs or 0):.1f}%; bulk goods are a bigger share: "
                   f"{100 * (bf or 0):.0f}% vs {100 * (bs or 0):.0f}%)", ok, False))
    if not tracks:
        print("(Single-track upgrade targets are skipped: run with --tracks to include them.)")
    show("Veteran balance targets:", checks)


def check_beginner(by, best, label, R, tracks, data):
    stay = by[f"{label}, free routes only"]
    hull = by[f"{label} + Hull only"]
    upgraded = by[f"{label} + all upgrades"]
    limit = R["day_limit"]
    base = best["mean gold"]
    sunk_cap = 1.0 * limit / 90
    bld = by[f"{label} + buildings"]
    simple = by[f"Fixed route, {label}"]
    checks = [
        ("B1: best fleet bot wins >= 95%", best["win %"] >= 95, True),
        (f"B2: its median win day is {0.65 * limit:.0f}-{0.9 * limit:.0f} (65-90% of the mission; {best['win day']})",
         best["win day"] is not None and 0.65 * limit <= best["win day"] <= 0.9 * limit, True),
        (f"B3: a simple plan works (one route, the best ship type, keep adding ships) wins >= 50% "
         f"({simple['win %']:.0f}%)", simple["win %"] >= 50, True),
        ("B4: random bot wins <= 5%", by["Random"]["win %"] <= 5, True),
        ("B5: 1-ship greedy wins <= 80%", by["Greedy, 1 ship"]["win %"] <= 80, True),
        (f"B6 (info only): free-routes-only bot earns <= 70% of the best fleet bot's gold "
         f"({100 * stay['mean gold'] / base:.0f}%)", stay["mean gold"] <= 0.7 * base, False),
        (f"B7: all upgrades give >= 10% more average gold ({100 * (upgraded['mean gold'] / base - 1):+.0f}%)",
         upgraded["mean gold"] >= 1.10 * base, True),
        (f"B8: Hull-only bot loses <= {sunk_cap:.0f} ships per session ({hull['sunk']:.1f})", hull["sunk"] <= sunk_cap, True),
        (f"B9: port development matters (a bot that builds, with ports that grow, earns >= 5% more than one that "
         f"doesn't; {100 * (bld['mean gold'] / base - 1):+.1f}%)", bld["mean gold"] >= 1.05 * base, True),
    ]
    checks += targets_common(by, best, label, R, data)[1:]
    show("Beginner balance targets:", checks)


if __name__ == "__main__":
    main()
