using SteelOfStalin.Attributes;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SteelOfStalin.Util.Utilities;
using Attribute = SteelOfStalin.Attributes.Attribute;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin.Assets.Props
{
    public abstract class Prop : ICloneable, IEquatable<Prop>, INamedAsset
    {
        public Coordinates CoOrds { get; set; }
        public CubeCoordinates CubeCoOrds => (CubeCoordinates)CoOrds;
        public string Name { get; set; }
        public string MeshName { get; set; }

        public Prop() { }
        public Prop(Prop another) => CoOrds = new Coordinates(another.CoOrds);

        // use CoOrds.ToString() and CubeCoOrds.ToString() directly for printing coords and cube coords
        public string PrintMembers()
        {
            string sep = Environment.NewLine;
            List<string> line = new List<string>() { $"{GetType().Name}" };
            GetType().GetProperties().ToList().ForEach(p => line.Add($"{p.Name}\t\t:{p.GetValue(this)}"));
            return string.Join(sep, line);
        }

        public virtual void AddToScene()
        {
            string scene = SceneManager.GetActiveScene().name;
            // TODO add a loading scene
            if (scene != "Battle" || scene != "Loading")
            {
                Debug.LogError($"Cannot add gameobject to scene: Current scene ({scene}) is not Battle or Loading.");
                return;
            }
            GameObject gameObject = Game.GameObjects.Find(g => g.name == Name);
            if (gameObject == null)
            {
                Debug.LogError($"Cannot find game object with name {Name}");
                return;
            }
            if (gameObject.GetComponent<PropObject>() == null)
            {
                gameObject.AddComponent<PropObject>();
            }
            gameObject.name += $"_{Guid.NewGuid().ToString().Replace("-", "")}";
            MeshName = gameObject.name;
            //UnityEngine.Object.Instantiate
        }
        public virtual void RemoveFromScene() => UnityEngine.Object.Destroy(GetObjectOnScene());
        public virtual GameObject GetObjectOnScene() => GameObject.Find(MeshName);

        public virtual int GetDistance(Prop prop) => CubeCoordinates.GetDistance(CubeCoOrds, prop.CubeCoOrds);
        public virtual double GetStraightLineDistance(Prop prop) => CubeCoordinates.GetStraightLineDistance(CubeCoOrds, prop.CubeCoOrds);
        public Vector3 GetOnScreenCoordinates() => GetObjectOnScene().transform.position;

        public virtual object Clone()
        {
            Prop copy = (Prop)MemberwiseClone();
            copy.CoOrds = (Coordinates)CoOrds.Clone();
            return copy;
        }
        public bool Equals(Prop other) => !string.IsNullOrEmpty(MeshName) && MeshName == other.MeshName;
        public override string ToString() => $"{Name} ({CoOrds})";
    }

    public class PropObject : MonoBehaviour
    {
        public AudioClip AudioOnPlaced { get; set; }
        public AudioClip AudioOnClicked { get; set; }
        public AudioClip AudioOnDestroy { get; set; }
        public AudioClip AudioOnFire { get; set; }
        public AudioClip AudioOnMove { get; set; }

        public string PrintOnScreenCoOrds() => $"({gameObject.transform.position.x},{gameObject.transform.position.y},{gameObject.transform.position.z})";

        public Coordinates GetCoordinates() => Map.Instance.GetProp(gameObject).CoOrds;

        public virtual void Start()
        {
            /*
            AudioSource placed = gameObject.AddComponent<AudioSource>();
            placed.name = "placed";
            placed.clip = AudioOnPlaced;

            if (AudioOnPlaced != null)
            {
                placed.Play();
            }*/
        }

        public virtual void OnMouseDown()
        {

        }

        public virtual void OnDestroy()
        {

        }
    }
}

