using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;

public class ResourcesPanel : MonoBehaviour
{
    public static ResourcesPanel instance;
    public Text roundDisplay;
    public Text timeDisplay;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timeDisplay.text = $"{System.DateTime.Now.Hour:D2}:{System.DateTime.Now.Minute:D2}";
        roundDisplay.text = $"Round{SteelOfStalin.Battle.Instance.RoundNumber}";
    }
}
