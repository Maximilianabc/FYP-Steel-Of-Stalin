using SteelOfStalin.Attributes;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using static SteelOfStalin.Util.Utilities;
using Attribute = SteelOfStalin.Attributes.Attribute;
using System;
using SteelOfStalin.Assets.Props.Buildings.Units;

namespace SteelOfStalin.Assets.Props.Units
{
    public abstract class Ground : Unit
    {
        public Ground() : base() { }
        public Ground(Ground another) : base(another) { }
        public abstract override object Clone();
        public abstract override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons);
        public abstract override IEnumerable<Module> GetModules();

        public override bool CanMove() => base.CanMove() && HasEnoughResourcesForMoving();
        public virtual bool HasEnoughResourcesForMoving()
        {
            IEnumerable<Tile> neighbours = GetAccessibleNeigbours();
            if (!neighbours.Any())
            {
                this.LogWarning("No direct accessible negibours (within 1 tile range)");
                return false;
            }
            if (Consumption.Fuel > 0)
            {
                IEnumerable<(decimal supplies, decimal fuel)> consumption_pairs = neighbours.Select(n => (n.TerrainMod.Supplies.ApplyTo(Consumption.Supplies), n.TerrainMod.Fuel.ApplyTo(Consumption.Fuel)));
                (decimal supplies, decimal fuel) cheapest_supplies = consumption_pairs.OrderBy(c => c.supplies).First();
                (decimal supplies, decimal fuel) cheapest_fuel = consumption_pairs.OrderBy(c => c.fuel).First();

                return (Carrying.Supplies >= cheapest_supplies.supplies && Carrying.Fuel >= cheapest_supplies.fuel)
                    || (Carrying.Supplies >= cheapest_fuel.supplies && Carrying.Fuel >= cheapest_fuel.fuel);
            }
            else
            {
                return Carrying.Supplies >= neighbours.Select(n => n.TerrainMod.Supplies.ApplyTo(Consumption.Supplies)).OrderBy(c => c).First();
            }
        }
    }

    public abstract class Naval : Unit
    {
        public Naval() : base() { }
        public Naval(Naval another) : base(another) { }
        public abstract override object Clone();
        public abstract override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons);
        public abstract override IEnumerable<Module> GetModules();
    }

    public abstract class Aerial : Unit
    {
        public Aerial() : base() { }
        public Aerial(Aerial another) : base(another) { }
        public abstract override object Clone();
        public abstract override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons);
        public abstract override IEnumerable<Module> GetModules();
    }
}

namespace SteelOfStalin.Assets.Props.Units.Land
{
    public abstract class Personnel : Ground
    {
        public Firearm PrimaryFirearm { get; set; }
        public Firearm SecondaryFirearm { get; set; }
        public string DefaultPrimary { get; set; }
        public List<string> AvailablePrimaryFirearms { get; set; } = new List<string>();
        public List<string> AvailableSecondaryFirearms { get; set; } = new List<string>();
        public Attribute CaptureEfficiency { get; set; } = new Attribute();

        public Personnel() : base() { }
        public Personnel(Personnel another) : base(another)
            => (PrimaryFirearm, SecondaryFirearm, DefaultPrimary, AvailablePrimaryFirearms, AvailableSecondaryFirearms, CaptureEfficiency)
            = ((Firearm)another.PrimaryFirearm?.Clone(),
               (Firearm)another.SecondaryFirearm?.Clone(),
               another.DefaultPrimary,
               new List<string>(another.AvailablePrimaryFirearms),
               new List<string>(another.AvailableSecondaryFirearms),
               (Attribute)another.CaptureEfficiency?.Clone());

