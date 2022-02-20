using SteelOfStalin.Attributes;
using SteelOfStalin.Props.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Resources = SteelOfStalin.Attributes.Resources;
using Attribute = SteelOfStalin.Attributes.Attribute;
using SteelOfStalin.Props.Buildings;
using System.Text.Json.Serialization;
using static SteelOfStalin.Util.Utilities;
using SteelOfStalin.Props.Units.Land;
using SteelOfStalin.Customizables;

namespace SteelOfStalin.Props
{
    public abstract class Prop : MonoBehaviour
    {
        public Point CoOrds { get; set; }
        public CubeCoordinates CubeCoOrds => (CubeCoordinates)CoOrds;

        // TODO add ignore attributes later after deciding serialization format
        protected AudioSource AudioOnPlaced { get; set; }
        protected AudioSource AudioOnDestroy { get; set; }

        public string PrintCoOrds() => $"({CoOrds.X},{CoOrds.Y})";
        public string PrintCubeCoOrds() => $"({CubeCoOrds.X},{CubeCoOrds.Y},{CubeCoOrds.Z})";
        public string PrintOnScreenCoOrds() => $"({gameObject.transform.position.x},{gameObject.transform.position.y},{gameObject.transform.position.z})";

        protected virtual void Load() { }
        public virtual void RemoveFromScene() => Destroy(gameObject);

        public void Start()
        {
            
        }

        public void OnDestroy()
        {
            
        }
    }
}

namespace SteelOfStalin.Props.Units
{
    [Flags]
    public enum UnitStatus
    {
        NONE = 0,
        IN_QUEUE = 1 << 0,
        CAN_BE_DEPLOYED = 1 << 1,
        ACTIVE = 1 << 2,
        MOVED = 1 << 3,
        FIRED = 1 << 4,
        SUPPRESSED = 1 << 5,
        DISCONNECTED = 1 << 6,
        WRECKED = 1 << 7,
        DESTROYED = 1 << 8,
    }
    [Flags]
    public enum AvailableCommands
    {
        NONE = 0,
        HOLD = 1 << 0,
        MOVE = 1 << 1,
        CAPTURE = 1 << 2,
        ABOARD = 1 << 3,
        DISEMBARK = 1 << 4,
        MERGE = 1 << 5,
        FIRE = 1 << 6,
        SUPPRESS = 1 << 7,
        SABOTAGE = 1 << 8,
        AMBUSH = 1 << 9,
        FORTIFY = 1 << 10,
        CONSTRUCT = 1 << 11,
        DEMOLISH = 1 << 12,
        SCAVENGE = 1 << 13,
        ASSEMBLE = 1 << 14,
        DISASSEMBLE = 1 << 15
    }
    public enum CommandAssigned
    {
        NONE,
        HOLD,
        MOVE,
        CAPTURE,
        ABOARD,
        DISEMBARK,
        MERGE,
        FIRE,
        SUPPRESS,
        SABOTAGE,
        AMBUSH,
        FORTIFY,
        CONSTRUCT,
        DEMOLISH,
        SCAVENGE,
        ASSEMBLE,
        DISASSEMBLE
    }

    public abstract class Unit : Prop, ICloneable
    {
        public UnitStatus Status { get; set; }
        public string Name { get; set; }
        [JsonIgnore] public Player Owner { get; set; }
        public Cost Cost { get; set; } = new Cost();
        public Maneuverability Maneuverability { get; set; } = new Maneuverability();
        public Defense Defense { get; set; } = new Defense();
        public Resources Consumption { get; set; } = new Resources();
        public Resources Carrying { get; set; } = new Resources();
        public Resources Capacity { get; set; } = new Resources();
        public Scouting Scouting { get; set; } = new Scouting();
        public Attribute Morale { get; set; } = new Attribute(100);

        public List<Unit> UnitsInSight { get; set; } = new List<Unit>();
        public List<Unit> UnitsUnknown { get; set; } = new List<Unit>();
        public List<Building> BuildingsInSight { get; set; } = new List<Building>();

        public CommandAssigned CommandAssigned { get; set; } = CommandAssigned.NONE;
        public AvailableCommands AvailableCommands { get; set; } = AvailableCommands.HOLD;
        public AutoCommands AutoCommands { get; set; }
        public Stack<Tile> AutoNavigationPath { get; set; } = new Stack<Tile>();

