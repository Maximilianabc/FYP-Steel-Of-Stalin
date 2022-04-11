using SteelOfStalin.Attributes;
using SteelOfStalin.Assets;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules.Guns;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.Assets.Customizables.Shells;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.Flow;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Assets.Props.Units.Sea;
using SteelOfStalin.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;
using static SteelOfStalin.Util.Utilities;
using static SteelOfStalin.DataIO.DataUtilities;
using Attribute = SteelOfStalin.Attributes.Attribute;
using Plane = SteelOfStalin.Assets.Props.Units.Air.Plane;
using Module = SteelOfStalin.Assets.Customizables.Module;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.Assets.Props.Units.Land.Artilleries;
using SteelOfStalin.Assets.Props.Units.Land.Vehicles;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Assets.Props.Buildings.Infrastructures;
using Unity.Netcode;
using System.Text;

namespace SteelOfStalin.Util
{
    public static class Utilities
    {
        public static void Log(this Command command, string @event) => Debug.Log($"{command.Unit} has the following event when executing the command {command.Name}: {@event}");
        public static void LogWarning(this INamedAsset asset, string reason, [CallerMemberName] string method_name = "") => Debug.LogWarning($"Warning while executing method {method_name} for {asset}: {reason}");
        public static void LogError(this Command command, string reason, string explanation = "") => Debug.LogError($"Failed to execute command {command.Name} for unit at {command.Unit}: {reason} {explanation}");
        public static void LogError(this INamedAsset asset, string reason, [CallerMemberName] string method_name = "") => Debug.LogError($"Failed to execute method {method_name} for {asset}: {reason}");
        public static void LogError(this Phase phase, string reason) => Debug.LogError($"Error when executing phases {phase.GetType().Name}: {reason}");

        public static IEnumerable<T> Flatten<T>(this T[][] ts) => ts.SelectMany(t => t);
        public static IEnumerable<T> CombineAll<T>(params IEnumerable<T>[] sources) => sources.SelectMany(t => t);
        public static IEnumerable<T> CombineAll<T>(params T[] sources) => sources;
        public static T Find<T>(this IEnumerable<T> source, Predicate<T> predicate) => source.Where(t => predicate(t)).FirstOrDefault();
        public static T[][] Transpose<T>(this T[][] ts)
        {
            T[][] transposed = new T[ts[0].Length][];
            for (int i = 0; i < transposed.Length; i++)
            {
                transposed[i] = new T[ts.Length];
                for (int j = 0; j < transposed[i].Length; j++)
                {
                    transposed[i][j] = ts[j][i];
                }
            }
            return transposed;
        }
        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            int index = 0;
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            foreach (T item in source)
            {
                if (comparer.Equals(item, value))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }
        public static T[] Slice<T>(this T[] source, int index, int length)
        {
            T[] sliced = new T[length];
            Array.Copy(source, index, sliced, 0, length);
            return sliced;
        }

        public static bool HasAnyOfFlags<T>(this T @enum, T group) where T : Enum => (Convert.ToInt64(@enum) & Convert.ToInt64(group)) != 0;

        public static Func<T, bool> AndAll<T>(this Func<T, bool>[] predicates) => (t) =>
        {
            foreach (Func<T, bool> predicate in predicates)
            {
                if (!predicate(t))
                {
                    return false;
                }
            }
            return true;
        };

        public static bool IsNonInteger(this Type type) => type.IsPrimitive && (type == typeof(decimal) || type == typeof(decimal) || type == typeof(float));

        public static T CloneNew<T>(this List<T> list, string name) where T : ICloneable, INamedAsset => (T)list.Find(a => a.Name == name)?.Clone() ?? throw new ArgumentException($"There is no {typeof(T).Name.ToLower()} with name {name}");

        public static GameObject GetChildByName(this GameObject parent, string child_name)
        {
            GameObject child_object = null;
            Transform transform = parent.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child_object = string.Equals(child.name, child_name, StringComparison.OrdinalIgnoreCase) ? child.gameObject : child.gameObject.GetChildByName(child_name);
                if (child_object != null)
                {
                    break;
                }
            }
            return child_object;
        }
        public static T GetComponentInSpecificChild<T>(this GameObject parent, string child_name) where T : Component => parent.GetChildByName(child_name)?.GetComponent<T>();

        public static decimal RandomBetweenSymmetricRange(decimal range) => range * (decimal)(new System.Random().NextDouble() * 2 - 1);
        public static string ToPascal(this string input) => Regex.Replace(input, @"(^[a-z])|[_ ]([a-z])", m => m.Value.ToUpper()).Replace("_", "").Replace(" ", "");
        public static string ToSnake(this string input) => Regex.Replace(input, @"(?<!^)([A-Z][a-z]+)", "_$1").ToLower();
        public static string PrintMembers(object invoke_target)
        {
            if (invoke_target == null)
            {
                return "null";
            }

            IEnumerable<Type> attributes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace != null && t.Namespace.Contains("SteelOfStalin.Attributes"));
            Type type = invoke_target.GetType();
            string sep = Environment.NewLine;
            List<string> line = new List<string>() { type.Name };

