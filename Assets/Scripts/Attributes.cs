using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteelOfStalin.Attributes
{
    public enum ModifierType
    {
        FIXED_VALUE,
        PERCENTAGE,
        MULTIPLE
    }

    public class Modifier
    {
        public ModifierType Type { get; set; }
        public double Value { get; set; }

        public Modifier() { }
        public Modifier(ModifierType type, double value) => (Type, Value) = (type, value);
    }

    public class TerrainModifier
    {
        public Modifier Recon { get; set; } = new Modifier();
        public Modifier Camouflage { get; set; } = new Modifier();
        public Modifier Supplies { get; set; } = new Modifier();
        public Modifier Fuel { get; set; } = new Modifier();
        public Modifier Mobility { get; set; } = new Modifier();
    }

    public class Attribute
    {
        public Modifier Mod { get; set; } = new Modifier();
        public double Value { get; set; }

        public Attribute() { }
        public Attribute(double value, Modifier mod = null) => (Value, Mod) = (value, mod);

        public double ApplyMod() => Mod?.Type switch
        {
            ModifierType.FIXED_VALUE => Value + Mod.Value,
            ModifierType.PERCENTAGE => Value * (1 + Mod.Value / 100),
            ModifierType.MULTIPLE => Value * Mod.Value,
            _ => Value
        };

        public static double operator +(Attribute a, Attribute b) => a.ApplyMod() + b.ApplyMod();
        public static double operator -(Attribute a, Attribute b) => a.ApplyMod() - b.ApplyMod();
        public static double operator *(Attribute a, Attribute b) => a.ApplyMod() * b.ApplyMod();
        public static double operator /(Attribute a, Attribute b) => a.ApplyMod() / b.ApplyMod();
    }

    public class Resources
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
    }

    public class Cost
    {
        public Resources Base { get; set; } = new Resources();
        public Resources Research { get; set; } = new Resources();
        public Resources Repair { get; set; } = new Resources();
        public Resources Fortification { get; set; } = new Resources();
        public Resources Manufacture { get; set; } = new Resources();
        public Resources Maintenance { get; set; } = new Resources();
        public Resources Recycling { get; set; } = new Resources();
        public Modifier CostModifier { get; set; } = new Modifier();
    }

    public class Maneuverability
    {
        public Attribute Speed { get; set; } = new Attribute();
        public Attribute Mobility { get; set; } = new Attribute();
        public Attribute Size { get; set; } = new Attribute();
        public Attribute Weight { get; set; } = new Attribute();
    }

    public class Defense
    {
        public Attribute Strength { get; set; } = new Attribute();
        public Attribute Resistance { get; set; } = new Attribute();
        public Attribute Evasion { get; set; } = new Attribute();
        public Attribute Hardness { get; set; } = new Attribute();
        public Attribute Integrity { get; set; } = new Attribute();
        public Suppression Suppression { get; set; } = new Suppression();
    }

    public class Suppression
    {
        public Attribute Threshold { get; set; } = new Attribute();
        public Attribute Resilience { get; set; } = new Attribute();
    }

    public class Offense
    {
        public Handling Handling { get; set; } = new Handling();
        public Damage Damage { get; set; } = new Damage();
        public Accuracy Accuracy { get; set; } = new Accuracy();
        public AOE AOE { get; set; } = new AOE();
        public Attribute Suppression { get; set; } = new Attribute();
        public Attribute MinRange { get; set; } = new Attribute();
        public Attribute MaxRange { get; set; } = new Attribute();
        public bool IsDirectFire { get; set; }
    }

    public class Damage
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
    }

    public class Handling
    {
        public Attribute Cyclic { get; set; } = new Attribute();
        public Attribute Clip { get; set; } = new Attribute();
        public Attribute Reload { get; set; } = new Attribute();
        public Attribute Aim { get; set; } = new Attribute();
        public Attribute Salvo { get; set; } = new Attribute();
        public double ROF { get; set; }
        public double ROFSuppress { get; set; }
    }

    public class Accuracy
    {
        public Attribute Normal { get; set; } = new Attribute();
        public Attribute Suppress { get; set; } = new Attribute();
        public Attribute Deviation { get; set; } = new Attribute();
    }

    public class AOE
    {
        public Attribute BlastRadius { get; set; } = new Attribute();
        public Attribute SplashDecay { get; set; } = new Attribute();
    }

    public class LoadLimit
    {
        public Attribute Size { get; set; } = new Attribute();
        public Attribute Weight { get; set; } = new Attribute();
        public Resources CargoCapacity { get; set; } = new Resources();
    }
    public class Scouting
    {
        public Attribute Reconnaissance { get; set; } = new Attribute();
        public Attribute Camouflage { get; set; } = new Attribute();
        public Attribute Detection { get; set; } = new Attribute();
        public Attribute Communication { get; set; } = new Attribute();
    }
}
