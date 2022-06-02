using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
        public decimal Value { get; set; }

        public Modifier() { }
        public Modifier(ModifierType type, decimal value) => (Type, Value) = (type, value);
        public Modifier(Modifier another) => (Type, Value) = (another.Type, another.Value);

        public decimal ApplyTo(decimal? value = null) => Type switch
        {
            ModifierType.FIXED_VALUE => (decimal)(value == null ? Value : value + Value),
            ModifierType.PERCENTAGE => (decimal)(value == null ? 1 + Value / 100 : value * (1 + Value / 100)),
            ModifierType.MULTIPLE => (decimal)(value == null ? Value : value * (1 + Value)),
            _ => throw new NotImplementedException(),
        };
        public decimal ApplyTo(Attribute a) => Type switch
        {
            ModifierType.FIXED_VALUE => a.ApplyMod() + Value,
            ModifierType.PERCENTAGE => a.ApplyMod() * (1 + Value / 100),
            ModifierType.MULTIPLE => a.ApplyMod() * (1 + Value),
            _ => throw new NotImplementedException(),
        };

        public object Clone() => MemberwiseClone();
        public override string ToString() => Type switch
        {
            ModifierType.FIXED_VALUE => Value.ToString("+0.##;-0.##"),
            ModifierType.PERCENTAGE => $"{Value:+0.##;-0.##}%",
            ModifierType.MULTIPLE => $"x{1 + Value}",
            _ => "",
        };
        public override bool Equals(object obj) => this == (Modifier)obj;
        public override int GetHashCode() => (Type, Value).GetHashCode();

        public static Modifier Min(params Modifier[] modifiers) =>
            (modifiers == null || modifiers.Length == 0)
                ? throw new ArgumentException("No modifiers to compare!")
                : !modifiers.All(m => m.Type == modifiers[0].Type)
                    ? throw new ArgumentException("Cannot compare modifiers with different types!")
                    : modifiers.OrderBy(m => m.Value).First();

        public static Modifier Max(params Modifier[] modifiers) =>
            (modifiers == null || modifiers.Length == 0)
                ? throw new ArgumentException("No modifiers to compare!")
                : !modifiers.All(m => m.Type == modifiers[0].Type)
                    ? throw new ArgumentException("Cannot compare modifiers with different types!")
                    : modifiers.OrderByDescending(m => m.Value).First();

        public static bool operator ==(Modifier a, Modifier b) => a?.Type == b?.Type && a?.Value == b?.Value;
        public static bool operator !=(Modifier a, Modifier b) => !(a?.Type == b?.Type && a?.Value == b?.Value);
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
        public override bool Equals(object obj) => this == (TerrainModifier)obj;
        public override int GetHashCode() => (Recon, Concealment, Supplies, Fuel, Mobility).GetHashCode();

        public static bool operator ==(TerrainModifier a, TerrainModifier b)
            => a.Recon == b.Recon
            && a.Concealment == b.Concealment
            && a.Supplies == b.Supplies
            && a.Fuel == b.Fuel
            && a.Mobility == b.Mobility;
        public static bool operator !=(TerrainModifier a, TerrainModifier b)
            => !(a.Recon == b.Recon
            && a.Concealment == b.Concealment
            && a.Supplies == b.Supplies
            && a.Fuel == b.Fuel
            && a.Mobility == b.Mobility);
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PathfindingOptimization
    {
        LEAST_SUPPLIES_COST,
        LEAST_FUEL_COST
    }

    public class Attribute : ICloneable
    {
        // TODO FUT. Impl. Handle attribute with mods and priority of mods (probably form expression during runtime)
        public Modifier Mod { get; set; } = new Modifier();
        public decimal Value { get; set; }

        public Attribute() { }
        public Attribute(decimal value, Modifier mod = null) => (Value, Mod) = (value, mod);

        public decimal ApplyMod() => (Mod == null || Mod == default) ? Value : Mod.ApplyTo(Value);
        public decimal ApplyDeviation() => Utilities.Random.NextInRangeSymmetric(ApplyMod());

        public void PlusEquals(decimal value) => Value = ApplyMod() + value;
        public void PlusEquals(Attribute attribute) => Value = ApplyMod() + attribute.ApplyMod();

        public void MinusEquals(decimal value) => Value = ApplyMod() - value;
        public void MinusEquals(Attribute attribute) => Value = ApplyMod() - attribute.ApplyMod();

        public bool TryTestEnough(Attribute test, out (decimal have, decimal discrepancy) tuple)
        {
            tuple = this >= test ? (0M, 0M) : (Value, test - this);
            return this >= test;
        }

        public object Clone()
        {
            Attribute attr = (Attribute)MemberwiseClone();
            attr.Mod = (Modifier)Mod?.Clone();
            return attr;
        }
        public override bool Equals(object obj) => this == (Attribute)obj;
        public override int GetHashCode() => (Mod, Value).GetHashCode();
        public override string ToString() => (Mod == null || Mod == default(Modifier)) ? ApplyMod().ToString() : $"{ApplyMod()} ({Mod})";
        public string ValueToString() => ApplyMod().ToString();

        public static decimal operator +(Attribute a, Attribute b) => a.ApplyMod() + b.ApplyMod();
        public static decimal operator +(Attribute b) => +b.ApplyMod();
        public static decimal operator -(Attribute a, Attribute b) => a.ApplyMod() - b.ApplyMod();
        public static decimal operator -(Attribute b) => -b.ApplyMod();
        public static decimal operator *(Attribute a, Attribute b) => a.ApplyMod() * b.ApplyMod();
        public static decimal operator /(Attribute a, Attribute b) => a.ApplyMod() / b.ApplyMod();
        public static bool operator >(Attribute a, Attribute b) => a.ApplyMod() > b.ApplyMod();
        public static bool operator <(Attribute a, Attribute b) => a.ApplyMod() < b.ApplyMod();
        public static bool operator >=(Attribute a, Attribute b) => a.ApplyMod() >= b.ApplyMod();
        public static bool operator <=(Attribute a, Attribute b) => a.ApplyMod() <= b.ApplyMod();
        public static bool operator ==(Attribute a, Attribute b) => a?.Mod == b?.Mod && a?.Value == b?.Value;
        public static bool operator !=(Attribute a, Attribute b) => !(a?.Mod == b?.Mod && a?.Value == b?.Value);

        public static decimal operator +(Attribute a, decimal b) => a.ApplyMod() + b;
        public static decimal operator -(Attribute a, decimal b) => a.ApplyMod() - b;
        public static decimal operator *(Attribute a, decimal b) => a.ApplyMod() * b;
        public static decimal operator /(Attribute a, decimal b) => a.ApplyMod() / b;
        public static bool operator >(Attribute a, decimal b) => a.ApplyMod() > b;
        public static bool operator <(Attribute a, decimal b) => a.ApplyMod() < b;
        public static bool operator >=(Attribute a, decimal b) => a.ApplyMod() >= b;
        public static bool operator <=(Attribute a, decimal b) => a.ApplyMod() <= b;
        public static bool operator ==(Attribute a, decimal b) => a.ApplyMod() == b;
        public static bool operator !=(Attribute a, decimal b) => a.ApplyMod() != b;

        public static decimal operator +(decimal a, Attribute b) => a + b.ApplyMod();
        public static decimal operator -(decimal a, Attribute b) => a - b.ApplyMod();
        public static decimal operator *(decimal a, Attribute b) => a * b.ApplyMod();
        public static decimal operator /(decimal a, Attribute b) => a / b.ApplyMod();
        public static bool operator >(decimal a, Attribute b) => a > b.ApplyMod();
        public static bool operator <(decimal a, Attribute b) => a < b.ApplyMod();
        public static bool operator >=(decimal a, Attribute b) => a >= b.ApplyMod();
        public static bool operator <=(decimal a, Attribute b) => a <= b.ApplyMod();
        public static bool operator ==(decimal a, Attribute b) => a == b.ApplyMod();
        public static bool operator !=(decimal a, Attribute b) => a != b.ApplyMod();
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
        internal static Resources INFINITE => new Resources()
        {
            Money = new Attribute(decimal.MaxValue),
            Steel = new Attribute(decimal.MaxValue),
            Supplies = new Attribute(decimal.MaxValue),
            Cartridges = new Attribute(decimal.MaxValue),
            Shells = new Attribute(decimal.MaxValue),
            Fuel = new Attribute(decimal.MaxValue),
            RareMetal = new Attribute(decimal.MaxValue),
            Manpower = new Attribute(decimal.MaxValue),
            Power = new Attribute(decimal.MaxValue),
            Time = new Attribute(decimal.MaxValue)
        };
        internal static Resources TEST => new Resources()
        {
            Money = new Attribute(99999),
            Steel = new Attribute(99999),
            Supplies = new Attribute(99999),
            Cartridges = new Attribute(99999),
            Shells = new Attribute(99999),
            Fuel = new Attribute(99999),
            RareMetal = new Attribute(99999),
            Manpower = new Attribute(99999),
            Power = new Attribute(99999),
            Time = new Attribute(99999)
        };
        internal static Resources START => new Resources()
        {
            Money = new Attribute(20000),
            Steel = new Attribute(10000),
            Supplies = new Attribute(5000),
            Cartridges = new Attribute(2500),
            Shells = new Attribute(1000),
            Fuel = new Attribute(2000),
            RareMetal = new Attribute(500),
            Manpower = new Attribute(10000),
            Power = new Attribute(0),
            Time = new Attribute(0)
        };
        [JsonIgnore] public IEnumerable<Attribute> All => Utilities.CombineAll(Money, Steel, Supplies, Cartridges, Shells, Fuel, RareMetal, Manpower, Time);
        [JsonIgnore] public bool IsZero => All.All(a => a.Value == 0);

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
        public override bool Equals(object obj) => this == (Resources)obj;
        public override int GetHashCode() => (Money, Steel, Supplies, Cartridges, Shells, Fuel, RareMetal, Manpower, Power, Time).GetHashCode();
        public override string ToString() => string.Join(";",
            Money.ValueToString(),
            Steel.ValueToString(),
            Supplies.ValueToString(),
            Cartridges.ValueToString(),
            Shells.ValueToString(),
            Fuel.ValueToString(),
            RareMetal.ValueToString(),
            Manpower.ValueToString(),
            Power.ValueToString(),
            Time.ValueToString());
        public void UpdateFromString(string res_string)
        {
            MatchCollection mc = Regex.Matches(res_string, @"(\d+\.?\d*)");
            if (mc.Count != 10)
            {
                Debug.LogError($"Failed to parse resources from string {res_string}. Expected match collection length: 10. Actual {mc.Count}");
                return;
            }

            /* note: order of properties retrieved by GetProperties is not guaranteed 
             * as stated in https://docs.microsoft.com/en-us/dotnet/api/system.type.getproperties?view=net-6.0
             * but so far it is in correct order
             * TODO FUT Impl. find a way to make this independent on the order
             */
            int i = 0;
            foreach (PropertyInfo prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(Attribute)))
            {
                if (decimal.TryParse(mc[i].Value, out decimal value))
                {
                    object prop_object = prop.GetValue(this);
                    typeof(Attribute).GetProperty("Value").SetValue(prop_object, value);
                }
                else
                {
                    Debug.LogError($"Failed to parse {prop.Name} from match {mc[i].Value}");
                }
                i++;
            }
        }

        // omit comparison for time intentionally, cuz it's meaningless (won't have insufficient "time")
        public bool HasEnoughResources(Resources need, bool print_discrepancy = true)
        {
            List<(string attr, decimal have, decimal discrepancy)> shortages = new List<(string attr, decimal have, decimal discrepancy)>();
            if (!Money.TryTestEnough(need.Money, out (decimal have, decimal discrepancy) money))
            {
                shortages.Add((nameof(Money), money.have, money.discrepancy));
            }
            if (!Steel.TryTestEnough(need.Steel, out (decimal have, decimal discrepancy) steel))
            {
                shortages.Add((nameof(Steel), steel.have, steel.discrepancy));
            }
            if (!Supplies.TryTestEnough(need.Supplies, out (decimal have, decimal discrepancy) supplies))
            {
                shortages.Add((nameof(Supplies), supplies.have, supplies.discrepancy));
            }
            if (!Cartridges.TryTestEnough(need.Cartridges, out (decimal have, decimal discrepancy) cartridges))
            {
                shortages.Add((nameof(Cartridges), cartridges.have, cartridges.discrepancy));
            }
            if (!Shells.TryTestEnough(need.Shells, out (decimal have, decimal discrepancy) shells))
            {
                shortages.Add((nameof(Shells), shells.have, shells.discrepancy));
            }
            if (!Fuel.TryTestEnough(need.Fuel, out (decimal have, decimal discrepancy) fuel))
            {
                shortages.Add((nameof(Fuel), fuel.have, fuel.discrepancy));
            }
            if (!RareMetal.TryTestEnough(need.RareMetal, out (decimal have, decimal discrepancy) raremetal))
            {
                shortages.Add((nameof(RareMetal), raremetal.have, raremetal.discrepancy));
            }
            if (!Manpower.TryTestEnough(need.Manpower, out (decimal have, decimal discrepancy) manpower))
            {
                shortages.Add((nameof(Manpower), manpower.have, manpower.discrepancy));
            };
            if (print_discrepancy)
            {
                foreach ((string attr, decimal have, decimal discrepancy) shortage in shortages)
                {
                    Debug.LogWarning($"Not enough {shortage.attr}! Have: {shortage.have}, Shortage: {shortage.discrepancy}");
                }
            }
            return !shortages.Any();
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

        public static bool operator ==(Resources a, Resources b)
            => a?.Money == b?.Money
            && a?.Steel == b?.Steel
            && a?.Supplies == b?.Supplies
            && a?.Cartridges == b?.Cartridges
            && a?.Shells == b?.Shells
            && a?.Fuel == b?.Fuel
            && a?.RareMetal == b?.RareMetal
            && a?.Manpower == b?.Manpower
            && a?.Power == b?.Power
            && a?.Time == b?.Time;
        public static bool operator !=(Resources a, Resources b)
            => !(a?.Money == b?.Money
            && a?.Steel == b?.Steel
            && a?.Supplies == b?.Supplies
            && a?.Cartridges == b?.Cartridges
            && a?.Shells == b?.Shells
            && a?.Fuel == b?.Fuel
            && a?.RareMetal == b?.RareMetal
            && a?.Manpower == b?.Manpower
            && a?.Power == b?.Power
            && a?.Time == b?.Time);
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

        [JsonIgnore] public IEnumerable<Resources> All => Utilities.CombineAll(Base, Research, Repair, Fortification, Manufacture, Maintenance, Recycling);

        public Cost() { }
        public Cost(Cost another)
            => (Base, Research, Repair, Fortification, Manufacture, Maintenance, Recycling, CostModifier)
            = ((Resources)another.Base.Clone(),
               (Resources)another.Research.Clone(),
               (Resources)another.Repair.Clone(),
               (Resources)another.Fortification.Clone(),
               (Resources)another.Manufacture.Clone(),
               (Resources)another.Maintenance.Clone(),
               (Resources)another.Recycling.Clone(),
               (Modifier)another.CostModifier.Clone());

        public object Clone() => new Cost(this);
        public override bool Equals(object obj) => this == (Cost)obj;
        public override int GetHashCode() => (Base, Research, Repair, Fortification, Manufacture, Maintenance, Recycling, CostModifier).GetHashCode();

        public static bool operator ==(Cost a, Cost b)
            => a.Base == b.Base
            && a.Research == b.Research
            && a.Repair == b.Repair
            && a.Fortification == b.Fortification
            && a.Manufacture == b.Manufacture
            && a.Maintenance == b.Maintenance
            && a.Recycling == b.Recycling
            && a.CostModifier == b.CostModifier;
        public static bool operator !=(Cost a, Cost b)
            => !(a.Base == b.Base
            && a.Research == b.Research
            && a.Repair == b.Repair
            && a.Fortification == b.Fortification
            && a.Manufacture == b.Manufacture
            && a.Maintenance == b.Maintenance
            && a.Recycling == b.Recycling
            && a.CostModifier == b.CostModifier);
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
        public override bool Equals(object obj) => this == (Maneuverability)obj;
        public override int GetHashCode() => (Speed, Mobility, Size, Weight).GetHashCode();

        public static bool operator ==(Maneuverability a, Maneuverability b)
            => a.Speed == b.Speed
            && a.Mobility == b.Mobility
            && a.Size == b.Size
            && a.Weight == b.Weight;
        public static bool operator !=(Maneuverability a, Maneuverability b)
            => !(a.Speed == b.Speed
            && a.Mobility == b.Mobility
            && a.Size == b.Size
            && a.Weight == b.Weight);
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
        public override bool Equals(object obj) => this == (Defense)obj;
        public override int GetHashCode() => (Strength, Resistance, Evasion, Hardness, Integrity, Suppression).GetHashCode();

        public static bool operator ==(Defense a, Defense b)
            => a.Strength == b.Strength
            && a.Resistance == b.Resistance
            && a.Evasion == b.Evasion
            && a.Hardness == b.Hardness
            && a.Integrity == b.Integrity
            && a.Suppression == b.Suppression;
        public static bool operator !=(Defense a, Defense b)
            => !(a.Strength == b.Strength
            && a.Resistance == b.Resistance
            && a.Evasion == b.Evasion
            && a.Hardness == b.Hardness
            && a.Integrity == b.Integrity
            && a.Suppression == b.Suppression);
    }

    public class Suppression : ICloneable
    {
        public Attribute Threshold { get; set; } = new Attribute();
        public Attribute Resilience { get; set; } = new Attribute();

        public Suppression() { }
        public Suppression(Suppression another)
            => (Threshold, Resilience) = ((Attribute)another.Threshold.Clone(), (Attribute)another.Resilience.Clone());

        public object Clone() => new Suppression(this);
        public override bool Equals(object obj) => this == (Suppression)obj;
        public override int GetHashCode() => (Threshold, Resilience).GetHashCode();

        public static bool operator ==(Suppression a, Suppression b) => a.Threshold == b.Threshold && a.Resilience == b.Resilience;
        public static bool operator !=(Suppression a, Suppression b) => !(a.Threshold == b.Threshold && a.Resilience == b.Resilience);
    }

    public class Offense : ICloneable, IEquatable<Offense>
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
        public bool Equals(Offense other) => this == other;
        public override bool Equals(object obj) => this == (Offense)obj;
        public override int GetHashCode() => (Handling, Damage, Accuracy, AOE, Suppression, MinRange, MaxRange, IsDirectFire).GetHashCode();

        public static bool operator ==(Offense a, Offense b)
            => a.Handling == b.Handling
            && a.Damage == b.Damage
            && a.Accuracy == b.Accuracy
            && a.AOE == b.AOE
            && a.Suppression == b.Suppression
            && a.MinRange == b.MinRange
            && a.MaxRange == b.MaxRange
            && a.IsDirectFire == b.IsDirectFire;
        public static bool operator !=(Offense a, Offense b)
            => !(a.Handling == b.Handling
            && a.Damage == b.Damage
            && a.Accuracy == b.Accuracy
            && a.AOE == b.AOE
            && a.Suppression == b.Suppression
            && a.MinRange == b.MinRange
            && a.MaxRange == b.MaxRange
            && a.IsDirectFire == b.IsDirectFire);
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
        public Damage(Damage another)
            => (Soft, Hard, Destruction, Deviation, Dropoff, Penetration)
            = ((Attribute)another.Soft.Clone(),
               (Attribute)another.Hard.Clone(),
               (Attribute)another.Destruction.Clone(),
               (Attribute)another.Deviation.Clone(),
               (Attribute)another.Dropoff.Clone(),
               (Attribute)another.Penetration.Clone());

        public object Clone() => new Damage(this);
        public override bool Equals(object obj) => this == (Damage)obj;
        public override int GetHashCode() => (Soft, Hard, Destruction, Deviation, Dropoff, Penetration).GetHashCode();

        public static bool operator ==(Damage a, Damage b)
            => a.Soft == b.Soft
            && a.Hard == b.Hard
            && a.Destruction == b.Destruction
            && a.Deviation == b.Deviation
            && a.Dropoff == b.Dropoff
            && a.Penetration == b.Penetration;
        public static bool operator !=(Damage a, Damage b)
            => !(a.Soft == b.Soft
            && a.Hard == b.Hard
            && a.Destruction == b.Destruction
            && a.Deviation == b.Deviation
            && a.Dropoff == b.Dropoff
            && a.Penetration == b.Penetration);
    }

    public class Handling : ICloneable
    {
        public Attribute Cyclic { get; set; } = new Attribute();
        public Attribute Clip { get; set; } = new Attribute();
        public Attribute Reload { get; set; } = new Attribute();
        public Attribute Aim { get; set; } = new Attribute();
        public Attribute Salvo { get; set; } = new Attribute();
        public decimal ROF { get; set; }
        public decimal ROFSuppress { get; set; }

        public Handling() { }
        public Handling(Handling another)
            => (Cyclic, Clip, Reload, Aim, Salvo, ROF, ROFSuppress)
            = ((Attribute)another.Cyclic.Clone(),
               (Attribute)another.Clip.Clone(),
               (Attribute)another.Reload.Clone(),
               (Attribute)another.Aim.Clone(),
               (Attribute)another.Salvo.Clone(),
               another.ROF,
               another.ROFSuppress);

        public object Clone() => new Handling(this);
        public override bool Equals(object obj) => this == (Handling)obj;
        public override int GetHashCode() => (Cyclic, Clip, Reload, Aim, Salvo, ROF, ROFSuppress).GetHashCode();

        public static bool operator ==(Handling a, Handling b)
            => a.Cyclic == b.Cyclic
            && a.Clip == b.Clip
            && a.Reload == b.Reload
            && a.Aim == b.Aim
            && a.Salvo == b.Salvo
            && a.ROF == b.ROF
            && a.ROFSuppress == b.ROFSuppress;
        public static bool operator !=(Handling a, Handling b)
            => !(a.Cyclic == b.Cyclic
            && a.Clip == b.Clip
            && a.Reload == b.Reload
            && a.Aim == b.Aim
            && a.Salvo == b.Salvo
            && a.ROF == b.ROF
            && a.ROFSuppress == b.ROFSuppress);
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
        public override bool Equals(object obj) => this == (Accuracy)obj;
        public override int GetHashCode() => (Normal, Suppress, Deviation).GetHashCode();

        public static bool operator ==(Accuracy a, Accuracy b)
            => a.Normal == b.Normal
            && a.Suppress == b.Suppress
            && a.Deviation == b.Deviation;
        public static bool operator !=(Accuracy a, Accuracy b)
            => !(a.Normal == b.Normal
            && a.Suppress == b.Suppress
            && a.Deviation == b.Deviation);
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
        public override bool Equals(object obj) => this == (AOE)obj;
        public override int GetHashCode() => (BlastRadius, SplashDecay).GetHashCode();

        public static bool operator ==(AOE a, AOE b) => a.BlastRadius == b.BlastRadius && a.SplashDecay == b.SplashDecay;
        public static bool operator !=(AOE a, AOE b) => !(a.BlastRadius == b.BlastRadius && a.SplashDecay == b.SplashDecay);
    }

    public class Payload : ICloneable
    {
        public List<Unit> Units { get; set; } = new List<Unit>();
        public Resources Cargo { get; set; } = new Resources();

        public Payload() { }
        public Payload(Payload another) => (Units, Cargo) = (new List<Unit>(another.Units), (Resources)another.Cargo.Clone());

        public object Clone() => new Payload(this);
        public override bool Equals(object obj) => this == (Payload)obj;
        public override int GetHashCode() => (Units, Cargo).GetHashCode();

        public static bool operator ==(Payload a, Payload b) => a.Units.All(b.Units.Contains) && a.Units.Count == b.Units.Count && a.Cargo == b.Cargo;
        public static bool operator !=(Payload a, Payload b) => !(a.Units.All(b.Units.Contains) && a.Units.Count == b.Units.Count && a.Cargo == b.Cargo);
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
        public override bool Equals(object obj) => this == (LoadLimit)obj;
        public override int GetHashCode() => (Size, Weight, CargoCapacity).GetHashCode();

        public static bool operator ==(LoadLimit a, LoadLimit b)
            => a.Size == b.Size
            && a.Weight == b.Weight
            && a.CargoCapacity == b.CargoCapacity;
        public static bool operator !=(LoadLimit a, LoadLimit b)
            => !(a.Size == b.Size
            && a.Weight == b.Weight
            && a.CargoCapacity == b.CargoCapacity);
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
        public override bool Equals(object obj) => this == (Scouting)obj;
        public override int GetHashCode() => (Reconnaissance, Concealment, Detection, Communication).GetHashCode();

        public static bool operator ==(Scouting a, Scouting b)
            => a.Reconnaissance == b.Reconnaissance
            && a.Concealment == b.Concealment
            && a.Detection == b.Detection
            && a.Communication == b.Communication;
        public static bool operator !=(Scouting a, Scouting b)
            => !(a.Reconnaissance == b.Reconnaissance
            && a.Concealment == b.Concealment
            && a.Detection == b.Detection
            && a.Communication == b.Communication);
    }
}
