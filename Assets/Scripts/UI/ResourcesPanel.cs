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
    public GameObject readyButton;
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
        readyButton.GetComponent<Button>().onClick.AddListener(delegate { GameObject.Find(Battle.Instance.Self.Name).GetComponent<PlayerObject>().ChangeReadyStatus(true); });
    }

    // Update is called once per frame
    void Update()
    {
        timeDisplay.text = $"{System.DateTime.Now.Hour:D2}:{System.DateTime.Now.Minute:D2}";
        roundDisplay.text = $"Round{SteelOfStalin.Battle.Instance.RoundNumber}";
        money.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Money.ApplyMod().ToString("F0");
        steel.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Steel.ApplyMod().ToString("F0");
        cartridges.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Cartridges.ApplyMod().ToString("F0");
        shells.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Shells.ApplyMod().ToString("F0");
        fuel.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Fuel.ApplyMod().ToString("F0");
        supplies.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Supplies.ApplyMod().ToString("F0");
        manpower.transform.Find("Text").GetComponent<Text>().text = Battle.Instance.Self.Resources.Manpower.ApplyMod().ToString("F0");
    }
}
