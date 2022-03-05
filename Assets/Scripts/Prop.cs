using SteelOfStalin.Attributes;
using SteelOfStalin.Props.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.Json.Serialization;
using SteelOfStalin.Props.Units.Land;
using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Customizables;
using static SteelOfStalin.Util.Utilities;
using Resources = SteelOfStalin.Attributes.Resources;
using Attribute = SteelOfStalin.Attributes.Attribute;

namespace SteelOfStalin.Props
{
    public abstract class Prop : ICloneable
    {
        public Coordinates CoOrds { get; set; }
        public CubeCoordinates CubeCoOrds => (CubeCoordinates)CoOrds;
        public string MeshName { get; set; }

        public Prop() { }
        public Prop(Prop another) => CoOrds = new Coordinates(another.CoOrds);

        public string PrintCoOrds() => $"({CoOrds.X},{CoOrds.Y})";
        public string PrintCubeCoOrds() => $"({CubeCoOrds.X},{CubeCoOrds.Y},{CubeCoOrds.Z})";
        public string PrintMembers()
        {
            string output = "";
            List<string> line = new List<string>() { $"{GetType().Name}\n" };
            GetType().GetProperties().ToList().ForEach(p => line.Add($"{p.Name}\t\t:{p.GetValue(this)}\n"));
            return string.Join(output, line);
        }

        public virtual void RemoveFromScene() => UnityEngine.Object.Destroy(GameObject.Find(MeshName));

        public virtual object Clone()
        {
            Prop copy = (Prop)MemberwiseClone();
            copy.CoOrds = (Coordinates)CoOrds.Clone();
            return copy;
        }
    }

    public class PropObject : MonoBehaviour
    {
        public AudioClip AudioOnPlaced { get; set; }
        public AudioClip AudioOnClicked { get; set; }
        public AudioClip AudioOnDestroy { get; set; }
        public AudioClip AudioOnFire { get; set; }
        public AudioClip AudioOnMove { get; set; }

        public string PrintOnScreenCoOrds() => $"({gameObject.transform.position.x},{gameObject.transform.position.y},{gameObject.transform.position.z})";

        public virtual void Start()
        {
            AudioSource placed = gameObject.AddComponent<AudioSource>();
            placed.name = "Placed";
            placed.clip = AudioOnPlaced;

            if (AudioOnPlaced != null)
            {
                placed.Play();
            }
        }

        public virtual void OnMouseDown()
        {
            
        }

        public virtual void OnDestroy()
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
    [Flags]
    public enum AutoCommands
    {
        NONE = 0,
        MOVE = 1 << 0,
        FIRE = 1 << 1,
        RESUPPLY = 1 << 2,
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
        public List<Tile> AutoNavigationPath { get; set; } = new List<Tile>();

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

        // Parameterless constructors are used for (de)serialization
        public Unit() : base() { }
        public Unit(Unit another) : base(another)
            => (Owner, Cost, Maneuverability, Defense, Consumption, Carrying, Capacity, Scouting, Morale)
            = (another.Owner, 
                (Cost)another.Cost.Clone(), 
                (Maneuverability)another.Maneuverability.Clone(), 
                (Defense)another.Defense.Clone(), 
                (Resources)another.Consumption.Clone(), 
                (Resources)another.Carrying.Clone(), 
                (Resources)another.Capacity.Clone(), 
                (Scouting)another.Scouting.Clone(), 
                (Attribute)another.Morale.Clone());

