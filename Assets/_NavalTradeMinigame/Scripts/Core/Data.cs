using System;
using System.Collections.Generic;

namespace Naval.Core
{
    public sealed class Good
    {
        public int Index { get; private set; }
        public string Name { get; private set; }
        public int BasePrice { get; private set; }

        public Good(int index, string name, int basePrice)
        {
            Index = index;
            Name = name;
            BasePrice = basePrice;
        }
    }

    public sealed class Port
    {
        public int Index { get; private set; }
        public string Name { get; private set; }
        public string Region { get; private set; }
        public bool HasShipyard { get; private set; }
        public bool InHurricaneZone { get; private set; }
        /// <summary>Position on the map: x runs west to east (0 to 100), y south to north (0 to 60).</summary>
        public double MapX { get; private set; }
        public double MapY { get; private set; }

        public Port(int index, string name, string region, bool hasShipyard, bool inHurricaneZone,
                    double mapX, double mapY)
        {
            Index = index;
            Name = name;
            Region = region;
            HasShipyard = hasShipyard;
            InHurricaneZone = inHurricaneZone;
            MapX = mapX;
            MapY = mapY;
        }
    }

    /// <summary>A sea route between two ports. The hazard values are chances per day at sea.</summary>
    public sealed class Route
    {
        public int Index { get; private set; }
        public Port A { get; private set; }
        public Port B { get; private set; }
        public double Distance { get; private set; }
        public double Storm { get; private set; }
        public double Pirates { get; private set; }
        public double Hurricanes { get; private set; }
        public int Price { get; private set; }
        public bool Ocean { get; private set; }

        public Route(int index, Port a, Port b, double distance, double storm, double pirates,
                     double hurricanes, int price, bool ocean)
        {
            Index = index;
            A = a;
            B = b;
            Distance = distance;
            Storm = storm;
            Pirates = pirates;
            Hurricanes = hurricanes;
            Price = price;
            Ocean = ocean;
        }

        public bool Touches(Port port)
        {
            return port == A || port == B;
        }

        public Port Other(Port port)
        {
            return port == A ? B : A;
        }
    }

    public sealed class ShipTier
    {
        public string Name { get; private set; }
        public int Price { get; private set; }
        public int Capacity { get; private set; }
        public double Speed { get; private set; }
        public int Hull { get; private set; }
        public int WagePerDay { get; private set; }
        public bool OceanGoing { get; private set; }

        public ShipTier(string name, int price, int capacity, double speed, int hull, int wagePerDay, bool oceanGoing)
        {
            Name = name;
            Price = price;
            Capacity = capacity;
            Speed = speed;
            Hull = hull;
            WagePerDay = wagePerDay;
            OceanGoing = oceanGoing;
        }
    }

    public enum UpgradeTrack { Sails, Hull, Hold }

    /// <summary>One upgrade level. The multipliers are the totals at this level, not on top of the level before.</summary>
    public sealed class UpgradeLevel
    {
        public static readonly int TrackCount = Enum.GetValues(typeof(UpgradeTrack)).Length;

        public UpgradeTrack Track { get; private set; }
        public int Level { get; private set; }
        public string Name { get; private set; }
        /// <summary>Cost as a fraction of the ship tier's price.</summary>
        public double CostFraction { get; private set; }
        public double SpeedMult { get; private set; }
        public double CapacityMult { get; private set; }
        public double HullMult { get; private set; }
        public double RepairMult { get; private set; }

        public UpgradeLevel(UpgradeTrack track, int level, string name, double costFraction,
                            double speedMult, double capacityMult, double hullMult, double repairMult)
        {
            Track = track;
            Level = level;
            Name = name;
            CostFraction = costFraction;
            SpeedMult = speedMult;
            CapacityMult = capacityMult;
            HullMult = hullMult;
            RepairMult = repairMult;
        }
    }

    public struct IntRange
    {
        public int Min;
        public int Max;

        public IntRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Roll(Random rng)
        {
            return rng.Next(Min, Max + 1);
        }
    }

    public struct FractionRange
    {
        public double Min;
        public double Max;

        public FractionRange(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double Roll(Random rng)
        {
            return Min + rng.NextDouble() * (Max - Min);
        }
    }

    public struct MonthDay
    {
        public int Month;
        public int Day;

        public MonthDay(int month, int day)
        {
            Month = month;
            Day = day;
        }

        public DateTime In(int year)
        {
            return new DateTime(year, Month, Day);
        }
    }

