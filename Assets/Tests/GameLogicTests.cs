using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SteelOfStalin;
using SteelOfStalin.DataIO;
using UnityEngine;
using UnityEngine.TestTools;

public class GameLogicTest
{
    public UnitData UnitData { get; set; } = new UnitData();
    public BuildingData BuildingData { get; set; } = new BuildingData();
    public TileData TileData { get; set; } = new TileData();
    public CustomizableData CustomizableData { get; set; } = new CustomizableData();
    public Map Map { get; set; }

    [SetUp]
    public void TestSetUp()
    {
        UnitData.Load();
        BuildingData.Load();
        TileData.Load();
        CustomizableData.Load();
        Map.Load();
    }

    // A Test behaves as an ordinary method
    [Test]
    public void GameLogicTestSimplePasses()
    {
        // Use the Assert class to test conditions
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
