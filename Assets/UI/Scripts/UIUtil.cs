using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIUtil : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
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
    }
    public void testfunction2() { 
        
    }
}
