using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.Assets.Props.Units.Land.Artilleries;
using SteelOfStalin.Assets.Props.Buildings.Productions;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Assets.Customizables.Firearms;
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
        public IEnumerable<Building> GetAllConstructableBuilding() => Game.BuildingData.FYPImplement.Where(u => HasEnoughResources(u.Cost.Base));
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
            var barracks = Buildings.Where(c => c is Barracks);
            var unitbuildings = Buildings.Where(c => c is UnitBuilding);
            //check number of resources generated
            var rate = Cities.Where(c => !c.IsDestroyed).Count();
        

            if(unitbuildings.Any()){
                //check training queue/slot
                var building = unitbuildings.Cast<UnitBuilding>();

                foreach(var x in building){
                    var unittype = GetAllTrainableUnits().OfType<Personnel>();
                    var Artiltype = GetAllTrainableUnits().OfType<Artillery>();
                    if(x.CanTrain()){
                        //may be adjusted later
                        if(rate <4){
                            // maybe use random?
                            if(x is Barracks){
                                if(unittype.Any(c => c is Militia) && Units.Where(c => c is Militia).Count() <= 8){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Militia>(),x,this));
                                    ConsumeResources(unittype.OfType<Militia>().First().Cost.Base);
                                }
                                else if(unittype.Any(c => c is Infantry) && Units.Where(c => c is Infantry).Count() <= 10){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Infantry>(),x,this));
                                    ConsumeResources(unittype.OfType<Infantry>().First().Cost.Base);
                                }
                                else if(unittype.Any(c => c is Assault) && Units.Where(c => c is Assault).Count() <= 10){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Assault>(),x,this));
                                    ConsumeResources(unittype.OfType<Assault>().First().Cost.Base);
                                }
                                else if(unittype.Any(c => c is Support) && Units.Where(c => c is Support).Count() <= 4){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Support>(),x,this));
                                    ConsumeResources(unittype.OfType<Support>().First().Cost.Base);
                                }
                                else if(unittype.Any(c => c is Mountain) && Units.Where(c => c is Mountain).Count() <= 3){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Mountain>(),x,this));
                                    ConsumeResources(unittype.OfType<Mountain>().First().Cost.Base);
                                }
                                else if(unittype.Any(c => c is Engineer) && Units.Where(c => c is Engineer).Count() <= 3){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Engineer>(),x,this));
                                    ConsumeResources(unittype.OfType<Engineer>().First().Cost.Base);
                                }
                            }
                            else if(x is Arsenal){
                                //for arsenal
                                //check enemy units composition
                                if(Artiltype.Any(c => c is Portable) && Units.Where(c => c is Portable).Count() <= 8){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Portable>(),x,this));
                                    ConsumeResources(Artiltype.OfType<Portable>().First().Cost.Base);
                                }
                                else if(Artiltype.Any(c => c is DirectFire) && Units.Where(c => c is DirectFire).Count() <= 10){
                                    Commands.Add(new Train(Game.UnitData.GetNew<DirectFire>(),x,this));
                                    ConsumeResources(Artiltype.OfType<DirectFire>().First().Cost.Base);
                                }
                                else if(Artiltype.Any(c => c is AntiTank) && Units.Where(c => c is AntiTank).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<AntiTank>(),x,this));
                                    ConsumeResources(Artiltype.OfType<AntiTank>().First().Cost.Base);
                                }
                                else if(Artiltype.Any(c => c is AntiAircraft) && Units.Where(c => c is AntiAircraft).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<AntiAircraft>(),x,this));
                                    ConsumeResources(Artiltype.OfType<AntiAircraft>().First().Cost.Base);
                                }
                                else if(Artiltype.Any(c => c is HeavySupport) && Units.Where(c => c is HeavySupport).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<HeavySupport>(),x,this));
                                    ConsumeResources(Artiltype.OfType<HeavySupport>().First().Cost.Base);
                                }
                                else if(Artiltype.Any(c => c is SelfPropelled) && Units.Where(c => c is SelfPropelled).Count() <= 3){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Engineer>(),x,this));
                                    ConsumeResources(Artiltype.OfType<SelfPropelled>().First().Cost.Base);
                                }
                                else if(Artiltype.Any(c => c is CoastalGun) && Units.Where(c => c is CoastalGun).Count() <= 3){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Engineer>(),x,this));
                                    ConsumeResources(Artiltype.OfType<CoastalGun>().First().Cost.Base);
                                }
                                // else if(Artiltype.Any(c => c is Railroad && Units.Where(c => c is Railroad).Count() <= 3)){
                                //     Commands.Add(new Train(Game.UnitData.GetNew<Engineer>(),x,this));
                                // }
                            }

                        }
                        else{



                        }           
                    }
                }                 
            }
        }
        void supplyunits(){
            //check available units
            foreach(var unit in Units){
                //fuel
                if(unit.Carrying.Fuel < unit.Capacity.Fuel){
                    var amount = unit.Capacity.Fuel - unit.Carrying.Fuel;
                        unit.Carrying.Fuel.PlusEquals(amount);
                        this.Resources.Fuel.MinusEquals(amount);
 
                }
                //ammo
                // if(!unit.HasEnoughAmmo(unit.GetWeapons())){
                    // var amount = unit.Capacity.Cartridges - unit.Carrying.Cartridges;
                    // unit.Carrying.Cartridges.PlusEquals(amount);
                    // this.Resources.Cartridges.MinusEquals(amount);
                // }

 
            }

        }

        void movetonewcity(){
            if(Cities.Count() < 5){
                //get a non occupied cities   
                var neut = Map.Instance.GetCities(c => c.IsNeutral());
                //get nearest city from base
                var nearest = neut.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c)).First();
                //get avaiable units nearest to the newest cities
                var avaunits = Units.OrderBy(c => Units.First(c => c is Personnel).GetDistance(nearest)).First();
                if(avaunits.GetDistance(nearest) == 0){
                   Commands.Add(new Capture(avaunits));
                   avaunits.CommandAssigned = CommandAssigned.CAPTURE;
                 }
                else{
                var pathtocity = avaunits.GetPath(avaunits.GetLocatedTile(), nearest);
                 //add commands to units (order a single units to new city)
                Commands.Add(new Move(avaunits, pathtocity.ToList()));   
                avaunits.CommandAssigned = CommandAssigned.MOVE;
                }
            }

        }

        //build building 
        void constructbuilding(){
            //for building in city range
            var orderedcities = Cities.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c));
            int num = 0;
            var enough = GetAllConstructableBuilding();

            //get nearby city tiles
            foreach(var i in orderedcities){
                var around = Map.Instance.GetNeighbours(i.CubeCoOrds, (int)i.ConstructionRange.ApplyMod());
                var tilearoundcity = Cities.Select(c => Map.Instance.GetNeighbours(i.CubeCoOrds, (int)i.ConstructionRange.ApplyMod())).SelectMany(ts => ts);
                // var buildingaroundcity = Map.Instance.GetBuildings(Buildings.Select(s => s.CoOrds).Intersect(GetAllTilesAroundCities().Select(t => t.CoOrds)));
                var buildingaround = Map.Instance.GetBuildings(this).Where(c => tilearoundcity.Contains(c.GetLocatedTile()));
                //limit number of building can be build each round
                if(num <= 2){
                    foreach(var j in around){
                        //get type of building should be constructed
                        //build priority
                        //find the limits of building can be build around the city.
                        //has enough resource to build
                        //limited number of buildings can be build for all cities
                        //every city can't have same type of building
                        if(!buildingaround.Any(c => c is Barracks) && Buildings.Where(c => c is Barracks).Count() < 3 && enough.Any(c => c is Barracks)){
                            //build unitbuilding
                            Commands.Add(new Construct(this,Game.BuildingData.GetNew<Barracks>(),j.CoOrds));
                            ConsumeResources(enough.OfType<Barracks>().First().Cost.Base);
                            num +=1;
                        }
                        else if(!buildingaround.Any(c => c is Foundry) && Buildings.Where(c => c is Foundry).Count() < 3 && enough.Any(c => c is Foundry)){
                            //build foundry
                            Commands.Add(new Construct(this,Game.BuildingData.GetNew<Foundry>(),j.CoOrds));
                            ConsumeResources(enough.OfType<Foundry>().First().Cost.Base);
                            num +=1;
                        }
                        else if(!buildingaround.Any(c => c is Arsenal) && Buildings.Where(c => c is Arsenal).Count() < 3 && enough.Any(c => c is Arsenal)){
                            //build Arsenal
                            Commands.Add(new Construct(this,Game.BuildingData.GetNew<Arsenal>(),j.CoOrds));
                            ConsumeResources(enough.OfType<Arsenal>().First().Cost.Base);
                            num +=1;
                        }
                        else if(!buildingaround.Any(c => c is AmmoFactory)&& Buildings.Where(c => c is AmmoFactory).Count() < 3 && enough.Any(c => c is AmmoFactory)){
                            //build AmmoFactory
                            Commands.Add(new Construct(this,Game.BuildingData.GetNew<AmmoFactory>(),j.CoOrds));
                            ConsumeResources(enough.OfType<AmmoFactory>().First().Cost.Base);
                            num =+1;
                        }
                        else if(!buildingaround.Any(c => c is  Industries)&& Buildings.Where(c => c is Industries).Count() < 3 && enough.Any(c => c is Industries)){
                            //build Industries
                            Commands.Add(new Construct(this,Game.BuildingData.GetNew<Industries>(),j.CoOrds));
                            ConsumeResources(enough.OfType<Industries>().First().Cost.Base);
                            num =+1;
                        }
                        else if(!buildingaround.Any(c => c is Refinery)&& Buildings.Where(c => c is Refinery).Count() < 3 && enough.Any(c => c is Refinery)){
                            //build Refinery
                            Commands.Add(new Construct(this,Game.BuildingData.GetNew<Refinery>(),j.CoOrds));
                            ConsumeResources(enough.OfType<Refinery>().First().Cost.Base);
                            num +=1;
                        }
                    }  
                }
            }
        }



        void movement(){
            //get all moveable units
            var moveable = Units.Where(c => c.CommandAssigned == CommandAssigned.NONE).Where(c => c.CanMove());
            //get nearest enemy city will fix later
            var enemy = Map.Instance.GetCities().Where(c => c.IsHostile(this));
            var nearest = enemy.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c)).First();
            //get number of units may improve later
            moveable = moveable.Where(c => c is Personnel).OrderBy(c => c.GetDistance(nearest)).Take(6);

            //any enemy spotted
            // if(Units.Any(c => c.HasSpotted(Map.Instance.GetUnits().Where(c => c.IsHostile())))){
            //     Map.Instance.GetUnits().Where(c => c is hostile);
                
            // }
            //move to enemy city

            foreach(var i in moveable){
                var pathtocity = i.GetPath(i.GetLocatedTile(), nearest);
                Commands.Add(new Move(i, pathtocity.ToList()));   
                i.CommandAssigned = CommandAssigned.MOVE;
            }

        }
        void combat(){
            //if enemy is spotted
            //get the number of enemies unit spotted
            //get number of units equal or more 
            //if enough, proceed to combat
            //if not, request retreat

            var insight = GetAllUnitsInSight();
            foreach(var ally in Units.OfType<Personnel>()){
                var enemy = ally.GetHostileUnitsInRange(ally.GetReconRange());


            }
            //if there is hostile unit
            if(insight.Any(c => c.IsHostile(this))){
                //get number of enemy
                // int numenemy = insight.Where(c => c.IsHostile(this)).Count;
                //get tnumber of ally nearby

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
