using System;
using Naval.Core;
using NUnit.Framework;

namespace Naval.Tests
{
    public class DataTests
    {
        [Test]
        public void RealData_Loads()
        {
            GameData data = TestData.Data();
            Assert.AreEqual(14, data.Goods.Count);
            Assert.AreEqual(12, data.Ports.Count);
            Assert.AreEqual(17, data.Routes.Count);
            Assert.AreEqual(3, data.ShipTiers.Count);
            Assert.AreEqual("Havana", data.StartPort.Name);
            Assert.AreEqual(new DateTime(1770, 7, 1), data.Rules.StartDate);
            Assert.AreEqual(8500, data.Rules.StartGold);
            Assert.AreEqual(730, data.Rules.MissionDays);
            Assert.AreEqual(2, data.Rules.FreeRoutes);
            Assert.Greater(data.Rules.GoalGold, data.Rules.StartGold);
        }

        [Test]
        public void RealData_FleetBudgetBuysOneMerchantmanOrTwoBrigsOrThreeSloops_AndLeavesGoldForCargo()
        {
            GameData data = TestData.Data();
            int budget = data.FleetBudget;
            Assert.AreEqual(budget, data.FindShipTier("Merchantman").Price);
            Assert.AreEqual(budget, 2 * data.FindShipTier("Brig").Price);
            Assert.AreEqual(budget, 3 * data.FindShipTier("Sloop").Price);
            Assert.Greater(data.Rules.StartGold, budget, "gold is left for the first cargoes");
            Assert.IsFalse(data.FindShipTier("Sloop").OceanGoing);
            Assert.IsTrue(data.FindShipTier("Brig").OceanGoing);
            Assert.IsTrue(data.FindShipTier("Merchantman").OceanGoing);
        }

        [Test]
        public void RealData_PortsRoutesAndUpgrades()
        {
            GameData data = TestData.Data();
            foreach (string yard in new[] { "Havana", "Boston", "Liverpool", "Bordeaux" })
                Assert.IsTrue(data.FindPort(yard).HasShipyard, yard);
            int yards = 0, zone = 0, ocean = 0;
            foreach (Port port in data.Ports)
            {
                if (port.HasShipyard) yards++;
                if (port.InHurricaneZone) zone++;
            }
            foreach (Route route in data.Routes) if (route.Ocean) ocean++;
            Assert.AreEqual(4, yards);
            Assert.AreEqual(7, zone, "Gulf and Caribbean ports");
            Assert.AreEqual(4, ocean);
            Assert.IsFalse(data.FindPort("Boston").InHurricaneZone);
            Assert.IsFalse(data.Multiplier(data.FindPort("Natchez"), data.FindGood("Salt Cod")).HasValue, "blank means not traded");
            Assert.AreEqual(0.55, data.Multiplier(data.FindPort("Natchez"), data.FindGood("Lumber & Naval Stores")).Value, 1e-9);
            foreach (UpgradeTrack track in new[] { UpgradeTrack.Sails, UpgradeTrack.Hull, UpgradeTrack.Hold })
            {
                Assert.IsNotNull(data.FindUpgrade(track, 2), track.ToString());
                Assert.IsNull(data.FindUpgrade(track, 3), track.ToString());
            }
            Assert.AreEqual(0.6, data.FindUpgrade(UpgradeTrack.Hull, 2).RepairMult, 1e-9);
            Assert.AreEqual(1.0, data.FindUpgrade(UpgradeTrack.Hull, 1).RepairMult, 1e-9);
        }

        static void AssertFails(SliceCsv csv, params string[] messageParts)
        {
            var ex = Assert.Throws<DataException>(() => DataLoader.Load(csv));
            foreach (string part in messageParts) StringAssert.Contains(part, ex.Message);
        }

        [Test]
        public void BadNumber_NamesFileRowAndColumn()
        {
            SliceCsv csv = TestData.Csv();
            csv.Goods = TestData.Replace(csv.Goods, "Salt Cod,35", "Salt Cod,abc");
            AssertFails(csv, "goods.csv", "row 3", "base_price", "abc");
        }

        [Test]
        public void MissingValue_NamesFileRowAndColumn()
        {
            SliceCsv csv = TestData.Csv();
            csv.Ships = TestData.Replace(csv.Ships, "Brig,3000,34", "Brig,3000,");
            AssertFails(csv, "ships.csv", "row 3", "capacity", "missing");
        }

        [Test]
        public void MissingColumn_IsNamed()
        {
            SliceCsv csv = TestData.Csv();
            csv.Ships = TestData.Replace(csv.Ships, "wage_per_day", "wages");
            AssertFails(csv, "ships.csv", "wage_per_day");
        }

