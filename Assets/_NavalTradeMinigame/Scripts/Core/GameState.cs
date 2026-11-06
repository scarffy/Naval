using System;
using System.Collections.Generic;

namespace Naval.Core
{
    public enum GameOutcome { InProgress, Won, Lost }

    /// <summary>
    /// The whole game: gold, fleet, routes, calendar and the random source. The UI calls the command
    /// methods, each of which returns a <see cref="CommandResult"/>, and calls <see cref="AdvanceDay"/> to let time pass.
    /// </summary>
    public sealed class GameState
    {
        const int PreviewTrials = 400;
        const int PreviewSeed = 4711;

        readonly Random rng;
        readonly List<Ship> fleet = new List<Ship>();
        readonly List<GameEvent> pending = new List<GameEvent>();
        int nextShipId = 1;
        bool fleetChosen;

        public GameData Data { get; private set; }
        public GameCalendar Calendar { get; private set; }
        public Market Market { get; private set; }
        public Network Routes { get; private set; }
        public int Gold { get; private set; }
        /// <summary>Days that have passed since the start. Day 0 is the first morning.</summary>
        public int Day { get; private set; }
        public GameOutcome Outcome { get; private set; }
        /// <summary>True once the opening is over: the fleet was chosen with ChooseFleet, or a ship was bought.</summary>
        public bool FleetChosen { get { return fleetChosen; } }
        /// <summary>Why the mission was lost, or null while it is not lost.</summary>
        public FailReason? LostBecause { get; private set; }

        /// <summary>
        /// While true, voyages started by Send are sheltered (they meet no hazards) for their whole trip,
        /// even if this is turned off on the way. Preview is not affected.
        /// </summary>
        public bool ShelterNewVoyages { get; set; }

        public GameState(GameData data) : this(data, data.Rules.DefaultSeed) { }

        public GameState(GameData data, int seed)
        {
            Data = data;
            Calendar = new GameCalendar(data.Rules);
            Market = new Market(data);
            Routes = new Network(data);
            rng = new Random(seed);
            Gold = data.Rules.StartGold;
        }

        public IReadOnlyList<Ship> Fleet { get { return fleet; } }
        public DateTime Date { get { return Calendar.DateOf(Day); } }
        public int DaysLeft { get { return Math.Max(0, Data.Rules.MissionDays - Day); } }
        /// <summary>Hurricane season strength today, 0 to 1.</summary>
        public double Season { get { return Calendar.Season(Date); } }

        public int BuyPrice(Port port, Good good)
        {
            return Market.BuyPrice(port, good, Season);
        }

        public int SellPrice(Port port, Good good)
        {
            return Market.SellPrice(port, good, Season);
        }

        public int RepairCost(Ship ship)
        {
            return Num.Round((ship.MaxHull - ship.Hull) * Data.Rules.RepairCostPerHull * ship.RepairMultiplier);
        }

        public int UpgradeCost(Ship ship, UpgradeTrack track)
        {
            UpgradeLevel next = Data.FindUpgrade(track, ship.Level(track) + 1);
            return next == null ? 0 : Num.Round(next.CostFraction * ship.Tier.Price);
        }

        /// <summary>What opening this route costs now: 0 while free routes remain.</summary>
        public int RouteCost(Route route)
        {
            return Routes.CostOf(route);
        }

        // ---- trading

        public CommandResult BuyGoods(Ship ship, string goodName, int crates)
        {
            CommandResult bad = CheckInPort(ship);
            if (bad != null) return bad;
            Good good = Data.FindGood(goodName);
            if (good == null) return CommandResult.Fail(Failure.UnknownName, "Unknown good '" + goodName + "'.");
            if (crates <= 0) return CommandResult.Fail(Failure.InvalidAmount, "Choose at least one crate.");
            if (!Market.Trades(ship.Port, good)) return CommandResult.Fail(Failure.NotTraded, ship.Port.Name + " does not trade " + good.Name + ".");
            if (crates > ship.FreeSpace) return CommandResult.Fail(Failure.HoldFull, "The hold has room for only " + ship.FreeSpace + " more crates.");
            int cost = crates * BuyPrice(ship.Port, good);
            bad = CheckAfford(cost);
            if (bad != null) return bad;
            Gold -= cost;
            ship.AddCargo(good, crates);
            return CommandResult.Success("Bought " + crates + " " + good.Name + " for " + cost + " gold.");
        }

