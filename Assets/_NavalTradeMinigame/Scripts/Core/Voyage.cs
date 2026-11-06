using System;
using System.Collections.Generic;

namespace Naval.Core
{
    /// <summary>The chances, for one day, of each hazard. Already scaled for a part day at sea and for the season.</summary>
    public struct DayOdds
    {
        public double Storm;
        public double Pirates;
        public double Hurricane;
    }

    /// <summary>What the dice gave for one day.</summary>
    public struct DayHazards
    {
        public bool Storm;
        public int StormDamage;
        public int StormDelay;
        public bool Pirates;
        public int PirateDamage;
        public double PirateLoss;
        public bool Hurricane;
        public int HurricaneDamage;
        public int HurricaneDelay;
        public double HurricaneLoss;

        public int Damage { get { return StormDamage + PirateDamage + HurricaneDamage; } }
        public int Delay { get { return StormDelay + HurricaneDelay; } }
    }

    /// <summary>What one day of sailing did to a ship.</summary>
    public struct DayResult
    {
        public DayHazards Hazards;
        public int PirateCratesLost;
        public int HurricaneCratesLost;
        public bool Sunk;
    }

    public static class Hazards
    {
        /// <summary>Rolls storm, pirates and hurricane, in that order.</summary>
        public static DayHazards Roll(Random rng, DayOdds odds, Rules rules)
        {
            var result = new DayHazards();
            if (rng.NextDouble() < odds.Storm)
            {
                result.Storm = true;
                result.StormDelay = rules.StormDelay.Roll(rng);
                result.StormDamage = rules.StormDamage.Roll(rng);
            }
            if (rng.NextDouble() < odds.Pirates)
            {
                result.Pirates = true;
                result.PirateLoss = rules.PirateLoss.Roll(rng);
                result.PirateDamage = rules.PirateDamage.Roll(rng);
            }
            if (rng.NextDouble() < odds.Hurricane)
            {
                result.Hurricane = true;
                result.HurricaneDelay = rules.HurricaneDelay.Roll(rng);
                result.HurricaneLoss = rules.HurricaneLoss.Roll(rng);
                result.HurricaneDamage = rules.HurricaneDamage.Roll(rng);
            }
            return result;
        }
    }

    /// <summary>
    /// A ship's trip along owned routes, one day at a time. Sailing takes distance / speed days, then the
    /// time in port. A delay stops the ship for whole days (no hazards roll while it waits).
    /// </summary>
    public sealed class Voyage
    {
        const double Epsilon = 1e-9;

        readonly List<Route> legs;
        readonly double speed;
        readonly double totalSeaDays;
        double seaDaysLeft;
        double portDaysLeft;
        int delayDaysLeft;

        public IReadOnlyList<Port> Stops { get; private set; }
        public double Distance { get; private set; }

        public Voyage(Port from, List<Route> legs, double speed, double portTimeDays)
        {
            this.legs = legs;
            this.speed = speed;
            var stops = new List<Port> { from };
            foreach (Route leg in legs)
            {
                stops.Add(leg.Other(stops[stops.Count - 1]));
                Distance += leg.Distance;
            }
            Stops = stops;
            totalSeaDays = Distance / speed;
            seaDaysLeft = totalSeaDays;
            portDaysLeft = portTimeDays;
        }

        public Port Destination { get { return Stops[Stops.Count - 1]; } }

        /// <summary>How far along the sea part is, 0 to 1. Stays put while the ship waits out a delay.</summary>
        public double Progress { get { return 1 - seaDaysLeft / totalSeaDays; } }

        public bool Arrived
        {
            get { return seaDaysLeft <= Epsilon && delayDaysLeft == 0 && portDaysLeft <= Epsilon; }
        }

        /// <summary>
        /// Moves the voyage on one day and returns that day's hazard chances.
        /// The season (0 to 1) is for the date of the day being sailed.
        /// </summary>
        public DayOdds NextDay(double season)
        {
            var odds = new DayOdds();
            if (delayDaysLeft > 0)
            {
                delayDaysLeft--;
                return odds;
            }
            double sea = Math.Min(1.0, seaDaysLeft);
            if (sea > 0)
            {
                Route leg = LegAt((totalSeaDays - seaDaysLeft + sea / 2) * speed);
                odds.Storm = leg.Storm * sea;
                odds.Pirates = leg.Pirates * sea;
                odds.Hurricane = leg.Hurricanes * season * sea;
            }
            seaDaysLeft -= sea;
            if (seaDaysLeft < Epsilon) seaDaysLeft = 0;
            portDaysLeft -= 1 - sea;
            return odds;
        }

        public void AddDelay(int days)
        {
            delayDaysLeft += days;
        }

        Route LegAt(double distanceSailed)
        {
            double end = 0;
            foreach (Route leg in legs)
            {
                end += leg.Distance;
                if (distanceSailed <= end) return leg;
            }
            return legs[legs.Count - 1];
        }
    }
}
