using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin;
using SteelOfStalin.Commands;

public class CommandPanel : MonoBehaviour
{
    public static CommandPanel instance;
    public int maxIconsInPage = 10;
    private Unit currentUnit;
    private List<string> availableCommands;
    //counts from 1
    private int currentPage;
    private int numPages;
    private bool isWaitingInput = false;

    void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        //testing code
        Debug.Log(((AvailableMovementCommands)14 & AvailableMovementCommands.MERGE) > 0);
        Debug.Log(((AvailableMovementCommands)14&AvailableMovementCommands.HOLD)>0);
        //
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show() { 
    
    }

    public void Hide() { 
    
    }


    public void SetUnit(Unit u) {
        if (u == null) return;
        if (!u.IsOwn(Battle.Instance.Self)) return;
        currentUnit = u;
        availableCommands = new List<string>();
        if ((u.AvailableMovementCommands & AvailableMovementCommands.HOLD) > 0) {
            availableCommands.Add("Hold");
        }
        //if ((u.AvailableMovementCommands & AvailableMovementCommands.LAND) > 0)
        //{
        //    availableCommands.Add("Land");
        //}
        if ((u.AvailableMovementCommands & AvailableMovementCommands.MERGE) > 0)
        {
            availableCommands.Add("Merge");
        }
        if ((u.AvailableMovementCommands & AvailableMovementCommands.MOVE) > 0)
        {
            availableCommands.Add("Move");
        }
        //if ((u.AvailableMovementCommands & AvailableMovementCommands.SUBMERGE) > 0)
        //{
        //    availableCommands.Add("Submerge");
        //}
        //if ((u.AvailableMovementCommands & AvailableMovementCommands.SURFACE) > 0)
        //{
        //    availableCommands.Add("Surface");
        //}
        if ((u.AvailableFiringCommands & AvailableFiringCommands.FIRE) > 0)
        {
            availableCommands.Add("Fire");
        }
        if ((u.AvailableFiringCommands & AvailableFiringCommands.SUPPRESS) > 0)
        {
            availableCommands.Add("Suppress");
        }
        if ((u.AvailableFiringCommands & AvailableFiringCommands.SABOTAGE) > 0)
        {
            availableCommands.Add("Sabotage");
        }
        if ((u.AvailableFiringCommands & AvailableFiringCommands.AMBUSH) > 0)
        {
            availableCommands.Add("Ambush");
        }
        //if ((u.AvailableFiringCommands & AvailableFiringCommands.BOMBARD) > 0)
        //{
        //    availableCommands.Add("Bombard");
        //}
        if ((u.AvailableConstructionCommands & AvailableConstructionCommands.CONSTRUCT) > 0)
        {
            availableCommands.Add("Construct");
        }
        if ((u.AvailableConstructionCommands & AvailableConstructionCommands.FORTIFY) > 0)
        {
            availableCommands.Add("Fortify");
        }
        if ((u.AvailableConstructionCommands & AvailableConstructionCommands.DEMOLISH) > 0)
        {
            availableCommands.Add("Demolish");
        }
        //if ((u.AvailableLogisticsCommands & AvailableLogisticsCommands.ABOARD) > 0) 
        //{
        //    availableCommands.Add("Aboard");
        //}
        //if ((u.AvailableLogisticsCommands & AvailableLogisticsCommands.DISEMBARK) > 0)
        //{
        //    availableCommands.Add("Disembark");
        //}
        //if ((u.AvailableLogisticsCommands & AvailableLogisticsCommands.LOAD) > 0)
        //{
        //    availableCommands.Add("Load");
        //}
        //if ((u.AvailableLogisticsCommands & AvailableLogisticsCommands.UNLOAD) > 0)
        //{
        //    availableCommands.Add("Unload");
        //}
        if ((u.AvailableLogisticsCommands & AvailableLogisticsCommands.RESUPPLY) > 0)
        {
            availableCommands.Add("Resupply");
        }
        if ((u.AvailableLogisticsCommands & AvailableLogisticsCommands.REPAIR) > 0)
        {
            availableCommands.Add("Repair");
        }
        //if ((u.AvailableLogisticsCommands & AvailableLogisticsCommands.RECONSTRUCT) > 0)
        //{
        //    availableCommands.Add("Reconstruct");
        //}
        if ((u.AvailableMiscCommands & AvailableMiscCommands.CAPTURE) > 0)
        {
            availableCommands.Add("Capture");
        }
        if ((u.AvailableMiscCommands & AvailableMiscCommands.ASSEMBLE) > 0)
        {
            availableCommands.Add("Assemble");
        }
        if ((u.AvailableMiscCommands & AvailableMiscCommands.DISASSEMBLE) > 0)
        {
            availableCommands.Add("Disassemble");
        }
        //if ((u.AvailableMiscCommands & AvailableMiscCommands.SCAVENGE) > 0)
        //{
        //    availableCommands.Add("Scavenge");
        //}
        numPages = Mathf.CeilToInt((float)availableCommands.Count / maxIconsInPage);
        if (numPages > 2) Debug.LogError("Command panel does not support more than 2 pages");
    }

