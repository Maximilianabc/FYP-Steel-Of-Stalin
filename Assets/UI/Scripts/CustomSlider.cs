using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomSlider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Slider slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { SliderValueChanged(slider.value); });
        TMPro.TMP_InputField inputField = transform.Find("InputField").GetComponent<TMPro.TMP_InputField>();
        inputField.onEndEdit.AddListener(delegate { InputFieldEndEdit(inputField.text); });
        slider.value = 2;
        inputField.text = slider.value.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SliderValueChanged(float sliderValue)
    {
        TMPro.TMP_InputField inputField = transform.Find("InputField").GetComponent<TMPro.TMP_InputField>();
        inputField.text= Mathf.RoundToInt(sliderValue).ToString();
    }

    private void InputFieldEndEdit(string input) {
        int result;
        Slider slider = GetComponent<Slider>();
        TMPro.TMP_InputField inputField = transform.Find("InputField").GetComponent<TMPro.TMP_InputField>();
        if (!int.TryParse(input.Trim(),out result)) {
            result = Mathf.RoundToInt(slider.minValue);
        }
        if (result < slider.minValue) { 
            result = Mathf.RoundToInt(slider.minValue);
        }
        if (result > slider.maxValue) {
            result = Mathf.RoundToInt(slider.maxValue);
        }
        slider.value = result;
        inputField.text = result.ToString();

    } 
}
