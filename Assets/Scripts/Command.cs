using SteelOfStalin.Attributes;
using SteelOfStalin.Customizables;
using SteelOfStalin.Customizables.Modules;
using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using SteelOfStalin.Props.Units.Land;
using SteelOfStalin.Util;
using System.Collections.Generic;
using System.Linq;
using static SteelOfStalin.Util.Utilities;

namespace SteelOfStalin.Commands
{
    #region Movement-Related
    public sealed class Hold : Command
    {
        public Hold() : base() { }
        public Hold(Unit u) : base(u) { }
        public Hold(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Hold(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            Unit.Carrying.Supplies.MinusEquals(Unit.GetSuppliesRequired(Unit.GetLocatedTile()));
            if (Unit.Carrying.Supplies < 0)
            {
                Unit.Carrying.Supplies.Value = 0;
            }
            if (Unit is Aerial)
            {
                Unit.Carrying.Fuel.MinusEquals(Unit.Consumption.Fuel);
            }
        }
    }
    public sealed class Move : Command
    {
        public List<Tile> Path { get; set; } // exclude source, last tile must be destination

        public Move() : base() { }
        public Move(Unit u, List<Tile> path) : base(u) => Path = path;
        public Move(Unit u, Coordinates src, Coordinates dest, List<Tile> path) : base(u, src, dest) => Path = path;
        public Move(Unit u, int srcX, int srcY, int destX, int destY, List<Tile> path) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => Path = path;

        // resolve move conflicts at moving phase
        public override void Execute()
        {
            if (Path == null || Path.Count == 0)
            {
                this.LogError("Path is null or empty");
                return;
            }
            Unit.Status |= UnitStatus.MOVED;
        }
    }
    public sealed class Capture : Command
    {
        public Capture() : base() { }
        public Capture(Unit u) : base(u) { }

