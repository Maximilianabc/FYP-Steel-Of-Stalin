using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FullscreenToggle : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //TODO: Deserialized from files
        bool playerFullscreen = false;
        Toggle toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(delegate { ToggleValueChanged(toggle.isOn); });
        toggle.isOn = playerFullscreen;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ToggleValueChanged(bool fullscreen) {
        Screen.fullScreen = fullscreen;
    }
}
