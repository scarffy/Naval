using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Naval.Core;
using NUnit.Framework;
using UnityEngine;

namespace Naval.Tests
{
    /// <summary>Loads the real data files and builds games, with helpers to bend the data for a test.</summary>
    static class TestData
    {
        const string Folder = "/_NavalTradeMinigame/Resources/SliceData/";

        static string Read(string name)
        {
            return File.ReadAllText(Application.dataPath + Folder + name + ".csv");
        }

        public static SliceCsv Csv()
        {
            return new SliceCsv
            {
                Goods = Read("goods"),
                Market = Read("market"),
                Ports = Read("ports"),
                Routes = Read("routes"),
                Ships = Read("ships"),
                Upgrades = Read("upgrades"),
                Rules = Read("rules")
            };
        }

        public static GameData Data()
        {
            return DataLoader.Load(Csv());
        }

        public static GameState NewGame(int seed = 1)
        {
            return new GameState(Data(), seed);
        }

        public static GameState NewGame(SliceCsv csv, int seed = 1)
        {
            return new GameState(DataLoader.Load(csv), seed);
        }

        /// <summary>Replaces text that must be there, so a test fails loudly if the data file changes under it.</summary>
        public static string Replace(string text, string old, string with)
        {
            Assert.IsTrue(text.Contains(old), "test data changed: '" + old + "' not found");
            return text.Replace(old, with);
        }

        /// <summary>
        /// The real data, except that Havana to Port-au-Prince has the given daily chances (4 days long, 500 gold),
        /// and optionally every storm does the given damage and the game starts on another date.
        /// </summary>
        public static SliceCsv Forced(double storm, double pirates, double hurricanes, int stormDamage = 0, string startDate = null)
        {
            SliceCsv csv = Csv();
            csv.Routes = Replace(csv.Routes, "Havana,Port-au-Prince,4,0.03,0.10,0.04,500,no",
                string.Format(CultureInfo.InvariantCulture, "Havana,Port-au-Prince,4,{0},{1},{2},500,no", storm, pirates, hurricanes));
            if (stormDamage > 0)
            {
                csv.Rules = Replace(csv.Rules, "storm_damage_min,6,", "storm_damage_min," + stormDamage + ",");
                csv.Rules = Replace(csv.Rules, "storm_damage_max,15,", "storm_damage_max," + stormDamage + ",");
            }
            if (startDate != null) csv.Rules = Replace(csv.Rules, "start_date,1770-07-01,", "start_date," + startDate + ",");
            return csv;
        }

        public static Ship Buy(GameState game, string tier, string port = "Havana")
        {
            Ship ship;
            CommandResult result = game.BuyShip(port, tier, out ship);
            Assert.IsTrue(result.Ok, result.Message);
            return ship;
        }

        public static void Open(GameState game, string a, string b)
        {
            CommandResult result = game.OpenRoute(game.Data.FindRoute(a, b));
            Assert.IsTrue(result.Ok, result.Message);
        }

        /// <summary>Advances days until the ship is in port again and returns every event on the way.</summary>
        public static List<GameEvent> SailToPort(GameState game, Ship ship, int maxDays = 200)
        {
            var events = new List<GameEvent>();
            for (int i = 0; i < maxDays && ship.Port == null && game.Fleet.Contains(ship); i++)
                events.AddRange(game.AdvanceDay());
            return events;
        }
    }
}
