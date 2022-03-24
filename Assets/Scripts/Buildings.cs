using SteelOfStalin.Attributes;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin.Assets.Props.Buildings
{
    public abstract class UnitBuilding : Building
    {
        public Attribute QueueCapacity { get; set; } = new Attribute(7);
        public Queue<Unit> TrainingQueue { get; set; } = new Queue<Unit>();
        public List<Unit> ReadyToDeploy { get; set; } = new List<Unit>();
        public Attribute DeployRange { get; set; } = new Attribute(1);
        public decimal CurrentQueueTime => TrainingQueue.LastOrDefault()?.TrainingTimeRemaining ?? 0;

        public UnitBuilding() : base() { }
        public UnitBuilding(UnitBuilding another) : base(another)
            => (QueueCapacity, TrainingQueue, ReadyToDeploy, DeployRange)
            = ((Attribute)another.QueueCapacity.Clone(),
                new Queue<Unit>(another.TrainingQueue),
                new List<Unit>(another.ReadyToDeploy),
                (Attribute)another.DeployRange.Clone());

        public abstract override object Clone();

        public virtual bool CanTrain() => Status == BuildingStatus.ACTIVE && TrainingQueue.Count < QueueCapacity;

        // has deployable units and has at least 1 available neighbours to deploy any of them 
        public virtual bool CanDeploy() => Status == BuildingStatus.ACTIVE && ReadyToDeploy.Count > 0 && ReadyToDeploy.Any(u => GetDeployableDestinations(u).Any());

        public IEnumerable<Tile> GetDeployableDestinations(Unit u) => u.GetAccessibleNeigbours(CubeCoOrds, (int)DeployRange.ApplyMod());
    }
    public abstract class ProductionBuilding : Building
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ProductionBuildingStatus
        {
            NONE,
            OPERATING,
            HALTED
        }
        public ProductionBuildingStatus ProductionStatus { get; set; } = ProductionBuildingStatus.NONE;
        public Resources Production { get; set; } = new Resources();

        public ProductionBuilding() : base() { }
        public ProductionBuilding(ProductionBuilding another) : base(another) => (ProductionStatus, Production) = (another.ProductionStatus, (Resources)another.Production.Clone());

        public abstract override object Clone();
    }
    public abstract class Infrastructure : Building
    {
        public Infrastructure() : base() { }
        public Infrastructure(Infrastructure another) : base(another) { }

        public abstract override object Clone();
    }
    public abstract class TransmissionBuilding : Building
    {
        public Attribute EffectiveRange { get; set; } = new Attribute();

        public TransmissionBuilding() : base() { }
        public TransmissionBuilding(TransmissionBuilding another) : base(another) => EffectiveRange = (Attribute)another.EffectiveRange.Clone();

        public abstract override object Clone();
    }
    public abstract class DefensiveBuilding : Building
    {
        public Offense Offense { get; set; } = new Offense();
        public Defense Defense { get; set; } = new Defense();

        public DefensiveBuilding() : base() { }
        public DefensiveBuilding(DefensiveBuilding another) : base(another) => (Offense, Defense) = ((Offense)another.Offense.Clone(), (Defense)another.Defense.Clone());

        public abstract override object Clone();
    }
}

namespace SteelOfStalin.Assets.Props.Buildings.Units
{
    public sealed class Barracks : UnitBuilding
    {
        public Barracks() : base() { }
        public Barracks(Barracks another) : base(another) { }
        public override object Clone() => new Barracks(this);
    }
    public sealed class Arsenal : UnitBuilding
    {
        public Arsenal() : base() { }
        public Arsenal(Arsenal another) : base(another) { }
        public override object Clone() => new Arsenal(this);
    }
    public sealed class Dockyard : UnitBuilding
    {
        public Dockyard() : base() { }
        public Dockyard(Dockyard another) : base(another) { }
        public override object Clone() => new Dockyard(this);
    }
    public sealed class Airfield : UnitBuilding
    {
        public Airfield() : base() { }
        public Airfield(Airfield another) : base(another) { }
        public override object Clone() => new Airfield(this);
    }
}

namespace SteelOfStalin.Assets.Props.Buildings.Productions
{
    public sealed class Foundry : ProductionBuilding
    {
        public Foundry() : base() { }
        public Foundry(Foundry another) : base(another) { }
        public override object Clone() => new Foundry(this);
    }
    public sealed class Industries : ProductionBuilding
    {
        public Industries() : base() { }
        public Industries(Industries another) : base(another) { }
        public override object Clone() => new Industries(this);
    }
    public sealed class AmmoFactory : ProductionBuilding
    {
        public bool ProduceCartridges { get; set; } = true;

