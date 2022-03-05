using SteelOfStalin.Attributes;
using SteelOfStalin.Customizables.Modules;
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

        public Firearm() : base() { }
        public Firearm(Firearm another) : base(another)
            => (FirearmType, Offense, AmmoWeight, ConsumptionNormal, ConsumptionSuppress, Noise, ConcealmentPenaltyMove, ConcealmentPenaltyFire, MobilityPenalty)
            = (another.FirearmType,
                (Offense)another.Offense.Clone(),
                another.AmmoWeight,
                (Resources)another.ConsumptionNormal.Clone(),
                (Resources)another.ConsumptionSuppress.Clone(),
                (Attribute)another.Noise.Clone(),
                (Modifier)another.ConcealmentPenaltyMove.Clone(),
                (Modifier)another.ConcealmentPenaltyFire.Clone(),
                (Modifier)another.MobilityPenalty.Clone());

        public abstract override object Clone();
    }

    public abstract class Module : Customizable
    {
        public Attribute Integrity { get; set; } = new Attribute();
        public Attribute Weight { get; set; } = new Attribute();
        public Attribute FunctionalThreshold { get; set; } = new Attribute();
        public Attribute TakeDamageChance { get; set; } = new Attribute();

        public Module() : base() { }
        public Module(Module another) : base(another)
            => (Integrity, Weight, FunctionalThreshold, TakeDamageChance)
            = ((Attribute)another.Integrity.Clone(), (Attribute)another.Weight.Clone(), (Attribute)another.FunctionalThreshold.Clone(), (Attribute)another.TakeDamageChance.Clone());

        public abstract override object Clone();
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
        public Pistol() { }
        public Pistol(Pistol another) : base(another) { }
        public override object Clone() => new Pistol(this);
    }
    public class Revolver : Firearm
    {
        public Revolver() { }
        public Revolver(Revolver another) : base(another) { }
        public override object Clone() => new Revolver(this);
    }
    public class BurstPistol : Firearm
    {
        public BurstPistol() { }
        public BurstPistol(BurstPistol another) : base(another) { }
        public override object Clone() => new BurstPistol(this);
    }
    public class MachinePistol : Firearm
    {
        public MachinePistol() { }
        public MachinePistol(MachinePistol another) : base(another) { }
        public override object Clone() => new MachinePistol(this);
    }
    public class Submachinegun : Firearm
    {
        public Submachinegun() { }
        public Submachinegun(Submachinegun another) : base(another) { }
        public override object Clone() => new Submachinegun(this);
    }
    public class Carbine : Firearm
    {
        public Carbine() { }
        public Carbine(Carbine another) : base(another) { }
        public override object Clone() => new Carbine(this);
    }
    public class Rifle : Firearm
    {
        public Rifle() { }
        public Rifle(Rifle another) : base(another) { }
        public override object Clone() => new Rifle(this);
    }
    public class SemiAutoRifle : Firearm
    {
        public SemiAutoRifle() { }
        public SemiAutoRifle(SemiAutoRifle another) : base(another) { }
        public override object Clone() => new SemiAutoRifle(this);
    }
    public class BattleRifle : Firearm
    {
        public BattleRifle() { }
        public BattleRifle(BattleRifle another) : base(another) { }
        public override object Clone() => new BattleRifle(this);
    }
    public class AssaultRifle : Firearm
    {
        public AssaultRifle() { }
        public AssaultRifle(AssaultRifle another) : base(another) { }
        public override object Clone() => new AssaultRifle(this);
    }
    public class ScopedRifle : Firearm
    {
        public ScopedRifle() { }
        public ScopedRifle(ScopedRifle another) : base(another) { }
        public override object Clone() => new ScopedRifle(this);
    }
    public class Shotgun : Firearm
    {
        public Shotgun() { }
        public Shotgun(Shotgun another) : base(another) { }
        public override object Clone() => new Shotgun(this);
    }
    public class SemiAutoCarbine : Firearm
    {
        public SemiAutoCarbine() { }
        public SemiAutoCarbine(SemiAutoCarbine another) : base(another) { }
        public override object Clone() => new SemiAutoCarbine(this);
    }
    public class RocketLauncher : Firearm
    {
        public RocketLauncher() { }
        public RocketLauncher(RocketLauncher another) : base(another) { }
        public override object Clone() => new RocketLauncher(this);
    }
    public class GrenadeLauncher : Firearm
    {
        public GrenadeLauncher() { }
        public GrenadeLauncher(GrenadeLauncher another) : base(another) { }
        public override object Clone() => new GrenadeLauncher(this);
    }
    public class Mortar : Firearm
    {
        public Mortar() { }
        public Mortar(Mortar another) : base(another) { }
        public override object Clone() => new Mortar(this);
    }
    public class InfantryGun : Firearm
    {
        public InfantryGun() { }
        public InfantryGun(InfantryGun another) : base(another) { }
        public override object Clone() => new InfantryGun(this);
    }
    public class RecoillessRifle : Firearm
    {
        public RecoillessRifle() { }
        public RecoillessRifle(RecoillessRifle another) : base(another) { }
        public override object Clone() => new RecoillessRifle(this);
    }
    public class AutomaticGrenadeLauncher : Firearm
    {
        public AutomaticGrenadeLauncher() { }
        public AutomaticGrenadeLauncher(AutomaticGrenadeLauncher another) : base(another) { }
        public override object Clone() => new AutomaticGrenadeLauncher(this);
    }
    public class MultipleRocketLauncher : Firearm
    {
        public MultipleRocketLauncher() { }
        public MultipleRocketLauncher(MultipleRocketLauncher another) : base(another) { }
        public override object Clone() => new MultipleRocketLauncher(this);
    }
    public class Molotov : Firearm
    {
        public Molotov() { }
        public Molotov(Molotov another) : base(another) { }
        public override object Clone() => new Molotov(this);
    }
    public class AutomaticRifle : Firearm
    {
        public AutomaticRifle() { }
        public AutomaticRifle(AutomaticRifle another) : base(another) { }
        public override object Clone() => new AutomaticRifle(this);
    }
    public class LightMachinegun : Firearm
    {
        public LightMachinegun() { }
        public LightMachinegun(LightMachinegun another) : base(another) { }
        public override object Clone() => new LightMachinegun(this);
    }
    public class Grenade : Firearm
    {
        public Grenade() { }
        public Grenade(Grenade another) : base(another) { }
        public override object Clone() => new Grenade(this);
    }
    public class RifleGrenade : Firearm
    {
        public RifleGrenade() { }
        public RifleGrenade(RifleGrenade another) : base(another) { }
        public override object Clone() => new RifleGrenade(this);
    }
    public class Flamethrower : Firearm
    {
        public Flamethrower() { }
        public Flamethrower(Flamethrower another) : base(another) { }
        public override object Clone() => new Flamethrower(this);
    }
    public class MountainGun : Firearm
    {
        public MountainGun() { }
        public MountainGun(MountainGun another) : base(another) { }
        public override object Clone() => new MountainGun(this);
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

        public Gun() : base() { }
        public Gun(Gun another) : base(another)
            => (Offense, Noise, ConcealmentPenaltyFire, ConsumptionNormal, ConsumptionSuppress, CompatibleShells, CurrentShell)
            = ((Offense)another.Offense.Clone(), (Attribute)another.Noise.Clone(), (Modifier)another.ConcealmentPenaltyFire.Clone(), (Resources)another.ConsumptionNormal.Clone(), (Resources)another.ConsumptionSuppress.Clone(), new List<string>(another.CompatibleShells), (Shell)another.CurrentShell.Clone());

        public abstract override object Clone();
    }

    public abstract class MountedMachineGun : Module, IOffensiveCustomizable
    {
        public Offense Offense { get; set; } = new Offense();
        public Attribute Noise { get; set; } = new Attribute();
        public Modifier ConcealmentPenaltyFire { get; set; } = new Modifier();
        public double AmmoWeight { get; set; }
        public Resources ConsumptionNormal { get; set; } = new Resources();
        public Resources ConsumptionSuppress { get; set; } = new Resources();

        public MountedMachineGun() : base() { }
        public MountedMachineGun(MountedMachineGun another) : base(another)
            => (Offense, Noise, ConcealmentPenaltyFire, AmmoWeight, ConsumptionNormal, ConsumptionSuppress)
            = ((Offense)another.Offense.Clone(), (Attribute)another.Noise.Clone(), (Modifier)another.ConcealmentPenaltyFire.Clone(), another.AmmoWeight, (Resources)another.ConsumptionNormal.Clone(), (Resources)another.ConsumptionSuppress.Clone());

        public abstract override object Clone();
    }

    public class Engine : Module
    {
        public Attribute Horsepower { get; set; } = new Attribute();
        public Attribute FuelConsumption { get; set; } = new Attribute();

        public Engine() : base() { }
        public Engine(Engine another) : base(another) => (Horsepower, FuelConsumption) = ((Attribute)another.Horsepower.Clone(), (Attribute)another.FuelConsumption.Clone());
        public override object Clone() => new Engine(this);
    }
    public class Suspension : Module
    {
        public Attribute Traverse { get; set; } = new Attribute();

        public Suspension() : base() { }
        public Suspension(Suspension another) : base(another) => Traverse = (Attribute)another.Traverse.Clone();
        public override object Clone() => new Suspension(this);
    }
    public class Radio : Module
    {
        public Attribute SignalStrength { get; set; } = new Attribute();

        public Radio() : base() { }
        public Radio(Radio another) : base(another) => SignalStrength = (Attribute)another.SignalStrength.Clone();
        public override object Clone() => new Radio(this);
    }
    public class Periscope : Module
    {
        public Attribute ObservableRange { get; set; } = new Attribute();

        public Periscope() : base() { }
        public Periscope(Periscope another) : base(another) => ObservableRange = (Attribute)another.ObservableRange.Clone();
        public override object Clone() => new Periscope(this);
    }
    public class FuelTank : Module
    {
        public Attribute Capacity { get; set; } = new Attribute();
        public Attribute Leakage { get; set; } = new Attribute();
        public Attribute CatchFireChance { get; set; } = new Attribute();

        public FuelTank() : base() { }
        public FuelTank(FuelTank another) : base(another) => (Capacity, Leakage, CatchFireChance) = ((Attribute)another.Capacity.Clone(), (Attribute)another.Leakage.Clone(), (Attribute)another.CatchFireChance.Clone());
        public override object Clone() => new FuelTank(this);
    }
    public class CannonBreech : Module
    {
        public Attribute MisfireChance { get; set; } = new Attribute();

        public CannonBreech() : base() { }
        public CannonBreech(CannonBreech another) : base(another) => MisfireChance = (Attribute)another.MisfireChance.Clone();
        public override object Clone() => new CannonBreech(this);
    }
    public class AmmoRack : Module
    {
        public Attribute Capacity { get; set; } = new Attribute();
        public Attribute ExplosionChance { get; set; } = new Attribute();

        public AmmoRack() : base() { }
        public AmmoRack(AmmoRack another) : base(another) => (Capacity, ExplosionChance) = ((Attribute)another.Capacity.Clone(), (Attribute)another.ExplosionChance.Clone());
        public override object Clone() => new AmmoRack(this);
    }
    public class TorpedoTubes : Module
    {
        public Attribute Capacity { get; set; } = new Attribute();

        public TorpedoTubes() : base() { }
        public TorpedoTubes(TorpedoTubes another) : base(another) => Capacity = (Attribute)another.Capacity.Clone();
        public override object Clone() => new TorpedoTubes(this);
    }
    public class Sonar : Module
    {
        public Attribute Range { get; set; } = new Attribute();

        public Sonar() : base() { }
        public Sonar(Sonar another) : base(another) => Range = (Attribute)another.Range.Clone();
        public override object Clone() => new Sonar(this);
    }
    public class Propeller : Module
    {
        public Attribute Thrust { get; set; } = new Attribute();

        public Propeller() : base() { }
        public Propeller(Propeller another) : base(another) => Thrust = (Attribute)another.Thrust.Clone();
        public override object Clone() => new Propeller(this);
    }
    public class Rudder : Module
    {
        public Attribute Steering { get; set; } = new Attribute();

        public Rudder() : base() { }
        public Rudder(Rudder another) : base(another) => Steering = (Attribute)another.Steering.Clone();
        public override object Clone() => new Rudder(this);
    }
    public class Wings : Module
    {
        public Wings() : base() { }
        public Wings(Wings another) : base(another) { }
        public override object Clone() => new Wings(this);
    }
    public class LandingGear : Module
    {
        public LandingGear() : base() { }
        public LandingGear(LandingGear another) : base(another) { }
        public override object Clone() => new LandingGear(this);
    }
    public class Radar : Module
    {
        public Attribute Range { get; set; } = new Attribute();

        public Radar() : base() { }
        public Radar(Radar another) : base(another) => Range = (Attribute)another.Range.Clone();
        public override object Clone() => new Radar(this);
    }
}

