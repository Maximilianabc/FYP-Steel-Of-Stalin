using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SteelOfStalin;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.Assets.Props;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Buildings.Infrastructures;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units.Land.Artilleries;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.Commands;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.DataIO;
using SteelOfStalin.Util;
using UnityEngine;
using UnityEngine.TestTools;

public class GameLogicTest
{
    public UnitData UnitData => Game.UnitData;
    public BuildingData BuildingData => Game.BuildingData;
    public TileData TileData => Game.TileData;
    public CustomizableData CustomizableData => Game.CustomizableData;
    public BattleRules Rules { get; set; } = new BattleRules();
    public Map Map { get; set; } = new Map();

    [SetUp]
    public void TestSetUp()
    {
        Game.LoadAllAssets();
        Rules = DataUtilities.DeserializeJson<BattleRules>(@"Saves\test\rules");
        Map.Load();
        if (Map.Players.Count < 3)
        {
            Map.Players.AddRange(Player.NewDummyPlayers(3 - Map.Players.Count));
            Map.SetMetropolisOwners();
            Map.SetDefaultUnitBuildingsOwners();
        }
    }

    [Test]
    public void GetNeighbourTest()
    {
        Coordinates random = new Coordinates(Utilities.Random.Next(100), Utilities.Random.Next(100));
        CubeCoordinates random_cube = (CubeCoordinates)random;

        IEnumerable<CubeCoordinates> cubes = random_cube.GetNeighbours();
        Assert.AreEqual(cubes.Count(), 7);

        IEnumerable<CubeCoordinates> expected = new List<CubeCoordinates>()
        {
            random_cube,
            new CubeCoordinates(random_cube.X + 1, random_cube.Y - 1, random_cube.Z),
            new CubeCoordinates(random_cube.X - 1, random_cube.Y + 1, random_cube.Z),
            new CubeCoordinates(random_cube.X + 1, random_cube.Y, random_cube.Z - 1),
            new CubeCoordinates(random_cube.X - 1, random_cube.Y, random_cube.Z + 1),
            new CubeCoordinates(random_cube.X, random_cube.Y + 1, random_cube.Z - 1),
            new CubeCoordinates(random_cube.X, random_cube.Y - 1, random_cube.Z + 1),
        };
        CollectionAssert.AreEquivalent(expected, cubes);
    }

    [Test]
    public void GetNeighboursWithSameTypeTest()
    {
        IEnumerable<Prop> boundary_neighbours = Map.GetTile(0, 2).GetNeighboursWithSameType();
        Assert.AreEqual(boundary_neighbours.Count(), 2);

        IEnumerable<Coordinates> expected_coords = new List<Coordinates>()
        {
            new Coordinates(0, 3),
            new Coordinates(0, 1)
        };
        CollectionAssert.AreEquivalent(expected_coords, boundary_neighbours.Select(b => b.CoOrds));
    }

    [Test]
    public void GetConnectionTest()
    {
        IEnumerable<Boundary> boundaries = Map.GetProps<Boundary>();
        foreach (Boundary boundary in boundaries)
        {
            PropConnection connection = boundary.GetConnection();
            Coordinates coords = boundary.CoOrds;

            if (coords.X == 0)
            {
                if (coords.Y > 1 && coords.Y < Map.Height - 1)
                {
                    Assert.AreEqual(PropConnection.POS_1 | PropConnection.POS_4, connection);
                }
                else if (coords.Y == Map.Height - 1)
                {
                    Assert.AreEqual(PropConnection.POS_2 | PropConnection.POS_4, connection);
                }
                else if (coords.Y == 1)
                {
                    Assert.AreEqual(PropConnection.POS_1 | PropConnection.POS_3 | PropConnection.POS_4, connection);
                }
                else if (coords.Y == 0)
                {
                    Assert.AreEqual(PropConnection.POS_1 | PropConnection.POS_2, connection);
                }
            }
            else if (coords.X == Map.Width - 1)
            {
                if (coords.Y > 0 && coords.Y < Map.Height - 2)
                {
                    Assert.AreEqual(PropConnection.POS_1 | PropConnection.POS_4, connection);
                }
                else if (coords.Y == Map.Height - 1)
                {
                    Assert.AreEqual(PropConnection.POS_4 | PropConnection.POS_5, connection);
                }
                else if (coords.Y == Map.Height - 2)
                {
                    Assert.AreEqual(PropConnection.POS_1 | PropConnection.POS_4 | PropConnection.POS_6, connection);
                }
                else if (coords.Y == 0)
                {
                    Assert.AreEqual(PropConnection.POS_1 | PropConnection.POS_5, connection);
                }
            }
            else if (coords.Y == 0)
            {
                if (coords.X == 1)
                {
                    Assert.AreEqual(PropConnection.POS_3 | PropConnection.POS_5 | PropConnection.POS_6, connection);
                }
                else if (coords.X % 2 == 1)
                {
                    Assert.AreEqual(PropConnection.POS_3 | PropConnection.POS_5, connection);
                }
                else
                {
                    Assert.AreEqual(PropConnection.POS_2 | PropConnection.POS_6, connection);
                }
            }
            else if (coords.Y == Map.Height - 1)
            {
                if (coords.X == Map.Width - 2)
                {
                    Assert.AreEqual(PropConnection.POS_2 | PropConnection.POS_3 | PropConnection.POS_6, connection);
                }
                else if (coords.X % 2 == 1)
                {
                    Assert.AreEqual(PropConnection.POS_3 | PropConnection.POS_5, connection);
                }
                else
                {
                    Assert.AreEqual(PropConnection.POS_2 | PropConnection.POS_6, connection);
                }
            }
        }
    }

