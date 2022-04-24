using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;

public class ResourcesPanel : MonoBehaviour
{
    public static ResourcesPanel instance;
    public Text roundDisplay;
    public Text timeDisplay;
    public GameObject money;
    public GameObject steel;
    public GameObject cartridges;
    public GameObject shells;
    public GameObject fuel;
    public GameObject supplies;
    public GameObject manpower;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timeDisplay.text = $"{System.DateTime.Now.Hour:D2}:{System.DateTime.Now.Minute:D2}";
        roundDisplay.text = $"Round{SteelOfStalin.Battle.Instance.RoundNumber}";
        money.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Money.ApplyMod().ToString("D");
        steel.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Steel.ApplyMod().ToString("D");
        cartridges.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Cartridges.ApplyMod().ToString("D");
        shells.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Shells.ApplyMod().ToString("D");
        fuel.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Fuel.ApplyMod().ToString("D");
        supplies.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Supplies.ApplyMod().ToString("D");
        manpower.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Manpower.ApplyMod().ToString("D");
    }
}
