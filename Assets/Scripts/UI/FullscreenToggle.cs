using SteelOfStalin;
using UnityEngine;
using UnityEngine.UI;

public class FullscreenToggle : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        Toggle toggle = GetComponent<Toggle>();
        toggle.isOn = Game.Settings.Fullscreen;
        toggle.onValueChanged.AddListener(delegate { UIUtil.instance.SetFullscreen(toggle.isOn); });

    }

    // Update is called once per frame
    private void Update()
    {

    }

}
