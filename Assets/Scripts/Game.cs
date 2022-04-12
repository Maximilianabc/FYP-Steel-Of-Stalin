using SteelOfStalin.Attributes;
using SteelOfStalin.Commands;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.DataIO;
using SteelOfStalin.Flow;
using SteelOfStalin.Assets.Props;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Air;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.Assets.Props.Units.Sea;
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
using Plane = SteelOfStalin.Assets.Props.Units.Air.Plane;
using SteelOfStalin.Assets;
using SteelOfStalin.Assets.Props.Buildings.Units;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using static Unity.Netcode.Transports.UTP.UnityTransport;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SteelOfStalin
{
    //Contains all information of the game itself
    public class Game : MonoBehaviour
    {
        // Handles scenes (un)loading using Unity.SceneManager directly (all of its methods are static)
        public static List<BattleInfo> BattleInfos { get; set; } = new List<BattleInfo>();
        public static GameSettings Settings { get; set; } = new GameSettings();
        public static List<GameObject> GameObjects { get; set; } = new List<GameObject>();
        public static List<AudioClip> AudioClips { get; set; } = new List<AudioClip>();
        public static BattleInfo ActiveBattle { get; set; }

        // TODO FUT Impl. add achievements

        public static UnitData UnitData { get; set; } = new UnitData();
        public static BuildingData BuildingData { get; set; } = new BuildingData();
        public static TileData TileData { get; set; } = new TileData();
        public static CustomizableData CustomizableData { get; set; } = new CustomizableData();

        public static bool AssetsLoaded { get; private set; }

        public void Start()
        {
            LoadAllAssets();
            LoadBattleInfos();
        }

        public static void StartHost() => NetworkManager.Singleton.StartHost();
        public static void StartServer() => NetworkManager.Singleton.StartServer();
        public static void StartClient()
        {
            NetworkManager manager = NetworkManager.Singleton;
            ConnectionAddressData connection = manager.GetComponent<UnityTransport>().ConnectionData;
            // TODO FUT. Impl. change these to player input instead of loopback address in production
            connection.Address = "127.0.0.1";
            connection.Port = 7777;
            NetworkManager.Singleton.StartClient();
        }

        public static void LoadAllAssets()
        {
            GameObjects = UnityEngine.Resources.LoadAll<GameObject>("Prefabs").ToList();
            AudioClips = UnityEngine.Resources.LoadAll<AudioClip>("Audio").ToList();
            UnitData.Load();
            BuildingData.Load();
            TileData.Load();
            CustomizableData.Load();
            AssetsLoaded = true;
        }

        public static void LoadBattleInfos()
        {
            foreach (string save in Directory.GetDirectories(@$"{ExternalFilePath}\Saves"))
            {
                string battle_name = Path.GetFileName(save);
                string map_path = $@"Saves\{battle_name}\map";
                string[] lines;
                try
                {
                    lines = ReadTxt($@"{map_path}\stats");
                }
                catch (FileNotFoundException)
                {
                    Debug.LogError($"stats.txt not found in path {map_path}");
                    continue;
                }

                BattleInfos.Add(new BattleInfo()
                {
                    Name = battle_name,
                    MapName = Regex.Match(lines[0], @"^Name: (\w+)$").Groups[1].Value,
                    MapWidth = int.Parse(Regex.Match(lines[1], @"^Width: (\d+)$").Groups[1].Value),
                    MapHeight = int.Parse(Regex.Match(lines[2], @"^Height: (\d+)$").Groups[1].Value),
                    MaxNumPlayers = int.Parse(Regex.Match(lines[3], @"^Players: (\d)$").Groups[1].Value),
                    Rules = DeserializeJson<BattleRules>($@"{save}\rules", false)
                });
            }
        }
    }

    public class GameSettings
    {
        public bool EnableAnimations { get; set; }
        public byte VolumeMusic { get; set; } = 100;
        public byte VolumeSoundFX { get; set; } = 100;

        public void Save() => this.SerializeJson("settings");
    }

    public class Battle : NetworkBehaviour, INamedAsset
    {
        public static Battle Instance { get; private set; }

        public string Name { get; set; }
        public Map Map { get; set; } = new Map();
        public List<Player> Players { get; set; } = new List<Player>();
        public BattleRules Rules { get; set; } = new BattleRules();

        public List<Round> Rounds = new List<Round>();
        public Round CurrentRound { get; set; }
        public int RoundNumber { get; set; } = 1;
        public int TimeRemaining { get; set; }
        public bool EnablePlayerInput { get; set; } = true;

        [JsonIgnore] public IEnumerable<Player> ActivePlayers => Players.Where(p => !p.IsDefeated);
        [JsonIgnore] public bool AreAllPlayersReady => ActivePlayers.All(p => p.IsReady);
        public NetworkUtilities NetworkUtilities { get; set; }

        private Player m_winner { get; set; } = null;
        private bool m_isInitialized => Players.Count > 0 && Map.IsInitialized;
        private string m_folder => $@"Saves\{Name}";

        private void Start()
        {
            Instance = this;
#if UNITY_EDITOR
            if (Game.ActiveBattle == null)
            {
                Game.ActiveBattle = new BattleInfo();
            }
#endif
            BattleInfo info = Game.ActiveBattle;
            Name = info.Name;
            Rules = info.Rules;
            Map.Name = info.MapName;
            Map.Width = info.MapWidth;
            Map.Height = info.MapHeight;

            NetworkUtilities = GameObject.FindObjectOfType<NetworkUtilities>();
            if (NetworkManager.IsHost)
            {
                Debug.Log("Started as host");
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                Load();
            }
            else if (NetworkManager.IsClient)
            {
                Debug.Log("Started as client");
            }
            else
            {
                Debug.LogError("Something went wrong: NetworkManager is neither host nor client.");
                return;
            }
            //_ = StartCoroutine(WaitForInitialization());
        }

        private IEnumerator GameLoop()
        {
            // main game logic loop
            while (m_winner == null)
            {
                CurrentRound = new Round();
                ActivePlayers.ToList().ForEach(p => p.IsReady = false);
                CurrentRound.InitializeRoundStart();
                CurrentRound.CommandPrerequisitesChecking();
                EnablePlayerInput = true;
                TimeRemaining = Rules.TimeForEachRound;
                yield return new WaitForEndOfFrame();

                bool unlimited_time = Rules.TimeForEachRound < 0;
                if (unlimited_time)
                {
                    Debug.Log("No time limit. Wait for all players ready to continue");
                }

                int counter = 0;
                while (counter < Rules.TimeForEachRound && !AreAllPlayersReady)
                {
                    yield return new WaitForSeconds(1);
                    if (!unlimited_time)
                    {
                        counter += 1;
                        TimeRemaining--;
                        Debug.Log($"Time remaining: {TimeRemaining} second(s)");
                    }
                }
                EndPlanning();
            }
            yield return null;
        }
        private IEnumerator WaitForInitialization()
        {
            yield return new WaitWhile(() => !m_isInitialized);
            AddPropsToScene();
            _ = StartCoroutine(GameLoop());
        }

        private void AddPropsToScene()
        {
            foreach (Unit unit in Map.GetUnits())
            {
                unit.AddToScene();
            }
            foreach (Building building in Map.GetBuildings(b => !(b is Barracks) && !(b is Arsenal)))
            {
                building.AddToScene();
            }
            foreach (Tile tile in Map.GetTiles())
            {
                tile.AddToScene();
            }
            foreach (IOwnableAsset ownable in Map.GetProps(p => p is IOwnableAsset))
            {
                if (!(ownable is Barracks) && !(ownable is Arsenal) && !string.IsNullOrEmpty(ownable.OwnerName))
                {
                    Prop ownable_prop = ((Prop)ownable);
                    GameObject ownable_object = ownable_prop.GetObjectOnScene();
                    if (ownable_object == null)
                    {
                        Debug.LogError($"Cannot get object {ownable_prop.MeshName} on screen");
                        continue;
                    }
                    MeshRenderer mr = ownable_object.GetComponent<MeshRenderer>();
                    if (mr == null)
                    {
                        mr = ownable_object.GetComponentInSpecificChild<MeshRenderer>(ownable_prop.Name);
                    }
                    // note: use _Color if not using HDRP
                    mr.material.SetColor("_BaseColor", GetPlayer(ownable.OwnerName).Color);
                }
            }
            // _ = StartCoroutine(GameLoop());
        }
        private void EndPlanning()
        {
            Debug.Log("End Turn");
            EnablePlayerInput = false;
            CurrentRound.EndPlanning();
            m_winner = CurrentRound.GetWinner();
            Rounds.Add(CurrentRound);
            RoundNumber++;
        }

        public Player GetPlayer(string name) => Players.Find(p => p.Name == name);
        public Player GetPlayer(Color color) => Players.Find(p => p.Color == color);

        // for testing maps, i.e. map and battle are decoupled (not generated together)
        public void SetMetropolisOwners()
        {
            int i = 0;
            foreach (Metropolis m in Map.GetCities<Metropolis>())
            {
                m.SetOwner(Players[i]);
                i++;
            }
        }
        // for testing maps
        public void SetDefaultUnitBuildingsOwners()
        {
            IEnumerable<Metropolis> metro = Map.GetCities<Metropolis>();
            foreach (Metropolis m in metro)
            {
                foreach (Building building in Map.Instance.GetBuildings(m.CoOrds))
                {
                    if (building is UnitBuilding ub)
                    {
                        ub.SetOwner(m.Owner);
                    }
                }
            }
        }
        
        public void Save()
        {
            Rules.Save();
            Players.SerializeJson($@"{m_folder}\players");
            Map.Save();
            Debug.Log($"Saved battle {Name}");
        }
        public void Load()
        {
            if (!StreamingAssetExists($@"{m_folder}\rules.json"))
            {
                Rules.Save();
            }
            Rules = DeserializeJson<BattleRules>($@"{m_folder}\rules");
            Players = DeserializeJson<List<Player>>($@"{m_folder}\players");
            Map.Load();
            Debug.Log($"Loaded battle {Name}");
        }

        private void OnClientConnected(ulong id)
        {
            Debug.Log($"Client (id: {id}) connected");
            ClientRpcParams send_params = NetworkUtilities.GetClientRpcSendParams(id);

            NetworkUtilities.SendFiles(NetworkUtilities.GetDumpPaths(Game.UnitData.LocalJsonFilePaths), send_params);
            NetworkUtilities.SendFiles(NetworkUtilities.GetDumpPaths(Game.BuildingData.LocalJsonFilePaths), send_params);
            NetworkUtilities.SendFiles(NetworkUtilities.GetDumpPaths(Game.TileData.LocalJsonFilePaths), send_params);
            NetworkUtilities.SendFiles(NetworkUtilities.GetDumpPaths(Game.CustomizableData.LocalJsonFilePaths), send_params);

            NetworkUtilities.SendNamedMessage(Map, id, NetworkMessageType.DATA);
            // NetworkUtilities.SendNamedMessage(Map.GetTilesUnflatterned(), id, NetworkMessageType.DATA);
            // NOTE: if sending a named message that is too long (like whole map), the handle of the reader on client side won't be able to accessed and throws NRE continuously
            NetworkUtilities.SendMessageFromHostByRpc(Map.GetTilesUnflatterned(), send_params);
            NetworkUtilities.SendNamedMessage(Map.GetUnits(), id, NetworkMessageType.DATA);
            NetworkUtilities.SendNamedMessage(Map.GetBuildings(), id, NetworkMessageType.DATA);
            NetworkUtilities.SendNamedMessage(Players, id, NetworkMessageType.DATA);
            NetworkUtilities.SendNamedMessage(Rules, id, NetworkMessageType.DATA);

            Debug.Log($"Sent all data to client (id: {id})");

            SetAllDataClientRpc(send_params);
        }

        [ClientRpc]
        private void SetAllDataClientRpc(ClientRpcParams @params)
        {
            _ = StartCoroutine(NetworkUtilities.TrySaveFiles());

            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<Map>(m => m.MessageType == NetworkMessageType.DATA, result => Map = result));
            _ = StartCoroutine(NetworkUtilities.TryGetRpcMessage<Tile[][]>(result => Map.SetTiles(result)));
            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<IEnumerable<Unit>>(m => m.MessageType == NetworkMessageType.DATA, result => Map.SetUnits(result)));
            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<IEnumerable<Building>>(m => m.MessageType == NetworkMessageType.DATA, result => Map.SetBuildings(result)));
            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<List<Player>>(m => m.MessageType == NetworkMessageType.DATA, result => Players = result));
            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<BattleRules>(m => m.MessageType == NetworkMessageType.DATA, result => Rules = result));
        }
    }

    // a simple class for reading stats of the battles when game starts
    public class BattleInfo
    {
        public string Name { get; set; } = "test";
        // TODO FUT. Impl. Add load map from map name when implementing Historical gamemode
        public string MapName { get; set; } = "testing123";
        public int MapWidth { get; set; } = 100;
        public int MapHeight { get; set; } = 100;
        public int MaxNumPlayers { get; set; } = 3;
        public BattleRules Rules { get; set; } = new BattleRules();

        public BattleInfo() { }

        public BattleInfo(string name, string map_name, int width, int height, int max_players, BattleRules rules)
            => (Name, MapName, MapWidth, MapHeight, MaxNumPlayers, Rules) = (name, map_name, width, height, max_players, rules);
    }

    // Contains different rules of the battle, like how much time is allowed for each round etc.
    public class BattleRules
    {
        public int TimeForEachRound { get; set; } = 10; // in seconds, -1 means unlimited
        public bool IsFogOfWar { get; set; } = true;
        public bool RequireSignalConnection { get; set; } = true;
        public bool DestroyedUnitsCanBeScavenged { get; set; }
        public bool AllowUniversalQueue { get; set; }

        public BattleRules() { }

        public void Save() => this.SerializeJson($@"Saves\{Battle.Instance.Name}\rules");
    }

    public class Map : ICloneable, INamedAsset, INetworkSerializable
    {
        public static Map Instance { get; private set; }
        public string Name { get; set; }
        public string BattleName { get; set; } = "test";
        public int Width { get; set; }
        public int Height { get; set; }

        [JsonIgnore] public List<Player> Players { get; set; }
        [JsonIgnore] public IEnumerable<Prop> AllProps => CombineAll<Prop>(Tiles.Flatten(), Units, Buildings);

        protected Tile[][] Tiles { get; set; }
        protected List<Unit> Units { get; set; } = new List<Unit>();
        protected List<Building> Buildings { get; set; } = new List<Building>();

        [JsonIgnore] public bool IsInitialized => Tiles != null && Width != 0 && Height != 0;
        // TODO FUT. Impl. add more validity check
        [JsonIgnore] public bool IsValid => Tiles != null && GetTiles().Count() == Width * Height;
        protected string Folder => $@"Saves\{BattleName}\map";
        protected string TileFolder => $@"{Folder}\tiles";

        public Map() => Instance = this;
        public Map(int width, int height) => (Width, Height, BattleName, Instance) = (width, height, Battle.Instance?.Name ?? "test", this);
        // for new battle
        public Map(int width, int height, string battle_name, string name) => (Width, Height, BattleName, Name, Instance) = (width, height, battle_name, name, this);

        public virtual void Save()
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
            Debug.Log($"Saved map {Name} for {BattleName}");
        }

        public virtual void Load()
        {
            BattleName = Battle.Instance?.Name ?? "test";
            // must be called from unit tests if Battle.Instance is null
            Players = Battle.Instance?.Players ?? DeserializeJson<List<Player>>($@"Saves\test\players");

            Units = DeserializeJson<List<Unit>>($@"{Folder}\units");
            foreach (Unit u in Units)
            {
                u.SetMeshName();
                if (!string.IsNullOrEmpty(u.OwnerName))
                {
                    u.SetOwnerFromName();
                }
                else
                {
                    Debug.LogWarning($"Unit {u} does not have an owner");
                }
            }

            Buildings = DeserializeJson<List<Building>>($@"{Folder}\buildings");
            foreach (Building b in Buildings)
            {
                b.SetMeshName();
                if (!string.IsNullOrEmpty(b.OwnerName))
                {
                    b.SetOwnerFromName();
                }
                // building can have no owners (e.g. unit buildings in neutral cities)
            }

            if (Width == 0 || Height == 0)
            {
                ReadStatistics();
            }
            Tiles = new Tile[Width][];
            for (int i = 0; i < Width; i++)
            {
                // TODO handle FileIO exceptions for all IO operations
                // TODO FUT. Impl. regenerate map files by reading the stats.txt in case any map files corrupted (e.g. edge of map is not boundary etc.)
                Tiles[i] = DeserializeJson<List<Tile>>($@"{TileFolder}\map_{i}").ToArray();
            }
            foreach (Tile t in Tiles.Flatten())
            {
                if (t is Cities c && !string.IsNullOrEmpty(c.OwnerName))
                {
                    c.SetOwnerFromName();
                }
                t.SetMeshName();
            }

            Debug.Log($"Loaded map {Name} for {BattleName}");
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
            _ = sb.AppendLine($"Name: {Name}");
            _ = sb.AppendLine($"Width: {Width}");
            _ = sb.AppendLine($"Height: {Height}");
            _ = sb.AppendLine($"Players: {TileCount(TileType.METROPOLIS)}"); // TODO FUT. Impl. change this to real number of players
            foreach (string name in Enum.GetNames(typeof(TileType)))
            {
                int count = TileCount((TileType)Enum.Parse(typeof(TileType), name));
                float percent = (float)Math.Round((decimal)count / (Width * Height) * 100, 2);
                _ = sb.AppendLine($"{name}:\t{count} ({percent}%)");
            }
            _ = sb.AppendLine($"Units: {Units.Count}");
            _ = sb.AppendLine($"Buildings: {Buildings.Count}");
            return sb.ToString();
        }
        public void ReadStatistics()
        {
            string[] lines = ReadTxt($@"{Folder}\stats");
            Name = Regex.Match(lines[0], @"^Name: (\w+)$").Groups[1].Value; 
            Width = int.Parse(Regex.Match(lines[1], @"(\d+)$").Groups[1].Value);
            Height = int.Parse(Regex.Match(lines[2], @"(\d+)$").Groups[1].Value);
        }

        public void SetTiles(Tile[][] tiles)
        {
            if (!new StackTrace().GetFrames().Select(s => s.GetMethod().Name).Contains("SetAllDataClientRpc"))
            {
                Debug.LogError("Cannot set tiles from methods other than Rpc");
                return;
            }
            Tiles = tiles;
        }
        public void SetUnits(IEnumerable<Unit> units)
        {
            if (!new StackTrace().GetFrames().Select(s => s.GetMethod().Name).Contains("SetAllDataClientRpc"))
            {
                Debug.LogError("Cannot set units from methods other than Rpc");
                return;
            }
            Units = units.ToList();
        }
        public void SetBuildings(IEnumerable<Building> buildings)
        {
            if (!new StackTrace().GetFrames().Select(s => s.GetMethod().Name).Contains("SetAllDataClientRpc"))
            {
                Debug.LogError("Cannot set buildings from methods other than Rpc");
                return;
            }
            Buildings = buildings.ToList();
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
                return false;
            }
            Units.Add(u);
            return true;
        }
        public void AddUnits(params Unit[] units) => Units.AddRange(units);
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
                return false;
            }
            Buildings.Add(b);
            return true;
        }
        public void AddBuildings(params Building[] buildings) => Buildings.AddRange(buildings);
        public bool RemoveUnit(Unit u)
        {
            if (u == null)
            {
                Debug.LogWarning("Cannot remove unit from map unit list: unit is null");
                return false;
            }
            return Units.Remove(u);
        }
        public void RemoveUnits(IEnumerable<Unit> units) => Units.RemoveAll(u => units.Contains(u));
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

        public Prop GetProp(GameObject gameObject) => AllProps.Find(p => p.MeshName == gameObject.name);
        public IEnumerable<Prop> GetProps(Coordinates c) => AllProps.Where(p => p.CoOrds == c);
        public IEnumerable<Prop> GetProps(CubeCoordinates c) => AllProps.Where(p => p.CubeCoOrds == c);
        public IEnumerable<Prop> GetProps(Predicate<Prop> predicate) => AllProps.Where(p => predicate(p));
        public IEnumerable<T> GetProps<T>() where T : Prop => AllProps.OfType<T>();
        public IEnumerable<T> GetProps<T>(CubeCoordinates c) where T : Prop => GetProps(c).OfType<T>();
        public IEnumerable<T> GetProps<T>(Predicate<T> predicate) where T : Prop => AllProps.OfType<T>().Where(p => predicate(p));

        public Tile GetTile(int x, int y) => Tiles[x][y];
        public Tile GetTile(Coordinates p) => Tiles[p.X][p.Y];
        public Tile GetTile(CubeCoordinates c) => Tiles[((Coordinates)c).X][((Coordinates)c).Y];
        public IEnumerable<Tile> GetTiles() => Tiles.Flatten();
        public IEnumerable<Tile> GetTiles(TileType type) => Tiles.Flatten().Where(t => t.Type == type);
        public IEnumerable<Tile> GetTiles(Predicate<Tile> predicate) => Tiles.Flatten().Where(t => predicate(t));
        public IEnumerable<T> GetTiles<T>() where T : Tile => Tiles.Flatten().OfType<T>();
        public Tile[][] GetTilesUnflatterned() => Tiles;

        public IEnumerable<Cities> GetCities() => GetTiles<Cities>();
        public IEnumerable<Cities> GetCities(Player player) => GetCities().Where(c => c.Owner == player);
        public IEnumerable<Cities> GetCities(Predicate<Cities> predicate) => GetCities().Where(c => predicate(c));
        public IEnumerable<T> GetCities<T>() where T : Cities => Tiles.Flatten().OfType<T>();

        public IEnumerable<Tile> GetNeighbours(CubeCoordinates c, int distance = 1, bool include_self = false) => c.GetNeigbours(distance, include_self).Where(c =>
        {
            Coordinates p = (Coordinates)c;
            return p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
        }).Select(c => GetTile(c));
        public IEnumerable<Tile> GetStraightLineNeighbours(CubeCoordinates c, decimal distance = 1) => distance < 1
                ? throw new ArgumentException("Distance must be >= 1.")
                : GetNeighbours(c, (int)Math.Ceiling(distance)).Where(t => CubeCoordinates.GetStraightLineDistance(c, t.CubeCoOrds) <= distance);
        public bool HasUnoccupiedNeighbours(CubeCoordinates c, int distance = 1) => GetNeighbours(c, distance).Any(t => !t.IsOccupied);

        public IEnumerable<Unit> GetUnits() => Units;
        public IEnumerable<T> GetUnits<T>() where T : Unit => Units.OfType<T>();
        public IEnumerable<T> GetUnits<T>(Predicate<Unit> predicate) where T : Unit => GetUnits<T>().Where(u => predicate(u));
        public IEnumerable<Unit> GetUnits(Coordinates p) => GetUnits(GetTile(p));
        public IEnumerable<Unit> GetUnits(Tile t)
        {
            if (t == null)
            {
                Debug.LogError("Error when trying to get unit: input tile is null");
                return Enumerable.Empty<Unit>();
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
        public IEnumerable<T> GetBuildings<T>() where T : Building => Buildings.OfType<T>();
        public IEnumerable<Building> GetBuildings(Coordinates p) => GetBuildings(GetTile(p));
        public IEnumerable<Building> GetBuildings(IEnumerable<Coordinates> coordinates) => coordinates.SelectMany(c => GetBuildings(c));
        public IEnumerable<Building> GetBuildings(Tile t)
        {
            if (t == null)
            {
                Debug.LogError("Error when trying to get building: input tile is null");
                return Enumerable.Empty<Building>();
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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            throw new NotImplementedException();
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
        public int CitiesSeed { get; set; } = -1;
        private List<CubeCoordinates> m_flatLands { get; set; }
        private List<CubeCoordinates> m_cities { get; set; } = new List<CubeCoordinates>();

        public RandomMap() { }

        public RandomMap(int width, int height, int num_player, string battle_name, string name) : base(width, height, battle_name, name)
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
                        Tiles[x][y] = Game.TileData.GetNew(TileType.BOUNDARY);
                        Tiles[x][y].CoOrds = new Coordinates(x, y);
                        continue;
                    }
                    else if (HeightMap.Values[x][y] <= 0.25)
                    {
                        Tiles[x][y] = Game.TileData.GetNew(TileType.OCEAN);
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
                        Tiles[x][y] = Game.TileData.GetNew(type);
                    }
                    else
                    {
                        Tiles[x][y] = HeightMap.Values[x][y] <= 0.65
                            ? Game.TileData.GetNew(TileType.HILLOCK)
                            : Game.TileData.GetNew(HeightMap.Values[x][y] <= 0.75 ? TileType.HILLS : TileType.MOUNTAINS);
                    }
                    Tiles[x][y].CoOrds = new Coordinates(x, y);
                }
            }
            GenerateRivers();
            GenerateCities(num_player);
            PostGenerateProcesses();
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
                    Tiles[coords.X][coords.Y] = Game.TileData.GetNew(type == TileType.STREAM ? TileType.RIVER : TileType.STREAM);
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
                Tiles[c.X][c.Y] = Game.TileData.GetNew<Cities>("metropolis");
                Tiles[c.X][c.Y].CoOrds = new Coordinates(c);
            }
            m_cities.RemoveRange(0, num_player);

            int num_suburb = (int)(CitiesNum * suburb_ratio);
            for (int i = 0; i < num_suburb; i++)
            {
                int index = new System.Random().Next(m_cities.Count);
                Coordinates c = (Coordinates)m_cities[index];
                Tiles[c.X][c.Y] = Game.TileData.GetNew<Cities>("suburb");
                Tiles[c.X][c.Y].CoOrds = new Coordinates(c);
                m_cities.RemoveAt(index);
            }

            foreach (CubeCoordinates cube in m_cities)
            {
                Coordinates c = (Coordinates)cube;
                Tiles[c.X][c.Y] = Game.TileData.GetNew<Cities>("city");
                Tiles[c.X][c.Y].CoOrds = new Coordinates(c);
            }
        }

        public Stack<CubeCoordinates> PathFind(WeightedTile start, WeightedTile end, decimal weight = -1, decimal max_weight = -1)
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

                // TODO FUT. Impl. it had thrown arithmetic overflow exception once here when testing, need further investigation
                GetNeighbours(check.CubeCoOrds).ToList().ForEach(t => neigbours.Add(new WeightedTile()
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

        public override void Save()
        {
            // TODO
            base.Save();
        }

        public override void Load()
        {
            // TODO
            base.Load();
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

                        // out of bounds
                        if (!(candidate_x > 0 && candidate_y > 0 && candidate_x < Width && candidate_y < Height))
                        {
                            continue;
                        }

                        CubeCoordinates candidate = (CubeCoordinates)new Coordinates(candidate_x, candidate_y);
                        if (!m_flatLands.Any(s => s == candidate)) // not a flatland
                        {
                            continue;
                        }
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

        private void PostGenerateProcesses()
        {
            int i = 0;
            foreach (Metropolis m in GetCities<Metropolis>())
            {
                m.SetOwner(Battle.Instance?.Players[i]);
                i++;
            }
            foreach (Cities c in GetCities())
            {
                Barracks barracks = Game.BuildingData.GetNew<Barracks>();
                Arsenal arsenal = Game.BuildingData.GetNew<Arsenal>();

                barracks.Initialize(c.Owner, new Coordinates(c.CoOrds));
                barracks.CoOrds = new Coordinates(c.CoOrds);
                arsenal.CoOrds = new Coordinates(c.CoOrds);
                if (c.Owner != null)
                {
                    barracks.Owner = c.Owner;
                    arsenal.Owner = c.Owner;
                }
                AddBuildings(barracks, arsenal);
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

    public class Command : ICloneable
    {
        public Coordinates Source { get; set; }
        public Coordinates Destination { get; set; }
        public Unit Unit { get; set; }

        [JsonIgnore] public string Name => GetType().Name;
        [JsonIgnore] public StringBuilder Recorder => new StringBuilder();

        public virtual void Execute() { }

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
            if (Unit == null)
            {
                return;
            }

            decimal supplies = Unit.GetSuppliesRequired(Unit.GetLocatedTile());
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
            if (Unit == null)
            {
                return;
            }

            decimal cartridges = normal ? weapon.ConsumptionNormal.Cartridges.ApplyMod() : weapon.ConsumptionSuppress.Cartridges.ApplyMod();
            decimal shells = normal ? weapon.ConsumptionNormal.Shells.ApplyMod() : weapon.ConsumptionSuppress.Shells.ApplyMod();
            decimal fuel = normal ? weapon.ConsumptionNormal.Fuel.ApplyMod() : weapon.ConsumptionSuppress.Fuel.ApplyMod();

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
            // TODO FUT. Impl.
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
            // not on screen, is active, has coordinates = newly deployed
            IEnumerable<Unit> newly_deployed_units = Map.Instance.GetUnits(u => u.GetObjectOnScene() == null && u.Status.HasFlag(UnitStatus.ACTIVE) && u.CoOrds != default);
            foreach (Unit u in newly_deployed_units)
            {
                u.AddToScene();
            }

            IEnumerable<Building> newly_constructed_buildings = Map.Instance.GetBuildings(b => b.GetObjectOnScene() == null && b.Status == BuildingStatus.UNDER_CONSTRUCTION);
            foreach (Building b in newly_constructed_buildings)
            {
                b.AddToScene();
            }

            IEnumerable<Unit> destroyed_unit = Map.Instance.GetUnits(UnitStatus.DESTROYED);
            destroyed_unit.ToList().ForEach(u => u.RemoveFromScene());
            Map.Instance.RemoveUnits(destroyed_unit);
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
                    decimal observer_recon = observer_tile.TerrainMod.Recon.ApplyTo(u.Scouting.Reconnaissance.ApplyMod());
                    decimal observer_detect = u.Scouting.Detection.ApplyMod();

                    u.GetHostileUnitsInReconRange().ToList().ForEach(h =>
                    {
                        Tile observee_tile = h.GetLocatedTile();
                        decimal st_line_distance = CubeCoordinates.GetStraightLineDistance(u.CubeCoOrds, h.CubeCoOrds);
                        decimal observee_conceal = observee_tile.TerrainMod.Concealment.ApplyTo(h.Scouting.Concealment.ApplyMod());

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
                        decimal st_line_distance = CubeCoordinates.GetStraightLineDistance(u.CubeCoOrds, b.CubeCoOrds);
                        decimal observee_conceal = observee_tile.TerrainMod.Concealment.ApplyTo(b.Scouting.Concealment.ApplyMod());
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