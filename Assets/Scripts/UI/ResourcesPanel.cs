using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SteelOfStalin;
using Resources = SteelOfStalin.Attributes.Resources;


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
    public Resources resources;

    public Button buttonOptions;
    public GameObject canvasMenu;

    private void Awake()
    {
        instance = this;
        resources = null;
    }
    // Start is called before the first frame update
    void Start()
    {
        readyButton.GetComponent<Button>().onClick.AddListener(delegate {
            Battle.Instance.Self.PlayerObjectComponent.ChangeReadyStatus(true);
            UIUtil.instance.RoundEndUIUpdate();
        });
        buttonOptions.onClick.AddListener(delegate
        {
            canvasMenu.SetActive(true);
        });
        canvasMenu.transform.Find("OptionMenu").Find("SaveQuit").GetComponent<Button>().onClick.AddListener(delegate {
            Battle.Instance.Save();
            Game.ShutDown();
            SceneManager.LoadScene("Menu");
        });
        canvasMenu.transform.Find("OptionMenu").Find("QuitWithoutSaving").GetComponent<Button>().onClick.AddListener(delegate {
            Game.ShutDown();
            SceneManager.LoadScene("Menu");
        });
    }

    // Update is called once per frame
    void Update()
    {
        timeDisplay.text = $"{Battle.Instance.TimeRemaining/60}:{Battle.Instance.TimeRemaining%60:D2}";
        roundDisplay.text = $"Round{Battle.Instance.RoundNumber}";
        if (resources != null) {
            money.transform.Find("Text").GetComponent<Text>().text = resources.Money.ApplyMod().ToString("F0");
            steel.transform.Find("Text").GetComponent<Text>().text = resources.Steel.ApplyMod().ToString("F0");
            cartridges.transform.Find("Text").GetComponent<Text>().text = resources.Cartridges.ApplyMod().ToString("F0");
            shells.transform.Find("Text").GetComponent<Text>().text = resources.Shells.ApplyMod().ToString("F0");
            fuel.transform.Find("Text").GetComponent<Text>().text = resources.Fuel.ApplyMod().ToString("F0");
            supplies.transform.Find("Text").GetComponent<Text>().text = resources.Supplies.ApplyMod().ToString("F0");
            manpower.transform.Find("Text").GetComponent<Text>().text = resources.Manpower.ApplyMod().ToString("F0");
        }
    }

    public void SetResources(Resources r) {
        resources = (Resources)r.Clone();
    }

    public bool Consume(Resources cost) {
        if (resources.HasEnoughResources(cost))
        {
            resources.Consume(cost);
            return true;
        }
        else return false;
    }

    public void Refund(Resources cost) {
        resources.Money.PlusEquals(cost.Money);
        resources.Steel.PlusEquals(cost.Steel);
        resources.Supplies.PlusEquals(cost.Supplies);
        resources.Cartridges.PlusEquals(cost.Cartridges);
        resources.Shells.PlusEquals(cost.Shells);
        resources.Fuel.PlusEquals(cost.Fuel);
        resources.RareMetal.PlusEquals(cost.RareMetal);
        resources.Manpower.PlusEquals(cost.Manpower);
    }
}