        public CommandResult SellGoods(Ship ship, string goodName, int crates)
        {
            CommandResult bad = CheckInPort(ship);
            if (bad != null) return bad;
            Good good = Data.FindGood(goodName);
            if (good == null) return CommandResult.Fail(Failure.UnknownName, "Unknown good '" + goodName + "'.");
            if (crates <= 0) return CommandResult.Fail(Failure.InvalidAmount, "Choose at least one crate.");
            if (!Market.Trades(ship.Port, good)) return CommandResult.Fail(Failure.NotTraded, ship.Port.Name + " does not trade " + good.Name + ".");
            if (crates > ship.CargoOf(good)) return CommandResult.Fail(Failure.NotEnoughCargo, "The ship carries only " + ship.CargoOf(good) + " " + good.Name + ".");
            int income = crates * SellPrice(ship.Port, good);
            Gold += income;
            ship.RemoveCargo(good, crates);
            CheckGoal();
            return CommandResult.Success("Sold " + crates + " " + good.Name + " for " + income + " gold.");
        }

        // ---- ships

        public CommandResult BuyShip(string portName, string tierName, out Ship ship)
        {
            ship = null;
            if (Outcome != GameOutcome.InProgress) return CommandResult.Fail(Failure.GameOver, "The mission is over.");
            Port port = Data.FindPort(portName);
            ShipTier tier = Data.FindShipTier(tierName);
            if (port == null) return CommandResult.Fail(Failure.UnknownName, "Unknown port '" + portName + "'.");
            if (tier == null) return CommandResult.Fail(Failure.UnknownName, "Unknown ship '" + tierName + "'.");
            if (!port.HasShipyard) return CommandResult.Fail(Failure.NotAtShipyard, port.Name + " has no shipyard.");
            if (!Routes.Reaches(port))
                return CommandResult.Fail(Failure.NotConnected, "None of your routes reach " + port.Name + ", so a ship bought there could not sail.");
            CommandResult poor = CheckAfford(tier.Price);
            if (poor != null) return poor;
            Gold -= tier.Price;
            ship = new Ship(Data, nextShipId++, tier, port);
            fleet.Add(ship);
            fleetChosen = true;
            return CommandResult.Success("Bought a " + tier.Name + " for " + tier.Price + " gold.");
        }

        /// <summary>The opening purchase: as many ships of one tier as FleetBudget buys (one Merchantman, two Brigs or three Sloops), at the start port. Only once, before any ship is bought.</summary>
        public CommandResult ChooseFleet(string tierName)
        {
            if (Outcome != GameOutcome.InProgress) return CommandResult.Fail(Failure.GameOver, "The mission is over.");
            if (fleetChosen) return CommandResult.Fail(Failure.FleetChosen, "The fleet has already been chosen.");
            ShipTier tier = Data.FindShipTier(tierName);
            if (tier == null) return CommandResult.Fail(Failure.UnknownName, "Unknown ship '" + tierName + "'.");
            int count = Data.FleetCount(tier);
            int total = count * tier.Price;
            CommandResult poor = CheckAfford(total);
            if (poor != null) return poor;
            for (int i = 0; i < count; i++) fleet.Add(new Ship(Data, nextShipId++, tier, Data.StartPort));
            Gold -= total;
            fleetChosen = true;
            return CommandResult.Success("Bought " + (count == 1 ? "a " + tier.Name : count + " " + tier.Name + "s") + " for " + total + " gold.");
        }

        /// <summary>Sells a ship at a shipyard for a fraction of the tier's price. The hold must be empty.</summary>
        public CommandResult SellShip(Ship ship)
        {
            CommandResult bad = CheckAtShipyard(ship);
            if (bad != null) return bad;
            if (ship.CargoCount > 0) return CommandResult.Fail(Failure.HoldNotEmpty, "Sell the cargo first.");
            int income = Num.Round(ship.Tier.Price * Data.Rules.ShipResaleFraction);
            Gold += income;
            fleet.Remove(ship);
            CheckGoal();
            return CommandResult.Success("Sold the " + ship.Tier.Name + " for " + income + " gold.");
        }

        public CommandResult Upgrade(Ship ship, UpgradeTrack track)
        {
            CommandResult bad = CheckAtShipyard(ship);
            if (bad != null) return bad;
            UpgradeLevel next = Data.FindUpgrade(track, ship.Level(track) + 1);
            if (next == null) return CommandResult.Fail(Failure.MaxUpgrade, "The " + track + " upgrades are complete.");
            int cost = UpgradeCost(ship, track);
            bad = CheckAfford(cost);
            if (bad != null) return bad;
            Gold -= cost;
            ship.Upgrade(track);
            return CommandResult.Success(next.Name + " fitted for " + cost + " gold.");
        }

        /// <summary>Repairs the hull to full in any port.</summary>
        public CommandResult Repair(Ship ship)
        {
            CommandResult bad = CheckInPort(ship);
            if (bad != null) return bad;
            if (ship.Hull >= ship.MaxHull) return CommandResult.Fail(Failure.NothingToRepair, "The hull is sound.");
            int cost = RepairCost(ship);
            bad = CheckAfford(cost);
            if (bad != null) return bad;
            Gold -= cost;
            ship.Hull = ship.MaxHull;
            return CommandResult.Success("Repaired for " + cost + " gold.");
        }