        public override void Execute()
        {
            Tile tile = Unit.GetLocatedTile();
            if (!tile.IsCity)
            {
                this.LogError("Tile is not a city");
                return;
            }
            if (!(Unit is Personnel))
            {
                this.LogError("Unit is not a personnel");
                return;
            }

            Personnel p = (Personnel)Unit;
            Cities c = (Cities)tile;
            if (c.IsFriendly(Unit.Owner))
            {
                if (c.Morale < 250)
                {
                    c.Morale.PlusEquals(c.IsAlly(Unit.Owner) ? p.CaptureEfficiency.ApplyMod() / 2 : p.CaptureEfficiency.ApplyMod());
                    if (c.Morale > 250)
                    {
                        _ = c.Morale.Value = 250;
                    }
                }
            }
            else
            {
                if (c.Morale > 0)
                {
                    c.Morale.MinusEquals(p.CaptureEfficiency.ApplyMod());
                    if (c.Morale < 0)
                    {
                        _ = c.Morale.Value = 0;
                    }
                }
            }
            if (c.Morale == 0)
            {
                c.Owner = Unit.Owner;
            }
        }
    }
    public sealed class Merge : Command
    {
        public Merge() : base() { }
        public Merge(Unit u) : base(u) { }
        public Merge(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Merge(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Submerge : Command
    {
        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Surface : Command
    {
        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Landing : Command
    {
        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    #endregion

    #region Firing-Related
    public sealed class Fire : Command
    {
        public Unit Target { get; set; }
        public IOffensiveCustomizable Weapon { get; set; }

        public Fire() : base() { }
        public Fire(Unit u, Unit target, IOffensiveCustomizable weapon) : base(u) => (Target, Weapon) = (target, weapon);
        public Fire(Unit u, Coordinates src, Coordinates dest, Unit target, IOffensiveCustomizable weapon) : base(u, src, dest) => (Target, Weapon) = (target, weapon);
        public Fire(Unit u, int srcX, int srcY, int destX, int destY, Unit target, IOffensiveCustomizable weapon) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => (Target, Weapon) = (target, weapon);

        // handle direct-fire in Firing Phase
        // TODO add module damage
        // TODO FUT Impl. add effect of module status
        public override void Execute()
        {
            if (Target == null)
            {
                this.LogError("Target is null.");
                return;
            }
            if (Weapon == null)
            {
                this.LogError("Weapon is null.");
                return;
            }

            bool missed = false;
            if (Weapon is Gun)
            {
                if (new System.Random().NextDouble() > Weapon.Offense.Accuracy.Normal)
                {
                    this.Log($"Missed the shot against target at {Target.PrintCoOrds()}.");
                    missed = true;
                }
            }

            double damage_dealt;
            double distance = CubeCoordinates.GetStraightLineDistance(Unit.CubeCoOrds, Target.CubeCoOrds);

            if (!missed)
            {
                if (!(Target is Personnel))
                {
                    if (Target.Defense.Resistance > 0)
                    {
                        // TODO introduce shell mechanics and then return to this part
                        if (Weapon is Gun)
                        {
                            // gun vs arty or vehicles
                            
                        }
                        else
                        {
                            // firearm or MG vs arty or vehicles
                        }
                    }
                    else
                    {
                        // so far nth is with 0 resistance except personnel, leave blank here
                    }
                }
                else
                {
                    if (new System.Random().NextDouble() <= Target.Defense.Evasion)
                    {
                        this.Log($"Target at {Target.PrintCoOrds()} evaded the attack.");
                    }
                    else
                    {
                        damage_dealt = !(Weapon is Gun)
                            ? Formula.DamageAgainstPersonnel((Weapon, Target, distance)) // firearm or MG vs personnel
                            : Formula.DamageAgainstZeroResistance((Weapon, Target, distance)); // gun vs personnel
                        this.Log($"Inflicted {damage_dealt} damage to target at {Target.PrintCoOrds()}.");
                    }
                }
            }

            Unit.Carrying.Cartridges.MinusEquals(Weapon.ConsumptionNormal.Cartridges);
            Unit.Carrying.Shells.MinusEquals(Weapon.ConsumptionNormal.Shells);
            Unit.Carrying.Fuel.MinusEquals(Weapon.ConsumptionNormal.Fuel);
            Unit.Status |= UnitStatus.FIRED;
        }
    }
    public sealed class Suppress : Command
    {
        public Unit Target { get; set; }
        public IOffensiveCustomizable Weapon { get; set; }

        public Suppress() : base() { }
        public Suppress(Unit u, Unit target, IOffensiveCustomizable weapon) : base(u) => (Target, Weapon) = (target, weapon);
        public Suppress(Unit u, Coordinates src, Coordinates dest, Unit target, IOffensiveCustomizable weapon) : base(u, src, dest) => (Target, Weapon) = (target, weapon);
        public Suppress(Unit u, int srcX, int srcY, int destX, int destY, Unit target, IOffensiveCustomizable weapon) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => (Target, Weapon) = (target, weapon);

        // handle direct-fire in Firing Phase
        public override void Execute()
        {
            if (Target == null)
            {
                this.LogError("Target is null.");
                return;
            }
            if (Weapon == null)
            {
                this.LogError("Weapon is null.");
                return;
            }
            if (Weapon is Gun)
            {
                this.LogError("Cannot use a gun to suppress");
                return;
            }

            Target.ConsecutiveSuppressedRound++;
            Target.CurrentSuppressionLevel = Formula.EffectiveSuppression((Weapon, Target));
            this.Log($"Raised suppression level of target at {Target.PrintCoOrds()} to {Target.CurrentSuppressionLevel}");
        }
    }
    public sealed class Sabotage : Command
    {
        public Building Target { get; set; }
        public IOffensiveCustomizable Weapon { get; set; }

        public Sabotage() : base() { }
        public Sabotage(Unit u, Building target, IOffensiveCustomizable weapon) : base(u) => (Target, Weapon) = (target, weapon);
        public Sabotage(Unit u, Coordinates src, Coordinates dest, Building target, IOffensiveCustomizable weapon) : base(u, src, dest) => (Target, Weapon) = (target, weapon);
        public Sabotage(Unit u, int srcX, int srcY, int destX, int destY, Building target, IOffensiveCustomizable weapon) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => (Target, Weapon) = (target, weapon);

        // assume target is large enough that no units can block, no need to handle direct-fire
        public override void Execute()
        {
            if (Target == null)
            {
                this.LogError("Target is null.");
                return;
            }
            if (Weapon == null)
            {
                this.LogError("Weapon is null.");
                return;
            }

            double damage_dealt = Formula.DamageAgainstBuilding(Weapon);
            Target.Durability.MinusEquals(damage_dealt);
            this.Log($"Inflicted {damage_dealt} damage to target at {Target.PrintCoOrds()}.");
        }
    }
    public sealed class Ambush : Command
    {
        public Ambush() : base() { }
        public Ambush(Unit u) : base(u) { }

        public override void Execute()
        {
            // TODO
        }
    }
    #endregion

    #region Logistics-Related
    public sealed class Aboard : Command
    {
        public Aboard() : base() { }
        public Aboard(Unit u) : base(u) { }
        public Aboard(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Aboard(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Disembark : Command
    {
        public Disembark() : base() { }
        public Disembark(Unit u) : base(u) { }
        public Disembark(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Disembark(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Load : Command
    {
        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Unload : Command
    {
        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Resupply : Command
    {
        public override void Execute()
        {
            // TODO
        }
    }
    public sealed class Repair : Command
    {
        public override void Execute()
        {
            // TODO
        }
    }
    #endregion

    #region Construction-Related
    public sealed class Fortify : Command
    {
        public Fortify() : base() { }
        public Fortify(Unit u) : base(u) { }
        public Fortify(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Fortify(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            
        }
    }
    public sealed class Construct : Command
    {
        public Construct() : base() { }
        public Construct(Unit u) : base(u) { }
        public Construct(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Construct(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            
        }
    }
    public sealed class Demolish : Command
    {
        public Demolish() : base() { }
        public Demolish(Unit u) : base(u) { }
        public Demolish(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Demolish(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {

        }
    }
    #endregion

    #region Training-Related
    public sealed class Train : Command
    {
        public UnitBuilding TrainingGround { get; set; }

        public Train() : base() { }
        public Train(Unit u) : base(u) { }
        public Train(Unit u, UnitBuilding training) : base(u) => TrainingGround = training;

        public override void Execute()
        {
            if (TrainingGround == null)
            {
                this.LogError("Training ground is null.");
                return;
            }
            if (TrainingGround.TrainingQueue.Count >= TrainingGround.QueueCapacity)
            {
                this.LogError("Training queue is full");
                return;
            }
            if (TrainingGround.ReadyToDeploy.Count >= TrainingGround.QueueCapacity)
            {
                this.LogError("Maximum number of deployable units reached.");
                return;
            }
            if (!Unit.Owner.HasEnoughResources(Unit.Cost.Base))
            {
                this.LogError("Not enough resources.");
                return;
            }

            Unit.Owner.ConsumeResources(Unit.Cost.Base);
            Unit.TrainingTimeRemaining = Unit.Cost.Base.Time.ApplyMod() + TrainingGround.CurrentQueueTime;

            TrainingGround.TrainingQueue.Enqueue(Unit);
            this.Log("Unit enqueued");

            if (Map.Instance.AddUnit(Unit))
            {
                this.Log("Unit added to unit list.");
            }
            else
            {
                this.LogError("Failed to add unit to unit list.");
            }
        }
    }
    public sealed class Deploy : Command
    {
        public UnitBuilding TrainingGround { get; set; }

        public Deploy() : base() { }
        public Deploy(Unit u, UnitBuilding training) : base(u) => TrainingGround = training;
        public Deploy(Unit u, Coordinates src, Coordinates dest, UnitBuilding training) : base(u, src, dest) => TrainingGround = training;
        public Deploy(Unit u, int srcX, int srcY, int destX, int destY, UnitBuilding training) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => TrainingGround = training;

        public override void Execute()
        {
            if (TrainingGround == null)
            {
                this.LogError("Training ground is null.");
                return;
            }
            if (Unit.Status != UnitStatus.CAN_BE_DEPLOYED)
            {
                this.LogError($"Status is not CAN_BE_DEPLOYED. It is {Unit.Status} instead.");
                return;
            }
            if (Map.Instance.GetUnits(Destination).Any())
            {
                this.LogError($"There is/are unit(s) at the destination {Destination}.");
                return;
            }

            Unit.Status = UnitStatus.ACTIVE;
            Unit.CoOrds = Destination;
        }
    }
    public sealed class Rearm : Command
    {
        public UnitBuilding TrainingGround { get; set; }

        public override void Execute()
        {
            // TODO FUT Impl.
        }
    }
    #endregion

    public sealed class Scavenge : Command
    {
        public Scavenge() : base() { }
        public Scavenge(Unit u) : base(u) { }

        public override void Execute()
        {

        }
    }
    public sealed class Disassemble : Command
    {
        public Disassemble() : base() { }
        public Disassemble(Unit u) : base(u) { }

        public override void Execute() => ((Artillery)Unit).IsAssembled = false;
    }
    public sealed class Assemble : Command
    {
        public Assemble() : base() { }
        public Assemble(Unit u) : base(u) { }

        public override void Execute() => ((Artillery)Unit).IsAssembled = true;
    }
}
