using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SteelOfStalin;
using SteelOfStalin.Assets.Props;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units.Land.Personnels;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.DataIO;
using UnityEngine;
using UnityEngine.TestTools;

public class GameLogicTest
{
    public UnitData UnitData { get; set; } = new UnitData();
    public BuildingData BuildingData { get; set; } = new BuildingData();
    public TileData TileData { get; set; } = new TileData();
    public CustomizableData CustomizableData { get; set; } = new CustomizableData();
    public Map Map { get; set; } = new Map();

    [SetUp]
    public void TestSetUp()
    {
        UnitData.Load();
        BuildingData.Load();
        TileData.Load();
        CustomizableData.Load();
        Map.Load();
    }

    [Test]
    public void GetNeighbourTest()
    {
        Coordinates random = new Coordinates(new System.Random().Next(100), new System.Random().Next(100));
        CubeCoordinates random_cube = (CubeCoordinates)random;

        IEnumerable<CubeCoordinates> cubes = random_cube.GetNeigbours();
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
        // i.SetMeshName();
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
        Player p2 = Map.Players[0];

        
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