        public double CurrentSuppressionLevel { get; set; } = 0;
        public int ConsecutiveSuppressedRound { get; set; } = 0;
        public double TrainingTimeRemaining { get; set; }

        public bool IsSuppressed => Status.HasFlag(UnitStatus.SUPPRESSED);
        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) && !IsNeutral();
        public bool IsSameSubclassOf<T>(Unit another) where T : Unit => GetType().IsSubclassOf(typeof(T)) && another.GetType().IsSubclassOf(typeof(T));
        public bool IsOfSameCategory(Unit another) => IsSameSubclassOf<Ground>(another) || IsSameSubclassOf<Naval>(another) || IsSameSubclassOf<Aerial>(another);

        protected AudioSource AudioOnFire { get; set; }
        protected AudioSource AudioOnMove { get; set; }

        // Parameterless constructors are used for (de)serialization
        public Unit() : base() { }
        public Unit(Player owner) => Owner = owner;

        // TODO handle same type but different altitude (e.g. planes at and above airfield)
        public virtual bool CanAccessTile(Tile t)
        {
            IEnumerable<Unit> units = Map.Instance.GetUnits(t);
            // either the tile does not have any unit on it, or none is of the same category as this unit
            // TODO consider altitude of the units as well
            return !units.Any() || !units.Any(u => IsOfSameCategory(u));
        }
        public virtual bool CanMove() => GetAccessibleNeigbours((int)Maneuverability.Speed.ApplyMod()).Any() && !IsSuppressed;
        public virtual bool CanMerge()
        {
            // TODO
            return false;
        }
        public virtual bool CanFire() => !IsSuppressed && GetWeapons().Any(w => HasHostileUnitsInFiringRange(w) && HasEnoughAmmo(w));
        public virtual bool CanSabotage() => !IsSuppressed && GetWeapons().Any(w => HasHostileBuildingsInFiringRange(w) && HasEnoughAmmo(w));
        // can be fired upon = can be suppressed
        public virtual bool CanSuppress() => !IsSuppressed && GetWeapons().Any(w => HasHostileUnitsInFiringRange(w) && HasEnoughAmmo(w, false));
        public virtual bool CanAmbush()
        {
            // TODO
            return false;
        }

        // has any tile in range that has hostile units
        public bool HasHostileUnitsInFiringRange(IOffensiveCustomizable weapon) => GetFiringRange(weapon).Where(t => t.HasUnit).Any(t => Map.Instance.GetUnits(t).Any(u => !u.IsFriendly(Owner) && HasSpotted(u)));
        // has any buildings in range that has hostile buildings
        public bool HasHostileBuildingsInFiringRange(IOffensiveCustomizable weapon) => GetFiringRange(weapon).Where(t => t.HasBuilding).Any(t => Map.Instance.GetBuildings(t).Any(b => !b.IsFriendly(Owner) && HasSpotted(b)));
        // true for normal, false for suppress
        public bool HasEnoughAmmo(IOffensiveCustomizable weapon, bool normal = true)
        {
            if (weapon == null)
            {
                LogError(this, "Weapon is null.");
                return false;
            }
            return Carrying.HasEnoughResources(normal ? weapon.ConsumptionNormal : weapon.ConsumptionSuppress);
        }
        // TODO should targets be able to fired upon only if they are spotted by this unit or any other units?
        public bool HasSpotted(Unit observee) => Owner.GetAllUnitsInSight().Contains(observee);
        public bool HasSpotted(Building building) => Owner.GetAllBuildingsInSight().Contains(building);

        public IEnumerable<Tile> GetAccessibleNeigbours(int distance = 1) 
            => Map.Instance.GetNeigbours(CubeCoOrds, distance).Where(n => CanAccessTile(n) && GetPath(GetLocatedTile(), n).Any());
        public IEnumerable<Tile> GetAccessibleNeigbours(CubeCoordinates c, int distance = 1)
            => Map.Instance.GetNeigbours(c, distance).Where(n => CanAccessTile(n) && GetPath(Map.Instance.GetTile(c), n).Any());
        public IEnumerable<Tile> GetFiringRange(IOffensiveCustomizable weapon)
        {
            IEnumerable<Tile> range = Map.Instance.GetStraightLineNeighbours(CubeCoOrds, weapon.Offense.MaxRange.ApplyMod());
            if (weapon.Offense.MinRange > 0)
            {
                // exclude tiles within weapon's min range
                range = range.Except(Map.Instance.GetStraightLineNeighbours(CubeCoOrds, weapon.Offense.MinRange.ApplyMod()));
            }
            return range;
        }
        public IEnumerable<Tile> GetReconRange() => Map.Instance.GetStraightLineNeighbours(CubeCoOrds, Scouting.Reconnaissance.ApplyMod());

