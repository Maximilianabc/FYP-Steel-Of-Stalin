using SteelOfStalin.Attributes;
using SteelOfStalin.Customizables;
using SteelOfStalin.Customizables.Modules;
using SteelOfStalin.Props.Tiles;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SteelOfStalin.Util.Utilities;

namespace SteelOfStalin.Props.Units
{
    public abstract class Ground : Unit
    {
        public Ground() : base() { }
        public Ground(Ground another) : base(another) { }
        public abstract override object Clone();
    }

    public abstract class Naval : Unit
    {
        public Naval() : base() { }
        public Naval(Naval another) : base(another) { }
        public abstract override object Clone();
    }

    public abstract class Aerial : Unit
    {
        public Aerial() : base() { }
        public Aerial(Aerial another) : base(another) { }
        public abstract override object Clone();
    }
}

namespace SteelOfStalin.Props.Units.Land
{

    public abstract class Personnel : Ground
    {
        public Firearm PrimaryFirearm { get; set; }
        public Firearm SecondaryFirearm { get; set; }
        public string DefaultPrimary { get; set; }
        public List<string> AvailableFirearms { get; set; } = new List<string>();
        public Attribute CaptureEfficiency { get; set; } = new Attribute();

        public Personnel() : base() { }
        public Personnel(Personnel another)
            => (PrimaryFirearm, SecondaryFirearm, DefaultPrimary, AvailableFirearms, CaptureEfficiency)
            = ((Firearm)another.PrimaryFirearm.Clone(),
                (Firearm)another.SecondaryFirearm.Clone(),
                (string)another.DefaultPrimary.Clone(),
                new List<string>(another.AvailableFirearms),
                (Attribute)another.CaptureEfficiency.Clone());

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.PERSONNEL);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0;
        public virtual bool CanCapture() => !IsSuppressed && GetLocatedTile().IsCity;
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
        public virtual bool CanFortify()
        {
            // TODO
            return false;
        }
        public virtual bool CanConstruct()
        {
            // TODO
            return false;
        }
        public virtual bool CanScavenge()
        {
            // TODO FUT Impl.
            return false;
        }

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => new List<IOffensiveCustomizable>() { PrimaryFirearm, SecondaryFirearm };

        // false for secondary
        public virtual void ChangeFirearm(Firearm firearm, bool primary = true)
        {
            if (firearm == null)
            {
                this.LogError("Firearm is null.");
                return;
            }
            if (!AvailableFirearms.Contains(firearm.Name))
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
        public double AssembleTime { get; set; }
        public string DefaultGun { get; set; }
        public List<string> AvailableGuns { get; set; } = new List<string>();
        public Gun Gun { get; set; }
        public Radio Radio { get; set; }
        public CannonBreech CannonBreech { get; set; }

        public Artillery() : base() { }
        public Artillery(Artillery another) : base(another)
            => (IsAssembled, AssembleTime, DefaultGun, AvailableGuns, Gun, Radio, CannonBreech)
            = (another.IsAssembled,
                another.AssembleTime,
                (string)another.DefaultGun.Clone(),
                new List<string>(another.AvailableGuns),
                (Gun)another.Gun.Clone(),
                (Radio)another.Radio.Clone(),
                (CannonBreech)another.CannonBreech.Clone());

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
        public override bool CanFire() => base.CanFire() && IsAssembled;
        public virtual bool CanAboard()
        {
            // TODO FUT Impl.
            return false;
        }
        public virtual bool CanAssemble() => !IsSuppressed && !IsAssembled;
        public virtual bool CanDisassemble() => !IsSuppressed && IsAssembled;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => new List<IOffensiveCustomizable>() { Gun };
    }

    public abstract class Vehicle : Ground
    {
        public string DefaultMainArmament { get; set; }
        public List<string> AvailableMainArmaments { get; set; } = new List<string>();
        public List<Gun> Guns { get; set; } = new List<Gun>();
        public List<MountedMachineGun> MountedMachineGuns { get; set; } = new List<MountedMachineGun>();
        public Engine Engine { get; set; }
        public Suspension Suspension { get; set; }
        public Radio Radio { get; set; }
        public Periscope Periscope { get; set; }
        public FuelTank FuelTank { get; set; }
        public CannonBreech CannonBreech { get; set; }
        public AmmoRack AmmoRack { get; set; }

        public Vehicle() : base() { }
        public Vehicle(Vehicle another) : base(another)
            => (DefaultMainArmament, AvailableMainArmaments, Guns, MountedMachineGuns, Engine, Suspension, Radio, Periscope, FuelTank, CannonBreech, AmmoRack)
            = (another.DefaultMainArmament,
                new List<string>(another.AvailableMainArmaments),
                new List<Gun>(another.Guns),
                new List<MountedMachineGun>(another.MountedMachineGuns),
                (Engine)another.Engine.Clone(),
                (Suspension)another.Suspension.Clone(),
                (Radio)another.Radio.Clone(),
                (Periscope)another.Periscope.Clone(),
                (FuelTank)another.FuelTank.Clone(),
                (CannonBreech)another.CannonBreech.Clone(),
                (AmmoRack)another.AmmoRack.Clone());

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.VEHICLE);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0 && Carrying.Fuel > 0;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(MountedMachineGuns);
    }
}

namespace SteelOfStalin.Props.Units.Land.Personnels
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
        public Engineer() : base() { }
        public Engineer(Engineer another) : base(another) { }
        public override object Clone() => new Engineer(this);

        public bool CanRepair()
        {
            // TODO
            return false;
        }
    }
}

