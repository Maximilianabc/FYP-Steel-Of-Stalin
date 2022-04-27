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
using UnityEngine.SceneManagement;
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
using Resources = SteelOfStalin.Attributes.Resources;
using System.Threading.Tasks;
using SteelOfStalin.Assets.Props.Buildings.Infrastructures;
using System.Threading;

namespace SteelOfStalin
{
    //Contains all information of the game itself
    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        // Handles scenes (un)loading using Unity.SceneManager directly (all of its methods are static)
        public static GameSettings Settings { get; set; } = new GameSettings();
        public static List<GameObject> GameObjects { get; set; } = new List<GameObject>();
        public static List<AudioClip> AudioClips { get; set; } = new List<AudioClip>();

        public static List<Sprite> Icons { get; set; } = new List<Sprite>();

        public static PlayerProfile Profile { get; set; } = new PlayerProfile();

        public static List<BattleInfo> BattleInfos { get; set; } = new List<BattleInfo>(); 
        public static BattleInfo ActiveBattle { get; set; }
        public static NetworkManager Network => NetworkManager.Singleton;

        public static UnitData UnitData { get; set; } = new UnitData();
        public static BuildingData BuildingData { get; set; } = new BuildingData();
        public static TileData TileData { get; set; } = new TileData();
        public static CustomizableData CustomizableData { get; set; } = new CustomizableData();

        public static bool AssetsLoaded { get; private set; }
        public static bool NeedReloadBattleObjects { get; set; } = true;

        public void Start()
        {
            LoadAllAssets();
            LoadBattleInfos();
            LoadProfile();
            LoadSettings();
            Network.ConnectionApprovalCallback += ApprovalCheck;
            Instance = this;
        }

        public static void StartHost() => Network.StartHost();

        public static void StartServer() => Network.StartServer();

