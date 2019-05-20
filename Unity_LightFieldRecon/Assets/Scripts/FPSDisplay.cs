using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    float deltaTime = 0.0f;
    Gyroscope gyro;
    Vector2 mousePosition;


    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

#if UNITY_ANDROID
            mousePosition.x += gyro.rotationRate.y*deltaTime*100.0f/5.0f;
            mousePosition.y += gyro.rotationRate.x*deltaTime*100.0f/5.0f;
            if(Input.touchCount > 0)
            {
                mousePosition.x = 7.5f;
                mousePosition.y = 7.5f;
                Touch touch = Input.GetTouch(0);
                if(touch.phase == TouchPhase.Moved && touch.deltaPosition.x > 1)
                {
                }
                else if(touch.phase == TouchPhase.Moved && touch.deltaPosition.x < -1)
                {
                }
            }
#else
        mousePosition.x = Input.mousePosition.x / Screen.width * 20;
        mousePosition.y = (Screen.height - Input.mousePosition.y) / Screen.height * 20;
        mousePosition.x = Mathf.Clamp(mousePosition.x,0f,15f);
        mousePosition.y = Mathf.Clamp(mousePosition.y,0f,15f);
#endif
    }

    void Start()
    {
        mousePosition = new Vector2(7.5f, 7.5f);
#if UNITY_ANDROID
            gyro = Input.gyro;
            gyro.enabled = true;
#endif
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(0, 0, w, h * 10 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 10 / 100;
        style.normal.textColor = new Color(0.0f, 0.7f, 0.0f, 1.0f);
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.} ms ({1:0.0} fps)\nX:{2:0.0}, Y:{3:0.0}", msec, fps, mousePosition.x, mousePosition.y);
        GUI.Label(rect, text, style);
    }


}