    [Test]
    public void GetVariantStringTest()
    {
        _ = Assert.Throws<ArgumentException>(() => Prop.Rotate(PropConnection.POS_1, -123456));

        // TODO modify to fit exactly what is doing in Prop.GetVariantString (i.e. rotation order: 1 -> -1 -> 2 -> -2 ...)
        foreach (PropConnection connection in Prop.UniqueVariants.Keys)
        {
            for (int i = -3; i <= 3; i++)
            {
                PropConnection rotated = Prop.Rotate(connection, i);
                string var_string = Prop.GetVariantString(rotated);
                string unique_var_string = Prop.UniqueVariants[connection];

                if (rotated == connection)
                {
                    Assert.AreEqual(unique_var_string, var_string);
                }
                else if (Math.Abs(i) == 3)
                {
                    Assert.AreEqual($"{unique_var_string}=", var_string);
                }
                else
                {
                    Assert.AreEqual($"{unique_var_string}{i:+0;-0}", var_string);
                }
            }
        }
    }

    [Test]
    public void MapAddUnitTest()
    {
        LogAssert.ignoreFailingMessages = true;

        Infantry i = UnitData.GetNew<Infantry>();
        i.SetMeshName();
        Assert.IsNotNull(i);

        Assert.IsTrue(Map.AddUnit(i));
        Assert.IsFalse(Map.AddUnit(null));
        Assert.IsFalse(Map.AddUnit(i));
        Assert.IsTrue(Map.GetUnits().Contains(i));

        Assert.IsFalse(Map.RemoveUnit(null));
        Assert.IsTrue(Map.RemoveUnit(i));
    }

    [Test]
    public void MapAddRemoveBuildingsTest()
    {
        LogAssert.ignoreFailingMessages = true;

        Barracks b = BuildingData.GetNew<Barracks>();
        Arsenal a = BuildingData.GetNew<Arsenal>();
        b.SetMeshName();
        Assert.IsNotNull(b);        

        Assert.IsTrue(Map.AddBuilding(b));
        Assert.IsFalse(Map.AddBuilding(null));
        Assert.IsFalse(Map.AddBuilding(b));
        Assert.IsTrue(Map.GetBuildings().Contains(b));

        Assert.IsFalse(Map.RemoveBuilding(null));
        Assert.IsTrue(Map.RemoveBuilding(b));

        Building[] buildArr = {a,b};
        Map.AddBuildings(buildArr);
        Assert.IsTrue(Map.GetBuildings().Contains(a));
        Assert.IsTrue(Map.GetBuildings().Contains(b));
    }

    [Test]
    public void MapGetByCoordIntTest()
    {
        Player p1 = Map.Players[0];
        Coordinates point = new Coordinates(1,1);
        Coordinates point2 = new Coordinates(2,2);

        Infantry i = UnitData.GetNew<Infantry>();
        i.SetMeshName();
        Assert.IsNotNull(i);
        i.Initialize(p1, new Coordinates(1,1), SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);

        List<Prop> tileList = new List<Prop>();
        tileList.Add(Map.GetTile(point));
        List<Prop> unitsList = Map.GetUnits(point).ToList<Prop>();
        List<Prop> tileUnitList = tileList.Concat<Prop>(unitsList).ToList<Prop>();
        List<Prop> propsList = Map.GetProps(point).ToList<Prop>();
        Assert.AreEqual(propsList, tileUnitList);

        //also do by int x, int y
        List<Prop> tileList2 = new List<Prop>();
        tileList2.Add(Map.GetTile(2, 2));
        List<Prop> propsList2 = Map.GetProps(point2).ToList<Prop>();
        Assert.AreEqual(propsList2, tileList2);
    }

