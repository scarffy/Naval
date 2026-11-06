using System;
using System.Collections.Generic;
using Naval.Core;
using NUnit.Framework;

namespace Naval.Tests
{
    public class RepairTests
    {
        /// <summary>A ship that has crossed a stormy route (10 damage a storm) and is docked at Port-au-Prince.</summary>
        static Ship DamagedShip(GameState game, string tier, bool copper)
        {
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, tier);
            if (copper)
            {
                Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Hull).Ok);
                Assert.IsTrue(game.Upgrade(ship, UpgradeTrack.Hull).Ok);
            }
            Assert.IsTrue(game.Send(ship, "Port-au-Prince").Ok);
            TestData.SailToPort(game, ship);
            Assert.IsNotNull(ship.Port, "the ship should have survived");
            Assert.Less(ship.Hull, ship.MaxHull);
            return ship;
        }

        [Test]
        public void Repair_CostsGoldPerHullPoint()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 0, 0, 10));
            Ship ship = DamagedShip(game, "Brig", false);
            int missing = ship.MaxHull - ship.Hull;
            Assert.AreEqual(missing * 10, game.RepairCost(ship));

            int gold = game.Gold;
            CommandResult result = game.Repair(ship);
            Assert.IsTrue(result.Ok, result.Message);
            Assert.AreEqual(gold - missing * 10, game.Gold);
            Assert.AreEqual(ship.MaxHull, ship.Hull);
            Assert.AreEqual(Failure.NothingToRepair, game.Repair(ship).Reason);
        }

        [Test]
        public void CopperSheathing_MakesRepairsCheaper()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 0, 0, 10));
            Ship ship = DamagedShip(game, "Brig", true);
            Assert.AreEqual(98, ship.MaxHull, "70 x 1.4");
            Assert.AreEqual(0.6, ship.RepairMultiplier, 1e-9);
            int missing = ship.MaxHull - ship.Hull;
            Assert.AreEqual(missing * 6, game.RepairCost(ship), "40% cheaper than 10 a point");

            int gold = game.Gold;
            Assert.IsTrue(game.Repair(ship).Ok);
            Assert.AreEqual(gold - missing * 6, game.Gold);
        }

        [Test]
        public void ThickerPlankingAlone_DoesNotCutTheCost()
        {
            GameState game = TestData.NewGame();
            Ship ship = TestData.Buy(game, "Brig");
            game.Upgrade(ship, UpgradeTrack.Hull);
            Assert.AreEqual(1.0, ship.RepairMultiplier, 1e-9);
        }

        [Test]
        public void Repair_IsRefusedInDebt()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 0, 0, 10));
            Ship ship = DamagedShip(game, "Merchantman", false);   // bought with every coin, then wages and dues
            Assert.Less(game.Gold, 0, "wages and dues put it in debt");
            int gold = game.Gold;
            int hull = ship.Hull;
            Assert.AreEqual(Failure.InDebt, game.Repair(ship).Reason);
            Assert.AreEqual(gold, game.Gold);
            Assert.AreEqual(hull, ship.Hull);
        }
    }

    public class GoalTests
    {
        static SliceCsv Rules(string old, string with)
        {
            SliceCsv csv = TestData.Csv();
            csv.Rules = TestData.Replace(csv.Rules, old, with);
            return csv;
        }

        [Test]
        public void ReachingTheGoal_Wins()
        {
            GameState game = TestData.NewGame(Rules("goal_gold,500000,", "goal_gold,6000,"));
            IReadOnlyList<GameEvent> events = game.AdvanceDay();
            Assert.AreEqual(GameOutcome.Won, game.Outcome);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventType.GoalReached, events[0].Type);

            Assert.AreEqual(0, game.AdvanceDay().Count, "the game is over");
            Assert.AreEqual(1, game.Day);
            Ship ship;
            Assert.AreEqual(Failure.GameOver, game.BuyShip("Havana", "Sloop", out ship).Reason);
        }

        [Test]
        public void NotReachingTheGoal_KeepsPlaying()
        {
            GameState game = TestData.NewGame();
            Assert.AreEqual(0, game.AdvanceDay().Count);
            Assert.AreEqual(GameOutcome.InProgress, game.Outcome);
        }

        [Test]
        public void SellingCanReachTheGoalAtOnce()
        {
            // Resale at full price: buying and selling a sloop returns to 6000 = the goal.
            SliceCsv csv = Rules("goal_gold,500000,", "goal_gold,6000,");
            csv.Rules = TestData.Replace(csv.Rules, "ship_resale_fraction,0.50,", "ship_resale_fraction,1.00,");
            GameState game = TestData.NewGame(csv);
            Ship ship = TestData.Buy(game, "Sloop");
            Assert.AreEqual(GameOutcome.InProgress, game.Outcome);
            Assert.IsTrue(game.SellShip(ship).Ok);
            Assert.AreEqual(GameOutcome.Won, game.Outcome);
            IReadOnlyList<GameEvent> events = game.TakeEvents();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(EventType.GoalReached, events[0].Type);
            Assert.AreEqual(0, game.TakeEvents().Count, "events are handed over once");
        }

        [Test]
        public void RunningOutOfDays_Loses()
        {
            GameState game = TestData.NewGame(Rules("mission_days,730,", "mission_days,3,"));
            Assert.AreEqual(3, game.DaysLeft);
            Assert.AreEqual(0, game.AdvanceDay().Count);
            Assert.AreEqual(0, game.AdvanceDay().Count);
            Assert.AreEqual(GameOutcome.InProgress, game.Outcome);
            IReadOnlyList<GameEvent> events = game.AdvanceDay();
            Assert.AreEqual(GameOutcome.Lost, game.Outcome);
            Assert.AreEqual(EventType.MissionFailed, events[0].Type);
            Assert.AreEqual(FailReason.OutOfTime, events[0].Reason);
            Assert.AreEqual(0, game.DaysLeft);
        }

        [Test]
        public void ReachingTheGoalOnTheLastDay_StillWins()
        {
            SliceCsv csv = Rules("mission_days,730,", "mission_days,1,");
            csv.Rules = TestData.Replace(csv.Rules, "goal_gold,500000,", "goal_gold,6000,");
            GameState game = TestData.NewGame(csv);
            game.AdvanceDay();
            Assert.AreEqual(GameOutcome.Won, game.Outcome);
        }

        [Test]
        public void NoShipsAndNotEnoughGoldForOne_Loses()
        {
            GameState game = TestData.NewGame(Rules("start_gold,6000,", "start_gold,1500,"));
            IReadOnlyList<GameEvent> events = game.AdvanceDay();
            Assert.AreEqual(GameOutcome.Lost, game.Outcome);
            Assert.AreEqual(FailReason.NoShips, events[0].Reason);
        }

        [Test]
        public void NoShipsYet_IsFineIfASloopIsAffordable()
        {
            GameState game = TestData.NewGame();
            game.AdvanceDay();
            Assert.AreEqual(GameOutcome.InProgress, game.Outcome, "the player may still be choosing a fleet");
        }

        [Test]
        public void LosingTheLastShip_Loses_WhenGoldCannotReplaceIt()
        {
            GameState game = TestData.NewGame(TestData.Forced(1, 0, 0, 500));
            TestData.Open(game, "Havana", "Port-au-Prince");
            Ship ship = TestData.Buy(game, "Merchantman");   // 0 gold left
            game.Send(ship, "Port-au-Prince");
            IReadOnlyList<GameEvent> events = game.AdvanceDay();
            Assert.AreEqual(EventType.ShipSunk, events[1].Type);
            Assert.AreEqual(EventType.MissionFailed, events[2].Type);
            Assert.AreEqual(FailReason.NoShips, events[2].Reason);
            Assert.AreEqual(GameOutcome.Lost, game.Outcome);
        }
    }
}
