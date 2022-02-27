using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public static Tooltip instance;
    private Text tooltipText;
    private RectTransform backgroundRectTransform;
    private void Start()
    {
        gameObject.SetActive(false);
    }
    private void Awake()
    {
        instance = this;
        backgroundRectTransform = transform.Find("Tooltip_Background").GetComponent<RectTransform>();
        tooltipText = transform.Find("Tooltip_Text").GetComponent<Text>();
        
    }
    private void Update()
    {
        transform.position = Input.mousePosition;
    }
    private void ShowTooltip(string tooltipString) {
        gameObject.SetActive(true);
        tooltipText.text = tooltipString;
        float textPaddingSize = 12f;
        Vector2 backgroundSize = new Vector2(tooltipText.preferredWidth+textPaddingSize*2f, tooltipText.preferredHeight+textPaddingSize*2f);        
        backgroundRectTransform.sizeDelta = backgroundSize;
    }
    
    private void HideTooltip() {
        gameObject.SetActive(false);
    }

    public static void ShowTooltip_Static(string tooltipString) {
        instance.ShowTooltip(tooltipString);
    }

    public static void HideTooltip_Static() {
        instance.HideTooltip();    
    }
}
