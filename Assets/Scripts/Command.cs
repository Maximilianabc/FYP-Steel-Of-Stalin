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
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Assets.Props.Units.Sea;
using SteelOfStalin.Assets.Props.Units.Air;
using UnityEngine;
using Random = SteelOfStalin.Util.Utilities.Random;
using Resources = SteelOfStalin.Attributes.Resources;

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
            _ = Recorder.AppendLine($" {Symbol} {Unit.CoOrds}");
        }
    }
    public sealed class Move : Command
    {
        public IEnumerable<Tile> Path { get; set; } // exclude source, last tile must be destination

        public Move() : base() { }
        public Move(Unit u, IEnumerable<Tile> path) : base(u) => Path = path;

        // resolve move conflicts at moving phase
        public override void Execute()
        {
            if (Unit == null)
            {
                this.LogError("Unit is null");
                return;
            }

            _ = Recorder.Append(Unit.ToString());

            if (!Path.Any())
            {
                this.Log("Path is empty. Only cause should be movement conflict and only one tile in original path.");
                ConsumeSuppliesStandingStill();
                _ = Recorder.AppendLine($" {SpecialSymbols[SpecialCommandResult.MOVE_CONFLICT_NO_MOVE]}");
                return;
            }

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
                this.Log($"Moved to {dest}. Consumed {supplies} supplies and {fuel} fuel");
                Unit.CoOrds = new Coordinates(dest);
                _ = Recorder.AppendLine($" {Symbol} {dest}");
                Unit.Status |= UnitStatus.MOVED;
            }
        }

        public override string ToStringBeforeExecution() => $"{base.ToStringBeforeExecution()}{string.Join(";", Path.Select(t => t.CoOrds))}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            Path = new List<Tile>(Coordinates.FromString(@params).Select(c => Map.Instance.GetTile(c)));
            IsValid = Path.Any();
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

            Unit.WeaponFired = Weapon;
            Unit.Status |= UnitStatus.FIRED;

            _ = Recorder.Append($" {Symbol} {Target}");

            if (Weapon is Gun)
            {
                if ((decimal)Random.NextDouble() > Weapon.Offense.Accuracy.Normal)
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
                    if (Weapon is Gun)
                    {
                        // gun vs arty or vehicles
                        decimal effective_penetration = Formula.EffectivePenetration((Weapon, distance));
                        if (effective_penetration > 2 * Target.Defense.Resistance)
                        {
                            this.Log($"Overpenetration");
                            _ = Recorder.AppendLine(" P");
                            return;
                        }
                        else if (2 * Target.Defense.Resistance > effective_penetration)
                        {
                            this.Log($"Target {Target} blocked the attack.");
                            _ = Recorder.AppendLine(" B");
                            return;
                        }
                        else
                        {
                            damage_dealt = Formula.DamageAgainstPenetratedTargets((Weapon, Target, distance));
                            decimal module_damage_dealt = Formula.ModuleDamageAgainstPenetratedTargets((Weapon, Target, distance));

                            if (module_damage_dealt > 0)
                            {
                                // TODO FUT. Impl. consider take damage change of modules as well
                                Module damaged_module = Utilities.Random.NextItem(Target.GetModules());
                                damaged_module.Integrity.MinusEquals(module_damage_dealt);
                                if (damaged_module.Integrity < 0)
                                {
                                    damaged_module.Integrity.Value = 0;
                                }
                                this.Log($"Inflicted {module_damage_dealt} damage to module {damaged_module.Name} on target {Target}.");
                                _ = Recorder.Append(damaged_module.GetIntegrityChangeRecord(-module_damage_dealt));
                            }
                        }

                    }
                    else
                    {
                        // firearm or MG vs arty or vehicles
                        damage_dealt = Formula.DamageWithoutPenetrationAgainstResistance((Weapon, Target, distance));
                    }
                }
                else
                {
                    // so far nth is with 0 resistance except personnel, leave blank here
                }
            }
            else
            {
                if ((decimal)Random.NextDouble() <= Target.Defense.Evasion)
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
        }

        public override string ToStringBeforeExecution() => $"{base.ToStringBeforeExecution()}{Target};{Weapon.Name}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            string[] ps = @params.Split(';');
            if (ps.Length != 2)
            {
                this.LogError($"params ({@params}) length mismatched, expected: 2, actual: {ps.Length}");
                IsValid = false;
                return;
            }
            Target = (Unit)Map.Instance.GetProp(ps[0]);
            if (Target == null)
            {
                this.LogError($"Cannot find unit {ps[0]}");
                IsValid = false;
                return;
            }
            Weapon = Unit.GetWeapons().Where(w => w.Name == ps[1]).FirstOrDefault();
            if (Weapon == null)
            {
                this.LogError($"{Unit} does not have weapon named {ps[1]}");
                IsValid = false;
                return;
            }
        }
        public override bool RelatedToPlayer(Player p) => (Target != null && Target.Owner == p) || base.RelatedToPlayer(p);
    }
    public sealed class Suppress : Command
    {
        public Unit Target { get; set; }
        public IOffensiveCustomizable Weapon { get; set; }

        public Suppress() : base() { }
        public Suppress(Unit u, Unit target, IOffensiveCustomizable weapon) : base(u) => (Target, Weapon) = (target, weapon);

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

            Unit.WeaponFired = Weapon;
            Unit.Status |= UnitStatus.FIRED;

            _ = Recorder.Append($" {Symbol} {Target}");

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
        public override string ToStringBeforeExecution() => $"{base.ToStringBeforeExecution()}{Target};{Weapon.Name}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            string[] ps = @params.Split(';');
            if (ps.Length != 2)
            {
                this.LogError($"params ({@params}) length mismatched, expected: 2, actual: {ps.Length}");
                IsValid = false;
                return;
            }
            Target = (Unit)Map.Instance.GetProp(ps[0]);
            if (Target == null)
            {
                this.LogError($"Cannot find unit {ps[0]}");
                IsValid = false;
                return;
            }
            Weapon = Unit.GetWeapons().Where(w => w.Name == ps[1]).FirstOrDefault();
            if (Weapon == null)
            {
                this.LogError($"{Unit} does not have weapon named {ps[1]}");
                IsValid = false;
                return;
            }
        }
        public override bool RelatedToPlayer(Player p) => (Target != null && Target.Owner == p) || base.RelatedToPlayer(p);
    }
    public sealed class Sabotage : Command
    {
        public Building Target { get; set; }
        public IOffensiveCustomizable Weapon { get; set; }

        public Sabotage() : base() { }
        public Sabotage(Unit u, Building target, IOffensiveCustomizable weapon) : base(u) => (Target, Weapon) = (target, weapon);

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

            Unit.WeaponFired = Weapon;
            Unit.Status |= UnitStatus.FIRED;

            _ = Recorder.Append($" {Symbol} {Target}");

            decimal damage_dealt = Formula.DamageAgainstBuilding(Weapon);
            if (damage_dealt > 0)
            {
                Target.Durability.MinusEquals(damage_dealt);
                this.Log($"Inflicted {damage_dealt} damage to target at {Target}.");
                _ = Recorder.Append(Target.GetDurabilityChangeRecord(-damage_dealt));
            }
            _ = Recorder.AppendLine();
        }
        public override string ToStringBeforeExecution() => $"{base.ToStringBeforeExecution()}{Target};{Weapon.Name}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            string[] ps = @params.Split(';');
            if (ps.Length != 2)
            {
                this.LogError($"params ({@params}) length mismatched, expected: 2, actual: {ps.Length}");
                IsValid = false;
                return;
            }
            Target = (Building)Map.Instance.GetProp(ps[0]);
            if (Target == null)
            {
                this.LogError($"Cannot find building {ps[0]}");
                IsValid = false;
                return;
            }
            Weapon = Unit.GetWeapons().Where(w => w.Name == ps[1]).FirstOrDefault();
            if (Weapon == null)
            {
                this.LogError($"{Unit} does not have weapon named {ps[1]}");
                IsValid = false;
                return;
            }
        }
        public override bool RelatedToPlayer(Player p) => (Target != null && Target.Owner == p) || base.RelatedToPlayer(p);
    }
    // handle units with ambusing status before the start of planning phase of next round
    public sealed class Ambush : Command
    {
        public Ambush() : base() { }
        public Ambush(Unit u) : base(u) { }

        // TODO FUT. Impl. add cancel for Ambush
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
            _ = Recorder.AppendLine($" {Symbol} {Unit.CoOrds}");
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

                    _ = Recorder.Append($" {Symbol} {Target}");

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

        public override string ToStringBeforeExecution() => $"{base.ToStringBeforeExecution()}{Target};{RepairingTarget.Name}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            string[] ps = @params.Split(';');
            if (ps.Length != 2)
            {
                this.LogError($"params ({@params}) length mismatched, expected: 2, actual: {ps.Length}");
                IsValid = false;
                return;
            }
            Target = (Unit)Map.Instance.GetProp(ps[0]);
            if (Target == null)
            {
                this.LogError($"Cannot find unit {ps[0]}");
                IsValid = false;
                return;
            }
            RepairingTarget = Target.GetRepairableModules().Where(m => m.Name == ps[1]).FirstOrDefault();
            if (RepairingTarget == null)
            {
                this.LogError($"No repairable modules named {ps[1]} on {Target}");
                IsValid = false;
                return;
            }
        }
        public override bool RelatedToPlayer(Player p) => (Target != null && Target.Owner == p) || base.RelatedToPlayer(p);
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
        // public Fortify(Unit u, Coordinates src, Coordinates dest, Building target) : base(u, src, dest) => Target = target;
        // public Fortify(Unit u, int srcX, int srcY, int destX, int destY, Building target) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => Target = target;

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
            _ = Recorder.AppendLine($" {Symbol} {Target} {Target.ConstructionTimeRemaining}");
            this.Log($"Fortifying {Target.Name} at {Target.CoOrds} for {Target.ConstructionTimeRemaining} round(s)");
        }

        public override string ToStringBeforeExecution() => Unit != null ? $"{base.ToStringBeforeExecution()}{Target}" : $"{Target.Owner} {Symbol} {Target}";

        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            Target = (Building)Map.Instance.GetProp(@params);
            if (Target == null)
            {
                this.LogError($"Cannot find target {@params}");
                IsValid = false;
                return;
            }
            Player player = Battle.Instance.GetPlayer(initiator);
            if (player == null || Target.Owner != player)
            {
                this.LogError($"Cannot find player {initiator} or {initiator} is not the owner of {Target}");
                IsValid = false;
                return;
            }
        }
    }
    public sealed class Construct : Command
    {
        public Player Builder { get; set; }
        public Building Target { get; set; }

        public Construct() : base() { }
        public Construct(Unit u, Building target, Coordinates dest) : base(u, default, dest) => Target = target;
        public Construct(Player builder, Building target, Coordinates dest) : base(null, default, dest) => (Builder, Target) = (builder, target);

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
            if (Unit == null && Builder != null)
            {
                if (Builder.GetAllConstructibleTilesAroundCities().Where(t => t.CoOrds == Destination).Any())
                {
                    _ = Recorder.Append(Builder.ToString());
                    Target.Owner.Resources.Consume(consume);
                    _ = Recorder.Append(Builder.GetResourcesChangeRecord(consume));
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

            Target.Initialize(Builder ?? Unit.Owner, Destination, BuildingStatus.UNDER_CONSTRUCTION);
            _ = Map.Instance.AddBuilding(Target);
            _ = Recorder.AppendLine($" {Symbol} {Target} {Target.ConstructionTimeRemaining}");
            this.Log($"Constructing {Target.Name} at {Destination} for {Target.ConstructionTimeRemaining} round(s)");
        }
        public override string ToStringBeforeExecution() => (Builder == null ? Unit.ToString() : Builder.ToString()) + $" {Symbol} {Target}";

        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            Target = (Building)Map.Instance.GetProp(@params);
            if (Target == null)
            {
                this.LogError($"Cannot find target {@params}");
                IsValid = false;
                return;
            }
            Builder = Battle.Instance.GetPlayer(initiator);
            if (Builder == null)
            {
                this.LogError($"Cannot find player {initiator}");
                IsValid = false;
                return;
            }
        }
    }
    public sealed class Demolish : Command
    {
        public Building Target { get; set; }

        public Demolish() : base() { }
        public Demolish(Unit u, Building target) : base(u) => Target = target;
        // public Demolish(Unit u, Coordinates src, Coordinates dest, Building target) : base(u, src, dest) => Target = target;
        // public Demolish(Unit u, int srcX, int srcY, int destX, int destY, Building target) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => Target = target;

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
            _ = Recorder.AppendLine((Unit == null ? Target.Owner.ToString() : Unit.ToString()) + $" {Symbol} {Target}");
            this.Log($"Demolished {Target.Name} at {Target.CoOrds}");
        }
        public override string ToStringBeforeExecution() => Unit != null ? $"{base.ToStringBeforeExecution()}{Target}" : $"{Target.Owner} {Symbol} {Target}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            Target = (Building)Map.Instance.GetProp(@params);
            if (Target == null)
            {
                this.LogError($"Cannot find target {@params}");
                IsValid = false;
                return;
            }
            Player player = Battle.Instance.GetPlayer(initiator);
            if (player == null || Target.Owner != player)
            {
                this.LogError($"Cannot find player {initiator} or {initiator} is not the owner of {Target}");
                IsValid = false;
                return;
            }
        }

    }
    #endregion

    #region Training-Related
    public sealed class Train : Command
    {
        public Player Trainer { get; set; }
        public UnitBuilding TrainingGround { get; set; }

        public Train() : base() { }
        public Train(Unit u, UnitBuilding training, Player trainer) : base(u) => (TrainingGround, Trainer) = (training, trainer);

        public override void Execute()
        {
            Debug.Log("Train command executing");
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
            if (Trainer == null)
            {
                this.LogError("Trainer must be set before training the unit");
                return;
            }
            if (!Trainer.HasEnoughResources(Unit.Cost.Base))
            {
                this.LogError("Not enough resources.");
                return;
            }
            if (!Unit.CanBeTrainedIn(TrainingGround))
            {
                this.LogError($"{Unit} cannot be trained in {TrainingGround}");
                return;
            }
            if (TrainingGround.Owner != Trainer)
            {
                this.LogError($"{TrainingGround} is not owned by {Trainer}");
                return;
            }

            Resources consume = Unit.Cost.Base;
            Trainer.ConsumeResources(consume);
            Unit.TrainingTimeRemaining = consume.Time.ApplyMod() + TrainingGround.CurrentQueueTime;

            TrainingGround.TrainingQueue.Enqueue(Unit);

            Unit.Initialize(Trainer, TrainingGround.CoOrds, UnitStatus.IN_QUEUE);

            _ = Recorder.AppendLine($"{Trainer}{Trainer.GetResourcesChangeRecord(consume)} {Symbol} {Unit} {Unit.TrainingTimeRemaining}");
            _ = Map.Instance.AddUnit(Unit);

            this.Log($"Training {Unit}. Time remaining {Unit.TrainingTimeRemaining}");
        }
        public override string ToStringBeforeExecution() => $"{Trainer} {Symbol} {Unit};{TrainingGround}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);
            Player player = Battle.Instance.GetPlayer(initiator);
            if (player == null)
            {
                this.LogError($"Cannot find player {initiator}");
                IsValid = false;
                return;
            }
            string[] ps = @params.Split(';');
            if (ps.Length != 2)
            {
                this.LogError($"params ({@params}) length mismatched, expected: 2, actual: {ps.Length}");
                IsValid = false;
                return;
            }
            Unit = (Unit)Map.Instance.GetProp(ps[0]);
            if (Unit == null)
            {
                this.LogError($"Cannot find unit {ps[0]}");
                IsValid = false;
                return;
            }
            TrainingGround = (UnitBuilding)Map.Instance.GetProp(ps[1]);
            if (TrainingGround == null)
            {
                this.LogError($"Cannot find unit building {ps[1]}");
                IsValid = false;
                return;
            }
        }
        public override bool RelatedToPlayer(Player p) => Trainer != null && Trainer == p && TrainingGround.Owner == p;
    }
    public sealed class Deploy : Command
    {
        public UnitBuilding TrainingGround { get; set; }
        public IEnumerable<IOffensiveCustomizable> Weapons { get; set; }

        public Deploy() : base() { }
        public Deploy(Unit u, UnitBuilding training, Coordinates dest, IEnumerable<IOffensiveCustomizable> weapons) : base(u, default, dest) => (TrainingGround, Weapons) = (training, weapons);
        // public Deploy(Unit u, Coordinates src, Coordinates dest, UnitBuilding training) : base(u, src, dest) => TrainingGround = training;
        // public Deploy(Unit u, int srcX, int srcY, int destX, int destY, UnitBuilding training) : base(u, new Coordinates(srcX, srcY), new Coordinates(destX, destY)) => TrainingGround = training;

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
            if (Weapons == null || Weapons == Enumerable.Empty<IOffensiveCustomizable>())
            {
                this.LogError($"Weapons must be assigned before deploying.");
                return;
            }
            if (Map.Instance.GetUnits(Destination).Any(u => u.IsOfSameCategory(Unit)))
            {
                // TODO maybe record this one and send back to client
                this.LogWarning($"There is/are unit(s) at the destination {Destination}.");
                return;
            }
            if (!TrainingGround.CubeCoOrds.GetNeighbours((int)TrainingGround.DeployRange.ApplyMod()).Any(c => c == (CubeCoordinates)Destination))
            {
                this.LogError($"Destination {Destination} is not in deploy range of training ground");
                return;
            }
            if (TrainingGround.Owner != Unit.Owner)
            {
                this.LogError($"{TrainingGround}'s owner {TrainingGround.Owner} does not own {Unit} (owner: {Unit.Owner}");
                return;
            }
            if (!TrainingGround.ReadyToDeploy.Contains(Unit))
            {
                this.LogError($"{TrainingGround}'s deploy queue does not contain {Unit}");
                return;
            }

            Unit.Status = UnitStatus.ACTIVE;
            Unit.CoOrds = new Coordinates(Destination);
            Unit.SetWeapons(Weapons);
            _ = TrainingGround.ReadyToDeploy.Remove(Unit);
            _ = Recorder.AppendLine($"{Unit.Owner} {Symbol} {Unit}");
            this.Log($"Deployed {Unit} at {Destination}");
        }

        public override string ToStringBeforeExecution() => $"{base.ToStringBeforeExecution()}{TrainingGround};{string.Join(";", Weapons.Select(w => w.Name))}";
        public override void SetParamsFromString(string initiator, string @params)
        {
            base.SetParamsFromString(initiator, @params);

            string[] ps = @params.Split(';');
            if (ps.Length < 2)
            {
                this.LogError($"params ({@params}) length mismatched, expected: at least 2, actual: {ps.Length}");
                IsValid = false;
                return;
            }
            TrainingGround = (UnitBuilding)Map.Instance.GetProp(ps[0]);
            if (TrainingGround == null)
            {
                this.LogError($"Cannot find unit building {ps[1]}");
                IsValid = false;
                return;
            }
            Weapons = ps.Skip(1).Select(s => Game.CustomizableData.GetNew<IOffensiveCustomizable>(s) as IOffensiveCustomizable);
            if (Weapons.Count() != ps.Length - 1)
            {
                this.LogError($"At least of the following weapons not found: {string.Join(";", ps.Skip(1))}");
                IsValid = false;
                return;
            }
        }
        public override bool RelatedToPlayer(Player p) => base.RelatedToPlayer(p) && TrainingGround.Owner == p && Unit.Owner == p;
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

    // TODO FUT Impl. add Spot command for active spotting

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
                _ = Recorder.Append($" {SpecialSymbols[SpecialCommandResult.CAPTURE_FRIENDLY]} {c}");

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
                _ = Recorder.Append($" {Symbol} {c}");
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
                if (c.Morale <= 0 && c.Owner != Unit.Owner)
                {
                    Player original = c.Owner;
                    c.SetOwner(Unit.Owner);
                    _ = Recorder.Append($"({original}=>{c.Owner})");
                    this.Log($"City's owner changed from {original} to {c.Owner}");
                }
            }
        }
        public override bool RelatedToPlayer(Player p) => (Unit.GetLocatedTile() is Cities city && city.Owner == p) || base.RelatedToPlayer(p);
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
                    _ = Recorder.AppendLine($"{a} {Symbol}");
                    this.Log("Disassembled");
                }
                else
                {
                    this.LogWarning("Already disassembled");
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
                    a.IsAssembled = true;
                    _ = Recorder.AppendLine($"{a} {Symbol}");
                    this.Log("Assembled");
                }
                else
                {
                    this.LogWarning("Already assembled");
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
