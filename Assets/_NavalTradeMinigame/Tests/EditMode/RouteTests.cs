using System.Collections.Generic;
using Naval.Core;
using NUnit.Framework;

namespace Naval.Tests
{
    public class RouteTests
    {
        GameState game;

        [SetUp]
        public void SetUp()
        {
            game = TestData.NewGame();
        }

        Route R(string a, string b) { return game.Data.FindRoute(a, b); }

        [Test]
        public void TheFirstTwoRoutesAreFree()
        {
            Assert.AreEqual(2, game.Routes.FreeRoutesLeft);
            Assert.AreEqual(0, game.RouteCost(R("Havana", "Port-au-Prince")));
            TestData.Open(game, "Havana", "Port-au-Prince");
            TestData.Open(game, "Havana", "New Orleans");
            Assert.AreEqual(6000, game.Gold);
            Assert.AreEqual(0, game.Routes.FreeRoutesLeft);
            Assert.AreEqual(2, game.Routes.Owned.Count);
        }

        [Test]
        public void LaterRoutesCostTheirPrice()
        {
            TestData.Open(game, "Havana", "Port-au-Prince");
            TestData.Open(game, "Havana", "New Orleans");
            Assert.AreEqual(400, game.RouteCost(R("Campeche", "Havana")));
            TestData.Open(game, "Campeche", "Havana");
            Assert.AreEqual(5600, game.Gold);
            Assert.IsTrue(game.Routes.Owns(R("Campeche", "Havana")));
        }

        [Test]
        public void ALaterRoute_NeedsTheGold()
        {
            TestData.Buy(game, "Merchantman");   // 0 gold
            TestData.Open(game, "Havana", "Port-au-Prince");
            TestData.Open(game, "Havana", "New Orleans");
            CommandResult result = game.OpenRoute(R("Campeche", "Havana"));
            Assert.AreEqual(Failure.NotEnoughGold, result.Reason);
            Assert.IsFalse(game.Routes.Owns(R("Campeche", "Havana")));
            Assert.AreEqual(0, game.Gold);
        }

        [Test]
        public void ARoute_MustTouchTheNetwork()
        {
            // The network starts at Havana: Boston-Liverpool and Natchez-New Orleans are out of reach.
            CommandResult far = game.OpenRoute(R("Boston", "Liverpool"));
            Assert.AreEqual(Failure.NotConnected, far.Reason);
            Assert.AreEqual(Failure.NotConnected, game.OpenRoute(R("Natchez", "New Orleans")).Reason);
            Assert.AreEqual(2, game.Routes.FreeRoutesLeft, "a refused route uses no free pick");
            Assert.AreEqual(6000, game.Gold);

            TestData.Open(game, "Havana", "New Orleans");
            Assert.IsTrue(game.OpenRoute(R("Natchez", "New Orleans")).Ok, "it now touches New Orleans");
        }

        [Test]
        public void AnOwnedRoute_CannotBeBoughtTwice()
        {
            TestData.Open(game, "Havana", "Port-au-Prince");
            Assert.AreEqual(Failure.AlreadyOwned, game.OpenRoute(R("Havana", "Port-au-Prince")).Reason);
            Assert.AreEqual(1, game.Routes.FreeRoutesLeft);
        }

        /// <summary>Havana - New York - Liverpool (the last leg an ocean crossing) and New York - Boston, with one ship bought.</summary>
        static Ship ShipOnTheAtlanticNetwork(GameState game, string tier)
        {
            TestData.Open(game, "Havana", "New York");
            TestData.Open(game, "New York", "Boston");
            TestData.Open(game, "New York", "Liverpool");
            return TestData.Buy(game, tier);
        }

        [Test]
        public void ASloop_CannotCrossTheOcean()
        {
            Ship sloop = ShipOnTheAtlanticNetwork(game, "Sloop");
            Assert.AreEqual(Failure.NeedsOceanShip, game.Send(sloop, "Liverpool").Reason);
            Assert.AreEqual(Failure.NeedsOceanShip, game.Preview(sloop, "Liverpool").Status.Reason);
            Assert.IsNotNull(sloop.Port, "a refused ship stays in port");
            Assert.IsTrue(game.Send(sloop, "Boston").Ok, "coastal routes are fine");
        }

        [Test]
        public void ABrig_CanCrossTheOcean()
        {
            Ship brig = ShipOnTheAtlanticNetwork(game, "Brig");
            Assert.IsTrue(game.Preview(brig, "Liverpool").Ok);
            Assert.IsTrue(game.Send(brig, "Liverpool").Ok);
            Assert.IsNull(brig.Port);
        }