        // ---- routes

        public CommandResult OpenRoute(Route route)
        {
            if (Outcome != GameOutcome.InProgress) return CommandResult.Fail(Failure.GameOver, "The mission is over.");
            if (route == null) return CommandResult.Fail(Failure.UnknownName, "Unknown route.");
            if (Routes.Owns(route)) return CommandResult.Fail(Failure.AlreadyOwned, "You already own this route.");
            if (!Routes.CanConnect(route)) return CommandResult.Fail(Failure.NotConnected, "A new route must connect to your network.");
            int cost = Routes.CostOf(route);
            CommandResult poor = CheckAfford(cost);
            if (poor != null) return poor;
            Gold -= cost;
            Routes.Add(route);
            return CommandResult.Success("Route opened for " + cost + " gold.");
        }

        public RoutePreview Preview(Ship ship, string destinationName)
        {
            Port destination;
            List<Route> legs;
            CommandResult bad = PlanVoyage(ship, destinationName, out destination, out legs);
            if (bad != null) return new RoutePreview { Status = bad };

            // Walk the trip once without dice for the arrival date and the risks.
            var seasons = new List<double>();
            Voyage trip = NewVoyage(ship, legs);
            double noPirates = 1, noHurricane = 1;
            int days = 0;
            while (!trip.Arrived)
            {
                DayOdds odds = trip.NextDay(SeasonOnDay(seasons, days));
                noPirates *= 1 - odds.Pirates;
                noHurricane *= 1 - odds.Hurricane;
                days++;
            }

            // Then sail a copy of the ship through the same daily rules many times with a fixed seed.
            var dice = new Random(PreviewSeed);
            int lost = 0;
            for (int trial = 0; trial < PreviewTrials; trial++)
            {
                Ship copy = ship.Clone();
                copy.Voyage = NewVoyage(ship, legs);
                for (int day = 0; !copy.Voyage.Arrived; day++)
                {
                    if (copy.SailDay(SeasonOnDay(seasons, day), dice, Data.Rules).Sunk)
                    {
                        lost++;
                        break;
                    }
                }
            }

            return new RoutePreview
            {
                Status = CommandResult.Success("Arrives " + Date.AddDays(days).ToString("d MMM yyyy")),
                Stops = trip.Stops,
                Distance = trip.Distance,
                Days = days,
                ArrivalDate = Date.AddDays(days),
                PirateRiskPercent = 100 * (1 - noPirates),
                HurricaneRiskPercent = 100 * (1 - noHurricane),
                LossChancePercent = 100.0 * lost / PreviewTrials
            };
        }

        public CommandResult Send(Ship ship, string destinationName)
        {
            Port destination;
            List<Route> legs;
            CommandResult bad = PlanVoyage(ship, destinationName, out destination, out legs);
            if (bad != null) return bad;
            ship.Voyage = NewVoyage(ship, legs, ShelterNewVoyages);
            ship.Port = null;
            return CommandResult.Success("Set sail for " + destination.Name + ".");
        }

        // ---- time

        /// <summary>Lets one day pass and returns everything that happened, including events left over from commands.</summary>
        public IReadOnlyList<GameEvent> AdvanceDay()
        {
            if (Outcome == GameOutcome.InProgress) Step();
            return TakeEvents();
        }

        /// <summary>Events raised since the last call (a command can end the game by reaching the goal).</summary>
        public IReadOnlyList<GameEvent> TakeEvents()
        {
            GameEvent[] events = pending.ToArray();
            pending.Clear();
            return events;
        }

        void Step()
        {
            double season = Calendar.Season(Date);   // the day being sailed
            Day++;                                    // events are stamped with the day that ends
            // Wages: full for a day at sea (the arrival day too), a fraction in port, nothing for a ship lost today.
            // The docked share is rounded once for the whole fleet, so a small fraction still adds up.
            double dockedWages = 0;
            foreach (Ship ship in fleet.ToArray())
            {
                if (ship.Voyage == null)
                {
                    dockedWages += ship.Tier.WagePerDay * Data.Rules.DockedWageFraction;
                    continue;
                }
                SailDay(ship, season);
                if (fleet.Contains(ship)) Gold -= ship.Tier.WagePerDay;
            }
            Gold -= Num.Round(dockedWages);

            CheckGoal();
            if (Outcome != GameOutcome.InProgress) return;
            if (Day >= Data.Rules.MissionDays) LoseMission(FailReason.OutOfTime);
            else if (fleet.Count == 0 && Gold < Data.CheapestShipPrice) LoseMission(FailReason.NoShips);
        }