            // skip members with index (like list, array etc.) for now, think of a way to deal with them later
            type.GetProperties().Where(p => p.GetIndexParameters().Length == 0).ToList().ForEach(p =>
            {
                object p_val = p.GetValue(invoke_target);
                if (p.PropertyType.IsPrimitive || p.PropertyType.IsEnum)
                {
                    line.Add($"{p.Name}\t\t:{p_val}");
                }
                else if (attributes.Contains(p.PropertyType))
                {
                    line.Add($"{p.Name}: " + (p_val != null ? PrintMembers(p_val) : "null"));
                }
            });
            return string.Join(sep, line);
        }
    }

    public static class Formula
    {
        public static Func<(IOffensiveCustomizable weapon, decimal distance), decimal> DamageDropoff => (input) =>
        {
            decimal d = input.weapon.Offense.Damage.Dropoff.ApplyMod();
            decimal r = 1 + 0.6M / Mathm.Pow(input.weapon.Offense.MaxRange.ApplyMod(), 2);
            return 1.4M / Mathm.PI * (Mathm.Acos(d * r * input.distance - 1) - d * r * Mathm.Sqrt(-input.distance * (input.distance - 2 / (d * r))));
        };
        public static Func<(Attribute soft, Attribute hard, decimal multiplier, Attribute hardness), decimal> DamageWithHardness => (input) =>
        {
            decimal soft_damage = input.soft * input.multiplier * (1 - input.hardness);
            decimal hard_damage = input.hard * input.multiplier * input.hardness;
            return soft_damage + hard_damage;
        };
        public static Func<(IOffensiveCustomizable weapon, Unit defender, decimal distance), decimal> DamageAgainstPersonnel => (input) =>
        {
            Accuracy accuracy = input.weapon.Offense.Accuracy;
            Damage damage = input.weapon.Offense.Damage;
            Defense defense = input.defender.Defense;

            decimal final_accuracy = accuracy.Normal.ApplyMod() + accuracy.Deviation.ApplyDeviation();
            decimal dropoff = input.weapon is Gun ? 1 : DamageDropoff((input.weapon, input.distance));
            decimal damage_multiplier = (1 + damage.Deviation.ApplyDeviation()) * (1 - defense.Evasion) * dropoff;
            return DamageWithHardness((damage.Soft, damage.Hard, damage_multiplier, defense.Hardness));
        };
        public static Func<(IOffensiveCustomizable weapon, Unit defender, decimal distance), decimal> DamageAgainstZeroResistance => (input) =>
        {
            Damage damage = input.weapon.Offense.Damage;
            Defense defense = input.defender.Defense;

            decimal dropoff = input.weapon is Gun ? 1 : DamageDropoff((input.weapon, input.distance));
            decimal damage_multiplier = 0.25M * (1 + damage.Deviation.ApplyDeviation()) * dropoff;
            return DamageWithHardness((damage.Soft, damage.Hard, damage_multiplier, defense.Hardness));
        };
        public static Func<(IOffensiveCustomizable weapon, Unit defender), decimal> EffectiveSuppression => (input) =>
        {
            Offense offense = input.weapon.Offense;
            Defense defense = input.defender.Defense;

            decimal final_accuracy = offense.Accuracy.Suppress.ApplyMod() + offense.Accuracy.Deviation.ApplyDeviation();
            decimal suppress = offense.Suppression.ApplyMod() * final_accuracy;
            int round = input.defender.ConsecutiveSuppressedRound;
            decimal determinant = -round * (round - 2 / suppress);

            return determinant > 0 ? 1.1M * (1 - 1 / Mathm.PI * Mathm.Acos((suppress * round - 1) - suppress * Mathm.Sqrt(determinant))) : 1.1M;
        };
        public static Func<IOffensiveCustomizable, decimal> DamageAgainstBuilding => (weapon) => weapon.Offense.Damage.Destruction * weapon.Offense.Damage.Deviation.ApplyDeviation();
        public static Func<(decimal observer_recon, decimal observer_detect, decimal observee_conceal, decimal distance), bool> VisualSpotting => (input) =>
        {
            if (input.distance > input.observer_recon || input.observer_recon <= 0.5M)
            {
                return false;
            }
            decimal determinant = 2 * input.observer_recon / input.distance - 1;
            if (determinant <= 0)
            {
                return false;
            }
            decimal detection_at_range = (decimal)(input.observer_detect / Mathm.Log(2 * input.observer_recon - 1) * Mathm.Log(determinant));
            return detection_at_range > input.observee_conceal;
        };
        public static Func<(Unit observer, Unit observee, decimal distance), bool> AcousticRanging => (input) =>
        {
            // TODO
            return false;
        };
    }

    public static class Mathm
    {
        public const decimal PI = (decimal)Math.PI;
        public static decimal Pow(decimal x, decimal y) => (decimal)Math.Pow((double)x, (double)y);
        public static decimal Log(decimal d) => (decimal)Math.Log((double)d);
        public static decimal Sqrt(decimal d) => (decimal)Math.Sqrt((double)d);
        public static decimal Acos(decimal d) => (decimal)Math.Acos((double)d);
    }
}

namespace SteelOfStalin.DataIO
{
    public abstract class Data<T> where T : INamedAsset, ICloneable
    {
        protected virtual string JsonFolderPath => $@"{ExternalFilePath}\Json";