namespace SteelOfStalin.Assets.Props.Units
{
    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum UnitStatus
    {
        NONE = 0,
        IN_QUEUE = 1 << 0,
        CAN_BE_DEPLOYED = 1 << 1,
        ACTIVE = 1 << 2,
        MOVED = 1 << 3,
        FIRED = 1 << 4,
        SUPPRESSED = 1 << 5,
        AMBUSHING = 1 << 6,
        CONSTRUCTING = 1 << 7,
        DISCONNECTED = 1 << 8,
        WRECKED = 1 << 9,
        DESTROYED = 1 << 10,
        IN_FIELD = ~IN_QUEUE & ~CAN_BE_DEPLOYED & ~WRECKED & ~DESTROYED,
        IMMOBILE = SUPPRESSED | AMBUSHING | CONSTRUCTING | DISCONNECTED,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableMovementCommands
    {
        NONE = 0,
        HOLD = 1 << 0,
        MOVE = 1 << 1,
        MERGE = 1 << 2,
        SUBMERGE = 1 << 3,
        SURFACE = 1 << 4,
        LAND = 1 << 5,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableFiringCommands
    {
        NONE = 0,
        FIRE = 1 << 0,
        SUPPRESS = 1 << 1,
        SABOTAGE = 1 << 2,
        AMBUSH = 1 << 3,
        BOMBARD = 1 << 4,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableLogisticsCommands
    {
        NONE = 0,
        ABOARD = 1 << 0,
        DISEMBARK = 1 << 1,
        LOAD = 1 << 2,
        UNLOAD = 1 << 3,
        RESUPPLY = 1 << 4,
        REPAIR = 1 << 5,
        RECONSTRUCT = 1 << 6,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableConstructionCommands
    {
        NONE = 0,
        FORTIFY = 1 << 0,
        CONSTRUCT = 1 << 1,
        DEMOLISH = 1 << 2,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableMiscCommands
    {
        NONE = 0,
        CAPTURE = 1 << 0,
        SCAVENGE = 1 << 1,
        ASSEMBLE = 1 << 2,
        DISASSEMBLE = 1 << 3,
        ALL = ~0
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CommandAssigned
    {
        NONE,
        HOLD,
        MOVE,
        MERGE,
        SUBMERGE,
        SURFACE,
        LAND,
        FIRE,
        SUPPRESS,
        SABOTAGE,
        AMBUSH,
        BOMBARD,
        ABOARD,
        DISEMBARK,
        LOAD,
        UNLOAD,
        RESUPPLY,
        REPAIR,
        RECONSTRUCT,
        FORTIFY,
        CONSTRUCT,
        DEMOLISH,
        CAPTURE,
        SCAVENGE,
        ASSEMBLE,
        DISASSEMBLE,
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AutoCommands
    {
        NONE = 0,
        MOVE = 1 << 0,
        FIRE = 1 << 1,
        RESUPPLY = 1 << 2,
    }

    [JsonConverter(typeof(AssetConverter<Unit>))]
    public abstract class Unit : Prop, ICloneable, IEquatable<Unit>
    {
        public UnitStatus Status { get; set; }
        [JsonIgnore] public Player Owner { get; set; }
        public string OwnerName { get; set; }
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
        public AvailableMovementCommands AvailableMovementCommands { get; set; } = AvailableMovementCommands.HOLD;
        public AvailableFiringCommands AvailableFiringCommands { get; set; } = AvailableFiringCommands.NONE;
        public AvailableLogisticsCommands AvailableLogisticsCommands { get; set; } = AvailableLogisticsCommands.NONE;
        public AvailableConstructionCommands AvailableConstructionCommands { get; set; } = AvailableConstructionCommands.NONE;
        public AvailableMiscCommands AvailableMiscCommands { get; set; } = AvailableMiscCommands.NONE;
        public AutoCommands AutoCommands { get; set; } = AutoCommands.NONE;
        public List<Tile> AutoNavigationPath { get; set; } = new List<Tile>();

        public double CurrentSuppressionLevel { get; set; } = 0;
        public int ConsecutiveSuppressedRound { get; set; } = 0;
        public double TrainingTimeRemaining { get; set; } = 0;

        public bool IsSuppressed => Status.HasFlag(UnitStatus.SUPPRESSED);
        public bool IsConstructing => Status.HasFlag(UnitStatus.CONSTRUCTING);
        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) && !IsNeutral();
        public bool IsSameSubclassOf<T>(Unit another) where T : Unit => GetType().IsSubclassOf(typeof(T)) && another.GetType().IsSubclassOf(typeof(T));
        public bool IsOfSameCategory(Unit another) => IsSameSubclassOf<Ground>(another) || IsSameSubclassOf<Naval>(another) || IsSameSubclassOf<Aerial>(another);

        // used for spotting phase only, reset to null at round start, need not to be saved (serialized)
        [JsonIgnore] public IOffensiveCustomizable WeaponFired { get; set; }

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
        public virtual bool CanMove() => GetAccessibleNeigbours((int)Maneuverability.Speed.ApplyMod()).Any();
        public virtual bool CanMerge()
        {
            // TODO FUT Impl. 
            return false;
        }
        public virtual bool CanFire() => GetWeapons().Any(w => HasHostileUnitsInFiringRange(w) && HasEnoughAmmo(w));
        public virtual bool CanSabotage() => GetWeapons().Any(w => HasHostileBuildingsInFiringRange(w) && HasEnoughAmmo(w));
        // can be fired upon = can be suppressed
        public virtual bool CanSuppress() => GetWeapons().Any(w => HasHostileUnitsInFiringRange(w) && HasEnoughAmmo(w, false));
        public virtual bool CanAmbush() => !Status.HasFlag(UnitStatus.AMBUSHING) && GetWeapons().Any(w => HasEnoughAmmo(w));
        public virtual bool CanCommunicateWith(Prop p) => p is Unit u ? CanCommunicateWith(u) : (p is Cities c && CanCommunicateWith(c));
        public virtual bool CanCommunicateWith(Unit communicatee) => this != communicatee && GetStraightLineDistance(communicatee) <= Scouting.Communication + communicatee.Scouting.Communication;
        public virtual bool CanCommunicateWith(Cities cities) => GetStraightLineDistance(cities) <= Scouting.Communication + cities.Communication;

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
        public virtual IEnumerable<Tile> GetReconRange() => Map.Instance.GetStraightLineNeighbours(CubeCoOrds, Scouting.Reconnaissance.ApplyMod());

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
        public abstract IEnumerable<Module> GetModules();
        public IEnumerable<T> GetModules<T>() where T : Module => GetModules()?.OfType<T>();
        public virtual IEnumerable<Module> GetRepairableModules() => GetModules().Where(m => m.Integrity < Game.CustomizableData.Modules[m.Name].Integrity);
        public abstract Modifier GetConcealmentPenaltyMove();

        public List<Unit> GetAvailableMergeTargets()
        {
            // TODO FUT Impl. 
            return null;
        }

        public IEnumerable<Unit> GetUnitsInRange(IEnumerable<Tile> range) => range.Where(t => t.HasUnit).SelectMany(t => Map.Instance.GetUnits(t));
        public IEnumerable<Building> GetBuildingsInRange(IEnumerable<Tile> range) => range.Where(t => t.HasBuilding).SelectMany(t => Map.Instance.GetBuildings(t));

        // TODO FUT. Impl. read game rules to decide whether targets of serveral commands can be ally units (e.g. help allies repair, re-capture allies cities)
        public IEnumerable<Unit> GetOwnUnitsInRange(IEnumerable<Tile> range) => GetUnitsInRange(range).Where(u => u.IsOwn(Owner));
        public IEnumerable<Building> GetOwnBuildingsInRange(IEnumerable<Tile> range) => GetBuildingsInRange(range).Where(b => b.IsOwn(Owner));

        public IEnumerable<Unit> GetFriendlyUnitsInRange(IEnumerable<Tile> range) => GetUnitsInRange(range).Where(u => u.IsFriendly(Owner));
        public IEnumerable<Building> GetFriendlyBuildingsInRange(IEnumerable<Tile> range) => GetBuildingsInRange(range).Where(b => b.IsFriendly(Owner));

        // include all enemies, be it spotted or not
        public IEnumerable<Unit> GetHostileUnitsInRange(IEnumerable<Tile> range) => GetUnitsInRange(range).Where(u => !u.IsFriendly(Owner));
        public IEnumerable<Building> GetHostileBuildingsInRange(IEnumerable<Tile> range) => GetBuildingsInRange(range).Where(b => !b.IsFriendly(Owner));

        // use these for selection of fire/sabotage/suppress targets in UI
        public IEnumerable<Unit> GetHostileUnitsInFiringRange(IOffensiveCustomizable weapon) => GetHostileUnitsInRange(GetFiringRange(weapon)).Where(u => HasSpotted(u));
        public IEnumerable<Building> GetHostileBuildingsInFiringRange(IOffensiveCustomizable weapon) => GetHostileBuildingsInRange(GetFiringRange(weapon)).Where(b => HasSpotted(b));
        public IEnumerable<Cities> GetHostileCitiesInRange(IOffensiveCustomizable weapon) => Map.Instance.GetCities(c => c.IsHostile(Owner) && GetFiringRange(weapon).Contains<Tile>(c));

        public IEnumerable<Unit> GetHostileUnitsInReconRange() => GetHostileUnitsInRange(GetReconRange());
        public IEnumerable<Building> GetHostileBuildingsInReconRange() => GetHostileBuildingsInRange(GetReconRange());

        public Tile GetLocatedTile() => Map.Instance.GetTile(CoOrds);
        public double GetSuppliesRequired(Tile t) => t.TerrainMod.Supplies.ApplyTo(Consumption.Supplies.ApplyMod());
        public double GetSuppliesRequired(List<Tile> path) => path.Last().CoOrds == CoOrds ? 0 : path.Select(t => GetSuppliesRequired(t)).Sum(); // if last tile of path is where the unit at, no supplies or fuel is consumed (i.e. cannot move due to move conflict)
        public double GetFuelRequired(Tile t) => t.TerrainMod.Fuel.ApplyTo(Consumption.Fuel.ApplyMod());
        public double GetFuelRequired(List<Tile> path) => path.Last().CoOrds == CoOrds ? 0 : path.Select(t => GetFuelRequired(t)).Sum();

        public string GetResourcesChangeRecord(string res, double change) => res switch
        {
            "Money" => $" m:{change:+#.##;-#.##}=>{Carrying.Money}/{Capacity.Money} ",
            "Steel'" => $" t:{change:+#.##;-#.##}=>{Carrying.Steel}/{Capacity.Steel} ",
            "Supplies" => $" s:{change:+#.##;-#.##}=>{Carrying.Supplies}/{Capacity.Supplies} ",
            "Cartridges" => $" c:{change:+#.##;-#.##}=>{Carrying.Cartridges}/{Capacity.Cartridges} ",
            "Shells" => $" h:{change:+#.##;-#.##}=>{Carrying.Shells}/{Capacity.Shells} ",
            "Fuel" => $" f:{change:+#.##;-#.##}=>{Carrying.Fuel}/{Capacity.Fuel} ",
            "RareMetal" => $" r:{change:+#.##;-#.##}=>{Carrying.RareMetal}/{Capacity.RareMetal} ",
            _ => throw new ArgumentException($"Unknown resources symbol {res}")
        };
        public string GetResourcesChangeRecord(Resources consume)
        {
            if (consume.IsZero)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            if (consume.Money > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Money", -consume.Money));
            }
            if (consume.Steel > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Steel", -consume.Steel));
            }
            if (consume.Supplies > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Supplies", -consume.Supplies));
            }
            if (consume.Cartridges > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Cartridges", -consume.Cartridges));
            }
            if (consume.Shells > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Shells", -consume.Shells));
            }
            if (consume.Fuel > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Fuel", -consume.Fuel));
            }
            if (consume.RareMetal > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("RareMetal", -consume.RareMetal));
            }
            return sb.ToString();
        }
        public string GetStrengthChangeRecord(double change) => $" hp:{change:+#.##;-#.##}=>{Defense.Strength}/{Game.UnitData[Name].Defense.Strength} ";
        public string GetSuppressionChangeRecord(double change) => $" sup:{change:+#.####;-#.####}=>{CurrentSuppressionLevel:#.####} ";

        public abstract override object Clone();
        public bool Equals(Unit other) => base.Equals(other);
    }
}

namespace SteelOfStalin.Assets.Props.Buildings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BuildingStatus
    {
        NONE,
        UNDER_CONSTRUCTION,
        ACTIVE,
        DESTROYED
    }

    // TODO FUT Impl. add toggle for accessibility to allies of buildings (e.g. allow ally planes land on own airfield etc.)
    [JsonConverter(typeof(AssetConverter<Building>))]
    public abstract class Building : Prop, ICloneable
    {
        [JsonIgnore] public Player Owner { get; set; }
        public string OwnerName { get; set; }
        public Coordinates BuilderLocation { get; set; }
        public BuildingStatus Status { get; set; } = BuildingStatus.NONE;
        public byte Level { get; set; }
        public byte MaxLevel { get; set; }
        public double Size { get; set; }
        public Cost Cost { get; set; } = new Cost();
        public Attribute Durability { get; set; } = new Attribute();
        public Scouting Scouting { get; set; } = new Scouting();
        public bool DestroyTerrainOnBuilt { get; set; } = true;
        public double ConstructionTimeRemaining { get; set; }

        public bool IsFortifying => Status == BuildingStatus.UNDER_CONSTRUCTION && Level > 0;

        public Building() : base() { }
        public Building(Building another) : base(another)
            => (Name, Owner, Status, Level, MaxLevel, Size, Cost, Durability, Scouting, DestroyTerrainOnBuilt, ConstructionTimeRemaining)
            = (another.Name, another.Owner, another.Status, another.Level, another.MaxLevel, another.Size, (Cost)another.Cost.Clone(), (Attribute)another.Durability.Clone(), (Scouting)another.Scouting.Clone(), another.DestroyTerrainOnBuilt, another.ConstructionTimeRemaining);

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        // public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p); /*&& !IsNeutral();*/

        public bool CanBeFortified() => Level < MaxLevel && Status == BuildingStatus.ACTIVE;
        public bool CanBeDemolished() => Level > 0 && Status == BuildingStatus.ACTIVE;

        public Tile GetLocatedTile() => Map.Instance.GetTile(CoOrds);
        public string GetDurabilityChangeRecord(double change) => $" d:{change:+#.##;-#.##}=>{Durability} ";

        public abstract override object Clone();
    }
}

namespace SteelOfStalin.Assets.Props.Tiles
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
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
        [JsonIgnore] public Player Owner { get; set; }
        public string OwnerName { get; set; }
        public double Population { get; set; }
        public Attribute ConstructionRange { get; set; } = new Attribute();
        public Attribute Communication { get; set; } = new Attribute();
        public Resources Production { get; set; } = new Resources();
        public Attribute Durability { get; set; } = new Attribute();
        public Attribute Morale { get; set; } = new Attribute();
        public bool IsDestroyed => Durability <= 0;

        public Cities() : base() { }
        public Cities(Cities another) : base(another)
            => (Owner, Population, ConstructionRange, Production, Durability, Morale)
            = (another.Owner, another.Population, (Attribute)another.ConstructionRange.Clone(), (Resources)another.Production.Clone(), (Attribute)another.Durability.Clone(), (Attribute)another.Morale.Clone());

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) && !IsNeutral();

        public bool CanCommunicateWith(Prop p) => p is Unit u ? CanCommunicateWith(u) : (p is Cities c && CanCommunicateWith(c));
        public bool CanCommunicateWith(Unit u) => GetStraightLineDistance(u) <= Communication + u.Scouting.Communication;
        public bool CanCommunicateWith(Cities c) => this != c && GetStraightLineDistance(c) <= Communication + c.Communication;

        public string GetMoraleChangeRecord(double change) => $" m:{change:+#.##,-#.##}=>{Morale}/{((Cities)Game.TileData[Name]).Morale} ";

        public abstract override object Clone();
    }

    [JsonConverter(typeof(AssetConverter<Tile>))]
    public abstract class Tile : Prop
    {
        public TileType Type { get; set; }
        public Accessibility Accessibility { get; set; }
        public TerrainModifier TerrainMod { get; set; } = new TerrainModifier();
        public double Obstruction { get; set; }
        public bool AllowConstruction { get; set; }
        public double Height { get; set; }
        public char Symbol { get; set; }

        public bool IsWater => Type == TileType.STREAM || Type == TileType.RIVER || Type == TileType.OCEAN || Type == TileType.SWAMP;
        public bool IsHill => Type is TileType.HILLOCK || Type is TileType.HILLS || Type is TileType.MOUNTAINS;
        public bool IsFlatLand => !IsWater && !IsHill && Type != TileType.BOUNDARY;
        public bool IsCity => Type is TileType.SUBURB || Type is TileType.CITY || Type is TileType.METROPOLIS;
        public bool HasUnit => Map.Instance.GetUnits(this).Any();
        public bool HasBuilding => Map.Instance.GetBuildings(this).Any();
        public bool IsOccupied => HasUnit || HasBuilding;

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
                            ? TerrainMod.Supplies.ApplyTo()
                            : TerrainMod.Fuel.ApplyTo(),
            SuppliesCostSoFar = (parent == null ? 0 : parent.SuppliesCostSoFar) + TerrainMod.Supplies.ApplyTo(consumption.Supplies.ApplyMod()),
            FuelCostSoFar = (parent == null ? 0 : parent.FuelCostSoFar) + TerrainMod.Fuel.ApplyTo(consumption.Fuel.ApplyMod()),
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