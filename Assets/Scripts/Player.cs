using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Commands;
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
        public List<Command> Commands { get; set; } = new List<Command>();

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
        public string GetResourcesChangeRecord(string res, decimal change) => res switch
        {
            "Money" => $" m:{change:+0.##;-0.##}=>{Resources.Money} ",
            "Steel'" => $" t:{change:+0.##;-0.##}=>{Resources.Steel} ",
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

        public static bool operator ==(Player p1, Player p2) => p1?.Name == p2?.Name && p1?.Color == p2?.Color;
        public static bool operator !=(Player p1, Player p2) => !(p1?.Name == p2?.Name && p1?.Color == p2?.Color);
    }

     public class AIPlayer : Player
    {
        // AI algo goes here
        //make flag true
        void train(){
            //get buidling
            var numberowned = Buildings.Count();
            var y = Buildings.Where(unitbuild => unitbuild is UnitBuilding);
            if(y.Any()){
                //check training queue/slot
                var number = y.Count();
                var building = y.Cast<UnitBuilding>();
                foreach(var x in building){
                    if(x.CanTrain()){
                        //cheack required resources
                        //     //check enemy units types
                        //     //train units
                               

                    }
                }                 
            }
        }
        void supplyunits(){
            //check available units
            foreach(var unit in Units){
                //get unit capacity
                var capa = unit.Capacity.Fuel.Value;
                //if capacity not full, assign supply to units

                //check if unit near owned cities
                    // if(unit.GetBuildingsInRange()){

                    // }
                //add supply to units

            }

        }

        void movetonewcities(){

            //get a non occupied cities   
            var neut = Map.Instance.GetCities(c => c.IsNeutral());
            //get nearest city from base
            var nearest = neut.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c)).First();
            //get avaiable units
            var avaunits = Units.Where(c => c.CanMove()).First();
            var pathtocity = avaunits.GetPath(avaunits.GetLocatedTile(), nearest);
            // var pathtocity = avaunits.GetPath(avaunits.GetLocatedTile(), Map.Instance.GetTile(nearest));
            Commands.Add(new Move(avaunits, pathtocity));
            // Commands.Add(new Move(avaunits, pathtocity));

            // var unitpath = Units.ElementAt(0).GetPath(Units.ElementAt(0).GetLocatedTile(), Map.Instance.GetTile(nearest));

            // var x = Cities.Where(x => x is Cities);

            //assign number of units to city according to the morale // a single unit is fine
            //Buildings.OfType<UnitBuilding>();

            //get available/moveable unit to the new city


            foreach(var unit in Units){
                //check if unit is not in command
                if(unit.CanMove()){
                    //get unit morale
                    //move units to city
                    // Units.ElementAt(0).GetFuelRequired(coor);

                }
            }       
        }

        //build building 
        void buildbuilding(){
            //get cities owned
            var x = Cities;
            var y = Buildings;
            //check if there any building/unit building
            if(!y.Any()){
                //build unit building
            }
            //for each cities, check not owned building
            for(var i = 0; i<x.Count(); i++){
                //if no building, build unit building
                


            }
            //check the required resources
            //build



        }
    }

    public class HumanPlayer : Player
    {

    }
}