        public T this[string name] => All.Find(a => a.Name == name);
        public T GetNew(string name) => (T)All.Find(a => a.Name == name)?.Clone() ?? throw new ArgumentException($"There is no {typeof(T).Name.ToLower()} with name {name}");
        public U GetNew<U>() where U : T => (U)All.OfType<U>().FirstOrDefault()?.Clone() ?? throw new ArgumentException($"There is no {typeof(T).Name.ToLower()} with type {typeof(U).Name.ToSnake()}");

        public abstract void Load();
        public abstract IEnumerable<T> All { get; }
        public abstract T GetNew<U>(string name) where U : INamedAsset;
        public virtual IEnumerable<T> FYPImplement => All;

        protected FileInfo[] GetJsonFiles(string path) => new DirectoryInfo(path).GetFiles("*.json", SearchOption.AllDirectories);
        protected void PrintEmptyListNames()
        {
            IEnumerable<PropertyInfo> lists = GetType().GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
            foreach (PropertyInfo info in lists)
            {
                object list = GetType().InvokeMember(info.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, this, null);
                int count = (int)list.GetType().GetProperty("Count").GetValue(list);
                if (count == 0)
                {
                    Debug.LogWarning($"{info.Name} is empty");
                }
            }
        }
    }

    public sealed class UnitData : Data<Unit>
    {
        protected override string JsonFolderPath => $@"{base.JsonFolderPath}\units";
        public List<Personnel> Personnels { get; set; } = new List<Personnel>();
        public List<Artillery> Artilleries { get; set; } = new List<Artillery>();
        public List<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public List<Vessel> Vessels { get; set; } = new List<Vessel>();
        public List<Plane> Planes { get; set; } = new List<Plane>();

        public override IEnumerable<Unit> All => CombineAll<Unit>(Personnels, Artilleries, Vehicles, Vessels, Planes);
        public override IEnumerable<Unit> FYPImplement => new List<Unit>()
        {
            GetNew<Militia>(),
            GetNew<Infantry>(),
            GetNew<Assault>(),
            GetNew<Engineer>(),
            GetNew<Mountain>(),
            GetNew<Support>(),
            GetNew<Portable>(),
            GetNew<DirectFire>(),
            GetNew<AntiTank>(),
            GetNew<HeavySupport>(),
            GetNew<SelfPropelled>(),
            GetNew<MotorisedInfantry>(),
            GetNew<Utility>(),
            GetNew<Carrier>(),
            GetNew<ArmouredCar>(),
            GetNew<TankDestroyer>(),
            GetNew<AssaultGun>(),
            GetNew<LightTank>(),
            GetNew<MediumTank>(),
            GetNew<HeavyTank>()
        };

        public override Unit GetNew<U>(string name) => typeof(U) switch
        {
            _ when typeof(U) == typeof(Personnel) || typeof(U).IsSubclassOf(typeof(Personnel)) => Personnels.CloneNew(name),
            _ when typeof(U) == typeof(Artillery) || typeof(U).IsSubclassOf(typeof(Artillery)) => Artilleries.CloneNew(name),
            _ when typeof(U) == typeof(Vehicle) || typeof(U).IsSubclassOf(typeof(Vehicle)) => Vehicles.CloneNew(name),
            _ when typeof(U) == typeof(Vessel) || typeof(U).IsSubclassOf(typeof(Vessel)) => Vessels.CloneNew(name),
            _ when typeof(U) == typeof(Plane) || typeof(U).IsSubclassOf(typeof(Plane)) => Planes.CloneNew(name),
            _ => throw new ArgumentException($"There is no {typeof(U).Name.ToLower()} in type Unit")
        };

        public override void Load()
        {
            foreach (FileInfo f in GetJsonFiles(JsonFolderPath))
            {
                string json = File.ReadAllText(f.FullName);
                Unit u = JsonSerializer.Deserialize<Unit>(json, Options);
                if (u is Personnel p)
                {
                    Personnels.Add(p);
                }
                else if (u is Artillery a)
                {
                    Artilleries.Add(a);
                }
                else if (u is Vehicle v)
                {
                    Vehicles.Add(v);
                }
                else if (u is Vessel e)
                {
                    Vessels.Add(e);
                }
                else if (u is Plane l)
                {
                    Planes.Add(l);
                }
                else
                {
                    Debug.LogError($"Unknown unit type {u.GetType().Name}");
                }
            }
            PrintEmptyListNames();
            Debug.Log("All units data loaded.");
        }
    }

    public sealed class BuildingData : Data<Building>
    {
        protected override string JsonFolderPath => $@"{base.JsonFolderPath}\buildings";
        public List<UnitBuilding> Units { get; set; } = new List<UnitBuilding>();
        public List<ProductionBuilding> Productions { get; set; } = new List<ProductionBuilding>();
        public List<Infrastructure> Infrastructures { get; set; } = new List<Infrastructure>();
        public List<TransmissionBuilding> Transmissions { get; set; } = new List<TransmissionBuilding>();
        public List<DefensiveBuilding> Defensives { get; set; } = new List<DefensiveBuilding>();

        public override IEnumerable<Building> All => CombineAll<Building>(Units, Productions, Infrastructures, Transmissions, Defensives);
        public override IEnumerable<Building> FYPImplement => new List<Building>()
        {
            GetNew<Barracks>(),
            GetNew<Arsenal>(),
            GetNew<Outpost>(),
            GetNew<Bridge>(),
        };

