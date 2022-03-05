using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin
{
    public abstract class Player : ICloneable
    {
        // TODO FUT Impl. add country property
        public string Name { get; set; }
        public Color Color { get; set; }
        public Resources Resources { get; set; } = new Resources();
        public List<Unit> Units => Map.Instance.GetUnits(this).ToList();
        public List<Building> Buildings => Map.Instance.GetBuildings(this).ToList();
        public List<Cities> Cities => Map.Instance.GetCities(this).ToList();
        public List<Player> Allies { get; set; } = new List<Player>();

        public bool IsReady { get; set; } = false;
        public bool IsDefeated => Cities.Count == 0;

        public List<Unit> GetAllUnitsInSight() => Units.SelectMany(u => u.UnitsInSight).Distinct().ToList();
        public List<Unit> GetAllUnitsUnknown() => Units.SelectMany(u => u.UnitsUnknown).Distinct().ToList();
        public List<Building> GetAllBuildingsInSight() => Units.SelectMany(u => u.BuildingsInSight).Distinct().ToList();

        public void ProduceResources() => Cities.Where(c => c.Durability > 0).ToList().ForEach(c => Resources.Produce(c.Production));
        public void ConsumeResources(Resources cost) => Resources.Consume(cost);
        public bool HasEnoughResources(Resources need) => Resources.HasEnoughResources(need);

        public object Clone()
        {
            Player copy = (Player)MemberwiseClone();
            copy.Resources = (Resources)Resources.Clone();
            return copy;
        }
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(Player p1, Player p2) => p1?.Name == p2?.Name && p1?.Color == p2?.Color;
        public static bool operator !=(Player p1, Player p2) => !(p1?.Name == p2?.Name && p1?.Color == p2?.Color);
    }

    public class AIPlayer : Player
    {
        // AI algo goes here
    }

    public class HumanPlayer : Player
    {

    }
}
