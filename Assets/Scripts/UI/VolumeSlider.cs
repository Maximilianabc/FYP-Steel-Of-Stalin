using SteelOfStalin;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{

    // Start is called before the first frame update
    private void Start()
    {
        float playerVolume = (float)Game.Settings.VolumeMusic / 100;
        Slider slider = GetComponent<Slider>();
        slider.SetValueWithoutNotify(playerVolume);
        slider.onValueChanged.AddListener(delegate { UIUtil.instance.SetVolume((byte)Mathf.RoundToInt(GetComponent<Slider>().value * 100)); });

    }

    // Update is called once per frame
    private void Update()
    {

    }

}