        public IEnumerable<Tile> GetPath(Tile start, Tile end, PathfindingOptimization opt = PathfindingOptimization.LEAST_SUPPLIES_COST)
        {
            if (this is Personnel && opt == PathfindingOptimization.LEAST_FUEL_COST)
            {
                LogError(this, "Only units with fuel capacity can be used with fuel cost optimization for pathfinding.");
                return new List<Tile>();
            }

            bool is_aerial = this is Aerial;
            WeightedTile w_start = start.ConvertToWeightedTile(Consumption, opt, end, is_aerial);
            WeightedTile w_end = end.ConvertToWeightedTile(Consumption, opt, end, is_aerial);

            List<WeightedTile> active = new List<WeightedTile>();
            List<WeightedTile> visited = new List<WeightedTile>();
            List<Tile> path = new List<Tile>();
            Func<WeightedTile, double> sort =
                opt == PathfindingOptimization.LEAST_SUPPLIES_COST
                ? new Func<WeightedTile, double>(w => w.SuppliesCostDistance)
                : w => w.FuelCostDistance;

            active.Add(w_start);
            while (active.Count > 0)
            {
                WeightedTile w_check = active.OrderBy(sort).FirstOrDefault();
                if (w_check == w_end
                    || w_check.SuppliesCostSoFar > Carrying.Supplies
                    || (Consumption.Fuel > 0 && w_check.FuelCostSoFar > Carrying.Fuel)
                    || w_check.DistanceSoFar > Maneuverability.Speed)
                {
                    while (w_check.Parent != null)
                    {
                        path.Add(Map.Instance.GetTile(w_check.CubeCoOrds));
                        w_check = w_check.Parent;
                    }
                    return path;
                }
                visited.Add(w_check);
                _ = active.Remove(w_check);

                List<WeightedTile> w_neighbours = new List<WeightedTile>();
                GetAccessibleNeigbours(w_check.CubeCoOrds).ToList().ForEach(t => w_neighbours.Add(t.ConvertToWeightedTile(Consumption, opt, end, is_aerial, w_check)));

                w_neighbours.ForEach(n =>
                {
                    if (!visited.Where(v => v == n).Any())
                    {
                        WeightedTile w_exist = active.Where(a => a == n).FirstOrDefault();
                        if (w_exist != null)
                        {
                            // exist in active list
                            bool moreExpensive =
                                opt == PathfindingOptimization.LEAST_SUPPLIES_COST
                                ? w_exist.SuppliesCostDistance > w_check.SuppliesCostDistance
                                : w_exist.FuelCostDistance > w_check.FuelCostDistance;

                            // remove from active list if it costs more than current (w_check)
                            if (moreExpensive)
                            {
                                _ = active.Remove(w_exist);
                            }
                        }
                        else
                        {
                            // add the neigbour to active list
                            active.Add(n);
                        }
                    }
                });
            }
            return path;
        }

        public abstract IEnumerable<IOffensiveCustomizable> GetWeapons();

        public List<Unit> GetAvailableMergeTargets()
        {
            // TODO
            return null;
        }
        // include all enemies, be it spotted or not
        public IEnumerable<Unit> GetHostileUnitsInRange(IEnumerable<Tile> range) => range.Where(t => t.HasUnit).SelectMany(t => Map.Instance.GetUnits(t)).Where(u => !u.IsFriendly(Owner));
        public IEnumerable<Building> GetHostileBuildingsInRange(IEnumerable<Tile> range) => range.Where(t => t.HasBuilding).SelectMany(t => Map.Instance.GetBuildings(t)).Where(b => !b.IsFriendly(Owner));

