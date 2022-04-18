using SteelOfStalin.Attributes;
using SteelOfStalin.Assets;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules.Guns;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.Assets.Customizables.Shells;
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
using static Unity.Netcode.CustomMessagingManager;

namespace SteelOfStalin.Util
{
    public static class Utilities
    {
        public static class Random
        {
            private static System.Random m_random = new System.Random();

            public static int Next() => m_random.Next();
            public static int Next(int max) => m_random.Next(max); 
            public static int Next(int min, int max) => m_random.Next(min, max);
            public static double NextDouble() => m_random.NextDouble();
            public static T NextItem<T>(IEnumerable<T> source) => source.ElementAt(m_random.Next(source.Count()));
        }
        public static List<Color> CommonColors => new List<Color>()
        {
            Color.red,
            Color.yellow,
            Color.green, // this one is lime
            Color.blue,
            new Color(0, 0.5F, 0, 1), // this one is green
            new Color(1, 0.5F, 0, 1), // orange
            new Color(0, 1, 1, 1), // cyan
            new Color(0.5F, 0, 1, 1), // purple
        };

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

        public static decimal RandomBetweenSymmetricRange(decimal range) => range * (decimal)(Random.NextDouble() * 2 - 1);
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
            int round = input.defender.ConsecutiveSuppressedRound + 1;
            decimal determinant = -round * (round - 2 / suppress);
            decimal acos = suppress * round - 1;

            if (acos < -1 || acos > 1)
            {
                Debug.LogError($"Acos value {acos} in EffectiveSuppression is out of range");
                return 0;
            }

            return determinant > 0 ? 1.1M * (1 - 1 / Mathm.PI * Mathm.Acos(acos) - suppress * Mathm.Sqrt(determinant)) : 1.1M;
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
        protected virtual string JsonFolderPath => JsonFolder;

        public T this[string name] => All.Find(a => a.Name == name);
        public T GetNew(string name) => (T)All.Find(a => a.Name == name)?.Clone() ?? throw new ArgumentException($"There is no {typeof(T).Name.ToLower()} with name {name}");
        public U GetNew<U>() where U : T => (U)All.OfType<U>().FirstOrDefault()?.Clone() ?? throw new ArgumentException($"There is no {typeof(T).Name.ToLower()} with type {typeof(U).Name.ToSnake()}");

        public abstract void Load(bool from_dump = false);
        public abstract void Clear();
        public abstract IEnumerable<T> All { get; }
        public abstract T GetNew<U>(string name) where U : INamedAsset;
        public virtual IEnumerable<T> FYPImplement => All;

        public List<string> LocalJsonFilePaths { get; set; } = new List<string>();
        // TODO FUT. Impl. add the path only if it's a valid data json
        protected virtual void AddLocalJsonPath(string file_full_name, bool from_dump)
        {
            if (!from_dump)
            {
                // fucking no Path.GetRelativePath in netstandard 2.0 zzz
                LocalJsonFilePaths.Add(ToRelativePath(file_full_name));
            }
        }

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
        protected override string JsonFolderPath => AppendPath(base.JsonFolderPath, "units");
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

        public override void Load(bool from_dump = false)
        {
            string folder = from_dump ? ToDumpPath(JsonFolderPath) : JsonFolderPath;
            foreach (FileInfo f in GetJsonFiles(folder))
            {
                string json = File.ReadAllText(f.FullName);
                AddLocalJsonPath(f.FullName, from_dump);

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
            Debug.Log("All units data loaded" + (from_dump ? " from dump" : ""));
        }

        public override void Clear()
        {
            Personnels.Clear();
            Artilleries.Clear();
            Vehicles.Clear();
            Vessels.Clear();
            Planes.Clear();
        }
    }

    public sealed class BuildingData : Data<Building>
    {
        protected override string JsonFolderPath => AppendPath(base.JsonFolderPath, "buildings");
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