        [Test]
        public void UnknownPort_InRoutes()
        {
            SliceCsv csv = TestData.Csv();
            csv.Routes = TestData.Replace(csv.Routes, "Natchez,New Orleans,3", "Natchez,Nowhere,3");
            AssertFails(csv, "routes.csv", "row 2", "'to'", "Nowhere");
        }

        [Test]
        public void MapPositions_Load()
        {
            GameData data = TestData.Data();
            Assert.AreEqual(16, data.FindPort("Natchez").MapX, 1e-9);
            Assert.AreEqual(44, data.FindPort("Natchez").MapY, 1e-9);
            Assert.AreEqual(90, data.FindPort("Bordeaux").MapX, 1e-9);
            Assert.AreEqual(46, data.FindPort("Bordeaux").MapY, 1e-9);
        }

        [Test]
        public void MapPosition_OutsideTheMap_Fails()
        {
            SliceCsv csv = TestData.Csv();
            csv.Ports = TestData.Replace(csv.Ports, "Natchez,Gulf,no,yes,16,44", "Natchez,Gulf,no,yes,16,75");
            AssertFails(csv, "ports.csv", "map_y");
        }

        [Test]
        public void MissingMapColumn_IsNamed()
        {
            SliceCsv csv = TestData.Csv();
            csv.Ports = TestData.Replace(csv.Ports, "map_y", "north");
            AssertFails(csv, "ports.csv", "map_y");
        }

        [Test]
        public void BadYesNo()
        {
            SliceCsv csv = TestData.Csv();
            csv.Ports = TestData.Replace(csv.Ports, "Havana,Caribbean,yes,yes", "Havana,Caribbean,maybe,yes");
            AssertFails(csv, "ports.csv", "row 6", "shipyard", "maybe");
        }

        [Test]
        public void BadMarketCell_NamesTheGoodColumn()
        {
            SliceCsv csv = TestData.Csv();
            csv.Market = TestData.Replace(csv.Market, "Natchez,0.55,,1.30", "Natchez,0.55,,x1.30");
            AssertFails(csv, "market.csv", "row 2", "Flour", "x1.30");
        }

        [Test]
        public void WrongNumberOfCells()
        {
            SliceCsv csv = TestData.Csv();
            csv.Goods = TestData.Replace(csv.Goods, "Salt Cod,35", "Salt Cod,35,7");
            AssertFails(csv, "goods.csv", "row 3");
        }

        [Test]
        public void MissingRule_NamesTheKey()
        {
            SliceCsv csv = TestData.Csv();
            csv.Rules = TestData.Replace(csv.Rules, "spread,0.10", "sprad,0.10");
            AssertFails(csv, "rules.csv", "spread");
        }

        [Test]
        public void BadRuleValue_NamesTheRow()
        {
            SliceCsv csv = TestData.Csv();
            csv.Rules = TestData.Replace(csv.Rules, "start_date,1770-07-01", "start_date,July 1st");
            AssertFails(csv, "rules.csv", "start_date", "value");
        }

        [Test]
        public void RangeWithMinAboveMax_Fails()
        {
            SliceCsv csv = TestData.Csv();
            csv.Rules = TestData.Replace(csv.Rules, "storm_delay_max,2,", "storm_delay_max,0,");
            AssertFails(csv, "rules.csv", "storm_delay_max");
        }

        [Test]
        public void SeasonDatesOutOfOrder_Fail()
        {
            SliceCsv csv = TestData.Csv();
            csv.Rules = TestData.Replace(csv.Rules, "hurricane_season_end,11-30", "hurricane_season_end,09-01");
            AssertFails(csv, "rules.csv", "season");
        }

        [Test]
        public void PortMissingFromMarket_Fails()
        {
            SliceCsv csv = TestData.Csv();
            csv.Market = TestData.Replace(csv.Market, "Seville,", "Sevile,");
            AssertFails(csv, "market.csv", "Sevile");
        }

        [Test]
        public void UpgradeLevelsOutOfOrder_Fail()
        {
            SliceCsv csv = TestData.Csv();
            csv.Upgrades = TestData.Replace(csv.Upgrades, "Sails,2,", "Sails,3,");
            AssertFails(csv, "upgrades.csv", "row 3", "level");
        }

        [Test]
        public void MissingFileText_Fails()
        {
            SliceCsv csv = TestData.Csv();
            csv.Routes = null;
            AssertFails(csv, "routes.csv");
        }
    }
}
