using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;

public class MenuNavigation : MonoBehaviour
{
    public static MenuNavigation instance;
    public Stack<GameObject> navigationStack;
    public GameObject mainMenu;
    public GameObject optionMenu;
    public GameObject singleMultiSelectionMenu;
    public GameObject hostGuestSelectionMenu;
    public GameObject IPMenu;
    public GameObject mapMenu;
    public GameObject battlesMenu;
    public GameObject profileMenu;
    public bool multiplayer;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        navigationStack = new Stack<GameObject>();
        navigationStack.Push(mainMenu);
        mainMenu.transform.Find("Menu").Find("Play").GetComponent<Button>().onClick.AddListener(delegate { NavigateTo(singleMultiSelectionMenu); });
        mainMenu.transform.Find("Menu").Find("Options").GetComponent<Button>().onClick.AddListener(delegate { NavigateTo(optionMenu); });
        optionMenu.transform.Find("BackButton").GetComponent<Button>().onClick.AddListener(delegate { NavigateBack(); });
        singleMultiSelectionMenu.transform.Find("BackButton").GetComponent<Button>().onClick.AddListener(delegate { NavigateBack(); });
        singleMultiSelectionMenu.transform.Find("Controller").Find("Button_Singleplayer").GetComponent<Button>().onClick.AddListener(delegate { NavigateTo(battlesMenu); multiplayer = false; });
        singleMultiSelectionMenu.transform.Find("Controller").Find("Button_Multiplayer").GetComponent<Button>().onClick.AddListener(delegate { NavigateTo(hostGuestSelectionMenu); multiplayer = true; });
        hostGuestSelectionMenu.transform.Find("BackButton").GetComponent<Button>().onClick.AddListener(delegate { NavigateBack(); });
        hostGuestSelectionMenu.transform.Find("Controller").Find("Button_Host").GetComponent<Button>().onClick.AddListener(delegate { NavigateTo(battlesMenu); });
        hostGuestSelectionMenu.transform.Find("Controller").Find("Button_Guest").GetComponent<Button>().onClick.AddListener(delegate { IPMenu.SetActive(true); });
        IPMenu.transform.Find("MessageBoxIP").Find("SubmitButton").GetComponent<Button>().onClick.AddListener(delegate { Game.StartClient(); });
        IPMenu.transform.Find("MessageBoxIP").Find("CancelButton").GetComponent<Button>().onClick.AddListener(delegate { IPMenu.SetActive(false); });
        battlesMenu.transform.Find("BackButton").GetComponent<Button>().onClick.AddListener(delegate { NavigateBack(); });
        battlesMenu.transform.Find("Battles").Find("Viewport").Find("Content").Find("Save_Add").GetComponent<Button>().onClick.AddListener(delegate { NavigateTo(mapMenu); });
        mapMenu.transform.Find("BackButton").GetComponent<Button>().onClick.AddListener(delegate { NavigateBack();});
        profileMenu.transform.Find("SubmitButton").GetComponent<Button>().onClick.AddListener(delegate { 
            NavigateBack();
            Game.Profile.Name = profileMenu.transform.Find("InputField_ProfileName").GetComponent<TMPro.TMP_InputField>().text;
            Game.Profile.Save();
            });
        if (!Game.AssetsLoaded)
        {
            LeanTween.delayedCall(1f, (System.Action)delegate
            {
                if (string.IsNullOrEmpty(Game.Profile.Name)) {
                    NavigateTo(profileMenu);
                }
            }).setDestroyOnComplete(true);
        }
        else
        {
            if (string.IsNullOrEmpty(Game.Profile.Name))
            {
                NavigateTo(profileMenu);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void NavigateTo(GameObject menu)
    {
        if (menu == null || navigationStack.Count < 1) return;
        navigationStack.Peek().SetActive(false);
        navigationStack.Push(menu);
        menu.SetActive(true);
    }

    public void NavigateBack()
    {
        if (navigationStack.Count < 2) return;
        navigationStack.Pop().SetActive(false);
        navigationStack.Peek().SetActive(true);
    }
}