        public IEnumerable<Unit> GetHostileUnitsInFiringRange(IOffensiveCustomizable weapon) => GetHostileUnitsInRange(GetFiringRange(weapon)).Where(u => HasSpotted(u));
        public IEnumerable<Building> GetHostileBuildingsInFiringRange(IOffensiveCustomizable weapon) => GetHostileBuildingsInRange(GetFiringRange(weapon)).Where(b => HasSpotted(b));
        public IEnumerable<Cities> GetHostileCitiesInRange(IOffensiveCustomizable weapon) => Map.Instance.GetCities(c => c.IsHostile(Owner) && GetFiringRange(weapon).Contains<Tile>(c));
        public IEnumerable<Unit> GetHostileUnitsInReconRange() => GetHostileUnitsInRange(Map.Instance.GetStraightLineNeighbours(CubeCoOrds, Scouting.Reconnaissance.ApplyMod()));
        public IEnumerable<Building> GetHostileBuildingsInReconRange() => GetHostileBuildingsInRange(Map.Instance.GetStraightLineNeighbours(CubeCoOrds, Scouting.Reconnaissance.ApplyMod()));

        public Tile GetLocatedTile() => Map.Instance.GetTile(CoOrds);
        public double GetSuppliesRequired(Tile t) => t.TerrainMod.Supplies.Apply(Consumption.Supplies.ApplyMod());
        public double GetSuppliesRequired(List<Tile> path) => path.Select(t => GetSuppliesRequired(t)).Sum();
        public double GetFuelRequired(Tile t) => t.TerrainMod.Fuel.Apply(Consumption.Fuel.ApplyMod());
        public double GetFuelRequired(List<Tile> path) => path.Select(t => GetFuelRequired(t)).Sum();

        public object Clone()
        {
            Unit copy = (Unit)MemberwiseClone();
            copy.Owner = Owner;
            copy.Cost = (Cost)Cost.Clone();
            copy.Maneuverability = (Maneuverability)Maneuverability.Clone();
            copy.Defense = (Defense)Defense.Clone();
            copy.Consumption = (Resources)Consumption.Clone();
            copy.Carrying = (Resources)Carrying.Clone();
            copy.Capacity = (Resources)Capacity.Clone();
            copy.Scouting = (Scouting)Scouting.Clone();
            copy.Morale = (Attribute)Morale.Clone();
            copy.AutoNavigationPath = new Stack<Tile>(AutoNavigationPath.Reverse());
            return copy;
        }
    }
}

namespace SteelOfStalin.Props.Buildings
{
    public enum BuildingStatus 
    { 
        NONE,
        UNDER_CONSTRUCTION, 
        ACTIVE,
        DESTROYED
    }

    public abstract class Building : Prop, ICloneable
    {
        public string Name { get; set; }
        [JsonIgnore] public Player Owner { get; set; }
        public BuildingStatus Status { get; set; } = BuildingStatus.NONE;
        public double Level { get; set; }
        public double MaxLevel { get; set; }
        public double Size { get; set; }
        public Cost Cost { get; set; } = new Cost();
        public Attribute Durability { get; set; } = new Attribute();
        public Scouting Scouting { get; set; } = new Scouting();
        public bool DestroyTerrainOnBuilt { get; set; } = true;
        public double ConstructionTimeRemaining { get; set; }

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        // public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) /*&& !IsNeutral()*/;

        public object Clone()
        {
            Building copy = (Building)MemberwiseClone();
            copy.Owner = Owner;
            copy.Cost = (Cost)Cost.Clone();
            copy.Durability = (Attribute)Durability.Clone();
            copy.Scouting = (Scouting)Scouting.Clone();
            return copy;
        }
    }
}

namespace SteelOfStalin.Props.Tiles
{
    public enum TileType
    {
        BOUNDARY,
        PLAINS,
        GRASSLAND,
        FOREST,
        JUNGLE,
        STREAM,
        RIVER,
        SWAMP,
        DESERT,
        HILLOCK,
        HILLS,
        MOUNTAINS,
        ROCKS,
        SUBURB,
        CITY,
        METROPOLIS,
        CAPTURABLE = SUBURB | CITY | METROPOLIS
    }

    [Flags]
    public enum Accessibility
    {
        NONE = 0,
        PERSONNEL = 1 << 0,
        ARTILLERY = 1 << 1,
        VEHICLE = 1 << 2,
        VESSEL = 1 << 3,
        PLANE = 1 << 4,
        GROUND = PERSONNEL | ARTILLERY | VEHICLE,
        ALL = ~0
    }