        public override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons)
        {
            if (!weapons.Any() || weapons.Count() > 2 )
            {
                this.LogError($"Weapons length mismatch. Expected: [1,2]. Actual: {weapons.Count()}");
                return;
            }
            if (!weapons.All(w => w is Firearm))
            {
                this.LogError("Cannot assign non-firearm weapons to personnels");
                return;
            }

            ChangeFirearm((Firearm)weapons.ElementAt(0));
            if (weapons.Count() > 1)
            {
                ChangeFirearm((Firearm)weapons.ElementAt(1), false);
            }
        }

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.PERSONNEL);
        public virtual bool CanCapture() => IsInField && !IsSuppressed && GetLocatedTile().IsCity;
        public virtual bool CanAboard()
        {
            // TODO FUT Impl.
            return false;
        }
        public virtual bool CanRearm()
        {
            // TODO FUT Impl.
            return false;
        }
        public virtual bool CanFortify() => IsInField && GetFriendlyBuildingsInRange(Map.Instance.GetNeighbours(CubeCoOrds)).Any(b => b.Status == BuildingStatus.ACTIVE && b.Level > 0 && b.Level < b.MaxLevel && Carrying.HasEnoughResources(b.Cost.Fortification, false));
        public virtual bool CanConstruct() => IsInField && Game.BuildingData.All.Any(b => Carrying.HasEnoughResources(b.Cost.Base, false));
        public virtual bool CanDemolish() => IsInField && GetFriendlyBuildingsInRange(Map.Instance.GetNeighbours(CubeCoOrds)).Any(b => b.Status == BuildingStatus.ACTIVE && b.Level > 0);
        public virtual bool CanScavenge()
        {
            // TODO FUT Impl.
            return false;
        }
        public override bool CanBeTrainedIn(UnitBuilding ub) => ub is Barracks;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => new List<IOffensiveCustomizable>() { PrimaryFirearm, SecondaryFirearm };
        public override IEnumerable<Module> GetModules() => Enumerable.Empty<Module>();
        public override IEnumerable<Module> GetRepairableModules() => Enumerable.Empty<Module>();
        public override void SetModules(params Module[] modules) { }

        public override Modifier GetConcealmentPenaltyMove() => SecondaryFirearm == null ? PrimaryFirearm.ConcealmentPenaltyMove : Modifier.Min(PrimaryFirearm.ConcealmentPenaltyMove, SecondaryFirearm.ConcealmentPenaltyMove);

        // false for secondary
        public virtual void ChangeFirearm(Firearm firearm, bool primary = true)
        {
            if (firearm == null)
            {
                this.LogError("Firearm is null.");
                return;
            }
            if (!(AvailablePrimaryFirearms.Contains(firearm.Name) || AvailableSecondaryFirearms.Contains(firearm.Name)))
            {
                this.LogError($"Firearm {firearm.Name} is not available for unit {Name}.");
                return;
            }
            // changing primary but firearm is not a primary, or changing secondary but firearm is not a secondary
            if ((primary && !firearm.FirearmType.HasFlag(FirearmType.PRIMARY)) || (!primary && !firearm.FirearmType.HasFlag(FirearmType.SECONDARY)))
            {
                this.LogError("Firearm type mismatch.");
                return;
            }

            if (primary)
            {
                PrimaryFirearm = firearm;
            }
            else
            {
                SecondaryFirearm = firearm;
            }
        }
    }

    public abstract class Artillery : Ground
    {
        public bool IsAssembled { get; set; } = false;
        public decimal AssembleTime { get; set; }
        public string DefaultGun { get; set; }
        public List<string> AvailableGuns { get; set; } = new List<string>();
        public Gun Gun { get; set; }
        public Radio Radio { get; set; }

        public Artillery() : base() { }
        public Artillery(Artillery another) : base(another)
            => (IsAssembled, AssembleTime, DefaultGun, AvailableGuns, Gun, Radio)
            = (another.IsAssembled,
               another.AssembleTime,
               another.DefaultGun,
               new List<string>(another.AvailableGuns),
               (Gun)another.Gun?.Clone(),
               (Radio)another.Radio?.Clone());

        public override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons)
        {
            if (!weapons.Any())
            {
                this.LogError("weapons is empty");
                return;
            }
            if (!(weapons.First() is Gun))
            {
                this.LogError("Only guns can be assigned as weapon to artilleries");
                return;
            }
            ChangeGun((Gun)weapons.First());
        }

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.ARTILLERY);
        public override bool CanMove()
        {
            if (IsAssembled)
            {
                Maneuverability.Speed.MinusEquals(2);
                return Maneuverability.Speed >= 0 && base.CanMove();
            }
            return base.CanMove();
        }
        public override bool CanFire() => IsAssembled && base.CanFire();
        public virtual bool CanAboard()
        {
            // TODO FUT Impl.
            return false;
        }
        public virtual bool CanAssemble() => IsInField && !IsSuppressed && !IsAssembled;
        public virtual bool CanDisassemble() => IsInField && !IsSuppressed && IsAssembled;
        public override bool CanBeTrainedIn(UnitBuilding ub) => ub is Arsenal;

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            // TODO FUT. Impl. have a set of default modules for non-historical units, set them all here, same for other sub-cats except Personnels
            Radio = Game.CustomizableData.GetNew<Radio>();
        }

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => new List<IOffensiveCustomizable>() { Gun };
        public override IEnumerable<Module> GetModules() => new List<Module>() { Gun, Radio };
        public override void SetModules(params Module[] modules)
        {
            if (modules.Length > 2)
            {
                this.LogError($"Modules length mismatch. Expected: at most 2. Actual: {modules.Length}");
                return;
            }
            if (!modules.All(m => m is Gun || m is Radio))
            {
                this.LogError("Invalid module type found. Artilleries only have a gun and a radio.");
                return;
            }
            foreach (Module module in modules)
            {
                if (module is Gun g)
                {
                    Gun = g;
                    continue;
                }
                if (module is Radio r)
                {
                    Radio = r;
                }
            }
        }

        public override Modifier GetConcealmentPenaltyMove() => Gun.ConcealmentPenaltyMove;

        public virtual void ChangeGun(Gun gun)
        {
            if (gun == null)
            {
                this.LogError("gun is null.");
                return;
            }
            if (!AvailableGuns.Contains(gun.Name))
            {
                this.LogError($"Gun {gun.Name} is not available for unit {Name}.");
                return;
            }
            Gun = gun;
        }
    }

    public abstract class Vehicle : Ground
    {
        public string DefaultMainArmament { get; set; }
        public List<string> AvailableMainArmaments { get; set; } = new List<string>();
        public List<Gun> Guns { get; set; } = new List<Gun>();
        public List<HeavyMachineGun> HeavyMachineGuns { get; set; } = new List<HeavyMachineGun>();
        public Engine Engine { get; set; }
        public Suspension Suspension { get; set; }
        public Radio Radio { get; set; }
        public Periscope Periscope { get; set; }
        public FuelTank FuelTank { get; set; }
        public AmmoRack AmmoRack { get; set; }

        public Vehicle() : base() { }
        public Vehicle(Vehicle another) : base(another)
            => (DefaultMainArmament, AvailableMainArmaments, Guns, HeavyMachineGuns, Engine, Suspension, Radio, Periscope, FuelTank, AmmoRack)
            = (another.DefaultMainArmament,
                new List<string>(another.AvailableMainArmaments),
                new List<Gun>(another.Guns),
                new List<HeavyMachineGun>(another.HeavyMachineGuns),
                (Engine)another.Engine?.Clone(),
                (Suspension)another.Suspension?.Clone(),
                (Radio)another.Radio?.Clone(),
                (Periscope)another.Periscope?.Clone(),
                (FuelTank)another.FuelTank?.Clone(),
                (AmmoRack)another.AmmoRack?.Clone());

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Radio = Game.CustomizableData.GetNew<Radio>();
        }

        public override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons)
        {
            if (!weapons.Any())
            {
                this.LogError("weapons is empty");
                return;
            }
            if (!weapons.All(w => w is Gun || w is HeavyMachineGun))
            {
                this.LogError("Only guns and heavy machine guns can be assigned as weapons to vehicles");
                return;
            }

            // TODO FUT. Impl. limit the number of guns and machine guns for each vehicles
            if (!weapons.OfType<Gun>().All(g => AvailableMainArmaments.Contains(g.Name)))
            {
                this.LogError($"At least of the guns is not available for unit {Name}");
                return;
            }
            Guns = new List<Gun>(weapons.OfType<Gun>());
            HeavyMachineGuns = new List<HeavyMachineGun>(weapons.OfType<HeavyMachineGun>());
        }

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.VEHICLE);

        // TODO FUT Impl. consider malfunction chance when conrresponding modules' integrities drop below their functioning thresholds
        public override bool CanMove() => Engine.Integrity > 0 && Suspension.Integrity > 0 && FuelTank.Integrity > 0 && base.CanMove();
        public override bool CanFire() => (Guns.Any(g => g.Integrity > 0 && g.CannonBreech.Integrity > 0) || HeavyMachineGuns.Any(mg => mg.Integrity > 0)) && base.CanFire();
        public override bool CanCommunicateWith(Prop p) => Radio.Integrity > 0 && base.CanCommunicateWith(p);
        public override bool CanCommunicateWith(Unit communicatee) => Radio.Integrity > 0 && base.CanCommunicateWith(communicatee);
        public override bool CanCommunicateWith(Cities cities) => Radio.Integrity > 0 && base.CanCommunicateWith(cities);
        public override bool CanBeTrainedIn(UnitBuilding ub) => ub is Arsenal;

        public override IEnumerable<Tile> GetReconRange() =>
            Periscope.Integrity > 0
                ? Map.Instance.GetStraightLineNeighbours(CubeCoOrds, Periscope.ReconBonus.ApplyTo(Scouting.Reconnaissance.ApplyMod()))
                : base.GetReconRange();

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(HeavyMachineGuns);
        public override IEnumerable<Module> GetModules()
        {
            List<Module> modules = new List<Module>()
            {
                Engine,
                Suspension,
                Radio,
                Periscope,
                FuelTank,
                AmmoRack
            };
            modules.AddRange(Guns);
            modules.AddRange(HeavyMachineGuns);
            return modules;
        }
        public override void SetModules(params Module[] modules)
        {
            foreach (Module module in modules)
            {
                if (module is Gun g)
                {
                    Guns.Add(g);
                    continue;
                }
                if (module is HeavyMachineGun hmg)
                {
                    HeavyMachineGuns.Add(hmg);
                    continue;
                }
                if (module is Engine e)
                {
                    Engine = e;
                    continue;
                }
                if (module is Suspension s)
                {
                    Suspension = s;
                    continue;
                }
                if (module is Radio r)
                {
                    Radio = r;
                    continue;
                }
                if (module is Periscope p)
                {
                    Periscope = p;
                    continue;
                }
                if (module is FuelTank f)
                {
                    FuelTank = f;
                    continue;
                }
                if (module is AmmoRack a)
                {
                    AmmoRack = a;
                    continue;
                }
                this.LogWarning($"Invalid module type {module.GetType().Name} found");
            }
        }

        public override Modifier GetConcealmentPenaltyMove() => Engine.ConcealmentPenaltyMove;

        public void ChangeArmaments(params IOffensiveCustomizable[] weapon)
        {
            // TODO FUT. Impl. find a way to identify the specific gun/HMG to be replaced
        }
        public void ChangeModule(Module module)
        {
            // TODO FUT. Impl.
        }
    }
}

