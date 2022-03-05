using SteelOfStalin.Attributes;
using SteelOfStalin.Customizables;
using SteelOfStalin.Customizables.Modules;
using SteelOfStalin.Flow;
using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using SteelOfStalin.Props.Units.Land;
using SteelOfStalin.Props.Units.Sea;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;
using static SteelOfStalin.DataIO.DataUtilities;
using Attribute = SteelOfStalin.Attributes.Attribute;
using Plane = SteelOfStalin.Props.Units.Air.Plane;

namespace SteelOfStalin.Util
{
    public static class Utilities
    {
        public static void Log(this Command command, string @event) => Debug.Log($"Unit at {command.Unit.PrintCoOrds()} has the following event when executing the command {command.GetType()}: {@event}");

        public static void LogError(this Command command, string reason, string explanation = "") => Debug.LogError($"Failed to execute command {command.GetType()} for unit at {command.Unit.PrintCoOrds()}: {reason} {explanation}");
        public static void LogError(this Unit unit, string reason, [CallerMemberName] string method_name = "") => Debug.LogError($"Failed to execute method {method_name} for unit at {unit.PrintCoOrds()}: {reason}");
        public static void LogError(this Phase phase, string reason) => Debug.LogError($"Error when executing phases {phase.GetType()}: {reason}");

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
        // TODO FUT Impl. so far the same as visual spotting for units, tweak a bit in the future
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

namespace SteelOfStalin.DataIO
{
    public abstract class Data
    {
        protected virtual string JsonFolderPath => $@"{ExternalFilePath}\Json";

        public abstract void Load();
        protected FileInfo[] GetJsonFiles(string path) => new DirectoryInfo(path).GetFiles("*.json");
    }

    public sealed class UnitData : Data
    {
        protected override string JsonFolderPath => $@"{base.JsonFolderPath}\units";
        public List<Personnel> PersonnelData { get; set; } = new List<Personnel>();
        public List<Artillery> ArtilleriesData { get; set; } = new List<Artillery>();
        public List<Vehicle> VehiclesData { get; set; } = new List<Vehicle>();
        public List<Vessel> VesselsData { get; set; } = new List<Vessel>();
        public List<Plane> PlanesData { get; set; } = new List<Plane>();

        public override void Load()
        {
            string[] subfolders = new string[] { "personnels", "artilleries", "vehicles", "vessels", "planes" };
            foreach (string subfolder in subfolders)
            {
                string subfolder_path = $@"{JsonFolderPath}\{subfolder}";
                string sub_type = subfolder == "planes" ? "Aerial" : subfolder == "vessels" ? "Sea" : "Land";
                string base_type_name = $"SteelOfStalin.Props.Units.{sub_type}.{ToProperCase(subfolder)}";
                foreach (FileInfo file in GetJsonFiles(subfolder_path))
                {
                    string proper_type_name = $"{base_type_name}.{ToProperCase(Path.GetFileNameWithoutExtension(file.Name))}";
                    Type type = Type.GetType(proper_type_name);
                    if (type == null)
                    {
                        Debug.LogError($"No type is with proper name {proper_type_name}");
                        continue;
                    }
                    string json = File.ReadAllText($@"{subfolder_path}\{file.Name}");
                    object o = JsonSerializer.Deserialize(json, type, Options);
                    if (o is Personnel p)
                    {
                        PersonnelData.Add(p);
                    }
                    else if (o is Artillery a)
                    {
                        ArtilleriesData.Add(a);
                    }
                    else if (o is Vehicle v)
                    {
                        VehiclesData.Add(v);
                    }
                    else if (o is Vessel e)
                    {
                        VesselsData.Add(e);
                    }
                    else if (o is Plane l)
                    {
                        PlanesData.Add(l);
                    }
                    else
                    {
                        Debug.LogError($"Unknown unit type {o.GetType().Name}");
                    }
                }
            }
            Debug.Log("All units data loaded.");
        }
    }

    public sealed class BuildingData : Data
    {
        protected override string JsonFolderPath => $@"{base.JsonFolderPath}\buildings";
        public List<UnitBuilding> UnitBuildingData { get; set; } = new List<UnitBuilding>();
        public List<ProductionBuilding> ResourcesBuildingsData { get; set; } = new List<ProductionBuilding>();
        public List<Infrastructure> InfrastructuresData { get; set; } = new List<Infrastructure>();
        public List<TransmissionBuilding> TransmissionBuildingsData { get; set; } = new List<TransmissionBuilding>();
        public List<DefensiveBuilding> DefensiveBuildingsData { get; set; } = new List<DefensiveBuilding>();

