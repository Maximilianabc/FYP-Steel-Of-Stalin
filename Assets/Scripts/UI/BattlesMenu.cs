using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SteelOfStalin;


public class BattlesMenu : MonoBehaviour
{
    public GameObject saveNormalPrefab;
    public GameObject saveSelectedPrefab;
    public GameObject savesListContent;
    public GameObject battleRuleTextContent;

    [SerializeField]
    private BattleInfo selectedBattle;
    private GameObject selectedBattleGameObject;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        ResetBattles();
    }

    public void ResetBattles()
    {
        selectedBattle = null;
        selectedBattleGameObject = null;
        foreach (Transform child in savesListContent.transform) {
            if(child.name!="Save_Add")Destroy(child.gameObject);
        }
        for (int i=Game.BattleInfos.Count-1;i>=0;i--) {
            BattleInfo bi = Game.BattleInfos[i];
            GameObject instance = Instantiate(saveNormalPrefab, savesListContent.transform,false);
            instance.GetComponent<Button>().onClick.AddListener(delegate { Select(instance, bi); });
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Battle Name: {bi.Name}");
            sb.AppendLine($"Map Name: {bi.MapName}");
            sb.AppendLine($"Map Size: {bi.MapWidth}กั{bi.MapHeight}");
            sb.AppendLine($"Num of Players: {bi.MaxNumPlayers}");

            instance.transform.Find("TextContent").GetComponent<TMPro.TMP_Text>().text=sb.ToString();
        }
        battleRuleTextContent.GetComponent<TMPro.TMP_Text>().text = "";
        GetComponent<Resize>().DelayResize(1);
    }

    public void Select(GameObject instance,BattleInfo bi) {
        if (instance == selectedBattleGameObject) return;
        if (selectedBattleGameObject != null) {
            selectedBattleGameObject.transform.Find("Background").GetComponent<Image>().color = saveNormalPrefab.transform.Find("Background").GetComponent<Image>().color;
            selectedBattleGameObject.transform.Find("Border").GetComponent<Image>().color = saveNormalPrefab.transform.Find("Border").GetComponent<Image>().color;
        }
        instance.transform.Find("Background").GetComponent<Image>().color = saveSelectedPrefab.transform.Find("Background").GetComponent<Image>().color;
        instance.transform.Find("Border").GetComponent<Image>().color = saveSelectedPrefab.transform.Find("Border").GetComponent<Image>().color;
        selectedBattle = bi;
        selectedBattleGameObject = instance;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Time For Each Round: {bi.Rules.TimeForEachRound}");
        sb.AppendLine($"Fog of War: {bi.Rules.IsFogOfWar}");
        sb.AppendLine($"Signal Connection: {bi.Rules.RequireSignalConnection}");
        sb.AppendLine($"Universal Queue: {bi.Rules.AllowUniversalQueue}");
        //sb.AppendLine($"Resources: {bi.Rules.StartingResources}");
        battleRuleTextContent.GetComponent<TMPro.TMP_Text>().text = sb.ToString();
    }

    public void LoadBattle() {
        if (selectedBattle == null) return;
        bool multiplayer = MenuNavigation.instance.multiplayer;
        selectedBattle.IsSinglePlayer = !multiplayer;
        Game.ActiveBattle = selectedBattle;
        SceneManager.LoadScene("Loading");
        Game.StartHost();
        
        
    }

}
