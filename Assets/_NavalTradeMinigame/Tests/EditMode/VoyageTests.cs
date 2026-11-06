using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Naval.Core;
using NUnit.Framework;

namespace Naval.Tests
{
    public class VoyageTests
    {
        GameData data;

        [SetUp]
        public void SetUp()
        {
            data = TestData.Data();
        }

        Voyage Trip(double speed, params string[] ports)
        {
            var legs = new List<Route>();
            for (int i = 0; i + 1 < ports.Length; i++) legs.Add(data.FindRoute(ports[i], ports[i + 1]));
            return new Voyage(data.FindPort(ports[0]), legs, speed, data.Rules.PortTimeDays);
        }

        static int Days(Voyage voyage)
        {
            int days = 0;
            while (!voyage.Arrived)
            {
                voyage.NextDay(0);
                days++;
                Assert.Less(days, 1000, "the voyage never ends");
            }
            return days;
        }

        [Test]
        public void PositionAt_StartMiddleOfSecondLegAndEnd()
        {
            Voyage trip = Trip(1.0, "Havana", "New York", "Boston");   // legs of 7 and 3
            Assert.AreEqual(2, trip.Legs.Count);
            Port from, to;
            double t;
            trip.PositionAt(0, out from, out to, out t);
            Assert.AreEqual("Havana", from.Name);
            Assert.AreEqual("New York", to.Name);
            Assert.AreEqual(0, t, 1e-9);
            trip.PositionAt(0.85, out from, out to, out t);
            Assert.AreEqual("New York", from.Name);
            Assert.AreEqual("Boston", to.Name);
            Assert.AreEqual(0.5, t, 1e-9);
            trip.PositionAt(1, out from, out to, out t);
            Assert.AreEqual("New York", from.Name);
            Assert.AreEqual("Boston", to.Name);
            Assert.AreEqual(1, t, 1e-9);
        }

        [Test]
        public void Duration_IsDistanceOverSpeedPlusTimeInPort_InWholeDays()
        {
            // 4 / 1.5 = 2.67 days at sea + 0.5 in port = 3.17 -> 4
            Assert.AreEqual(4, Days(Trip(1.5, "Havana", "Port-au-Prince")));
            // 4 / 1.3 = 3.08 + 0.5 = 3.58 -> 4
            Assert.AreEqual(4, Days(Trip(1.3, "Havana", "Port-au-Prince")));
            // 3 / 1.5 = 2.0 + 0.5 = 2.5 -> 3
            Assert.AreEqual(3, Days(Trip(1.5, "Natchez", "New Orleans")));
            // 3 / 1.0 = 3 + 0.5 -> 4
            Assert.AreEqual(4, Days(Trip(1.0, "Natchez", "New Orleans")));
            // 9 / 1.16 = 7.76 + 0.5 = 8.26 -> 9
            Assert.AreEqual(9, Days(Trip(1.16, "Bridgetown", "New York")));
        }

        [Test]
        public void Duration_FollowsEveryLegOfThePath()
        {
            // 4 + 4 = 8 days at speed 1, + 0.5 -> 9
            Assert.AreEqual(9, Days(Trip(1.0, "Havana", "Port-au-Prince", "Bridgetown")));
        }

        [Test]
        public void ADelay_AddsWholeDays()
        {
            Voyage voyage = Trip(1.5, "Havana", "Port-au-Prince");
            voyage.NextDay(0);
            voyage.AddDelay(3);
            Assert.AreEqual(3 + 3, Days(voyage), "3 more days of the trip plus 3 waiting");
        }

        [Test]
        public void NoHazardsRollWhileWaitingOutADelay()
        {
            Voyage voyage = Trip(1.0, "Havana", "Port-au-Prince");
            voyage.NextDay(1);
            voyage.AddDelay(2);
            for (int i = 0; i < 2; i++)
            {
                DayOdds odds = voyage.NextDay(1);
                Assert.AreEqual(0, odds.Storm + odds.Pirates + odds.Hurricane);
            }
            Assert.Greater(voyage.NextDay(1).Pirates, 0);
        }

