using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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
    public void SetVolume(byte volume)
    {
        AudioListener.volume = (float)volume/100;
        Game.Settings.VolumeMusic = volume;
        Game.Settings.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        Game.Settings.Fullscreen = fullscreen;
        Game.Settings.Save();
    }

    public bool isBlockedByUI() {
        bool mouseOnUI = false;
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (RaycastResult raycastResult in raycastResults)
        {
            if (raycastResult.gameObject.layer == 5)
            {
                mouseOnUI = true;
                break;
            }
        }
        return mouseOnUI;
    }

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!Game.AssetsLoaded)
        {
            LeanTween.delayedCall(3f, (System.Action)delegate
            {
                SetScreenResolution(new Resolution(Game.Settings.ResolutionX, Game.Settings.ResolutionY));
                SetFullscreen(Game.Settings.Fullscreen);
                SetVolume(Game.Settings.VolumeMusic);
                AudioSource bgm = gameObject.AddComponent<AudioSource>();
                AudioClip clip=Game.AudioClips.Find(a => a.name == "RiseAndFall");
                if (clip != null) {
                    bgm.clip = clip;
                    bgm.loop = true;
                    bgm.Play();
                }
            });
        }
        else {
            SetScreenResolution(new Resolution(Game.Settings.ResolutionX, Game.Settings.ResolutionY));
            SetFullscreen(Game.Settings.Fullscreen);
            SetVolume(Game.Settings.VolumeMusic);
            AudioSource bgm = gameObject.AddComponent<AudioSource>();
            AudioClip clip = Game.AudioClips.Find(a => a.name == "RiseAndFall");
            if (clip != null)
            {
                bgm.clip = clip;
                bgm.loop = true;
                bgm.Play();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    public void Exit() {
        Application.Quit();
    }

    public void RoundStartUIUpdate() {
        TrainPanel.instance.SetCity(Battle.Instance.Self.Capital);
        ResourcesPanel.instance.SetResources(Battle.Instance.Self.Resources);
    }

    public void RoundEndUIUpdate() {
        TrainPanel.instance.HideAll();
        UnitPanel.instance.Hide();
        CommandPanel.instance.Hide();
    }

    public bool isListenToUIEvent => Battle.Instance != null && !Battle.Instance.Self.IsReady && Battle.Instance.EnablePlayerInput;

}
