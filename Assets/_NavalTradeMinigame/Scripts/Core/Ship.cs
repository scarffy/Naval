using System;

namespace Naval.Core
{
    /// <summary>A ship: tier, upgrades, hull, cargo, and where it is (in a port, or on a voyage).</summary>
    public sealed class Ship
    {
        readonly GameData data;
        readonly int[] cargo;
        readonly int[] levels = new int[UpgradeLevel.TrackCount];

        public int Id { get; private set; }
        public ShipTier Tier { get; private set; }
        public int Hull { get; internal set; }
        /// <summary>The port the ship is in, or null while it is at sea.</summary>
        public Port Port { get; internal set; }
        /// <summary>The voyage in progress, or null while the ship is in port.</summary>
        public Voyage Voyage { get; internal set; }

        internal Ship(GameData data, int id, ShipTier tier, Port port)
        {
            this.data = data;
            cargo = new int[data.Goods.Count];
            Id = id;
            Tier = tier;
            Port = port;
            Hull = MaxHull;
        }

        /// <summary>A copy in the same port with the same upgrades, hull and cargo, and no voyage. Used to try out a trip.</summary>
        internal Ship Clone()
        {
            var copy = new Ship(data, Id, Tier, Port);
            Array.Copy(levels, copy.levels, levels.Length);
            Array.Copy(cargo, copy.cargo, cargo.Length);
            copy.Hull = Hull;
            return copy;
        }

        /// <summary>
        /// Sails one day of the current voyage: rolls the hazards, applies damage, lost cargo and delay.
        /// The real voyage and the route preview both use this, so they follow the same rules.
        /// </summary>
        internal DayResult SailDay(double season, Random rng, Rules rules)
        {
            DayHazards h = Hazards.Roll(rng, Voyage.NextDay(season), rules);
            Voyage.AddDelay(h.Delay);
            var result = new DayResult { Hazards = h };
            Hull -= h.Damage;
            if (h.Pirates) result.PirateCratesLost = LoseCargo(h.PirateLoss);
            if (h.Hurricane) result.HurricaneCratesLost = LoseCargo(h.HurricaneLoss);
            if (Hull <= 0)
            {
                Hull = 0;
                result.Sunk = true;
            }
            return result;
        }

        public double Speed { get { return Tier.Speed * Multiplier(u => u.SpeedMult); } }
        public int Capacity { get { return Num.Round(Tier.Capacity * Multiplier(u => u.CapacityMult)); } }
        public int MaxHull { get { return Num.Round(Tier.Hull * Multiplier(u => u.HullMult)); } }
        /// <summary>1 normally, less with copper sheathing.</summary>
        public double RepairMultiplier { get { return Multiplier(u => u.RepairMult); } }

        public int Level(UpgradeTrack track)
        {
            return levels[(int)track];
        }

        public int CargoOf(Good good)
        {
            return cargo[good.Index];
        }

        public int CargoCount
        {
            get
            {
                int total = 0;
                foreach (int n in cargo) total += n;
                return total;
            }
        }

        public int FreeSpace { get { return Capacity - CargoCount; } }

        internal void AddCargo(Good good, int crates)
        {
            cargo[good.Index] += crates;
        }

        internal void RemoveCargo(Good good, int crates)
        {
            cargo[good.Index] -= crates;
        }

        /// <summary>Removes the fraction (rounded) of every good aboard and returns the crates lost.</summary>
        internal int LoseCargo(double fraction)
        {
            int lost = 0;
            for (int i = 0; i < cargo.Length; i++)
            {
                int taken = Num.Round(cargo[i] * fraction);
                cargo[i] -= taken;
                lost += taken;
            }
            return lost;
        }

        /// <summary>Buys the next level. A bigger hull comes with the extra hull points already in place.</summary>
        internal void Upgrade(UpgradeTrack track)
        {
            int oldMax = MaxHull;
            levels[(int)track]++;
            Hull += MaxHull - oldMax;
        }

        double Multiplier(Func<UpgradeLevel, double> effect)
        {
            double result = 1;
            for (int track = 0; track < levels.Length; track++)
                if (levels[track] > 0) result *= effect(data.FindUpgrade((UpgradeTrack)track, levels[track]));
            return result;
        }
    }
}
