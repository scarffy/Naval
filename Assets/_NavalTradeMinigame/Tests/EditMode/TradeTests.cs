using Naval.Core;
using NUnit.Framework;

namespace Naval.Tests
{
    public class TradeTests
    {
        GameState game;

        [SetUp]
        public void SetUp()
        {
            game = TestData.NewGame();
        }

        Good Sugar { get { return game.Data.FindGood("Sugar"); } }

        [Test]
        public void StartingGoldBuysTheThreeFleets()
        {
            int start = game.Data.Rules.StartGold;
            int budget = game.Data.FleetBudget;
            Assert.AreEqual(start, game.Gold);
            TestData.Buy(game, "Merchantman");
            Assert.AreEqual(start - budget, game.Gold);

            game = TestData.NewGame();
            TestData.Buy(game, "Brig");
            TestData.Buy(game, "Brig");
            Assert.AreEqual(start - budget, game.Gold);

            game = TestData.NewGame();
            for (int i = 0; i < 3; i++) TestData.Buy(game, "Sloop");
            Assert.AreEqual(start - budget, game.Gold);
            Assert.AreEqual(3, game.Fleet.Count);
        }

        [TestCase("Merchantman", 1)]
        [TestCase("Brig", 2)]
        [TestCase("Sloop", 3)]
        public void ChooseFleet_BuysTheWholeFleetAtTheStartPort(string tier, int count)
        {
            int start = game.Gold;
            Assert.IsFalse(game.FleetChosen);
            CommandResult result = game.ChooseFleet(tier);
            Assert.IsTrue(result.Ok, result.Message);
            Assert.AreEqual(count, game.Fleet.Count);
            foreach (Ship ship in game.Fleet)
            {
                Assert.AreEqual(tier, ship.Tier.Name);
                Assert.AreSame(game.Data.StartPort, ship.Port);
            }
            Assert.AreEqual(start - game.Data.FleetBudget, game.Gold);
            Assert.IsTrue(game.FleetChosen);
        }

        [Test]
        public void ChooseFleet_OnlyWorksOnce()
        {
            Assert.IsTrue(game.ChooseFleet("Brig").Ok);
            int gold = game.Gold;
            CommandResult again = game.ChooseFleet("Sloop");
            Assert.AreEqual(Failure.FleetChosen, again.Reason);
            Assert.IsNotEmpty(again.Message);
            Assert.AreEqual(gold, game.Gold);
            Assert.AreEqual(2, game.Fleet.Count);
        }

        [Test]
        public void ChooseFleet_IsRefusedAfterAShipWasBought()
        {
            TestData.Buy(game, "Sloop");
            int gold = game.Gold;
            Assert.AreEqual(Failure.FleetChosen, game.ChooseFleet("Brig").Reason);
            Assert.AreEqual(gold, game.Gold);
            Assert.AreEqual(1, game.Fleet.Count);
            Assert.IsTrue(game.FleetChosen, "buying a ship ends the opening");
        }

        [Test]
        public void BuyingAShip_EndsTheOpening()
        {
            Assert.IsFalse(game.FleetChosen);
            TestData.Buy(game, "Sloop");
            Assert.IsTrue(game.FleetChosen);
            Assert.AreEqual(Failure.FleetChosen, game.ChooseFleet("Brig").Reason);
        }

        [Test]
        public void ChooseFleet_RejectsAnUnknownTier()
        {
            int gold = game.Gold;
            Assert.AreEqual(Failure.UnknownName, game.ChooseFleet("Galleon").Reason);
            Assert.AreEqual(gold, game.Gold);
            Assert.AreEqual(0, game.Fleet.Count);
            Assert.IsFalse(game.FleetChosen);
        }

        [Test]
        public void BuyingAndSelling_UpdateGoldAndCargo()
        {
            Ship ship = TestData.Buy(game, "Brig");
            Port havana = ship.Port;
            int gold = game.Gold;
            int buy = game.BuyPrice(havana, Sugar);

            CommandResult bought = game.BuyGoods(ship, "Sugar", 10);
            Assert.IsTrue(bought.Ok, bought.Message);
            Assert.AreEqual(gold - 10 * buy, game.Gold);
            Assert.AreEqual(10, ship.CargoOf(Sugar));
            Assert.AreEqual(10, ship.CargoCount);

            gold = game.Gold;
            int sell = game.SellPrice(havana, Sugar);
            Assert.Less(sell, buy);
            CommandResult sold = game.SellGoods(ship, "Sugar", 4);
            Assert.IsTrue(sold.Ok, sold.Message);
            Assert.AreEqual(gold + 4 * sell, game.Gold);
            Assert.AreEqual(6, ship.CargoOf(Sugar));
        }