        [Test]
        public void ASloop_IsNotToldNeedsOceanShip_WhenThereIsNoRouteAtAll()
        {
            TestData.Open(game, "Havana", "New York");
            Ship sloop = TestData.Buy(game, "Sloop");
            Assert.AreEqual(Failure.NoRoute, game.Send(sloop, "Liverpool").Reason);
            Assert.AreEqual(Failure.NoRoute, game.Send(sloop, "Seville").Reason);
            Assert.AreEqual(Failure.SamePort, game.Send(sloop, "Havana").Reason);
            Assert.AreEqual(Failure.UnknownName, game.Send(sloop, "Atlantis").Reason);
        }

        [Test]
        public void Upgrades_NeedAShipyard()
        {
            TestData.Open(game, "Havana", "New York");
            Ship ship = TestData.Buy(game, "Brig");
            Assert.IsTrue(game.Send(ship, "New York").Ok);
            TestData.SailToPort(game, ship);
            Assert.AreEqual("New York", ship.Port.Name);
            Assert.AreEqual(Failure.NotAtShipyard, game.Upgrade(ship, UpgradeTrack.Hold).Reason);
            Assert.AreEqual(Failure.NotAtShipyard, game.SellShip(ship).Reason);
        }
    }

    public class PathTests
    {
        GameData data;
        Network network;

        [SetUp]
        public void SetUp()
        {
            data = TestData.Data();
            network = new Network(data);
        }

        void Own(params string[][] pairs)
        {
            foreach (string[] pair in pairs) network.Add(data.FindRoute(pair[0], pair[1]));
        }

        static string[] Pair(string a, string b) { return new[] { a, b }; }

        double Length(List<Route> path)
        {
            double total = 0;
            foreach (Route leg in path) total += leg.Distance;
            return total;
        }

        [Test]
        public void TheShorterOwnedPathWins()
        {
            Own(Pair("Havana", "New York"), Pair("Havana", "Port-au-Prince"),
                Pair("Port-au-Prince", "Bridgetown"), Pair("Bridgetown", "New York"));
            List<Route> direct = network.ShortestPath(data.FindPort("Havana"), data.FindPort("New York"), false);
            Assert.AreEqual(1, direct.Count);
            Assert.AreEqual(7, Length(direct), "7 direct beats 4 + 4 + 9");

            List<Route> viaPrince = network.ShortestPath(data.FindPort("Havana"), data.FindPort("Bridgetown"), false);
            Assert.AreEqual(2, viaPrince.Count);
            Assert.AreEqual(8, Length(viaPrince), "4 + 4 beats 7 + 9");
        }

        [Test]
        public void ThePathListsTheLegsInOrder()
        {
            Own(Pair("Havana", "Port-au-Prince"), Pair("Port-au-Prince", "Bridgetown"), Pair("Bridgetown", "New York"));
            List<Route> path = network.ShortestPath(data.FindPort("Havana"), data.FindPort("New York"), false);
            Assert.AreEqual(3, path.Count);
            Assert.AreEqual(data.FindRoute("Havana", "Port-au-Prince"), path[0]);
            Assert.AreEqual(data.FindRoute("Port-au-Prince", "Bridgetown"), path[1]);
            Assert.AreEqual(data.FindRoute("Bridgetown", "New York"), path[2]);
            Assert.AreEqual(17, Length(path));

            var voyage = new Voyage(data.FindPort("Havana"), path, 1.0, 0.5);
            var names = new List<string>();
            foreach (Port stop in voyage.Stops) names.Add(stop.Name);
            CollectionAssert.AreEqual(new[] { "Havana", "Port-au-Prince", "Bridgetown", "New York" }, names);
        }

        [Test]
        public void RoutesYouDoNotOwnAreNotUsed()
        {
            Own(Pair("Havana", "Port-au-Prince"));
            Assert.IsNull(network.ShortestPath(data.FindPort("Havana"), data.FindPort("Bridgetown"), false));
            Assert.IsNull(network.ShortestPath(data.FindPort("Havana"), data.FindPort("Boston"), true));
        }

        [Test]
        public void OceanRoutes_AreSkippedUnlessAllowed()
        {
            Own(Pair("Havana", "New York"), Pair("New York", "Boston"), Pair("Boston", "Liverpool"));
            Assert.IsNull(network.ShortestPath(data.FindPort("Havana"), data.FindPort("Liverpool"), false));
            List<Route> path = network.ShortestPath(data.FindPort("Havana"), data.FindPort("Liverpool"), true);
            Assert.AreEqual(3, path.Count);
            Assert.AreEqual(18, Length(path));
            Assert.IsNotNull(network.ShortestPath(data.FindPort("Havana"), data.FindPort("Boston"), false));
        }
    }
}
