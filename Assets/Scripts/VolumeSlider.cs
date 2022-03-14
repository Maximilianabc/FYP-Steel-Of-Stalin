using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        //TODO: Deserialized from files
        float playerVolume = 0.3f;
        Slider slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { SliderValueChanged(GetComponent<Slider>().value); });
        slider.value = playerVolume;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //TODO: functionality not throughly tested
    public void SliderValueChanged(float volume) {
        AudioListener.volume = volume;
    }
}