namespace SteelOfStalin.Customizables.Guns
{
    public abstract class Cannon : Gun
    {
        public Cannon() : base() { }
        public Cannon(Cannon another) : base(another) { }
        public abstract override object Clone();
    }
    public abstract class Howitzer : Gun
    {
        public Howitzer() : base() { }
        public Howitzer(Howitzer another) : base(another) { }
        public abstract override object Clone();
    }
    public abstract class AutoCannon : Gun
    {
        public AutoCannon() : base() { }
        public AutoCannon(AutoCannon another) : base(another) { }
        public abstract override object Clone();
    }

    public class C20mm : Cannon
    {
        public C20mm() { }
        public C20mm(C20mm another) : base(another) { }
        public override object Clone() => new C20mm(this);
    }
    public class C37mm : Cannon
    {
        public C37mm() { }
        public C37mm(C37mm another) : base(another) { }
        public override object Clone() => new C37mm(this);
    }
    public class C50mm : Cannon
    {
        public C50mm() { }
        public C50mm(C50mm another) : base(another) { }
        public override object Clone() => new C50mm(this);
    }
    public class C75mm : Cannon
    {
        public C75mm() { }
        public C75mm(C75mm another) : base(another) { }
        public override object Clone() => new C75mm(this);
    }
    public class C88mm : Cannon
    {
        public C88mm() { }
        public C88mm(C88mm another) : base(another) { }
        public override object Clone() => new C88mm(this);
    }
    public class C128mm : Cannon
    {
        public C128mm() { }
        public C128mm(C128mm another) : base(another) { }
        public override object Clone() => new C128mm(this);
    }
    public class C152mm : Cannon
    {
        public C152mm() { }
        public C152mm(C152mm another) : base(another) { }
        public override object Clone() => new C152mm(this);
    }
    public class C203mm : Cannon
    {
        public C203mm() { }
        public C203mm(C203mm another) : base(another) { }
        public override object Clone() => new C203mm(this);
    }
    public class C280mm : Cannon
    {
        public C280mm() { }
        public C280mm(C280mm another) : base(another) { }
        public override object Clone() => new C280mm(this);
    }
    public class C305mm : Cannon
    {
        public C305mm() { }
        public C305mm(C305mm another) : base(another) { }
        public override object Clone() => new C305mm(this);
    }
    public class C381mm : Cannon
    {
        public C381mm() { }
        public C381mm(C381mm another) : base(another) { }
        public override object Clone() => new C381mm(this);
    }
    public class C480mm : Cannon
    {
        public C480mm() { }
        public C480mm(C480mm another) : base(another) { }
        public override object Clone() => new C480mm(this);
    }
    public class C800mm : Cannon
    {
        public C800mm() { }
        public C800mm(C800mm another) : base(another) { }
        public override object Clone() => new C800mm(this);
    }
    public class H75mm : Howitzer
    {
        public H75mm() { }
        public H75mm(H75mm another) : base(another) { }
        public override object Clone() => new H75mm(this);
    }
    public class H105mm : Howitzer
    {
        public H105mm() { }
        public H105mm(H105mm another) : base(another) { }
        public override object Clone() => new H105mm(this);
    }
    public class H122mm : Howitzer
    {
        public H122mm() { }
        public H122mm(H122mm another) : base(another) { }
        public override object Clone() => new H122mm(this);
    }
    public class H155mm : Howitzer
    {
        public H155mm() { }
        public H155mm(H155mm another) : base(another) { }
        public override object Clone() => new H155mm(this);
    }
    public class H203mm : Howitzer
    {
        public H203mm() { }
        public H203mm(H203mm another) : base(another) { }
        public override object Clone() => new H203mm(this);
    }
    public class H280mm : Howitzer
    {
        public H280mm() { }
        public H280mm(H280mm another) : base(another) { }
        public override object Clone() => new H280mm(this);
    }
    public class AC20mm : AutoCannon
    {
        public AC20mm() { }
        public AC20mm(AC20mm another) : base(another) { }
        public override object Clone() => new AC20mm(this);
    }
    public class AC30mm : AutoCannon
    {
        public AC30mm() { }
        public AC30mm(AC30mm another) : base(another) { }
        public override object Clone() => new AC30mm(this);
    }
    public class AC40mm : AutoCannon
    {
        public AC40mm() { }
        public AC40mm(AC40mm another) : base(another) { }
        public override object Clone() => new AC40mm(this);
    }
    public class AC57mm : AutoCannon
    {
        public AC57mm() { }
        public AC57mm(AC57mm another) : base(another) { }
        public override object Clone() => new AC57mm(this);
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

        public Shell() : base() { }
        public Shell(Shell another) : base(another)
            => (PenetrationCoefficient, PenetrationDeviation, AOEModifier, SplashDecayModifier, DropoffModifier)
            = ((Modifier)another.PenetrationCoefficient.Clone(),
                (Modifier)another.PenetrationDeviation.Clone(),
                (Modifier)another.AOEModifier.Clone(),
                (Modifier)another.SplashDecayModifier.Clone(),
                (Modifier)another.DropoffModifier.Clone());

        public abstract override object Clone();
    }

    public class AP : Shell
    {
        public AP() : base() { }
        public AP(AP another) : base(another) { }
        public override object Clone() => new AP(this);
    }
    public class HE : Shell
    {
        public HE() : base() { }
        public HE(HE another) : base(another) { }
        public override object Clone() => new HE(this);
    }
}
