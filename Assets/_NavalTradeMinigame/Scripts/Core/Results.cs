using System;
using System.Collections.Generic;

namespace Naval.Core
{
    public enum Failure
    {
        None,
        GameOver,
        UnknownShip,
        UnknownName,
        InvalidAmount,
        ShipAtSea,
        NotTraded,
        NotEnoughGold,
        InDebt,
        HoldFull,
        NotEnoughCargo,
        NotAtShipyard,
        HoldNotEmpty,
        MaxUpgrade,
        NothingToRepair,
        AlreadyOwned,
        NotConnected,
        NoRoute,
        NeedsOceanShip,
        SamePort
    }

    /// <summary>What a command did: success, or the reason it was refused with a message the UI can show.</summary>
    public sealed class CommandResult
    {
        public bool Ok { get { return Reason == Failure.None; } }
        public Failure Reason { get; private set; }
        public string Message { get; private set; }

        CommandResult(Failure reason, string message)
        {
            Reason = reason;
            Message = message;
        }

        public static CommandResult Success(string message)
        {
            return new CommandResult(Failure.None, message);
        }

        public static CommandResult Fail(Failure reason, string message)
        {
            return new CommandResult(reason, message);
        }
    }

    /// <summary>What the player is told before sending a ship. When Status is not Ok, the other fields are empty.</summary>
    public sealed class RoutePreview
    {
        public CommandResult Status { get; internal set; }
        public bool Ok { get { return Status.Ok; } }
        public IReadOnlyList<Port> Stops { get; internal set; }
        public double Distance { get; internal set; }
        /// <summary>Days until arrival if nothing goes wrong.</summary>
        public int Days { get; internal set; }
        public DateTime ArrivalDate { get; internal set; }
        /// <summary>Chance of at least one pirate attack, 0 to 100.</summary>
        public double PirateRiskPercent { get; internal set; }
        /// <summary>Chance of at least one hurricane on this trip at this time of year, 0 to 100 (0 out of season).</summary>
        public double HurricaneRiskPercent { get; internal set; }
        /// <summary>Estimated chance this ship's hull reaches 0, 0 to 100, from its current hull.</summary>
        public double LossChancePercent { get; internal set; }
    }

    public enum EventType
    {
        ShipArrived,
        PirateAttack,
        Storm,
        Hurricane,
        ShipSunk,
        GoalReached,
        MissionFailed
    }

    public enum FailReason { OutOfTime, NoShips }

    /// <summary>Something that happened while time passed. Only the fields that apply to the type are filled.</summary>
    public sealed class GameEvent
    {
        public EventType Type { get; internal set; }
        public int Day { get; internal set; }
        public DateTime Date { get; internal set; }
        /// <summary>The ship involved. For ShipSunk it is no longer in the fleet.</summary>
        public Ship Ship { get; internal set; }
        /// <summary>ShipArrived: the port reached.</summary>
        public Port Port { get; internal set; }
        /// <summary>ShipArrived: harbour dues paid.</summary>
        public int Dues { get; internal set; }
        public int HullDamage { get; internal set; }
        /// <summary>Storm and Hurricane: days the ship is held up.</summary>
        public int DelayDays { get; internal set; }
        /// <summary>PirateAttack and Hurricane: crates taken or washed overboard.</summary>
        public int CratesLost { get; internal set; }
        /// <summary>MissionFailed: why.</summary>
        public FailReason Reason { get; internal set; }
    }
}
