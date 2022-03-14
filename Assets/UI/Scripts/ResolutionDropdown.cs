using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionDropdown : MonoBehaviour
{
    public struct Resolution {
        public Resolution(int x, int y) {
            this.x = x;
            this.y = y;
        }
        public readonly int x;
        public readonly int y;
        public static implicit operator string(Resolution resolution) {
            return $"{resolution.x} กั {resolution.y}";
        }
        

    }
    
    // Start is called before the first frame update
    void Start()
    {

        //TODO: Deserialized from files
        List<Resolution> resolutions = new List<Resolution> { 
            new Resolution(1366, 768), new Resolution(1600, 900), new Resolution(1920, 1080), new Resolution(2560, 1440) 
        };
        int playerResolution = 2;


        TMPro.TMP_Dropdown dropdown = GetComponent<TMPro.TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(resolutions.ConvertAll<string>(resolution => resolution));
        dropdown.onValueChanged.AddListener(delegate { SetScreenResolution(resolutions[dropdown.value]); });
        dropdown.value = playerResolution;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetScreenResolution(Resolution resolution) {
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
        Debug.Log($"resolution changed to {resolution.x},{resolution.y}. fullscreen={Screen.fullScreen}");
    }
}