        [Test]
        public void Overspending_IsRefused()
        {
            game = TestData.NewGame(TestData.ExactFleetGold(TestData.Csv()));
            Ship ship = TestData.Buy(game, "Merchantman");   // leaves 0 gold
            CommandResult result = game.BuyGoods(ship, "Sugar", 1);
            Assert.AreEqual(Failure.NotEnoughGold, result.Reason);
            Assert.IsNotEmpty(result.Message);
            Assert.AreEqual(0, game.Gold);
            Assert.AreEqual(0, ship.CargoCount);
        }

        [Test]
        public void Overfilling_IsRefused()
        {
            Ship ship = TestData.Buy(game, "Sloop");   // 16 crates
            int gold = game.Gold;
            Assert.AreEqual(Failure.HoldFull, game.BuyGoods(ship, "Sugar", 17).Reason);
            Assert.AreEqual(gold, game.Gold);
            Assert.AreEqual(0, ship.CargoCount);

            Assert.IsTrue(game.BuyGoods(ship, "Sugar", 10).Ok);
            Assert.AreEqual(Failure.HoldFull, game.BuyGoods(ship, "Rum", 7).Reason, "the hold is shared by all goods");
            Assert.IsTrue(game.BuyGoods(ship, "Rum", 6).Ok);
            Assert.AreEqual(0, ship.FreeSpace);
            Assert.AreEqual(Failure.HoldFull, game.BuyGoods(ship, "Sugar", 1).Reason);
        }

        [Test]
        public void SellingMoreThanCarried_IsRefused()
        {
            Ship ship = TestData.Buy(game, "Brig");
            game.BuyGoods(ship, "Sugar", 5);
            int gold = game.Gold;
            Assert.AreEqual(Failure.NotEnoughCargo, game.SellGoods(ship, "Sugar", 6).Reason);
            Assert.AreEqual(gold, game.Gold);
            Assert.AreEqual(5, ship.CargoOf(Sugar));
        }

        [Test]
        public void BadRequests_AreRefusedWithAReason()
        {
            Ship ship = TestData.Buy(game, "Brig");
            Assert.AreEqual(Failure.InvalidAmount, game.BuyGoods(ship, "Sugar", 0).Reason);
            Assert.AreEqual(Failure.InvalidAmount, game.SellGoods(ship, "Sugar", -3).Reason);
            Assert.AreEqual(Failure.UnknownName, game.BuyGoods(ship, "Gold dust", 1).Reason);
            Assert.AreEqual(Failure.NotTraded, game.BuyGoods(ship, "Furs & Deerskins", 1).Reason, "Havana does not trade furs");
            Assert.AreEqual(Failure.UnknownShip, game.BuyGoods(null, "Sugar", 1).Reason);
        }

        [Test]
        public void AShipAtSea_CannotTrade()
        {
            Ship ship = TestData.Buy(game, "Brig");
            TestData.Open(game, "Havana", "Port-au-Prince");
            Assert.IsTrue(game.Send(ship, "Port-au-Prince").Ok);
            Assert.IsNull(ship.Port);
            Assert.AreEqual(Failure.ShipAtSea, game.BuyGoods(ship, "Sugar", 1).Reason);
            Assert.AreEqual(Failure.ShipAtSea, game.Repair(ship).Reason);
        }

        [Test]
        public void ShipsCanOnlyBeBoughtAtAShipyard()
        {
            Ship ship;
            CommandResult result = game.BuyShip("Natchez", "Sloop", out ship);
            Assert.AreEqual(Failure.NotAtShipyard, result.Reason);
            Assert.IsNull(ship);
            Assert.AreEqual(game.Data.Rules.StartGold, game.Gold);
            Assert.AreEqual(Failure.UnknownName, game.BuyShip("Havana", "Galleon", out ship).Reason);
        }

        [Test]
        public void ABuyShip_NeedsTheGold()
        {
            game = TestData.NewGame(TestData.ExactFleetGold(TestData.Csv()));
            TestData.Buy(game, "Merchantman");
            Ship ship;
            Assert.AreEqual(Failure.NotEnoughGold, game.BuyShip("Havana", "Sloop", out ship).Reason);
            Assert.AreEqual(1, game.Fleet.Count);
        }

