using SteelOfStalin.Attributes;
using SteelOfStalin.Props.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteelOfStalin.Props.Buildings
{
    public abstract class UnitBuilding : Building
    {
        public Attribute QueueCapacity { get; set; } = new Attribute(10, new Modifier(ModifierType.FIXED_VALUE, 2));
        public List<Unit> TrainingQueue { get; set; } = new List<Unit>();
        public double CurrentQueueTime { get; set; }
        public List<Unit> ReadyToDeploy { get; set; } = new List<Unit>();
        public Attribute DeployRange { get; set; } = new Attribute(1, new Modifier(ModifierType.FIXED_VALUE, 0.25));

        public UnitBuilding() : base() { }
    }
    public abstract class ResourcesBuilding : Building
    {
        public enum ResourceBuildingStatus { None, Operating, ProductionHalted }
        public ResourceBuildingStatus RBStatus { get; set; } = ResourceBuildingStatus.None;
        public Attributes.Resources Production { get; set; } = new Attributes.Resources();

    }
    public abstract class Infrastructure : Building
    {

    }
    public abstract class TransmissionBuilding : Building
    {
        public Attribute EffectiveRange { get; set; } = new Attribute(5, new Modifier(ModifierType.FIXED_VALUE, 0.5));
    }
    public abstract class DefensiveBuilding : Building
    {
        public Offense Offense { get; set; } = new Offense();
        public Defense Defense { get; set; } = new Defense();
    }
}