        // TODO FUT Impl. handle same type but different altitude (e.g. planes at and above airfield)
        public virtual bool CanAccessTile(Tile t)
        {
            IEnumerable<Unit> units = Map.Instance.GetUnits(t);
            // either the tile does not have any unit on it, or none is of the same category as this unit
            // TODO FUT Impl. consider altitude of the units as well
            return !units.Any() || !units.Any(u => IsOfSameCategory(u));
        }
        public virtual bool CanMove() => GetAccessibleNeigbours((int)Maneuverability.Speed.ApplyMod()).Any() && !IsSuppressed;
        public virtual bool CanMerge()
        {
            // TODO FUT Impl. 
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
                this.LogError("Weapon is null.");
                return false;
            }
            return Carrying.HasEnoughResources(normal ? weapon.ConsumptionNormal : weapon.ConsumptionSuppress);
        }

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
                this.LogError("Only units with fuel capacity can be used with fuel cost optimization for pathfinding.");
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
            while (active.Any())
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
            // TODO FUT Impl. 
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

        public abstract override object Clone();
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

        public Building() : base() { }
        public Building(Building another) : base(another)
            => (Name, Owner, Status, Level, MaxLevel, Size, Cost, Durability, Scouting, DestroyTerrainOnBuilt, ConstructionTimeRemaining)
            = (another.Name, another.Owner, another.Status, another.Level, another.MaxLevel, another.Size, (Cost)another.Cost.Clone(), (Attribute)another.Durability.Clone(), (Scouting)another.Scouting.Clone(), another.DestroyTerrainOnBuilt, another.ConstructionTimeRemaining);

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        // public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) /*&& !IsNeutral()*/;

        public abstract override object Clone();
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
        OCEAN,
        SWAMP,
        DESERT,
        HILLOCK,
        HILLS,
        MOUNTAINS,
        ROCKS,
        SUBURB,
        CITY,
        METROPOLIS
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

        public Cities() : base() { }
        public Cities(Cities another) : base(another)
            => (Owner, Population, ConstructionRange, Production, Durability, Morale)
            = (another.Owner, another.Population, (Attribute)another.ConstructionRange.Clone(), (Resources)another.Production.Clone(), (Attribute)another.Durability.Clone(), (Attribute)another.Morale.Clone());

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) && !IsNeutral();