namespace SteelOfStalin.Assets.Props.Units.Land.Personnels
{
    // different types of personnel units here, all should inherit Personnel
    public class Militia : Personnel
    {
        public Militia() : base() { }
        public Militia(Militia another) : base(another) { }
        public override object Clone() => new Militia(this);
    }
    public class Infantry : Personnel
    {
        public Infantry() : base() { }
        public Infantry(Infantry another) : base(another) { }
        public override object Clone() => new Infantry(this);
    }
    public class Assault : Personnel
    {
        public Assault() : base() { }
        public Assault(Assault another) : base(another) { }
        public override object Clone() => new Assault(this);
    }
    public class Support : Personnel
    {
        public Support() : base() { }
        public Support(Support another) : base(another) { }
        public override object Clone() => new Support(this);
    }
    public class Mountain : Personnel
    {
        public Mountain() : base() { }
        public Mountain(Mountain another) : base(another) { }
        public override object Clone() => new Mountain(this);

        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && (t.Accessibility.HasFlag(Accessibility.PERSONNEL) || t.Type == TileType.MOUNTAINS);
    }
    public class Engineer : Personnel
    {
        public Attribute RepairingEfficiency { get; set; }

        public Engineer() : base() { }
        public Engineer(Engineer another) : base(another) => RepairingEfficiency = (Attribute)another.RepairingEfficiency?.Clone();
        public override object Clone() => new Engineer(this);

