using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SteelOfStalin;

public class UIUtil : MonoBehaviour
{
    public static UIUtil instance;

    public struct Resolution
    {
        public Resolution(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public readonly int x;
        public readonly int y;
        public static implicit operator string(Resolution resolution)
        {
            return $"{resolution.x} กั {resolution.y}";
        }
        public override bool Equals(object obj)
        {
            return (obj is Resolution resolution) && (this.x == resolution.x) && (this.y == resolution.y);
        }
        public override int GetHashCode()
        {
            return (x * 10000 + y).GetHashCode();
        }

    }
    public void SetScreenResolution(Resolution resolution)
    {
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
        Game.Settings.ResolutionX = resolution.x;
        Game.Settings.ResolutionY = resolution.y;
        Game.Settings.Save();
        Debug.Log($"resolution changed to {resolution.x},{resolution.y}. fullscreen={Screen.fullScreen}");
    }

    //TODO: functionality not throughly tested
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        Game.Settings.VolumeMusic = (byte)Mathf.RoundToInt(volume * 100);
        Game.Settings.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        Game.Settings.Fullscreen = fullscreen;
        Game.Settings.Save();
    }

    void Awake()
    {
        instance = this;
    }

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
        switch (sceneName) {
            case "Loading": SceneManager.LoadScene(sceneName);break;
            case "Game":AsyncOperation operation = SceneManager.LoadSceneAsync("Game");operation.completed += delegate { }; break;
        }
        
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
