using SteelOfStalin;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Commands;
using SteelOfStalin.CustomTypes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeployPanel : MonoBehaviour
{
    public static DeployPanel instance;
    public int currentPage;
    public Cities currentCity;
    public Sprite buttonRed;
    public Sprite buttonGreen;
    public Sprite transparent;
    public float animationTime = 1.5f;
    public int maxItemsInPage = 6;

    public List<Cities> cities;
    public List<UnitBuilding> buildings;
    public List<IOffensiveCustomizable> weapons;
    public List<IOffensiveCustomizable> weaponOptions;
    public List<Tile> destinationTiles;

    public Color barracksColor;
    public Color arsenalColor;
    public Color airfieldColor;
    public Color dockyardColor;
    private GameObject citySelection;
    private GameObject unitList;
    private GameObject customizableParts;
    private GameObject customizableOptions;
    private GameObject deployButton;
    private GameObject pageFlipButton;

    private List<Unit> deployableUnits;
    private List<Unit> unitsSubList;
    private Unit selectedUnit;
    private IOffensiveCustomizable selectedWeapon;
    private bool isWaitingInput = false;

    private void Awake()
    {
        instance = this;
        citySelection = transform.Find("City").gameObject;
        unitList = transform.Find("UnitList").gameObject;
        customizableParts = transform.Find("CustomizablePanel").Find("CustomizableParts").gameObject;
        customizableOptions = transform.Find("CustomizablePanel").Find("CustomizableOptions").Find("Items").Find("Viewport").Find("Content").gameObject;
        deployButton = transform.Find("Buttons").Find("Deploy").gameObject;
        deployButton.GetComponent<TMPro.TMP_Text>().text = "Deploy";
        deployButton.GetComponent<Button>().onClick.AddListener(delegate
        {
            ButtonDeployOnclickHandler();
        });
        pageFlipButton = transform.Find("Buttons").Find("PageFlip").gameObject;
        pageFlipButton.GetComponent<Button>().onClick.AddListener(delegate
        {
            if (currentCity == null) return;
            TrainPanel.instance.SetCity(currentCity);
        });

    }

    // Start is called before the first frame update
    private void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {

    }

    public void ResetCity()
    {
        cities = Battle.Instance.Map.GetCities(Battle.Instance.Self).ToList();
        citySelection.GetComponent<TMPro.TMP_Dropdown>().ClearOptions();
        citySelection.GetComponent<TMPro.TMP_Dropdown>().AddOptions(cities.ConvertAll<string>(c => $"{c.Name} {c.CoOrds}"));
        currentCity = null;
    }

    public void SetCity(Cities city)
    {
        if (!UIUtil.instance.isListenToUIEvent) return;
        ResetCity();
        currentCity = city;
        CameraController.instance.FocusOn(city.PropObject.transform);
        citySelection.GetComponent<TMPro.TMP_Dropdown>().SetValueWithoutNotify(cities.IndexOf(city));
        selectedUnit = null;
        RedrawUnitsList();
        Show();
    }

    public void RedrawUnitsList()
    {
        buildings = Battle.Instance.Map.GetBuildings(currentCity.CoOrds).Where(b => b is UnitBuilding).ToList().ConvertAll<UnitBuilding>(b => (UnitBuilding)b.Clone());
        deployableUnits = new List<Unit>();
        foreach (UnitBuilding b in buildings)
        {
            Debug.Log($"{b.Name}:{b.ReadyToDeploy.Count},{b.Status}");
            foreach (Command c in Battle.Instance.Self.Commands)
            {
                if (c is Deploy)
                {
                    Deploy deploy = c as Deploy;
                    if (deploy.TrainingGround.MeshName == b.MeshName)
                    {
                        //remove units from deployable units
                        b.ReadyToDeploy.Remove(deploy.Unit);
                    }
                }
            }
            deployableUnits.AddRange(b.ReadyToDeploy);
        }
        Debug.Log($"buildings count:{buildings.Count}");
        Debug.Log($"deployable units count:{deployableUnits.Count}");


        Transform unitListItems = unitList.transform.Find("UnitListItems");
        Transform navigationButtons = unitList.transform.Find("NavigationButtons");
        foreach (Transform child in unitListItems)
        {
            Destroy(child.gameObject);
        }
        int numPage = Mathf.CeilToInt((float)deployableUnits.Count / maxItemsInPage);
        Debug.Log("numPage" + numPage);
        foreach (Transform child in navigationButtons)
        {
            Destroy(child.gameObject);
        }
        GameObject navigationButton = Resources.Load<GameObject>(@"Prefabs\TrainMenuButtonGreen");
        for (int i = 1; i <= numPage; i++)
        {
            int x = i;
            GameObject instance = Instantiate(navigationButton, navigationButtons, false);
            instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = x.ToString();
            instance.GetComponent<Button>().onClick.AddListener(delegate { SetPage(x); });
            instance.name = x.ToString();
        }
        if (numPage >= 1) SetPage(1);
        RedrawCustomizablePart();
    }

    public void SetPage(int pageNum)
    {
        selectedUnit = null;
        foreach (Transform child in unitList.transform.Find("NavigationButtons"))
        {
            if (child.name == pageNum.ToString())
            {
                child.GetComponent<Image>().sprite = buttonRed;
            }
            else
            {
                child.GetComponent<Image>().sprite = buttonGreen;
            }

        }
        currentPage = pageNum;
        unitsSubList = deployableUnits.GetRange((pageNum - 1) * maxItemsInPage, System.Math.Min(maxItemsInPage, deployableUnits.Count - (pageNum - 1) * maxItemsInPage));
        foreach (Transform child in unitList.transform.Find("UnitListItems"))
        {
            Destroy(child.gameObject);
        }
        foreach (Unit u in unitsSubList)
        {
            GameObject instance = Instantiate(Game.GameObjects.Find(g => g.name == "UnitListItem"), unitList.transform.Find("UnitListItems"), false);
            instance.transform.Find("Icon").GetComponent<Image>().sprite = Game.Icons.Find(I => I.name == u.Name);
            instance.transform.Find("Name").GetComponent<TMPro.TMP_Text>().text = u.Name.Replace('_', ' ');
            if (u is Personnel || u is Artillery) instance.transform.Find("Banner").GetComponent<Image>().color = barracksColor;
            else if (u is Vehicle) instance.transform.Find("Banner").GetComponent<Image>().color = arsenalColor;
            //TODO: air and sea units
            instance.GetComponent<Button>().onClick.AddListener(delegate { SelectUnit(u); });
        }
    }

    public void SelectUnit(Unit u)
    {
        if (selectedUnit == u) return;
        selectedUnit = u;
        weapons = null;
        RedrawCustomizablePart();

    }

    public void RedrawCustomizablePart()
    {
        foreach (Transform child in customizableParts.transform)
        {
            Destroy(child.gameObject);
        }
        //only clean up if no unitselected
        if (selectedUnit != null)
        {
            //update weapons only if a new unit is selected
            if (weapons == null) weapons = selectedUnit.GetWeapons().ToList();

            for (int i = 0; i < weapons.Count; i++)
            {
                //initialize default weapons
                if (weapons[i] == null)
                {
                    if (selectedUnit is Personnel p)
                    {
                        if (i == 0)
                        {
                            weapons[i] = Game.CustomizableData.GetNewFirearm(p.DefaultPrimary);
                        }
                    }
                    else if (selectedUnit is Vehicle v)
                    {
                        weapons[i] = (IOffensiveCustomizable)Game.CustomizableData.GetNewModule(v.DefaultMainArmament);
                    }
                    else if (selectedUnit is Artillery a)
                    {
                        weapons[i] = (IOffensiveCustomizable)Game.CustomizableData.GetNewModule(a.DefaultGun);
                    }
                }
                IOffensiveCustomizable weapon = weapons[i];
                //show weapons
                GameObject instance = Instantiate(Game.GameObjects.Find(g => g.name == "DeployPanelWeapon"), customizableParts.transform, false);
                if (weapon != null)
                {
                    instance.name = weapon.Name;
                    Sprite icon = Game.Icons.Find(s => s.name == weapon.Name);
                    if (icon != null)
                    {
                        instance.transform.Find("Icon").GetComponent<Image>().sprite = icon;
                    }
                    else
                    {
                        instance.transform.Find("Icon").GetComponent<Image>().sprite = transparent;
                    }
                }
                else
                {
                    instance.name = "Empty";
                    instance.transform.Find("Icon").GetComponent<Image>().sprite = transparent;
                }


                if (instance.GetComponent<EventTrigger>() == null)
                {
                    instance.AddComponent<EventTrigger>();
                }
                EventTrigger et = instance.GetComponent<EventTrigger>();
                et.triggers.Clear();
                EventTrigger.Entry onEnter = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerEnter,
                    callback = new EventTrigger.TriggerEvent()
                };
                onEnter.callback.AddListener(new UnityAction<BaseEventData>(e =>
                {
                    StringBuilder sb = new StringBuilder();
                    if (weapon != null)
                    {
                        sb.AppendLine(weapon.Name.Replace('_', ' '));
                        sb.AppendLine($"Range: {weapon.Offense.MinRange.Value}-{weapon.Offense.MaxRange.Value}");
                        sb.AppendLine($"Accuracy: {weapon.Offense.Accuracy.Normal.Value}");
                        sb.AppendLine($"Hard Damage:{weapon.Offense.Damage.Hard.Value}");
                        sb.AppendLine($"Soft Damage:{weapon.Offense.Damage.Soft.Value}");
                        sb.Append($"Destruction Damage:{weapon.Offense.Damage.Destruction.Value}");
                    }
                    else
                    {
                        sb.Append("Empty");
                    }
                    Tooltip.ShowTooltip_Static(sb.ToString());

                }));
                et.triggers.Add(onEnter);
                EventTrigger.Entry onExit = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerExit,
                    callback = new EventTrigger.TriggerEvent()
                };
                onExit.callback.AddListener(new UnityAction<BaseEventData>(e => Tooltip.HideTooltip_Static()));
                et.triggers.Add(onExit);
                EventTrigger.Entry onClick = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick,
                    callback = new EventTrigger.TriggerEvent()
                };
                int x = i;
                onClick.callback.AddListener(new UnityAction<BaseEventData>(e => SelectWeapon(weapon, x)));
                et.triggers.Add(onClick);
                //Reset selected visual indicator 
                foreach (Transform child in customizableParts.transform)
                {
                    child.Find("Selected").gameObject.SetActive(false);
                }
            }
        }
        foreach (Transform child in customizableOptions.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SelectWeapon(IOffensiveCustomizable weapon, int index)
    {
        if (selectedUnit == null) return;
        selectedWeapon = weapon;
        //reset selected visual indicator
        foreach (Transform child in customizableParts.transform)
        {
            child.Find("Selected").gameObject.SetActive(false);
        }
        customizableParts.transform.GetChild(index).Find("Selected").gameObject.SetActive(true);

        //draw all options
        RedrawWeaponOptions(index);



    }

    public void RedrawWeaponOptions(int index)
    {
        foreach (Transform child in customizableOptions.transform)
        {
            Destroy(child.gameObject);
        }
        if (selectedUnit != null)
        {
            weaponOptions = null;
            if (selectedUnit is Personnel p)
            {
                if (index == 0)
                {
                    weaponOptions = p.AvailablePrimaryFirearms.ConvertAll<IOffensiveCustomizable>(s => Game.CustomizableData.GetNewFirearm(s));
                }
                else if (index == 1)
                {
                    weaponOptions = p.AvailableSecondaryFirearms.ConvertAll<IOffensiveCustomizable>(s => Game.CustomizableData.GetNewFirearm(s));
                    weaponOptions.Insert(0, null);
                }
                else
                {
                    Debug.LogError("undefined weapons");
                }
            }
            else if (selectedUnit is Artillery a)
            {
                weaponOptions = a.AvailableGuns.ConvertAll<IOffensiveCustomizable>(s => (IOffensiveCustomizable)Game.CustomizableData.GetNewModule(s));
            }


            if (weaponOptions != null)
            {
                foreach (IOffensiveCustomizable weaponOption in weaponOptions)
                {
                    GameObject instance = Instantiate(Game.GameObjects.Find(g => g.name == "DeployPanelWeaponOptions"), customizableOptions.transform, false);
                    //TODO: avoid using null as part of the logic for IOffensiveCustomizable, possible solution: create wrapper class for IOffensiveCustomizable
                    instance.name = weaponOption != null ? weaponOption.Name : "Empty";
                    if (weaponOption != null)
                    {
                        Sprite icon = Game.Icons.Find(s => s.name == weaponOption.Name);
                        if (icon != null)
                        {
                            instance.transform.Find("Icon").GetComponent<Image>().sprite = icon;
                        }
                        else
                        {
                            instance.transform.Find("Icon").GetComponent<Image>().sprite = transparent;
                        }
                    }
                    else
                    {
                        instance.transform.Find("Icon").GetComponent<Image>().sprite = transparent;
                    }
                    if (instance.GetComponent<EventTrigger>() == null)
                    {
                        instance.AddComponent<EventTrigger>();
                    }
                    EventTrigger et = instance.GetComponent<EventTrigger>();
                    et.triggers.Clear();
                    EventTrigger.Entry onEnter = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerEnter,
                        callback = new EventTrigger.TriggerEvent()
                    };
                    onEnter.callback.AddListener(new UnityAction<BaseEventData>(e =>
                    {
                        StringBuilder sb = new StringBuilder();
                        if (weaponOption != null)
                        {
                            sb.AppendLine(weaponOption.Name);
                            sb.AppendLine($"Range: {weaponOption.Offense.MinRange.Value}-{weaponOption.Offense.MaxRange.Value}");
                            sb.AppendLine($"Accuracy: {weaponOption.Offense.Accuracy.Normal.Value}");
                            sb.Append($"Damage:{weaponOption.Offense.Damage.Hard.Value}");
                        }
                        else
                        {
                            sb.Append("Empty");
                        }
                        Tooltip.ShowTooltip_Static(sb.ToString());

                    }));
                    et.triggers.Add(onEnter);
                    EventTrigger.Entry onExit = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerExit,
                        callback = new EventTrigger.TriggerEvent()
                    };
                    onExit.callback.AddListener(new UnityAction<BaseEventData>(e => Tooltip.HideTooltip_Static()));
                    et.triggers.Add(onExit);
                    EventTrigger.Entry onClick = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerClick,
                        callback = new EventTrigger.TriggerEvent()
                    };
                    onClick.callback.AddListener(new UnityAction<BaseEventData>(e =>
                    {
                        //handle weaponOption being chosen
                        if (selectedUnit == null) return;
                        weapons[index] = weaponOption;
                        RedrawCustomizablePart();
                    }));
                    et.triggers.Add(onClick);

                }
            }

        }

        if (transform.Find("CustomizablePanel").Find("CustomizableOptions").gameObject.activeInHierarchy)
        {
            transform.Find("CustomizablePanel").Find("CustomizableOptions").GetComponent<Resize>().DelayResize(1);
        }

    }

    private void OnEnable()
    {
        transform.Find("CustomizablePanel").Find("CustomizableOptions").GetComponent<Resize>().DelayResize(1);
    }


    public void PrepareDeployableTiles()
    {
        if (isWaitingInput)
        {
            Debug.Log("already deploying");
            return;
        }
        if (selectedUnit == null)
        {
            Debug.Log("Unit not set");
            return;
        }
        if (weapons == null)
        {
            Debug.Log("weapons not set");
            return;
        }
        UnitBuilding unitBuilding = buildings.Find(u => u.ReadyToDeploy.Contains(selectedUnit));
        destinationTiles = unitBuilding.GetDeployableDestinations(selectedUnit).ToList();
        foreach (Command c in Battle.Instance.Self.Commands)
        {
            if (c is Deploy d)
            {
                destinationTiles.RemoveAll(t => t.CoOrds == d.Destination);
            }
        }
        if (destinationTiles == null || destinationTiles.Count < 1)
        {
            Debug.Log("No possible deploy destination");
            return;
        }
        foreach (Tile t in destinationTiles)
        {
            t.PropObjectComponent.Highlight();
            PropEventTrigger trigger = t.PropObjectComponent.Trigger;
            trigger.SetActive("deploy_tile", true);
            trigger.SetActive("focus", false);
        }
        isWaitingInput = true;
        deployButton.GetComponent<TMPro.TMP_Text>().text = "Cancel";
    }

    public void DeployUnit(Tile t)
    {
        if (selectedUnit == null)
        {
            Debug.Log("Unit not set");
            return;
        }
        if (weapons == null)
        {
            Debug.Log("Weapons not set");
        }
        weapons.RemoveAll(w => w == null);
        UnitBuilding unitBuilding = Battle.Instance.Map.GetBuildings<UnitBuilding>().ToList().Find(u => u.ReadyToDeploy.Contains(selectedUnit));
        Deploy deploy = new Deploy(selectedUnit, unitBuilding, t.CoOrds, weapons);
        Battle.Instance.Self.Commands.Add(deploy);
        selectedUnit = null;
        CleanUpTrigger();
        RedrawUnitsList();
        //TODO: add animation?
    }


    public void CleanUpTrigger()
    {
        if (!isWaitingInput) return;
        if (destinationTiles == null || destinationTiles.Count < 1) return;
        foreach (Tile t in destinationTiles)
        {
            t.PropObjectComponent.RestoreHighlight();
            PropEventTrigger trigger = t.PropObjectComponent.Trigger;
            trigger.SetActive("deploy_tile", false);
            trigger.SetActive("focus", true);
        }
        destinationTiles = null;
        isWaitingInput = false;
        deployButton.GetComponent<TMPro.TMP_Text>().text = "Deploy";
    }

    private void ButtonDeployOnclickHandler()
    {
        //normal state
        if (!isWaitingInput)
        {
            PrepareDeployableTiles();
        }
        //waiting for tile being clicked
        else
        {
            CleanUpTrigger();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.parent.Find("Panel_Left_Train").gameObject.SetActive(false);
        LeanTween.moveX(transform.parent.GetComponent<RectTransform>(), 0f, animationTime);
    }

    public void HideAll()
    {
        CleanUpTrigger();
        LeanTween.moveX(transform.parent.GetComponent<RectTransform>(), -transform.parent.GetComponent<RectTransform>().sizeDelta.x, animationTime);

    }



}