        public bool CanRepair() => GetOwnUnitsInRange(Map.Instance.GetNeighbours(CubeCoOrds)).Any(u => u.GetModules() != null);

        public IEnumerable<Unit> GetRepairableTargets() => GetOwnUnitsInRange(Map.Instance.GetNeighbours(CubeCoOrds)).Where(u => u.GetRepairableModules().Any());
    }
}

namespace SteelOfStalin.Assets.Props.Units.Land.Artilleries
{
    // different types of artilleries units here, all should inherit Artillery
    public class Portable : Artillery
    {
        public Portable() : base() { }
        public Portable(Portable another) : base(another) { }
        public override object Clone() => new Portable(this);
    }
    public class DirectFire : Artillery
    {
        public DirectFire() : base() { }
        public DirectFire(DirectFire another) : base(another) { }
        public override object Clone() => new DirectFire(this);
    }
    public class AntiTank : Artillery
    {
        public AntiTank() : base() { }
        public AntiTank(AntiTank another) : base(another) { }
        public override object Clone() => new AntiTank(this);
    }
    public class AntiAircraft : Artillery
    {
        public AntiAircraft() : base() { }
        public AntiAircraft(AntiAircraft another) : base(another) { }
        public override object Clone() => new AntiAircraft(this);
    }
    public class HeavySupport : Artillery
    {
        public HeavySupport() : base() { }
        public HeavySupport(HeavySupport another) : base(another) { }
        public override object Clone() => new HeavySupport(this);
    }
    public class SelfPropelled : Artillery
    {
        public SelfPropelled() : base() { }
        public SelfPropelled(SelfPropelled another) : base(another) { }

