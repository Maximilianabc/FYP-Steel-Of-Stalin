﻿using SteelOfStalin.Attributes;
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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;
using static SteelOfStalin.Util.Utilities;
using static SteelOfStalin.DataIO.DataUtilities;
using Attribute = SteelOfStalin.Attributes.Attribute;
using Plane = SteelOfStalin.Assets.Props.Units.Air.Plane;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.Assets;
using System.Reflection;
using Module = SteelOfStalin.Assets.Customizables.Module;

namespace SteelOfStalin.Util
{
    public static class Utilities
    {
        public static void Log(this Command command, string @event) => Debug.Log($"{command.Unit} has the following event when executing the command {command.Name}: {@event}");

        public static void LogError(this Command command, string reason, string explanation = "") => Debug.LogError($"Failed to execute command {command.Name} for unit at {command.Unit}: {reason} {explanation}");
        public static void LogError(this Unit unit, string reason, [CallerMemberName] string method_name = "") => Debug.LogError($"Failed to execute method {method_name} for {unit}: {reason}");
        public static void LogError(this Phase phase, string reason) => Debug.LogError($"Error when executing phases {phase.GetType().Name}: {reason}");

        public static IEnumerable<T> Flatten<T>(this T[][] ts) => ts.SelectMany(t => t);
        public static IEnumerable<T> CombineAll<T>(params IEnumerable<T>[] sources) => sources.SelectMany(t => t);
        public static IEnumerable<T> CombineAll<T>(params T[] sources) => sources;
        public static T Find<T>(this IEnumerable<T> source, Predicate<T> match) => source.Where(t => match(t)).FirstOrDefault();
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

        public static bool HasAnyOfFlags<T>(this T @enum, T group) where T : Enum => (Convert.ToInt64(@enum) & Convert.ToInt64(group)) != 0;

        public static double RandomBetweenSymmetricRange(double range) => range * (new System.Random().NextDouble() * 2 - 1);
        public static string ToPascalCase(string input) => Regex.Replace(input, @"(^[a-z])|[_ ]([a-z])", m => m.Value.ToUpper()).Replace("_", "").Replace(" ", "");
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
        public static Func<(double observer_recon, double observer_detect, double observee_conceal, double distance), bool> VisualSpotting => (input) =>
        {
            if (input.distance > input.observer_recon || input.observer_recon <= 0.5)
            {
                return false;
            }
            double determinant = 2 * input.observer_recon / input.distance - 1;
            if (determinant <= 0)
            {
                return false;
            }
            double detection_at_range = input.observer_detect / Math.Log(2 * input.observer_recon - 1) * Math.Log(determinant);
            return detection_at_range > input.observee_conceal;
        };
        public static Func<(Unit observer, Unit observee, double distance), bool> AcousticRanging => (input) =>
        {
            // TODO
            return false;
        };
    }
}

namespace SteelOfStalin.DataIO
{
    public abstract class Data<T> where T : INamedAsset
    {
        protected virtual string JsonFolderPath => $@"{ExternalFilePath}\Json";

        public abstract void Load();
        public abstract IEnumerable<T> All { get; }
        public T this[string name] => All.Find(a => a.Name == name);
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

        public override IEnumerable<Unit> All => Utilities.CombineAll<Unit>(Personnels, Artilleries, Vehicles, Vessels, Planes);

        public Unit GetNewUnit(string name) => (Unit)All.Find(u => u.Name == name).Clone();

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
        public List<ProductionBuilding> Resources { get; set; } = new List<ProductionBuilding>();
        public List<Infrastructure> Infrastructures { get; set; } = new List<Infrastructure>();
        public List<TransmissionBuilding> Transmissions { get; set; } = new List<TransmissionBuilding>();
        public List<DefensiveBuilding> Defensives { get; set; } = new List<DefensiveBuilding>();

        public override IEnumerable<Building> All => Utilities.CombineAll<Building>(Units, Resources, Infrastructures, Transmissions, Defensives);

        public Building GetNewBuilding(string name) => (Building)All.Find(b => b.Name == name).Clone();

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
                    Resources.Add(r);
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

        public override IEnumerable<Tile> All => Utilities.CombineAll<Tile>(Terrains, Cities);

        public Tile GetNewTile(string name) => (Tile)Terrains.Find(t => t.Name == name).Clone();
        public Tile GetNewTile(TileType type) => (Tile)Terrains.Find(t => t.Type == type).Clone();
        public Cities GetNewCities(string name) => (Cities)Cities.Find(c => c.Name == name).Clone();

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

        public override IEnumerable<Customizable> All => Utilities.CombineAll<Customizable>(Firearms, Modules.All, Shells);

        public Customizable GetNewCustomizable(string name) => (Customizable)All.Find(c => c.Name == name).Clone();
        public Firearm GetNewFirearm(string name) => (Firearm)Firearms.Find(f => f.Name == name).Clone();
        public Module GetNewModule(string name) => (Module)Modules.All.Find(m => m.Name == name).Clone();
        public Shell GetNewShell(string name) => (Shell)Shells.Find(s => s.Name == name).Clone();

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

        public override IEnumerable<Module> All => Utilities.CombineAll<Module>(Guns.All, HeavyMachineGuns, Engines, Suspensions, Radios, Periscopes, FuelTanks, AmmoRacks, TorpedoTubes, Sonars, Propellers, Rudders, Wings, LandingGears, Radars);

        public override void Load()
        {
            
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

        public override IEnumerable<Gun> All => Utilities.CombineAll<Gun>(Cannons, Howitzers, AutoCannons);

        public override void Load()
        {
            
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

    public static class DataUtilities
    {
        public static JsonSerializerOptions Options => new JsonSerializerOptions()
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
            Converters = { new RoundingJsonConverter() }
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
                                string proper_type_name = $"{base_type_name}.{ToPascalCase(tile_name)}";
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
    }
}
