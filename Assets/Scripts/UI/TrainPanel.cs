using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin.Commands;
using SteelOfStalin;

public class TrainPanel : MonoBehaviour
{
    public static TrainPanel instance; 
    public int currentPage;
    public Cities currentCity;
    public UnitBuilding selectedUnitBuilding;
    public Sprite buttonRed;
    public Sprite buttonGreen;
    public Sprite transparent;
    public float animationTime=1.5f;
    [SerializeField]
    private int maxItemsInPage;

    public List<Cities> cities;
    public List<UnitBuilding> buildings;

    public Color barracksColor;
    public Color arsenalColor;
    public Color airfieldColor;
    public Color dockyardColor;
    public Color hightlightColor;
    public Color transparentColor = Color.clear;
    private GameObject citySelection;
    private GameObject unitList;
    private GameObject pageFlipButton;

    private List<Unit> trainableUnits;
    private List<Unit> unitsSubList;

    void Awake()
    {
        instance = this;
        citySelection = transform.Find("City").gameObject;
        unitList = transform.Find("UnitList").gameObject;
        trainableUnits = Game.UnitData.FYPImplement.ToList();
        citySelection.GetComponent<TMPro.TMP_Dropdown>().onValueChanged.AddListener(delegate
        {
            int cityIndex = (int)Mathf.Clamp(citySelection.GetComponent<TMPro.TMP_Dropdown>().value, 0f, cities.Count - 1);
            SetCity(cities[cityIndex]);
        });
        GetComponent<Button>().onClick.AddListener(delegate { UnselectUnitBuilding(); });
        pageFlipButton = transform.Find("PageFlipContainer").Find("PageFlip").gameObject;

    }
    // Start is called before the first frame update
    void Start()
    {
        pageFlipButton.GetComponent<Button>().onClick.AddListener(delegate {
            if (currentCity == null) return;
            DeployPanel.instance.SetCity(currentCity);
        });
        //should be handled after round start
        LeanTween.delayedCall(0.2f, (System.Action)delegate
        {
            SetCity(Battle.Instance.Self.Capital);
        });



    }

    // Update is called once per frame
    void Update()
    {

    }

