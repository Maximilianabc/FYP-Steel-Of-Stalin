using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using NUnit.Framework;
using SteelOfStalin.Assets;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.Assets.Customizables.Shells;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.DataIO;
using SteelOfStalin.Util;
using UnityEngine;
using UnityEngine.TestTools;
using Module = SteelOfStalin.Assets.Customizables.Module;

namespace SteelOfStalin.Tests
{
    public class AssetLoadingTests
    {
        public UnitData UnitData { get; set; } = new UnitData();
        public BuildingData BuildingData { get; set; } = new BuildingData();
        public TileData TileData { get; set; } = new TileData();
        public CustomizableData CustomizableData { get; set; } = new CustomizableData();

        [SetUp]
        public void TestSetUp()
        {
            UnitData.Load();
            BuildingData.Load();
            TileData.Load();
            CustomizableData.Load();
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void UnitLoadingTest() => TestHelper.LoadingTest(UnitData.All, prop => prop.PropertyType.IsSubclassOf(typeof(Customizable)));

        [Test]
        public void BuildingLoadingTest() => TestHelper.LoadingTest(BuildingData.All);

        [Test]
        public void TileLoadingTest() => TestHelper.LoadingTest(TileData.All);

        [Test]
        public void CustomizableLoadingTest() => TestHelper.LoadingTest(CustomizableData.All, prop => prop.PropertyType == typeof(Shell) || (prop.PropertyType.IsSubclassOf(typeof(Module)) && prop.PropertyType != typeof(CannonBreech)));

        [Test]
        public void GetNewUnitTest() => TestHelper.GetNewAssetTest(UnitData);

        [Test]
        public void GetNewBuildingTest() => TestHelper.GetNewAssetTest(BuildingData);

        [Test]
        public void GetNewTileTest() => TestHelper.GetNewAssetTest(TileData);

        [Test]
        public void GetNewCustomizableTest() => TestHelper.GetNewAssetTest(CustomizableData);

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

    public class TestHelper
    {
        // has public setter and is not ignored in serialization
        private static Func<PropertyInfo, bool> m_can_set_and_not_ignored => prop => prop.CanWrite && (bool)prop.GetSetMethod()?.IsPublic && prop.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null;

        public static void LoadingTest<T>(IEnumerable<T> data, params Func<PropertyInfo, bool>[] null_conditions) where T : INamedAsset 
        {
            foreach (T t in data)
            {
                IEnumerable<PropertyInfo> props = t.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(m_can_set_and_not_ignored);
                foreach (PropertyInfo prop in props)
                {
                    bool null_conditions_result = null_conditions.Length > 0 && null_conditions.AndAll().Invoke(prop);
                    if (null_conditions_result)
                    {
                        // some properties are expected to be null
                        Assert.IsNull(prop.GetValue(t), $"{t.Name}: {prop.Name}");
                    }
                    else
                    {
                        Assert.IsNotNull(prop.GetValue(t), $"{t.Name}: {prop.Name}");
                    }
                }
            }
        }

        public static void GetNewAssetTest<T>(Data<T> data) where T : INamedAsset, ICloneable
        {
            foreach (T stat_t in data.All)
            {
                T t = data.GetNew(stat_t.Name);
                Assert.IsNotNull(t);

                IEnumerable<PropertyInfo> t_props = t.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(m_can_set_and_not_ignored);
                IEnumerable<PropertyInfo> stat_t_props = stat_t.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(m_can_set_and_not_ignored);

                Assert.Greater(t_props.Count(), 0);
                Assert.Greater(stat_t_props.Count(), 0);
                Assert.IsTrue(t_props.Count() == stat_t_props.Count());

                Dictionary<PropertyInfo, PropertyInfo> properties = t_props.Zip(stat_t_props, (ap, asset_p) => new { ap, asset_p }).ToDictionary(p => p.ap, p => p.asset_p);
                foreach (KeyValuePair<PropertyInfo, PropertyInfo> pair in properties)
                {
                    object t_val = pair.Key.GetValue(t);
                    object stat_t_val = pair.Value.GetValue(stat_t);

                    // skip checking for sub-modules
                    if (pair.Key.PropertyType.IsSubclassOf(typeof(Module)))
                    {
                        continue;
                    }
                    Assert.AreEqual(stat_t_val, t_val, $"Asset Name: {t.Name}\nMismatched property: {pair.Key.Name};");
                }
            }
            _ = Assert.Throws<ArgumentException>(() => data.GetNew("drawing_models_is_tiresome"));
        }
    }
}