        public override Building GetNew<U>(string name) => typeof(U) switch 
        { 
            _ when typeof(U) == typeof(UnitBuilding) || typeof(U).IsSubclassOf(typeof(UnitBuilding)) => Units.CloneNew(name),
            _ when typeof(U) == typeof(ProductionBuilding) || typeof(U).IsSubclassOf(typeof(ProductionBuilding)) => Productions.CloneNew(name),
            _ when typeof(U) == typeof(Infrastructure) || typeof(U).IsSubclassOf(typeof(Infrastructure)) => Infrastructures.CloneNew(name),
            _ when typeof(U) == typeof(TransmissionBuilding) || typeof(U).IsSubclassOf(typeof(TransmissionBuilding)) => Transmissions.CloneNew(name),
            _ when typeof(U) == typeof(DefensiveBuilding) || typeof(U).IsSubclassOf(typeof(DefensiveBuilding)) => Defensives.CloneNew(name),
            _ => throw new ArgumentException($"There is no sub-type {typeof(U).Name.ToLower()} in type Building")
        };

        public override void Load()
        {
            foreach (FileInfo f in GetJsonFiles(JsonFolderPath))
            {
                string json = File.ReadAllText(f.FullName);
                object b = JsonSerializer.Deserialize<Building>(json, Options);
                if (b is UnitBuilding u)
                {
                    Units.Add(u);
                }
                else if (b is ProductionBuilding r)
                {
                    Productions.Add(r);
                }
                else if (b is Infrastructure i)
                {
                    Infrastructures.Add(i);
                }
                else if (b is TransmissionBuilding t)
                {
                    Transmissions.Add(t);
                }
                else if (b is DefensiveBuilding d)
                {
                    Defensives.Add(d);
                }
                else
                {
                    Debug.LogError($"Unknown building type {b.GetType().Name}");
                }
            }
            PrintEmptyListNames();
            Debug.Log("All buildings data loaded.");
        }
    }

    public sealed class TileData : Data<Tile>
    {
        protected override string JsonFolderPath => $@"{base.JsonFolderPath}\tiles";

        public List<Tile> Terrains { get; set; } = new List<Tile>();
        public List<Cities> Cities { get; set; } = new List<Cities>();

        public override IEnumerable<Tile> All => CombineAll<Tile>(Terrains, Cities);

        public Tile GetNew(TileType type) => (Tile)Terrains.Find(t => t.Type == type)?.Clone() ?? throw new ArgumentException($"There is no tiles with type {type}");

        public override Tile GetNew<U>(string name) => typeof(U) switch
        {
            _ when typeof(U) == typeof(Cities) || typeof(U).IsSubclassOf(typeof(Cities)) => Cities.CloneNew(name),
            _ => throw new ArgumentException($"There is no sub-type with name {typeof(U).Name.ToLower()} in type Tile")
        };

        public override void Load()
        {
            foreach (FileInfo f in GetJsonFiles(JsonFolderPath))
            {
                string json = File.ReadAllText(f.FullName);
                object t = JsonSerializer.Deserialize<Tile>(json, Options);
                if (t.GetType().IsSubclassOf(typeof(Cities)))
                {
                    Cities.Add((Cities)t);
                }
                else if (t is Tile tile)
                {
                    Terrains.Add(tile);
                }
                else
                {
                    Debug.LogError($"Unknown tile type {t.GetType().Name}");
                }
            }
            PrintEmptyListNames();
            Debug.Log("All tiles data loaded.");
        }
    }

    public sealed class CustomizableData : Data<Customizable>
    {
        protected override string JsonFolderPath => $@"{base.JsonFolderPath}\customizables";
        public List<Firearm> Firearms { get; set; } = new List<Firearm>();
        public ModuleData Modules { get; set; } = new ModuleData();
        public List<Shell> Shells { get; set; } = new List<Shell>();

        public override IEnumerable<Customizable> All => CombineAll<Customizable>(Firearms, Modules.All, Shells);

        public Firearm GetNewFirearm(string name) => (Firearm)Firearms.Find(f => f.Name == name)?.Clone() ?? throw new ArgumentException($"There is no firearms named {name}");
        public Module GetNewModule(string name) => (Module)Modules.All.Find(m => m.Name == name)?.Clone() ?? throw new ArgumentException($"There is no modules named {name}");
        public Shell GetNewShell(string name) => (Shell)Shells.Find(s => s.Name == name)?.Clone() ?? throw new ArgumentException($"There is no shells named {name}");

        public override Customizable GetNew<U>(string name) => typeof(U) switch
        {
            _ when typeof(U).IsSubclassOf(typeof(Module)) => Modules.GetNew<U>(name),
            _ when typeof(U) == typeof(Module) => Modules.GetNew(name),
            _ when typeof(U) == typeof(Firearm) || typeof(U).IsSubclassOf(typeof(Firearm)) => Firearms.CloneNew(name),
            _ when typeof(U) == typeof(Shell) || typeof(U).IsSubclassOf(typeof(Shell)) => Shells.CloneNew(name),
            _ => throw new ArgumentException($"There is no sub-type with name {typeof(U).Name.ToLower()} in type Customizable")
        };

