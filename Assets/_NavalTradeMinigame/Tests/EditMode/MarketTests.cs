using System;
using Naval.Core;
using NUnit.Framework;

namespace Naval.Tests
{
    public class MarketTests
    {
        GameData data;
        Market market;

        [SetUp]
        public void SetUp()
        {
            data = TestData.Data();
            market = new Market(data);
        }

        Port P(string name) { return data.FindPort(name); }
        Good G(string name) { return data.FindGood(name); }

        [Test]
        public void Price_IsBaseTimesMultiplier_WithTheSpreadAround()
        {
            // Cochineal base 220 x Seville 1.82 = 400.4
            Assert.AreEqual(400.4, market.MidPrice(P("Seville"), G("Cochineal"), 0), 1e-9);
            Assert.AreEqual(420, market.BuyPrice(P("Seville"), G("Cochineal"), 0));    // +5%
            Assert.AreEqual(380, market.SellPrice(P("Seville"), G("Cochineal"), 0));   // -5%
        }

        [Test]
        public void YouAlwaysBuyDearerThanYouSell()
        {
            foreach (Port port in data.Ports)
                foreach (Good good in data.Goods)
                {
                    if (!market.Trades(port, good)) continue;
                    foreach (double season in new[] { 0.0, 0.5, 1.0 })
                        Assert.Greater(market.BuyPrice(port, good, season), market.SellPrice(port, good, season),
                            port.Name + " " + good.Name);
                }
        }

        [Test]
        public void BlankMarketCell_MeansNotTraded()
        {
            Assert.IsFalse(market.Trades(P("Havana"), G("Furs & Deerskins")));
            Assert.Throws<InvalidOperationException>(() => market.MidPrice(P("Havana"), G("Furs & Deerskins"), 0));
        }

        [Test]
        public void ZoneExports_AreTheGoodsTheZoneProducesCheaply()
        {
            foreach (string name in new[] { "Sugar", "Tobacco", "Rum", "Coffee", "Indigo", "Cochineal", "Logwood" })
                Assert.IsTrue(market.IsZoneExport(G(name)), name);
            foreach (string name in new[] { "Wine", "Cloth", "Hardware", "Flour", "Salt Cod" })
                Assert.IsFalse(market.IsZoneExport(G(name)), name);
        }

        [Test]
        public void SeasonPremium_ZoneExportsSellForMoreOutsideTheZone()
        {
            double premium = data.Rules.SeasonExportPremium;
            double calm = market.MidPrice(P("Seville"), G("Cochineal"), 0);
            Assert.AreEqual(calm * (1 + premium), market.MidPrice(P("Seville"), G("Cochineal"), 1), 1e-9);
            Assert.AreEqual(calm * (1 + premium / 2), market.MidPrice(P("Seville"), G("Cochineal"), 0.5), 1e-9);
            Assert.Greater(market.SellPrice(P("Seville"), G("Cochineal"), 1), market.SellPrice(P("Seville"), G("Cochineal"), 0));
        }

        [Test]
        public void SeasonPremium_ZonePortsPayMoreForImports()
        {
            double calm = market.MidPrice(P("Havana"), G("Wine"), 0);   // 100 x 1.30
            Assert.AreEqual(130, calm, 1e-9);
            Assert.AreEqual(130 * (1 + data.Rules.SeasonImportPremium), market.MidPrice(P("Havana"), G("Wine"), 1), 1e-9);
        }

        [Test]
        public void SeasonPremium_DoesNotTouchOtherPrices()
        {
            // A zone export at a zone port, and an import at a port outside the zone.
            Assert.AreEqual(30, market.MidPrice(P("Havana"), G("Sugar"), 1), 1e-9);
            Assert.AreEqual(55, market.MidPrice(P("Seville"), G("Wine"), 1), 1e-9);
        }

        [Test]
        public void PricesAreFixed_TradingDoesNotMoveThem()
        {
            GameState game = TestData.NewGame();
            Ship ship = TestData.Buy(game, "Brig");
            Port havana = game.Data.FindPort("Havana");
            Good sugar = game.Data.FindGood("Sugar");
            int buy = game.BuyPrice(havana, sugar), sell = game.SellPrice(havana, sugar);
            Assert.IsTrue(game.BuyGoods(ship, "Sugar", 30).Ok);
            Assert.IsTrue(game.SellGoods(ship, "Sugar", 30).Ok);
            Assert.AreEqual(buy, game.BuyPrice(havana, sugar));
            Assert.AreEqual(sell, game.SellPrice(havana, sugar));
        }
    }

    public class CalendarTests
    {
        GameCalendar calendar;

        [SetUp]
        public void SetUp()
        {
            calendar = new GameCalendar(TestData.Data().Rules);
        }

        static DateTime D(int y, int m, int d) { return new DateTime(y, m, d); }

        [Test]
        public void DayIndex_MapsToDates()
        {
            Assert.AreEqual(D(1770, 7, 1), calendar.DateOf(0));
            Assert.AreEqual(D(1770, 7, 2), calendar.DateOf(1));
            Assert.AreEqual(D(1771, 7, 1), calendar.DateOf(365));
            Assert.AreEqual(D(1772, 6, 29), calendar.DateOf(729), "day 730 of the mission, 1772 being a leap year");
        }

        [Test]
        public void Season_IsZeroOutsideJuneToNovember()
        {
            Assert.AreEqual(0, calendar.Season(D(1771, 1, 15)), 1e-9);
            Assert.AreEqual(0, calendar.Season(D(1770, 5, 31)), 1e-9);
            Assert.AreEqual(0, calendar.Season(D(1770, 6, 1)), 1e-9, "starts at zero");
            Assert.AreEqual(0, calendar.Season(D(1770, 11, 30)), 1e-9, "over by November 30");
            Assert.AreEqual(0, calendar.Season(D(1770, 12, 25)), 1e-9);
        }

        [Test]
        public void Season_RisesToFullStrengthByMidAugust()
        {
            Assert.AreEqual(0.4, calendar.Season(D(1770, 7, 1)), 1e-9, "30 of 75 days");
            Assert.Greater(calendar.Season(D(1770, 8, 1)), calendar.Season(D(1770, 7, 1)));
            Assert.AreEqual(1, calendar.Season(D(1770, 8, 15)), 1e-9);
        }

        [Test]
        public void Season_IsFullFromAugust15ToOctober15()
        {
            Assert.AreEqual(1, calendar.Season(D(1770, 9, 10)), 1e-9);
            Assert.AreEqual(1, calendar.Season(D(1770, 10, 15)), 1e-9);
            Assert.AreEqual(1, calendar.Season(D(1772, 8, 20)), 1e-9, "also in the leap year");
        }

        [Test]
        public void Season_FallsAwayAfterMidOctober()
        {
            Assert.AreEqual(0.5, calendar.Season(D(1770, 11, 7)), 1e-9, "23 of 46 days");
            Assert.Less(calendar.Season(D(1770, 11, 20)), calendar.Season(D(1770, 11, 7)));
        }

        [Test]
        public void Season_RepeatsEveryYear()
        {
            Assert.AreEqual(calendar.Season(D(1770, 7, 1)), calendar.Season(D(1771, 7, 1)), 1e-9);
            Assert.AreEqual(calendar.Season(D(1770, 11, 7)), calendar.Season(D(1771, 11, 7)), 1e-9);
        }
    }
}