namespace SteelOfStalin.Props.Units.Land.Artilleries
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
        public override object Clone() => new SelfPropelled(this);
    }
    public class Railroad : Artillery
    {
        public Railroad() : base() { }
        public Railroad(Railroad another) : base(another) { }
        public override object Clone() => new Railroad(this);
    }
    public class CoastalGun : Artillery
    {
        public CoastalGun() : base() { }
        public CoastalGun(CoastalGun another) : base(another) { }
        public override object Clone() => new CoastalGun(this);
    }
}

namespace SteelOfStalin.Props.Units.Land.Vehicles
{
    // different types of vehicles units here, all should inherit Vehicle
    public class MotorisedInfantry : Vehicle
    {
        public MotorisedInfantry() : base() { }
        public MotorisedInfantry(MotorisedInfantry another) : base(another) { }
        public override object Clone() => new MotorisedInfantry(this);
    }
    public class Utility : Vehicle
    {
        public Utility() : base() { }
        public Utility(Utility another) : base(another) { }
        public override object Clone() => new Utility(this);
    }
    public class Carrier : Vehicle
    {
        public Carrier() : base() { }
        public Carrier(Carrier another) : base(another) { }
        public override object Clone() => new Carrier(this);
    }
    public class ArmouredCar : Vehicle
    {
        public ArmouredCar() : base() { }
        public ArmouredCar(ArmouredCar another) : base(another) { }
        public override object Clone() => new ArmouredCar(this);
    }
    public class TankDestroyer : Vehicle
    {
        public TankDestroyer() : base() { }
        public TankDestroyer(TankDestroyer another) : base(another) { }
        public override object Clone() => new TankDestroyer(this);
    }
    public class AssaultGun : Vehicle
    {
        public AssaultGun() : base() { }
        public AssaultGun(AssaultGun another) : base(another) { }
        public override object Clone() => new AssaultGun(this);
    }
    public class LightTank : Vehicle
    {
        public LightTank() : base() { }
        public LightTank(LightTank another) : base(another) { }
        public override object Clone() => new LightTank(this);
    }
    public class MediumTank : Vehicle
    {
        public MediumTank() : base() { }
        public MediumTank(MediumTank another) : base(another) { }
        public override object Clone() => new MediumTank(this);
    }
    public class HeavyTank : Vehicle
    {
        public HeavyTank() : base() { }
        public HeavyTank(HeavyTank another) : base(another) { }
        public override object Clone() => new HeavyTank(this);
    }
    public class ArmouredTrain : Vehicle
    {
        public ArmouredTrain() : base() { }
        public ArmouredTrain(ArmouredTrain another) : base(another) { }
        public override object Clone() => new ArmouredTrain(this);
    }

}

namespace SteelOfStalin.Props.Units.Sea
{
    public abstract class Vessel : Naval
    {
        public List<string> DefaultMainArmaments { get; set; } = new List<string>();
        public List<string> DefaultSecondaryArmaments { get; set; } = new List<string>();
        public List<string> AvailableMainArmaments { get; set; } = new List<string>();
        public List<string> AvailableSecondaryArmaments { get; set; } = new List<string>();
        public List<Gun> Guns { get; set; } = new List<Gun>();
        public List<MountedMachineGun> MountedMachineGuns { get; set; } = new List<MountedMachineGun>();
        public Engine Engine { get; set; }
        public Radio Radio { get; set; }
        public Periscope Periscope { get; set; }
        public FuelTank FuelTank { get; set; }
        public CannonBreech CannonBreech { get; set; }
        public AmmoRack AmmoRack { get; set; }
        public Propeller Propeller { get; set; }
        public Rudder Rudder { get; set; }
        public Radar Radar { get; set; }
        public double Altitude { get; set; }

        public Vessel() : base() { }
        public Vessel(Vessel another) : base(another)
            => (DefaultMainArmaments, DefaultSecondaryArmaments, AvailableMainArmaments, AvailableSecondaryArmaments, Guns, MountedMachineGuns, Engine, Radio, Periscope, FuelTank, CannonBreech, AmmoRack, Propeller, Rudder, Radar, Altitude)
            = (new List<string>(another.DefaultMainArmaments),
                new List<string>(another.DefaultSecondaryArmaments),
                new List<string>(another.AvailableMainArmaments),
                new List<string>(another.AvailableSecondaryArmaments),
                new List<Gun>(another.Guns),
                new List<MountedMachineGun>(another.MountedMachineGuns),
                (Engine)another.Engine.Clone(),
                (Radio)another.Radio.Clone(),
                (Periscope)another.Periscope.Clone(),
                (FuelTank)another.FuelTank.Clone(),
                (CannonBreech)another.CannonBreech.Clone(),
                (AmmoRack)another.AmmoRack.Clone(),
                (Propeller)another.Propeller.Clone(),
                (Rudder)another.Rudder.Clone(),
                (Radar)another.Radar.Clone(),
                another.Altitude);

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.VESSEL);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0 && Carrying.Fuel > 0;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(MountedMachineGuns);
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

namespace SteelOfStalin.Props.Units.Air
{
    public abstract class Plane : Aerial
    {
        public List<Gun> Guns { get; set; }
        public List<MountedMachineGun> MountedMachineGuns { get; set; }

        public Plane() : base() { }
        public Plane(Plane another) : base(another) => (Guns, MountedMachineGuns) = (new List<Gun>(another.Guns), new List<MountedMachineGun>(another.MountedMachineGuns));

        public abstract override object Clone();
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.PLANE);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0 && Carrying.Fuel > 0;
        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(MountedMachineGuns);
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