        public override void Load()
        {
            foreach (FileInfo f in GetJsonFiles(JsonFolderPath))
            {
                string json = File.ReadAllText(f.FullName); 
                Customizable c = JsonSerializer.Deserialize<Customizable>(json, Options);
                if (c is Module m)
                {
                    Modules.Add(m);
                }
                else if (c is Firearm fire)
                {
                    Firearms.Add(fire);
                }
                else if (c is Shell s)
                {
                    Shells.Add(s);
                }
                else
                {
                    Debug.LogError($"Unknown customizable type {c.GetType().Name}");
                }
            }
            PrintEmptyListNames();
            Modules.Load();
            Debug.Log("All customizables data loaded.");
        }
    }

    public sealed class ModuleData : Data<Module>
    {
        protected override string JsonFolderPath => $@"{base.JsonFolderPath}\customizables\modules";

        public GunData Guns { get; set; } = new GunData();
        public List<HeavyMachineGun> HeavyMachineGuns { get; set; } = new List<HeavyMachineGun>();
        public List<Engine> Engines { get; set; } = new List<Engine>();
        public List<Suspension> Suspensions { get; set; } = new List<Suspension>();
        public List<Radio> Radios { get; set; } = new List<Radio>();
        public List<Periscope> Periscopes { get; set; } = new List<Periscope>();
        public List<FuelTank> FuelTanks { get; set; } = new List<FuelTank>();
        public List<AmmoRack> AmmoRacks { get; set; } = new List<AmmoRack>();
        public List<TorpedoTubes> TorpedoTubes { get; set; } = new List<TorpedoTubes>();
        public List<Sonar> Sonars { get; set; } = new List<Sonar>();
        public List<Propeller> Propellers { get; set; } = new List<Propeller>();
        public List<Rudder> Rudders { get; set; } = new List<Rudder>();
        public List<Wings> Wings { get; set; } = new List<Wings>();
        public List<LandingGear> LandingGears { get; set; } = new List<LandingGear>();
        public List<Radar> Radars { get; set; } = new List<Radar>();

        public override IEnumerable<Module> All => CombineAll<Module>(Guns.All, HeavyMachineGuns, Engines, Suspensions, Radios, Periscopes, FuelTanks, AmmoRacks, TorpedoTubes, Sonars, Propellers, Rudders, Wings, LandingGears, Radars);

        public override Module GetNew<U>(string name) => typeof(U) switch
        {
            _ when typeof(U).IsSubclassOf(typeof(Gun)) => Guns.GetNew<U>(name),
            _ when typeof(U) == typeof(Gun) => Guns.GetNew(name),
            _ when typeof(U) == typeof(HeavyMachineGun) || typeof(U).IsSubclassOf(typeof(HeavyMachineGun)) => HeavyMachineGuns.CloneNew(name),
            _ when typeof(U) == typeof(Engine) || typeof(U).IsSubclassOf(typeof(Engine)) => Engines.CloneNew(name),
            _ when typeof(U) == typeof(Suspension) || typeof(U).IsSubclassOf(typeof(Suspension)) => Suspensions.CloneNew(name),
            _ when typeof(U) == typeof(Radio) || typeof(U).IsSubclassOf(typeof(Radio)) => Radios.CloneNew(name),
            _ when typeof(U) == typeof(Periscope) || typeof(U).IsSubclassOf(typeof(Periscope)) => Periscopes.CloneNew(name),
            _ when typeof(U) == typeof(FuelTank) || typeof(U).IsSubclassOf(typeof(FuelTank)) => FuelTanks.CloneNew(name),
            _ when typeof(U) == typeof(AmmoRack) || typeof(U).IsSubclassOf(typeof(AmmoRack)) => AmmoRacks.CloneNew(name),
            _ when typeof(U) == typeof(TorpedoTubes) || typeof(U).IsSubclassOf(typeof(TorpedoTubes)) => TorpedoTubes.CloneNew(name),
            _ when typeof(U) == typeof(Sonar) || typeof(U).IsSubclassOf(typeof(Sonar)) => Sonars.CloneNew(name),
            _ when typeof(U) == typeof(Propeller) || typeof(U).IsSubclassOf(typeof(Propeller)) => Propellers.CloneNew(name),
            _ when typeof(U) == typeof(Rudder) || typeof(U).IsSubclassOf(typeof(Rudder)) => Rudders.CloneNew(name),
            _ when typeof(U) == typeof(Wings) || typeof(U).IsSubclassOf(typeof(Wings)) => Wings.CloneNew(name),
            _ when typeof(U) == typeof(LandingGear) || typeof(U).IsSubclassOf(typeof(LandingGear)) => LandingGears.CloneNew(name),
            _ when typeof(U) == typeof(Radar) || typeof(U).IsSubclassOf(typeof(Radar)) => Radars.CloneNew(name),
            _ => throw new ArgumentException($"There is no sub-type with name {typeof(U).Name.ToLower()} in type Module")
        };

        public override void Load()
        {
            PrintEmptyListNames();
            Guns.Load();
        }