        public static void StartClient()
        {
            ConnectionAddressData connection = Network.GetComponent<UnityTransport>().ConnectionData;
            // TODO FUT. Impl. change these to player input instead of loopback address in production
            connection.Address = "127.0.0.1";
            connection.Port = 7777;
            Network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(Profile.Name) ? "dummy_123" : Profile.Name); // TODO FUT. Impl. add password here
            Network.StartClient();
        }

        public static void ShutDown()
        {
            NeedReloadBattleObjects = false;

            GameObject network = GameObject.Find("network");
            GameObject battle = GameObject.Find("battle");
            GameObject network_util = GameObject.Find("network_util");
            GameObject player = GameObject.Find(Game.Profile.Name);

            if (network != null)
            {
                DestroyImmediate(network);
            }
            if (battle != null)
            {
                Battle.Instance.CancelTokenSource.Cancel();
                battle.GetComponent<Battle>().StopAllCoroutines();
                DestroyImmediate(battle.gameObject);
            }
            if (network_util != null)
            {
                network_util.GetComponent<NetworkUtilities>().StopAllCoroutines();
                DestroyImmediate(network_util.gameObject);
            }
            if (player != null)
            {
                DestroyImmediate(player);
            }
            _ = Instance.StartCoroutine(WaitForObjectsDestroy(() =>
            {
                Network.Shutdown();
                NeedReloadBattleObjects = true;
            }));
        }

        public static void LoadAllAssets(bool from_dump = false)
        {
            AssetsLoaded = false;
            if (!from_dump)
            {
                GameObjects = UnityEngine.Resources.LoadAll<GameObject>("Prefabs").ToList();
                AudioClips = UnityEngine.Resources.LoadAll<AudioClip>("Audio").ToList();
                Icons = UnityEngine.Resources.LoadAll<Sprite>("Icons").ToList();
            }
            UnitData.Load(from_dump);
            BuildingData.Load(from_dump);
            TileData.Load(from_dump);
            CustomizableData.Load(from_dump);
            AssetsLoaded = true;
        }

        // TODO FUT. Impl. Handle corrupted save files
        public static void LoadBattleInfos()
        {
            foreach (string save in Directory.GetDirectories(SavesFolder))
            {
                string battle_name = Path.GetFileName(save);
                string map_path = GetRelativePath(ExternalFolder.SAVES, battle_name, "map");
                string[] lines;
                try
                {
                    lines = ReadTxt(AppendPath(map_path, "stats"));
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
                    Rules = DeserializeJson<BattleRules>(AppendPath(save, "rules"), false)
                });
            }
        }

        public static void LoadProfile()
        {
            if (StreamingAssetExists("profile.json"))
            {
                Profile = DeserializeJson<PlayerProfile>("profile");
#if UNITY_EDITOR
                Profile.Name = $"random_{Utilities.Random.Next()}";
#endif
            }
            else
            {
                Debug.Log("No profile is found");
            }
        }

        public static void LoadSettings() => Settings = DeserializeJson<GameSettings>("settings");

        public static void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
        {
            // TODO FUT. Impl. sanitize the data because it is passed via network
            string player_name = Encoding.UTF8.GetString(connectionData);

            Battle current_battle = Battle.Instance;

            if (clientId != 0)
            {
                bool has_vacancies = !current_battle.IsServerFull;
                bool is_existing_player = current_battle.GetPlayer(player_name) != null;
                bool approve = has_vacancies || is_existing_player;

                if (approve)
                {
                    Debug.Log($"Approved connection from client (id: {clientId}, name: {player_name})");
                    if (!is_existing_player)
                    {
                        Player connected_player = new Player()
                        {
                            Name = player_name,
                            SerializableColor = (SerializableColor)current_battle.GetRandomAvailablePlayerColor(),
                            Resources = (Resources)current_battle.Rules.StartingResources.Clone()
                        };
                        current_battle.Players.Add(connected_player);
                    }
                    current_battle.ConnectedPlayerIDs.Add(clientId, player_name);
                }
                else
                {
                    Debug.Log($"Rejected a connection: server is full");
                }
                // note: it creates duplicated player objects on scene, not sure what causing this
                callback(false, null, approve, null, null);
            }
            else
            {
                Debug.Log("Skipped approval check for host client (id: 0)");
                callback(false, null, true, null, null);
            }
        }

        private static IEnumerator WaitForObjectsDestroy(Action callback)
        {
            Debug.Log("Waiting for battle and network_util to be destroyed");
            yield return new WaitWhile(() => GameObject.Find("battle") != null);
            yield return new WaitWhile(() => GameObject.Find("network_util") != null);
            Debug.Log("battle and network_util destroyed");
            callback.Invoke();
        }
    }

    public class GameSettings
    {
        public bool EnableAnimations { get; set; }
        public byte VolumeMusic { get; set; } = 100;
        public byte VolumeSoundFX { get; set; } = 100;
        public bool Fullscreen { get; set; } = false;
        public int ResolutionX { get; set; } = 1366;
        public int ResolutionY { get; set; } = 768;

        public void Save() => this.SerializeJson("settings");
    }

    public class Battle : NetworkBehaviour, INamedAsset
    {
        public static Battle Instance { get; private set; }

        public string Name { get; set; }
        public Map Map { get; set; } = new Map();
        public List<Player> Players { get; set; } = new List<Player>();
        public BattleRules Rules { get; set; } = new BattleRules();
        public int MaxNumPlayers { get; set; }

        public List<Round> Rounds = new List<Round>();
        public Round CurrentRound { get; set; }
        public int RoundNumber { get; set; } = 1;
        public int TimeRemaining { get; set; }
        public bool EnablePlayerInput { get; private set; } = true;
        public bool IsSinglePlayer { get; private set; }
        public bool IsEnded => m_winner != null;

        private bool m_abort { get; set; }

        [JsonIgnore] public Player Self { get; set; }
        [JsonIgnore] public IEnumerable<AIPlayer> Bots => Players.OfType<AIPlayer>();
        [JsonIgnore] public IEnumerable<Player> ActivePlayers => Players.Where(p => !p.IsDefeated);
        [JsonIgnore] public bool AreAllPlayersReady => ActivePlayers.All(p => p.IsReady);
        [JsonIgnore] public bool AreAllPlayersReadyToStart => Players.All(p => p.IsReady);
        [JsonIgnore] public bool IsServerFull => Game.Network.IsServer && Players.Count >= MaxNumPlayers;
        [JsonIgnore] public bool AreCapitalsSet { get; private set; } = false;
        [JsonIgnore] public NetworkUtilities NetworkUtilities { get; set; }

        [JsonIgnore] public CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();

        private Player m_winner { get; set; } = null;
        private bool m_isInitialized => Players.Count > 0 && Map.IsInitialized;
        private bool m_isLoaded = false;
        private string m_folder => AppendPath(FolderNames[ExternalFolder.SAVES], Name);
        private List<Color> m_availableColors { get; set; } = new List<Color>(CommonColors);

        private Dictionary<ulong, string> _playerIDs = new Dictionary<ulong, string>();
        [JsonIgnore] public Dictionary<ulong, string> ConnectedPlayerIDs 
        {
            get => _playerIDs; 
            set
            {
                if (Game.Network.IsServer)
                {
                    _playerIDs = value;
                }
            }
        }

        private void Start()
        {
            Instance = this;
            NetworkUtilities = FindObjectOfType<NetworkUtilities>();
            _ = StartCoroutine(Initialize());
        }

        private IEnumerator GameLoop()
        {
            // main game logic loop
            while (m_winner == null)
            {
                CurrentRound = new Round();
                ActivePlayers.ToList().ForEach(p => p.IsReady = false);
                Debug.Log("after setting players as not ready");
                CurrentRound.InitializeRoundStart();
                Debug.Log("after initializing round start");
                CurrentRound.CommandPrerequisitesChecking();
                Debug.Log("CommandPrerequisitesChecking");
                EnablePlayerInput = true;
                TimeRemaining = Rules.TimeForEachRound;
                yield return new WaitForEndOfFrame();

                bool unlimited_time = Rules.TimeForEachRound < 0;
                if (unlimited_time)
                {
                    Debug.Log("No time limit. Wait for all players ready to continue");
                }
                
                // TODO FUT. Impl. add bots for multi
                // TODO FUT. Impl. bot flows should be running in parallel with players' decisions to ensure fairness
                if (Game.ActiveBattle.IsSinglePlayer)
                {
                    foreach (AIPlayer bot in Bots)
                    {
                        Debug.Log("Execute bot flow");
                        bot.Botflow();
                    }
                }
                UIUtil.instance.RoundStartUIUpdate();

                int counter = 0;
                if (!unlimited_time)
                {
                    while (counter < Rules.TimeForEachRound && !AreAllPlayersReady)
                    {
                        yield return new WaitForSeconds(1);
                        counter += 1;
                        TimeRemaining--;
                        Debug.Log($"Time remaining: {TimeRemaining} second(s)");
                    }
                }
                else
                {
                    Debug.Log("Waiting for all players to be ready");
                    yield return new WaitWhile(() => !AreAllPlayersReady);
                    Debug.Log("All players are ready");
                }
                UIUtil.instance.RoundEndUIUpdate();
                Debug.Log("End Turn");
                EnablePlayerInput = false;
                if (Game.Network.IsServer)
                {
                    if (Game.ActiveBattle.IsSinglePlayer)
                    {
                        foreach (Player player in Players)
                        {
                            CurrentRound.Commands.AddRange(player.Commands);
                        }
                        CurrentRound.EndPlanning();
                        m_winner = CurrentRound.GetWinner();
                        Rounds.Add(CurrentRound);
                        CurrentRound.ReadyToProceedToNext = true;
                    }
                    else
                    {
                        CurrentRound.Commands.AddRange(Self.Commands);
                        CurrentRound.NumPlayersCommandReceived++;
                        Debug.Log("Waiting for all players to send their commands");
                        yield return new WaitWhile(() => !CurrentRound.ReadyToExecutePhases);
                        Debug.Log("Received commands from all players.");

                        CurrentRound.EndPlanning();
                        m_winner = CurrentRound.GetWinner();
                        Rounds.Add(CurrentRound);
                        foreach (Player player in Players)
                        {
                            ClientRpcParams @params = NetworkUtilities.GetClientRpcSendParams(ConnectedPlayerIDs.ReverseLookup(player.Name));
                            List<string> results = CurrentRound.Commands.Where(c => c.RelatedToPlayer(player)).Select(c => c.ToStringAfterExecution()).ToList();

                            // TODO FUT. Impl. consider having separating client rpcs for empty result/units/buildings
                            if (results.Count == 0)
                            {
                                GetCommandResultsClientRpc("", 0, @params);
                            }
                            else
                            {
                                foreach (string result in results)
                                {
                                    GetCommandResultsClientRpc(result, results.Count, @params);
                                }
                            }

                            IEnumerable<Unit> units = Map.GetUnits(player);
                            if (!units.Any())
                            {
                                GetUnitStatusesClientRpc("", 0, @params);
                            }
                            else
                            {
                                foreach (Unit u in units)
                                {
                                    GetUnitStatusesClientRpc(u.ToString(), u.Status, @params);
                                }
                            }

                            IEnumerable<Building> buildings = Map.GetBuildings(player);
                            if (!buildings.Any())
                            {
                                GetBuildingStatusesClientRpc("", 0, @params);
                            }
                            else
                            {
                                foreach (Building b in buildings)
                                {
                                    GetBuildingStatusesClientRpc(b.ToString(), b.Status, @params);
                                }
                            }
                            GetResourcesLeftClientRpc(player.Resources.ToString(), @params);
                        }
                        CurrentRound.NumPlayersReadyToProceed++;
                    }
                }
                else
                {
                    Self.PlayerObjectComponent.SendCommandsToServer();
                    _ = StartCoroutine(WaitForServerCalculationResults());
                }

                Debug.Log("Waiting for proceeding to next round.");
                yield return new WaitWhile(() => !CurrentRound.ReadyToProceedToNext);
                Debug.Log("Ready to proceed to next round.");

                CurrentRound.ScreenUpdate();
                Self.Commands.Clear();
                RoundNumber++;
            }
            Debug.Log($"Winner is {m_winner}!");
            Game.Network.Shutdown();
            SceneManager.LoadScene("Menu");
            yield return null;
        }
        private IEnumerator Initialize()
        {
            yield return new WaitWhile(() => !(Game.Network.IsClient || Game.Network.IsServer));
            if (Game.Network.IsHost)
            {
                Debug.Log("Started as host");

                BattleInfo info = Game.ActiveBattle;
                Name = info.Name;
                Rules = info.Rules;
                MaxNumPlayers = info.MaxNumPlayers;
                Map.Name = info.MapName;
                Map.Width = info.MapWidth;
                Map.Height = info.MapHeight;
                IsSinglePlayer = info.IsSinglePlayer;
                LoadTask();
                if (!m_abort)
                {
                    _ = StartCoroutine(HostPostInitialization());
                }
                else
                {
                    Debug.Log("Abort");
                    yield break;
                }
            }
            else if (Game.Network.IsClient)
            {
                Debug.Log("Started as client");
            }
            else
            {
                Debug.LogError("Something went wrong: NetworkManager is neither host nor client.");
                yield break;
            }
            _ = StartCoroutine(WaitForGameStart());
            yield return null;
        }
        private IEnumerator HostPostInitialization()
        {
            yield return new WaitWhile(() => Self == null);
            if (string.IsNullOrEmpty(Self.Name))
            {
                Debug.LogWarning("Profile name is null");
            }
            ConnectedPlayerIDs = new Dictionary<ulong, string>()
            {
                [0] = Self.Name
            };
            NetworkManager.OnClientConnectedCallback += OnClientConnected;

            // remove colors of existing players from available colors
            m_availableColors = m_availableColors.Except(Players.Select(p => p.Color)).ToList();
            Debug.Log("Post initialization finished");
            yield return null;
        }
        private IEnumerator WaitForGameStart()
        {
            Debug.Log("Waiting for map initialization");
            yield return new WaitWhile(() => !m_isInitialized);
            Debug.Log($"Map {Map.Name} initialized");
            //TODO: add handler for failed async load
            //TODO FUT. Impl. Cope with async loading
            //AsyncOperation operation = SceneManager.LoadSceneAsync("Game");
            //operation.allowSceneActivation = false;
            //TODO: wait for all players to load the battle for fairness
            Debug.Log("Waiting for loading battle");
            yield return new WaitWhile(() => !m_isLoaded);
            Debug.Log($"Battle {Name} loaded");

            Debug.Log("Waiting for all players to be connected");
            yield return new WaitWhile(() => (Game.Network.IsClient || Game.ActiveBattle.IsSinglePlayer) ? Players.Count != MaxNumPlayers : ConnectedPlayerIDs.Count != MaxNumPlayers);
            Debug.Log("All players connected");

            Debug.Log("Waiting for all players to be ready");
            yield return new WaitWhile(() => !AreAllPlayersReadyToStart);
            Debug.Log("All players are ready");
            //operation.allowSceneActivation = true;
            SceneManager.LoadScene("Game");
            yield return new WaitWhile(() => SceneManager.GetActiveScene() != SceneManager.GetSceneByName("Game"));
            AddPropsToScene();
            _ = StartCoroutine(GameLoop());
        }
        private IEnumerator WaitForAllDataSet(Func<bool> wait_condition)
        {
            yield return new WaitWhile(wait_condition);
            m_isLoaded = true;
            yield return null;
        }
        private IEnumerator WaitForServerCalculationResults()
        {
            Debug.Log("Waiting for all command results to be processed");
            yield return new WaitWhile(() => !Self.AllCommandsProcessed);
            Debug.Log("All commands processed");

            Self.CommandsProcessed = 0;
            Self.AllCommandsProcessed = false;

            SignifyReadyProceedServerRpc(NetworkUtilities.GetServerRpcParams());

            yield return null;
        }
        private IEnumerator SendMapDetailsToClient(ulong id, ClientRpcParams send_params, Action callback)
        {
            yield return new WaitWhile(() => !Map.IsLoaded);
            NetworkUtilities.SendMessageFromHostByRpc(Map.GetTilesUnflatterned(), send_params);
            // TODO FUT. Impl. send only units and buildings which belong to and currently spotted to the client
            NetworkUtilities.SendNamedMessage(Map.GetUnits(), id, NetworkMessageType.DATA);
            NetworkUtilities.SendNamedMessage(Map.GetBuildings(), id, NetworkMessageType.DATA);
            callback.Invoke();
            yield return null;
        }
        private IEnumerator InvokeClientRpcAfterSendingData(ulong id, ClientRpcParams send_params, Func<bool> wait_flag)
        {
            yield return new WaitWhile(wait_flag);
            Debug.Log($"Sent all data to client (id: {id}, name: {ConnectedPlayerIDs[id]})");

            SetAllDataClientRpc(send_params);

            Debug.Log($"Invoke all client rpc to update player list");
            NetworkUtilities.SendMessageFromHostByRpc(Players);
            UpdatePlayerListAllClientRpc(MaxNumPlayers);
            yield return null;
        }

        private async void LoadTask()
        {
            Task load_task = Task.Run(() => Load(), CancelTokenSource.Token);
            try
            {
                await load_task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Load cancelled");
                m_abort = true;
            }
            finally
            {
                CancelTokenSource.Dispose();
            }
        }
        private void AddPropsToScene()
        {
            IEnumerable<Metropolis> metropolis = Map.GetCities<Metropolis>();
            if (metropolis.All(m => string.IsNullOrEmpty(m.OwnerName)))
            {
                Map.SetMetropolisOwners();
                Map.InitializeDefaultUnitBuildings();
            }
            AreCapitalsSet = true;

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
                    GameObject ownable_object = ownable_prop.PropObject;
                    if (ownable_object == null)
                    {
                        Debug.LogError($"Cannot get object {ownable_prop.MeshName} on screen");
                        continue;
                    }
                    ownable_prop.PropObjectComponent.SetColorForChild(GetPlayer(ownable.OwnerName).Color, ownable_prop.Name);
                }
            }
        }

        public Player GetPlayer(string name) => Players.Find(p => p.Name == name);
        public Player GetPlayer(Color color) => Players.Find(p => p.Color == color);
        public Player GetPlayer(ulong id) => Game.Network.IsServer ? GetPlayer(ConnectedPlayerIDs[id]) : null;

        // TODO FUT. Impl. REMOVE THIS IN PRODCUTION, FOR TESTING PURPOSE ONLY
        public void SetWinner(Player player) => m_winner = player;

        public void Save()
        {
            Rules.Save();
            Players.SerializeJson(AppendPath(m_folder, "players"));
            Map.Save();
            Debug.Log($"Saved battle {Name}");
        }
        public void Load()
        {
            CancelTokenSource.Token.ThrowIfCancellationRequested();

            if (!StreamingAssetExists(AppendPath(m_folder, "rules.json")))
            {
                Rules.Save();
            }
            Rules = DeserializeJson<BattleRules>(AppendPath(m_folder, "rules"));

            Players = DeserializeJson<List<Player>>(AppendPath(m_folder, "players"));
            if (Players.Count == 0)
            {
                Self = new Player()
                {
                    Name = Game.Profile.Name,
                    SerializableColor = (SerializableColor)GetRandomAvailablePlayerColor(),
                    Resources = (Resources)Rules.StartingResources.Clone()
                };
                Players.Add(Self);
                Debug.Log("Added self to Players");

                if (IsSinglePlayer)
                {
                    foreach (AIPlayer ai in Player.NewDummyPlayers(MaxNumPlayers - Players.Count))
                    {
                        Players.Add(ai);
                        Debug.Log($"Added bot {ai.Name} to Players");
                    }
                }
            }

            CancelTokenSource.Token.ThrowIfCancellationRequested();

            Map.Load();
            Debug.Log($"Loaded battle {Name}");
            m_isLoaded = true;
        }

        public Color GetRandomAvailablePlayerColor()
        {
            int index = Utilities.Random.Next(m_availableColors.Count);
            Color color = m_availableColors[index];
            m_availableColors.RemoveAt(index);
            return color;
        }

        private void OnClientConnected(ulong id)
        {
            Debug.Log($"Client (id: {id}) connected");
            ClientRpcParams send_params = NetworkUtilities.GetClientRpcSendParams(id);
            ClientRpcParams send_params_except_host = NetworkUtilities.GetClientRpcSendParams(ConnectedPlayerIDs.Keys.Where(id => id != Game.Network.LocalClientId));

            NetworkUtilities.SendDumpFiles(Game.UnitData.LocalJsonFilePaths, send_params);
            NetworkUtilities.SendDumpFiles(Game.BuildingData.LocalJsonFilePaths, send_params);
            NetworkUtilities.SendDumpFiles(Game.TileData.LocalJsonFilePaths, send_params);
            NetworkUtilities.SendDumpFiles(Game.CustomizableData.LocalJsonFilePaths, send_params);

            // send map info
            NetworkUtilities.SendNamedMessage(Map, id, NetworkMessageType.DATA);
            // NetworkUtilities.SendNamedMessage(Map.GetTilesUnflatterned(), id, NetworkMessageType.DATA);
            // NOTE: if sending a named message that is too long (like whole map), the handle of the reader on client side won't be able to accessed and throws NRE continuously
            bool map_sent = false;
            _ = StartCoroutine(SendMapDetailsToClient(id, send_params, () => map_sent = true));
            NetworkUtilities.SendNamedMessage(Rules, id, NetworkMessageType.DATA);

            _ = StartCoroutine(InvokeClientRpcAfterSendingData(id, send_params, () => !map_sent));
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void SetAllDataClientRpc(ClientRpcParams @params)
        {
            bool dump_loaded = false;
            bool map_basic_info_loaded = false;
            bool tiles_set = false;
            bool units_set = false;
            bool buildings_set = false;
            bool rules_set = false;

            _ = StartCoroutine(NetworkUtilities.TrySaveFiles(() =>
            {
                Game.LoadAllAssets(true);
                dump_loaded = true;
            }));

            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<Map>(m => m.MessageType == NetworkMessageType.DATA, result =>
            {
                Map = result;
                map_basic_info_loaded = true;
            }));

            _ = StartCoroutine(NetworkUtilities.TryGetRpcMessage<Tile[][]>(result => 
            { 
                Map.SetTiles(result); 
                tiles_set = true;
            }));

            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<IEnumerable<Unit>>(m => m.MessageType == NetworkMessageType.DATA, result => 
            { 
                Map.SetUnits(result); 
                units_set = true;
            }));

            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<IEnumerable<Building>>(m => m.MessageType == NetworkMessageType.DATA, result => 
            { 
                Map.SetBuildings(result); 
                buildings_set = true;
            }));

            _ = StartCoroutine(NetworkUtilities.TryGetNamedMessage<BattleRules>(m => m.MessageType == NetworkMessageType.DATA, result => 
            { 
                Rules = result; 
                rules_set = true;
            }));

            // TODO FUT. Impl. add coroutine utilities for simple wait / do coroutines
            _ = StartCoroutine(WaitForAllDataSet(() => !(dump_loaded || map_basic_info_loaded || tiles_set || units_set || buildings_set || rules_set)));
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdatePlayerListAllClientRpc(int max_num_players)
        {
            _ = StartCoroutine(NetworkUtilities.TryGetRpcMessage<List<Player>>(result =>
            {
                Players = result;
                if (Self == null)
                {
                    Self = GetPlayer(Game.Profile.Name);
                }
            }));
            MaxNumPlayers = max_num_players;
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void GetCommandResultsClientRpc(string result, int num_to_receive, ClientRpcParams @params)
        {
            Debug.Log($"Command result {result} received.");
            if (!string.IsNullOrEmpty(result))
            {
                // TODO apply changes from commands
                Self.CommandsProcessed++;
            }
            if (Self.CommandsProcessed == num_to_receive)
            {
                Self.AllCommandsProcessed = true;
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void GetUnitStatusesClientRpc(string unit, UnitStatus status, ClientRpcParams @params)
        {
            if (!string.IsNullOrEmpty(unit))
            {
                ((Unit)Map.Instance.GetProp(unit)).Status = status;
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void GetBuildingStatusesClientRpc(string building, BuildingStatus status, ClientRpcParams @params)
        {
            if (!string.IsNullOrEmpty(building))
            {
                ((Building)Map.Instance.GetProp(building)).Status = status;
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void GetResourcesLeftClientRpc(string resources, ClientRpcParams @params)
        {
            Debug.Log("Updated resources received");
            Self.Resources.UpdateFromString(resources);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ProceedToNextRoundPushClientRpc()
        {
            CurrentRound.ReadyToProceedToNext = true;
        }

        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void SignifyReadyProceedServerRpc(ServerRpcParams @params)
        {
            Debug.Log($"player {ConnectedPlayerIDs[@params.Receive.SenderClientId]} ready to proceed");
            CurrentRound.NumPlayersReadyToProceed++;
            if (CurrentRound.NumPlayersReadyToProceed == ConnectedPlayerIDs.Count)
            {
                CurrentRound.ReadyToProceedToNext = true;
                ProceedToNextRoundPushClientRpc();
            }
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
        public bool IsSinglePlayer { get; set; } = false;
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

        public Resources StartingResources { get; set; } = (Resources)Resources.TEST.Clone();

        public BattleRules() { }

        public void Save() => this.SerializeJson(AppendPath(Battle.Instance.Name, "rules"), ExternalFolder.SAVES);
        public void Save(string battle_name) => this.SerializeJson(AppendPath(battle_name, "rules"), ExternalFolder.SAVES);
    }

    public class Map : ICloneable, INamedAsset
    {
        public static Map Instance { get; private set; }
        public string Name { get; set; }
        public string BattleName { get; set; } = "test";
        public int Width { get; set; }
        public int Height { get; set; }

        [JsonIgnore] public List<Player> Players => Battle.Instance.Players;
        [JsonIgnore] public IEnumerable<Prop> AllProps => CombineAll<Prop>(Tiles.Flatten(), Units, Buildings);

        protected Tile[][] Tiles { get; set; }
        protected List<Unit> Units { get; set; } = new List<Unit>();
        protected List<Building> Buildings { get; set; } = new List<Building>();

        [JsonIgnore] public bool IsInitialized => Tiles != null && Width != 0 && Height != 0;
        [JsonIgnore] public bool IsLoaded { get; private set; } = false;
        // TODO FUT. Impl. add more validity check
        [JsonIgnore] public bool IsValid => Tiles != null && GetTiles().Count() == Width * Height;
        protected string Folder => GetRelativePath(ExternalFolder.SAVES, BattleName, "map");
        protected string TileFolder => AppendPath(Folder, "tiles");

        public Map() => Instance = this;
        public Map(int width, int height) => (Width, Height, BattleName, Instance) = (width, height, Battle.Instance?.Name ?? "test", this);
        // for new battle
        public Map(int width, int height, string battle_name, string name) => (Width, Height, BattleName, Name, Instance) = (width, height, battle_name, name, this);

        public virtual void Save()
        {
            CreateStreamingAssetsFolder(Folder);
            Units.SerializeJson(AppendPath(Folder, "units"));
            Buildings.SerializeJson(AppendPath(Folder, "buildings"));
            SaveToTxt(AppendPath(Folder, "stats"), GetStatistics());
            SaveToPng(AppendPath(Folder, "minimap"), Visualize());

            CreateStreamingAssetsFolder(TileFolder);
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i].SerializeJson(AppendPath(TileFolder, $"map_{i}"));
            }
            Debug.Log($"Saved map {Name} for {BattleName}");
        }

        public virtual void Load()
        {
            BattleName = Battle.Instance?.Name ?? "test";

            Battle.Instance.CancelTokenSource.Token.ThrowIfCancellationRequested();
            Units = DeserializeJson<List<Unit>>(AppendPath(Folder, "units"));
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

            Battle.Instance.CancelTokenSource.Token.ThrowIfCancellationRequested();
            Buildings = DeserializeJson<List<Building>>(AppendPath(Folder, "buildings"));
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
                Battle.Instance.CancelTokenSource.Token.ThrowIfCancellationRequested();
                // TODO handle FileIO exceptions for all IO operations
                // TODO FUT. Impl. regenerate map files by reading the stats.txt in case any map files corrupted (e.g. edge of map is not boundary etc.)
                Tiles[i] = DeserializeJson<List<Tile>>(AppendPath(TileFolder, $"map_{i}")).ToArray();
            }
            foreach (Tile t in Tiles.Flatten())
            {
                if (t is Cities c && !string.IsNullOrEmpty(c.OwnerName))
                {
                    c.SetOwnerFromName();
                }
                t.SetMeshName();
            }

            IsLoaded = true;
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
            string[] lines = ReadTxt(AppendPath(Folder, "stats"));
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

        // for host with new generated map
        public void SetMetropolisOwners()
        {
            int i = 0;
            foreach (Metropolis m in GetCities<Metropolis>())
            {
                m.SetOwner(Players[i]);
                i++;
            }
        }
        public void InitializeDefaultUnitBuildings()
        {
            IEnumerable<Metropolis> metro = GetCities<Metropolis>();
            foreach (Metropolis m in metro)
            {
                foreach (Building building in Map.Instance.GetBuildings(m.CoOrds))
                {
                    if (building is UnitBuilding ub)
                    {
                        ub.Initialize(m.Owner, m.CoOrds);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a unit but does not initialize it
        /// </summary>
        /// <param name="u">Unit to be added</param>
        /// <returns>If addition successful, returns true, else returns false</returns>        
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
        
        /// <summary>
        /// Adds a building but does not initialize it
        /// </summary>
        /// <param name="b">Building to be added</param>
        /// <returns>If addition successful, returns true, else returns false</returns>
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

        /// <summary>
        /// Adds an array of buildings, but does not initialize them
        /// </summary>
        /// <param name="buildings">Buildings to be added</param>
        /// <returns></returns>
        public void AddBuildings(params Building[] buildings) => Buildings.AddRange(buildings);

        /// <summary>
        /// Removes a unit
        /// </summary>
        /// <param name="u">Unit to be removed</param>
        /// <returns>If removal successful, returns true, else returns false</returns>
        public bool RemoveUnit(Unit u)
        {
            if (u == null)
            {
                Debug.LogWarning("Cannot remove unit from map unit list: unit is null");
                return false;
            }
            return Units.Remove(u);
        }

        /// <summary>
        /// Removes a building
        /// </summary>
        /// <param name="b">Building to be removed</param>
        /// <returns>If removal successful, returns true, else returns false</returns>
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

        
        /// <summary>
        /// Prints unit list
        /// </summary>
        /// <param></param>
        /// <returns></returns>
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
        
        /// <summary>
        /// Prints building list
        /// </summary>
        /// <param></param>
        /// <returns></returns>
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

        
        /// <summary>
        /// Gets Prop from a GameObject
        /// </summary>
        /// <param name="gameObject">GameObject to get Prop from</param>
        /// <returns>Prop that shares MeshName with gameObject name</returns>
        public Prop GetProp(GameObject gameObject) => AllProps.Find(p => p.MeshName == gameObject.name);
        
        public Prop GetProp(string to_string_name) => AllProps.Find(p => p.ToString() == to_string_name);

        /// <summary>
        /// Gets all Props from a Coordinate
        /// </summary>
        /// <param name="c">A Coordinate</param>
        /// <returns>Props at given Coordinate</returns>
        public IEnumerable<Prop> GetProps(Coordinates c) => AllProps.Where(p => p.CoOrds == c);
        
        /// <summary>
        /// Gets all Props from a CubeCoordinate
        /// </summary>
        /// <param name="c">A CubeCoordinate</param>
        /// <returns>Props at a given CubeCoordinate</returns>
        public IEnumerable<Prop> GetProps(CubeCoordinates c) => AllProps.Where(p => p.CubeCoOrds == c);
        
        /// <summary>
        /// Wildcard function that gets all Props via predicate
        /// </summary>
        /// <param name="predicate">A Predicate</param>
        /// <returns>Props that match predicate</returns>
        public IEnumerable<Prop> GetProps(Predicate<Prop> predicate) => AllProps.Where(p => predicate(p));
        
        /// <summary>
        /// Gets all Props of a specific type
        /// </summary>
        /// <param></param>
        /// <returns>Props of a specific type</returns>
        public IEnumerable<T> GetProps<T>() where T : Prop => AllProps.OfType<T>();
        
        /// <summary>
        /// Gets all Props of a specific type from a CubeCoordinate
        /// </summary>
        /// <param name="c">A CubeCoordinate</param>
        /// <returns>Props of a specific type at given CubeCoordinate</returns>
        public IEnumerable<T> GetProps<T>(CubeCoordinates c) where T : Prop => GetProps(c).OfType<T>();
        
        /// <summary>
        /// Wildcard function that gets all Props of a specific type via predicate
        /// </summary>
        /// <param name="predicate">A Predicate</param>
        /// <returns>Props of a specific type that match predicate</returns>
        public IEnumerable<T> GetProps<T>(Predicate<T> predicate) where T : Prop => AllProps.OfType<T>().Where(p => predicate(p));

        /// <summary>
        /// Gets a Tile from position x, y
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <returns>Tile at position x, y</returns>
        public Tile GetTile(int x, int y) => Tiles[x][y];
        
        /// <summary>
        /// Gets a Tile from position x, y
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <returns>Tile at position x, y</returns>
        public Tile GetTile(Coordinates p) => Tiles[p.X][p.Y];
        
        /// <summary>
        /// Gets a Tile at a CubeCoordinate
        /// </summary>
        /// <param name="c">A CubeCoordinate</param>
        /// <returns>Tile at a given CubeCoordinate</returns>
        public Tile GetTile(CubeCoordinates c) => Tiles[((Coordinates)c).X][((Coordinates)c).Y];
        
        /// <summary>
        /// Gets all Tiles
        /// </summary>
        /// <param></param>
        /// <returns>All Tiles</returns>
        public IEnumerable<Tile> GetTiles() => Tiles.Flatten();
        
        /// <summary>
        /// Gets all Tiles of a given TileType
        /// </summary>
        /// <param name="type">A TileType</param>
        /// <returns>Tiles of a given TileType</returns>
        public IEnumerable<Tile> GetTiles(TileType type) => Tiles.Flatten().Where(t => t.Type == type);
        
        /// <summary>
        /// Wildcard function that gets all Tiles that match a given predicate
        /// </summary>
        /// <param name="predicate">A Predicate</param>
        /// <returns>Tiles that match predicate</returns>
        public IEnumerable<Tile> GetTiles(Predicate<Tile> predicate) => Tiles.Flatten().Where(t => predicate(t));
        
        /// <summary>
        /// Gets all Tiles of specific type
        /// </summary>
        /// <param></param>
        /// <returns>Tiles of a specific type</returns>
        public IEnumerable<T> GetTiles<T>() where T : Tile => Tiles.Flatten().OfType<T>();
        public Tile[][] GetTilesUnflatterned() => Tiles;

        /// <summary>
        /// Gets all Cities
        /// </summary>
        /// <param></param>
        /// <returns>All Cities</returns>
        public IEnumerable<Cities> GetCities() => GetTiles<Cities>();

        /// <summary>
        /// Gets all Cities controlled by a given player
        /// </summary>
        /// <param name="player">A player</param>
        /// <returns>Cities controlled by a given player</returns>
        public IEnumerable<Cities> GetCities(Player player) => GetCities().Where(c => c.Owner == player);
        
        /// <summary>
        /// Wildcard function that gets all Cities that match a given predicate
        /// </summary>
        /// <param name="predicate">A Predicate</param>
        /// <returns>Cities that match predicate</returns>
        public IEnumerable<Cities> GetCities(Predicate<Cities> predicate) => GetCities().Where(c => predicate(c));
        
        /// <summary>
        /// Gets all Cities of specific type
        /// </summary>
        /// <param></param>
        /// <returns>Cities of a specific type</returns>
        public IEnumerable<T> GetCities<T>() where T : Cities => Tiles.Flatten().OfType<T>();

        /// <summary>
        /// Gets neighbouring Tiles of a Tile at a given CubeCoordinate
        /// </summary>
        /// <param name="c">A CubeCoordinate specifying location of tile</param>
        /// <returns>Neighbouring Tiles</returns>
        public IEnumerable<Tile> GetNeighbours(CubeCoordinates c, int distance = 1, bool include_self = false) => c.GetNeighbours(distance, include_self).Where(c =>
        {
            Coordinates p = (Coordinates)c;
            return p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
        }).Select(c => GetTile(c));
        
        /// <summary>
        /// Gets straight line neighbouring Tiles of a Tile at a given CubeCoordinate
        /// </summary>
        /// <param name="c">A CubeCoordinate specifying location of tile</param>
        /// <returns>Straight line neighbouring Tiles</returns>
        public IEnumerable<Tile> GetStraightLineNeighbours(CubeCoordinates c, decimal distance = 1) => distance < 1
                ? throw new ArgumentException("Distance must be >= 1.")
                : GetNeighbours(c, (int)Math.Ceiling(distance)).Where(t => CubeCoordinates.GetStraightLineDistance(c, t.CubeCoOrds) <= distance);
        
        public Tile GetRandomNeighbour(CubeCoordinates c, int distance = 1)
            => GetTile(Utilities.Random.NextItem(c.GetNeighbours(distance)));

        /// <summary>
        /// Function querying a tile for unoccupied neighbours
        /// </summary>
        /// <param name="c">A CubeCoordinate specifying location of main tile</param>
        /// <returns>If there are unoccupied neighbouring Tiles, returns true, else false </returns>
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

            List<Unit> units = Units.Where(u => u.Status.HasAnyOfFlags(UnitStatus.IN_FIELD) && u.CoOrds.X == t.CoOrds.X && u.CoOrds.Y == t.CoOrds.Y).ToList();
            if (units.Count > 2 || (units.Count == 2 && units[0].IsOfSameCategory(units[1])))
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

        #region testing methods
        public T InitializeNewUnit<T>(Player owner, Coordinates c) where T : Unit
        {
            Unit u = Game.UnitData.GetNew<T>();
            u.Initialize(owner, c, UnitStatus.ACTIVE);
            _ = AddUnit(u);
            return (T)u;
        }
        public T InitializeNewBuilding<T>(Player owner, Coordinates c) where T : Building
        {
            Building b = Game.BuildingData.GetNew<T>();
            b.Initialize(owner, c, BuildingStatus.ACTIVE);
            _ = AddBuilding(b);
            return (T)b;

        }
        #endregion

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
        public int CitiesSeed { get; set; } = -1;
        private List<CubeCoordinates> m_flatLands { get; set; }
        private List<CubeCoordinates> m_cities { get; set; } = new List<CubeCoordinates>();

        public RandomMap() { }

        public RandomMap(int width, int height, int num_player, string battle_name, string name) : base(width, height, battle_name, name)
        {
            HeightMap = new PerlinMap(width, height, seed_x: Utilities.Random.Next(-(1 << 16), 1 << 16), seed_y: Utilities.Random.Next(-(1 << 16), 1 << 16));
            HumidityMap = new PerlinMap(width, height, seed_x: Utilities.Random.Next(-(1 << 16), 1 << 16), seed_y: Utilities.Random.Next(-(1 << 16), 1 << 16));

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
            PickCities(num_player, Math.Max(Width, Height) / num_player, max_sep);
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
                int index = Utilities.Random.Next(m_cities.Count);
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
                    CubeCoordinates c = m_flatLands[Utilities.Random.Next(m_flatLands.Count)];
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
                        int rand_dist = Utilities.Random.Next(min_sep, max_sep);
                        double rand_theta = Utilities.Random.NextDouble() * 2 * Math.PI;

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
                        has_cities_within_range = candidate.GetNeighbours(min_sep).Intersect(m_cities).Any();

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
            Debug.Log("Adding default barracks and arsenals");
            foreach (Cities c in GetCities())
            {
                Barracks barracks = Game.BuildingData.GetNew<Barracks>();
                Arsenal arsenal = Game.BuildingData.GetNew<Arsenal>();
                barracks.CoOrds = new Coordinates(c.CoOrds);
                arsenal.CoOrds = new Coordinates(c.CoOrds);
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

    public enum SpecialCommandResult
    {
        NONE,
        MOVE_CONFLICT_NO_MOVE,
        CAPTURE_FRIENDLY,
        MISSED,
        EVADED,

    }

    public class Command : ICloneable
    {
        public Coordinates Source { get; set; }
        public Coordinates Destination { get; set; }
        public Unit Unit { get; set; }

        [JsonIgnore] public string Name => GetType().Name;
        [JsonIgnore] public string Symbol => m_symbols[GetType()];
        [JsonIgnore] public StringBuilder Recorder { get; set; } = new StringBuilder();
        [JsonIgnore] public bool IsValid { get; protected set; }

        private static Dictionary<Type, string> m_symbols => new Dictionary<Type, string>()
        {
            [typeof(Hold)] = "@",
            [typeof(Move)] = "->",
            [typeof(Merge)] = "&",
            [typeof(Submerge)] = "=v",
            [typeof(Surface)] = "=^",
            [typeof(Land)] = "~v",
            [typeof(Fire)] = "!",
            [typeof(Suppress)] = "!!",
            [typeof(Sabotage)] = "!`",
            [typeof(Ambush)] = "!?",
            [typeof(Bombard)] = "!v",
            [typeof(Aboard)] = "|>",
            [typeof(Disembark)] = "<|",
            [typeof(Load)] = "$+",
            [typeof(Unload)] = "$-",
            [typeof(Resupply)] = "#+",
            [typeof(Repair)] = "%+",
            [typeof(Reconstruct)] = "`+",
            [typeof(Fortify)] = "`^",
            [typeof(Construct)] = "`$",
            [typeof(Demolish)] = "`v",
            [typeof(Train)] = "|$",
            [typeof(Deploy)] = "|@",
            [typeof(Rearm)] = "||",
            [typeof(Capture)] = "|v",
            [typeof(Scavenge)] = "|+",
            [typeof(Assemble)] = ".",
            [typeof(Disassemble)] = "..",
        };
        protected static Dictionary<SpecialCommandResult, string> SpecialSymbols => new Dictionary<SpecialCommandResult, string>()
        {
            [SpecialCommandResult.NONE] = "",
            [SpecialCommandResult.MOVE_CONFLICT_NO_MOVE] = "-x->",
            [SpecialCommandResult.CAPTURE_FRIENDLY] = "|^",
            [SpecialCommandResult.MISSED] = "M",
            [SpecialCommandResult.EVADED] = "E",
        };
        public static Dictionary<ulong, List<Command>> CommandsRelatedToPlayer { get; set; } = new Dictionary<ulong, List<Command>>();

        public Command() { }
        public Command(Unit u) => Unit = u;
        public Command(Unit u, Coordinates src, Coordinates dest) => (Unit, Source, Destination) = (u, src, dest);
        public Command(Unit u, int srcX, int srcY, int destX, int destY) => (Unit, Source, Destination) = (u, new Coordinates(srcX, srcY), new Coordinates(destX, destY));

        public virtual void Execute() { }
        public virtual string ToStringBeforeExecution() => $"{Unit} {Symbol} ";
        public virtual string ToStringAfterExecution() => Recorder.ToString();
        public virtual void SetParamsFromString(string initiator, string @params) 
        {
            Debug.Log($"init: {initiator}");
            Prop prop = Map.Instance.GetProp(initiator);
            if (prop != null)
            {
                Unit = (Unit)prop;
                Unit.CommandAssigned = (CommandAssigned)Enum.Parse(typeof(CommandAssigned), GetType().Name.ToUpper());
            }
            else if (!(this is Construct || this is Fortify || this is Demolish || this is Reconstruct))
            {
                Debug.LogError($"No unit is found with name {initiator}, while it is necessary for {GetType().Name} command");
                IsValid = false;
                return;
            }
            IsValid = true;
        }
        public static Command FromStringBeforeExecution(string command_string_without_result)
        {
            Match match = Regex.Match(command_string_without_result, @"^(.*?) ([+\-<>`~!?@#$%^&|v.]{1,2}) (.*?)?$");
            if (!match.Success)
            {
                Debug.LogWarning($"Command string {command_string_without_result} is not in correct format");
                return null;
            }

            string symbol = match.Groups[2].Value;
            Type command_type = m_symbols.ReverseLookup(symbol);
            if (command_type == null)
            {
                Debug.LogError($"No command with symbol {symbol} found.");
                return null;
            }

            Command cmd = (Command)Activator.CreateInstance(command_type);
            string initiator = match.Groups[1].Value;
            string parameters = match.Groups[3].Value;
            if (!string.IsNullOrEmpty(parameters))
            {
                cmd.SetParamsFromString(initiator, parameters);
            }
            else
            {
                cmd.IsValid = true;
            }
            return cmd;
        }
        public virtual bool RelatedToPlayer(Player p) => Unit != null && Unit.Owner == p;

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

        public object Clone()
        {
            Command copy = (Command)MemberwiseClone();
            copy.Unit = (Unit)Unit.Clone();
            return copy;
        }
    }
}

namespace SteelOfStalin.Flow
{
    public class Round : ICloneable
    {
        public int Number { get; set; }

        public List<Command> Commands { get; set; } = new List<Command>();
        public List<Phase> Phases { get; set; } = new List<Phase>();
        public Planning Planning { get; set; } = new Planning();

        [JsonIgnore] public List<Player> Players { get; set; }
        // TODO FUT. Impl. think of a better way to check whether all players have sent their commands to server (probably using enum to indicate status of each client)
        [JsonIgnore] public int NumPlayersCommandReceived { get; set; } = 0;
        [JsonIgnore] public bool ReadyToExecutePhases => NumPlayersCommandReceived == Players.Count;
        [JsonIgnore] public int NumPlayersReadyToProceed { get; set; } = 0;
        [JsonIgnore] public bool ReadyToProceedToNext { get; set; } = false;

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

        public void AmbushStatusHandling()
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
            // not on screen, is active, has coordinates = new (newly deployed / spotted)
            IEnumerable<Unit> new_units = Map.Instance.GetUnits(u => u.PropObject == null && u.Status.HasFlag(UnitStatus.ACTIVE) && u.CoOrds != default);
            foreach (Unit u in new_units)
            {
                u.AddToScene();
            }

            IEnumerable<Building> new_buildings = Map.Instance.GetBuildings(b => !(b is Barracks || b is Arsenal) && b.PropObject == null/* && b.Status == BuildingStatus.UNDER_CONSTRUCTION*/);
            foreach (Building b in new_buildings)
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
            Map.Instance.GetUnits(u => u.Status.HasAnyOfFlags(UnitStatus.IN_FIELD) && u.CommandAssigned == CommandAssigned.NONE).ToList().ForEach(u => Commands.Add(new Hold(u)));
            Phases.AddRange(new Phase[]
            {
                new Firing(Commands),
                new Moving(Commands),
                new CounterAttacking(),
                new Spotting(),
                new Signaling(),
                new Constructing(Commands),
                new Training(Commands),
                new Misc(Commands)
            });
            Phases.ForEach(p => p.Execute());
            Commands.Clear();
            Players.ForEach(p => p.Commands.Clear());
            if (Game.ActiveBattle.IsSinglePlayer)
            {
                ReadyToProceedToNext = true;
            }
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
        protected StringBuilder Recorder { get; set; } = new StringBuilder();
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
        public override string ToString()
        {
            RecordPhase();
            return Recorder.ToString();
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
            /* TODO FUT. Impl. handle edge case: path overlapping with conflict and one of the units has no more tiles left in path
             * e.g. Unit A at tile a moves to tile x, the path contains tile b, where unit B is at
             * Unit B (at tile b) moves to tile x which is a direct neighbouring tile of tile b
             * Unit C moves to tile x, priority value: (C > A > B)
             * after first round conflict resolution, destination of both Unit A and Unit B will be at tile b
             * at second round conflict resolution, unit B's path should remove one more tile but it doesn't have any left
             * it will be very likely to throw at m.Path.Take(...) in this case
             */
            // move commands with the same destination
            IEnumerable<IGrouping<Tile, Move>> conflicts = CommandsForThisPhase.OfType<Move>().GroupBy(m => m.Path.Last()).Where(m => m.Count() > 1);
            Debug.Log($"Conflicts: {conflicts.Count()}");

            int counter = 0; 
            while (conflicts.Any() && counter < 10) // should be less than 10 recursive conflicts (?)
            {
                conflicts
                    .Select(m =>
                        m.OrderBy(x => Formula.PriorityValue(x.Unit, 
                            x.Path.Count() - 2 < 0
                                ? x.Unit.GetLocatedTile() // no second last tile => only one tile in path
                                : x.Path.ElementAt(x.Path.Count() - 2))) // sort each group by priority value
                        .Skip(1)) // skip top one in group as it can occupy the tile
                    .SelectMany(m => m)
                    .ToList().ForEach(m => m.Path = m.Path.Take(m.Path.Count() - 1)); // remove the last tile (original destination) from every command remaining

                // repeat until there's no conflict
                conflicts = CommandsForThisPhase.OfType<Move>()
                    .Where(m => m.Path.Count() > 0) // added because there maybe move commands with no tile in path left
                    .GroupBy(m => m.Path.Last())
                    .Where(m => m.Count() > 1);
                Debug.Log($"Conflicts remaining: {conflicts.Count()}");
                counter++;
            }
            if (counter >= 50)
            {
                this.LogError("Something went wrong in movement conflict resolution: resolution count > 50");
            }

            // auto resupply if any outpost nearby
            foreach (Move move in CommandsForThisPhase.OfType<Move>())
            {
                if (move.Path.Count() == 0 || move.Unit.CarryingIsFull)
                {
                    continue;
                }
                foreach (Tile tile in move.Path)
                {
                    IEnumerable<Tile> neighbours_have_buildings = tile.GetNeighbours(2).Where(t => t.HasBuilding);
                    if (!neighbours_have_buildings.Any())
                    {
                        continue;
                    }
                    foreach (Building b in neighbours_have_buildings.SelectMany(t => Map.Instance.GetBuildings(t.CoOrds)))
                    {
                        if (b is Outpost o && o.Owner == move.Unit.Owner)
                        {
                            // TODO
                        }
                    }
                }
            }

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
                            observee_conceal = h.GetConcealmentPenaltyFire().ApplyTo(observee_conceal);
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
                    // TODO FUT Impl. add acousting ranging logic
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
                if (b.ConstructionTimeRemaining > 0)
                {
                    return;
                }
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
                if (b.BuilderLocation != default)
                {
                    // remove the constructing flag for the builder of this building
                    Map.Instance.GetUnits(b.BuilderLocation).Where(u => u is Personnel && u.Owner == b.Owner).ToList().ForEach(p => p.Status &= ~UnitStatus.CONSTRUCTING);
                    b.BuilderLocation = default;
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
            base.Execute();
            Map.Instance.GetBuildings<UnitBuilding>().Where(ub => ub.Status == BuildingStatus.ACTIVE).ToList().ForEach(ub =>
            {
                foreach (Unit u in ub.TrainingQueue)
                {
                    u.TrainingTimeRemaining -= 1;
                }

                while (ub.TrainingQueue.Count > 0)
                {
                    // retrieve the first unit in queue without removing it from the queue
                    Unit queueing = ub.TrainingQueue.Peek();

                    if (queueing.TrainingTimeRemaining <= 0)
                    {
                        queueing.TrainingTimeRemaining = 0;

                        // if capacity is reached, no more units will be ready to be deployed
                        if (ub.ReadyToDeploy.Count < ub.QueueCapacity)
                        {
                            // retrieve the first unit in queue and remove it from the queue
                            Unit ready = ub.TrainingQueue.Dequeue();

                            // change its status
                            ready.Status = UnitStatus.CAN_BE_DEPLOYED;
                            // add it to deploy list
                            ub.ReadyToDeploy.Add(ready);
                        }
                        continue;
                    }
                    break;
                }
            });
            
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
            // reset weapon fired
            Map.Instance.GetUnits(u => u.WeaponFired != null).ToList().ForEach(u => u.WeaponFired = null);
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
                    if (Utilities.Random.NextDouble() < 0.1)
                    {
                        // TODO FUT Impl. add some chance for surrender effect: enemy can capture this unit
                        u.Status = UnitStatus.DESTROYED;
                    }
                }
            });
        }
    }
}