    [Test]
    public void MapGetTileByCubeCoordTest()
    {
        CubeCoordinates point = new CubeCoordinates(1, -1, 0);
        Assert.IsTrue(point.IsValid);

        List<Prop> tileList = new List<Prop>();
        tileList.Add(Map.GetTile(point));
        List<Prop> propsList = Map.GetProps(point).ToList<Prop>();
        Assert.AreEqual(propsList, tileList);

        propsList = Map.GetProps<Tile>(point).ToList<Prop>();
        Assert.AreEqual(propsList, tileList);        
    }

    [Test]
    public void MapGetByTypeTest()
    {
        Player p1 = Map.Players[0];
        Coordinates point = new Coordinates(1,1);
        Coordinates point2 = new Coordinates(2,2);

        Infantry i = UnitData.GetNew<Infantry>();
        i.SetMeshName();
        Assert.IsNotNull(i);
        i.Initialize(p1, new Coordinates(1,1), SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);

        Barracks b = BuildingData.GetNew<Barracks>();
        Map.AddBuilding(b);

        List<Prop> tileList = Map.GetTiles<Forest>().ToList<Prop>();
        List<Prop> propList = Map.GetProps<Forest>().ToList<Prop>();
        Assert.AreEqual(tileList, propList);

        List<Prop> unitList = Map.GetUnits<Infantry>().ToList<Prop>();
        List<Prop> propList2 = Map.GetProps<Infantry>().ToList<Prop>();
        Assert.AreEqual(unitList, propList2);
        
        List<Prop> suburbList = Map.GetCities<Suburb>().ToList<Prop>();
        List<Prop> propList3 = Map.GetProps<Suburb>().ToList<Prop>();
        Assert.AreEqual(suburbList, propList3);

        List<Prop> barracksList = Map.GetBuildings<Barracks>().ToList<Prop>();
        List<Prop> propList4 = Map.GetProps<Barracks>().ToList<Prop>();
        Assert.AreEqual(barracksList, propList4);
    }

    [Test]
    public void MapGetTilesMiscTest()
    {
        List<Prop> tileList = Map.GetTiles().ToList<Prop>();
        List<Prop> propList = Map.GetProps<Tile>().ToList<Prop>();
        Assert.AreEqual(tileList, propList);

        List<Prop> tileList2 = Map.GetTiles(TileType.PLAINS).ToList<Prop>();
        List<Prop> propList2 = Map.GetProps<Plains>().ToList<Prop>();
        Assert.AreEqual(tileList2, propList2);
    }

    [Test]
    public void MapGetCitiesMiscTest()
    {
        Player p1 = Map.Players[0];
        List<Prop> citiesList = Map.GetCities().ToList<Prop>();
        List<Prop> propList = Map.GetProps<Cities>().ToList<Prop>();
        Assert.AreEqual(citiesList, propList);

        // City c = citiesList[0] as City;
        // c.SetOwner(p1);
        // List<Prop> cList = new List<Prop>();
        // cList.Add(c);
        // List<Prop> p1CitiesList = Map.GetCities(p1).ToList<Prop>();
        // Assert.AreEqual(p1CitiesList, cList);
    }

    [Test]
    public void UnitOwnershipTest(){
        Player p1 = Map.Players[0];
        Player p2 = Map.Players[1];
        Coordinates point = new Coordinates(0,0);

        Infantry i = UnitData.GetNew<Infantry>();
        i.SetMeshName();
        i.Initialize(p1, point, SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);

        Infantry i2 = UnitData.GetNew<Infantry>();
        i2.SetMeshName();
        i2.Initialize(null, point, SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);

        Assert.IsTrue(i.IsOwn(p1));
        Assert.IsFalse(i.IsOwn(p2));
        Assert.IsTrue(i.IsFriendly(p1));
        Assert.IsFalse(i.IsFriendly(p2));
        Assert.IsTrue(i2.IsNeutral());
    }