        [Test]
        public void SellingAShip_ReturnsAFractionOfThePrice()
        {
            Ship ship = TestData.Buy(game, "Sloop");
            game.BuyGoods(ship, "Sugar", 3);
            Assert.AreEqual(Failure.HoldNotEmpty, game.SellShip(ship).Reason);
            Assert.AreEqual(1, game.Fleet.Count);

            game.SellGoods(ship, "Sugar", 3);
            int gold = game.Gold;
            Assert.IsTrue(game.SellShip(ship).Ok);
            Assert.AreEqual(gold + 1000, game.Gold, "half of 2000");
            Assert.AreEqual(0, game.Fleet.Count);
            Assert.AreEqual(Failure.UnknownShip, game.SellShip(ship).Reason);
        }

        [Test]
        public void Upgrades_ChangeTheShip_AndCostAFractionOfItsPrice()
        {
            Ship ship = TestData.Buy(game, "Sloop");
            Assert.AreEqual(16, ship.Capacity);
            Assert.AreEqual(40, ship.MaxHull);
            Assert.AreEqual(1.5, ship.Speed, 1e-9);

            int gold = game.Gold;
            Assert.AreEqual(160, game.UpgradeCost(ship, UpgradeTrack.Hold));   // 8% of 2000
            Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Hold).Ok);
            Assert.AreEqual(gold - 160, game.Gold);
            Assert.AreEqual(21, ship.Capacity, "16 x 1.30 rounded");

            Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Sails).Ok);
            Assert.AreEqual(1.5 * 1.08, ship.Speed, 1e-9);
            Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Sails).Ok);
            Assert.AreEqual(1.5 * 1.16, ship.Speed, 1e-9, "level 2 replaces level 1");

            Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Hull).Ok);
            Assert.AreEqual(56, ship.MaxHull);
            Assert.AreEqual(56, ship.Hull, "the new planking arrives intact");
        }

        [Test]
        public void Upgrades_AreLimitedToTwoLevels_AndNeedGoldAndAShipyard()
        {
            Ship ship = TestData.Buy(game, "Sloop");
            Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Hold).Ok);
            Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Hold).Ok);
            Assert.AreEqual(Failure.MaxUpgrade, game.Upgrade(ship, UpgradeTrack.Hold).Reason);

            game = TestData.NewGame(TestData.ExactFleetGold(TestData.Csv()));
            ship = TestData.Buy(game, "Merchantman");   // 0 gold left
            Assert.AreEqual(Failure.NotEnoughGold, game.Upgrade(ship, UpgradeTrack.Sails).Reason);
            Assert.AreEqual(1.15, ship.Speed, 1e-9);
        }

        [Test]
        public void ShipsCanOnlyBeBoughtInThePortsYourRoutesReach()
        {
            Ship ship;
            CommandResult result = game.BuyShip("Liverpool", "Sloop", out ship);
            Assert.AreEqual(Failure.NotConnected, result.Reason);
            Assert.IsNull(ship);
            Assert.AreEqual(game.Data.Rules.StartGold, game.Gold);

            TestData.Open(game, "Havana", "New York");
            TestData.Open(game, "New York", "Boston");
            Assert.IsTrue(game.BuyShip("Boston", "Sloop", out ship).Ok, "Boston is now on the network");
            Assert.AreEqual("Boston", ship.Port.Name);
        }

        [Test]
        public void InDebt_NothingCanBeBought_NotEvenAFreeRoute()
        {
            SliceCsv csv = TestData.ExactFleetGold(TestData.Csv());
            csv.Rules = TestData.Replace(csv.Rules, "docked_wage_fraction,0,", "docked_wage_fraction,1,");   // so wages are due in port
            game = TestData.NewGame(csv);
            Ship ship = TestData.Buy(game, "Merchantman");   // 0 gold: free things are still allowed
            Assert.IsTrue(game.OpenRoute(game.Data.FindRoute("Havana", "Port-au-Prince")).Ok);

            game.AdvanceDay();   // wages
            Assert.Less(game.Gold, 0);
            CommandResult route = game.OpenRoute(game.Data.FindRoute("Havana", "New Orleans"));
            Assert.AreEqual(Failure.InDebt, route.Reason);
            StringAssert.Contains("debt", route.Message);
            Assert.AreEqual(Failure.InDebt, game.BuyGoods(ship, "Sugar", 1).Reason);
            Ship other;
            Assert.AreEqual(Failure.InDebt, game.BuyShip("Havana", "Sloop", out other).Reason);
            Assert.AreEqual(Failure.InDebt, game.Upgrade(ship, UpgradeTrack.Sails).Reason);
        }

        [Test]
        public void OpeningANullRoute_IsRefusedNotACrash()
        {
            Assert.AreEqual(Failure.UnknownName, game.OpenRoute(null).Reason);
            Assert.AreEqual(Failure.UnknownName, game.OpenRoute(game.Data.FindRoute("Havana", "Boston")).Reason);
        }
    }
}