        public void Add(Module m)
        {
            if (m is Gun g)
            {
                Guns.Add(g);
            }
            else if (m is HeavyMachineGun h)
            {
                HeavyMachineGuns.Add(h);
            }
            else if (m is Engine e)
            {
                Engines.Add(e);
            }
            else if (m is Suspension s)
            {
                Suspensions.Add(s);
            }
            else if (m is Radio r)
            {
                Radios.Add(r);
            }
            else if (m is Periscope p)
            {
                Periscopes.Add(p);
            }
            else if (m is FuelTank f)
            {
                FuelTanks.Add(f);
            }
            else if (m is AmmoRack a)
            {
                AmmoRacks.Add(a);
            }
            else if (m is TorpedoTubes t)
            {
                TorpedoTubes.Add(t);
            }
            else if (m is Sonar so)
            {
                Sonars.Add(so);
            }
            else if (m is Propeller pr)
            {
                Propellers.Add(pr);
            }
            else if (m is Rudder ru)
            {
                Rudders.Add(ru);
            }
            else if (m is Wings w)
            {
                Wings.Add(w);
            }
            else if (m is LandingGear l)
            {
                LandingGears.Add(l);
            }
            else if (m is Radar ra)
            {
                Radars.Add(ra);
            }
            else
            {
                Debug.LogError($"Unknown module type {m.GetType().Name}");
            }
        }
    }

    public sealed class GunData : Data<Gun>
    {
        public List<Cannon> Cannons { get; set; } = new List<Cannon>();
        public List<Howitzer> Howitzers { get; set; } = new List<Howitzer>();
        public List<AutoCannon> AutoCannons { get; set; } = new List<AutoCannon>();

        public override IEnumerable<Gun> All => CombineAll<Gun>(Cannons, Howitzers, AutoCannons);

        public override Gun GetNew<U>(string name) => typeof(U) switch
        {
            _ when typeof(U) == typeof(Cannon) || typeof(U).IsSubclassOf(typeof(Cannon)) => Cannons.CloneNew(name),
            _ when typeof(U) == typeof(Howitzer) || typeof(U).IsSubclassOf(typeof(Howitzer)) => Howitzers.CloneNew(name),
            _ when typeof(U) == typeof(AutoCannon) || typeof(U).IsSubclassOf(typeof(AutoCannon)) => AutoCannons.CloneNew(name),
            _ => throw new ArgumentException($"There is no sub-type with name {typeof(U).Name.ToLower()} in type Gun")
        };

        public override void Load() => PrintEmptyListNames();

        public void Add(Gun g)
        {
            if (g is Cannon c)
            {
                Cannons.Add(c);
            }
            else if (g is Howitzer h)
            {
                Howitzers.Add(h);
            }
            else if (g is AutoCannon a)
            {
                AutoCannons.Add(a);
            }
            else
            {
                Debug.LogError($"Unknown gun type {g.GetType().Name}");
            }
        }
    }

    public static class DataUtilities
    {
        public static JsonSerializerOptions Options => new JsonSerializerOptions()
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
            IgnoreReadOnlyFields = true,
            // Converters = { new RoundingJsonConverter() }
        };
        public static string ExternalFilePath => ConvertToWindowsPath(Application.streamingAssetsPath);

        public static string ConvertToWindowsPath(string path) => path.Replace("/", @"\");
        public static void CreateStreamingAssetsFolder(string path) => Directory.CreateDirectory($@"{ExternalFilePath}\{path}");

        public static void SerializeJson<T>(this T input, string path) => File.WriteAllText($@"{ExternalFilePath}\{path}.json", JsonSerializer.Serialize<T>(input, Options));
        public static T DeserializeJson<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText($@"{ExternalFilePath}\{path}.json"), Options);
        public static IEnumerable<T> DeserializeJsonWithAbstractType<T>(string path, string identifier_property_name, string base_type_name)
        {
            object deserialized = DeserializeJson<object>(path);
            if (deserialized is JsonElement j)
            {
                if (j.GetArrayLength() > 0)
                {
                    IEnumerator enumerator = j.EnumerateArray().GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current is JsonElement js)
                        {
                            if (js.TryGetProperty(identifier_property_name, out JsonElement name))
                            {
                                string tile_name = name.GetString();
                                string proper_type_name = $"{base_type_name}.{tile_name.ToPascal()}";
                                Type tile_type = Type.GetType(proper_type_name);
                                yield return (T)js.Deserialize(tile_type, Options);
                            }
                            else
                            {
                                Debug.LogError($"Cannot get property {identifier_property_name} of current JsonElement");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("Deserialized object is not an array or is empty.");
                    yield return default;
                }
            }
        }

        public static void SaveToTxt(string path, string content) => File.WriteAllText($@"{ExternalFilePath}\{path}.txt", content);
        public static string[] ReadTxt(string path) => File.ReadAllLines($@"{ExternalFilePath}\{path}.txt");
        public static void SaveToPng(string path, Texture2D texture)
        {
            byte[] bs = texture.EncodeToPNG();
            using FileStream fs = new FileStream($@"{ExternalFilePath}\{path}.png", FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(bs, 0, bs.Length);
            fs.Close();
        }

        public static bool StreamingAssetExists(string path) => File.Exists($@"{ExternalFilePath}\{path}");

        public static DataChunk[] MakeChunks<T>(this T data)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data);
            int num_chunks = (int)Math.Ceiling((float)bytes.Length / DataChunk.CHUNK_SIZE);
            if (num_chunks > ushort.MaxValue)
            {
                Debug.LogError($"number of chunks needed ({num_chunks}) exceeds 65535");
                return null;
            }

            DataChunk[] chunks = new DataChunk[num_chunks];
            for (int i = 0; i < num_chunks; i++)
            {
                int current_index = DataChunk.CHUNK_SIZE * i;
                chunks[i] = new DataChunk()
                {
                    Order = (ushort)i,
                    Data = bytes.Slice(current_index, bytes.Length - current_index < DataChunk.CHUNK_SIZE ? bytes.Length - current_index : DataChunk.CHUNK_SIZE),
                };
            }
            return chunks;
        }

