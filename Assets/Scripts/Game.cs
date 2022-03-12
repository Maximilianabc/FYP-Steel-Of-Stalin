using SteelOfStalin.Attributes;
using SteelOfStalin.Commands;
using SteelOfStalin.Customizables;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.DataIO;
using SteelOfStalin.Flow;
using SteelOfStalin.Props;
using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using SteelOfStalin.Props.Units.Air;
using SteelOfStalin.Props.Units.Land;
using SteelOfStalin.Props.Units.Land.Personnels;
using SteelOfStalin.Props.Units.Sea;
using SteelOfStalin.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using UnityEngine;
using static SteelOfStalin.DataIO.DataUtilities;
using static SteelOfStalin.Util.Utilities;
using Capture = SteelOfStalin.Commands.Capture;
using Plane = SteelOfStalin.Props.Units.Air.Plane;

namespace SteelOfStalin
{
    //Contains all information of the game itself
    public class Game : MonoBehaviour
    {
        // Handles scenes (un)loading using Unity.SceneManager directly (all of its methods are static)
        public static List<Battle> Battles { get; set; } = new List<Battle>();
        public static GameSettings Settings { get; set; } = new GameSettings();
        public static List<GameObject> GameObjects { get; set; } = new List<GameObject>();
        public static List<AudioClip> AudioClips { get; set; } = new List<AudioClip>();
        // TODO FUT Impl. add achievements

        public static UnitData UnitData { get; set; } = new UnitData();
        public static BuildingData BuildingData { get; set; } = new BuildingData();
        public static TileData TileData { get; set; } = new TileData();
        public static CustomizableData CustomizableData { get; set; } = new CustomizableData();

        public void Start()
        {
            GameObjects = UnityEngine.Resources.LoadAll<GameObject>("Prefabs").ToList();
            AudioClips = UnityEngine.Resources.LoadAll<AudioClip>("Audio").ToList();
            UnitData.Load();
            BuildingData.Load();
            TileData.Load();
            // new Map().Load();
            // Settings = DeserializeJson<GameSettings>("settings.json");
        }
    }

    public class GameSettings
    {
        public bool EnableAnimations { get; set; }
        public byte VolumeMusic { get; set; } = 100;
        public byte VolumeSoundFX { get; set; } = 100;
        private string m_settingsPath => $@"{ExternalFilePath}\settings.json";

        public void Save() => File.WriteAllText(m_settingsPath, JsonSerializer.Serialize(this, Options));
        public GameSettings Load() => JsonSerializer.Deserialize<GameSettings>(m_settingsPath);
    }

    public class Battle : MonoBehaviour, INamedAsset
    {
        public static Battle Instance { get; private set; }

        public string Name { get; set; }
        public Map Map { get; set; } = new Map();
        public List<Player> Players { get; set; } = new List<Player>();
        public BattleRules Rules { get; set; } = new BattleRules();
        public IEnumerable<Player> ActivePlayers => Players.Where(p => !p.IsDefeated);

        public List<Round> Rounds = new List<Round>();
        public Round CurrentRound { get; set; }
        public int RoundNumber { get; set; } = 1;

        public bool EnablePlayerInput { get; set; } = true;
        public bool AreAllPlayersReady => ActivePlayers.All(p => p.IsReady);

        private Player m_winner { get; set; } = null;

        public void Start()
        {
            Instance = this;
            Load();

            // main game logic loop
            //while (m_winner == null)
            //{
            CurrentRound = new Round();
            ActivePlayers.ToList().ForEach(p => p.IsReady = false);
            CurrentRound.InitializeRoundStart();
            CurrentRound.CommandPrerequisitesChecking();
            EnablePlayerInput = true;

            _ = StartCoroutine(CurrentRound.Planning.PhaseLoop(Rules.TimeForEachRound));
            Debug.Log("End Turn");
            EnablePlayerInput = false;
            CurrentRound.EndPlanning();
            m_winner = CurrentRound.GetWinner();
            Rounds.Add(CurrentRound);
            RoundNumber++;
            //}
        }
        public void Save()
        {
            Rules.SerializeJson($@"Saves\{Name}\rules");
            Players.SerializeJson($@"Saves\{Name}\players");
        }
        public void Load()
        {
            Rules = DeserializeJson<BattleRules>($@"Saves\{Name}\rules");
            Map.Load();
        }
    }

    // Contains different rules of the battle, like how much time is allowed for each round etc.
    public class BattleRules
    {
        public int TimeForEachRound { get; set; } = 120; // in seconds, -1 means unlimited
        public bool IsFogOfWar { get; set; } = true;
        public bool RequireSignalConnection { get; set; } = true;
        public bool DestroyedUnitsCanBeScavenged { get; set; }
        public bool AllowUniversalQueue { get; set; }

        public BattleRules() { }
    }

    public class Map : ICloneable, INamedAsset
    {
        public static Map Instance { get; private set; }
        public string Name { get; set; } = "test";

        protected Tile[][] Tiles { get; set; }
        protected List<Unit> Units { get; set; } = new List<Unit>();
        protected List<Building> Buildings { get; set; } = new List<Building>();
        protected int Width { get; set; }
        protected int Height { get; set; }

        public Map() => Instance = this;
        public Map(int width, int height) => (Width, Height, Instance) = (width, height, this);

        protected string Folder => $@"Saves\Maps\{Name}";
        protected string TileFolder => $@"Saves\Maps\{Name}\tiles";