        public override bool CanAssemble() => false;
        public override bool CanDisassemble() => false;
        public override object Clone() => new SelfPropelled(this);
    }
    public class Railroad : Artillery
    {
        public Railroad() : base() { }
        public Railroad(Railroad another) : base(another) { }

        public override bool CanAssemble() => false;
        public override bool CanDisassemble() => false; 
        public override object Clone() => new Railroad(this);
    }
    public class CoastalGun : Artillery
    {
        public CoastalGun() : base() { }
        public CoastalGun(CoastalGun another) : base(another) { }
        public override object Clone() => new CoastalGun(this);
    }
}

namespace SteelOfStalin.Assets.Props.Units.Land.Vehicles
{
    // different types of vehicles units here, all should inherit Vehicle
    public class MotorisedInfantry : Vehicle
    {
        public MotorisedInfantry() : base() { }
        public MotorisedInfantry(MotorisedInfantry another) : base(another) { }

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("small_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("small_suspension");
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("small_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("small_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("small_ammo_rack");
        }

        public override object Clone() => new MotorisedInfantry(this);
    }
    public class Utility : Vehicle
    {
        public LoadLimit LoadLimit { get; set; }

        public Utility() : base() { }
        public Utility(Utility another) : base(another) => LoadLimit = (LoadLimit)another.LoadLimit.Clone();

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("small_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("small_suspension"); // TODO add Wheels module
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("small_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("small_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("small_ammo_rack");
        }

        public override object Clone() => new Utility(this);
    }
    public class Carrier : Vehicle
    {
        public LoadLimit LoadLimit { get; set; }