    public abstract class Cities : Tile
    {
        public Player Owner { get; set; }
        public double Population { get; set; }
        public Attribute ConstructionRange { get; set; } = new Attribute();
        public Resources Production { get; set; } = new Resources();
        public Attribute Durability { get; set; } = new Attribute();
        public Attribute Morale { get; set; } = new Attribute(250);

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) && !IsNeutral();
    }

    public abstract class Tile : Prop
    {
        public string Name { get; set; }
        public TileType Type { get; set; }
        public Accessibility Accessibility { get; set; }
        public TerrainModifier TerrainMod { get; set; }
        public double Obstruction { get; set; }
        public bool AllowConstruction { get; set; }
        public double Height { get; set; }
        [JsonIgnore] public char Symbol { get; set; }

        public bool IsCity => TileType.CAPTURABLE.HasFlag(Type);
        public bool HasUnit => Map.Instance.GetUnits(this).Any();
        public bool HasBuilding => Map.Instance.GetBuildings(this).Any();
        public bool IsOccupied => HasUnit || HasBuilding;

        public WeightedTile ConvertToWeightedTile(Resources consumption, PathfindingOptimization opt, Tile end, bool IsAerial, WeightedTile parent = null) => new WeightedTile()
        {
            Parent = parent,
            CubeCoOrds = CubeCoOrds,
            BaseCost = opt == PathfindingOptimization.LEAST_SUPPLIES_COST ? consumption.Supplies.ApplyMod() : consumption.Fuel.ApplyMod(),
            Weight = IsAerial 
                        ? 1 
                        : opt == PathfindingOptimization.LEAST_SUPPLIES_COST 
                            ? TerrainMod.Supplies.Apply() 
                            : TerrainMod.Fuel.Apply(),
            SuppliesCostSoFar = (parent == null ? 0 : parent.SuppliesCostSoFar) + TerrainMod.Supplies.Apply(consumption.Supplies.ApplyMod()),
            FuelCostSoFar = (parent == null ? 0 : parent.FuelCostSoFar) + TerrainMod.Fuel.Apply(consumption.Fuel.ApplyMod()),
            DistanceSoFar = parent == null ? 0 : parent.DistanceSoFar + 1,
            DistanceToGoal = CubeCoordinates.GetDistance(CubeCoOrds, end.CubeCoOrds)
        };

        public override bool Equals(object other) => this == (Tile)other;
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(Tile t1, Tile t2) => t1.CoOrds.X == t2.CoOrds.X && t2.CoOrds.Y == t2.CoOrds.Y;
        public static bool operator !=(Tile t1, Tile t2) => !(t1.CoOrds.X == t2.CoOrds.X && t2.CoOrds.Y == t2.CoOrds.Y);
    }

    public class WeightedTile
    {
        public WeightedTile Parent { get; set; }
        public CubeCoordinates CubeCoOrds { get; set; }
        public double BaseCost { get; set; }
        public double Weight { get; set; }
        public double SuppliesCostSoFar { get; set; }
        public double FuelCostSoFar { get; set; }
        public int DistanceSoFar { get; set; }
        public int DistanceToGoal { get; set; }
        public double SuppliesCostDistance => SuppliesCostSoFar + DistanceToGoal * BaseCost * 2;
        public double FuelCostDistance => FuelCostSoFar + DistanceToGoal * BaseCost * 2;

        public override bool Equals(object obj) => this == (WeightedTile)obj;
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(WeightedTile w1, WeightedTile w2) => w1?.CubeCoOrds == w2?.CubeCoOrds;
        public static bool operator !=(WeightedTile w1, WeightedTile w2) => !(w1?.CubeCoOrds == w2?.CubeCoOrds);
    }


    public sealed class Boundary : Tile
    {

    }
    public sealed class Plains : Tile
    {

    }
    public sealed class Grassland : Tile
    {

    }
    public sealed class Forest : Tile
    {

    }
    public sealed class Jungle : Tile
    {

    }
    public sealed class Stream : Tile
    {

    }
    public sealed class River : Tile
    {

    }
    public sealed class Swamp : Tile
    {

    }
    public sealed class Desert : Tile
    {

    }
    public sealed class Hillock : Tile
    {

    }
    public sealed class Hills : Tile
    {

    }
    public sealed class Mountains : Tile
    {

    }
    public sealed class Rocks : Tile
    {

    }
    public sealed class Suburb : Cities
    {

    }
    public sealed class City : Cities
    {

    }
    public sealed class Metropolis : Cities
    {

    }
}