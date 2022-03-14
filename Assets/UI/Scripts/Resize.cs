using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resize : MonoBehaviour
{
    public GameObject objectToBeResized;
    public GameObject parentOfContent;
    public float paddingLeft = 10f;
    public float paddingRight = 10f;
    public float paddingUp = 10f;
    public float paddingDown = 10f;
    public bool  resizeWidth=false;
    public bool  resizeHeight=false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void DoResize() {
        if (objectToBeResized == null || parentOfContent == null) {
            return;
        }
        if (!resizeWidth && !resizeHeight) {
            return;
        }
        RectTransform children = parentOfContent.transform.GetComponentInChildren<RectTransform>();
        float min_x, max_x, min_y, max_y;
        min_x = min_y = float.MaxValue;
        max_x = max_y = float.MinValue;

        foreach (RectTransform child in children)
        {
            if (!child.gameObject.activeSelf) continue;
            Vector2 scale = child.sizeDelta;
            float temp_min_x, temp_max_x, temp_min_y, temp_max_y;

            temp_min_x = child.localPosition.x - (scale.x / 2);
            temp_max_x = child.localPosition.x + (scale.x / 2);
            temp_min_y = child.localPosition.y - (scale.y / 2);
            temp_max_y = child.localPosition.y + (scale.y / 2);

            if (temp_min_x < min_x)
                min_x = temp_min_x;
            if (temp_max_x > max_x)
                max_x = temp_max_x;

            if (temp_min_y < min_y)
                min_y = temp_min_y;
            if (temp_max_y > max_y)
                max_y = temp_max_y;
        }
        Vector2 resultVector = objectToBeResized.GetComponent<RectTransform>().sizeDelta;
        if (resizeWidth) {
            resultVector.x = max_x - min_x + paddingLeft + paddingRight;
        }
        if (resizeHeight) {
            resultVector.y = max_y - min_y + paddingUp + paddingDown;
        }

        objectToBeResized.GetComponent<RectTransform>().sizeDelta = resultVector;
    }
}