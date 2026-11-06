using System;

namespace Naval.Core
{
    /// <summary>
    /// Fixed prices: base price x port multiplier, with the season premium and the buy/sell spread.
    /// Trading never moves them.
    /// </summary>
    public sealed class Market
    {
        readonly GameData data;
        readonly bool[] zoneExport;

        public Market(GameData data)
        {
            this.data = data;
            zoneExport = new bool[data.Goods.Count];
            foreach (Port port in data.Ports)
            {
                if (!port.InHurricaneZone) continue;
                foreach (Good good in data.Goods)
                {
                    double? multiplier = data.Multiplier(port, good);
                    if (multiplier.HasValue && multiplier.Value <= data.Rules.ZoneExportMaxMultiplier)
                        zoneExport[good.Index] = true;
                }
            }
        }

        public bool Trades(Port port, Good good)
        {
            return data.Multiplier(port, good).HasValue;
        }

        /// <summary>A good the hurricane zone produces cheaply. It sells for more elsewhere in season.</summary>
        public bool IsZoneExport(Good good)
        {
            return zoneExport[good.Index];
        }

        /// <summary>The price halfway between buying and selling. Season is 0 to 1 (see <see cref="GameCalendar.Season"/>).</summary>
        public double MidPrice(Port port, Good good, double season)
        {
            double? multiplier = data.Multiplier(port, good);
            if (!multiplier.HasValue)
                throw new InvalidOperationException(port.Name + " does not trade " + good.Name);
            double premium = 0;
            if (IsZoneExport(good) && !port.InHurricaneZone) premium = data.Rules.SeasonExportPremium;
            else if (!IsZoneExport(good) && port.InHurricaneZone) premium = data.Rules.SeasonImportPremium;
            return good.BasePrice * multiplier.Value * (1 + premium * season);
        }

        public int BuyPrice(Port port, Good good, double season)
        {
            return Num.Round(MidPrice(port, good, season) * (1 + data.Rules.Spread / 2));
        }

        public int SellPrice(Port port, Good good, double season)
        {
            return Num.Round(MidPrice(port, good, season) * (1 - data.Rules.Spread / 2));
        }
    }
}
