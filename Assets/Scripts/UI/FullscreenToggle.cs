using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;

public class FullscreenToggle : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //TODO: Deserialized from files
        Toggle toggle = GetComponent<Toggle>();
        toggle.isOn = Game.Settings.Fullscreen;
        toggle.onValueChanged.AddListener(delegate { UIUtil.instance.SetFullscreen(toggle.isOn); });

    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
