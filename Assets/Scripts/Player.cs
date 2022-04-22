using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Commands;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.DataIO;
using SteelOfStalin.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin
{
    // local player profile
    public class PlayerProfile
    {
        // passed to host if connected as client
        // TODO FUT. Impl. add support for changing names, think of how to identify the same player on servers even if he changes his name (maybe using player ID)
        public string Name { get; set; }
        // TODO FUT. Impl. add more profile items: achievements, battle statistics etc..

        public void Save() => this.SerializeJson("profile");
    }

    public class Player : ICloneable
    {
        // TODO FUT Impl. add country property
        public string Name { get; set; }
        public SerializableColor SerializableColor { get; set; }
        public Resources Resources { get; set; } = new Resources();
        public List<Player> Allies { get; set; } = new List<Player>(); // TODO FUT. Impl. Ally system (historical, designated before battle starts / unknown handshake)
        public List<Command> Commands { get; set; } = new List<Command>();
        public bool IsReady { get; set; } = false;

        [JsonIgnore] public bool IsDefeated => !Cities.Any(c => c.Durability > 0);
        [JsonIgnore] public Color Color => (Color)SerializableColor;
        [JsonIgnore] public IEnumerable<Unit> Units => Map.Instance.GetUnits(this);
        [JsonIgnore] public IEnumerable<Building> Buildings => Map.Instance.GetBuildings(this);
        [JsonIgnore] public IEnumerable<Cities> Cities => Map.Instance.GetCities(this);
        [JsonIgnore] public Metropolis Capital => Cities.OfType<Metropolis>().First(); // TODO FUT. Impl. Consider distinguishing Metropolis and the Capital
        [JsonIgnore] public PlayerObject PlayerObjectComponent => GameObject.Find(Name)?.GetComponent<PlayerObject>();

        // TODO FUT. Impl. Consider researches as well, change back FYPImplement to All
        public IEnumerable<Unit> GetAllTrainableUnits() => Game.UnitData.FYPImplement.Where(u => HasEnoughResources(u.Cost.Base));
        public IEnumerable<Unit> GetAllUnitsInSight() => Units.SelectMany(u => u.UnitsInSight).Distinct();
        public IEnumerable<Unit> GetAllUnitsUnknown() => Units.SelectMany(u => u.UnitsUnknown).Distinct();
        public IEnumerable<Building> GetAllBuildingsInSight() => Units.SelectMany(u => u.BuildingsInSight).Distinct();
        public IEnumerable<Tile> GetAllTilesAroundCities() => Cities.Select(c => Map.Instance.GetNeighbours(c.CubeCoOrds, (int)c.ConstructionRange.ApplyMod())).SelectMany(ts => ts);
        public IEnumerable<Building> GetAllBuildingsAroundCities() => Map.Instance.GetBuildings(Buildings.Select(s => s.CoOrds).Intersect(GetAllTilesAroundCities().Select(t => t.CoOrds)));
        public IEnumerable<Tile> GetAllConstructibleTilesAroundCities() => GetAllTilesAroundCities().Where(n => n.AllowConstruction).ToList();

        public void ProduceResources() => Cities.Where(c => !c.IsDestroyed).ToList().ForEach(c => Resources.Produce(c.Production));
        public void ConsumeResources(Resources cost) => Resources.Consume(cost);
        public bool HasEnoughResources(Resources need) => Resources.HasEnoughResources(need);
        public string GetResourcesChangeRecord(string res, decimal change) => res switch
        {
            "Money" => $" m:{change:+0.##;-0.##}=>{Resources.Money} ",
            "Steel" => $" t:{change:+0.##;-0.##}=>{Resources.Steel} ",
            "Supplies" => $" s:{change:+0.##;-0.##}=>{Resources.Supplies} ",
            "Cartridges" => $" c:{change:+0.##;-0.##}=>{Resources.Cartridges} ",
            "Shells" => $" h:{change:+0.##;-0.##}=>{Resources.Shells} ",
            "Fuel" => $" f:{change:+0.##;-0.##}=>{Resources.Fuel} ",
            "RareMetal" => $" r:{change:+0.##;-0.##}=>{Resources.RareMetal} ",
            _ => throw new ArgumentException($"Unknown resources symbol {res}")
        };
        public string GetResourcesChangeRecord(Resources consume)
        {
            if (consume.IsZero)
            {
                return "";
            }
            
            StringBuilder sb = new StringBuilder();
            if (consume.Money > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Money", -consume.Money));
            }
            if (consume.Steel > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Steel", -consume.Steel));
            }
            if (consume.Supplies > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Supplies", -consume.Supplies));
            }
            if (consume.Cartridges > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Cartridges", -consume.Cartridges));
            }
            if (consume.Shells > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Shells", -consume.Shells));
            }
            if (consume.Fuel > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Fuel", -consume.Fuel));
            }
            if (consume.RareMetal > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("RareMetal", -consume.RareMetal));
            }
            return sb.ToString();
        }


        public object Clone()
        {
            Player copy = (Player)MemberwiseClone();
            copy.Resources = (Resources)Resources.Clone();
            return copy;
        }
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Name;

        public static AIPlayer NewDummyPlayer() => new AIPlayer()
        {
            Name = $"dummy_{Utilities.Random.Next()}",
            SerializableColor = (SerializableColor)new Color((float)Utilities.Random.NextDouble(), (float)Utilities.Random.NextDouble(), (float)Utilities.Random.NextDouble()),
            Resources = (Resources)Resources.TEST.Clone(),
            IsReady = true
        };

        public static IEnumerable<AIPlayer> NewDummyPlayers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return NewDummyPlayer();
            }
        }

        public static bool operator ==(Player p1, Player p2) => p1?.Name == p2?.Name && p1?.Color == p2?.Color;
        public static bool operator !=(Player p1, Player p2) => !(p1?.Name == p2?.Name && p1?.Color == p2?.Color);
    }

    public class AIPlayer : Player
    {
        // AI algo goes here
    }

    public class PlayerObject : NetworkBehaviour
    {
        public bool IsAI => m_self is AIPlayer;
        private Battle m_battle => Battle.Instance;
        private Player m_self => m_battle.Self;
        private bool m_isInitialized { get; set; } = false;

        private void Start()
        {
            _ = StartCoroutine(Initialize());
        }
        private void FixedUpdate()
        {
            // TODO FUT. Impl. Add key-binding options and move to KeyboardController.cs
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                ChangeReadyStatus(true);
            }
        }
        private IEnumerator Initialize()
        {
            yield return new WaitWhile(() => m_battle == null);
            yield return new WaitWhile(() => m_self == null);
            gameObject.name = m_self.Name;
            m_isInitialized = true;
            Debug.Log("Player object initialized");
            yield return null;
        }

        public void ChangeReadyStatus(bool ready)
        {
            if (!m_isInitialized)
            {
                return;
            }
            m_self.IsReady = ready;
            UpdateReadyStatusServerRpc(ready, NetworkUtilities.GetServerRpcParams());
        }
        
        [ClientRpc]
        private void UpdateReadyStatusAllClientRpc(bool ready, string player_name)
        {
            m_battle.GetPlayer(player_name).IsReady = ready;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void UpdateReadyStatusServerRpc(bool ready, ServerRpcParams @params)
        {
            ulong sender = @params.Receive.SenderClientId;
            Player player = m_battle.GetPlayer(sender);
            player.IsReady = ready;
            Debug.Log($"{player} ready status: {ready}");
            UpdateReadyStatusAllClientRpc(ready, player.Name);
        }
    }
}
