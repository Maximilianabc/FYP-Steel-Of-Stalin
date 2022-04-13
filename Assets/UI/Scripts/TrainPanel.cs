using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin;

public class TrainPanel : MonoBehaviour
{
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

    void Awake() {
        citySelection = transform.Find("City").gameObject;
        barrack = transform.Find("Barrack").gameObject;
        arsenal = transform.Find("Arsenal").gameObject;
        dockyard = transform.Find("Dockyard").gameObject;
        airfield = transform.Find("Airfield").gameObject;
        unitList = transform.Find("UnitList").gameObject;
        //trainableUnits = Game.UnitData.FYPImplement.ToList();
        trainableUnits = new List<Unit>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ResizeUnitsList();
    }

    public void Show() {
        
    }

    public void ResizeUnitsList() {
        float height=0;
        height += gameObject.GetComponent<VerticalLayoutGroup>().padding.top;
        height += gameObject.GetComponent<VerticalLayoutGroup>().padding.bottom;
        foreach (Transform child in transform) {
            if (child.gameObject.activeSelf) {
                height += child.gameObject.GetComponent<RectTransform>().sizeDelta.y;
                height += gameObject.GetComponent<VerticalLayoutGroup>().spacing;
            }
        }
        height-= gameObject.GetComponent<VerticalLayoutGroup>().spacing;
        float maxHeight = GetComponent<RectTransform>().sizeDelta.y;
        unitList.GetComponent<RectTransform>().sizeDelta += new Vector2(0, maxHeight-height);
        float unitListItemsHeight = unitList.GetComponent<RectTransform>().sizeDelta.y - 50;
        Transform unitListItems = unitList.transform.Find("UnitListItems");
        unitListItemsHeight -= unitListItems.GetComponent<VerticalLayoutGroup>().padding.top;
        unitListItemsHeight -= unitListItems.GetComponent<VerticalLayoutGroup>().padding.bottom;
        unitListItemsHeight -= unitListItems.GetComponent<VerticalLayoutGroup>().spacing;
        maxItemsInPage = Mathf.FloorToInt(unitListItemsHeight / (unitListItems.GetChild(0).GetComponent<RectTransform>().sizeDelta.y + unitListItems.GetComponent<VerticalLayoutGroup>().spacing));

        Transform navigationButtons = unitList.transform.Find("NavigationButtons");
        int numPage = Mathf.CeilToInt(trainableUnits.Count / maxItemsInPage);
        foreach (Transform child in navigationButtons) {
            Destroy(child);
        }
        GameObject navigationButton = Resources.Load<GameObject>(@"Prefab\TrainMenuButtonGreen");
        for (int i = 0; i < numPage; i++) {
            GameObject instance = Instantiate(navigationButton);
            instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = i.ToString();
            instance.GetComponent<Button>().onClick.AddListener(delegate { SetPage(i); });
            instance.transform.SetParent(navigationButtons); 
        }
    }

    public void SetPage(int i) { 
        
    }
    
}