        public override void Load(bool from_dump = false)
        {
            foreach (FileInfo f in GetJsonFiles(JsonFolderPath))
            {
                string json = File.ReadAllText(f.FullName);
                AddLocalJsonPath(f.FullName, from_dump);

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
            Debug.Log("All buildings data loaded" + (from_dump ? " from dump" : ""));
        }

        public override void Clear()
        {
            Units.Clear();
            Productions.Clear();
            Infrastructures.Clear();
            Transmissions.Clear();
            Defensives.Clear();
        }
    }

    public sealed class TileData : Data<Tile>
    {
        protected override string JsonFolderPath => AppendPath(base.JsonFolderPath, "tiles");

        public List<Tile> Terrains { get; set; } = new List<Tile>();
        public List<Cities> Cities { get; set; } = new List<Cities>();

        public override IEnumerable<Tile> All => CombineAll<Tile>(Terrains, Cities);

        public Tile GetNew(TileType type) => (Tile)Terrains.Find(t => t.Type == type)?.Clone() ?? throw new ArgumentException($"There is no tiles with type {type}");

        public override Tile GetNew<U>(string name) => typeof(U) switch
        {
            _ when typeof(U) == typeof(Cities) || typeof(U).IsSubclassOf(typeof(Cities)) => Cities.CloneNew(name),
            _ => throw new ArgumentException($"There is no sub-type with name {typeof(U).Name.ToLower()} in type Tile")
        };

        public override void Load(bool from_dump = false)
        {
            foreach (FileInfo f in GetJsonFiles(JsonFolderPath))
            {
                string json = File.ReadAllText(f.FullName);
                AddLocalJsonPath(f.FullName, from_dump);

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
            Debug.Log("All tiles data loaded" + (from_dump ? " from dump" : ""));
        }

        public override void Clear()
        {
            Terrains.Clear();
            Cities.Clear();
        }
    }

    public sealed class CustomizableData : Data<Customizable>
    {
        protected override string JsonFolderPath => AppendPath(base.JsonFolderPath, "customizables");
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
            _ when typeof(U) == typeof(IOffensiveCustomizable) => All.OfType<IOffensiveCustomizable>().Select(w => w as Customizable).ToList().CloneNew(name),
            _ => throw new ArgumentException($"There is no sub-type with name {typeof(U).Name.ToLower()} in type Customizable")
        };

        public override void Load(bool from_dump = false)
        {
            foreach (FileInfo f in GetJsonFiles(JsonFolderPath))
            {
                string json = File.ReadAllText(f.FullName);
                AddLocalJsonPath(f.FullName, from_dump);

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
            Debug.Log("All customizables data loaded" + (from_dump ? " from dump" : ""));
        }

        public override void Clear()
        {
            Firearms.Clear();
            Modules.Clear();
            Shells.Clear();
        }
    }

    public sealed class ModuleData : Data<Module>
    {
        protected override string JsonFolderPath => AppendPaths(base.JsonFolderPath, "customizables", "modules");

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

        public override void Load(bool from_dump = false)
        {
            PrintEmptyListNames();
            Guns.Load(from_dump);
        }

