using UnityEngine;
using System.Collections;
 
public class FPSDisplay : MonoBehaviour
{
	float deltaTime = 0.0f;
 
	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
	}
 
	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;
 
		GUIStyle style = new GUIStyle();
 
		Rect rect = new Rect(0, 0, w, h * 4 / 100);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 4 / 100;
		style.normal.textColor = new Color (0.0f, 0.7f, 0.0f, 1.0f);
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		float mouseX = Input.mousePosition.x/Screen.width*20;
		float mouseY = Input.mousePosition.y/Screen.height*20;
		string text = string.Format("{0:0.} ms ({1:0.} fps)\ncameraX: {2:0.0}, cameraY: {3:0.0}", msec, fps, mouseX, mouseY);
		GUI.Label(rect, text, style);
	}
}