        public abstract override object Clone();
    }

    public abstract class Tile : Prop
    {
        public string Name { get; set; }
        public TileType Type { get; set; }
        public Accessibility Accessibility { get; set; }
        public TerrainModifier TerrainMod { get; set; } = new TerrainModifier();
        public double Obstruction { get; set; }
        public bool AllowConstruction { get; set; }
        public double Height { get; set; }
        public char Symbol { get; set; }

        [JsonIgnore] public bool IsWater => Type == TileType.STREAM || Type == TileType.RIVER || Type == TileType.OCEAN || Type == TileType.SWAMP;
        [JsonIgnore] public bool IsHill => Type is TileType.HILLOCK || Type is TileType.HILLS || Type is TileType.MOUNTAINS;
        [JsonIgnore] public bool IsFlatLand => !IsWater && !IsHill && Type != TileType.BOUNDARY;
        [JsonIgnore] public bool IsCity => Type is TileType.SUBURB || Type is TileType.CITY || Type is TileType.METROPOLIS;
        [JsonIgnore] public bool HasUnit => Map.Instance.GetUnits(this).Any();
        [JsonIgnore] public bool HasBuilding => Map.Instance.GetBuildings(this).Any();
        [JsonIgnore] public bool IsOccupied => HasUnit || HasBuilding;

        public Tile() : base() { }
        public Tile(Tile another) : base(another) 
            => (Name, Type, Accessibility, TerrainMod, Height, Symbol)
            = (another.Name, another.Type, another.Accessibility, (TerrainModifier)another.TerrainMod.Clone(), another.Height, another.Symbol);

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
        public abstract override object Clone();

        public static bool operator ==(Tile t1, Tile t2) => t1?.CoOrds.X == t2?.CoOrds.X && t2?.CoOrds.Y == t2?.CoOrds.Y;
        public static bool operator !=(Tile t1, Tile t2) => !(t1?.CoOrds.X == t2?.CoOrds.X && t2?.CoOrds.Y == t2?.CoOrds.Y);
    }

    public class WeightedTile
    {
        public WeightedTile Parent { get; set; }
        public CubeCoordinates CubeCoOrds { get; set; }
        public double BaseCost { get; set; }
        public double Weight { get; set; }
        // use 2 for tile pathfinding
        public double MaxWeight { get; set; } = 2;
        public double SuppliesCostSoFar { get; set; }
        public double FuelCostSoFar { get; set; }
        public int DistanceSoFar { get; set; }
        public int DistanceToGoal { get; set; }
        public double SuppliesCostDistance => SuppliesCostSoFar + DistanceToGoal * BaseCost * MaxWeight;
        public double FuelCostDistance => FuelCostSoFar + DistanceToGoal * BaseCost * MaxWeight;

        public override bool Equals(object obj) => this == (WeightedTile)obj;
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(WeightedTile w1, WeightedTile w2) => w1?.CubeCoOrds == w2?.CubeCoOrds;
        public static bool operator !=(WeightedTile w1, WeightedTile w2) => !(w1?.CubeCoOrds == w2?.CubeCoOrds);
    }

    public sealed class Boundary : Tile
    {
        public Boundary() : base() { }
        public Boundary(Boundary another) : base(another) { }
        public override object Clone() => new Boundary(this);
    }
    public sealed class Plains : Tile
    {
        public Plains() : base() { }
        public Plains(Plains another) : base(another) { }
        public override object Clone() => new Plains(this);
    }
    public sealed class Grassland : Tile
    {
        public Grassland() : base() { }
        public Grassland(Grassland another) : base(another) { }
        public override object Clone() => new Grassland(this);
    }
    public sealed class Forest : Tile
    {
        public Forest() : base() { }
        public Forest(Forest another) : base(another) { }
        public override object Clone() => new Forest(this);
    }
    public sealed class Jungle : Tile
    {
        public Jungle() : base() { }
        public Jungle(Jungle another) : base(another) { }
        public override object Clone() => new Jungle(this);
    }
    public sealed class Stream : Tile
    {
        public Stream() : base() { }
        public Stream(Stream another) : base(another) { }
        public override object Clone() => new Stream(this);
    }
    public sealed class River : Tile
    {
        public River() : base() { }
        public River(River another) : base(another) { }
        public override object Clone() => new River(this);
    }
    public sealed class Ocean : Tile
    {
        public Ocean() : base() { }
        public Ocean(Ocean another) : base(another) { }
        public override object Clone() => new Ocean(this);
    }
    public sealed class Swamp : Tile
    {
        public Swamp() : base() { }
        public Swamp(Swamp another) : base(another) { }
        public override object Clone() => new Swamp(this);
    }
    public sealed class Desert : Tile
    {
        public Desert() : base() { }
        public Desert(Desert another) : base(another) { }
        public override object Clone() => new Desert(this);
    }
    public sealed class Hillock : Tile
    {
        public Hillock() : base() { }
        public Hillock(Hillock another) : base(another) { }
        public override object Clone() => new Hillock(this);
    }
    public sealed class Hills : Tile
    {
        public Hills() : base() { }
        public Hills(Hills another) : base(another) { }
        public override object Clone() => new Hills(this);
    }
    public sealed class Mountains : Tile
    {
        public Mountains() : base() { }
        public Mountains(Mountains another) : base(another) { }
        public override object Clone() => new Mountains(this);
    }
    public sealed class Rocks : Tile
    {
        public Rocks() : base() { }
        public Rocks(Rocks another) : base(another) { }
        public override object Clone() => new Rocks(this);
    }
    public sealed class Suburb : Cities
    {
        public Suburb() : base() { }
        public Suburb(Suburb another) : base(another) { }
        public override object Clone() => new Suburb(this);
    }
    public sealed class City : Cities
    {
        public City() : base() { }
        public City(City another) : base(another) { }
        public override object Clone() => new City(this);
    }
    public sealed class Metropolis : Cities
    {
        public Metropolis() : base() { }
        public Metropolis(Metropolis another) : base(another) { }
        public override object Clone() => new Metropolis(this);
    }
}