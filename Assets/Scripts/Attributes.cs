using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

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

        public object Clone() => MemberwiseClone();
    }

    public class TerrainModifier : ICloneable
    {
        public Modifier Recon { get; set; } = new Modifier();
        public Modifier Camouflage { get; set; } = new Modifier();
        public Modifier Supplies { get; set; } = new Modifier();
        public Modifier Fuel { get; set; } = new Modifier();
        public Modifier Mobility { get; set; } = new Modifier();

        public TerrainModifier(TerrainModifier another)
            => (Recon, Camouflage, Supplies, Fuel, Mobility) 
            = ((Modifier)another.Recon.Clone(), 
               (Modifier)another.Camouflage.Clone(), 
               (Modifier)another.Supplies.Clone(),
               (Modifier)another.Fuel.Clone(), 
               (Modifier)another.Mobility.Clone());

        public object Clone() => new TerrainModifier(this);
    }

    public struct CubeCoordinates
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public CubeCoordinates(int x, int y, int z) : this() => (X, Y, Z) = (x, y, z);

        public static int GetDistance(CubeCoordinates c1, CubeCoordinates c2)
            => Mathf.Max(Mathf.Abs(c1.X - c2.X), Mathf.Abs(c1.Y - c2.Y), Mathf.Abs(c1.Z - c2.Z));

        /// <summary>
        /// Returns an enumerable of negibouring points with distance specified
        /// </summary>
        /// <param name="distance">The distance from the cube coordinates, inclusive</param>
        /// <returns></returns>
        public IEnumerable<Point> GetNeigbours(int distance = 1)
        {
            for (int x = 1; x <= distance; x++)
            {
                for (int y = 1; y <= distance; y++)
                {
                    for (int z = 1; z <= distance; z++)
                    {
                        yield return (Point)new CubeCoordinates(X + x, Y + y, Z + z);
                    }
                }
            }
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(CubeCoordinates c1, CubeCoordinates c2) 
            => c1.X == c2.X && c1.Y == c2.Y && c1.Z == c2.Z;
        public static bool operator !=(CubeCoordinates c1, CubeCoordinates c2)
            => !(c1.X == c2.X && c1.Y == c2.Y && c1.Z == c2.Z);

        public static explicit operator Point(CubeCoordinates c) => new Point(c.X, c.Z + (c.X - c.X % 2) / 2);
        public static explicit operator CubeCoordinates(Point p)
        {
            int z = p.Y - (p.X - p.X % 2) / 2;
            int y = -p.X - z;
            return new CubeCoordinates(p.X, y, z);
        }
    }

    public class Attribute : ICloneable
    {
        public Modifier Mod { get; set; } = new Modifier();
        public double Value { get; set; }

        public Attribute() { }
        public Attribute(double value, Modifier mod = null) => (Value, Mod) = (value, mod);

        public double ApplyMod() => Mod == null || Mod == default(Modifier) ? Value : Mod.Apply(Value); 

        public object Clone()
        {
            Attribute attr = (Attribute)MemberwiseClone();
            attr.Mod = (Modifier)Mod.Clone();
            return attr;
        }

        public static double operator +(Attribute a, Attribute b) => a.ApplyMod() + b.ApplyMod();
        public static double operator -(Attribute a, Attribute b) => a.ApplyMod() - b.ApplyMod();
        public static double operator *(Attribute a, Attribute b) => a.ApplyMod() * b.ApplyMod();
        public static double operator /(Attribute a, Attribute b) => a.ApplyMod() / b.ApplyMod();
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
        public Suppression Suppression { get; set; } = new Suppression();
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
               (Suppression)another.Suppression.Clone(),
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
        public Attribute Camouflage { get; set; } = new Attribute();
        public Attribute Detection { get; set; } = new Attribute();
        public Attribute Communication { get; set; } = new Attribute();

        public Scouting() { }
        public Scouting(Scouting another)
            => (Reconnaissance, Camouflage, Detection, Communication)
            = ((Attribute)another.Reconnaissance.Clone(),
               (Attribute)another.Camouflage.Clone(),
               (Attribute)another.Detection.Clone(),
               (Attribute)another.Communication.Clone());

        public object Clone() => new Scouting(this);
    }

    public enum PathfindingOptimization
    {
        LEAST_SUPPLIES_COST,
        LEAST_FUEL_COST
    }

    [Flags]
    public enum AutoCommands
    {
        NONE = 0,
        MOVE = 1 << 0,
        FIRE = 1 << 1,
        RESUPPLY = 1 << 2,
    }
}