        public override void Clear()
        {
            Guns.Clear();
            HeavyMachineGuns.Clear();
            Engines.Clear();
            Suspensions.Clear();
            Radios.Clear();
            Periscopes.Clear();
            FuelTanks.Clear();
            AmmoRacks.Clear();
            TorpedoTubes.Clear();
            Sonars.Clear();
            Propellers.Clear();
            Rudders.Clear();
            Wings.Clear();
            LandingGears.Clear();
            Radars.Clear();
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

        public override void Load(bool from_dump = false) => PrintEmptyListNames();

        public override void Clear()
        {
            Cannons.Clear();
            Howitzers.Clear();
            AutoCannons.Clear();
        }

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

    public enum ExternalFolder
    {
        NONE,
        DUMP,
        SAVES,
        JSON
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
        public static string ExternalFolderRootPath => Application.streamingAssetsPath.Replace('/', path_sep);
        public static Func<string, string> ToRelativePath => full => full.Replace(ExternalFolderRootPath, ""); // with slash in front
        public static Func<string, string> AddDumpPath => path => @$"{FolderNames[ExternalFolder.DUMP]}{path}";
        public static Func<string, string> ToDumpPath => local => local.Replace(ExternalFolderRootPath, DumpFolder);

        public static char path_sep => Path.DirectorySeparatorChar;
        public static Dictionary<ExternalFolder, string> FolderNames => new Dictionary<ExternalFolder, string>()
        {
            [ExternalFolder.NONE] = "",
            [ExternalFolder.DUMP] = "Multiplayer_Dump",
            [ExternalFolder.SAVES] = "Saves",
            [ExternalFolder.JSON] = "Json",
        };
        public static string SavesFolder => GetFullPath(ExternalFolder.SAVES);
        public static string JsonFolder => GetFullPath(ExternalFolder.JSON);
        public static string DumpFolder => GetFullPath(ExternalFolder.DUMP);

        /// <summary>
        /// Get a path relative to <see cref="ExternalFolderRootPath"/> with sub-folders names provided
        /// </summary>
        /// <param name="base_folder">The base sub-folder in <see cref="ExternalFolderRootPath"/>, use none if <paramref name="sub_path_names"/> is directly relative to <see cref="ExternalFolderRootPath"/></param>
        /// <param name="sub_path_names">include the file name and extension at the end if this is a file path</param>
        /// <returns></returns>
        public static string GetRelativePath(ExternalFolder base_folder, params string[] sub_path_names) => Path.Combine(FolderNames[base_folder], Path.Combine(sub_path_names));

        /// <summary>
        /// Get the full path with sub-folders names provided
        /// </summary>
        /// <param name="base_folder">The base sub-folder in <see cref="ExternalFolderRootPath"/>, use none if <paramref name="sub_path_names"/> is directly relative to <see cref="ExternalFolderRootPath"/></param>
        /// <param name="sub_path_names">include the file name and extension at the end if this is a file path</param>
        /// <returns></returns>
        public static string GetFullPath(ExternalFolder base_folder = ExternalFolder.NONE, params string[] sub_path_names) => Path.Combine(ExternalFolderRootPath, GetRelativePath(base_folder, sub_path_names));

        /// <summary>
        /// Get the full file path with sub-folders names and file extension provided
        /// </summary>
        /// <param name="extension">The extension of the file</param>
        /// <param name="base_folder">The base sub-folder in <see cref="ExternalFolderRootPath"/>, use none if <paramref name="sub_path_names"/> is directly relative to <see cref="ExternalFolderRootPath"/></param>
        /// <param name="sub_path_names">The name of file must be included at the end</param>
        /// <returns></returns>
        public static string GetFullFilePath(string extension, ExternalFolder base_folder = ExternalFolder.NONE, params string[] sub_path_names) => $"{GetFullPath(base_folder, sub_path_names)}.{extension}";

        /// <summary>
        /// Append a path to an exisiting path
        /// </summary>
        /// <param name="original">The original path</param>
        /// <param name="append">The path which is appended to <paramref name="original"/></param>
        /// <returns></returns>
        public static string AppendPath(string original, string append) => Path.Combine(original, append);

        /// <summary>
        /// Append multiple paths to an exisiting path
        /// </summary>
        /// <param name="original">The original path</param>
        /// <param name="appends">The paths which are appended to <paramref name="original"/></param>
        /// <returns></returns>
        public static string AppendPaths(string original, params string[] appends) => Path.Combine(original, Path.Combine(appends));

        /// <summary>
        /// Prepend a path to an exisiting path
        /// </summary>
        /// <param name="original">The original path</param>
        /// <param name="prepend">The path which is prepended to <paramref name="original"/></param>
        /// <returns></returns>
        public static string PrependPath(string original, string prepend) => Path.Combine(prepend, original);

        public static void CreateStreamingAssetsFolder(string relative_path, ExternalFolder base_folder = ExternalFolder.NONE) => Directory.CreateDirectory(GetFullPath(base_folder, relative_path));

        public static void SerializeJson<T>(this T input, string relative_path, ExternalFolder base_folder = ExternalFolder.NONE) => File.WriteAllText(GetFullFilePath("json", base_folder, relative_path), JsonSerializer.Serialize<T>(input, Options));
        public static T DeserializeJson<T>(string path, bool is_relative = true, ExternalFolder base_folder = ExternalFolder.NONE) => JsonSerializer.Deserialize<T>(File.ReadAllText(is_relative ? GetFullFilePath("json", base_folder, path) : $"{path}.json"), Options);
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

        public static void SaveToTxt(string relative_path, string content, ExternalFolder base_folder = ExternalFolder.NONE) => File.WriteAllText(GetFullFilePath("txt", base_folder, relative_path), content);
        public static string[] ReadTxt(string relative_path, ExternalFolder base_folder = ExternalFolder.NONE) => File.ReadAllLines(GetFullFilePath("txt", base_folder, relative_path));
        public static void SaveToPng(string relative_path, Texture2D texture, ExternalFolder base_folder = ExternalFolder.NONE)
        {
            byte[] bs = texture.EncodeToPNG();
            using FileStream fs = new FileStream(GetFullFilePath("png", base_folder, relative_path), FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(bs, 0, bs.Length);
            fs.Close();
        }

        public static bool StreamingAssetExists(string relative_path, ExternalFolder base_folder = ExternalFolder.NONE) => File.Exists(GetFullPath(base_folder, relative_path));

        public static RpcMessageChunk[] MakeChunks(this byte[] bytes)
        {
            int num_chunks = (int)Math.Ceiling((float)bytes.Length / RpcMessageChunk.CHUNK_SIZE);
            if (num_chunks > ushort.MaxValue)
            {
                Debug.LogError($"number of chunks needed ({num_chunks}) exceeds 65535");
                return null;
            }

            RpcMessageChunk[] chunks = new RpcMessageChunk[num_chunks];
            for (int i = 0; i < num_chunks; i++)
            {
                int current_index = RpcMessageChunk.CHUNK_SIZE * i;
                chunks[i] = new RpcMessageChunk()
                {
                    Order = (ushort)i,
                    Data = bytes.Slice(current_index, bytes.Length - current_index < RpcMessageChunk.CHUNK_SIZE ? bytes.Length - current_index : RpcMessageChunk.CHUNK_SIZE),
                };
            }
            return chunks;
        }

        public static RpcMessageChunk[] MakeChunks<T>(this T data) => JsonSerializer.SerializeToUtf8Bytes(data).MakeChunks();

        public static byte[] AssembleChunks(this List<RpcMessageChunk> chunks) => chunks.OrderBy(d => d.Order).SelectMany(d => d.Data).ToArray();

        public static object AssembleChunksIntoObject(this List<RpcMessageChunk> chunks, Type type) => JsonSerializer.Deserialize(chunks.AssembleChunks(), type);

        /// <summary>
        /// Assemble chunks received from rpc and save it into json file.
        /// </summary>
        /// <param name="chunks">the list of chunks received</param>
        /// <param name="relative_path">the file path relative to streaming asset path</param>
        public static void AssembleChunksIntoFile(this List<RpcMessageChunk> chunks, string relative_path)
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(GetFullPath(sub_path_names: relative_path)));
            File.WriteAllBytes(GetFullPath(sub_path_names: relative_path), chunks.AssembleChunks());
        }
    }