        public void Save()
        {
            CreateStreamingAssetsFolder(Folder);
            Units.SerializeJson($@"{Folder}\units");
            Buildings.SerializeJson($@"{Folder}\buildings");
            SaveToTxt($@"{Folder}\stats", GetStatistics());
            SaveToPng($@"{Folder}\minimap", Visualize());

            CreateStreamingAssetsFolder(TileFolder);
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i].SerializeJson($@"{TileFolder}\map_{i}");
            }
            Debug.Log($"Saved map {Name}");
        }

        public void Load()
        {
            Units = DeserializeJson<List<Unit>>($@"{Folder}\units");
            Buildings = DeserializeJson<List<Building>>($@"{Folder}\buildings");
            ReadStatistics();

            Tiles = new Tile[Width][];
            for (int i = 0; i < Width; i++)
            {
                // TODO handle FileIO exceptions for all IO operations
                // TODO FUT. Impl. regenerate map files by reading the stats.txt in case any map files corrupted (e.g. edge of map is not boundary etc.)
                Tiles[i] = DeserializeJson<List<Tile>>($@"{TileFolder}\map_{i}").ToArray();
            }
            Debug.Log($"Loaded map {Name}");
        }

        // get a color png for the map
        public Texture2D Visualize()
        {
            Texture2D texture = new Texture2D(Width * 2, Height * 2);

            int mapx = 0;
            for (int x = 0; x < Width * 2; x += 2)
            {
                int mapy = 0;
                for (int y = 0; y < Height * 2; y += 2)
                {
                    // TODO add options for display colors
                    Color color = Tiles[mapx][mapy].Type switch
                    {
                        TileType.BOUNDARY => new Color(200 / 255F, 200 / 255F, 200 / 255F),
                        TileType.PLAINS => new Color(217 / 255F, 181 / 255F, 28 / 255F),
                        TileType.GRASSLAND => new Color(0 / 255F, 230 / 255F, 0 / 255F),
                        TileType.FOREST => new Color(0 / 255F, 128 / 255F, 0 / 255F),
                        TileType.JUNGLE => new Color(0 / 255F, 51 / 255F, 0 / 255F),
                        TileType.STREAM => new Color(154 / 255F, 203 / 255F, 255 / 255F),
                        TileType.RIVER => new Color(13 / 255F, 91 / 255F, 225 / 255F),
                        TileType.OCEAN => new Color(0 / 255F, 0 / 255F, 128 / 255F),
                        TileType.SWAMP => new Color(19 / 255F, 179 / 255F, 172 / 255F),
                        TileType.DESERT => new Color(243 / 255F, 226 / 255F, 96 / 255F),
                        TileType.HILLOCK => new Color(220 / 255F, 180 / 255F, 148 / 255F),
                        TileType.HILLS => new Color(179 / 255F, 107 / 255F, 67 / 255F),
                        TileType.MOUNTAINS => new Color(132 / 255F, 68 / 255F, 33 / 255F),
                        TileType.ROCKS => new Color(100 / 255F, 100 / 255F, 100 / 255F),
                        TileType.SUBURB => new Color(230 / 255F, 0 / 255F, 230 / 255F),
                        TileType.CITY => new Color(96 / 255F, 0 / 255F, 96 / 255F),
                        TileType.METROPOLIS => new Color(250 / 255F, 0 / 255F, 0 / 255F),
                        _ => throw new ArgumentException("Unknown tile type")
                    };
                    // shift the pixels upward by 1 for odd columns
                    texture.SetPixel(x, y + mapx % 2, color);
                    texture.SetPixel(x + 1, y + mapx % 2, color);
                    texture.SetPixel(x, y + 1 + mapx % 2, color);
                    texture.SetPixel(x + 1, y + 1 + mapx % 2, color);

                    mapy++;
                }
                mapx++;
            }
            texture.Apply();
            return texture;
        }

        public virtual string GetStatistics()
        {
            StringBuilder sb = new StringBuilder();
            _ = sb.AppendLine($"Width: {Width}");
            _ = sb.AppendLine($"Height: {Height}");
            foreach (string name in Enum.GetNames(typeof(TileType)))
            {
                int count = TileCount((TileType)Enum.Parse(typeof(TileType), name));
                float percent = (float)Math.Round((double)count / (Width * Height) * 100, 2);
                _ = sb.AppendLine($"{name}:\t{count} ({percent}%)");
            }
            _ = sb.AppendLine($"Units: {Units.Count}");
            _ = sb.AppendLine($"Buildings: {Buildings.Count}");
            return sb.ToString();
        }

        public void ReadStatistics()
        {
            string[] lines = ReadTxt($@"{Folder}\stats");
            Width = int.Parse(Regex.Match(lines[0], @"(\d+)$").Groups[1].Value);
            Height = int.Parse(Regex.Match(lines[1], @"(\d+)$").Groups[1].Value);
        }

        public bool AddUnit(Unit u)
        {
            if (u == null)
            {
                Debug.LogError("Cannot add unit to map unit list: unit is null");
                return false;
            }
            if (Units.Contains(u))
            {
                Debug.LogWarning($"Unit {u.Name} ({u.CoOrds.X}, {u.CoOrds.Y}) already in map unit array. Skipping operation.");
                return true;
            }
            Units.Add(u);
            return true;
        }
        public bool AddBuilding(Building b)
        {
            if (b == null)
            {
                Debug.LogError("Cannot add building to map building list: building is null");
                return false;
            }
            if (Buildings.Contains(b))
            {
                Debug.LogWarning($"Building {b.Name} ({b.CoOrds.X}, {b.CoOrds.Y}) already in map unit array. Skipping operation.");
                return true;
            }
            Buildings.Add(b);
            return true;
        }
        public bool RemoveUnit(Unit u)
        {
            if (u == null)
            {
                Debug.LogWarning("Cannot remove unit from map unit list: unit is null");
                return false;
            }
            return Units.Remove(u);
        }
        public bool RemoveBuilding(Building b)
        {
            if (b == null)
            {
                Debug.LogWarning("Cannot remove building from map unit list: building is null");
                return false;
            }
            return Buildings.Remove(b);
        }

        public void PrintUnitList()
        {
            StringBuilder sb = new StringBuilder();
            _ = sb.AppendLine("Current unit list:");
            foreach (Unit unit in Units)
            {
                _ = sb.AppendLine($"{unit.Name} ({unit.CoOrds.X}, {unit.CoOrds.Y}): {unit.Status}");
            }
            Debug.Log(sb.ToString());
        }
        public void PrintBuildingList()
        {
            StringBuilder sb = new StringBuilder();
            _ = sb.Append("Current building list:\n");
            foreach (Building building in Buildings)
            {
                _ = sb.Append($"{building.Name} ({building.CoOrds.X}, {building.CoOrds.Y}): {building.Status}\n");
            }
            Debug.Log(sb.ToString());
        }

        public Tile GetTile(int x, int y) => Tiles[x][y];
        public Tile GetTile(Coordinates p) => Tiles[p.X][p.Y];

        public Tile GetTile(CubeCoordinates c) => Tiles[((Coordinates)c).X][((Coordinates)c).Y];
        public IEnumerable<Tile> GetTiles() => Tiles.Flatten();
        public IEnumerable<Tile> GetTiles(TileType type) => Tiles.Flatten().Where(t => t.Type == type);
        public IEnumerable<Tile> GetTiles(Predicate<Tile> predicate) => Tiles.Flatten().Where(t => predicate(t));

        public IEnumerable<Cities> GetCities() => Tiles.Flatten().Where(t => t is Cities).Cast<Cities>();
        public IEnumerable<Cities> GetCities(Player player) => GetCities().Where(c => c.Owner == player);
        public IEnumerable<Cities> GetCities(Predicate<Cities> predicate) => GetCities().Where(c => predicate(c));

        public IEnumerable<Tile> GetNeigbours(CubeCoordinates c, int distance = 1) => c.GetNeigbours(distance).Where(c =>
        {
            Coordinates p = (Coordinates)c;
            return p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
        }).Select(c => GetTile(c));
        public IEnumerable<Tile> GetStraightLineNeighbours(CubeCoordinates c, double distance = 1) => distance < 1
                ? throw new ArgumentException("Distance must be >= 1.")
                : GetNeigbours(c, (int)Math.Ceiling(distance)).Where(t => CubeCoordinates.GetStraightLineDistance(c, t.CubeCoOrds) <= distance);
        public bool HasUnoccupiedNeighbours(CubeCoordinates c, int distance = 1) => GetNeigbours(c, distance).Any(t => !t.IsOccupied);

        public IEnumerable<Unit> GetUnits() => Units;
        public IEnumerable<Unit> GetUnits<T>() where T : Unit => Units.OfType<T>();
        public IEnumerable<Unit> GetUnits<T>(Predicate<Unit> predicate) where T : Unit => GetUnits<T>().Where(u => predicate(u));
        public IEnumerable<Unit> GetUnits(Coordinates p) => GetUnits(GetTile(p));
        public IEnumerable<Unit> GetUnits(Tile t)
        {
            if (t == null)
            {
                Debug.LogWarning("Error when trying to get unit: input tile is null");
                return null;
            }

            List<Unit> units = Units.Where(u => u.CoOrds.X == t.CoOrds.X && u.CoOrds.Y == t.CoOrds.Y).ToList();
            if (units.Count == 0)
            {
                Debug.Log($"No unit found at {t}");
            }
            else if (units.Count > 2 || (units.Count == 2 && units[0].IsOfSameCategory(units[1])))
            {
                Debug.LogError($"Illegal stacking of units found at {t}!");
            }
            return units;
        }
        public IEnumerable<Unit> GetUnits(UnitStatus status) => Units.Where(u => (u.Status & status) != 0);
        public IEnumerable<Unit> GetUnits(Player player) => Units.Where(u => u.Owner == player);
        public IEnumerable<Unit> GetUnits(Predicate<Unit> predicate) => Units.Where(u => predicate(u));

        public IEnumerable<Building> GetBuildings() => Buildings;
        public IEnumerable<Building> GetBuildings<T>() where T : Building => Buildings.OfType<T>();
        public IEnumerable<Building> GetBuildings(Coordinates p) => GetBuildings(GetTile(p));
        public IEnumerable<Building> GetBuildings(IEnumerable<Coordinates> coordinates) => coordinates.SelectMany(c => GetBuildings(c));
        public IEnumerable<Building> GetBuildings(Tile t)
        {
            if (t == null)
            {
                Debug.LogWarning("Error when trying to get building: input tile is null");
                return null;
            }
            return Buildings.Where(b => b.CoOrds.X == t.CoOrds.X && b.CoOrds.Y == t.CoOrds.Y);
        }
        public IEnumerable<Building> GetBuildings(IEnumerable<Tile> tiles) => tiles.SelectMany(t => GetBuildings(t));
        public IEnumerable<Building> GetBuildings(BuildingStatus status) => Buildings.Where(b => b.Status == status);
        public IEnumerable<Building> GetBuildings(Player player) => Buildings.Where(b => b.Owner == player);
        public IEnumerable<Building> GetBuildings(Predicate<Building> predicate) => Buildings.Where(b => predicate(b));

        public int TileCount(TileType type) => Tiles.Flatten().Where(t => t.Type == type).Count();
        public float TilePercentage(TileType type) => (float)Math.Round(TileCount(type) / (Width * Height) * 100D, 2);

        public object Clone()
        {
            Map copy = (Map)MemberwiseClone();
            copy.Tiles = Tiles.Select(t => t.ToArray()).ToArray();
            copy.Units = Units.Select(u => (Unit)u.Clone()).ToList();
            return copy;
        }
    }

    // TODO FUT Impl. handle irregular-shaped map generation
    // TODO add options for variating height & humdity const., no. of river sources and no. of cities
    public class RandomMap : Map, ICloneable
    {
        public PerlinMap HeightMap { get; set; }
        public PerlinMap HumidityMap { get; set; }
        public int WaterSourceNum { get; set; } = -1;
        public int CitiesNum { get; set; } = -1;
        private List<CubeCoordinates> m_flatLands { get; set; }
        private List<CubeCoordinates> m_cities { get; set; } = new List<CubeCoordinates>();

        public RandomMap() { }

        public RandomMap(int width, int height, int num_player) : base(width, height)
        {
            System.Random random = new System.Random();
            HeightMap = new PerlinMap(width, height, seed_x: random.Next(-(1 << 16), 1 << 16), seed_y: random.Next(-(1 << 16), 1 << 16));
            HumidityMap = new PerlinMap(width, height, seed_x: random.Next(-(1 << 16), 1 << 16), seed_y: random.Next(-(1 << 16), 1 << 16));

            HeightMap.Generate();
            HumidityMap.Generate();

            Tiles = new Tile[width][];
            for (int x = 0; x < width; x++)
            {
                Tiles[x] = new Tile[height];
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    {
                        Tiles[x][y] = Game.TileData.GetNewTile(TileType.BOUNDARY);
                        Tiles[x][y].CoOrds = new Coordinates(x, y);
                        continue;
                    }
                    else if (HeightMap.Values[x][y] <= 0.25)
                    {
                        Tiles[x][y] = Game.TileData.GetNewTile(TileType.OCEAN);
                    }
                    else if (HeightMap.Values[x][y] <= 0.6)
                    {
                        TileType type = HumidityMap.Values[x][y] switch
                        {
                            _ when HeightMap.Values[x][y] < 0.4 && HumidityMap.Values[x][y] >= 0.7 => TileType.SWAMP,
                            _ when HumidityMap.Values[x][y] >= 0.65 => TileType.JUNGLE,
                            _ when HumidityMap.Values[x][y] >= 0.55 => TileType.FOREST,
                            _ when HumidityMap.Values[x][y] >= 0.45 => TileType.GRASSLAND,
                            _ when HumidityMap.Values[x][y] < 0.3 => TileType.DESERT,
                            _ => TileType.PLAINS,
                        };
                        Tiles[x][y] = Game.TileData.GetNewTile(type);
                    }
                    else
                    {
                        Tiles[x][y] = HeightMap.Values[x][y] <= 0.65
                            ? Game.TileData.GetNewTile(TileType.HILLOCK)
                            : Game.TileData.GetNewTile(HeightMap.Values[x][y] <= 0.75 ? TileType.HILLS : TileType.MOUNTAINS);
                    }
                    // TODO move these back to json generation
                    Tiles[x][y].Height = Tiles[x][y].Type switch
                    {
                        TileType.BOUNDARY => double.MaxValue,
                        TileType.PLAINS => 2,
                        TileType.GRASSLAND => 2,
                        TileType.FOREST => 2,
                        TileType.JUNGLE => 2,
                        TileType.STREAM => 1,
                        TileType.RIVER => 1,
                        TileType.OCEAN => 1,
                        TileType.SWAMP => 1,
                        TileType.DESERT => 2,
                        TileType.HILLOCK => 4,
                        TileType.HILLS => 8,
                        TileType.MOUNTAINS => 16,
                        TileType.ROCKS => 4,
                        TileType.SUBURB => 2,
                        TileType.CITY => 2,
                        TileType.METROPOLIS => 2,
                        _ => throw new NotImplementedException(),
                    };
                    Tiles[x][y].CoOrds = new Coordinates(x, y);
                }
            }
            GenerateRivers();
            GenerateCities(num_player);
        }

        // TODO FUT Impl. Replace A* with a more advanced procedural generation algo
        public void GenerateRivers()
        {
            Debug.Log("Generating rivers.");
            List<CubeCoordinates> hills = GetTiles(t => t.IsHill).Select(t => t.CubeCoOrds).ToList();
            if (WaterSourceNum < 0)
            {
                WaterSourceNum = (int)Math.Ceiling(hills.Count / 1000D);
            }
            if (WaterSourceNum == 0)
            {
                return;
            }

            List<CubeCoordinates> oceans = GetTiles(t => t.Type == TileType.OCEAN).Select(t => t.CubeCoOrds).ToList();
            if (!oceans.Any())
            {
                Debug.LogWarning("No ocean in this map.");
                return;
            }

            // get the nearest oceans to all sources
            List<CubeCoordinates> sources = hills.OrderBy(x => Guid.NewGuid()).Take(WaterSourceNum).ToList();
            List<CubeCoordinates> destinations = new List<CubeCoordinates>();
            foreach (CubeCoordinates source in sources)
            {
                int current_distance = int.MaxValue;
                CubeCoordinates nearest_ocean = new CubeCoordinates();
                foreach (CubeCoordinates ocean in oceans)
                {
                    int distance = CubeCoordinates.GetDistance(ocean, source);
                    if (distance < current_distance)
                    {
                        current_distance = distance;
                        nearest_ocean = ocean;
                    }
                }
                destinations.Add(nearest_ocean);
            }

            // path-find with height as cost from water sources to nearest oceans
            List<Stack<CubeCoordinates>> paths = new List<Stack<CubeCoordinates>>();
            Dictionary<CubeCoordinates, CubeCoordinates> source_destination_pairs = sources.Zip(destinations, (s, d) => new { s, d }).ToDictionary(x => x.s, x => x.d);
            foreach (KeyValuePair<CubeCoordinates, CubeCoordinates> pair in source_destination_pairs)
            {
                Coordinates start_coord = (Coordinates)pair.Key;
                Coordinates end_coord = (Coordinates)pair.Value;
                int shortest_distance = CubeCoordinates.GetDistance(pair.Key, pair.Value);
                WeightedTile start = new WeightedTile()
                {
                    CubeCoOrds = pair.Key,
                    BaseCost = 1,
                    Weight = Tiles[start_coord.X][start_coord.Y].Height,
                    MaxWeight = 16,
                    DistanceToGoal = shortest_distance
                };
                WeightedTile end = new WeightedTile()
                {
                    CubeCoOrds = pair.Value,
                    BaseCost = 1,
                    Weight = Tiles[end_coord.X][end_coord.Y].Height,
                    MaxWeight = 16,
                    DistanceToGoal = 0
                };

                Debug.Log($"Generating river from {start_coord} to {end_coord}.");
                Stack<CubeCoordinates> path = PathFind(start, end, max_weight: 16);
                paths.Add(path);
            }

            Debug.Log("Overwriting tiles along river path.");
            foreach (Stack<CubeCoordinates> p in paths)
            {
                while (p.Count > 0)
                {
                    Coordinates coords = (Coordinates)p.Pop();
                    TileType type = Tiles[coords.X][coords.Y].Type;
                    if (type == TileType.OCEAN || type == TileType.BOUNDARY)
                    {
                        break;
                    }
                    else if (type == TileType.RIVER)
                    {
                        continue;
                    }
                    Tiles[coords.X][coords.Y] = Game.TileData.GetNewTile(type == TileType.STREAM ? TileType.RIVER : TileType.STREAM);
                    Tiles[coords.X][coords.Y].CoOrds = new Coordinates(coords);
                }
            }
        }

        public void GenerateCities(int num_player, int min_sep = -1, int max_sep = -1, float suburb_ratio = 0.2F)
        {
            if (min_sep > max_sep)
            {
                Debug.LogError("Minimum separation cannot be larger than maximum separation");
                return;
            }
            if (max_sep < 0)
            {
                max_sep = Math.Max(Width, Height);
            }
            if (CitiesNum < 0)
            {
                CitiesNum = (int)Math.Ceiling(Width * Height / 2000D) + 8 - num_player;
            }
            if (min_sep < 0)
            {
                double determinant = 1 + 4 * (Width * Height / (3 * (CitiesNum + num_player))) * 2;
                min_sep = (int)((Math.Sqrt(determinant) - 1) / 2);
            }
            // area of a "cell" is 3n(n+1) hex, where n is the separation, a cell can contain only 1 city
            // looks like it is very hard to reach the limit, so divide by two to allow large seperations
            if (3 * min_sep * (min_sep + 1) * (CitiesNum + num_player) / 2 > Width * Height)
            {
                Debug.LogError("Not enough space for generating all cities. Consider lowering minimum separation of number of cities.");
                return;
            }

            m_flatLands = Tiles.Flatten().Where(t => t.IsFlatLand).Select(t => t.CubeCoOrds).ToList();

            // generate capitals first
            PickCities(num_player, Math.Max(Width, Height) / 2, max_sep);
            PickCities(CitiesNum, min_sep, max_sep);

            Debug.Log("Overwriting tiles with cities.");
            for (int i = 0; i < num_player; i++)
            {
                Coordinates c = (Coordinates)m_cities[i];
                Tiles[c.X][c.Y] = Game.TileData.GetNewCities("metropolis");
                Tiles[c.X][c.Y].CoOrds = new Coordinates(c);
            }
            m_cities.RemoveRange(0, num_player);

            int num_suburb = (int)(CitiesNum * suburb_ratio);
            for (int i = 0; i < num_suburb; i++)
            {
                int index = new System.Random().Next(m_cities.Count);
                Coordinates c = (Coordinates)m_cities[index];
                Tiles[c.X][c.Y] = Game.TileData.GetNewCities("suburb");
                Tiles[c.X][c.Y].CoOrds = new Coordinates(c);
                m_cities.RemoveAt(index);
            }

            foreach (CubeCoordinates cube in m_cities)
            {
                Coordinates c = (Coordinates)cube;
                Tiles[c.X][c.Y] = Game.TileData.GetNewCities("city");
                Tiles[c.X][c.Y].CoOrds = new Coordinates(c);
            }
        }

        public Stack<CubeCoordinates> PathFind(WeightedTile start, WeightedTile end, double weight = -1, double max_weight = -1)
        {
            Stack<CubeCoordinates> path = new Stack<CubeCoordinates>();
            List<WeightedTile> active = new List<WeightedTile>();
            List<WeightedTile> visited = new List<WeightedTile>();
            active.Add(start);

            int limit = Width * Height / 500;
            while (active.Any())
            {
                WeightedTile check = active.OrderBy(w => w.SuppliesCostDistance).FirstOrDefault();
                Coordinates c = (Coordinates)check.CubeCoOrds;
                if (check == end)
                {
                    while (check.Parent != null)
                    {
                        path.Push(check.CubeCoOrds);
                        check = check.Parent;
                    }
                    break;
                }
                visited.Add(check);
                _ = active.Remove(check);

                List<WeightedTile> neigbours = new List<WeightedTile>();
                GetNeigbours(check.CubeCoOrds).ToList().ForEach(t => neigbours.Add(new WeightedTile()
                {
                    Parent = check,
                    CubeCoOrds = t.CubeCoOrds,
                    BaseCost = t.Height,
                    Weight = 1,
                    MaxWeight = max_weight < 0 ? 1 : max_weight,
                    SuppliesCostSoFar = check.SuppliesCostSoFar + (weight < 0 ? t.Height : weight),
                    DistanceSoFar = check.DistanceSoFar + 1,
                    DistanceToGoal = CubeCoordinates.GetDistance(t.CubeCoOrds, end.CubeCoOrds)
                }));

                neigbours.ForEach(n =>
                {
                    if (!visited.Where(v => v == n).Any())
                    {
                        WeightedTile exist = active.Where(a => a == n).FirstOrDefault();
                        if (exist != null && exist.SuppliesCostDistance > check.SuppliesCostDistance)
                        {
                            _ = active.Remove(exist);
                        }
                        else
                        {
                            active.Add(n);
                        }
                    }
                });
            }
            return path;
        }

        public override string GetStatistics()
        {
            StringBuilder sb = new StringBuilder(base.GetStatistics());
            _ = sb.AppendLine($"Number of water sources: {WaterSourceNum}\n");
            _ = sb.AppendLine("Height Map:");
            _ = sb.AppendLine(HeightMap.GetStatistics());
            _ = sb.AppendLine("Humidity Map:");
            _ = sb.AppendLine(HumidityMap.GetStatistics());
            return sb.ToString();
        }

        private void PickCities(int num_cities, int min_sep, int max_sep)
        {
            Debug.Log($"Generating cities: no. of cities to generate: {num_cities}, min. separation: {min_sep}, max. separation: {max_sep}");
            bool force_generate_without_checking = false;
            while (num_cities > 0)
            {
                // if not have any cities yet, randomly pick one from available;
                if (m_cities.Count == 0 || force_generate_without_checking)
                {
                    CubeCoordinates c = m_flatLands[new System.Random().Next(m_flatLands.Count)];
                    m_cities.Add(c);
                    _ = m_flatLands.Remove(c);
                    force_generate_without_checking = false;
                    num_cities--;
                }
                else
                {
                    Coordinates last = (Coordinates)m_cities.Last();
                    // test for any cities already generated within min_sep of candidate, if true, pick another one
                    bool has_cities_within_range = true;
                    while (has_cities_within_range)
                    {
                        // pick one
                        int rand_dist = new System.Random().Next(min_sep, max_sep);
                        double rand_theta = new System.Random().NextDouble() * 2 * Math.PI;

                        int candidate_x = (int)(last.X + rand_dist * Math.Cos(rand_theta));
                        int candidate_y = (int)(last.Y + rand_dist * Math.Sin(rand_theta));
                        if (candidate_x > 0 && candidate_y > 0 && candidate_x < Width && candidate_y < Height)
                        {
                            CubeCoordinates candidate = (CubeCoordinates)new Coordinates(candidate_x, candidate_y);
                            if (m_flatLands.Any(s => s == candidate))
                            {
                                has_cities_within_range = candidate.GetNeigbours(min_sep).Intersect(m_cities).Any();

                                // be it passing the test or not, it won't be suitable: if it passes, it will be turned into a city, if not then obviously not a suitable tile for any other city generation
                                _ = m_flatLands.Remove(candidate);

                                if (!has_cities_within_range)
                                {
                                    m_cities.Add(candidate);
                                    num_cities--;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class PerlinMap
    {
        public int Height { get; set; }
        public int Width { get; set; }
        // the higher the more bumpy (zoom out from the noise map)
        public float Frequency { get; set; }
        // the higher the more detailed
        public byte Octaves { get; set; }
        // how much influence should each octave has
        public float Persistence { get; set; }
        // the lower the flatter
        public float Exponent { get; set; }
        // the higher the more distorted, 0.0F (min) means no distortion
        public float WarpStrength { get; set; }
        // not rly a seed, but instead random offsets
        // TODO FUT Impl. Replace Unity's Mathf.PerlinNoise with own implementation of Perlin random number
        public int Seedx { get; set; }
        public int Seedy { get; set; }

        public float[][] Values { get; set; }

        public PerlinMap() { }

        // generation of a new perlin noise map
        public PerlinMap(int width, int height, float freq = 3.0F, byte octaves = 1, float persist = 0.8F, float exp = 1.0F, float warp_strength = 0.1F, int seed_x = 0, int seed_y = 0)
            => (Width, Height, Frequency, Octaves, Persistence, Exponent, WarpStrength, Seedx, Seedy)
            = (width, height, freq, octaves, persist, exp, warp_strength, seed_x, seed_y);

        public void Generate()
        {
            Values = new float[Width][];
            for (int x = 0; x < Width; x++)
            {
                Values[x] = new float[Height];
                for (int y = 0; y < Height; y++)
                {
                    float nx = x / (float)Width;
                    float ny = y / (float)Height;
                    if (WarpStrength > 0)
                    {
                        nx += PerlinWithParams(nx, ny) * WarpStrength;
                        ny += PerlinWithParams(nx, ny) * WarpStrength;
                    }
                    Values[x][y] = PerlinWithParams(nx, ny);
                }
            }
        }

        // save the noise map in .txt with all values
        public void SaveTxt(string file_name)
        {
            if (Values == null)
            {
                Debug.LogError("Value Map is not initialized. Probably not yet called the Generate method.");
                return;
            }
            StringBuilder sb = new StringBuilder();
            foreach (float[] fs in Values)
            {
                foreach (float f in fs)
                {
                    _ = sb.Append($"{Math.Round(f, 3):0.000} ");
                }
                _ = sb.AppendLine();
            }

            float[] flat = Values.Flatten().ToArray();
            _ = sb.AppendLine($"(0, 0.1]: {Math.Round(flat.Where(f => f <= 0.1).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.1, 0.2]: {Math.Round(flat.Where(f => 0.1 < f && f <= 0.2).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.2, 0.3]: {Math.Round(flat.Where(f => 0.2 < f && f <= 0.3).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.3, 0.4]: {Math.Round(flat.Where(f => 0.3 < f && f <= 0.4).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.4, 0.5]: {Math.Round(flat.Where(f => 0.4 < f && f <= 0.5).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.5, 0.6]: {Math.Round(flat.Where(f => 0.5 < f && f <= 0.6).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.6, 0.7]: {Math.Round(flat.Where(f => 0.6 < f && f <= 0.7).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.7, 0.8]: {Math.Round(flat.Where(f => 0.7 < f && f <= 0.8).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.8, 0.9]: {Math.Round(flat.Where(f => 0.8 < f && f <= 0.9).Count() / (float)(Width * Height) * 100, 2)}%");
            _ = sb.AppendLine($"(0.9, 1]: {Math.Round(flat.Where(f => 0.9 < f && f <= 1).Count() / (float)(Width * Height) * 100, 2)}%");
            File.WriteAllText($"{file_name}.txt", sb.ToString());
        }

        // save a black and white png for the noise
        public void Visualize(string file_name)
        {
            Texture2D texture = new Texture2D(Width, Height);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float depth = Values[x][y];
                    texture.SetPixel(x, y, new UnityEngine.Color(depth, depth, depth));
                }
            }
            texture.Apply();
            byte[] bs = texture.EncodeToPNG();
            using FileStream fs = new FileStream($"{file_name}.png", FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(bs, 0, bs.Length);
            fs.Close();
        }

        public string GetStatistics() => $"Frequency: {Frequency}\nOctaves: {Octaves}\nPersistence: {Persistence}\nExponent: {Exponent}\nWarp Strength: {WarpStrength}\nOffset:({Seedx},{Seedy})\n";

        private float PerlinWithParams(float nx, float ny)
        {
            float freq = Frequency;
            float amp = 1;
            float sum_amp = 0;
            float value = 0;
            for (byte o = 0; o < Octaves; o++)
            {
                float raw = Mathf.PerlinNoise(freq * nx + Seedx, freq * ny + Seedy);
                value += raw * amp;
                sum_amp += amp;
                freq *= 2;
                amp *= Persistence;
            }
            return (float)Math.Pow(value / sum_amp, Exponent);
        }
    }

    public abstract class Command : ICloneable
    {
        public Coordinates Source { get; set; }
        public Coordinates Destination { get; set; }
        public Unit Unit { get; set; }
        public string Name => GetType().Name;
        public StringBuilder Recorder => new StringBuilder();

        public abstract void Execute();

        public Command() { }
        public Command(Unit u) => Unit = u;
        public Command(Unit u, Coordinates src, Coordinates dest) => (Unit, Source, Destination) = (u, src, dest);
        public Command(Unit u, int srcX, int srcY, int destX, int destY) => (Unit, Source, Destination) = (u, new Coordinates(srcX, srcY), new Coordinates(destX, destY));

        public override string ToString() => Recorder.ToString();
        public object Clone()
        {
            Command copy = (Command)MemberwiseClone();
            copy.Unit = (Unit)Unit.Clone();
            return copy;
        }

        protected void ConsumeSuppliesStandingStill()
        {
            double supplies = Unit.GetSuppliesRequired(Unit.GetLocatedTile());
            Unit.Carrying.Supplies.MinusEquals(supplies);
            if (Unit.Carrying.Supplies < 0)
            {
                Unit.Carrying.Supplies.Value = 0;
            }
            this.Log($"Consumed {supplies} supplies when standing still. Supplies remaining : {Unit.Carrying.Supplies}");
            _ = Recorder.Append(Unit.GetResourcesChangeRecord("Supplies", -supplies));
        }
        protected void ConsumeAmmoFiring(IOffensiveCustomizable weapon, bool normal = true)
        {
            double cartridges = normal ? weapon.ConsumptionNormal.Cartridges.ApplyMod() : weapon.ConsumptionSuppress.Cartridges.ApplyMod();
            double shells = normal ? weapon.ConsumptionNormal.Shells.ApplyMod() : weapon.ConsumptionSuppress.Shells.ApplyMod();
            double fuel = normal ? weapon.ConsumptionNormal.Fuel.ApplyMod() : weapon.ConsumptionSuppress.Fuel.ApplyMod();

            if (cartridges > 0)
            {
                Unit.Carrying.Cartridges.MinusEquals(cartridges);
                _ = Recorder.Append(Unit.GetResourcesChangeRecord("Cartridges", -cartridges));
            }
            if (shells > 0)
            {
                Unit.Carrying.Shells.MinusEquals(shells);
                _ = Recorder.Append(Unit.GetResourcesChangeRecord("Shells", -shells));
            }
            if (fuel > 0)
            {
                Unit.Carrying.Fuel.MinusEquals(fuel);
                _ = Recorder.Append(Unit.GetResourcesChangeRecord("Fuel", -fuel));
            }
            this.Log($"Consumed {cartridges} cartridges, {shells} shells and {fuel} fuel when firing. Remaining: {Unit.Carrying.Cartridges} cartridges, {Unit.Carrying.Shells} shells and {Unit.Carrying.Fuel} fuel");
        }
    }

    public abstract class Customizable : ICloneable, INamedAsset
    {
        public string Name { get; set; }
        public Cost Cost { get; set; } = new Cost();

        public Customizable() { }
        public Customizable(Customizable another) => (Name, Cost) = (another.Name, (Cost)another.Cost.Clone());

        public abstract object Clone();
    }

    public interface INamedAsset
    {
        public string Name { get; set; }
    }
}

namespace SteelOfStalin.Flow
{
    public class Round : ICloneable
    {
        public int Number { get; set; }

        [JsonIgnore] public List<Player> Players { get; set; }
        public List<Command> Commands { get; set; } = new List<Command>();
        public List<Phase> Phases { get; set; } = new List<Phase>();
        public Planning Planning { get; set; } = new Planning();

        public Round()
        {
            Players = Battle.Instance.Players;
            Number = Battle.Instance.RoundNumber;
        }

        // Remove fired and moved flags from all units
        public void InitializeRoundStart() => Map.Instance.GetUnits(UnitStatus.ACTIVE).ToList().ForEach(u => 
        { 
            u.Status &= ~(UnitStatus.MOVED | UnitStatus.FIRED);
            u.AvailableConstructionCommands = AvailableConstructionCommands.NONE;
            u.AvailableFiringCommands = AvailableFiringCommands.NONE;
            u.AvailableLogisticsCommands = AvailableLogisticsCommands.NONE;
            u.AvailableMiscCommands = AvailableMiscCommands.NONE;
            // all active units should be able to hold at round start
            u.AvailableMovementCommands = AvailableMovementCommands.HOLD;
        });

        public void CommandPrerequisitesChecking()
        {
            List<Unit> ActiveUnits = Map.Instance.GetUnits(UnitStatus.ACTIVE).ToList();
            foreach (Unit u in ActiveUnits)
            {
                if (u.IsConstructing || u.IsSuppressed)
                {
                    continue;
                }
                if (u.CanMove())
                {
                    u.AvailableMovementCommands |= AvailableMovementCommands.MOVE;
                }
                if (u.CanMerge())
                {
                    u.AvailableMovementCommands |= AvailableMovementCommands.MERGE;
                }
                if (u.CanFire())
                {
                    u.AvailableFiringCommands |= AvailableFiringCommands.FIRE;
                }
                if (u.CanSabotage())
                {
                    u.AvailableFiringCommands |= AvailableFiringCommands.SABOTAGE;
                }
                if (u is Ground g)
                {
                    if (g.CanSuppress())
                    {
                        g.AvailableFiringCommands |= AvailableFiringCommands.SUPPRESS;
                    }
                    if (g.CanAmbush())
                    {
                        g.AvailableFiringCommands |= AvailableFiringCommands.AMBUSH;
                    }
                }
                if (u is Personnel p)
                {
                    if (p.CanAboard())
                    {
                        p.AvailableLogisticsCommands |= AvailableLogisticsCommands.ABOARD;
                    }
                    if (p.CanCapture())
                    {
                        p.AvailableMiscCommands |= AvailableMiscCommands.CAPTURE;
                    }
                    if (p.CanConstruct())
                    {
                        p.AvailableConstructionCommands |= AvailableConstructionCommands.CONSTRUCT;
                    }
                    if (p.CanFortify())
                    {
                        p.AvailableConstructionCommands |= AvailableConstructionCommands.FORTIFY;
                    }
                    if (p.CanDemolish())
                    {
                        p.AvailableConstructionCommands |= AvailableConstructionCommands.DEMOLISH;
                    }
                    if (Battle.Instance.Rules.DestroyedUnitsCanBeScavenged && p.CanScavenge())
                    {
                        p.AvailableMiscCommands |= AvailableMiscCommands.SCAVENGE;
                    }
                    if (p is Engineer e && e.CanRepair())
                    {
                        e.AvailableLogisticsCommands |= AvailableLogisticsCommands.REPAIR;
                    }
                }
                if (u is Artillery a)
                {
                    if (a.CanAboard())
                    {
                        a.AvailableLogisticsCommands |= AvailableLogisticsCommands.ABOARD;
                    }
                    if (a.CanAssemble())
                    {
                        a.AvailableMiscCommands |= AvailableMiscCommands.ASSEMBLE;
                    }
                    if (a.CanDisassemble())
                    {
                        a.AvailableMiscCommands |= AvailableMiscCommands.DISASSEMBLE;
                    }
                }
                if (u is Submarine s)
                {
                    if (s.CanSubmerge())
                    {
                        s.AvailableMovementCommands |= AvailableMovementCommands.SUBMERGE;
                    }
                    if (s.CanSurface())
                    {
                        s.AvailableMovementCommands |= AvailableMovementCommands.SURFACE;
                    }
                }
                if (u is Plane l)
                {
                    if (l.CanLand())
                    {
                        l.AvailableMovementCommands |= AvailableMovementCommands.LAND;
                    }
                    if (l is Bomber b && b.CanBombard())
                    {
                        b.AvailableFiringCommands |= AvailableFiringCommands.BOMBARD;
                    }
                }
            }
        }

        public void AddAutoCommands()
        {
            // TODO
        }

        public Player GetWinner()
        {
            // TODO FUT Impl. handle different winning conditions for different gamemodes
            IEnumerable<Player> surviving_players = Players.Where(p => !p.IsDefeated);
            return surviving_players.Count() == 1 ? surviving_players.First() : null;
        }

        public void ScreenUpdate()
        {
            // handle screen update here
            IEnumerable<Unit> destroyed_unit = Map.Instance.GetUnits(UnitStatus.DESTROYED);
            destroyed_unit.ToList().ForEach(u => u.RemoveFromScene());
        }

        public void EndPlanning()
        {
            // add Hold command for any units that aren't assigned command
            Map.Instance.GetUnits(u => (u.Status != UnitStatus.IN_QUEUE || u.Status != UnitStatus.CAN_BE_DEPLOYED) && u.CommandAssigned == CommandAssigned.NONE).ToList().ForEach(u => Commands.Add(new Hold(u)));
            Phases.AddRange(new Phase[]
            {
                new Firing(Commands),
                new Moving(Commands),
                new CounterAttacking(),
                new Constructing(Commands),
                new Training(Commands),
                new Spotting(),
                new Signaling(),
                new Misc(Commands)
            });
            Phases.ForEach(p => p.Execute());
            ScreenUpdate();
            Commands.Clear();
        }

        // output this round to JSON
        public void Record()
        {
            // TODO
        }

        // load a round from JSON
        public void Replay()
        {
            // TODO
        }

        public object Clone()
        {
            Round copy = (Round)MemberwiseClone();
            copy.Players = Players.Select(p => (Player)p.Clone()).ToList();
            copy.Commands = Commands.Select(c => (Command)c.Clone()).ToList();
            copy.Phases = Phases.Select(p => (Phase)p.Clone()).ToList();
            return copy;
        }
    }

    public abstract class Phase : ICloneable
    {
        public List<Command> CommandsForThisPhase { get; set; } = new List<Command>();
        protected StringBuilder Recorder => new StringBuilder();
        protected string Header => GetType().Name;

        // empty ctor for (de)serialization
        public Phase()
        {

        }
        public Phase(List<Command> commands, params Type[] commandTypes) => Array.ForEach(commandTypes, t => CommandsForThisPhase.AddRange(commands.Where(c => c.GetType() == t)));

        public virtual void Execute()
        {
            CommandsForThisPhase.ForEach(c => c.Execute());
            RecordPhase();
        }

        private void RecordPhase()
        {
            foreach (Command command in CommandsForThisPhase)
            {
                string record = command.Recorder.ToString();
                if (!string.IsNullOrEmpty(record))
                {
                    _ = Recorder.Append($"[{GetType().Name}] {record}");
                }
            }
        }

        public object Clone()
        {
            Phase copy = (Phase)MemberwiseClone();
            copy.CommandsForThisPhase = CommandsForThisPhase.Select(c => (Command)c.Clone()).ToList();
            return copy;
        }
    }

    public sealed class Planning : Phase
    {
        public Planning() { }
        public IEnumerator PhaseLoop(float wait_time)
        {
            float counter = 0;
            while (counter < wait_time || !Battle.Instance.AreAllPlayersReady)
            {
                yield return new WaitForSeconds(1);
                counter += 1;
                Debug.Log($"Time remaining: {wait_time - counter} second(s)");
                yield return null;
            }
        }
    }
    public sealed class Moving : Phase
    {
        public Moving() : base() { }
        public Moving(List<Command> commands)
            : base(commands, typeof(Hold), typeof(Move), typeof(Merge), typeof(Aboard), typeof(Disembark), typeof(Capture), typeof(Submerge), typeof(Surface), typeof(Land)) { }

        public override void Execute()
        {
            // TODO resolve move conflicts here

            base.Execute();
        }
    }
    public sealed class Firing : Phase
    {
        public Firing() : base() { }
        public Firing(List<Command> commands) : base(commands, typeof(Fire), typeof(Suppress), typeof(Sabotage), typeof(Ambush)) { }

        public override void Execute()
        {
            IEnumerable<Unit> units = Map.Instance.GetUnits();
            units.Where(u => u.CurrentSuppressionLevel > 0).ToList().ForEach(u =>
            {
                u.CurrentSuppressionLevel -= u.Defense.Suppression.Resilience.ApplyMod();
                if (u.CurrentSuppressionLevel < 0)
                {
                    u.CurrentSuppressionLevel = 0;
                }
                if (u.CurrentSuppressionLevel < u.Defense.Suppression.Threshold)
                {
                    u.Status &= ~UnitStatus.SUPPRESSED;
                }
            });

            // TODO LOS logic here

            IEnumerable<Unit> suppress_targets = CommandsForThisPhase.Where(c => c is Suppress).Select(c => ((Suppress)c).Target);
            // reset cons. sup. round for those units with the following conditions:
            // 1. cons. sup. round > 0 (consecutively suppressed for at least 1 round before), and
            // 2. are not under suppression this round
            units.Where(u => u.ConsecutiveSuppressedRound > 0 && !suppress_targets.Contains(u)).ToList().ForEach(u => u.ConsecutiveSuppressedRound = 0);

            base.Execute();
            suppress_targets.ToList().ForEach(t =>
            {
                if (t.CurrentSuppressionLevel > t.Defense.Suppression.Threshold)
                {
                    t.Status |= UnitStatus.SUPPRESSED;
                }
            });
        }
    }
    public sealed class CounterAttacking : Phase
    {
        public CounterAttacking() : base() { }
        //public CounterAttacking(List<Command> commands) : base(commands, typeof(Ambush)) { }
        public override void Execute()
        {
            // TODO FUT Impl. 
        }
    }
    public sealed class Spotting : Phase
    {
        public Spotting() : base() { }

        public override void Execute()
        {
            // TODO add LOS logic
            Map.Instance.GetUnits(u => u.Status.HasAnyOfFlags(UnitStatus.IN_FIELD)).ToList().ForEach(u =>
            {
                u.UnitsInSight.Clear();
                u.BuildingsInSight.Clear();
                if (Battle.Instance.Rules.IsFogOfWar)
                {
                    Tile observer_tile = u.GetLocatedTile();
                    double observer_recon = observer_tile.TerrainMod.Recon.ApplyTo(u.Scouting.Reconnaissance.ApplyMod());
                    double observer_detect = u.Scouting.Detection.ApplyMod();

                    u.GetHostileUnitsInReconRange().ToList().ForEach(h =>
                    {
                        Tile observee_tile = h.GetLocatedTile();
                        double st_line_distance = CubeCoordinates.GetStraightLineDistance(u.CubeCoOrds, h.CubeCoOrds);
                        double observee_conceal = observee_tile.TerrainMod.Concealment.ApplyTo(h.Scouting.Concealment.ApplyMod());

                        if (h.Status.HasFlag(UnitStatus.MOVED))
                        {
                            observee_conceal = h.GetConcealmentPenaltyMove().ApplyTo(observee_conceal);
                        }
                        if (h.Status.HasFlag(UnitStatus.FIRED))
                        {
                            // TODO
                        }

                        if (Formula.VisualSpotting((observer_recon, observer_detect, observee_conceal, st_line_distance)))
                        {
                            u.UnitsInSight.Add(h);
                        }
                    });
                    u.GetHostileBuildingsInReconRange().ToList().ForEach(b =>
                    {
                        Tile observee_tile = b.GetLocatedTile();
                        double st_line_distance = CubeCoordinates.GetStraightLineDistance(u.CubeCoOrds, b.CubeCoOrds);
                        double observee_conceal = observee_tile.TerrainMod.Concealment.ApplyTo(b.Scouting.Concealment.ApplyMod());
                        if (Formula.VisualSpotting((observer_recon, observer_detect, observee_conceal, st_line_distance)))
                        {
                            u.BuildingsInSight.Add(b);
                        }
                    });
                    // TODO add acousting ranging logic
                }
                else
                {
                    // TODO FUT Impl. handle non fog-of-war
                }
            });
        }
    }
    public sealed class Signaling : Phase
    {
        public Signaling() : base() { }

        public override void Execute()
        {
            if (!Battle.Instance.Rules.RequireSignalConnection)
            {
                return;
            }

            // remove DISCONNECTED flags for all units first
            Map.Instance.GetUnits(u => u.Status.HasAnyOfFlags(UnitStatus.IN_FIELD)).ToList().ForEach(u => u.Status &= ~UnitStatus.DISCONNECTED);

            foreach (Player player in Battle.Instance.ActivePlayers)
            {
                IEnumerable<Unit> units = player.Units;
                IEnumerable<Cities> cities = player.Cities;
                IEnumerable<Prop> comm_source = units.Concat<Prop>(cities);

                Graph<Prop> connections = new Graph<Prop>(comm_source);
                foreach (Prop p in comm_source)
                {
                    foreach (Prop q in comm_source)
                    {
                        if (connections.HasEdge(p, q))
                        {
                            continue;
                        }
                        if ((p is Unit u && u.CanCommunicateWith(q)) || (p is Cities c && c.CanCommunicateWith(q)))
                        {
                            connections.SetEdge(p, q);
                        }
                    }
                }

                connections.GetIsloatedVertices().ToList().ForEach(v =>
                {
                    if (v is Unit u)
                    {
                        u.Status |= UnitStatus.DISCONNECTED;
                    }
                });
            }
        }
    }
    public sealed class Constructing : Phase
    {
        public Constructing() : base() { }
        public Constructing(List<Command> commands) : base(commands, typeof(Construct), typeof(Fortify), typeof(Demolish)) { }

        public override void Execute()
        {
            Map.Instance.GetBuildings(BuildingStatus.UNDER_CONSTRUCTION).ToList().ForEach(b =>
            {
                b.ConstructionTimeRemaining -= 1;
                if (b.ConstructionTimeRemaining < 0)
                {
                    b.ConstructionTimeRemaining = 0;
                    b.Level += 1;
                    b.Status = BuildingStatus.ACTIVE;
                    b.BuilderLocation = new Coordinates(-1, -1);

                    if (b.Level > 1)
                    {
                        // fortification complete
                        // TODO FUT Impl. apply mod for other attributes as well
                        b.Durability.PlusEquals(Game.BuildingData.All.Find(o => o.Name == b.Name).Durability.ApplyMod());
                    }
                    else
                    {
                        // TODO instantiate the building
                    }
                }
                if (b.BuilderLocation != null && b.BuilderLocation != default)
                {
                    // remove the constructing flag for the builder of this building
                    Map.Instance.GetUnits(b.BuilderLocation).Where(u => u is Personnel && u.Owner == b.Owner).ToList().ForEach(p => p.Status &= ~UnitStatus.CONSTRUCTING);
                }
            });
            base.Execute();
        }
    }
    public sealed class Training : Phase
    {
        public Training() : base() { }
        public Training(List<Command> commands) : base(commands, typeof(Train), typeof(Deploy), typeof(Rearm)) { }

        public override void Execute()
        {
            Map.Instance.GetBuildings<UnitBuilding>().Where(ub => ub.Status == BuildingStatus.ACTIVE).ToList().ForEach(ub =>
            {
                UnitBuilding b = (UnitBuilding)ub;
                while (b.TrainingQueue.Count > 0)
                {
                    // retrieve the first unit in queue without removing it from the queue
                    Unit queueing = b.TrainingQueue.Peek();
                    queueing.TrainingTimeRemaining -= 1;

                    if (queueing.TrainingTimeRemaining <= 0)
                    {
                        queueing.TrainingTimeRemaining = 0;

                        // if capacity is reached, no more units will be ready to be deployed
                        if (b.ReadyToDeploy.Count < b.QueueCapacity)
                        {
                            // retrieve the first unit in queue and remove it from the queue
                            Unit ready = b.TrainingQueue.Dequeue();

                            // change its status
                            ready.Status = UnitStatus.CAN_BE_DEPLOYED;
                            // add it to deploy list
                            b.ReadyToDeploy.Add(ready);
                        }
                        continue;
                    }
                    break;
                }
            });
            base.Execute();
        }
    }
    public sealed class Misc : Phase
    {
        public Misc() : base() { }
        public Misc(List<Command> commands)
            : base(commands, typeof(Scavenge), typeof(Disassemble), typeof(Assemble)) { }

        public override void Execute()
        {
            base.Execute();
            CalculateMorale();
            AddDestroyedFlags();
            Battle.Instance.Players.ForEach(p => p.ProduceResources());
        }

        public void AddDestroyedFlags()
        {
            // strength <= 0 or planes that have no fuel
            // TODO FUT Impl. add crash landing success chance for planes that have no fuel
            Predicate<Unit> unit_destroyed = u => u.Defense.Strength <= 0 || (u is Aerial && u.Carrying.Fuel <= 0);
            Predicate<Building> building_destroyed = b => b.Durability <= 0;

            // TODO FUT Impl. add wrecked flags if still resources carrying is not 0, change to destroyed if it is.

            Map.Instance.GetUnits(unit_destroyed).ToList().ForEach(u => u.Status = UnitStatus.DESTROYED);
            Map.Instance.GetBuildings(building_destroyed).ToList().ForEach(b => b.Status = BuildingStatus.DESTROYED);
        }

        public void CalculateMorale()
        {
            Map.Instance.GetUnits(u => u.Carrying.Supplies == 0).ToList().ForEach(u =>
            {
                // TODO FUT Impl. add some variations to the penalty
                u.Morale.MinusEquals(10);
                if (u.Morale < 0)
                {
                    u.Morale.Value = 0;
                    // TODO FUT Impl. add some variations to the chance
                    if (new System.Random().NextDouble() < 0.1)
                    {
                        // TODO FUT Impl. add some chance for surrender effect: enemy can capture this unit
                        u.Status = UnitStatus.DESTROYED;
                    }
                }
            });
        }
    }
}