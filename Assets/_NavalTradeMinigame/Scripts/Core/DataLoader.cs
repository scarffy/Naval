using System;
using System.Collections.Generic;
using System.Globalization;

namespace Naval.Core
{
    /// <summary>The text of each data file. The file names in error messages are goods.csv, market.csv and so on.</summary>
    public sealed class SliceCsv
    {
        public string Goods;
        public string Market;
        public string Ports;
        public string Routes;
        public string Ships;
        public string Upgrades;
        public string Rules;
    }

    /// <summary>Turns the CSV texts into <see cref="GameData"/>. Any bad or missing value throws a <see cref="DataException"/>.</summary>
    public static class DataLoader
    {
        public static GameData Load(SliceCsv csv)
        {
            List<Good> goods = LoadGoods(new CsvTable("goods.csv", csv.Goods));
            List<Port> ports = LoadPorts(new CsvTable("ports.csv", csv.Ports));
            double?[,] multipliers = LoadMarket(new CsvTable("market.csv", csv.Market), goods, ports);
            List<Route> routes = LoadRoutes(new CsvTable("routes.csv", csv.Routes), ports);
            List<ShipTier> tiers = LoadShips(new CsvTable("ships.csv", csv.Ships));
            UpgradeLevel[][] upgrades = LoadUpgrades(new CsvTable("upgrades.csv", csv.Upgrades));
            var rulesTable = new CsvTable("rules.csv", csv.Rules);
            Rules rules = LoadRules(rulesTable);

            Port start = null;
            foreach (Port port in ports) if (port.Name == rules.StartPortName) start = port;
            if (start == null)
                throw new DataException("rules.csv, key 'start_port': unknown port '" + rules.StartPortName + "'");
            return new GameData(goods, ports, multipliers, routes, tiers, upgrades, rules, start);
        }

        static List<Good> LoadGoods(CsvTable table)
        {
            var goods = new List<Good>();
            foreach (CsvRow row in table.Rows)
            {
                string name = row.Text("good");
                if (goods.Exists(g => g.Name == name)) throw row.Error("good", "'" + name + "' appears twice");
                goods.Add(new Good(goods.Count, name, row.Int("base_price", 1)));
            }
            if (goods.Count == 0) throw new DataException("goods.csv: no goods");
            return goods;
        }

        static List<Port> LoadPorts(CsvTable table)
        {
            var ports = new List<Port>();
            foreach (CsvRow row in table.Rows)
            {
                string name = row.Text("port");
                if (ports.Exists(p => p.Name == name)) throw row.Error("port", "'" + name + "' appears twice");
                ports.Add(new Port(ports.Count, name, row.Text("region"), row.YesNo("shipyard"), row.YesNo("hurricane_zone"),
                    row.Number("map_x", 0, 100), row.Number("map_y", 0, 60)));
            }
            if (ports.Count == 0) throw new DataException("ports.csv: no ports");
            return ports;
        }

        static double?[,] LoadMarket(CsvTable table, List<Good> goods, List<Port> ports)
        {
            table.ColumnIndex("port");
            foreach (string column in table.Header)
                if (column != "port" && !goods.Exists(g => g.Name == column))
                    throw new DataException("market.csv: column '" + column + "' is not a good in goods.csv");
            var multipliers = new double?[ports.Count, goods.Count];
            var seen = new bool[ports.Count];
            foreach (CsvRow row in table.Rows)
            {
                string name = row.Text("port");
                Port port = ports.Find(p => p.Name == name);
                if (port == null) throw row.Error("port", "unknown port '" + name + "'");
                if (seen[port.Index]) throw row.Error("port", "'" + name + "' appears twice");
                seen[port.Index] = true;
                foreach (Good good in goods)
                    multipliers[port.Index, good.Index] = row.OptionalNumber(good.Name, 0.01, 100);
            }
            foreach (Port port in ports)
                if (!seen[port.Index]) throw new DataException("market.csv: no row for port '" + port.Name + "'");
            return multipliers;
        }

