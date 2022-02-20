using SteelOfStalin.Attributes;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin.Props.Buildings
{
    public abstract class UnitBuilding : Building
    {
        public Attribute QueueCapacity { get; set; } = new Attribute(10);
        public Queue<Unit> TrainingQueue { get; set; } = new Queue<Unit>();
        public double CurrentQueueTime => TrainingQueue.LastOrDefault()?.TrainingTimeRemaining ?? 0;
        public List<Unit> ReadyToDeploy { get; set; } = new List<Unit>();
        public Attribute DeployRange { get; set; } = new Attribute(1);

        public UnitBuilding() : base() { }

        public virtual bool CanTrain() => Status == BuildingStatus.ACTIVE && TrainingQueue.Count < QueueCapacity;

        // has deployable units and has at least 1 available neighbours to deploy any of them 
        public virtual bool CanDeploy() => Status == BuildingStatus.ACTIVE && ReadyToDeploy.Count > 0 && ReadyToDeploy.Any(u => GetDeployableDestinations(u).Any());

        public IEnumerable<Tile> GetDeployableDestinations(Unit u) => u.GetAccessibleNeigbours(CubeCoOrds, (int)DeployRange.ApplyMod());
    }
    public abstract class ResourcesBuilding : Building
    {
        public enum ResourceBuildingStatus 
        { 
            None, 
            Operating,
            ProductionHalted
        }
        public ResourceBuildingStatus RBStatus { get; set; } = ResourceBuildingStatus.None;
        public Resources Production { get; set; } = new Resources();
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