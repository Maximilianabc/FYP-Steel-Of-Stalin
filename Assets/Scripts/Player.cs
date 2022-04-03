using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.Assets.Props.Buildings.Productions;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Commands;
using SteelOfStalin.CustomTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using UnityEngine;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin
{
    public class Player : ICloneable
    {
        // TODO FUT Impl. add country property
        public string Name { get; set; }
        public SerializableColor SerializableColor { get; set; }
        public Resources Resources { get; set; } = new Resources();
        public List<Player> Allies { get; set; } = new List<Player>();
        public List<Command> Commands { get; set; } = new List<Command>();
        public bool IsReady { get; set; } = false;

        [JsonIgnore] public bool IsDefeated => !Cities.Any(c => c.Durability > 0);
        [JsonIgnore] public Color Color => (Color)SerializableColor;
        [JsonIgnore] public IEnumerable<Unit> Units => Map.Instance.GetUnits(this);
        [JsonIgnore] public IEnumerable<Building> Buildings => Map.Instance.GetBuildings(this);
        [JsonIgnore] public IEnumerable<Cities> Cities => Map.Instance.GetCities(this);
        [JsonIgnore] public Metropolis Capital => Cities.OfType<Metropolis>().First(); // TODO FUT. Impl. Consider distinguishing Metropolis and the Capital

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
            //get training buidling
            var y = Buildings.Where(c => c is UnitBuilding);
            //check number of resources generated
            var rate = Cities.Where(c => !c.IsDestroyed).Count();
            
            if(y.Any()){
                //check training queue/slot
                var building = y.Cast<UnitBuilding>();

                foreach(var x in building){
                    if(x.CanTrain()){
                        //check required resources
                        //     //check enemy units types
                        //     //train units
                        // consume resource depending the unit
                        Commands.Add(new Train(Game.UnitData.GetNew<Militia>()));

                        // ConsumeResources(10);             
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
                         //add supply to units
                    // }

            }

        }

        void movetonewcity(){
            //get a non occupied cities   
            var neut = Map.Instance.GetCities(c => c.IsNeutral());
            //get nearest city from base
            var nearest = neut.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c)).First();
            //get avaiable units nearest to the newest cities
            var avaunits = Units.OrderBy(c => Units.First(c => c is Personnel).GetDistance(nearest)).First();
            if(avaunits.GetDistance(nearest) == 0){
                Commands.Add(new Capture());
            }
            else{
            var pathtocity = avaunits.GetPath(avaunits.GetLocatedTile(), nearest);
            //add commands to units (order a single units to new city)
            Commands.Add(new Move(avaunits, pathtocity.ToList()));   
            avaunits.CommandAssigned = CommandAssigned.MOVE;
            }
        }

        //build building 
        void constructbuilding(){
            var orderedcities = Cities.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c));
            //get nearby city tiles
            var x = GetAllTilesAroundCities();
            //check if there any building/unit building near cities
            foreach(var i in orderedcities){
                 var around = Map.Instance.GetNeighbours(i.CubeCoOrds);
                 if(!around.Any(c => c.HasBuilding)){
                    //check the required resources
                    //build unit building
                 }
                 
            }
            foreach(var i in orderedcities){
                var around = Map.Instance.GetNeighbours(i.CubeCoOrds);
                foreach(var j in around){
                    var typebuild = Map.Instance.GetBuildings(j);
                    if(!typebuild.Any(c => c is Arsenal)){
                        //build unitbuilding
                        Commands.Add(new Construct());
                    }
                    else if(!typebuild.Any(c => c is Foundry)){
                        //build foundry
                        Commands.Add(new Construct());
                    }
                    else if(!typebuild.Any(c => c is Barracks)){
                        //build unitbuilding
                        Commands.Add(new Construct());
                    }
                    else if(!typebuild.Any(c => c is AmmoFactory)){
                        //build unitbuilding
                        Commands.Add(new Construct());
                    }
                    else if(!typebuild.Any(c => c is  Industries)){
                        //build unitbuilding
                        Commands.Add(new Construct());
                    }
                    else if(!typebuild.Any(c => c is Refinery)){
                        //build unitbuilding
                        Commands.Add(new Construct());
                    }
                }          
            }



        }
    }

    public class PlayerObject : MonoBehaviour
    {
        public Player Player { get; set; }
        public bool IsAI => Player is AIPlayer;

        private void Start()
        {
            
        }
        private void FixedUpdate()
        {
            // TODO FUT. Impl. Add key-binding options
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {

            }
        }

        // [ClientRPC]
        public void SendCommands()
        {

        }

        public void ReceiveCommand()
        {

        }
    }
}