        public AmmoFactory() : base() { }
        public AmmoFactory(AmmoFactory another) : base(another) => ProduceCartridges = another.ProduceCartridges;
        public override object Clone() => new AmmoFactory(this);
    }
    public sealed class Refinery : ProductionBuilding
    {
        public Refinery() : base() { }
        public Refinery(Refinery another) : base(another) { }
        public override object Clone() => new Refinery(this);
    }
    public sealed class Quarry : ProductionBuilding
    {
        public Quarry() : base() { }
        public Quarry(Quarry another) : base(another) { }
        public override object Clone() => new Quarry(this);
    }
    public sealed class PowerPlant : ProductionBuilding
    {
        public PowerPlant() : base() { }
        public PowerPlant(PowerPlant another) : base(another) { }
        public override object Clone() => new PowerPlant(this);
    }
}

namespace SteelOfStalin.Assets.Props.Buildings.Infrastructures
{
    public sealed class Road : Infrastructure
    {
        public Road() : base() { }
        public Road(Road another) : base(another) { }
        public override object Clone() => new Road(this);
    }
    public sealed class Railway : Infrastructure
    {
        public Railway() : base() { }
        public Railway(Railway another) : base(another) { }
        public override object Clone() => new Railway(this);
    }
    public sealed class Bridge : Infrastructure
    {
        public Bridge() : base() { }
        public Bridge(Bridge another) : base(another) { }
        public override object Clone() => new Bridge(this);
    }
    public sealed class Depot : Infrastructure
    {
        public Depot() : base() { }
        public Depot(Depot another) : base(another) { }
        public override object Clone() => new Depot(this);
    }
    public sealed class Outpost : Infrastructure
    {
        public Outpost() : base() { }
        public Outpost(Outpost another) : base(another) { }
        public override object Clone() => new Outpost(this);
    }
}

namespace SteelOfStalin.Assets.Props.Buildings.Transmissions
{
    public sealed class Watchtower : TransmissionBuilding
    {
        public Watchtower() : base() { }
        public Watchtower(Watchtower another) : base(another) { }
        public override object Clone() => new Watchtower(this);
    }
    public sealed class SignalTower : TransmissionBuilding
    {
        public SignalTower() : base() { }
        public SignalTower(SignalTower another) : base(another) { }
        public override object Clone() => new SignalTower(this);
    }
    public sealed class JammingTower : TransmissionBuilding
    {
        public JammingTower() : base() { }
        public JammingTower(JammingTower another) : base(another) { }
        public override object Clone() => new JammingTower(this);
    }
    public sealed class RadarTower : TransmissionBuilding
    {
        public RadarTower() : base() { }
        public RadarTower(RadarTower another) : base(another) { }
        public override object Clone() => new RadarTower(this);
    }
}

namespace SteelOfStalin.Assets.Props.Buildings.Defensives
{
    public sealed class Trench : DefensiveBuilding
    {
        public Trench() : base() { }
        public Trench(Trench another) : base(another) { }
        public override object Clone() => new Trench(this);
    }
    public sealed class Foxhole : DefensiveBuilding
    {
        public Foxhole() : base() { }
        public Foxhole(Foxhole another) : base(another) { }
        public override object Clone() => new Foxhole(this);
    }
    public sealed class Pillbox : DefensiveBuilding
    {
        public Pillbox() : base() { }
        public Pillbox(Pillbox another) : base(another) { }
        public override object Clone() => new Pillbox(this);
    }
    public sealed class Bunker : DefensiveBuilding
    {
        public Bunker() : base() { }
        public Bunker(Bunker another) : base(another) { }
        public override object Clone() => new Bunker(this);
    }
    public sealed class Wires : DefensiveBuilding
    {
        public Wires() : base() { }
        public Wires(Wires another) : base(another) { }
        public override object Clone() => new Wires(this);
    }
    public sealed class TankTraps : DefensiveBuilding
    {
        public TankTraps() : base() { }
        public TankTraps(TankTraps another) : base(another) { }
        public override object Clone() => new TankTraps(this);
    }
    public sealed class Minefield : DefensiveBuilding
    {
        public Minefield() : base() { }
        public Minefield(Minefield another) : base(another) { }
        public override object Clone() => new Minefield(this);
    }
}