    public class RpcMessageChunk : INetworkSerializable
    {
        public const int CHUNK_SIZE = 1290; // FastBufferReader's buffer maximum (1292) - sizeof(_order) = 1290

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
        public List<RpcMessageChunk> Chunks { get; set; }
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
        public static NetworkUtilities Instance { get; private set; }

        private Dictionary<Type, RpcMessageObject> m_rpcMessages = new Dictionary<Type, RpcMessageObject>();
        private List<NamedMessageObject> m_namedMessages = new List<NamedMessageObject>();
        private Dictionary<string, RpcMessageObject> m_files = new Dictionary<string, RpcMessageObject>();

        public Dictionary<NetworkMessageType, string> MessageNames => new Dictionary<NetworkMessageType, string>()
        {
            [NetworkMessageType.HANDSHAKE] = "handshake",
            [NetworkMessageType.DATA] = "data",
            [NetworkMessageType.COMMAND] = "command",
            [NetworkMessageType.CHAT] = "chat",
        };

        public static ClientRpcParams GetClientRpcSendParams(params ulong[] ids) 
            => Game.Network.IsServer && ids.Length != 0 ? new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new List<ulong>(ids) } } : default;
        public static ClientRpcParams GetClientRpcSendParams(IEnumerable<ulong> ids)
            => Game.Network.IsServer && ids.Count() != 0 ? new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new List<ulong>(ids) } } : default;
        public static ServerRpcParams GetServerRpcParams()
            => Game.Network.IsClient ? new ServerRpcParams() { Receive = new ServerRpcReceiveParams() { SenderClientId = Game.Network.LocalClientId } } : default;

