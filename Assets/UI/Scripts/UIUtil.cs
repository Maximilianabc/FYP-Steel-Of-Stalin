using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIUtil : MonoBehaviour
{
    public UIUtil instance;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Exit() {
        Application.Quit();
    }

    public void ChangeScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }


    //Only for testing 
    public void testfunction() {
        DontDestroyOnLoad(transform.parent.gameObject);
        //SteelOfStalin.Battle battleInstance=GameObject.Find("battle").GetComponent<SteelOfStalin.Battle>();
        //battleInstance.Map.AddUnit(new SteelOfStalin.Props.Units.Land.Personnels.Infantry());
        //Debug.Log(battleInstance.Map.GetUnits().ToString());
        //Debug.Log(SteelOfStalin.Battle.Instance != null);
        
    }
    public void testfunction2() { 
        
    }
}
