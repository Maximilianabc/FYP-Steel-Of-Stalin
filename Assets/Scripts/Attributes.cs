using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SteelOfStalin.Util;
using SteelOfStalin.Props.Units;

namespace SteelOfStalin.Attributes
{
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

        public double Apply(double? value = null) => Type switch
        {
            ModifierType.FIXED_VALUE => (double)(value == null ? Value : value + Value),
            ModifierType.PERCENTAGE => (double)(value == null ? 1 + Value / 100 : value * (1 + Value / 100)),
            ModifierType.MULTIPLE => (double)(value == null ? Value : value * Value),
            _ => throw new NotImplementedException(),
        };
        public double Apply(Attribute a) => Type switch
        {
            ModifierType.FIXED_VALUE => a.Value + Value,
            ModifierType.PERCENTAGE => a.Value * (1 + Value / 100),
            ModifierType.MULTIPLE => a.Value * Value,
            _ => throw new NotImplementedException(),
        };

        public Modifier SetValue(double value)
        {
            Value = value;
            return this;
        }

        public object Clone() => MemberwiseClone();
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

    public struct Coordinates : ICloneable, IEquatable<Coordinates>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinates(int x, int y) : this() => (X, Y) = (x, y);
        public Coordinates(Coordinates another) : this() => (X, Y) = (another.X, another.Y);

        public object Clone() => new Coordinates(this);
        public override string ToString() => $"({X},{Y})";
        public override bool Equals(object obj) => Equals((Coordinates)obj);
        public bool Equals(Coordinates other) => this == other;
        public override int GetHashCode() => (X, Y).GetHashCode();

        public static bool operator ==(Coordinates c1, Coordinates c2) => c1.X == c2.X && c1.Y == c2.Y;
        public static bool operator !=(Coordinates c1, Coordinates c2) => !(c1.X == c2.X && c1.Y == c2.Y);

    }

    public struct CubeCoordinates : IEquatable<CubeCoordinates>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public CubeCoordinates(int x, int y, int z) : this() => (X, Y, Z) = (x, y, z);

        public static int GetDistance(CubeCoordinates c1, CubeCoordinates c2)
            => Mathf.Max(Math.Abs(c1.X - c2.X), Math.Abs(c1.Y - c2.Y), Math.Abs(c1.Z - c2.Z));

        public static double GetStraightLineDistance(CubeCoordinates c1, CubeCoordinates c2)
        {
            List<int> diff = new List<int>()
            {
                Math.Abs(c1.X - c2.X),
                Math.Abs(c1.Y - c2.Y),
                Math.Abs(c1.Z - c2.Z)
            };
            diff = diff.OrderByDescending(x => x).ToList();

            // if one of the abs diff is 0, the tiles are on the same x/y/z axis
            // else apply cosine theorem with the abs diffs except the largest one (becuz it is the tile distance, not the sides of the triangle)
            // the angle is always 2 / 3 rad
            return diff.Contains(0)
                ? diff.Max()
                : Math.Sqrt(Math.Pow(diff[1], 2) + Math.Pow(diff[2], 2) - 2 * diff[1] * diff[2] * Math.Cos(2 / 3D));
        }

        /// <summary>
        /// Returns an enumerable of negibouring points with distance specified
        /// </summary>
        /// <param name="distance">The distance from the cube coordinates, inclusive</param>
        /// <returns></returns>
        public IEnumerable<CubeCoordinates> GetNeigbours(int distance = 1)
        {
            for (int x = -1; x <= distance; x++)
            {
                for (int y = -1; y <= distance; y++)
                {
                    for (int z = -1; z <= distance; z++)
                    {
                        yield return new CubeCoordinates(X + x, Y + y, Z + z);
                    }
                }
            }
        }

        public override string ToString() => $"({X},{Y},{Z})";
        public override bool Equals(object obj) => Equals((CubeCoordinates)obj);
        public bool Equals(CubeCoordinates other) => this == other;
        public override int GetHashCode() => (X, Y, Z).GetHashCode();

        public static bool operator ==(CubeCoordinates c1, CubeCoordinates c2) 
            => c1.X == c2.X && c1.Y == c2.Y && c1.Z == c2.Z;
        public static bool operator !=(CubeCoordinates c1, CubeCoordinates c2)
            => !(c1.X == c2.X && c1.Y == c2.Y && c1.Z == c2.Z);

        public static explicit operator Coordinates(CubeCoordinates c) => new Coordinates(c.X, c.Z + (c.X - c.X % 2) / 2);
        public static explicit operator CubeCoordinates(Coordinates p)
        {
            int z = p.Y - (p.X - p.X % 2) / 2;
            int y = -p.X - z;
            return new CubeCoordinates(p.X, y, z);
        }

    }

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

        public double ApplyMod() => Mod == null || Mod == default(Modifier) ? Value : Mod.Apply(Value);
        public double ApplyDeviation() => Utilities.RandomBetweenSymmetricRange(ApplyMod());

        public void PlusEquals(double value) => Value += value;
        public void PlusEquals(Attribute attribute) => Value = ApplyMod() + attribute.ApplyMod();

        public void MinusEquals(double value) => Value -= value;
        public void MinusEquals(Attribute attribute) => Value = ApplyMod() - attribute.ApplyMod();

        public object Clone()
        {
            Attribute attr = (Attribute)MemberwiseClone();
            attr.Mod = (Modifier)Mod.Clone();
            return attr;
        }
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public static double operator +(Attribute a, Attribute b) => a.ApplyMod() + b.ApplyMod();
        public static double operator -(Attribute a, Attribute b) => a.ApplyMod() - b.ApplyMod();
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
        public bool HasEnoughResources(Resources need) =>
               Money >= need.Money
            && Steel >= need.Steel
            && Supplies >= need.Supplies
            && Cartridges >= need.Cartridges
            && Shells >= need.Shells
            && Fuel >= need.Fuel
            && RareMetal >= need.RareMetal
            && Manpower >= need.Manpower
            && Power >= need.Power;

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
