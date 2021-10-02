using SteelOfStalin.Attributes;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace SteelOfStalin.Props
{
    public abstract class Prop : MonoBehaviour
    {
        // TODO add ignore attributes later after deciding serialization format
        protected AudioSource audioOnPlaced { get; set; }
        protected AudioSource audioOnDestroy { get; set; }
        protected abstract void Load();
    }
}

namespace SteelOfStalin.Props.Units
{
    public enum UnitStatus
    {
        NONE,
        IN_QUEUE,
        CAN_BE_DEPLOYED,
        ACTIVE,
        MOVED,
        FIRED,
        WRECKED,
        DESTROYED
    }

    public abstract class Unit : Prop
    {
        public UnitStatus Status { get; set; }
        public string Name { get; set; }
        public Player Owner { get; set; }
        public Point CoOrds { get; set; }
        public Cost Cost { get; set; } = new Cost();
        public Maneuverability Maneuverability { get; set; } = new Maneuverability();
        public Defense Defense { get; set; } = new Defense();
        public Attributes.Resources Consumption { get; set; } = new Attributes.Resources();
        public Attributes.Resources Carrying { get; set; } = new Attributes.Resources();
        public Attributes.Resources Capacity { get; set; } = new Attributes.Resources();
        public Scouting Scouting { get; set; } = new Scouting();
        public Attribute Morale { get; set; } = new Attribute(100);

        public double CurrentSuppressionLevel { get; set; } = 0;
        public int LastSuppressedRound { get; set; } = 0;
        public bool IsSuppressed { get; set; } = false;
        public bool IsDisconnected { get; set; } = false;
        public double TrainingTimeRemaining { get; set; }

        public bool IsCommandSet { get; set; } = false;

        protected AudioSource audioOnFire { get; set; }
        protected AudioSource audioOnMove { get; set; }

        // Parameterless constructors are used for (de)serialization
        public Unit() : base() { }
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

    public abstract class Building : Prop
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
        public TileType Type { get; set; }
        public TerrainModifier TerrainMod { get; set; }
        public double Obstruction { get; set; }
        public bool AllowConstruction { get; set; }
        public double Height { get; set; }
    }
}