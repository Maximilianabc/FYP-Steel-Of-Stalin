using SteelOfStalin.Attributes;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.Util;
using System.Collections.Generic;
using System.Linq;
using static SteelOfStalin.Util.Utilities;

/* Symbols:
 * Hold: @
 * Move: ->, Cannot move due to conflict: -x->
 * Merge: &
 * Submerge: =v
 * Surface: =^
 * Land: ~v
 * Fire: !
 * Suppress: !!
 * Sabotage: !`
 * Ambush: !?
 * Bombard: !v
 * Aboard: |>
 * Disembark: <|
 * Load: $+
 * Unload: $-
 * Resupply: #+
 * Repair: %+
 * Reconstruct: `+
 * Fortify: `^
 * Construct: `$
 * Demolish: `v
 * Train: |$
 * Deploy: |@
 * Rearm: ||
 * Capture: |v (hostile cities), |^ (friendly cities)
 * Scavenge: |+
 * Assemble: .
 * Disassemble: ..
 */ 
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
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }

            _ = Recorder.Append(Unit.ToString());
            ConsumeSuppliesStandingStill();

            if (Unit is Aerial)
            {
                decimal fuel = Unit.Consumption.Fuel.ApplyMod();
                Unit.Carrying.Fuel.MinusEquals(fuel);
                _ = Recorder.Append(Unit.GetResourcesChangeRecord("Fuel", -fuel));
            }
            _ = Recorder.AppendLine("@");
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
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
            if (Path == null || Path.Count == 0)
            {
                this.LogError("Path is null or empty");
                return;
            }

            _ = Recorder.Append(Unit.ToString());

            decimal supplies = Unit.GetSuppliesRequired(Path);
            decimal fuel = Unit.GetFuelRequired(Path);
            if (supplies > 0)
            {
                Unit.Carrying.Supplies.MinusEquals(supplies);
                if (Unit.Carrying.Supplies < 0)
                {
                    Unit.Carrying.Supplies.Value = 0;
                }
                _ = Recorder.Append(Unit.GetResourcesChangeRecord("Supplies", -supplies));
            }
            if (fuel > 0)
            {
                Unit.Carrying.Fuel.MinusEquals(fuel);
                if (Unit.Carrying.Fuel < 0)
                {
                    Unit.Carrying.Fuel.Value = 0;
                }
                _ = Recorder.Append(Unit.GetResourcesChangeRecord("Fuel", -fuel));
            }

            Coordinates dest = Path.Last().CoOrds;
            if (Unit.CoOrds != dest)
            {
                Unit.CoOrds = new Coordinates(dest);
                this.Log($"Moved to {dest}. Consumed {supplies} supplies and {fuel} fuel");
                _ = Recorder.AppendLine($"-> {dest}");
                Unit.Status |= UnitStatus.MOVED;
            }
            else
            {
                this.Log($"Destination {dest} is the same as original coords of unit {Unit}. Only cause should be movement conflict.");
                _ = Recorder.AppendLine($"-x->");
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
    public sealed class Land : Command
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

            _ = Recorder.Append(Unit.ToString());
            ConsumeSuppliesStandingStill();
            ConsumeAmmoFiring(Weapon);
            Unit.Status |= UnitStatus.FIRED;
            _ = Recorder.Append($" ! {Target}");

            if (Weapon is Gun)
            {
                if ((decimal)new System.Random().NextDouble() > Weapon.Offense.Accuracy.Normal)
                {
                    this.Log($"Missed the shot against target {Target}.");
                    _ = Recorder.AppendLine(" M");
                    return;
                }
            }

            decimal damage_dealt = 0;
            decimal distance = CubeCoordinates.GetStraightLineDistance(Unit.CubeCoOrds, Target.CubeCoOrds);

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
                if ((decimal)new System.Random().NextDouble() <= Target.Defense.Evasion)
                {
                    this.Log($"Target {Target} evaded the attack.");
                    _ = Recorder.AppendLine(" E");
                    return;
                }
                else
                {
                    damage_dealt = !(Weapon is Gun)
                        ? Formula.DamageAgainstPersonnel((Weapon, Target, distance)) // firearm or MG vs personnel
                        : Formula.DamageAgainstZeroResistance((Weapon, Target, distance)); // gun vs personnel
                }
            }

            if (damage_dealt > 0)
            {
                Target.Defense.Strength.MinusEquals(damage_dealt);
                this.Log($"Inflicted {damage_dealt} damage to target {Target}.");
                _ = Recorder.AppendLine(Target.GetStrengthChangeRecord(-damage_dealt));
            }

            // TODO add module damage

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
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
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

            _ = Recorder.Append(Unit.ToString());
            ConsumeSuppliesStandingStill();
            ConsumeAmmoFiring(Weapon, false);
            Unit.Status |= UnitStatus.FIRED;
            _ = Recorder.Append($" !! {Target}");

            Target.ConsecutiveSuppressedRound++;
            decimal sup = Formula.EffectiveSuppression((Weapon, Target));
            decimal change = sup - Target.CurrentSuppressionLevel;
            if (change > 0)
            {
                Target.CurrentSuppressionLevel = sup;
                this.Log($"Raised suppression level of target {Target} to {Target.CurrentSuppressionLevel}");
                _ = Recorder.Append(Target.GetSuppressionChangeRecord(change));
            }
            _ = Recorder.AppendLine();
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
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
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

            _ = Recorder.Append(Unit.ToString());
            ConsumeSuppliesStandingStill();
            ConsumeAmmoFiring(Weapon);
            Unit.Status |= UnitStatus.FIRED;
            _ = Recorder.Append($"^ {Target}");

            decimal damage_dealt = Formula.DamageAgainstBuilding(Weapon);
            if (damage_dealt > 0)
            {
                Target.Durability.MinusEquals(damage_dealt);
                this.Log($"Inflicted {damage_dealt} damage to target at {Target}.");
                _ = Recorder.Append(Target.GetDurabilityChangeRecord(-damage_dealt));
            }
            _ = Recorder.AppendLine();
        }
    }
    public sealed class Ambush : Command
    {
        public Ambush() : base() { }
        public Ambush(Unit u) : base(u) { }

        // TODO handle units with ambusing status before the start of planning phase of next round
        public override void Execute()
        {
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
            if (!(Unit is Ground))
            {
                this.LogError("Only ground units can ambush enemies.");
                return;
            }
            _ = Recorder.Append(Unit.ToString());
            ConsumeSuppliesStandingStill();

            this.Log($"Ambushing");
            _ = Recorder.AppendLine("!?");
            Unit.Status |= UnitStatus.AMBUSHING;
        }
    }
    public sealed class Bombard : Command
    {
        public Bombard() : base() { }
        public Bombard(Unit u) : base(u) { }
        public Bombard(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Bombard(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            // TODO FUT Impl.
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
        public Load() : base() { }
        public Load(Unit u) : base(u) { }
        public Load(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Load(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Unload : Command
    {
        public Unload() : base() { }
        public Unload(Unit u) : base(u) { }
        public Unload(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Unload(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Resupply : Command
    {
        public Resupply() : base() { }
        public Resupply(Unit u) : base(u) { }
        public Resupply(Unit u, Coordinates src, Coordinates dest) : base(u, src, dest) { }
        public Resupply(Unit u, int srcX, int srcY, int destX, int destY) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) { }

        public override void Execute()
        {
            // TODO FUT Impl.
        }
    }
    public sealed class Repair : Command
    {
        public Unit Target { get; set; }
        public Module RepairingTarget { get; set; }

        public Repair() : base() { }
        public Repair(Unit u, Unit t, Module target) : base(u) => (Target, RepairingTarget) = (t, target);
        public Repair(Unit u, Coordinates src, Coordinates dest, Unit t, Module target) : base(u, src, dest) => (Target, RepairingTarget) = (t, target);
        public Repair(Unit u, int srcX, int srcY, int destX, int destY, Unit t, Module target) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => (Target, RepairingTarget) = (t, target);

        public override void Execute()
        {
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
            if (Target == null)
            {
                this.LogError("Target is null");
                return;
            }
            if (RepairingTarget == null)
            {
                this.LogError("No modules assigned to be repaired.");
                return;
            }
            if (Target is Personnel)
            {
                this.LogError($"{Target} does not have any modules.");
                return;
            }
            if (!Target.GetModules().Where(m => m == RepairingTarget).Any())
            {
                this.LogError($"{Target} does not have the corresponding module");
                return;
            }
            if (Unit is Engineer e)
            {
                Attribute integrity_cap = Game.CustomizableData.Modules[RepairingTarget.Name].Integrity;
                decimal repairing_amount = integrity_cap * e.RepairingEfficiency;
                if (repairing_amount > 0)
                {
                    _ = Recorder.Append(Unit.ToString());
                    ConsumeSuppliesStandingStill();

                    Resources repair_cost = RepairingTarget.Cost.Repair;
                    e.Carrying.Consume(repair_cost);
                    _ = Recorder.Append(e.GetResourcesChangeRecord(repair_cost));

                    _ = Recorder.Append($"%+ {Target}");

                    RepairingTarget.Integrity.PlusEquals(repairing_amount);
                    if (RepairingTarget.Integrity >= integrity_cap)
                    {
                        RepairingTarget.Integrity.Value = integrity_cap.Value;
                    }
                    _ = Recorder.AppendLine(RepairingTarget.GetIntegrityChangeRecord(repairing_amount));

                    this.Log($"Repaired {RepairingTarget.Name} of {Unit.Name} at {Unit.CoOrds} for {repairing_amount} of integrity. The module's integrity is now {RepairingTarget.Integrity.Value}");
                }
                else
                {
                    this.Log("No repair could be made");
                    return;
                }
            }
            else
            {
                this.LogError("Only engineers can repair modules.");
                return;
            }
        }
    }
    public sealed class Reconstruct : Command
    {
        // for repairing buildings and cities
        public override void Execute()
        {
            // TODO FUT Impl.
        }
    }
    #endregion

    #region Construction-Related
    // Unit can be null in case the building is within construction range of cities
    public sealed class Fortify : Command
    {
        public Building Target { get; set; }

        public Fortify() : base() { }
        public Fortify(Unit u, Building target) : base(u) => Target = target;
        public Fortify(Unit u, Coordinates src, Coordinates dest, Building target) : base(u, src, dest) => Target = target;
        public Fortify(Unit u, int srcX, int srcY, int destX, int destY, Building target) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => Target = target;

        public override void Execute()
        {
            if (Target == null)
            {
                this.LogError("Target is null");
                return;
            }
            if (Target.Level >= Target.MaxLevel)
            {
                Target.Level = Target.MaxLevel;
                this.LogError("Max level reached.");
                return;
            }
            if (Target.Status != BuildingStatus.ACTIVE)
            {
                this.LogError("Only active buildings can be fortified.");
                return;
            }

            Resources consume = Target.Cost.Fortification;
            if (Unit == null)
            {
                if (Target.Owner.GetAllBuildingsAroundCities().Where(b => b.CoOrds == Target.CoOrds).Any())
                {
                    _ = Recorder.Append(Target.Owner.ToString());
                    Target.Owner.Resources.Consume(consume);
                    _ = Recorder.Append(Target.Owner.GetResourcesChangeRecord(consume));
                }
                else
                {
                    this.LogError("Destination is not inside constructible range of any cities");
                    return;
                }
            }
            else if (Unit is Personnel p)
            {
                _ = Recorder.Append(Unit);

                p.Carrying.Consume(consume);
                _ = Recorder.Append(Unit.GetResourcesChangeRecord(consume));

                ConsumeSuppliesStandingStill();

                Target.BuilderLocation = new Coordinates(p.CoOrds);
                p.Status |= UnitStatus.CONSTRUCTING;
            }
            else
            {
                this.LogError("Only personnel can fortify buildings outside construction ranges of cities");
                return;
            }

            Target.Status = BuildingStatus.UNDER_CONSTRUCTION;
            Target.ConstructionTimeRemaining = consume.Time.ApplyMod();
            _ = Recorder.AppendLine($"`^ {Target} {Target.ConstructionTimeRemaining}");
            this.Log($"Fortifying {Target.Name} at {Target.CoOrds} for {Target.ConstructionTimeRemaining} round(s)");
        }
    }
    public sealed class Construct : Command
    {
        public Building Target { get; set; }

        public Construct() : base() { }
        public Construct(Unit u, Building target) : base(u) => Target = target;
        public Construct(Unit u, Coordinates src, Coordinates dest, Building target) : base(u, src, dest) => Target = target;
        public Construct(Unit u, int srcX, int srcY, int destX, int destY, Building target) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => Target = target;

        public override void Execute()
        {
            if (Target == null)
            {
                this.LogError("Target is null");
                return;
            }
            if (Destination == null || Destination == default)
            {
                this.LogError("Destination not set");
                return;
            }
            if (Map.Instance.GetTile(Destination).HasBuilding)
            {
                // resolved building conflict: the player who is ready earlier can construct the building at the destination
                this.LogError("Destination already occupied");
            }

            Resources consume = Target.Cost.Base;
            if (Unit == null)
            {
                if (Target.Owner.GetAllConstructibleTilesAroundCities().Where(t => t.CoOrds == Destination).Any())
                {
                    _ = Recorder.Append(Target.Owner.ToString());
                    Target.Owner.Resources.Consume(consume);
                    _ = Recorder.Append(Target.Owner.GetResourcesChangeRecord(consume));
                }
                else
                {
                    this.LogError("Destination is not inside constructible range of any cities");
                    return;
                }
            }
            else if (Unit is Personnel p)
            {
                _ = Recorder.Append(Unit);

                p.Carrying.Consume(consume);
                _ = Recorder.Append(Unit.GetResourcesChangeRecord(consume));

                ConsumeSuppliesStandingStill();

                Target.BuilderLocation = new Coordinates(p.CoOrds);
                p.Status |= UnitStatus.CONSTRUCTING;
            }
            else
            {
                this.LogError("Only personnel can construct buildings outside construction ranges of cities");
                return;
            }

            _ = Map.Instance.AddBuilding(Target);
            Target.CoOrds = new Coordinates(Destination);
            Target.Status = BuildingStatus.UNDER_CONSTRUCTION;
            Target.ConstructionTimeRemaining = consume.Time.ApplyMod();
            _ = Recorder.AppendLine($"`$ {Target} {Target.ConstructionTimeRemaining}");
            this.Log($"Constructing {Target.Name} at {Destination} for {Target.ConstructionTimeRemaining} round(s)");
        }
    }
    public sealed class Demolish : Command
    {
        public Building Target { get; set; }

        public Demolish() : base() { }
        public Demolish(Unit u, Building target) : base(u) => Target = target;
        public Demolish(Unit u, Coordinates src, Coordinates dest, Building target) : base(u, src, dest) => Target = target;
        public Demolish(Unit u, int srcX, int srcY, int destX, int destY, Building target) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => Target = target;

        public override void Execute()
        {
            if (Target == null)
            {
                this.LogError("Target is null");
                return;
            }
            if (Unit == null)
            {
                if (!Target.Owner.GetAllConstructibleTilesAroundCities().Where(t => t.CoOrds == Target.CoOrds).Any())
                {
                    this.LogError("Destination is not inside constructible range of any cities");
                    return;
                }
            }
            else if (!(Unit is Personnel))
            {
                this.LogError("Only personnel can demolish buildings outside construction ranges of cities");
                return;
            }

            Target.Status = BuildingStatus.DESTROYED;
            _ = Recorder.AppendLine((Unit == null ? Target.Owner.ToString() : Unit.ToString()) + $" `v {Target}");
            this.Log($"Demolished {Target.Name} at {Target.CoOrds}");
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
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
            if (Unit.Owner == null)
            {
                this.LogError("Owner must be set before training the unit");
                return;
            }
            if (!Unit.Owner.HasEnoughResources(Unit.Cost.Base))
            {
                this.LogError("Not enough resources.");
                return;
            }

            Resources consume = Unit.Cost.Base;
            Unit.Owner.ConsumeResources(consume);
            Unit.TrainingTimeRemaining = consume.Time.ApplyMod() + TrainingGround.CurrentQueueTime;

            TrainingGround.TrainingQueue.Enqueue(Unit);

            Unit.CoOrds = new Coordinates(TrainingGround.CoOrds);
            Unit.Status = UnitStatus.IN_QUEUE;

            _ = Recorder.AppendLine($"{Unit.Owner}{Unit.Owner.GetResourcesChangeRecord(consume)}|$ {Unit} {Unit.TrainingTimeRemaining}");
            _ = Map.Instance.AddUnit(Unit);

            this.Log($"Training {Unit}. Time remaining {Unit.TrainingTimeRemaining}");
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
            if (TrainingGround.CoOrds != Unit.CoOrds)
            {
                this.LogError($"{TrainingGround} and {Unit} coords unmatched");
                return;
            }
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
            if (Unit.Owner == null)
            {
                this.LogError("Unit's owner is null");
                return;
            }
            if (Unit.Status != UnitStatus.CAN_BE_DEPLOYED)
            {
                this.LogError($"Status is not CAN_BE_DEPLOYED. It is {Unit.Status} instead.");
                return;
            }
            if (Map.Instance.GetUnits(Destination).Any(u => u.IsOfSameCategory(Unit)))
            {
                this.LogError($"There is/are unit(s) at the destination {Destination}.");
                return;
            }
            if (!TrainingGround.CubeCoOrds.GetNeigbours((int)TrainingGround.DeployRange.ApplyMod()).Any(c => c == (CubeCoordinates)Destination))
            {
                this.LogError($"Destination {Destination} is not in deploy range of training ground");
                return;
            }

            Unit.Status = UnitStatus.ACTIVE;
            Unit.CoOrds = new Coordinates(Destination);
            _ = Recorder.AppendLine($"{Unit.Owner} |@ {Unit}");
            this.Log($"Deployed {Unit}");
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

    public sealed class Capture : Command
    {
        public Capture() : base() { }
        public Capture(Unit u) : base(u) { }

        public override void Execute()
        {
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }

            Tile tile = Unit.GetLocatedTile();
            if (!tile.IsCity || !(tile is Cities))
            {
                this.LogError("Tile is not a city");
                return;
            }
            if (!(Unit is Personnel))
            {
                this.LogError("Only personnel can capture cities");
                return;
            }

            _ = Recorder.Append(Unit.ToString());
            ConsumeSuppliesStandingStill();

            Personnel p = (Personnel)Unit;
            Cities c = (Cities)tile;
            decimal morale = p.CaptureEfficiency.ApplyMod();

            if (c.IsFriendly(Unit.Owner))
            {
                _ = Recorder.Append($"|^ {c}");

                Attribute morale_cap = ((Cities)Game.TileData[c.Name]).Morale;

                if (c.Morale < morale_cap)
                {
                    morale /= c.IsAlly(Unit.Owner) ? 2 : 1;
                    c.Morale.PlusEquals(morale);
                    if (c.Morale > morale_cap)
                    {
                        _ = c.Morale.Value = morale_cap.Value;
                    }
                    _ = Recorder.Append(c.GetMoraleChangeRecord(morale));
                    this.Log($"Increased morale of {c} by {morale}");
                }
            }
            else
            {
                _ = Recorder.Append($"|v {c}");
                if (c.Morale > 0)
                {

                    c.Morale.MinusEquals(morale);
                    if (c.Morale < 0)
                    {
                        _ = c.Morale.Value = 0;
                    }
                    _ = Recorder.Append(c.GetMoraleChangeRecord(-morale));
                    this.Log($"Decreased morale of {c} by {morale}");
                }
                if (c.Morale == 0 && c.Owner != Unit.Owner)
                {
                    Player original = c.Owner;
                    c.Owner = Unit.Owner;
                    _ = Recorder.Append($"({original}=>{Unit.Owner})");
                    this.Log($"City's owner changed from {original} to {Unit.Owner}");
                }
            }
        }
    }
    public sealed class Scavenge : Command
    {
        public Scavenge() : base() { }
        public Scavenge(Unit u) : base(u) { }

        public override void Execute()
        {
            // TODO FUT Impl.
        }
    }
    public sealed class Disassemble : Command
    {
        public Disassemble() : base() { }
        public Disassemble(Unit u) : base(u) { }

        public override void Execute()
        {
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
            if (Unit is Artillery a)
            {
                if (a.CanDisassemble())
                {
                    a.IsAssembled = false;
                    _ = Recorder.AppendLine($"{a} ..");
                    this.Log("Disassembled");
                }
            }
            else
            {
                this.LogError("Only artilleries can disassemble");
                return;
            }
        }
    }
    public sealed class Assemble : Command
    {
        public Assemble() : base() { }
        public Assemble(Unit u) : base(u) { }

        public override void Execute()
        {
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }
            if (Unit is Artillery a)
            {
                if (a.CanAssemble())
                {
                    a.IsAssembled = false;
                    _ = Recorder.AppendLine($"{a} .");
                    this.Log("Assembled");
                }
            }
            else
            {
                this.LogError("Only artilleries can assemble");
                return;
            }
        }
    }
}