        public Carrier() : base() { }
        public Carrier(Carrier another) : base(another) => LoadLimit = (LoadLimit)another.LoadLimit.Clone();
        public override object Clone() => new Carrier(this);
    }
    public class ArmouredCar : Vehicle
    {
        public ArmouredCar() : base() { }
        public ArmouredCar(ArmouredCar another) : base(another) { }

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("small_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("small_suspension"); // TODO add Wheels module
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("small_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("medium_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("medium_ammo_rack");
        }

        public override object Clone() => new ArmouredCar(this);
    }
    public class TankDestroyer : Vehicle
    {
        public TankDestroyer() : base() { }
        public TankDestroyer(TankDestroyer another) : base(another) { }

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("medium_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("medium_suspension");
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("medium_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("medium_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("medium_ammo_rack");
        }

        public override object Clone() => new TankDestroyer(this);
    }
    public class AssaultGun : Vehicle
    {
        public AssaultGun() : base() { }
        public AssaultGun(AssaultGun another) : base(another) { }

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("large_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("medium_suspension");
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("medium_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("large_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("medium_ammo_rack");
        }

        public override object Clone() => new AssaultGun(this);
    }
    public class LightTank : Vehicle
    {
        public LightTank() : base() { }
        public LightTank(LightTank another) : base(another) { }

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("small_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("small_suspension");
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("medium_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("medium_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("medium_ammo_rack");
        }

        public override object Clone() => new LightTank(this);
    }
    public class MediumTank : Vehicle
    {
        public MediumTank() : base() { }
        public MediumTank(MediumTank another) : base(another) { }

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("medium_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("medium_suspension");
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("medium_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("large_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("large_ammo_rack");
        }

        public override object Clone() => new MediumTank(this);
    }
    public class HeavyTank : Vehicle
    {
        public HeavyTank() : base() { }
        public HeavyTank(HeavyTank another) : base(another) { }

        public override void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            base.Initialize(owner, coordinates, status);
            Engine = (Engine)Game.CustomizableData.GetNew<Engine>("large_engine");
            Suspension = (Suspension)Game.CustomizableData.GetNew<Suspension>("large_suspension");
            Periscope = (Periscope)Game.CustomizableData.GetNew<Periscope>("large_periscope");
            FuelTank = (FuelTank)Game.CustomizableData.GetNew<FuelTank>("large_fuel_tank");
            AmmoRack = (AmmoRack)Game.CustomizableData.GetNew<AmmoRack>("large_ammo_rack");
        }

        public override object Clone() => new HeavyTank(this);
    }
    public class ArmouredTrain : Vehicle
    {
        public LoadLimit LoadLimit { get; set; }

        public ArmouredTrain() : base() { }
        public ArmouredTrain(ArmouredTrain another) : base(another) => LoadLimit = (LoadLimit)another.LoadLimit.Clone();
        public override object Clone() => new ArmouredTrain(this);
    }
}

namespace SteelOfStalin.Assets.Props.Units.Sea
{
    public abstract class Vessel : Naval
    {
        public List<string> DefaultMainArmaments { get; set; } = new List<string>();
        public List<string> DefaultSecondaryArmaments { get; set; } = new List<string>();
        public List<string> AvailableMainArmaments { get; set; } = new List<string>();
        public List<string> AvailableSecondaryArmaments { get; set; } = new List<string>();
        public List<Gun> Guns { get; set; } = new List<Gun>();
        public List<HeavyMachineGun> HeavyMachineGuns { get; set; } = new List<HeavyMachineGun>();
        public Engine Engine { get; set; }
        public Radio Radio { get; set; }
        public Periscope Periscope { get; set; }
        public FuelTank FuelTank { get; set; }
        public AmmoRack AmmoRack { get; set; }
        public Propeller Propeller { get; set; }
        public Rudder Rudder { get; set; }
        public Radar Radar { get; set; }
        public decimal Altitude { get; set; }

