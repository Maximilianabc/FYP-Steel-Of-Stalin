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

namespace SteelOfStalin.Props
{
    public abstract class Prop : MonoBehaviour
    {
        // TODO add ignore attributes later after deciding serialization format
        protected AudioSource AudioOnPlaced { get; set; }
        protected AudioSource AudioOnDestroy { get; set; }
        protected abstract void Load();
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
        FIRE = 1 << 2,

    }

    public abstract class Unit : Prop, ICloneable
    {
        public UnitStatus Status { get; set; }
        public string Name { get; set; }
        public Player Owner { get; set; }
        public Point CoOrds { get; set; }
        public CubeCoordinates CubeCoOrds => (CubeCoordinates)CoOrds;
        public Cost Cost { get; set; } = new Cost();
        public Maneuverability Maneuverability { get; set; } = new Maneuverability();
        public Defense Defense { get; set; } = new Defense();
        public Resources Consumption { get; set; } = new Resources();
        public Resources Carrying { get; set; } = new Resources();
        public Resources Capacity { get; set; } = new Resources();
        public Scouting Scouting { get; set; } = new Scouting();
        public Attribute Morale { get; set; } = new Attribute(100);

        public AvailableCommands AvailableCommands { get; set; }
        public AutoCommands AutoCommands { get; set; }
        public Stack<Tile> AutoNavigationPath { get; set; } = new Stack<Tile>();

        public double CurrentSuppressionLevel { get; set; } = 0;
        public int LastSuppressedRound { get; set; } = 0;
        public double TrainingTimeRemaining { get; set; }

        public bool IsCommandSet { get; set; } = false;

        protected AudioSource AudioOnFire { get; set; }
        protected AudioSource AudioOnMove { get; set; }

        // Parameterless constructors are used for (de)serialization
        public Unit() : base() { }

        public virtual void Move()
        {

        }

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
        public Player Owner { get; set; }
        public BuildingStatus Status { get; set; } = BuildingStatus.NONE;
        public Point CoOrds { get; set; } = new Point();
        public double Level { get; set; }
        public double MaxLevel { get; set; }
        public double Size { get; set; }
        public Cost Cost { get; set; } = new Cost();
        public Attribute Durability { get; set; } = new Attribute();
        public Scouting Scouting { get; set; } = new Scouting();
        public bool DestroyTerrainOnBuilt { get; set; } = true;
        public double ConstructionTimeRemaining { get; set; }

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
        METROPOLIS
    }

    public abstract class Tile : Prop
    {
        public string Name { get; set; }
        public Point CoOrds { get; set; } = new Point();
        public CubeCoordinates CubeCoOrds => (CubeCoordinates)CoOrds;
        public TileType Type { get; set; }
        public TerrainModifier TerrainMod { get; set; }
        public double Obstruction { get; set; }
        public bool AllowConstruction { get; set; }
        public double Height { get; set; }

        public WeightedTile ConvertToWeightedTile(Attributes.Resources consumption, PathfindingOptimization opt, Tile end, WeightedTile parent = null) => new WeightedTile()
        {
            Parent = parent,
            CubeCoOrds = CubeCoOrds,
            BaseCost = opt == PathfindingOptimization.LEAST_SUPPLIES_COST ? consumption.Supplies.ApplyMod() : consumption.Fuel.ApplyMod(),
            Weight = opt == PathfindingOptimization.LEAST_SUPPLIES_COST ? TerrainMod.Supplies.Apply() : TerrainMod.Fuel.Apply(),
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
}