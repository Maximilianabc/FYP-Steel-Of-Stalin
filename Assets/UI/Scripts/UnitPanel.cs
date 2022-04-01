using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.Assets;
using SteelOfStalin.Assets.Props.Units.Land;

public class UnitPanel : MonoBehaviour
{
    private Unit currentUnit = null;
    private GameObject menu;

    private void Awake()
    {
        menu = transform.Find("Panel_Right").gameObject;
    }
    // Start is called before the first frame update
    void Start()
    {
        
        Unit u = Game.UnitData.GetNew("infantry");
        SteelOfStalin.Assets.Props.Units.Land.Personnels.Infantry infantry = u as SteelOfStalin.Assets.Props.Units.Land.Personnels.Infantry;
        infantry.PrimaryFirearm = Game.CustomizableData["rifle"].Clone() as Firearm;
        infantry.SecondaryFirearm = Game.CustomizableData["rifle"].Clone() as Firearm;
        SetUnit(u);




    }

    // Update is called once per frame
    void Update()
    {

    }

    private void SetUnit(Unit u) {
        if (menu == null) return;
        GameObject instance;
        currentUnit = u;
        GameObject unitTitle = menu.transform.Find("Text_UnitTitle").gameObject;
        unitTitle.GetComponent<TMPro.TMP_Text>().text = u.Name;
        GameObject strength = menu.transform.Find("Strength").gameObject;
        strength.transform.Find("ProgressBar").GetComponent<ProgressBar>().SetProgressBarValue(Mathf.RoundToInt((float)Game.UnitData[u.Name].Defense.Strength.Value),Mathf.RoundToInt((float)u.Defense.Strength.Value));
        GameObject morale = menu.transform.Find("Morale").gameObject;
        morale.transform.Find("ProgressBar").GetComponent<ProgressBar>().SetProgressBarValue(Mathf.RoundToInt((float)Game.UnitData[u.Name].Morale.Value), Mathf.RoundToInt((float)u.Morale.Value));
        GameObject modules = menu.transform.Find("Modules").gameObject;
        foreach (Transform child in modules.transform) {
            if(child.gameObject.name!= "Text_ModulesIntegrity") Destroy(child.gameObject);
        }
        List<Module> unitModules;
        if (u.GetModules() == null) { unitModules = null; }
        else { unitModules = u.GetModules().ToList(); }
        modules.SetActive(unitModules != null && unitModules.Count != 0);
        if (unitModules != null) {
            foreach (Module module in unitModules)
            {
                instance = Instantiate(Resources.Load<GameObject>(@"Prefabs\UnitPanelAttribute"));
                instance.name = module.Name;
                instance.transform.SetParent(modules.transform);
                instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = module.Name;
                instance.transform.Find("ProgressBar").GetComponent<ProgressBar>().SetProgressBarValue(Mathf.RoundToInt((float)Game.CustomizableData.Modules[module.Name].Integrity.Value), Mathf.RoundToInt((float)module.Integrity.Value));
            }
        }      
        modules.GetComponent<Resize>().DoResize();

        GameObject carrying = menu.transform.Find("Carrying").gameObject;
        foreach (Transform child in carrying.transform)
        {
            if (child.gameObject.name != "Text_Carrying") Destroy(child.gameObject);
        }
        //Supplies
        instance = Instantiate(Resources.Load<GameObject>(@"Prefabs\UnitPanelAttribute"));
        instance.transform.SetParent(carrying.transform);
        instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = "Supplies";
        instance.transform.Find("ProgressBar").GetComponent<ProgressBar>().SetProgressBarValue(Mathf.RoundToInt((float)u.Capacity.Supplies.Value), Mathf.RoundToInt((float)u.Carrying.Supplies.Value));
        //Catridges
        instance = Instantiate(Resources.Load<GameObject>(@"Prefabs\UnitPanelAttribute"));
        instance.transform.SetParent(carrying.transform);
        instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = "Catridges";
        instance.transform.Find("ProgressBar").GetComponent<ProgressBar>().SetProgressBarValue(Mathf.RoundToInt((float)u.Capacity.Cartridges.Value), Mathf.RoundToInt((float)u.Carrying.Cartridges.Value));
        //Shells
        instance = Instantiate(Resources.Load<GameObject>(@"Prefabs\UnitPanelAttribute"));
        instance.transform.SetParent(carrying.transform);
        instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = "Shells";
        instance.transform.Find("ProgressBar").GetComponent<ProgressBar>().SetProgressBarValue(Mathf.RoundToInt((float)u.Capacity.Shells.Value), Mathf.RoundToInt((float)u.Carrying.Shells.Value));
        //Fuel
        instance = Instantiate(Resources.Load<GameObject>(@"Prefabs\UnitPanelAttribute"));
        instance.transform.SetParent(carrying.transform);
        instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = "Fuel";
        instance.transform.Find("ProgressBar").GetComponent<ProgressBar>().SetProgressBarValue(Mathf.RoundToInt((float)u.Capacity.Fuel.Value), Mathf.RoundToInt((float)u.Carrying.Fuel.Value));
        carrying.GetComponent<Resize>().DoResize();
        

        GameObject weapons = menu.transform.Find("Weapons").gameObject;
        foreach (Transform child in weapons.transform)
        {
            if (child.gameObject.name != "Text_Weapons"&&child.gameObject.name!="WeaponsHeading") Destroy(child.gameObject);
        }
        List<IOffensiveCustomizable> unitWeapons;
        if (u.GetWeapons() == null) { unitWeapons = null; }
        else { unitWeapons = u.GetWeapons().ToList(); }
        weapons.SetActive(unitWeapons!=null&&unitWeapons.Count != 0);
        if (unitWeapons != null) {
            foreach (IOffensiveCustomizable weapon in unitWeapons)
            {
                instance = Instantiate(Resources.Load<GameObject>(@"Prefabs\UnitPanelWeapon"));
                instance.name = weapon.Name;
                instance.transform.SetParent(weapons.transform);
                instance.transform.Find("Text").GetComponent<TMPro.TMP_Text>().text = weapon.Name;
                instance.transform.Find("Soft").GetComponent<TMPro.TMP_Text>().text = Mathf.RoundToInt((float)weapon.Offense.Damage.Soft.Value).ToString();
                instance.transform.Find("Hard").GetComponent<TMPro.TMP_Text>().text = Mathf.RoundToInt((float)weapon.Offense.Damage.Hard.Value).ToString();
                instance.transform.Find("Destruct.").GetComponent<TMPro.TMP_Text>().text = Mathf.RoundToInt((float)weapon.Offense.Damage.Destruction.Value).ToString();
            }
        }       
        weapons.GetComponent<Resize>().DoResize();

        GameObject shellType = menu.transform.Find("ShellType").gameObject;
        List<Gun> unitGuns;
        if (u.GetModules<Gun>() == null) {unitGuns = null;} 
        else { unitGuns = u.GetModules<Gun>().ToList();}
        shellType.SetActive(unitGuns != null && unitGuns.Count != 0);








        GameObject speed = menu.transform.Find("Speed").gameObject;
        speed.transform.Find("Text_Speed").GetComponent<TMPro.TMP_Text>().text= Mathf.RoundToInt((float)u.Maneuverability.Speed.Value).ToString();
        GameObject recon = menu.transform.Find("Recon").gameObject;
        recon.transform.Find("Text_Recon").GetComponent<TMPro.TMP_Text>().text= Mathf.RoundToInt((float)u.Scouting.Reconnaissance.Value).ToString();
        GameObject communication = menu.transform.Find("Communication").gameObject;
        communication.transform.Find("Text_Communication").GetComponent<TMPro.TMP_Text>().text = Mathf.RoundToInt((float)u.Scouting.Communication.Value).ToString();
        Resize r = GetComponent<Resize>();
        if (r != null)
        {
            r.DoResize();
        }
        if (!gameObject.activeSelf) {
            this.Show();
        }

    }

    private void SetCustomizable(Customizable c) {
        if (currentUnit == null) return;
    }

    private void UnsetCustomizable() {
        if (currentUnit == null) return;
    }

        

    //TODO: Animation
    public void Show() {
        gameObject.SetActive(true);
    }

    public void Hide() {
        gameObject.SetActive(false);
    }


}