        public static Dictionary<string, string> GetRelativePathsWithPattern(List<string> local_relative_paths, Func<string, string> replacer)
            => local_relative_paths.Zip(local_relative_paths.Select(p => replacer(p)), (local, dest) => new { local, dest }).ToDictionary(ps => ps.local, ps => ps.dest);
        public static Dictionary<string, string> GetDumpPaths(List<string> local_relative_paths) 
            => GetRelativePathsWithPattern(local_relative_paths, AddDumpPath);

        private void Start()
        {
            Instance = this;
            _ = StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            yield return new WaitWhile(() => Game.Network == null);
            yield return new WaitWhile(() => Game.Network.CustomMessagingManager == null);

            Game.Network.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.HANDSHAKE], MessageHandler(NetworkMessageType.HANDSHAKE));
            Game.Network.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.DATA], MessageHandler(NetworkMessageType.DATA));
            Game.Network.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.COMMAND], MessageHandler(NetworkMessageType.COMMAND));
            Game.Network.CustomMessagingManager.RegisterNamedMessageHandler(MessageNames[NetworkMessageType.CHAT], MessageHandler(NetworkMessageType.CHAT));

            Debug.Log("Network utilities initialized");
            yield return null;
        }

        public void CacheRpcMessageChunk(RpcMessageChunk chunk, ushort num_to_receive, string type_name, bool isAppend = false)
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
                    Chunks = new List<RpcMessageChunk>() { chunk },
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

        public void SendMessageFromHostByRpc<T>(T obj, ClientRpcParams @params = default)
        {
            RpcMessageChunk[] object_chunks = obj.MakeChunks();
            /*
#pragma warning disable IDE0004 // The cast is neccessary for unity compiler unless newer version of unity (compiler) is used
            Action<DataChunk, ushort> rpc = from_host ? (Action<DataChunk, ushort>)ReceiveDataClientRpc<T> : ReceiveDataServerRpc<T>;
#pragma warning restore IDE0004
            */
            Action<RpcMessageChunk, ushort, ClientRpcParams> client_rpc = typeof(T) switch
            {
                _ when typeof(T) == typeof(Map) => ReceiveMapInfoClientRpc,
                _ when typeof(T) == typeof(Tile[][]) => ReceiveMapTilesClientRpc,
                _ when typeof(T) == typeof(IEnumerable<Unit>) => ReceiveMapUnitsClientRpc,
                _ when typeof(T) == typeof(IEnumerable<Building>) => ReceiveMapBuildingsClientRpc,
                _ when typeof(T) == typeof(List<Player>) => ReceiveBattlePlayersClientRpc,
                _ when typeof(T) == typeof(BattleRules) => ReceiveBattleRulesClientRpc,
                _ when typeof(T) == typeof(UnitData) => ReceiveUnitDataClientRpc,
                _ when typeof(T) == typeof(BuildingData) => ReceiveBuildingDataClientRpc,
                _ when typeof(T) == typeof(TileData) => ReceiveTileDataClientRpc,
                _ when typeof(T) == typeof(CustomizableData) => ReceiveCustomizableDataClientRpc,
                _ => throw new NotImplementedException($"Sending message by rpc from host with type {typeof(T).Name} is not supported at the moment")
            };
            foreach (RpcMessageChunk chunk in object_chunks)
            {
                client_rpc(chunk, (ushort)object_chunks.Length, @params);
            }
        }

        public void SendMessageFromClientByRpc<T>(T obj)
        {
            RpcMessageChunk[] object_chunks = obj.MakeChunks();
            Action<RpcMessageChunk, ushort> server_rpc = typeof(T) switch
            {
                // TODO add ServerRpc
                _ => throw new NotImplementedException($"Sending message by rpc from client with type {typeof(T).Name} is not supported at the moment")
            };
            foreach (RpcMessageChunk chunk in object_chunks)
            {
                server_rpc(chunk, (ushort)object_chunks.Length);
            }
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
            yield return null;
        }

        public void CacheNamedMessage(NetworkMessageType message_type, ulong sender_id, string message)
        {
            Match match = Regex.Match(message, @"^([^:]+): (.*?)$");
            if (!match.Success)
            {
                Debug.LogError($"Message {message} is not in correct format");
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

        public void SendNamedMessage<T>(T obj, ulong receiver_id, NetworkMessageType type)
        {
            string data = $"{typeof(T).FullName}: {JsonSerializer.Serialize(obj)}";
            int byte_length = Encoding.UTF8.GetByteCount(data) * 2 + 4;
            using FastBufferWriter writer = new FastBufferWriter(byte_length, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(data);
            NetworkDelivery delivery = byte_length > 1292
                ? NetworkDelivery.ReliableFragmentedSequenced
                : type == NetworkMessageType.CHAT
                    ? NetworkDelivery.ReliableSequenced
                    : NetworkDelivery.Reliable;
            NetworkManager.CustomMessagingManager.SendNamedMessage(MessageNames[type], receiver_id, writer, delivery);
        }

        public IEnumerable<object> GetNamedMessages(Predicate<NamedMessageObject> predicate)
        {
            IEnumerable<NamedMessageObject> targets = m_namedMessages.Where(m => predicate(m));
            IEnumerable<object> deserialized = targets.Select(m => m.GetDeserializedObject());
            m_namedMessages = m_namedMessages.Except(targets).ToList();
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
            yield return null;
        }

        public void CacheFileChunk(RpcMessageChunk chunk, ushort num_to_receive, string file_name)
        {
            if (m_files.ContainsKey(file_name))
            {
                if (m_files[file_name].Chunks.Count < num_to_receive )
                {
                    m_files[file_name].Chunks.Add(chunk);
                }
                else
                {
                    Debug.LogError($"Number of chunks received exceed {num_to_receive}");
                }
            }
            else
            {
                m_files.Add(file_name, new RpcMessageObject()
                {
                    Chunks = new List<RpcMessageChunk>() { chunk },
                    IsReady = false
                });
            }
            if (m_files[file_name].Chunks.Count == num_to_receive)
            {
                m_files[file_name].IsReady = true;
            }
        }

        public void SendFile(string local_relative_path, string destination_relative_path = "", ClientRpcParams @params = default)
        {
            string local_full_path = GetFullPath(ExternalFolder.NONE, local_relative_path.TrimStart(path_sep));
            if (!File.Exists(local_full_path))
            {
                Debug.LogError($"File not found at {local_full_path}");
                return;
            }
            byte[] bytes = File.ReadAllBytes(local_full_path);
            if (string.IsNullOrEmpty(destination_relative_path))
            {
                destination_relative_path = local_relative_path;
            }

            RpcMessageChunk[] file_chunks = bytes.MakeChunks();
            foreach (RpcMessageChunk chunk in file_chunks)
            {
                ReceiveFileClientRpc(chunk, (ushort)file_chunks.Length, destination_relative_path, @params);
            }
        }

        public void SendFiles(Dictionary<string, string> relative_paths, ClientRpcParams @params = default)
        {
            foreach (KeyValuePair<string, string> relative_path in relative_paths)
            {
                SendFile(relative_path.Key, relative_path.Value, @params);
            }
        }

        public void SendDumpFiles(List<string> local_file_paths, ClientRpcParams @params = default)
        {
            Dictionary<string, string> relative_paths = GetDumpPaths(local_file_paths);
            foreach (KeyValuePair<string, string> relative_path in relative_paths)
            {
                SendFile(relative_path.Key, relative_path.Value, @params);
            }
        }

        public bool SaveFile(string relative_path, bool delete_on_saved = true)
        {
            if (!m_files.ContainsKey(relative_path))
            {
                Debug.LogError($"No file with path {relative_path} is found.");
                return false;
            }
            if (!m_files[relative_path].IsReady)
            {
                return false;
            }
            m_files[relative_path].Chunks.AssembleChunksIntoFile(relative_path);
            if (delete_on_saved)
            {
                m_files.Remove(relative_path);
            }
            return true;
        }

        public IEnumerator TrySaveFile(string relative_path, bool delete_on_saved = true)
        {
            int counter = 0;
            bool saved = false;
            while (!saved && counter < 60)
            {
                saved = SaveFile(relative_path, delete_on_saved);
                yield return new WaitForSeconds(2);
                counter++;
            }
            if (!saved)
            {
                Debug.LogError("Failed to retrieve named message after 60 tries");
            }
            yield return saved;
        }

        public IEnumerator TrySaveFiles(Action callback = null)
        {
            List<IEnumerator> save_file_coroutines = new List<IEnumerator>();
            foreach (KeyValuePair<string, RpcMessageObject> files in m_files)
            {
                IEnumerator coroutine = TrySaveFile(files.Key, false);
                save_file_coroutines.Add(coroutine);
                StartCoroutine(coroutine);
            }

            yield return new WaitWhile(() => !save_file_coroutines.All(c => c.Current is bool));

            int failure_count = save_file_coroutines.Where(c => !(bool)c.Current).Count();
            if (failure_count > 0)
            {
                Debug.LogWarning($"Failed to save {failure_count} file(s)");
            }
            m_files.Clear();

            callback?.Invoke();

            yield return null;
        }

        /* UNITY FUCKING CRASHED ON CALLING A GENERIC RPC AND IT IS A FUCKING BUG
         * REFERNCE: https://forum.unity.com/threads/unity-crashes-on-rpc-call.1256361/
        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveDataClientRpc<T>(RpcMessageChunk chunk, ushort num_to_receive)
        {
            CacheRpcMessage(chunk, num_to_receive, typeof(T).FullName);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveDataServerRpc<T>(RpcMessageChunk chunk, ushort num_to_receive)
        {
            CacheRpcMessage(chunk, num_to_receive, typeof(T).FullName);
        }*/

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapInfoClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default) 
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(Map).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapTilesClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default) 
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(Tile[][]).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapUnitsClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default)
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(IEnumerable<Unit>).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveMapBuildingsClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default) 
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(IEnumerable<Building>).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveBattlePlayersClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default) 
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(List<Player>).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveBattleRulesClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default) 
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(BattleRules).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveUnitDataClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default)
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(UnitData).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveBuildingDataClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default)
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(BuildingData).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveTileDataClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default)
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(TileData).FullName);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveCustomizableDataClientRpc(RpcMessageChunk chunk, ushort num_to_receive, ClientRpcParams @params = default)
            => CacheRpcMessageChunk(chunk, num_to_receive, typeof(CustomizableData).FullName);

        private HandleNamedMessageDelegate MessageHandler(NetworkMessageType type) => (sender, reader) =>
        {
            reader.ReadValueSafe(out string message);
            Debug.Log($"Received a {type} message (length: {message.Length}) from " + (sender == 0 ? "server" : $"client with id {sender}"));
            CacheNamedMessage(type, sender, message);
        };

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ReceiveFileClientRpc(RpcMessageChunk chunk, ushort num_to_receive, string destination_relative_file_path, ClientRpcParams @params = default)
            => CacheFileChunk(chunk, num_to_receive, destination_relative_file_path);
    }
}