        public Vessel() : base() { }
        public Vessel(Vessel another) : base(another)
            => (DefaultMainArmaments, DefaultSecondaryArmaments, AvailableMainArmaments, AvailableSecondaryArmaments, Guns, HeavyMachineGuns, Engine, Radio, Periscope, FuelTank, AmmoRack, Propeller, Rudder, Radar, Altitude)
            = (new List<string>(another.DefaultMainArmaments),
                new List<string>(another.DefaultSecondaryArmaments),
                new List<string>(another.AvailableMainArmaments),
                new List<string>(another.AvailableSecondaryArmaments),
                new List<Gun>(another.Guns),
                new List<HeavyMachineGun>(another.HeavyMachineGuns),
                (Engine)another.Engine?.Clone(),
                (Radio)another.Radio?.Clone(),
                (Periscope)another.Periscope?.Clone(),
                (FuelTank)another.FuelTank?.Clone(),
                (AmmoRack)another.AmmoRack?.Clone(),
                (Propeller)another.Propeller?.Clone(),
                (Rudder)another.Rudder?.Clone(),
                (Radar)another.Radar?.Clone(),
                another.Altitude);

        public override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons)
        {
            // TODO FUT. Impl.
        }

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.VESSEL);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0 && Carrying.Fuel > 0;
        public override bool CanBeTrainedIn(UnitBuilding ub) => ub is Dockyard;

        public override Modifier GetConcealmentPenaltyMove() => Engine.ConcealmentPenaltyMove;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(HeavyMachineGuns);
        public override IEnumerable<Module> GetModules()
        {
            List<Module> modules = new List<Module>()
            {
                Engine,
                Radio,
                Periscope,
                FuelTank,
                AmmoRack,
                Propeller,
                Rudder,
                Radar
            };
            modules.AddRange(Guns);
            modules.AddRange(HeavyMachineGuns);
            return modules;
        }
        public override void SetModules(params Module[] modules)
        {
            // TODO FUT. Impl.
        }
    }

    // different types of naval units here, all should inherit Vessel
    public class Gunboat : Vessel
    {
        public Gunboat() : base() { }
        public Gunboat(Gunboat another) : base(another) { }
        public override object Clone() => new Gunboat(this);
    }
    public class Frigate : Vessel
    {
        public Frigate() : base() { }
        public Frigate(Frigate another) : base(another) { }
        public override object Clone() => new Frigate(this);
    }
    public class Destroyer : Vessel
    {
        public Destroyer() : base() { }
        public Destroyer(Destroyer another) : base(another) { }
        public override object Clone() => new Destroyer(this);
    }
    public class LightCruiser : Vessel
    {
        public LightCruiser() : base() { }
        public LightCruiser(LightCruiser another) : base(another) { }
        public override object Clone() => new LightCruiser(this);
    }
    public class Battlecruiser : Vessel
    {
        public Battlecruiser() : base() { }
        public Battlecruiser(Battlecruiser another) : base(another) { }
        public override object Clone() => new Battlecruiser(this);
    }
    public class Battleship : Vessel
    {
        public Battleship() : base() { }
        public Battleship(Battleship another) : base(another) { }
        public override object Clone() => new Battleship(this);
    }
    public class AircraftCarrier : Vessel
    {
        public AircraftCarrier() : base() { }
        public AircraftCarrier(AircraftCarrier another) : base(another) { }
        public override object Clone() => new AircraftCarrier(this);
    }
    public class Submarine : Vessel
    {
        public Submarine() : base() { }
        public Submarine(Submarine another) : base(another) { }
        public override object Clone() => new Submarine(this);

