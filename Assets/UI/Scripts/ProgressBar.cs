using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode()]
public class ProgressBar : MonoBehaviour
{
    public int maximum;
    public int current;
    public Image fill;
    private TMPro.TMP_Text text;
    private void Awake()
    {
        text = transform.Find("Text").GetComponent<TMPro.TMP_Text>();
    }
    private void Update()
    {
        
    }
    private void UpdateCurrentFill()
    {
        float fillAmount = (float)current / (float)maximum;
        fill.fillAmount = fillAmount;
    }

    private void UpdateText() {
        text.text = $"{current}/{maximum}";    
    }

    public void SetProgressBarValue(int maximum,int current) {
        //drop invalid calls
        if (maximum < current || maximum < 0 || current < 0) {
            return;
        }
        this.maximum = maximum;
        this.current = current;
        UpdateCurrentFill();
        UpdateText();
    }
}