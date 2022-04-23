using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;

public class LobbyMenu : MonoBehaviour
{
    private bool isMultiplayer;
    private List<Player> listPlayers;
    private GameObject menu;
    private GameObject menuItem;

    void Awake() {
        menu = transform.Find("Players").gameObject;
        menuItem = Game.GameObjects.Find(g=>g.name=="MultiplayerMenuPlayer");
        transform.Find("Ready").GetComponent<Button>().enabled = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        transform.Find("Ready").GetComponent<Button>().onClick.AddListener(delegate {
            transform.Find("Ready").GetComponent<TMPro.TMP_Text>().text = Battle.Instance.Self.IsReady ? "Ready" : "Cancel Ready";
            transform.Find("Ready").GetComponent<RectTransform>().sizeDelta = new Vector2(
                transform.Find("Ready").GetComponent<TMPro.TMP_Text>().preferredWidth,
                transform.Find("Ready").GetComponent<TMPro.TMP_Text>().preferredHeight);
            GameObject.Find(Game.Profile.Name).GetComponent<PlayerObject>().ChangeReadyStatus(!Battle.Instance.Self.IsReady);
            
        });
        transform.Find("Ready").GetComponent<Button>().enabled = true;

    }

    // Update is called once per frame
    void Update()
    {
        Redraw();
    }

    public void Redraw() {
        if (Battle.Instance == null) return; 
        listPlayers = Battle.Instance.Players;
        foreach (Transform child in menu.transform) {
            if (child.gameObject.name != "PlayersCount") { Destroy(child.gameObject); }
            else {
                child.gameObject.GetComponent<TMPro.TMP_Text>().text = $"{listPlayers.Count}/{Battle.Instance.MaxNumPlayers}";
            }
        }      
        foreach (Player player in listPlayers) {
            GameObject instance=Instantiate(menuItem,menu.transform,false);
            instance.transform.Find("Name").GetComponent<TMPro.TMP_Text>().text = player.Name;
            instance.transform.Find("Status").GetComponent<TMPro.TMP_Text>().text = player.IsReady?"Ready":"...";
        }
        
        
    }

}