    /// <summary>Every number from rules.csv.</summary>
    public sealed class Rules
    {
        public int StartGold { get; internal set; }
        public int GoalGold { get; internal set; }
        public int MissionDays { get; internal set; }
        public DateTime StartDate { get; internal set; }
        public string StartPortName { get; internal set; }
        public int FreeRoutes { get; internal set; }
        public double Spread { get; internal set; }
        public int PortFee { get; internal set; }
        public double PortTimeDays { get; internal set; }
        public int RepairCostPerHull { get; internal set; }
        public double ShipResaleFraction { get; internal set; }

        public IntRange StormDelay { get; internal set; }
        public IntRange StormDamage { get; internal set; }
        public FractionRange PirateLoss { get; internal set; }
        public IntRange PirateDamage { get; internal set; }
        public IntRange HurricaneDelay { get; internal set; }
        public IntRange HurricaneDamage { get; internal set; }
        public FractionRange HurricaneLoss { get; internal set; }

        public MonthDay SeasonStart { get; internal set; }
        public MonthDay PeakStart { get; internal set; }
        public MonthDay PeakEnd { get; internal set; }
        public MonthDay SeasonEnd { get; internal set; }
        public double SeasonExportPremium { get; internal set; }
        public double SeasonImportPremium { get; internal set; }
        public double ZoneExportMaxMultiplier { get; internal set; }

        public int DefaultSeed { get; internal set; }
    }

    /// <summary>All loaded data. Immutable after loading.</summary>
    public sealed class GameData
    {
        readonly double?[,] multipliers;
        readonly UpgradeLevel[][] upgrades;

        public IReadOnlyList<Good> Goods { get; private set; }
        public IReadOnlyList<Port> Ports { get; private set; }
        public IReadOnlyList<Route> Routes { get; private set; }
        public IReadOnlyList<ShipTier> ShipTiers { get; private set; }
        public Rules Rules { get; private set; }
        public Port StartPort { get; private set; }
        public int CheapestShipPrice { get; private set; }
        /// <summary>What the opening fleet choice costs: the price of the dearest ship tier.</summary>
        public int FleetBudget { get; private set; }

        internal GameData(List<Good> goods, List<Port> ports, double?[,] multipliers, List<Route> routes,
                          List<ShipTier> tiers, UpgradeLevel[][] upgrades, Rules rules, Port startPort)
        {
            Goods = goods;
            Ports = ports;
            this.multipliers = multipliers;
            Routes = routes;
            ShipTiers = tiers;
            this.upgrades = upgrades;
            Rules = rules;
            StartPort = startPort;
            CheapestShipPrice = int.MaxValue;
            FleetBudget = 0;
            foreach (ShipTier tier in tiers)
            {
                CheapestShipPrice = Math.Min(CheapestShipPrice, tier.Price);
                FleetBudget = Math.Max(FleetBudget, tier.Price);
            }
        }

        /// <summary>How many ships of this tier the opening fleet choice buys.</summary>
        public int FleetCount(ShipTier tier)
        {
            return Math.Max(1, FleetBudget / tier.Price);
        }

        /// <summary>The port's price multiplier for a good, or null if the port does not trade it.</summary>
        public double? Multiplier(Port port, Good good)
        {
            return multipliers[port.Index, good.Index];
        }

        public Port FindPort(string name)
        {
            foreach (Port port in Ports) if (port.Name == name) return port;
            return null;
        }

        public Good FindGood(string name)
        {
            foreach (Good good in Goods) if (good.Name == name) return good;
            return null;
        }

        public ShipTier FindShipTier(string name)
        {
            foreach (ShipTier tier in ShipTiers) if (tier.Name == name) return tier;
            return null;
        }

        /// <summary>The route between two ports, in either order, or null.</summary>
        public Route FindRoute(string a, string b)
        {
            foreach (Route route in Routes)
            {
                if (route.A.Name == a && route.B.Name == b) return route;
                if (route.A.Name == b && route.B.Name == a) return route;
            }
            return null;
        }

        /// <summary>Level 1 is the first upgrade. Returns null when the track has no such level.</summary>
        public UpgradeLevel FindUpgrade(UpgradeTrack track, int level)
        {
            UpgradeLevel[] levels = upgrades[(int)track];
            return level >= 1 && level <= levels.Length ? levels[level - 1] : null;
        }
    }

    internal static class Num
    {
        /// <summary>Whole gold, halves rounded up.</summary>
        public static int Round(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
