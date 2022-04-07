using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteelOfStalin;
using SteelOfStalin.Assets.Props.Tiles;
using System.Threading.Tasks;

public class MapMenu : MonoBehaviour
{
    RandomMap map;
    Task taskMapGen;
    public Vector2 maxMapPreviewSize;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (taskMapGen != null && taskMapGen.IsCompleted && !taskMapGen.IsFaulted && !taskMapGen.IsCanceled)
        {
            taskMapGen = null;
            RawImage mapPreview = transform.Find("MapPreview").GetComponent<RawImage>();
            mapPreview.texture = map.Visualize();
            float resizeRatio = Mathf.Min(maxMapPreviewSize.x / map.Width, maxMapPreviewSize.y / map.Height);
            mapPreview.rectTransform.sizeDelta = new Vector2(resizeRatio * map.Width, resizeRatio * map.Height);
            transform.Find("MapPreview").gameObject.SetActive(true);
            transform.Find("LoadingMessage").gameObject.SetActive(false);
            Debug.Log("Visualized");
        }
        else if (taskMapGen != null && taskMapGen.IsCompleted && taskMapGen.IsFaulted) {
            //retry if error occurred
            Debug.LogError("Map Generation Faulted,"+taskMapGen.Exception.InnerException.Message);
            GenerateMap();
        }
    }

    public void GenerateMap() {
        if (taskMapGen != null && !taskMapGen.IsCompleted) {
            return;
        }
        transform.Find("MapPreview").gameObject.SetActive(false);
        transform.Find("LoadingMessage").gameObject.SetActive(true);
        int width = Mathf.RoundToInt(transform.Find("WidthSlider").GetComponent<Slider>().value);
        int height = Mathf.RoundToInt(transform.Find("HeightSlider").GetComponent<Slider>().value);
        int numPlayers = Mathf.RoundToInt(transform.Find("PlayerSlider").GetComponent<Slider>().value);
        try {
            taskMapGen=GenerateMap(width, height, numPlayers);
        }
        catch (System.Exception ex){ Debug.LogError(ex.Message); }
        
    }



    async Task GenerateMap(int width,int height,int numPlayers) {
        await Task.Run(() =>
        {
            map = new RandomMap(width, height, numPlayers);
            Debug.Log("taskfinished");
        });
    }

}
