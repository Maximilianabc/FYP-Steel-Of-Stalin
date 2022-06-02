using SteelOfStalin;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Props;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Commands;
using SteelOfStalin.CustomTypes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private string currentCommand;
    private List<Prop> triggerProps;
    private IOffensiveCustomizable currentWeapon;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    private void Start()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Update is called once per frame
    private void Update()
    {

    }

    public void Show()
    {

    }

    public void Hide()
    {
        CleanUpTrigger();
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }


    public void SetUnit(Unit u)
    {
        if (!UIUtil.instance.isListenToUIEvent) return;
        if (u == null) return;
        if (!u.IsOwn(Battle.Instance.Self)) return;
        currentUnit = u;
        availableCommands = new List<string>();
        if (u.CommandAssigned != CommandAssigned.NONE)
        {
            Debug.Log("Command Already Assigned");
            return;
        }
        if ((u.AvailableMovementCommands & AvailableMovementCommands.HOLD) > 0)
        {
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
        if (numPages == 0)
        {
            Debug.Log("No available Commands");
            return;
        }
        if (numPages > 2) Debug.LogError("Command panel does not support more than 2 pages");
        SetPage(1);
    }

    public void SetPage(int pageNum)
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (pageNum > numPages) return;
        List<string> subList = new List<string>();
        if (availableCommands.Count <= maxIconsInPage) subList = availableCommands;
        else if (pageNum == 1) subList = availableCommands.GetRange(0, maxIconsInPage);
        else if (pageNum == 2) subList = availableCommands.GetRange(availableCommands.Count - maxIconsInPage, maxIconsInPage);
        else { Debug.LogError("case not defined"); return; }
        foreach (string commandString in subList)
        {
            GameObject instance = Instantiate(Game.GameObjects.Find(g => g.name == "CommandPanelButton"), transform, false);
            instance.name = commandString;
            Sprite icon = Game.Icons.Find(s => s.name == commandString);
            if (icon != null)
            {
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
        else if (pageNum == 1)
        {
            GameObject navigateRightButton = Instantiate(Game.GameObjects.Find(g => g.name == "CommandPanelRightButton"), transform, false);
            navigateRightButton.transform.SetAsLastSibling();
            navigateRightButton.GetComponent<Button>().onClick.AddListener(delegate { SetPage(2); });
        }
        else if (pageNum == 2)
        {
            GameObject navigateLeftButton = Instantiate(Game.GameObjects.Find(g => g.name == "CommandPanelLeftButton"), transform, false);
            navigateLeftButton.transform.SetAsFirstSibling();
            navigateLeftButton.GetComponent<Button>().onClick.AddListener(delegate { SetPage(1); });
        }
        else Debug.LogError("case not defined");

    }

    public void PrepareCommand(string commandString)
    {
        if (currentUnit == null) return;
        if (availableCommands == null) return;
        if (!availableCommands.Contains(commandString)) return;
        if (isWaitingInput)
        {
            CleanUpTrigger();
        }
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        currentCommand = commandString;
        if (commandString == "Hold")
        {
            Hold hold = new Hold(currentUnit);
            Battle.Instance.Self.Commands.Add(hold);
            currentUnit.CommandAssigned = CommandAssigned.HOLD;
        }
        else if (commandString == "Move")
        {
            //simple implementation for now
            //can only control the destination
            List<Tile> reachableTiles = currentUnit.GetMoveRange().ToList();
            foreach (Command c in Battle.Instance.Self.Commands)
            {
                if (c is Deploy || c is Move)
                {
                    reachableTiles.RemoveAll(t => t.CoOrds == c.Destination);
                }
            }
            foreach (Tile t in reachableTiles)
            {
                t.PropObjectComponent.Highlight();
                PropEventTrigger trigger = t.PropObjectComponent.Trigger;
                trigger.SetActive("move_tile", true);
                trigger.SetActive("focus", false);
            }
            triggerProps = reachableTiles.ConvertAll<Prop>(t => t);
            isWaitingInput = true;
        }
        else if (commandString == "Ambush")
        {
            Ambush ambush = new Ambush(currentUnit);
            Battle.Instance.Self.Commands.Add(ambush);
            currentUnit.CommandAssigned = CommandAssigned.AMBUSH;
        }
        else if (commandString == "Fire" || commandString == "Suppress" || commandString == "Sabotage")
        {
            //Fire(Unit u,Unit Target,IOffensiveCustomizable weapon)
            IEnumerable<IOffensiveCustomizable> weapons = currentUnit.GetWeapons().Where(w => w != null);
            if (weapons.Count() == 0) return;

            foreach (IOffensiveCustomizable weapon in weapons)
            {
                GameObject instance = Instantiate(Game.GameObjects.Find(g => g.name == "CommandPanelWeapon"), transform, false);
                Sprite icon = Game.Icons.Find(i => i.name == weapon.Name);
                if (icon != null)
                {
                    instance.transform.Find("Icon").GetComponent<Image>().sprite = icon;
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
                        sb.AppendLine(weapon.Name);
                        sb.AppendLine($"Range: {weapon.Offense.MinRange.Value}-{weapon.Offense.MaxRange.Value}");
                        sb.AppendLine($"Accuracy: {weapon.Offense.Accuracy.Normal.Value}");
                        sb.AppendLine($"Hard Damage:{weapon.Offense.Damage.Hard.Value}");
                        sb.AppendLine($"Soft Damage:{weapon.Offense.Damage.Soft.Value}");
                        sb.Append($"Destruc. Damage:{weapon.Offense.Damage.Destruction.Value}");
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
                    foreach (Transform child in transform)
                    {
                        child.Find("Selected").gameObject.SetActive(false);
                    }
                    e.selectedObject.transform.Find("Selected").gameObject.SetActive(true);
                    SetWeapon(weapon);
                }));
                et.triggers.Add(onClick);

            }
            //foreach (Transform child in transform)
            //{
            //    child.Find("Selected").gameObject.SetActive(false);
            //}
            //transform.GetChild(0).Find("Selected").gameObject.SetActive(true);
            //SetWeapon(weapons.First());


        }
        else if (commandString == "Repair")
        {
            //Repair(Unit u,Unit target,Module module)

        }
        else if (commandString == "Resupply")
        {
            //Resupply resupply = new Resupply();
        }
        else if (commandString == "Construct")
        {
            //Construct(unit u,Building target,Coor dest)
        }
        else if (commandString == "Demolish")
        {
            //Demolish(Unit u,Building target)

        }
        else if (commandString == "Fortify")
        {
            //Fortify(Unit u,Building target)
        }
        else if (commandString == "Assemble")
        {
            Assemble assemble = new Assemble(currentUnit);
            Battle.Instance.Self.Commands.Add(assemble);
            currentUnit.CommandAssigned = CommandAssigned.ASSEMBLE;
        }
        else if (commandString == "Capture")
        {
            Capture capture = new Capture(currentUnit);
            Battle.Instance.Self.Commands.Add(capture);
            currentUnit.CommandAssigned = CommandAssigned.CAPTURE;
        }
        else if (commandString == "Disassemble")
        {
            Disassemble disassemble = new Disassemble(currentUnit);
            Battle.Instance.Self.Commands.Add(disassemble);
            currentUnit.CommandAssigned = CommandAssigned.DISASSEMBLE;
        }

    }

    public void ExecuteCommand(string commandString, Prop p)
    {
        if (currentUnit == null)
        {
            Debug.LogWarning("command execution failed");
            return;
        }
        if (!isWaitingInput)
        {
            Debug.LogWarning("command execution failed");
            return;
        }
        CleanUpTrigger();
        if (commandString == "Move")
        {
            if (p is Tile t)
            {
                Move move = new Move(currentUnit, currentUnit.GetPath(currentUnit.GetLocatedTile(), t).ToList());
                Battle.Instance.Self.Commands.Add(move);
                currentUnit.CommandAssigned = CommandAssigned.MOVE;
            }
        }
        else if (commandString == "Fire")
        {
            if (p is Unit u)
            {
                Fire fire = new Fire(currentUnit, u, currentWeapon);
                Battle.Instance.Self.Commands.Add(fire);
                currentUnit.CommandAssigned = CommandAssigned.FIRE;
            }
        }
        else if (commandString == "Suppress")
        {
            if (p is Unit u)
            {
                Suppress suppress = new Suppress(currentUnit, u, currentWeapon);
                Battle.Instance.Self.Commands.Add(suppress);
                currentUnit.CommandAssigned = CommandAssigned.SUPPRESS;
            }
        }
        else if (commandString == "Sabotage")
        {
            if (p is Building b)
            {
                Sabotage sabotage = new Sabotage(currentUnit, b, currentWeapon);
                Battle.Instance.Self.Commands.Add(sabotage);
                currentUnit.CommandAssigned = CommandAssigned.SABOTAGE;
            }
        }
    }

    public void SetWeapon(IOffensiveCustomizable weapon)
    {

        if (currentUnit == null) return;
        CleanUpTrigger();
        currentWeapon = weapon;
        IEnumerable<Tile> tilesInRange = currentUnit.GetFiringRange(weapon);
        if (currentCommand == "Fire" || currentCommand == "Suppress")
        {
            triggerProps = currentUnit.GetHostileUnitsInFiringRange(weapon).ToList().ConvertAll<Prop>(b => b);
        }
        else if (currentCommand == "Sabotage")
        {
            triggerProps = currentUnit.GetHostileBuildingsInFiringRange(weapon).ToList().ConvertAll<Prop>(b => b);
        }
        isWaitingInput = true;
        foreach (Prop p in triggerProps)
        {
            p.PropObjectComponent.Highlight();
            PropEventTrigger trigger = p.PropObjectComponent.Trigger;
            if (currentCommand == "Fire") trigger.SetActive("fire_unit", true);
            else if (currentCommand == "Suppress") trigger.SetActive("suppress_unit", true);
            else if (currentCommand == "Sabotage") trigger.SetActive("sabotage_building", true);
            trigger.SetActive("focus", false);
        }
    }

    public void CleanUpTrigger()
    {
        if (!isWaitingInput) return;
        foreach (Prop p in triggerProps)
        {

            p.PropObjectComponent.RestoreHighlight();
            PropEventTrigger trigger = p.PropObjectComponent.Trigger;
            if (currentCommand == "Move") trigger.SetActive("move_tile", false);
            else if (currentCommand == "Fire") trigger.SetActive("fire_unit", false);
            else if (currentCommand == "Suppress") trigger.SetActive("suppress_unit", false);
            else if (currentCommand == "Sabotage") trigger.SetActive("sabotage_building", false);
            trigger.SetActive("focus", true);
        }
        isWaitingInput = false;
    }
}
