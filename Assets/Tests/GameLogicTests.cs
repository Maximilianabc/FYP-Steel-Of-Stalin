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
        i.SetMeshName();
        Assert.IsNotNull(i);

        Assert.IsTrue(Map.AddUnit(i));
        Assert.IsFalse(Map.AddUnit(null));
        Assert.IsFalse(Map.AddUnit(i));
        Assert.IsTrue(Map.GetUnits().Contains(i));
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