    public void SetPage(int pageNum) {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        if (pageNum > numPages) return;
        List<string> subList=new List<string>();
        if (availableCommands.Count <= maxIconsInPage) subList = availableCommands;
        else if (pageNum == 1) subList = availableCommands.GetRange(0, maxIconsInPage);
        else if (pageNum == 2) subList = availableCommands.GetRange(availableCommands.Count - maxIconsInPage, maxIconsInPage);
        else { Debug.LogError("case not defined"); return; }
        foreach (string commandString in subList) {
            GameObject instance = Instantiate(Game.GameObjects.Find(g => g.name == "CommandPanelButton"), transform, false);
            instance.name = commandString;
            Sprite icon = Game.Icons.Find(s => s.name == commandString);
            if (icon != null) {
                instance.GetComponent<Image>().sprite = icon;
            }
            string s = commandString;
            instance.GetComponent<Button>().onClick.AddListener(delegate { PrepareCommand(s); });
            EventTrigger et = instance.GetComponent<EventTrigger>();
            et.triggers.Clear();
            EventTrigger.Entry onEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter,
                callback = new EventTrigger.TriggerEvent()
            };
            onEnter.callback.AddListener(new UnityAction<BaseEventData>(e => { Tooltip.ShowTooltip_Static(commandString); }));
            et.triggers.Add(onEnter);
            EventTrigger.Entry onExit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit,
                callback = new EventTrigger.TriggerEvent()
            };
            onExit.callback.AddListener(new UnityAction<BaseEventData>(e => Tooltip.HideTooltip_Static()));
            et.triggers.Add(onExit);
        }
        if (availableCommands.Count <= maxIconsInPage) { }
        else if (pageNum == 1) { 
            GameObject navigateRightButton = Instantiate(Game.GameObjects.Find(g => g.name == "CommandPanelRightButton"), transform, false);
            navigateRightButton.transform.SetAsLastSibling();
            navigateRightButton.GetComponent<Button>().onClick.AddListener(delegate { SetPage(2); });
        }
        else if (pageNum == 2) {
            GameObject navigateLeftButton = Instantiate(Game.GameObjects.Find(g => g.name == "CommandPanelLeftButton"), transform, false);
            navigateLeftButton.transform.SetAsFirstSibling();
            navigateLeftButton.GetComponent<Button>().onClick.AddListener(delegate { SetPage(1); });
        }
        else Debug.LogError("case not defined");

    }

    public void PrepareCommand(string commandString) {
        if (currentUnit == null) return;
        if (availableCommands == null) return;
        if (!availableCommands.Contains(commandString)) return;
        if (isWaitingInput) {
            CleanUpTrigger();
        }
        //if(commandString=="")
    }

    public void CleanUpTrigger() { 
        
    }
}
