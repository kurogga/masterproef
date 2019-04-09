using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureCacher : MonoBehaviour
{
    int currentRow;
    Texture2D middleRowTexture;
    Material material;
    Vector3 mousePosition;
    GameObject quad;

    // Start is called before the first frame update
    void Start()
    {
        currentRow = 0;
        quad = GameObject.Find("QuadMulti");
        material = quad.GetComponent<Renderer>().sharedMaterial;
        middleRowTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
        material.SetInt("currentRow", currentRow);
        mousePosition = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mouseDelta = Input.mousePosition - mousePosition;
        if (mouseDelta.x != 0 || mouseDelta.y != 0)
        {
            currentRow = 0;
            material.SetInt("currentRow", currentRow);
        }
        mousePosition = Input.mousePosition;
    }

    void OnPostRender()
    {
        if (currentRow == 0)
        {
            // middleRowTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            middleRowTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
            middleRowTexture.Apply();
            material.SetTexture("middleRowTexture", middleRowTexture);
        }
        // else if (currentRow == 1)
        // {
        //     bottomRowTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        //     bottomRowTexture.Apply();
        //     material.SetTexture("bottomRowTexture", bottomRowTexture);
        // }
        currentRow++;
        // if (currentRow == 5) currentRow = 0;
        material.SetInt("currentRow", currentRow);
    }

}