        [Test]
        public void DailyOdds_FollowTheLegTheShipIsOn_AndThePartDay()
        {
            Voyage voyage = Trip(1.0, "Havana", "Port-au-Prince", "Bridgetown");   // two 4-day legs, pirates 0.10 then 0.08
            Assert.AreEqual(0.10, voyage.NextDay(0).Pirates, 1e-9);
            for (int i = 0; i < 3; i++) voyage.NextDay(0);
            Assert.AreEqual(0.08, voyage.NextDay(0).Pirates, 1e-9, "day 5 is on the second leg");

            voyage = Trip(1.5, "Havana", "Port-au-Prince");   // 2.67 days at sea: 1, 1 and 0.67
            voyage.NextDay(0);
            voyage.NextDay(0);
            Assert.AreEqual(0.10 * (4 / 1.5 - 2), voyage.NextDay(0).Pirates, 1e-9);
            Assert.AreEqual(0, voyage.NextDay(0).Pirates, 1e-9, "the last day is spent in port");
        }

        [Test]
        public void HurricaneOdds_ScaleWithTheSeason_OthersDoNot()
        {
            DayOdds calm = Trip(1.0, "Havana", "Port-au-Prince").NextDay(0);
            DayOdds half = Trip(1.0, "Havana", "Port-au-Prince").NextDay(0.5);
            DayOdds full = Trip(1.0, "Havana", "Port-au-Prince").NextDay(1);
            Assert.AreEqual(0, calm.Hurricane, 1e-9);
            Assert.AreEqual(0.02, half.Hurricane, 1e-9);
            Assert.AreEqual(0.04, full.Hurricane, 1e-9);
            Assert.AreEqual(0.03, full.Storm, 1e-9);
            Assert.AreEqual(0.10, calm.Pirates, 1e-9);
        }

        [Test]
        public void TheSameSeedGivesTheSameHazards()
        {
            var odds = new DayOdds { Storm = 0.3, Pirates = 0.3, Hurricane = 0.3 };
            Assert.AreEqual(Rolls(7, odds), Rolls(7, odds));
            Assert.AreNotEqual(Rolls(7, odds), Rolls(8, odds));
        }

        string Rolls(int seed, DayOdds odds)
        {
            var rng = new Random(seed);
            var text = new StringBuilder();
            for (int i = 0; i < 200; i++)
            {
                DayHazards h = Hazards.Roll(rng, odds, data.Rules);
                text.Append(h.Storm ? "S" + h.StormDamage + "/" + h.StormDelay : "-")
                    .Append(h.Pirates ? "P" + h.PirateDamage + "/" + h.PirateLoss : "-")
                    .Append(h.Hurricane ? "H" + h.HurricaneDamage + "/" + h.HurricaneDelay + "/" + h.HurricaneLoss : "-")
                    .Append(' ');
            }
            return text.ToString();
        }

        [Test]
        public void HazardRolls_StayInTheDataRanges()
        {
            var rng = new Random(3);
            var odds = new DayOdds { Storm = 1, Pirates = 1, Hurricane = 1 };
            Rules r = data.Rules;
            for (int i = 0; i < 500; i++)
            {
                DayHazards h = Hazards.Roll(rng, odds, r);
                Assert.That(h.StormDamage, Is.InRange(r.StormDamage.Min, r.StormDamage.Max));
                Assert.That(h.StormDelay, Is.InRange(r.StormDelay.Min, r.StormDelay.Max));
                Assert.That(h.PirateDamage, Is.InRange(r.PirateDamage.Min, r.PirateDamage.Max));
                Assert.That(h.PirateLoss, Is.InRange(0.25, 0.50));
                Assert.That(h.HurricaneDamage, Is.InRange(r.HurricaneDamage.Min, r.HurricaneDamage.Max));
                Assert.That(h.HurricaneDelay, Is.InRange(r.HurricaneDelay.Min, r.HurricaneDelay.Max));
                Assert.That(h.HurricaneLoss, Is.InRange(0.25, 0.45));
            }
            Assert.IsFalse(Hazards.Roll(rng, new DayOdds(), r).Storm);
        }