    public void RedrawUnitsList()
    {
        float height = 0;
        height += gameObject.GetComponent<VerticalLayoutGroup>().padding.top;
        height += gameObject.GetComponent<VerticalLayoutGroup>().padding.bottom;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                height += child.gameObject.GetComponent<RectTransform>().sizeDelta.y;
                height += gameObject.GetComponent<VerticalLayoutGroup>().spacing;
            }
        }
        height -= gameObject.GetComponent<VerticalLayoutGroup>().spacing;
        float maxHeight = GetComponent<RectTransform>().sizeDelta.y;
        unitList.GetComponent<RectTransform>().sizeDelta += new Vector2(0, maxHeight - height);
        float unitListItemsHeight = unitList.GetComponent<RectTransform>().sizeDelta.y - 50;
        Transform unitListItems = unitList.transform.Find("UnitListItems");
        unitListItemsHeight -= unitListItems.GetComponent<VerticalLayoutGroup>().padding.top;
        unitListItemsHeight -= unitListItems.GetComponent<VerticalLayoutGroup>().padding.bottom;
        unitListItemsHeight -= unitListItems.GetComponent<VerticalLayoutGroup>().spacing;
        maxItemsInPage = Mathf.FloorToInt(unitListItemsHeight / (unitListItems.GetChild(0).GetComponent<RectTransform>().sizeDelta.y + unitListItems.GetComponent<VerticalLayoutGroup>().spacing));

        Transform navigationButtons = unitList.transform.Find("NavigationButtons");
        int numPage = Mathf.CeilToInt((float)trainableUnits.Count / maxItemsInPage);
        foreach (Transform child in navigationButtons)
        {
            Destroy(child.gameObject);
        }
        GameObject navigationButton = Game.GameObjects.Find(g=>g.name=="TrainMenuButtonGreen");
        for (int i = 1; i <= numPage; i++)
        {
            int x = i;
            GameObject instance = Instantiate(navigationButton,navigationButtons,false);
            instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = x.ToString();
            instance.GetComponent<Button>().onClick.AddListener(delegate { SetPage(x); });
            instance.name = x.ToString();
        }
        if (numPage >= 1) { 
            SetPage(1);
        }
    }

    public void SetPage(int pageNum)
    {
        foreach (Transform child in unitList.transform.Find("NavigationButtons")) {
            if (child.name == pageNum.ToString())
            {
                child.GetComponent<Image>().sprite = buttonRed;
            }
            else {
                child.GetComponent<Image>().sprite = buttonGreen;
            }
            
        }
        
        currentPage = pageNum;
        unitsSubList = trainableUnits.GetRange((pageNum - 1)*maxItemsInPage, System.Math.Min(maxItemsInPage,trainableUnits.Count- (pageNum - 1) * maxItemsInPage));
        foreach (Transform child in unitList.transform.Find("UnitListItems")) {
            Destroy(child.gameObject);
        }
        foreach (Unit u in unitsSubList) {
            GameObject instance = Instantiate(Game.GameObjects.Find(g => g.name == "UnitListItem"), unitList.transform.Find("UnitListItems"),false);
            instance.transform.Find("Icon").GetComponent<Image>().sprite = Game.Icons.Find(I => I.name == u.Name);
            instance.transform.Find("Name").GetComponent<TMPro.TMP_Text>().text = u.Name;
            if (u is Personnel) instance.transform.Find("Banner").GetComponent<Image>().color=barracksColor;
            else if(u is Vehicle || u is Artillery) instance.transform.Find("Banner").GetComponent<Image>().color = arsenalColor;
            instance.GetComponent<Button>().onClick.AddListener(delegate { SelectUnit(u); });
        }
        

    }

    public void ResetCity()
    {
        cities = Battle.Instance.Map.GetCities(Battle.Instance.Self).ToList();
        citySelection.GetComponent<TMPro.TMP_Dropdown>().ClearOptions();
        citySelection.GetComponent<TMPro.TMP_Dropdown>().AddOptions(cities.ConvertAll<string>(c=>$"{c.Name} {c.CubeCoOrds}"));
        currentCity = null;
    }

    public void SetCity(Cities city) {
        ResetCity();
        currentCity = city;
        CameraController.instance.FocusOn(city.PropObject.transform);
        citySelection.GetComponent<TMPro.TMP_Dropdown>().SetValueWithoutNotify(cities.IndexOf(city));
        RedrawQueues();
        Show();
    }

    public void RedrawQueues() {
        buildings =Battle.Instance.Map.GetBuildings(currentCity.CoOrds).Where(b => b is UnitBuilding).ToList().ConvertAll<UnitBuilding>(b=>(UnitBuilding)b.Clone());
        transform.Find("barracks").gameObject.SetActive(false);
        transform.Find("arsenal").gameObject.SetActive(false);
        transform.Find("dockyard").gameObject.SetActive(false);
        transform.Find("airfield").gameObject.SetActive(false);
        selectedUnitBuilding = null;
        foreach (UnitBuilding b in buildings) {
            foreach (Command c in Battle.Instance.Self.Commands) {
                if (c is Train) {
                    Train train = c as Train;
                    if (train.TrainingGround == b) {
                        b.TrainingQueue.Enqueue(train.Unit);
                    }
                }
            }
            Transform t = transform.Find(b.Name);
            if (t != null) {
                t.gameObject.SetActive(true);
                t.GetComponent<Image>().color = transparentColor;
                t.GetComponent<Button>().onClick.RemoveAllListeners();
                t.GetComponent<Button>().onClick.AddListener(delegate
                {
                    SelectUnitBuilding(b);
                });
                for (int i = 0; i < 8; i++) {
                    t.Find("Queue").Find($"Item{i}").GetComponent<Image>().sprite =i<b.TrainingQueue.Count?Game.Icons.Find(s => s.name == b.TrainingQueue.ElementAt(i).Name):transparent;
                    if (i < b.TrainingQueue.Count) {
                        EventTrigger et = t.Find("Queue").Find($"Item{i}").GetComponent<EventTrigger>();
                        et.triggers.Clear();
                        EventTrigger.Entry onEnter = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.PointerEnter,
                            callback = new EventTrigger.TriggerEvent()
                        };
                        int x = i;
                        onEnter.callback.AddListener(new UnityAction<BaseEventData>(e => {Tooltip.ShowTooltip_Static(b.TrainingQueue.ElementAt(x).Name); }));
                        et.triggers.Add(onEnter);
                        EventTrigger.Entry onExit = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.PointerExit,
                            callback = new EventTrigger.TriggerEvent()
                        };
                        onExit.callback.AddListener(new UnityAction<BaseEventData>(e=>Tooltip.HideTooltip_Static()));
                        et.triggers.Add(onExit);
                    }
                    //Update progress for first item
                    if (i == 0) {
                        if (b.TrainingQueue.Count > 0)
                        {
                            float progress = 1-(float)b.CurrentQueueTime / (float)b.TrainingQueue.ElementAt(0).Cost.Manufacture.Time.Value;
                            t.Find("Queue").Find($"Item{i}").Find("Progress").GetComponent<Image>().fillAmount = progress;
                        }
                        else {
                            t.Find("Queue").Find($"Item{i}").Find("Progress").GetComponent<Image>().fillAmount = 0;
                        }
                        
                    }

                }
            }            
        }
        RedrawUnitsList();
    }

    public void SelectUnit(Unit u) {
        if (selectedUnitBuilding == null) return;
        if (u == null) return;
        if (selectedUnitBuilding.TrainingQueue.Count >= selectedUnitBuilding.QueueCapacity) return;
        if (!Battle.Instance.Self.HasEnoughResources(u.Cost.Base)) return;
        if (selectedUnitBuilding is Barracks && !(u is Personnel)) return;
        if (selectedUnitBuilding is Arsenal && !(u is Vehicle||u is Artillery)) return;
        //TODO: all sorts of checking, prediction of resources consumption
        //maybe only call a function to update the resources 
        Battle.Instance.Self.Commands.Add(new Train((Unit)u.Clone(), selectedUnitBuilding, Battle.Instance.Self));
        RedrawQueues();
    }

    public void SelectUnitBuilding(UnitBuilding ub) {
        if (ub == null) return;
        if (ub == selectedUnitBuilding) {
            UnselectUnitBuilding();
            return;
        }
        UnselectUnitBuilding();
        Transform t = transform.Find(ub.Name);
        t.GetComponent<Image>().color = hightlightColor;
        selectedUnitBuilding = ub;
    }

    public void UnselectUnitBuilding() {
        if (selectedUnitBuilding == null) return;
        Transform t = transform.Find(selectedUnitBuilding.Name);
        t.GetComponent<Image>().color = transparentColor;
        selectedUnitBuilding = null;

    }

    public void Show() {
        gameObject.SetActive(true);
        transform.parent.Find("Panel_Left_Deploy").gameObject.SetActive(false);
        LeanTween.moveX(transform.parent.GetComponent<RectTransform>(), 0f, animationTime);


    }

    public void HideAll() {
        LeanTween.moveX(transform.parent.GetComponent<RectTransform>(), -transform.parent.GetComponent<RectTransform>().sizeDelta.x, animationTime);
    }
}
