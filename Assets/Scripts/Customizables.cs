using SteelOfStalin.Attributes;
using SteelOfStalin.Customizables.Shells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Attribute = SteelOfStalin.Attributes.Attribute;

namespace SteelOfStalin.Customizables
{
    public enum FirearmType
    {
        NONE = 0,
        PRIMARY = 1 << 0,
        SECONDARY = 1 << 1,
        BOTH = PRIMARY | SECONDARY
    }

    public abstract class Firearm : Customizable, IOffensiveCustomizable
    {
        public FirearmType FirearmType { get; set; }
        public Offense Offense { get; set; } = new Offense();
        public double AmmoWeight { get; set; }
        public Resources ConsumptionNormal { get; set; } = new Resources();
        public Resources ConsumptionSuppress { get; set; } = new Resources();
        public Attribute Noise { get; set; } = new Attribute();
        public Modifier ConcealmentPenaltyMove { get; set; } = new Modifier();
        public Modifier ConcealmentPenaltyFire { get; set; } = new Modifier();
        public Modifier MobilityPenalty { get; set; } = new Modifier();
    }

    public abstract class Module : Customizable
    {
        public Attribute Integrity { get; set; } = new Attribute();
        public Attribute Weight { get; set; } = new Attribute();
        public Attribute FunctionalThreshold { get; set; } = new Attribute();
        public Attribute TakeDamageChance { get; set; } = new Attribute();
    }

    public interface IOffensiveCustomizable
    {
        public Offense Offense { get; set; }
        public Attribute Noise { get; set; }
        public Resources ConsumptionNormal { get; set; }
        public Resources ConsumptionSuppress { get; set; }
        public Modifier ConcealmentPenaltyFire { get; set; }
    }
}

namespace SteelOfStalin.Customizables.Firearms
{
    public class Pistol : Firearm
    {

    }
    public class Revolver : Firearm
    {

    }
    public class BurstPistol : Firearm
    {

    }
    public class MachinePistol : Firearm
    {

    }
    public class Submachinegun : Firearm
    {

    }
    public class Carbine : Firearm
    {

    }
    public class Rifle : Firearm
    {

    }
    public class SemiAutoRifle : Firearm
    {

    }
    public class BattleRifle : Firearm
    {
        
    }
    public class AssaultRifle : Firearm
    {

    }
    public class ScopedRifle : Firearm
    {

    }
    public class Shotgun : Firearm
    {

    }
    public class SemiAutoCarbine : Firearm
    {

    }
    public class RocketLauncher : Firearm
    {

    }
    public class GrenadeLauncher : Firearm
    {

    }
    public class Mortar : Firearm
    {

    }
    public class InfantryGun : Firearm
    {

    }
    public class RecoillessRifle : Firearm
    {

    }
    public class AutomaticGrenadeLauncher : Firearm
    {

    }
    public class MultipleRocketLauncher : Firearm
    {

    }
    public class Molotov : Firearm
    {

    }
    public class AutomaticRifle : Firearm
    {

    }
    public class LightMachinegun : Firearm
    {

    }
    public class Grenade : Firearm
    {

    }
    public class RifleGrenade : Firearm
    {

    }
    public class Flamethrower : Firearm
    {

    }
    public class MountainGun : Firearm
    {

    }
}

namespace SteelOfStalin.Customizables.Modules
{
    public abstract class Gun : Module, IOffensiveCustomizable
    {
        public Offense Offense { get; set; } = new Offense();
        public Attribute Noise { get; set; } = new Attribute();
        public Modifier ConcealmentPenaltyFire { get; set; } = new Modifier();
        public Resources ConsumptionNormal { get; set; } = new Resources();
        public Resources ConsumptionSuppress { get; set; } = new Resources(); // nth so far
        public List<string> CompatibleShells { get; set; } = new List<string>();
        public Shell CurrentShell { get; set; }
    }

    public abstract class MountedMachineGun : Module, IOffensiveCustomizable
    {
        public Offense Offense { get; set; } = new Offense();
        public Attribute Noise { get; set; } = new Attribute();
        public Modifier ConcealmentPenaltyFire { get; set; } = new Modifier();
        public double AmmoWeight { get; set; }
        public Resources ConsumptionNormal { get; set; } = new Resources();
        public Resources ConsumptionSuppress { get; set; } = new Resources();
    }

    public class Engine : Module
    {
        public Attribute Horsepower { get; set; } = new Attribute();
        public Attribute FuelConsumption { get; set; } = new Attribute();
    }
    public class Suspension
    {
        public Attribute Traverse { get; set; } = new Attribute();
    }
    public class Radio : Module
    {
        public Attribute SignalStrength { get; set; } = new Attribute();
    }
    public class Periscope : Module
    {
        public Attribute ObservableRange { get; set; } = new Attribute();
    }
    public class FuelTank : Module
    {
        public Attribute Capacity { get; set; } = new Attribute();
        public Attribute Leakage { get; set; } = new Attribute();
        public Attribute CatchFireChance { get; set; } = new Attribute();
    }
    public class CannonBreech : Module
    {
        public Attribute MisfireChance { get; set; } = new Attribute();
    }
    public class AmmoRack : Module
    {
        public Attribute Capacity { get; set; } = new Attribute();
    }
    public class TorpedoTubes : Module
    {
        public Attribute Capacity { get; set; } = new Attribute();
    }
    public class Sonar : Module
    {
        public Attribute Range { get; set; } = new Attribute();
    }
    public class Propeller : Module
    {
        public Attribute Thrust { get; set; } = new Attribute();
    }
    public class Rudder : Module
    {
        public Attribute Steering { get; set; } = new Attribute();
    }
    public class Wings : Module
    {

    }
    public class LandingGear : Module
    {

    }
    public class Radar : Module
    {
        public Attribute Range { get; set; } = new Attribute();
    }
}

namespace SteelOfStalin.Customizables.Shells
{
    public abstract class Shell : Customizable
    {
        public Modifier PenetrationCoefficient { get; set; } = new Modifier();
        public Modifier PenetrationDeviation { get; set; } = new Modifier();
        public Modifier AOEModifier { get; set; } = new Modifier();
        public Modifier SplashDecayModifier { get; set; } = new Modifier();
        public Modifier DropoffModifier { get; set; } = new Modifier();
    }

    public class AP : Shell
    {

    }
    public class HE : Shell
    {

    }
}