    [Test]
    public void UnitFunctionTest(){
        Player p1 = Map.Players[0];

        Coordinates point1 = new Coordinates(0,0);
        Coordinates point2 = new Coordinates(1,1);
        Tile tile1 = Map.GetTile(point1);
        Tile tile2 = Map.GetTile(point2);

        Infantry i = UnitData.GetNew<Infantry>();
        i.SetMeshName();
        i.Initialize(p1, point1, SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);
        IEnumerable<SteelOfStalin.Assets.Customizables.IOffensiveCustomizable> weapons = i.GetWeapons();
        i.SetWeapons(weapons);

        Assert.IsTrue(i.CanAccessTile(tile1));
        Assert.IsFalse(i.CanAccessTile(tile2));

        // Assert.IsTrue(i.CanMove());

    }

    [Test]
    public void GetPathTest()
    {
        Player p1 = Map.Players[0];
        Infantry i = UnitData.GetNew<Infantry>();
        i.SetMeshName();
        i.Initialize(p1, new Coordinates(1, 1), SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);

        List<Tile> path = i.GetPath(i.GetLocatedTile(), Map.Instance.GetTile(1, 3)).ToList();
        Assert.AreEqual(2, path.Count);
    }

    [Test]
    public void CommandsTest()
    {
        Player p1 = Map.Players[0];

        Infantry i = UnitData.GetNew<Infantry>(); 
        UnitBuilding train_ground = Map.Instance.GetBuildings(p1.Capital.CoOrds).OfType<Barracks>().FirstOrDefault();

        Command train = new Train(i, train_ground, p1);
        Debug.Log(train.ToStringBeforeExecution());
        train.Execute();
        Debug.Log(train.ToStringAfterExecution());

        i.Status = SteelOfStalin.Assets.Props.Units.UnitStatus.CAN_BE_DEPLOYED;
        List<IOffensiveCustomizable> weapons = new List<IOffensiveCustomizable>()
        {
            CustomizableData.GetNew<IOffensiveCustomizable>(i.AvailableFirearms[1]) as Firearm
        };
        Coordinates coords = Utilities.Random.NextItem(train_ground.GetDeployableDestinations(i).Select(t => t.CoOrds));
        Command deploy = new Deploy(i, train_ground, coords, weapons);
        deploy.Execute();
        Debug.Log(deploy.ToStringAfterExecution());

        Tile end_point = Utilities.Random.NextItem(i.GetMoveRange());

        List<Command> commands = new List<Command>();
        Command move = new Move(i, i.GetPath(i.GetLocatedTile(), end_point).ToList());
        commands.Add(move);

        Command hold = new Hold(i);
        commands.Add(hold);

        Command ambush = new Ambush(i);
        commands.Add(ambush);

        Player p2 = Map.Players[1];
        Assault a = UnitData.GetNew<Assault>();
        a.Initialize(p2, Utilities.Random.NextItem(coords.GetNeighbours()), SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);

        Command fire = new Fire(i, a, i.PrimaryFirearm);
        commands.Add(fire);

        Command suppress = new Suppress(i, a, i.PrimaryFirearm);
        commands.Add(suppress);

        Building outpost = BuildingData.GetNew<Outpost>();
        outpost.Initialize(p2, new Coordinates(coords.X - 1, coords.Y));

        Command sabotage = new Sabotage(i, outpost, i.PrimaryFirearm);
        commands.Add(sabotage);

        Portable portable = UnitData.GetNew<Portable>();
        portable.Initialize(p1, Utilities.Random.NextItem(coords.GetNeighbours()), SteelOfStalin.Assets.Props.Units.UnitStatus.ACTIVE);
        portable.SetWeapons(new List<IOffensiveCustomizable>()
        {
            CustomizableData.GetNew<IOffensiveCustomizable>(portable.AvailableGuns[0]) as Gun
        });
        Command assemble = new Assemble(portable);
        commands.Add(assemble);

        Command disassemble = new Disassemble(portable);
        commands.Add(disassemble);

        Debug.Log("Before:\n" + string.Join(Environment.NewLine, commands.Select(c => c.ToStringBeforeExecution())));

        move.Execute();
        hold.Execute();
        ambush.Execute();
        fire.Execute();
        suppress.Execute();
        sabotage.Execute();
        assemble.Execute();
        disassemble.Execute();

        Debug.Log("After:\n" + string.Join(Environment.NewLine, commands.Select(c => c.ToStringAfterExecution())));
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator GameLogicTestWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
