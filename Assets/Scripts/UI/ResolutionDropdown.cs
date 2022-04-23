using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;

public class ResolutionDropdown : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //TODO: Deserialized from files
        List<UIUtil.Resolution> resolutions = new List<UIUtil.Resolution> { 
            new UIUtil.Resolution(1366, 768), new UIUtil.Resolution(1600, 900), new UIUtil.Resolution(1920, 1080), new UIUtil.Resolution(2560, 1440) 
        };

        TMPro.TMP_Dropdown dropdown = GetComponent<TMPro.TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(resolutions.ConvertAll<string>(resolution => resolution));
        dropdown.value = resolutions.FindIndex(r => r.Equals(new UIUtil.Resolution(Game.Settings.ResolutionX, Game.Settings.ResolutionY)));
        dropdown.onValueChanged.AddListener(delegate { UIUtil.instance.SetScreenResolution(resolutions[dropdown.value]); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}

