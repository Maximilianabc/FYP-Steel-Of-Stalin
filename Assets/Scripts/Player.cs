using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Assets.Props.Buildings.Infrastructures;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.Assets.Props.Units.Land.Artilleries;
using SteelOfStalin.Assets.Props.Units.Land.Vehicles;
using SteelOfStalin.Assets.Props.Buildings.Productions;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Assets.Customizables.Firearms;
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
        public void botflow(){
            train();
            supplyunits();
            movetonewcity();
            constructoutpost();
            movement();
            checkcombat();
            this.IsReady = true;
        }

        public void train(){
            //get training buidling
            var barracks = Buildings.Where(c => c is Barracks);
            var unitbuildings = Buildings.Where(c => c is UnitBuilding);
            //check number of resources generated
            var rate = Cities.Where(c => !c.IsDestroyed).Count();

            //deploy
            if(unitbuildings.Any()){
                var building = unitbuildings.Cast<UnitBuilding>();
                foreach(var c in building){
                    if(c.CanDeploy()){
                        var readyunit = c.ReadyToDeploy.First();
                        Commands.Add(new Deploy(readyunit,c,c.GetDeployableDestinations(readyunit).First().CoOrds,readyunit.GetWeapons()));
                    }
                }
            }

        
            //train
            if(unitbuildings.Any()){
                //check training queue/slot
                var building = unitbuildings.Cast<UnitBuilding>();

                foreach(var x in building){
                    var unittype = GetAllTrainableUnits().OfType<Personnel>();
                    var Artiltype = GetAllTrainableUnits().OfType<Artillery>();
                    if(x.CanTrain()){
                        //may be adjusted later
                        if(rate < 6){
                            //maybe use random?
                            var rand = new System.Random();
                            int num = rand.Next(1,unittype.Count());

                            //train priority
                            if(x is Barracks){
                                if(unittype.Any(c => c is Militia) && Units.Where(c => c is Militia).Count() <= 10){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Militia>(),x,this));
                                }
                                else if(unittype.Any(c => c is Infantry) && Units.Where(c => c is Infantry).Count() <= 10){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Infantry>(),x,this));
                                }
                                else if(unittype.Any(c => c is Assault) && Units.Where(c => c is Assault).Count() <= 10){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Assault>(),x,this));
                                }
                                else if(unittype.Any(c => c is Support) && Units.Where(c => c is Support).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Support>(),x,this));
                                }
                                else if(unittype.Any(c => c is Mountain) && Units.Where(c => c is Mountain).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Mountain>(),x,this));
                                }
                                else if(unittype.Any(c => c is Engineer) && Units.Where(c => c is Engineer).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Engineer>(),x,this));
                                }
                            }
                            else if(x is Arsenal && Units.OfType<Personnel>().Count() > 15){
                                //for arsenal
                                //check enemy units composition
                                //will be adjusted
                                if(Artiltype.Any(c => c is Portable) && Units.Where(c => c is Portable).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Portable>(),x,this));
                                }
                                else if(Artiltype.Any(c => c is DirectFire) && Units.Where(c => c is DirectFire).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<DirectFire>(),x,this));
                                }
                                else if(Artiltype.Any(c => c is AntiTank) && Units.Where(c => c is AntiTank).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<AntiTank>(),x,this));
                                }
                                else if(Artiltype.Any(c => c is AntiAircraft) && Units.Where(c => c is AntiAircraft).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<AntiAircraft>(),x,this));
                                }
                                else if(Artiltype.Any(c => c is HeavySupport) && Units.Where(c => c is HeavySupport).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<HeavySupport>(),x,this));
                                }
                                else if(Artiltype.Any(c => c is SelfPropelled) && Units.Where(c => c is SelfPropelled).Count() <= 2){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Engineer>(),x,this));
                                }
                            }
                            //vehicles
                            // else if(x is Dockyard){
                            //     if(Units.OfType<MotorisedInfantry>().Count() > 5){
                            //          Commands.Add(new Train(Game.UnitData.GetNew<MotorisedInfantry>(),x,this));
                            //     }
                            //     if(Units.OfType<Utility>().Count() > 5){
                            //          Commands.Add(new Train(Game.UnitData.GetNew<Utility>(),x,this));
                            //     }
                            //     if(Units.OfType<Carrier>().Count() > 5){
                            //          Commands.Add(new Train(Game.UnitData.GetNew<Carrier>(),x,this));
                            //     }
                            //     if(Units.OfType<ArmouredCar>().Count() > 5){
                            //          Commands.Add(new Train(Game.UnitData.GetNew<ArmouredCar>(),x,this));
                            //     }
                            //     if(Units.OfType<TankDestroyer>().Count() > 5){
                            //          Commands.Add(new Train(Game.UnitData.GetNew<TankDestroyer>(),x,this));
                            //     }
                            //     if(Units.OfType<AssaultGun>().Count() > 5){
                            //          Commands.Add(new Train(Game.UnitData.GetNew<AssaultGun>(),x,this));
                            //     }
                            //     // GetNew<LightTank>(),
                            //     // GetNew<MediumTank>(),
                            //     // GetNew<HeavyTank>()
                            // }
                        }
                        //if rate > 6
                        else{
                        //change the limit and priority
                            if(x is Barracks){
                                if(unittype.Any(c => c is Infantry) && Units.Where(c => c is Infantry).Count() <= 15){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Infantry>(),x,this));
                                }
                                else if(unittype.Any(c => c is Assault) && Units.Where(c => c is Assault).Count() <= 15){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Assault>(),x,this));
                                }
                                else if(unittype.Any(c => c is Support) && Units.Where(c => c is Support).Count() <= 4){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Support>(),x,this));
                                }
                                else if(unittype.Any(c => c is Mountain) && Units.Where(c => c is Mountain).Count() <= 4){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Mountain>(),x,this));
                                }
                                else if(unittype.Any(c => c is Engineer) && Units.Where(c => c is Engineer).Count() <= 4){
                                    Commands.Add(new Train(Game.UnitData.GetNew<Engineer>(),x,this));
                                }
                            }

                        }           
                    }
                }                 
            }
        }
        public void supplyunits(){
            //check available units
            foreach(var unit in Units){
                var aroundcity = unit.GetFriendlyBuildingsInRange(Map.Instance.GetNeighbours(unit.CubeCoOrds,3)).OfType<Barracks>().Count();
                var aroundoutpost = unit.GetFriendlyBuildingsInRange(Map.Instance.GetNeighbours(unit.CubeCoOrds,3)).OfType<Outpost>().Count();
                if(aroundcity + aroundoutpost >=1){
                    //fuel
                    if(unit.Carrying.Fuel < unit.Capacity.Fuel){
                        var amount = unit.Capacity.Fuel - unit.Carrying.Fuel;
                            unit.Carrying.Fuel.PlusEquals(amount);
                            this.Resources.Fuel.MinusEquals(amount);
                    }
                }  
            }

        }

        public void movetonewcity(){
            //get a non occupied cities  
            var neut = Map.Instance.GetCities(c => c.IsNeutral());
            if(neut.Count() > 3){
                //get nearest city from base
                var nearest = neut.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c)).First();
                //get avaiable units nearest to the newest cities
                var avaunits = Units.Where(c => c.CommandAssigned == CommandAssigned.NONE).OrderBy(c => c.GetDistance(nearest));

                if(avaunits.Count() >= 1){             
                    var first = avaunits.First();
                    if(first.GetDistance(nearest) == 0){
                        Commands.Add(new Capture(first));
                        first.CommandAssigned = CommandAssigned.CAPTURE;
                    }
                    else{
                        var pathtocity = first.GetPath(first.GetLocatedTile(), nearest);
                        //add commands to units (order a single units to new city)
                        Commands.Add(new Move(first, pathtocity.ToList())); 
                        first.CommandAssigned = CommandAssigned.MOVE;
                    }
                }
            }
        }


        //build building 
        public void constructbuilding(){
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
                            num +=1;
                        }
                        else if(!buildingaround.Any(c => c is Arsenal) && Buildings.Where(c => c is Arsenal).Count() < 3 && enough.Any(c => c is Arsenal)){
                            //build Arsenal
                            Commands.Add(new Construct(this,Game.BuildingData.GetNew<Arsenal>(),j.CoOrds));
                            num +=1;
                        }

                        // else if(!buildingaround.Any(c => c is Foundry) && Buildings.Where(c => c is Foundry).Count() < 3 && enough.Any(c => c is Foundry)){
                        //     //build foundry
                        //     Commands.Add(new Construct(this,Game.BuildingData.GetNew<Foundry>(),j.CoOrds));
                        //     num +=1;
                        // }
                        // else if(!buildingaround.Any(c => c is AmmoFactory)&& Buildings.Where(c => c is AmmoFactory).Count() < 3 && enough.Any(c => c is AmmoFactory)){
                        //     //build AmmoFactory
                        //     Commands.Add(new Construct(this,Game.BuildingData.GetNew<AmmoFactory>(),j.CoOrds));
                        //     num =+1;
                        // }
                        // else if(!buildingaround.Any(c => c is  Industries)&& Buildings.Where(c => c is Industries).Count() < 3 && enough.Any(c => c is Industries)){
                        //     //build Industries
                        //     Commands.Add(new Construct(this,Game.BuildingData.GetNew<Industries>(),j.CoOrds));
                        //     num =+1;
                        // }
                        // else if(!buildingaround.Any(c => c is Refinery)&& Buildings.Where(c => c is Refinery).Count() < 3 && enough.Any(c => c is Refinery)){
                        //     //build Refinery
                        //     Commands.Add(new Construct(this,Game.BuildingData.GetNew<Refinery>(),j.CoOrds));
                        //     num +=1;
                        // }
                    }  
                }
            }
        }
        public void constructoutpost(){
            //outpost
            var orderedcities = Cities.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c));
            int num = orderedcities.Count();
        
            if(num > 1){
                int x = (orderedcities.First().CoOrds.X + orderedcities.ElementAt(num -1 ).CoOrds.X) / 2;
                int y = (orderedcities.First().CoOrds.Y + orderedcities.ElementAt(num - 1).CoOrds.Y) / 2;
                var midtile = Map.Instance.GetTile(x,y);
                var avaunits = Units.Where(c => c.CommandAssigned == CommandAssigned.NONE).OrderBy(c => c.GetDistance(midtile)).First();
                var pathtolocation = avaunits.GetPath(avaunits.GetLocatedTile(), Map.Instance.GetTile(x,y));

                if(Buildings.Where(b => b.CoOrds.X == midtile.CoOrds.X && b.CoOrds.Y == midtile.CoOrds.Y).Count() < 1){
                    if(avaunits.GetDistance(Map.Instance.GetTile(x,y)) == 0){
                        Commands.Add(new Construct(this,Game.BuildingData.GetNew<Outpost>(),midtile.CoOrds));
                        avaunits.CommandAssigned = CommandAssigned.CONSTRUCT;
                    }
                    else{
                        Commands.Add(new Move(avaunits, pathtolocation.ToList()));
                        avaunits.CommandAssigned = CommandAssigned.MOVE;
                    }
                }
            }

        }



        public void movement(){
            //get all moveable units
            if(Cities.Count() > 3){
                var moveable = Units.Where(c => c.CommandAssigned == CommandAssigned.NONE).Where(c => c.CanMove());
                if(Units.Count() > 12 && moveable.Count() > 8){
                    //get nearest enemy city
                    var enemy = Map.Instance.GetCities().Where(c => c.IsHostile(this));
                    var nearest = enemy.OrderBy(c => Cities.First(c => c is Metropolis).GetDistance(c)).First();
                    // Map.Instance.GetUnits()

                    //get number of units may improve later
                    var selected = moveable.Where(c => c is Personnel).OrderBy(c => c.GetDistance(nearest)).Take(8);

                    //middle point for outpost
                    var path = selected.First().GetPath(selected.First().GetLocatedTile(), nearest.GetLocatedTile());
                    int middle = path.Count()/2;
                    // int x = (Cities.Where(c => c is Metropolis).First().CoOrds.X + nearest.CoOrds.X) / 2;
                    // int y = (Cities.Where(c => c is Metropolis).First().CoOrds.Y + nearest.CoOrds.Y) / 2;
                    var tileofoutpost = path.ElementAt(middle);

                    //as the first one, construct outpost
                    if(selected.First().GetDistance(tileofoutpost) == 0){
                        Commands.Add(new Construct(this,Game.BuildingData.GetNew<Outpost>(),tileofoutpost.CoOrds));
                        selected.First().CommandAssigned = CommandAssigned.CONSTRUCT;
                    }
                    else{
                        foreach(var i in selected){
                            var pathtooutpost = i.GetPath(i.GetLocatedTile(), tileofoutpost);
                            if(!i.CanAccessTile(tileofoutpost)){
                                var newpathtooutpost = i.GetPath(i.GetLocatedTile(), i.GetAccessibleNeigbours(tileofoutpost.CubeCoOrds,1).First());
                                Commands.Add(new Move(i, newpathtooutpost.ToList()));
                                i.CommandAssigned = CommandAssigned.MOVE;
                            }
                            else{
                                Commands.Add(new Move(i, pathtooutpost.ToList()));
                                i.CommandAssigned = CommandAssigned.MOVE;
                            }
                        }
                    }

                    //move to enemy city if outpost has been built
                    var outpost = Buildings.Where(b => b.CoOrds.X == tileofoutpost.CoOrds.X && b.CoOrds.Y == tileofoutpost.CoOrds.Y);
                    if(outpost.Count() >= 1){
                        foreach(var i in moveable){
                            //if arrive
                            if(i.GetDistance(nearest) == 0){
                                Commands.Add(new Capture(i));
                                i.CommandAssigned = CommandAssigned.CAPTURE;
                            }
                            //if units have not arrived yet
                            else{
                                var pathtocity = i.GetPath(i.GetLocatedTile(), nearest.GetLocatedTile());
                                //if unit already exist inside the tile
                                if(!i.CanAccessTile(nearest.GetLocatedTile())){
                                    pathtocity = i.GetPath(i.GetLocatedTile(), i.GetAccessibleNeigbours(nearest.CubeCoOrds,2).First());
                                    Commands.Add(new Move(i, pathtocity.ToList()));
                                    i.CommandAssigned = CommandAssigned.MOVE;
                                }
                                else{
                                    Commands.Add(new Move(i, pathtocity.ToList()));   
                                    i.CommandAssigned = CommandAssigned.MOVE;
                                }
                            }
                        }
                    }
                }
            }
        }
        public void checkcombat(){
            //if enemy is spotted
            //get the number of enemies unit spotted
            //get number of units equal or more 
            //if enough, proceed to combat
            //if not, request retreat

            foreach(var ally in Units.OfType<Personnel>()){
                foreach(IOffensiveCustomizable weapon in ally.GetWeapons()){
                    var enemy = ally.GetHostileUnitsInFiringRange(weapon);
                    //if there is hostile unit
                    if(enemy.Count() > 0){
                        enemy = enemy.OrderBy(c => c.GetDistance(ally));
                        //if only one
                        if(enemy.Count() == 1){
                            Commands.Add(new Fire(ally,enemy.First(), weapon));
                            ally.CommandAssigned = CommandAssigned.FIRE;
                        }
                        //if more
                        else{
                            //check nearby in range of 5
                            var nearby1 = ally.GetFriendlyUnitsInRange(Map.Instance.GetNeighbours(ally.CubeCoOrds,5));
                            //if allied more
                            if(nearby1.Count() >= enemy.Count()){
                                Commands.Add(new Fire(ally,enemy.First(), weapon));
                                ally.CommandAssigned = CommandAssigned.FIRE;
                            }
                            else{
                                //if there are ally in range of 10
                                var nearby2 = ally.GetFriendlyUnitsInRange(Map.Instance.GetNeighbours(ally.CubeCoOrds,8)).OrderBy(c => ally.GetDistance(c));
                                if(nearby2.Count() > nearby1.Count()){
                                    var pathtoally = ally.GetPath(Map.Instance.GetTile(ally.CoOrds.X,ally.CoOrds.Y), nearby2.Last().GetLocatedTile());
                                    Commands.Add(new Move(ally,pathtoally.ToList()));
                                    ally.CommandAssigned = CommandAssigned.MOVE;
                                }
                                //if no ally in range of 8, just attack
                                else{
                                    Commands.Add(new Fire(ally,enemy.First(), weapon));
                                    ally.CommandAssigned = CommandAssigned.FIRE;
                                }
                               
                            }
                        }
                    }
                }  
            }
        }
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

        public void SendCommandsToServer()
        {
            List<string> commands = m_self.Commands.Select(c => c.ToStringBeforeExecution()).ToList();
            if (commands.Count == 0)
            {
                ReceiveCommandsServerRpc("", 0, NetworkUtilities.GetServerRpcParams());
                return;
            }
            foreach (string command in commands)
            {
                ReceiveCommandsServerRpc(command, commands.Count, NetworkUtilities.GetServerRpcParams());
            }
        }
        
        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateReadyStatusAllClientRpc(bool ready, string player_name)
        {
            m_battle.GetPlayer(player_name).IsReady = ready;
        }

        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void UpdateReadyStatusServerRpc(bool ready, ServerRpcParams @params)
        {
            ulong sender = @params.Receive.SenderClientId;
            Player player = m_battle.GetPlayer(sender);
            player.IsReady = ready;
            Debug.Log($"{player} ready status: {ready}");
            UpdateReadyStatusAllClientRpc(ready, player.Name);
        }

        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void ReceiveCommandsServerRpc(string command, int num_to_send, ServerRpcParams @params)
        {
            ulong sender = @params.Receive.SenderClientId;
            Player player = m_battle.GetPlayer(sender);

            Command cmd = Command.FromStringBeforeExecution(command);
            if (cmd == null)
            {
                Debug.LogError($"Failed to parse command {command}");
            }
            if (!cmd.IsValid)
            {
                Debug.LogWarning($"Command {command} is invalid");
            }
            player.Commands.Add(cmd);
            if (player.Commands.Count == num_to_send)
            {
                Debug.Log($"Receive all commands ({num_to_send} in total) from player {player}");
                Battle.Instance.CurrentRound.Commands.AddRange(player.Commands.Where(c => c != null && c.IsValid));
                Battle.Instance.CurrentRound.NumPlayersCommandReceived++;
            }
        }
    }
}
