using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteelOfStalin;

public class MultiplayerMenu : MonoBehaviour
{
    private bool isHost;
    private List<Player> listPlayers;
    private GameObject menu;
    private GameObject menuItem;

    void Awake() {
        menu = transform.Find("Players").gameObject;
        menuItem = Resources.Load<GameObject>(@"Prefabs\MultiplayerMenuPlayer");
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show(bool isHost) {
        this.isHost = isHost;
        gameObject.SetActive(true);
        listPlayers.Clear();
        foreach (Transform child in menu.transform) {
            Destroy(child);
        }
        //TODO: initialize players list
        foreach (Player player in listPlayers) {
            GameObject instance=Instantiate(menuItem);
            instance.transform.parent = menu.transform;
            instance.transform.Find("Name").GetComponent<TMPro.TMP_Text>().text = player.Name;
            //TODO
            instance.transform.Find("Status").GetComponent<TMPro.TMP_Text>().text = "...";
        }
        transform.Find("StartGame").gameObject.SetActive(isHost);
        transform.Find("Ready").gameObject.SetActive(!isHost);
        
    }

    public void Show() {
        Show(isHost);
    }
}