        public bool CanSurface()
        {
            // TODO FUT Impl.
            return false;
        }
        public bool CanSubmerge()
        {
            // TODO FUT Impl.
            return false;
        }
    }
    public class EscortCarrier : Vessel
    {
        public EscortCarrier() : base() { }
        public EscortCarrier(EscortCarrier another) : base(another) { }
        public override object Clone() => new EscortCarrier(this);
    }
    public class ReplenishmentOiler : Vessel
    {
        public ReplenishmentOiler() : base() { }
        public ReplenishmentOiler(ReplenishmentOiler another) : base(another) { }
        public override object Clone() => new ReplenishmentOiler(this);
    }
}

namespace SteelOfStalin.Assets.Props.Units.Air
{
    public abstract class Plane : Aerial
    {
        public List<string> DefaultMainArmaments { get; set; } = new List<string>();
        public List<string> AvailableMainArmaments { get; set; } = new List<string>();
        public List<Gun> Guns { get; set; } = new List<Gun>();
        public List<HeavyMachineGun> HeavyMachineGuns { get; set; } = new List<HeavyMachineGun>();
        public Engine Engine { get; set; }
        public Radio Radio { get; set; }
        public FuelTank FuelTank { get; set; }
        public AmmoRack AmmoRack { get; set; }
        public Propeller Propeller { get; set; }
        public Rudder Rudder { get; set; }
        public Wings Wings { get; set; }
        public LandingGear LandingGear { get; set; }
        public Radar Radar { get; set; }
        public decimal Altitude { get; set; }

        public Plane() : base() { }
        public Plane(Plane another) : base(another) 
            => (Guns, HeavyMachineGuns, Engine, Radio, FuelTank, AmmoRack, Propeller, Rudder, Wings, LandingGear, Radar) 
            = (new List<Gun>(another.Guns),
               new List<HeavyMachineGun>(another.HeavyMachineGuns),
               (Engine)another.Engine?.Clone(),
               (Radio)another.Radio?.Clone(),
               (FuelTank)another.FuelTank?.Clone(),
               (AmmoRack)another.AmmoRack?.Clone(),
               (Propeller)another.Propeller?.Clone(),
               (Rudder)another.Rudder?.Clone(),
               (Wings)another.Wings?.Clone(),
               (LandingGear)another.LandingGear?.Clone(),
               (Radar)another.Radar?.Clone());

        public override void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons)
        {
            // TODO FUT. Impl.
        }

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.PLANE);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0 && Carrying.Fuel > 0;
        public bool CanLand()
        {
            // TODO FUT Impl.
            return false;
        }
        public override bool CanBeTrainedIn(UnitBuilding ub) => ub is Airfield;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(HeavyMachineGuns);
        public override IEnumerable<Module> GetModules()
        {
            List<Module> modules = new List<Module>()
            {
                Engine,
                Radio,
                FuelTank,
                AmmoRack,
                Propeller,
                Rudder,
                Wings,
                LandingGear,
                Radar
            };
            modules.AddRange(Guns);
            modules.AddRange(HeavyMachineGuns);
            return modules;
        }
        public override void SetModules(params Module[] modules)
        {
            // TODO FUT. Impl.
        }

        public override Modifier GetConcealmentPenaltyMove() => null; // TODO FUT. Impl.
    }

    // different types of aerial units here, all should inherit Plane
    public class Attacker : Plane
    {
        public Attacker() : base() { }
        public Attacker(Attacker another) : base(another) { }
        public override object Clone() => new Attacker(this);
    }
    public class Fighter : Plane
    {
        public Fighter() : base() { }
        public Fighter(Fighter another) : base(another) { }
        public override object Clone() => new Fighter(this);
    }
    public class Bomber : Plane
    {
        public Bomber() : base() { }
        public Bomber(Bomber another) : base(another) { }
        public override object Clone() => new Bomber(this);

        public bool CanBombard()
        {
            // TODO FUT Impl.
            return false;
        }
    }
    public class TransportAircraft : Plane
    {
        public TransportAircraft() : base() { }
        public TransportAircraft(TransportAircraft another) : base(another) { }
        public override object Clone() => new TransportAircraft(this);
    }
    public class SurveillanceAircraft : Plane
    {
        public SurveillanceAircraft() : base() { }
        public SurveillanceAircraft(SurveillanceAircraft another) : base(another) { }
        public override object Clone() => new SurveillanceAircraft(this);
    }
}