        public override void Load()
        {
            string[] subfolders = new string[] { "productions", "units", "infrastructures", "defensives", "transmissions" };
            foreach (string subfolder in subfolders)
            {
                string subfolder_path = $@"{JsonFolderPath}\{subfolder}";
                string base_type_name = $"SteelOfStalin.Props.Buildings.{ToProperCase(subfolder)}";
                foreach (FileInfo file in GetJsonFiles(subfolder_path))
                {
                    string proper_type_name = $"{base_type_name}.{ToProperCase(Path.GetFileNameWithoutExtension(file.Name))}";
                    Type type = Type.GetType(proper_type_name);
                    if (type == null)
                    {
                        Debug.LogError($"No type is with proper name {proper_type_name}");
                        return;
                    }
                    string json = File.ReadAllText($@"{subfolder_path}\{file.Name}");
                    object o = JsonSerializer.Deserialize(json, type, Options);
                    if (o is UnitBuilding u)
                    {
                        UnitBuildingData.Add(u);
                    }
                    else if (o is ProductionBuilding r)
                    {
                        ResourcesBuildingsData.Add(r);
                    }
                    else if (o is Infrastructure i)
                    {
                        InfrastructuresData.Add(i);
                    }
                    else if (o is TransmissionBuilding t)
                    {
                        TransmissionBuildingsData.Add(t);
                    }
                    else if (o is DefensiveBuilding d)
                    {
                        DefensiveBuildingsData.Add(d);
                    }
                }
            }
            Debug.Log("All buildings data loaded.");
        }
    }

    public sealed class TileData : Data
    {
        public List<Tile> TilesData { get; set; } = new List<Tile>();
        public List<Cities> CitiesData { get; set; } = new List<Cities>();

        public Tile GetNewTile(string name) => (Tile)TilesData.Find(t => t.Name == name).Clone();
        public Tile GetNewTile(TileType type) => (Tile)TilesData.Find(t => t.Type == type).Clone();
        public Cities GetNewCities(string name) => (Cities)CitiesData.Find(c => c.Name == name).Clone();

        public override void Load()
        {
            TilesData = DeserializeJsonWithAbstractType<Tile>(@"Json\tile", "Name", "SteelOfStalin.Props.Tiles").ToList();
            IEnumerable<Cities> cities = DeserializeJsonWithAbstractType<Cities>(@"Json\cities", "Name", "SteelOfStalin.Props.Tiles").ToList();
            TilesData.AddRange(cities);
            CitiesData = cities.ToList();
            Debug.Log("All tiles data loaded.");
        }
    }

    public sealed class CustomizableData : Data
    {
        public override void Load()
        {

        }
    }

    public static class DataUtilities
    {
        public static JsonSerializerOptions Options => new JsonSerializerOptions() { WriteIndented = true };
        public static string ExternalFilePath => ConvertToWindowsPath(Application.streamingAssetsPath);

        public static string ConvertToWindowsPath(string path) => path.Replace("/", @"\");
        public static string ToProperCase(string input) => Regex.Replace(input, @"(^[a-z])|[_ ]([a-z])", m => m.Value.ToUpper()).Replace("_", "").Replace(" ", "");

        public static void CreateStreamingAssetsFolder(string path) => Directory.CreateDirectory($@"{ExternalFilePath}\{path}");

        public static void SerializeJson<T>(this T input, string path) => File.WriteAllText($@"{ExternalFilePath}\{path}.json", Regex.Replace(JsonSerializer.Serialize<T>(input, Options), @"\r\n\s+""IsEmpty"": (?:true|false),", ""));
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
                                string proper_type_name = $"{base_type_name}.{ToProperCase(tile_name)}";
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
        public static void SaveToPng(string path, Texture2D texture)
        {
            byte[] bs = texture.EncodeToPNG();
            using FileStream fs = new FileStream($@"{ExternalFilePath}\{path}.png", FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(bs, 0, bs.Length);
            fs.Close();
        }
    }
}