        static List<Route> LoadRoutes(CsvTable table, List<Port> ports)
        {
            var routes = new List<Route>();
            foreach (CsvRow row in table.Rows)
            {
                Port a = FindPort(row, "from", ports);
                Port b = FindPort(row, "to", ports);
                if (a == b) throw row.Error("to", "a route cannot start and end at the same port");
                if (routes.Exists(r => r.Touches(a) && r.Touches(b)))
                    throw row.Error("to", "the route " + a.Name + " - " + b.Name + " appears twice");
                routes.Add(new Route(routes.Count, a, b,
                    row.Number("distance", 0.1, 1000),
                    row.Number("storm_per_day", 0, 1),
                    row.Number("pirates_per_day", 0, 1),
                    row.Number("hurricanes_per_day", 0, 1),
                    row.Int("price", 0),
                    row.YesNo("ocean")));
            }
            return routes;
        }

        static Port FindPort(CsvRow row, string column, List<Port> ports)
        {
            string name = row.Text(column);
            Port port = ports.Find(p => p.Name == name);
            if (port == null) throw row.Error(column, "unknown port '" + name + "'");
            return port;
        }

        static List<ShipTier> LoadShips(CsvTable table)
        {
            var tiers = new List<ShipTier>();
            foreach (CsvRow row in table.Rows)
            {
                string name = row.Text("ship");
                if (tiers.Exists(t => t.Name == name)) throw row.Error("ship", "'" + name + "' appears twice");
                tiers.Add(new ShipTier(name, row.Int("price", 1), row.Int("capacity", 1), row.Number("speed", 0.1, 100),
                    row.Int("hull", 1), row.Int("wage_per_day", 0), row.YesNo("ocean_going")));
            }
            if (tiers.Count == 0) throw new DataException("ships.csv: no ships");
            return tiers;
        }

        static UpgradeLevel[][] LoadUpgrades(CsvTable table)
        {
            var byTrack = new List<UpgradeLevel>[UpgradeLevel.TrackCount];
            for (int i = 0; i < byTrack.Length; i++) byTrack[i] = new List<UpgradeLevel>();
            foreach (CsvRow row in table.Rows)
            {
                string text = row.Text("track");
                UpgradeTrack track;
                if (!Enum.TryParse(text, out track) || !Enum.IsDefined(typeof(UpgradeTrack), track))
                    throw row.Error("track", "'" + text + "' is not Sails, Hull or Hold");
                List<UpgradeLevel> levels = byTrack[(int)track];
                int level = row.Int("level", 1);
                if (level != levels.Count + 1)
                    throw row.Error("level", "expected level " + (levels.Count + 1) + " for " + text + " but found " + level);
                levels.Add(new UpgradeLevel(track, level, row.Text("name"), row.Number("cost_fraction", 0, 10),
                    row.Number("speed_mult", 0.1, 10), row.Number("capacity_mult", 0.1, 10),
                    row.Number("hull_mult", 0.1, 10), row.Number("repair_mult", 0, 10)));
            }
            var result = new UpgradeLevel[UpgradeLevel.TrackCount][];
            for (int i = 0; i < result.Length; i++) result[i] = byTrack[i].ToArray();
            return result;
        }

