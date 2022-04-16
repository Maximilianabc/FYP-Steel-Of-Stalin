using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin;

public class TrainPanel : MonoBehaviour
{
    public int currentPage;
    public Sprite buttonRed;
    public Sprite buttonGreen;
    [SerializeField]
    private int maxItemsInPage;

    private GameObject citySelection;
    private GameObject barrack;
    private GameObject arsenal;
    private GameObject dockyard;
    private GameObject airfield;
    private GameObject unitList;


    private List<Unit> barrackQueue;
    private List<Unit> arsenalQueue;
    private List<Unit> dockyardQueue;
    private List<Unit> airfieldQueue;

    private List<Unit> trainableUnits;

    void Awake()
    {
        citySelection = transform.Find("City").gameObject;
        barrack = transform.Find("Barrack").gameObject;
        arsenal = transform.Find("Arsenal").gameObject;
        dockyard = transform.Find("Dockyard").gameObject;
        airfield = transform.Find("Airfield").gameObject;
        unitList = transform.Find("UnitList").gameObject;
        //trainableUnits = Game.UnitData.FYPImplement.ToList();
        trainableUnits = new List<Unit>();
        for (int i = 0; i < 30; i++) {
            trainableUnits.Add(new SteelOfStalin.Assets.Props.Units.Land.Personnels.Infantry());
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        RedrawUnitsList();
        SetPage(1);
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
}
