using SteelOfStalin.Props.Units;
using SteelOfStalin.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using UnityEngine;

namespace SteelOfStalin.Attributes
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModifierType
    {
        FIXED_VALUE,
        PERCENTAGE,
        MULTIPLE
    }

    public class Modifier : ICloneable
    {
        public ModifierType Type { get; set; }
        public double Value { get; set; }

        public Modifier() { }
        public Modifier(ModifierType type, double value) => (Type, Value) = (type, value);
        public Modifier(Modifier another) => (Type, Value) = (another.Type, another.Value);

        public double ApplyTo(double? value = null) => Type switch
        {
            ModifierType.FIXED_VALUE => (double)(value == null ? Value : value + Value),
            ModifierType.PERCENTAGE => (double)(value == null ? 1 + Value / 100 : value * (1 + Value / 100)),
            ModifierType.MULTIPLE => (double)(value == null ? Value : value * (1 + Value)),
            _ => throw new NotImplementedException(),
        };
        public double ApplyTo(Attribute a) => Type switch
        {
            ModifierType.FIXED_VALUE => a.ApplyMod() + Value,
            ModifierType.PERCENTAGE => a.ApplyMod() * (1 + Value / 100),
            ModifierType.MULTIPLE => a.ApplyMod() * (1 + Value),
            _ => throw new NotImplementedException(),
        };

        public object Clone() => MemberwiseClone();
        public override string ToString() => Type switch
        {
            ModifierType.FIXED_VALUE => Value.ToString("+#.##;-#.##"),
            ModifierType.PERCENTAGE => $"{Value:+#.##;-#.##}%",
            ModifierType.MULTIPLE => $"x{1 + Value}",
            _ => "",
        };

        public static Modifier Min(params Modifier[] modifiers) =>
            modifiers == null || modifiers.Length == 0
                ? throw new ArgumentException("No modifiers to compare!")
                : !modifiers.All(m => m.Type == modifiers[0].Type)
                    ? throw new ArgumentException("Cannot compare modifiers with different types!")
                    : modifiers.OrderBy(m => m.Value).First();

        public static Modifier Max(params Modifier[] modifiers) =>
            modifiers == null || modifiers.Length == 0
                ? throw new ArgumentException("No modifiers to compare!")
                : !modifiers.All(m => m.Type == modifiers[0].Type)
                    ? throw new ArgumentException("Cannot compare modifiers with different types!")
                    : modifiers.OrderByDescending(m => m.Value).First();
    }

    public class TerrainModifier : ICloneable
    {
        public Modifier Recon { get; set; } = new Modifier();
        public Modifier Concealment { get; set; } = new Modifier();
        public Modifier Supplies { get; set; } = new Modifier();
        public Modifier Fuel { get; set; } = new Modifier();
        public Modifier Mobility { get; set; } = new Modifier();

        public TerrainModifier() { }
        public TerrainModifier(TerrainModifier another)
            => (Recon, Concealment, Supplies, Fuel, Mobility)
            = ((Modifier)another.Recon.Clone(),
               (Modifier)another.Concealment.Clone(),
               (Modifier)another.Supplies.Clone(),
               (Modifier)another.Fuel.Clone(),
               (Modifier)another.Mobility.Clone());

        public object Clone() => new TerrainModifier(this);
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PathfindingOptimization
    {
        LEAST_SUPPLIES_COST,
        LEAST_FUEL_COST
    }

    public class Attribute : ICloneable
    {
        public Modifier Mod { get; set; } = new Modifier();
        public double Value { get; set; }

        public Attribute() { }
        public Attribute(double value, Modifier mod = null) => (Value, Mod) = (value, mod);

        public double ApplyMod() => Mod == null || Mod == default(Modifier) ? Value : Mod.ApplyTo(Value);
        public double ApplyDeviation() => Utilities.RandomBetweenSymmetricRange(ApplyMod());

        public void PlusEquals(double value) => Value = ApplyMod() + value;
        public void PlusEquals(Attribute attribute) => Value = ApplyMod() + attribute.ApplyMod();

        public void MinusEquals(double value) => Value = ApplyMod() - value;
        public void MinusEquals(Attribute attribute) => Value = ApplyMod() - attribute.ApplyMod();

        public bool TryTestEnough(Attribute test, out (double have, double discrepancy) tuple)
        {
            tuple = this >= test ? (0D, 0D) : (Value, test - this);
            return this >= test;
        }

        public object Clone()
        {
            Attribute attr = (Attribute)MemberwiseClone();
            attr.Mod = (Modifier)Mod.Clone();
            return attr;
        }
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Mod == null || Mod == default(Modifier) ? ApplyMod().ToString() : $"{Value} ({Mod})";

        public static double operator +(Attribute a, Attribute b) => a.ApplyMod() + b.ApplyMod();
        public static double operator +(Attribute b) => +b.ApplyMod();
        public static double operator -(Attribute a, Attribute b) => a.ApplyMod() - b.ApplyMod();
        public static double operator -(Attribute b) => -b.ApplyMod();
        public static double operator *(Attribute a, Attribute b) => a.ApplyMod() * b.ApplyMod();
        public static double operator /(Attribute a, Attribute b) => a.ApplyMod() / b.ApplyMod();
        public static bool operator >(Attribute a, Attribute b) => a.ApplyMod() > b.ApplyMod();
        public static bool operator <(Attribute a, Attribute b) => a.ApplyMod() < b.ApplyMod();
        public static bool operator >=(Attribute a, Attribute b) => a.ApplyMod() >= b.ApplyMod();
        public static bool operator <=(Attribute a, Attribute b) => a.ApplyMod() <= b.ApplyMod();
        public static bool operator ==(Attribute a, Attribute b) => a.ApplyMod() == b.ApplyMod();
        public static bool operator !=(Attribute a, Attribute b) => a.ApplyMod() != b.ApplyMod();

        public static double operator +(Attribute a, double b) => a.ApplyMod() + b;
        public static double operator -(Attribute a, double b) => a.ApplyMod() - b;
        public static double operator *(Attribute a, double b) => a.ApplyMod() * b;
        public static double operator /(Attribute a, double b) => a.ApplyMod() / b;
        public static bool operator >(Attribute a, double b) => a.ApplyMod() > b;
        public static bool operator <(Attribute a, double b) => a.ApplyMod() < b;
        public static bool operator >=(Attribute a, double b) => a.ApplyMod() >= b;
        public static bool operator <=(Attribute a, double b) => a.ApplyMod() <= b;
        public static bool operator ==(Attribute a, double b) => a.ApplyMod() == b;
        public static bool operator !=(Attribute a, double b) => a.ApplyMod() != b;

        public static double operator +(double a, Attribute b) => b.ApplyMod() + a;
        public static double operator -(double a, Attribute b) => b.ApplyMod() - a;
        public static double operator *(double a, Attribute b) => b.ApplyMod() * a;
        public static double operator /(double a, Attribute b) => b.ApplyMod() / a;
        public static bool operator >(double a, Attribute b) => b.ApplyMod() > a;
        public static bool operator <(double a, Attribute b) => b.ApplyMod() < a;
        public static bool operator >=(double a, Attribute b) => b.ApplyMod() >= a;
        public static bool operator <=(double a, Attribute b) => b.ApplyMod() <= a;
        public static bool operator ==(double a, Attribute b) => b.ApplyMod() == a;
        public static bool operator !=(double a, Attribute b) => b.ApplyMod() != a;
    }

    public class Resources : ICloneable
    {
        public Attribute Money { get; set; } = new Attribute();
        public Attribute Steel { get; set; } = new Attribute();
        public Attribute Supplies { get; set; } = new Attribute();
        public Attribute Cartridges { get; set; } = new Attribute();
        public Attribute Shells { get; set; } = new Attribute();
        public Attribute Fuel { get; set; } = new Attribute();
        public Attribute RareMetal { get; set; } = new Attribute();
        public Attribute Manpower { get; set; } = new Attribute();
        public Attribute Power { get; set; } = new Attribute();
        public Attribute Time { get; set; } = new Attribute();

        public IEnumerable<Attribute> All => Utilities.CombineAll(Money, Steel, Supplies, Cartridges, Shells, Fuel, RareMetal, Manpower, Time);
        public bool IsZero => All.All(a => a.Value == 0);

        public Resources() { }
        public Resources(Resources another)
            => (Money, Steel, Supplies, Cartridges, Shells, Fuel, RareMetal, Manpower, Power, Time)
            = ((Attribute)another.Money.Clone(),
               (Attribute)another.Steel.Clone(),
               (Attribute)another.Supplies.Clone(),
               (Attribute)another.Cartridges.Clone(),
               (Attribute)another.Shells.Clone(),
               (Attribute)another.Fuel.Clone(),
               (Attribute)another.RareMetal.Clone(),
               (Attribute)another.Manpower.Clone(),
               (Attribute)another.Power.Clone(),
               (Attribute)another.Time.Clone());

        public object Clone() => new Resources(this);

        // omit comparison for time intentionally, cuz it's meaningless (won't have insufficient "time")
        public bool HasEnoughResources(Resources need, bool print_discrepancy = true)
        {
            List<(string attr, double have, double discrepancy)> shortages = new List<(string attr, double have, double discrepancy)>();
            if (!Money.TryTestEnough(need.Money, out (double have, double discrepancy) money))
            {
                shortages.Add((nameof(Money), money.have, money.discrepancy));
            }
            if (!Steel.TryTestEnough(need.Steel, out (double have, double discrepancy) steel))
            {
                shortages.Add((nameof(Steel), steel.have, steel.discrepancy));
            }
            if (!Supplies.TryTestEnough(need.Supplies, out (double have, double discrepancy) supplies))
            {
                shortages.Add((nameof(Supplies), supplies.have, supplies.discrepancy));
            }
            if (!Cartridges.TryTestEnough(need.Cartridges, out (double have, double discrepancy) cartridges))
            {
                shortages.Add((nameof(Cartridges), cartridges.have, cartridges.discrepancy));
            }
            if (!Shells.TryTestEnough(need.Shells, out (double have, double discrepancy) shells))
            {
                shortages.Add((nameof(Shells), shells.have, shells.discrepancy));
            }
            if (!Fuel.TryTestEnough(need.Fuel, out (double have, double discrepancy) fuel))
            {
                shortages.Add((nameof(Fuel), fuel.have, fuel.discrepancy));
            }
            if (!RareMetal.TryTestEnough(need.RareMetal, out (double have, double discrepancy) raremetal))
            {
                shortages.Add((nameof(RareMetal), raremetal.have, raremetal.discrepancy));
            }
            if (!Manpower.TryTestEnough(need.Manpower, out (double have, double discrepancy) manpower))
            {
                shortages.Add((nameof(Manpower), manpower.have, manpower.discrepancy));
            };
            if (print_discrepancy)
            {
                foreach ((string attr, double have, double discrepancy) shortage in shortages)
                {
                    Debug.LogWarning($"Not enough {shortage.attr}! Have: {shortage.have}, Shortage: {shortage.discrepancy}");
                }
            }
            return shortages.Any();
        }

        public void Consume(Resources cost)
        {
            Money.MinusEquals(cost.Money);
            Steel.MinusEquals(cost.Steel);
            Supplies.MinusEquals(cost.Supplies);
            Cartridges.MinusEquals(cost.Cartridges);
            Shells.MinusEquals(cost.Shells);
            Fuel.MinusEquals(cost.Fuel);
            RareMetal.MinusEquals(cost.RareMetal);
            Manpower.MinusEquals(cost.Manpower);
        }
        public void Produce(Resources production)
        {
            Money.PlusEquals(production.Money);
            Steel.PlusEquals(production.Steel);
            Supplies.PlusEquals(production.Supplies);
            Cartridges.PlusEquals(production.Cartridges);
            Shells.PlusEquals(production.Shells);
            Fuel.PlusEquals(production.Fuel);
            RareMetal.PlusEquals(production.RareMetal);
            Manpower.PlusEquals(production.Manpower);
        }
    }

    public class Cost : ICloneable
    {
        public Resources Base { get; set; } = new Resources();
        public Resources Research { get; set; } = new Resources();
        public Resources Repair { get; set; } = new Resources();
        public Resources Fortification { get; set; } = new Resources();
        public Resources Manufacture { get; set; } = new Resources();
        public Resources Maintenance { get; set; } = new Resources();
        public Resources Recycling { get; set; } = new Resources();
        public Modifier CostModifier { get; set; } = new Modifier();

        public IEnumerable<Resources> All => Utilities.CombineAll(Base, Research, Repair, Fortification, Manufacture, Maintenance, Recycling);

        public Cost() { }
        public Cost(Cost another)
            => (Base, Research, Repair, Fortification, Manufacture, Maintenance, CostModifier)
            = ((Resources)another.Base.Clone(),
               (Resources)another.Research.Clone(),
               (Resources)another.Repair.Clone(),
               (Resources)another.Fortification.Clone(),
               (Resources)another.Manufacture.Clone(),
               (Resources)another.Recycling.Clone(),
               (Modifier)another.CostModifier.Clone());

        public object Clone() => new Cost(this);
    }

    public class Maneuverability : ICloneable
    {
        public Attribute Speed { get; set; } = new Attribute();
        public Attribute Mobility { get; set; } = new Attribute();
        public Attribute Size { get; set; } = new Attribute();
        public Attribute Weight { get; set; } = new Attribute();

        public Maneuverability() { }
        public Maneuverability(Maneuverability another)
            => (Speed, Mobility, Size, Weight)
            = ((Attribute)another.Speed.Clone(),
               (Attribute)another.Mobility.Clone(),
               (Attribute)another.Size.Clone(),
               (Attribute)another.Weight.Clone());

        public object Clone() => new Maneuverability(this);
    }

    public class Defense : ICloneable
    {
        public Attribute Strength { get; set; } = new Attribute();
        public Attribute Resistance { get; set; } = new Attribute();
        public Attribute Evasion { get; set; } = new Attribute();
        public Attribute Hardness { get; set; } = new Attribute();
        public Attribute Integrity { get; set; } = new Attribute();
        public Suppression Suppression { get; set; } = new Suppression();

        public Defense() { }
        public Defense(Defense another)
            => (Strength, Resistance, Evasion, Hardness, Integrity, Suppression)
            = ((Attribute)another.Strength.Clone(),
               (Attribute)another.Resistance.Clone(),
               (Attribute)another.Evasion.Clone(),
               (Attribute)another.Hardness.Clone(),
               (Attribute)another.Integrity.Clone(),
               (Suppression)another.Suppression.Clone());

        public object Clone() => new Defense(this);
    }

    public class Suppression : ICloneable
    {
        public Attribute Threshold { get; set; } = new Attribute();
        public Attribute Resilience { get; set; } = new Attribute();

        public Suppression() { }
        public Suppression(Suppression another)
            => (Threshold, Resilience) = ((Attribute)another.Threshold.Clone(), (Attribute)another.Resilience.Clone());

        public object Clone() => new Suppression(this);
    }

    public class Offense : ICloneable
    {
        public Handling Handling { get; set; } = new Handling();
        public Damage Damage { get; set; } = new Damage();
        public Accuracy Accuracy { get; set; } = new Accuracy();
        public AOE AOE { get; set; } = new AOE();
        public Attribute Suppression { get; set; } = new Attribute();
        public Attribute MinRange { get; set; } = new Attribute();
        public Attribute MaxRange { get; set; } = new Attribute();
        public bool IsDirectFire { get; set; }

        public Offense() { }
        public Offense(Offense another)
            => (Handling, Damage, Accuracy, AOE, Suppression, MinRange, MaxRange, IsDirectFire)
            = ((Handling)another.Handling.Clone(),
               (Damage)another.Damage.Clone(),
               (Accuracy)another.Accuracy.Clone(),
               (AOE)another.AOE.Clone(),
               (Attribute)another.Suppression.Clone(),
               (Attribute)another.MinRange.Clone(),
               (Attribute)another.MaxRange.Clone(),
               another.IsDirectFire);

        public object Clone() => new Offense(this);
    }

    public class Damage : ICloneable
    {
        public Attribute Soft { get; set; } = new Attribute();
        public Attribute Hard { get; set; } = new Attribute();
        public Attribute Destruction { get; set; } = new Attribute();
        public Attribute Deviation { get; set; } = new Attribute();
        public Attribute Dropoff { get; set; } = new Attribute();
        public Attribute Penetration { get; set; } = new Attribute();

        public Damage() { }
        public Damage(double soft, double hard, double destruction, double deviation, double dropoff)
            => (Soft.Value, Hard.Value, Destruction.Value, Deviation.Value, Dropoff.Value)
            = (soft, hard, destruction, deviation, dropoff);
        public Damage(double soft, double hard, double destruction, double deviation, double dropoff, double penetration)
            => (Soft.Value, Hard.Value, Destruction.Value, Deviation.Value, Dropoff.Value, Penetration.Value)
            = (soft, hard, destruction, deviation, dropoff, penetration);
        public Damage(Damage another)
            => (Soft, Hard, Destruction, Deviation, Dropoff, Penetration)
            = ((Attribute)another.Soft.Clone(),
               (Attribute)another.Hard.Clone(),
               (Attribute)another.Destruction.Clone(),
               (Attribute)another.Deviation.Clone(),
               (Attribute)another.Dropoff.Clone(),
               (Attribute)another.Penetration.Clone());

        public object Clone() => new Damage(this);
    }

    public class Handling : ICloneable
    {
        public Attribute Cyclic { get; set; } = new Attribute();
        public Attribute Clip { get; set; } = new Attribute();
        public Attribute Reload { get; set; } = new Attribute();
        public Attribute Aim { get; set; } = new Attribute();
        public Attribute Salvo { get; set; } = new Attribute();
        public double ROF { get; set; }
        public double ROFSuppress { get; set; }

        public Handling() { }
        public Handling(Handling another)
            => (Cyclic, Clip, Reload, Aim, Salvo, ROF, ROFSuppress)
            = ((Attribute)another.Cyclic.Clone(),
               (Attribute)another.Cyclic.Clone(),
               (Attribute)another.Cyclic.Clone(),
               (Attribute)another.Cyclic.Clone(),
               (Attribute)another.Cyclic.Clone(),
               another.ROF,
               another.ROFSuppress);

        public object Clone() => new Handling(this);
    }

    public class Accuracy : ICloneable
    {
        public Attribute Normal { get; set; } = new Attribute();
        public Attribute Suppress { get; set; } = new Attribute();
        public Attribute Deviation { get; set; } = new Attribute();

        public Accuracy() { }
        public Accuracy(Accuracy another)
            => (Normal, Suppress, Deviation)
            = ((Attribute)another.Normal.Clone(), (Attribute)another.Suppress.Clone(), (Attribute)another.Deviation.Clone());

        public object Clone() => new Accuracy(this);
    }

    public class AOE : ICloneable
    {
        public Attribute BlastRadius { get; set; } = new Attribute();
        public Attribute SplashDecay { get; set; } = new Attribute();

        public AOE() { }
        public AOE(AOE another)
            => (BlastRadius, SplashDecay)
            = ((Attribute)another.BlastRadius.Clone(),
               (Attribute)another.SplashDecay.Clone());

        public object Clone() => new AOE(this);
    }

    public class Payload : ICloneable
    {
        public List<Unit> Units { get; set; } = new List<Unit>();
        public Resources Cargo { get; set; } = new Resources();

        public Payload() { }
        public Payload(Payload another) => (Units, Cargo) = (another.Units, (Resources)another.Cargo.Clone());

        public object Clone() => new Payload(this);
    }

    public class LoadLimit : ICloneable
    {
        public Attribute Size { get; set; } = new Attribute();
        public Attribute Weight { get; set; } = new Attribute();
        public Resources CargoCapacity { get; set; } = new Resources();

        public LoadLimit() { }
        public LoadLimit(LoadLimit another)
            => (Size, Weight, CargoCapacity)
            = ((Attribute)another.Size.Clone(),
               (Attribute)another.Weight.Clone(),
               (Resources)another.CargoCapacity.Clone());

        public object Clone() => new LoadLimit(this);
    }

    public class Scouting : ICloneable
    {
        public Attribute Reconnaissance { get; set; } = new Attribute();
        public Attribute Concealment { get; set; } = new Attribute();
        public Attribute Detection { get; set; } = new Attribute();
        public Attribute Communication { get; set; } = new Attribute();

        public Scouting() { }
        public Scouting(Scouting another)
            => (Reconnaissance, Concealment, Detection, Communication)
            = ((Attribute)another.Reconnaissance.Clone(),
               (Attribute)another.Concealment.Clone(),
               (Attribute)another.Detection.Clone(),
               (Attribute)another.Communication.Clone());

        public object Clone() => new Scouting(this);
    }
}