        public static object AssembleChunksIntoObject(this List<DataChunk> chunks, Type type)
        {
            byte[] data = chunks.OrderBy(d => d.Order).SelectMany(d => d.Data).ToArray();
            return JsonSerializer.Deserialize(data, type);
        }
    }

    public class DataChunk : INetworkSerializable
    {
        public const int CHUNK_SIZE = 1500; // FastBufferReader's buffer maximum is around 1500 bytes

        private ushort _order;
        public ushort Order { get => _order; set => _order = value; }

        private byte[] _data;
        public byte[] Data { get => _data; set => _data = value; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _order);
            serializer.SerializeValue(ref _data);
        }
    }

    public class RpcMessageObject
    {
        public List<DataChunk> Chunks { get; set; }
        public bool IsReady { get; set; }
    }

    public class NamedMessageObject
    {
        public ulong SenderID { get; set; }
        public NetworkMessageType MessageType { get; set; }
        public Type Type { get; set; }
        public string Message { get; set; }

        public object GetDeserializedObject() => JsonSerializer.Deserialize(Message, Type);
    }

    public enum NetworkMessageType
    {
        NONE,
        HANDSHAKE,
        DATA,
        COMMAND,
        CHAT
    }

    public class NetworkUtilities : NetworkBehaviour
    {
        private Dictionary<Type, RpcMessageObject> m_rpcMessages = new Dictionary<Type, RpcMessageObject>();
        private List<NamedMessageObject> m_namedMessages = new List<NamedMessageObject>();
        public Dictionary<NetworkMessageType, string> MessageNames => new Dictionary<NetworkMessageType, string>()
        {
            [NetworkMessageType.HANDSHAKE] = "handshake",
            [NetworkMessageType.DATA] = "data",
            [NetworkMessageType.COMMAND] = "command",
            [NetworkMessageType.CHAT] = "chat",
        };
        public static ClientRpcParams GetClientRpcSendParams(params ulong[] ids) 
            => NetworkManager.Singleton.IsServer ? new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new List<ulong>(ids) } } : default;

        private void Start()
        {
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.HANDSHAKE], (sender_id, reader) =>
            {
                reader.ReadValueSafe(out string message);
                CacheNamedMessage(NetworkMessageType.HANDSHAKE, sender_id, message);
            });
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.DATA], (sender_id, reader) =>
            {
                reader.ReadValueSafe(out string message);
                CacheNamedMessage(NetworkMessageType.DATA, sender_id, message);

            });
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.COMMAND], (sender_id, reader) =>
            {
                reader.ReadValueSafe(out string message);
                CacheNamedMessage(NetworkMessageType.COMMAND, sender_id, message);

            });
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.CHAT], (sender_id, reader) =>
            {
                reader.ReadValueSafe(out string message);
                CacheNamedMessage(NetworkMessageType.CHAT, sender_id, message);
            });
        }

        public void CacheClinetRpcMessage(string type_name, ushort num_to_receive, DataChunk chunk, bool isAppend = false)
        {
            Type type = Type.GetType(type_name);
            if (m_rpcMessages.ContainsKey(type))
            {
                if (m_rpcMessages[type].Chunks.Count < num_to_receive || isAppend)
                {
                    m_rpcMessages[type].Chunks.Add(chunk);
                }
                else
                {
                    Debug.LogError($"Number of chunks received exceed {num_to_receive}");
                }
            }
            else
            {
                m_rpcMessages.Add(type, new RpcMessageObject()
                { 
                    Chunks = new List<DataChunk>() { chunk },
                    IsReady = isAppend
                });
                Debug.Log($"New type {type_name} received.");
            }
            if (m_rpcMessages[type].Chunks.Count == num_to_receive && !isAppend)
            {
                m_rpcMessages[type].IsReady = true;
                Debug.Log($"Received all chunks ({num_to_receive} in total) for type {type}");
            }
        }

        public void CacheNamedMessage(NetworkMessageType message_type, ulong sender_id, string message)
        {
            Match match = Regex.Match(message, @"^([^:]+): (.*?)$");
            if (!match.Success)
            {
                Debug.Log(message);
                Debug.LogError($"Message is not in correct format");
                return;
            }
            m_namedMessages.Add(new NamedMessageObject()
            {
                SenderID = sender_id,
                MessageType = message_type,
                Type = Type.GetType(match.Groups[1].Value),
                Message = match.Groups[2].Value
            });
        }

        public void SendMessageByRpc<T>(T obj, bool from_host = true)
        {
            DataChunk[] object_chunks = obj.MakeChunks();
            /*
#pragma warning disable IDE0004 // The cast is neccessary for unity compiler unless newer version of unity (compiler) is used
            Action<DataChunk, ushort> rpc = from_host ? (Action<DataChunk, ushort>)ReceiveDataClientRpc<T> : ReceiveDataServerRpc<T>;
#pragma warning restore IDE0004
            */
            Action<DataChunk, ushort> rpc = typeof(T) switch
            {
                _ when typeof(T) == typeof(Map) && from_host => ReceiveMapInfoClientRpc,
                _ when typeof(T) == typeof(Tile[][]) && from_host => ReceiveMapTilesClientRpc,
                _ when typeof(T) == typeof(IEnumerable<Unit>) && from_host => ReceiveMapUnitsClientRpc,
                _ when typeof(T) == typeof(IEnumerable<Building>) && from_host => ReceiveMapBuildingsClientRpc,
                _ when typeof(T) == typeof(List<Player>) && from_host => ReceiveBattlePlayersClientRpc,
                _ when typeof(T) == typeof(BattleRules) && from_host => ReceiveBattleRulesClientRpc,
                _ => throw new NotImplementedException($"Sending message by rpc from " + (from_host ? "host" : "client") + $" with type {typeof(T).Name} is not supported at the moment")
            };
            foreach (DataChunk chunk in object_chunks)
            {
                rpc(chunk, (ushort)object_chunks.Length);
            }
        }

        public void SendNamedMessage<T>(T obj, ulong receiver_id, NetworkMessageType type)
        {
            string data = $"{typeof(T).FullName}: {JsonSerializer.Serialize(obj)}";
            int byte_length = Encoding.UTF8.GetByteCount(data);
            using FastBufferWriter writer = new FastBufferWriter(byte_length * 2 + 4, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(data);
            NetworkManager.CustomMessagingManager.SendNamedMessage(MessageNames[type], receiver_id, writer, NetworkDelivery.Reliable);
        }

        public object GetRpcMessage(Type type)
        {
            if (!m_rpcMessages.ContainsKey(type))
            {
                Debug.LogError($"No data with type {type} is found.");
                return null;
            }
            if (!m_rpcMessages[type].IsReady)
            {
                return null;
            }
            object obj = m_rpcMessages[type].Chunks.AssembleChunksIntoObject(type);
            m_rpcMessages.Remove(type);
            return obj;
        }

        public IEnumerator TryGetRpcMessage<T>(Action<T> callback)
        {
            int counter = 0;
            object message = GetRpcMessage(typeof(T));
            while (message == null && counter < 20)
            {
                message = GetRpcMessage(typeof(T));
                yield return new WaitForSeconds(2);
                counter++;
            }
            if (message == null)
            {
                Debug.LogError("Failed to retrieve cached rpc message after 20 tries");
                yield return null;
            }
            else
            {
                callback?.Invoke((T)message);
            }
        }

        public IEnumerable<object> GetNamedMessages(Predicate<NamedMessageObject> predicate)
        {
            IEnumerable<NamedMessageObject> targets = m_namedMessages.Where(m => predicate(m));
            IEnumerable<object> deserialized = targets.Select(m => m.GetDeserializedObject());
            Debug.Log(m_namedMessages.Count);
            m_namedMessages = m_namedMessages.Except(targets).ToList();
            Debug.Log(m_namedMessages.Count);
            return deserialized;
        }

        public IEnumerator TryGetNamedMessage<T>(Predicate<NamedMessageObject> predicate, Action<T> callback)
        {
            int counter = 0;
            IEnumerable<object> messages = GetNamedMessages(m => m.Type == typeof(T) && predicate(m));
            while (messages.Count() == 0 && counter < 20)
            {
                messages = GetNamedMessages(m => m.Type == typeof(T) && predicate(m));
                yield return new WaitForSeconds(2);
                counter++;
            }
            if (messages == null)
            {
                Debug.LogError("Failed to retrieve named message after 20 tries");
                yield return null;
            }
            else
            {
                foreach (object message in messages)
                {
                    callback?.Invoke((T)message);
                }
            }
        }

        /* UNITY FUCKING CRASHED ON CALLING A GENERIC RPC AND IT IS A FUCKING BUG
         * REFERNCE: https://forum.unity.com/threads/unity-crashes-on-rpc-call.1256361/
        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveDataClientRpc<T>(DataChunk chunk, ushort num_to_receive)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CachePackets(typeof(T).FullName, num_to_receive, chunk);
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveDataServerRpc<T>(DataChunk chunk, ushort num_to_receive)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                CachePackets(typeof(T).FullName, num_to_receive, chunk);
            }
        }*/

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapInfoClientRpc(DataChunk chunk, ushort num_to_receive)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CacheClinetRpcMessage(typeof(Map).FullName, num_to_receive, chunk);
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapTilesClientRpc(DataChunk chunk, ushort num_to_receive)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CacheClinetRpcMessage(typeof(Tile[][]).FullName, num_to_receive, chunk);
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapUnitsClientRpc(DataChunk chunk, ushort num_to_receive)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CacheClinetRpcMessage(typeof(IEnumerable<Unit>).FullName, num_to_receive, chunk);
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapBuildingsClientRpc(DataChunk chunk, ushort num_to_receive)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CacheClinetRpcMessage(typeof(IEnumerable<Building>).FullName, num_to_receive, chunk);
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveBattlePlayersClientRpc(DataChunk chunk, ushort num_to_receive)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CacheClinetRpcMessage(typeof(List<Player>).FullName, num_to_receive, chunk);
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveBattleRulesClientRpc(DataChunk chunk, ushort num_to_receive)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CacheClinetRpcMessage(typeof(BattleRules).FullName, num_to_receive, chunk);
            }
        }
    }
}