        void SailDay(Ship ship, double season)
        {
            DayResult day = ship.SailDay(season, rng, Data.Rules);
            DayHazards h = day.Hazards;
            if (h.Storm)
            {
                GameEvent e = Raise(EventType.Storm, ship);
                e.HullDamage = h.StormDamage;
                e.DelayDays = h.StormDelay;
            }
            if (h.Pirates)
            {
                GameEvent e = Raise(EventType.PirateAttack, ship);
                e.HullDamage = h.PirateDamage;
                e.CratesLost = day.PirateCratesLost;
            }
            if (h.Hurricane)
            {
                GameEvent e = Raise(EventType.Hurricane, ship);
                e.HullDamage = h.HurricaneDamage;
                e.DelayDays = h.HurricaneDelay;
                e.CratesLost = day.HurricaneCratesLost;
            }
            if (day.Sunk)
            {
                fleet.Remove(ship);   // the cargo goes down with it
                Raise(EventType.ShipSunk, ship);
            }
            else if (ship.Voyage.Arrived)
            {
                ship.Port = ship.Voyage.Destination;
                ship.Voyage = null;
                Gold -= Data.Rules.PortFee;
                GameEvent e = Raise(EventType.ShipArrived, ship);
                e.Port = ship.Port;
                e.Dues = Data.Rules.PortFee;
            }
        }

        // ---- helpers

        void CheckGoal()
        {
            if (Outcome != GameOutcome.InProgress || Gold < Data.Rules.GoalGold) return;
            Outcome = GameOutcome.Won;
            Raise(EventType.GoalReached, null);
        }

        void LoseMission(FailReason reason)
        {
            Outcome = GameOutcome.Lost;
            LostBecause = reason;
            Raise(EventType.MissionFailed, null).Reason = reason;
        }

        GameEvent Raise(EventType type, Ship ship)
        {
            var e = new GameEvent { Type = type, Day = Day, Date = Date, Ship = ship };
            pending.Add(e);
            return e;
        }

        /// <summary>Season strength a given number of days from today, computed once per day offset.</summary>
        double SeasonOnDay(List<double> cache, int offset)
        {
            while (cache.Count <= offset) cache.Add(Calendar.Season(Date.AddDays(cache.Count)));
            return cache[offset];
        }

        Voyage NewVoyage(Ship ship, List<Route> legs, bool sheltered = false)
        {
            return new Voyage(ship.Port, legs, ship.Speed, Data.Rules.PortTimeDays, sheltered);
        }

        /// <summary>Null if the cost can be paid. In debt (gold below 0) nothing can be bought, even for free.</summary>
        CommandResult CheckAfford(int cost)
        {
            if (Gold < 0) return CommandResult.Fail(Failure.InDebt, "You are in debt (" + Gold + " gold) and cannot buy anything.");
            if (cost > Gold) return CommandResult.Fail(Failure.NotEnoughGold, "Not enough gold: " + cost + " needed.");
            return null;
        }

        /// <summary>Null if the game is running and the ship is one of ours, sitting in a port.</summary>
        CommandResult CheckInPort(Ship ship)
        {
            if (Outcome != GameOutcome.InProgress) return CommandResult.Fail(Failure.GameOver, "The mission is over.");
            if (ship == null || !fleet.Contains(ship)) return CommandResult.Fail(Failure.UnknownShip, "That ship is not in your fleet.");
            if (ship.Port == null) return CommandResult.Fail(Failure.ShipAtSea, "The ship is at sea.");
            return null;
        }

        CommandResult CheckAtShipyard(Ship ship)
        {
            CommandResult bad = CheckInPort(ship);
            if (bad != null) return bad;
            if (!ship.Port.HasShipyard) return CommandResult.Fail(Failure.NotAtShipyard, ship.Port.Name + " has no shipyard.");
            return null;
        }

        /// <summary>Checks a ship can sail to the destination and finds its legs. Null means it can.</summary>
        CommandResult PlanVoyage(Ship ship, string destinationName, out Port destination, out List<Route> legs)
        {
            destination = null;
            legs = null;
            CommandResult bad = CheckInPort(ship);
            if (bad != null) return bad;
            destination = Data.FindPort(destinationName);
            if (destination == null) return CommandResult.Fail(Failure.UnknownName, "Unknown port '" + destinationName + "'.");
            if (destination == ship.Port) return CommandResult.Fail(Failure.SamePort, "The ship is already in " + destination.Name + ".");
            legs = Routes.ShortestPath(ship.Port, destination, ship.Tier.OceanGoing);
            if (legs != null) return null;
            if (!ship.Tier.OceanGoing && Routes.ShortestPath(ship.Port, destination, true) != null)
                return CommandResult.Fail(Failure.NeedsOceanShip, "Only an ocean-going ship can sail there.");
            return CommandResult.Fail(Failure.NoRoute, "You own no route from " + ship.Port.Name + " to " + destination.Name + ".");
        }
    }
}
