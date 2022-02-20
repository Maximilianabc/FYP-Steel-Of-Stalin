using SteelOfStalin.Attributes;
using SteelOfStalin.Customizables;
using SteelOfStalin.Customizables.Modules;
using SteelOfStalin.Flow;
using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Attribute = SteelOfStalin.Attributes.Attribute;

namespace SteelOfStalin.Util
{
    public static class Utilities
    {
        public static void Log(Command command, string @event) => Debug.Log($"Unit at {command.Unit.PrintCoOrds()} has the following event when executing the command {command.GetType()}: {@event}");

        public static void LogError(Command command, string reason, string explanation = "") => Debug.LogError($"Failed to execute command {command.GetType()} for unit at {command.Unit.PrintCoOrds()}: {reason} {explanation}");
        public static void LogError(Unit unit, string reason, [CallerMemberName] string method_name = "") => Debug.LogError($"Failed to execute method {method_name} for unit at {unit.PrintCoOrds()}: {reason}");
        public static void LogError(Phase phase, string reason) => Debug.LogError($"Error when executing phases {phase.GetType()}: {reason}");

        public static IEnumerable<T> Flatten<T>(this T[][] ts) => ts.SelectMany(t => t);

        public static double RandomBetweenSymmetricRange(double range) => range * (new System.Random().NextDouble() * 2 - 1);
    }

    public static class Formula
    {
        public static Func<(IOffensiveCustomizable weapon, double distance), double> DamageDropoff => (input) =>
        {
            double d = input.weapon.Offense.Damage.Dropoff.ApplyMod();
            double r = 1 + 0.6 / Math.Pow(input.weapon.Offense.MaxRange.ApplyMod(), 2);
            return 1.4 / Math.PI * (Math.Acos(d * r * input.distance - 1) - d * r * Math.Sqrt(-input.distance * (input.distance - 2 / (d * r))));
        };
        public static Func<(Attribute soft, Attribute hard, double multiplier, Attribute hardness), double> DamageWithHardness => (input) =>
        {
            double soft_damage = input.soft * input.multiplier * (1 - input.hardness);
            double hard_damage = input.hard * input.multiplier * input.hardness;
            return soft_damage + hard_damage;
        };
        public static Func<(IOffensiveCustomizable weapon, Unit defender, double distance), double> DamageAgainstPersonnel => (input) =>
        {
            Accuracy accuracy = input.weapon.Offense.Accuracy;
            Damage damage = input.weapon.Offense.Damage;
            Defense defense = input.defender.Defense;

            double final_accuracy = accuracy.Normal.ApplyMod() + accuracy.Deviation.ApplyDeviation();
            double dropoff = input.weapon is Gun ? 1 : DamageDropoff((input.weapon, input.distance));
            double damage_multiplier = (1 + damage.Deviation.ApplyDeviation()) * (1 - defense.Evasion) * dropoff;
            return DamageWithHardness((damage.Soft, damage.Hard, damage_multiplier, defense.Hardness));
        };
        public static Func<(IOffensiveCustomizable weapon, Unit defender, double distance), double> DamageAgainstZeroResistance => (input) =>
        {
            Damage damage = input.weapon.Offense.Damage;
            Defense defense = input.defender.Defense;

            double dropoff = input.weapon is Gun ? 1 : DamageDropoff((input.weapon, input.distance));
            double damage_multiplier = 0.25 * (1 + damage.Deviation.ApplyDeviation()) * dropoff;
            return DamageWithHardness((damage.Soft, damage.Hard, damage_multiplier, defense.Hardness));
        };
        public static Func<(IOffensiveCustomizable weapon, Unit defender), double> EffectiveSuppression => (input) =>
        {
            Offense offense = input.weapon.Offense;
            Defense defense = input.defender.Defense;

            double final_accuracy = offense.Accuracy.Suppress.ApplyMod() + offense.Accuracy.Deviation.ApplyDeviation();
            double suppress = offense.Suppression.ApplyMod() * final_accuracy;
            int round = input.defender.ConsecutiveSuppressedRound;
            double determinant = -round * (round - 2 / suppress);

            return determinant > 0 ? 1.1 * (1 - 1 / Math.PI * (Math.Acos(suppress * round - 1) - suppress * Math.Sqrt(determinant))) : 1.1;
        };
        public static Func<IOffensiveCustomizable, double> DamageAgainstBuilding => (weapon) => weapon.Offense.Damage.Destruction * weapon.Offense.Damage.Deviation.ApplyDeviation();
        public static Func<(Unit observer, Unit observee, double distance), bool> VisualSpotting => (input) =>
        {
            Scouting observer_scouting = input.observer.Scouting;

            if (input.distance > observer_scouting.Reconnaissance || observer_scouting.Reconnaissance <= 0.5)
            {
                return false;
            }
            double determinant = 2 * observer_scouting.Reconnaissance / input.distance - 1;
            if (determinant <= 0)
            {
                return false;
            }
            double detection_at_range = observer_scouting.Detection / Math.Log(2 * observer_scouting.Reconnaissance - 1) * Math.Log(determinant);
            return detection_at_range > input.observee.Scouting.Concealment;
        };
        // TODO so far the same as visual spotting for units, tweak a bit in the future
        public static Func<(Unit observer, Building observee, double distance), bool> VisualSpottingForBuildings => (input) =>
        {
            Scouting observer_scouting = input.observer.Scouting;

            if (input.distance > observer_scouting.Reconnaissance || observer_scouting.Reconnaissance <= 0.5)
            {
                return false;
            }
            double determinant = 2 * observer_scouting.Reconnaissance / input.distance - 1;
            if (determinant <= 0)
            {
                return false;
            }
            double detection_at_range = observer_scouting.Detection / Math.Log(2 * observer_scouting.Reconnaissance - 1) * Math.Log(determinant);
            return detection_at_range > input.observee.Scouting.Concealment;
        };
        public static Func<(Unit observer, Unit observee, double distance), bool> AcousticRanging => (input) =>
        {
            // TODO
            return false;
        };
    }
}
