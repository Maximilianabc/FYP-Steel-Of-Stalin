using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Buildings.Units;
using SteelOfStalin;

public class TrainPanel : MonoBehaviour
{
    public int currentPage;
    public Cities currentCity;
    public Sprite buttonRed;
    public Sprite buttonGreen;
    private int maxItemsInPage;

    public List<Cities> cities;
    public List<UnitBuilding> buildings;
    private GameObject citySelection;
    private GameObject unitList;


    private Queue<Unit> barrackQueue;
    private Queue<Unit> arsenalQueue;
    private Queue<Unit> dockyardQueue;
    private Queue<Unit> airfieldQueue;
    private List<Unit> trainableUnits;

    void Awake()
    {
        citySelection = transform.Find("City").gameObject;
        unitList = transform.Find("UnitList").gameObject;
        trainableUnits = Game.UnitData.FYPImplement.ToList();
    }
    // Start is called before the first frame update
    void Start()
    {
        citySelection.GetComponent<TMPro.TMP_Dropdown>().onValueChanged.AddListener(delegate
        {
            int cityIndex = (int)Mathf.Clamp(citySelection.GetComponent<TMPro.TMP_Dropdown>().value, 0f, cities.Count - 1);
            SetCity(cities[cityIndex]);
        });
        LeanTween.delayedCall(0.5f, (System.Action)delegate
        {
            ResetCity();
            Debug.Log(cities.Count);
            //
            RedrawUnitsList();
            SetPage(1);
        });


    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Show()
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
        int numPage = Mathf.CeilToInt(trainableUnits.Count / maxItemsInPage);
        foreach (Transform child in navigationButtons)
        {
            Destroy(child.gameObject);
        }
        GameObject navigationButton = Resources.Load<GameObject>(@"Prefabs\TrainMenuButtonGreen");
        for (int i = 1; i <= numPage; i++)
        {
            int x = i;
            GameObject instance = Instantiate(navigationButton);
            instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = x.ToString();
            instance.GetComponent<Button>().onClick.AddListener(delegate { SetPage(x); });
            instance.name = x.ToString();
            instance.transform.SetParent(navigationButtons);
        }
    }

    public void SetPage(int pageNum)
    {
        Transform previousButton = unitList.transform.Find("NavigationButtons").Find(currentPage.ToString());
        if (previousButton != null) { 
            previousButton.GetComponent<Image>().sprite = buttonGreen;
        }
        unitList.transform.Find("NavigationButtons").Find(pageNum.ToString()).GetComponent<Image>().sprite = buttonRed;
        currentPage = pageNum;


    }

    public void ResetCity()
    {
        cities = Battle.Instance.Map.GetCities(Battle.Instance.Self).ToList();
        citySelection.GetComponent<TMPro.TMP_Dropdown>().ClearOptions();
        citySelection.GetComponent<TMPro.TMP_Dropdown>().AddOptions(cities.ConvertAll<string>(c=>c.Name));
        if (cities.Count > 0) SetCity(cities[0]);
    }

    public void SetCity(Cities city) {
        currentCity = city;
        CameraController.instance.FocusOn(GameObject.Find(city.MeshName).transform);
        RedrawQueues();
    }

    public void RedrawQueues() {
        buildings =Battle.Instance.Map.GetBuildings(currentCity.CoOrds).Where(b => b is UnitBuilding).ToList().ConvertAll<UnitBuilding>(b=>(UnitBuilding)b.Clone());
        transform.Find("barracks").gameObject.SetActive(false);
        transform.Find("arsenal").gameObject.SetActive(false);
        transform.Find("dockyard").gameObject.SetActive(false);
        transform.Find("airfield").gameObject.SetActive(false);
        foreach (UnitBuilding b in buildings) {
            Transform t = transform.Find(b.Name);
            if (t != null) {
                t.gameObject.SetActive(true);
                b.TrainingQueue
            }
        }
        
    }


}