        static Rules LoadRules(CsvTable table)
        {
            var reader = new RulesReader(table);
            var rules = new Rules
            {
                StartGold = reader.Int("start_gold", 0),
                GoalGold = reader.Int("goal_gold", 1),
                MissionDays = reader.Int("mission_days", 1),
                StartDate = reader.DateOf("start_date"),
                StartPortName = reader.Text("start_port"),
                FreeRoutes = reader.Int("free_routes", 0),
                Spread = reader.Number("spread", 0, 1),
                PortFee = reader.Int("port_fee", 0),
                PortTimeDays = reader.Number("port_time_days", 0, 100),
                RepairCostPerHull = reader.Int("repair_cost_per_hull", 0),
                ShipResaleFraction = reader.Number("ship_resale_fraction", 0, 1),
                StormDelay = reader.IntsBetween("storm_delay"),
                StormDamage = reader.IntsBetween("storm_damage"),
                PirateLoss = reader.FractionsBetween("pirate_loss"),
                PirateDamage = reader.IntsBetween("pirate_damage"),
                HurricaneDelay = reader.IntsBetween("hurricane_delay"),
                HurricaneDamage = reader.IntsBetween("hurricane_damage"),
                HurricaneLoss = reader.FractionsBetween("hurricane_loss"),
                SeasonStart = reader.MonthDayOf("hurricane_season_start"),
                PeakStart = reader.MonthDayOf("hurricane_peak_start"),
                PeakEnd = reader.MonthDayOf("hurricane_peak_end"),
                SeasonEnd = reader.MonthDayOf("hurricane_season_end"),
                SeasonExportPremium = reader.Number("season_export_premium", 0, 10),
                SeasonImportPremium = reader.Number("season_import_premium", 0, 10),
                ZoneExportMaxMultiplier = reader.Number("zone_export_max_multiplier", 0, 100),
                DefaultSeed = reader.Int("default_seed", int.MinValue),
            };
            DateTime start = rules.SeasonStart.In(2001), peakStart = rules.PeakStart.In(2001);
            DateTime peakEnd = rules.PeakEnd.In(2001), end = rules.SeasonEnd.In(2001);
            if (!(start < peakStart && peakStart <= peakEnd && peakEnd < end))
                throw new DataException("rules.csv: the hurricane season dates must run start < peak_start <= peak_end < end");
            return rules;
        }

        /// <summary>Reads rules.csv by key. Errors name the row and the 'value' column.</summary>
        sealed class RulesReader
        {
            readonly Dictionary<string, CsvRow> rows = new Dictionary<string, CsvRow>();

            public RulesReader(CsvTable table)
            {
                table.ColumnIndex("value");
                foreach (CsvRow row in table.Rows)
                {
                    string key = row.Text("key");
                    if (rows.ContainsKey(key)) throw row.Error("key", "'" + key + "' appears twice");
                    rows[key] = row;
                }
            }

            CsvRow Row(string key)
            {
                CsvRow row;
                if (!rows.TryGetValue(key, out row)) throw new DataException("rules.csv: missing key '" + key + "'");
                return row;
            }

            public int Int(string key, int min) { return Read(key, row => row.Int("value", min)); }
            public double Number(string key, double min, double max) { return Read(key, row => row.Number("value", min, max)); }
            public string Text(string key) { return Read(key, row => row.Text("value")); }

            public DateTime DateOf(string key)
            {
                return Read(key, row =>
                {
                    DateTime date;
                    if (!DateTime.TryParseExact(row.Text("value"), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                                DateTimeStyles.None, out date))
                        throw row.Error("value", "'" + row.Raw("value") + "' is not a date like 1770-07-01");
                    return date;
                });
            }

            public MonthDay MonthDayOf(string key)
            {
                return Read(key, row =>
                {
                    string[] parts = row.Text("value").Split('-');
                    int month, day;
                    if (parts.Length != 2 || !int.TryParse(parts[0], out month) || !int.TryParse(parts[1], out day) ||
                        month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(2001, month))
                        throw row.Error("value", "'" + row.Raw("value") + "' is not a month-day like 06-01");
                    return new MonthDay(month, day);
                });
            }

            public IntRange IntsBetween(string key)
            {
                int min = Int(key + "_min", 0), max = Int(key + "_max", 0);
                if (min > max) throw Row(key + "_max").Error("value", key + "_max is below " + key + "_min");
                return new IntRange(min, max);
            }

            public FractionRange FractionsBetween(string key)
            {
                double min = Number(key + "_min", 0, 1), max = Number(key + "_max", 0, 1);
                if (min > max) throw Row(key + "_max").Error("value", key + "_max is below " + key + "_min");
                return new FractionRange(min, max);
            }

            /// <summary>Reads the row for a key, adding the key to any error so the message says which rule is wrong.</summary>
            T Read<T>(string key, Func<CsvRow, T> read)
            {
                CsvRow row = Row(key);
                try
                {
                    return read(row);
                }
                catch (DataException e)
                {
                    throw new DataException(e.Message + " (rule '" + key + "')");
                }
            }
        }
    }
}