        [Test]
        public void ShelteredVoyage_HasNoOddsButTakesTheSameTime()
        {
            var legs = new List<Route> { data.FindRoute("Havana", "Port-au-Prince") };
            var plain = new Voyage(data.FindPort("Havana"), legs, 1.0, data.Rules.PortTimeDays);
            var sheltered = new Voyage(data.FindPort("Havana"), legs, 1.0, data.Rules.PortTimeDays, true);
            Assert.IsFalse(plain.Sheltered);
            Assert.IsTrue(sheltered.Sheltered);
            int days = 0;
            while (!sheltered.Arrived)
            {
                DayOdds odds = sheltered.NextDay(1);
                Assert.AreEqual(0, odds.Storm + odds.Pirates + odds.Hurricane);
                days++;
                Assert.Less(days, 1000);
            }
            Assert.AreEqual(Days(plain), days);
            Assert.AreEqual(1.0, sheltered.Progress, 1e-9);
        }

        [Test]
        public void ShelterNewVoyages_DefaultsToFalse_AndLeavesVoyagesUnsheltered()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 1, 1, 10));
            Assert.IsFalse(game.ShelterNewVoyages);
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Brig");
            Assert.IsTrue(game.Send(ship, "Port-au-Prince").Ok);
            Assert.IsFalse(ship.Voyage.Sheltered);
        }

        [Test]
        public void ShelterNewVoyages_MeansNoHazardsAndNoLosses_EvenWhenTurnedOffOnTheWay()
        {
            foreach (bool turnOffOnTheWay in new[] { false, true })
            {
                GameState game = TestData.NewGame(TestData.Forced(1, 1, 1, 10, "1770-09-01"));
                TestData.Open(game, "Havana", "Port-au-Prince");
                Ship ship = TestData.Buy(game, "Brig");
                game.BuyGoods(ship, "Sugar", 20);
                int hull = ship.Hull;
                game.ShelterNewVoyages = true;
                Assert.IsTrue(game.Send(ship, "Port-au-Prince").Ok);
                Assert.IsTrue(ship.Voyage.Sheltered);
                if (turnOffOnTheWay) game.ShelterNewVoyages = false;

                List<GameEvent> events = TestData.SailToPort(game, ship);

                Assert.IsNotNull(ship.Port, "the ship arrived");
                Assert.IsTrue(events.Any(e => e.Type == EventType.ShipArrived));
                Assert.IsFalse(events.Any(e => e.Type == EventType.Storm || e.Type == EventType.PirateAttack
                    || e.Type == EventType.Hurricane || e.Type == EventType.ShipSunk));
                Assert.AreEqual(hull, ship.Hull);
                Assert.AreEqual(20, ship.CargoOf(data.FindGood("Sugar")));
            }
        }
    }

    public class SailingTests
    {
        static string Signature(GameState game, List<GameEvent> events)
        {
            var text = new StringBuilder();
            foreach (GameEvent e in events)
                text.Append(e.Type).Append('@').Append(e.Day).Append(':').Append(e.HullDamage).Append('/')
                    .Append(e.DelayDays).Append('/').Append(e.CratesLost).Append(' ');
            return text + "gold=" + game.Gold;
        }

        string Trip(int seed)
        {
            GameState game = TestData.NewGame(seed);
            TestData.Open(game, "Havana", "Port-au-Prince");
            TestData.Open(game, "Port-au-Prince", "Bridgetown");
            Ship ship = TestData.Buy(game, "Brig");
            game.BuyGoods(ship, "Sugar", 20);
            Assert.IsTrue(game.Send(ship, "Bridgetown").Ok);
            return Signature(game, TestData.SailToPort(game, ship));
        }

        [Test]
        public void TheSameSeedPlaysTheSameVoyage()
        {
            Assert.AreEqual(Trip(5), Trip(5));
            Assert.AreEqual(Trip(99), Trip(99));
        }

        [Test]
        public void ACalmVoyage_TakesExactlyThePreviewedTime_AndCostsWagesAndDues()
        {
            GameState game = TestData.NewGame(TestData.Forced(0, 0, 0));
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Sloop");
            RoutePreview preview = game.Preview(ship, "Port-au-Prince");
            Assert.AreEqual(4, preview.Days);
            Assert.AreEqual(new DateTime(1770, 7, 5), preview.ArrivalDate);

            int gold = game.Gold;
            Assert.IsTrue(game.Send(ship, "Port-au-Prince").Ok);
            List<GameEvent> events = TestData.SailToPort(game, ship);

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventType.ShipArrived, events[0].Type);
            Assert.AreEqual(preview.ArrivalDate, events[0].Date);
            Assert.AreEqual(4, game.Day);
            Assert.AreEqual("Port-au-Prince", ship.Port.Name);
            Assert.IsNull(ship.Voyage);
            Assert.AreEqual(40, events[0].Dues);
            Assert.AreEqual(gold - 4 * 12 - 40, game.Gold, "four days of wages and the harbour dues");
        }

        [Test]
        public void ThePreviewTrial_AndARealVoyage_FollowTheSameRules()
        {
            SliceCsv csv = TestData.Forced(0.5, 0.3, 0, 15);
            GameState game = TestData.NewGame(csv, 42);
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Sloop");
            game.BuyGoods(ship, "Sugar", 10);

            // One trial the way the preview runs it, with the same seed the game starts from.
            Ship trial = ship.Clone();
            trial.Voyage = new Voyage(ship.Port, game.Routes.ShortestPath(ship.Port, game.Data.FindPort("Port-au-Prince"), false),
                                      ship.Speed, game.Data.Rules.PortTimeDays);
            var dice = new Random(42);
            for (int day = 0; !trial.Voyage.Arrived; day++)
                if (trial.SailDay(game.Calendar.Season(game.Date.AddDays(day)), dice, game.Data.Rules).Sunk) break;

            game.Send(ship, "Port-au-Prince");
            TestData.SailToPort(game, ship);
            Assert.AreEqual(trial.Hull, ship.Hull);
            Assert.AreEqual(trial.CargoCount, ship.CargoCount);
            Assert.AreEqual(game.Fleet.Contains(ship), trial.Hull > 0);
        }

        [Test]
        public void Wages_ArePaidEveryDay_SailingOrDocked()
        {
            GameState game = TestData.NewGame();
            TestData.Buy(game, "Sloop");
            TestData.Buy(game, "Brig");
            int gold = game.Gold;
            game.AdvanceDay();
            game.AdvanceDay();
            Assert.AreEqual(gold - 2 * (12 + 18), game.Gold);
        }

        [Test]
        public void Pirates_TakeAQuarterToAHalfOfEachGood_AndDamageTheHull()
        {
            GameState game = TestData.NewGame(TestData.Forced(0, 1, 0));
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Sloop");
            game.BuyGoods(ship, "Sugar", 8);
            game.BuyGoods(ship, "Rum", 8);
            game.Send(ship, "Port-au-Prince");

            IReadOnlyList<GameEvent> events = game.AdvanceDay();
            GameEvent attack = events[0];
            Assert.AreEqual(EventType.PirateAttack, attack.Type);
            Assert.AreSame(ship, attack.Ship);
            Assert.That(attack.HullDamage, Is.InRange(5, 12));
            Assert.AreEqual(40 - attack.HullDamage, ship.Hull);
            Assert.That(ship.CargoOf(game.Data.FindGood("Sugar")), Is.InRange(4, 6), "8 crates less 25-50%");
            Assert.That(ship.CargoOf(game.Data.FindGood("Rum")), Is.InRange(4, 6));
            Assert.AreEqual(16 - ship.CargoCount, attack.CratesLost);
        }

        [Test]
        public void Storms_DelayAndDamage()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 0, 0));
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Brig");
            game.BuyGoods(ship, "Sugar", 10);
            game.Send(ship, "Port-au-Prince");

            GameEvent storm = game.AdvanceDay()[0];
            Assert.AreEqual(EventType.Storm, storm.Type);
            Assert.That(storm.HullDamage, Is.InRange(6, 15));
            Assert.That(storm.DelayDays, Is.InRange(1, 2));
            Assert.AreEqual(70 - storm.HullDamage, ship.Hull);
            Assert.AreEqual(10, ship.CargoCount, "a storm does not take cargo");
        }

        [Test]
        public void Hurricanes_DelayBadly_Damage_AndWashCargoOverboard()
        {
            GameState game = TestData.NewGame(TestData.Forced(0, 0, 1, 0, "1770-09-01"));   // full strength
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Brig");
            game.BuyGoods(ship, "Sugar", 20);
            game.Send(ship, "Port-au-Prince");
            Assert.AreEqual(1.0, game.Season, 1e-9);

            GameEvent hurricane = game.AdvanceDay()[0];
            Assert.AreEqual(EventType.Hurricane, hurricane.Type);
            Assert.That(hurricane.HullDamage, Is.InRange(25, 45));
            Assert.That(hurricane.DelayDays, Is.InRange(10, 16));
            Assert.That(hurricane.CratesLost, Is.InRange(5, 9), "20 crates less 25-45%");
            Assert.AreEqual(20 - hurricane.CratesLost, ship.CargoCount);
            Assert.AreEqual(70 - hurricane.HullDamage, ship.Hull);
        }

        [Test]
        public void NoHurricanesOutOfSeason()
        {
            GameState game = TestData.NewGame(TestData.Forced(0, 0, 1, 0, "1771-01-15"));
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Brig");
            game.Send(ship, "Port-au-Prince");
            List<GameEvent> events = TestData.SailToPort(game, ship);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventType.ShipArrived, events[0].Type);
        }

        [Test]
        public void AtZeroHull_TheShipAndItsCargoAreGone()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 0, 0, 500));
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Sloop");
            game.BuyGoods(ship, "Sugar", 5);
            int gold = game.Gold;
            game.Send(ship, "Port-au-Prince");

            IReadOnlyList<GameEvent> events = game.AdvanceDay();
            Assert.AreEqual(EventType.Storm, events[0].Type);
            Assert.AreEqual(EventType.ShipSunk, events[1].Type);
            Assert.AreSame(ship, events[1].Ship);
            Assert.AreEqual(0, game.Fleet.Count);
            Assert.AreEqual(0, ship.Hull);
            Assert.AreEqual(gold, game.Gold, "a lost ship earns nothing, costs no wages, and pays no dues");
            Assert.AreEqual(Failure.UnknownShip, game.SellGoods(ship, "Sugar", 1).Reason);
            Assert.AreEqual(EventType.ShipSunk, events[events.Count - 1].Type);
            Assert.AreEqual(GameOutcome.InProgress, game.Outcome, "gold buys another sloop");
        }

        [Test]
        public void ALostShip_LeavesTheOthersSailing()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 0, 0, 500));
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship doomed = TestData.Buy(game, "Sloop");
            Ship safe = TestData.Buy(game, "Sloop");
            game.Send(doomed, "Port-au-Prince");
            game.AdvanceDay();
            Assert.AreEqual(1, game.Fleet.Count);
            Assert.AreSame(safe, game.Fleet[0]);
            Assert.IsNotNull(safe.Port);
        }
    }

    public class PreviewTests
    {
        static GameState Game(SliceCsv csv)
        {
            GameState game = TestData.NewGame(csv);
            TestData.Open(game, "Havana", "Port-au-Prince");
            return game;
        }

        [Test]
        public void Preview_GivesArrivalDateAndRisks()
        {
            GameState game = Game(TestData.Csv());
            Ship sloop = TestData.Buy(game, "Sloop");
            RoutePreview preview = game.Preview(sloop, "Port-au-Prince");

            Assert.IsTrue(preview.Ok);
            Assert.AreEqual(4, preview.Days);
            Assert.AreEqual(new DateTime(1770, 7, 5), preview.ArrivalDate);
            Assert.AreEqual(4.0, preview.Distance, 1e-9);
            Assert.AreEqual("Port-au-Prince", preview.Stops[preview.Stops.Count - 1].Name);

            double sea = 4 / 1.5;   // days at sea: 1, 1 and a part day
            double noPirates = 0.9 * 0.9 * (1 - 0.1 * (sea - 2));
            Assert.AreEqual(100 * (1 - noPirates), preview.PirateRiskPercent, 1e-6);

            double noHurricane = 1;
            for (int day = 0; day < 3; day++)
                noHurricane *= 1 - 0.04 * game.Calendar.Season(game.Date.AddDays(day)) * (day < 2 ? 1 : sea - 2);
            Assert.AreEqual(100 * (1 - noHurricane), preview.HurricaneRiskPercent, 1e-6);
            Assert.Greater(preview.HurricaneRiskPercent, 0, "July is already in season");
            Assert.That(preview.LossChancePercent, Is.InRange(0.0, 100.0));
        }

        [Test]
        public void Preview_ShowsNoHurricaneRiskOutOfSeason()
        {
            GameState game = Game(TestData.Forced(0.03, 0.10, 0.04, 0, "1771-01-15"));
            Ship sloop = TestData.Buy(game, "Sloop");
            Assert.AreEqual(0, game.Preview(sloop, "Port-au-Prince").HurricaneRiskPercent, 1e-9);
        }

        [Test]
        public void Preview_IsDeterministic()
        {
            GameState game = Game(TestData.Forced(0.5, 0, 0, 15));
            Ship sloop = TestData.Buy(game, "Sloop");
            double first = game.Preview(sloop, "Port-au-Prince").LossChancePercent;
            Assert.AreEqual(first, game.Preview(sloop, "Port-au-Prince").LossChancePercent);
        }

        [Test]
        public void LossChance_IsZeroOnACalmSeaAndCertainWhenEveryStormSinksTheShip()
        {
            GameState calm = Game(TestData.Forced(0, 0, 0));
            Ship sloop = TestData.Buy(calm, "Sloop");
            RoutePreview preview = calm.Preview(sloop, "Port-au-Prince");
            Assert.AreEqual(0, preview.LossChancePercent, 1e-9);
            Assert.AreEqual(0, preview.PirateRiskPercent, 1e-9);
            Assert.AreEqual(0, preview.HurricaneRiskPercent, 1e-9);

            GameState deadly = Game(TestData.Forced(1, 0, 0, 500));
            Ship doomed = TestData.Buy(deadly, "Sloop");
            Assert.AreEqual(100, deadly.Preview(doomed, "Port-au-Prince").LossChancePercent, 1e-9);
        }

        [Test]
        public void LossChance_DependsOnTheCurrentHull()
        {
            // Storms hit half the days for 15 damage: a 40-hull sloop can die, a 56-hull one cannot.
            GameState game = Game(TestData.Forced(0.5, 0, 0, 15));
            Ship sloop = TestData.Buy(game, "Sloop");
            double weak = game.Preview(sloop, "Port-au-Prince").LossChancePercent;
            Assert.Greater(weak, 0);
            Assert.Less(weak, 50);

            Assert.IsTrue(game.Upgrade(sloop, UpgradeTrack.Hull).Ok);   // 56 hull: four storms needed, only three days at sea
            Assert.AreEqual(0, game.Preview(sloop, "Port-au-Prince").LossChancePercent, 1e-9);
        }

        [Test]
        public void Preview_ExplainsWhyItCannotBeSent()
        {
            GameState game = Game(TestData.Csv());
            Ship ship = TestData.Buy(game, "Brig");
            Assert.AreEqual(Failure.NoRoute, game.Preview(ship, "Seville").Status.Reason);
            Assert.AreEqual(Failure.SamePort, game.Preview(ship, "Havana").Status.Reason);
            game.Send(ship, "Port-au-Prince");
            Assert.AreEqual(Failure.ShipAtSea, game.Preview(ship, "Havana").Status.Reason);
        }
    }
}
