using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public IEnumerable<Unit> Units => Map.Instance.GetUnits(this);
        public IEnumerable<Building> Buildings => Map.Instance.GetBuildings(this);
        public IEnumerable<Cities> Cities => Map.Instance.GetCities(this);
        public List<Player> Allies { get; set; } = new List<Player>();

        public bool IsReady { get; set; } = false;
        public bool IsDefeated => !Cities.Any(c => c.Durability > 0);

        public IEnumerable<Unit> GetAllUnitsInSight() => Units.SelectMany(u => u.UnitsInSight).Distinct();
        public IEnumerable<Unit> GetAllUnitsUnknown() => Units.SelectMany(u => u.UnitsUnknown).Distinct();
        public IEnumerable<Building> GetAllBuildingsInSight() => Units.SelectMany(u => u.BuildingsInSight).Distinct();
        public IEnumerable<Tile> GetAllTilesAroundCities() => Cities.Select(c => Map.Instance.GetNeigbours(c.CubeCoOrds, (int)c.ConstructionRange.ApplyMod())).SelectMany(ts => ts);
        public IEnumerable<Building> GetAllBuildingsAroundCities() => Map.Instance.GetBuildings(Buildings.Select(s => s.CoOrds).Intersect(GetAllTilesAroundCities().Select(t => t.CoOrds)));
        public IEnumerable<Tile> GetAllConstructibleTilesAroundCities() => GetAllTilesAroundCities().Where(n => n.AllowConstruction).ToList();

        public void ProduceResources() => Cities.Where(c => !c.IsDestroyed).ToList().ForEach(c => Resources.Produce(c.Production));
        public void ConsumeResources(Resources cost) => Resources.Consume(cost);
        public bool HasEnoughResources(Resources need) => Resources.HasEnoughResources(need);
        public string GetResourcesChangeRecord(string res, double change) => res switch
        {
            "Money" => $" m:{change:+#.##;-#.##}=>{Resources.Money} ",
            "Steel'" => $" t:{change:+#.##;-#.##}=>{Resources.Steel} ",
            "Supplies" => $" s:{change:+#.##;-#.##}=>{Resources.Supplies} ",
            "Cartridges" => $" c:{change:+#.##;-#.##}=>{Resources.Cartridges} ",
            "Shells" => $" h:{change:+#.##;-#.##}=>{Resources.Shells} ",
            "Fuel" => $" f:{change:+#.##;-#.##}=>{Resources.Fuel} ",
            "RareMetal" => $" r:{change:+#.##;-#.##}=>{Resources.RareMetal} ",
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
