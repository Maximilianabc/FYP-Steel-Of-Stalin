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

    }

    public abstract class Naval : Unit
    {

    }

    public abstract class Aerial : Unit
    {

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

        public Personnel() : base()
        {

        }

        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.PERSONNEL);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0;
        public virtual bool CanCapture() => !IsSuppressed && GetLocatedTile().IsCity;
        public virtual bool CanAboard()
        {
            // TODO
            return false;
        }
        public virtual bool CanRearm()
        {
            // TODO
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
            // TODO
            return false;
        }

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => new List<IOffensiveCustomizable>() { PrimaryFirearm, SecondaryFirearm };

        // false for secondary
        public virtual void ChangeFirearm(Firearm firearm, bool primary = true)
        {
            if (firearm == null)
            {
                LogError(this, "Firearm is null.");
                return;
            }
            if (!AvailableFirearms.Contains(firearm.Name))
            {
                LogError(this, $"Firearm {firearm.Name} is not available for unit {Name}.");
                return;
            }
            // changing primary but firearm is not a primary, or changing secondary but firearm is not a secondary
            if ((primary && !firearm.FirearmType.HasFlag(FirearmType.PRIMARY)) || (!primary && !firearm.FirearmType.HasFlag(FirearmType.SECONDARY)))
            {
                LogError(this, "Firearm type mismatch.");
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
        public bool IsAssembled = false;
        public double AssembleTime { get; set; }
        public string DefaultGun { get; set; }
        public Gun Gun { get; set; }
        public Radio Radio { get; set; }
        public CannonBreech CannonBreech { get; set; }

        public Artillery() : base()
        {

        }

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
            // TODO
            return false;
        }
        public virtual bool CanAssemble() => !IsSuppressed && !IsAssembled;
        public virtual bool CanDisassemble() => !IsSuppressed && IsAssembled;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => new List<IOffensiveCustomizable>() { Gun };
    }

    public abstract class Vehicle : Ground
    {
        public string DefaultMainArmament { get; set; }
        public List<Gun> Guns { get; set; } = new List<Gun>();
        public List<MountedMachineGun> MountedMachineGuns { get; set; } = new List<MountedMachineGun>();
        public Engine Engine { get; set; }
        public Suspension Suspension { get; set; }
        public Radio Radio { get; set; }
        public Periscope Periscope { get; set; }
        public FuelTank FuelTank { get; set; }
        public CannonBreech CannonBreech { get; set; }
        public AmmoRack AmmoRack { get; set; }

        public Vehicle() : base()
        {

        }

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

    }
    public class Infantry : Personnel
    {

    }
    public class Assault : Personnel
    {

    }
    public class Support : Personnel
    {

    }
    public class Mountain : Personnel
    {
        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && (t.Accessibility.HasFlag(Accessibility.PERSONNEL) || t.Type == TileType.MOUNTAINS);
    }
    public class Engineer : Personnel
    {
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

}

namespace SteelOfStalin.Props.Units.Land.Vehicles
{
    // different types of vehicles units here, all should inherit Vehicle

}

namespace SteelOfStalin.Props.Units.Sea
{
    public abstract class Vessel : Naval
    {
        public List<string> DefaultMainArmaments { get; set; } = new List<string>();
        public List<string> DefaultSecondaryArmaments { get; set; } = new List<string>();
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

        public Vessel() : base()
        {
            
        }

        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.VESSEL);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0 && Carrying.Fuel > 0;

        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(MountedMachineGuns);
    }

    // different types of naval units here, all should inherit Vessel
    public class Gunboat : Vessel
    {

    }
    public class Frigate : Vessel
    {

    }
    public class Destroyer : Vessel
    {

    }
    public class LightCruiser : Vessel
    {

    }
    public class Battlecruiser : Vessel
    {

    }
    public class Battelship : Vessel
    {

    }
    public class AircraftCarrier : Vessel
    {

    }
    public class Submarine : Vessel
    {

    }
    public class EscortCarrier : Vessel
    {

    }
    public class ReplenishmentOiler : Vessel
    {

    }
}

namespace SteelOfStalin.Props.Units.Air
{
    public abstract class Plane : Aerial
    {
        public List<Gun> Guns { get; set; }
        public List<MountedMachineGun> MountedMachineGuns { get; set; }
        public Plane() : base()
        {

        }

        public override bool CanAccessTile(Tile t) => base.CanAccessTile(t) && t.Accessibility.HasFlag(Accessibility.PLANE);
        public override bool CanMove() => base.CanMove() && Carrying.Supplies > 0 && Carrying.Fuel > 0;
        public override IEnumerable<IOffensiveCustomizable> GetWeapons() => Guns.Concat<IOffensiveCustomizable>(MountedMachineGuns);
    }

    // different types of aerial units here, all should inherit Plane
    public class Attacker : Plane
    {

    }
    public class Fighter : Plane
    {

    }
    public class Bomber : Plane
    {

    }
    public class TransportAircraft : Plane
    {

    }
    public class SurveillanceAircraft : Plane
    {

    }
}
