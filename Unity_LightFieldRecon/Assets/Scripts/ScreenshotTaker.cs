using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System;
using UnityEngine;

public class ScreenshotTaker : MonoBehaviour
{
    public int frameCount;
    public float deltaTime;
    public bool first;
    public const string DIRECTORY = "bikes_4pmin80k/";
    public string path;

    // Start is called before the first frame update
    void Start()
    {
        frameCount = 0;
        deltaTime = 0.0f;
        first = true;
        path = Application.dataPath + "/../Screenshot/" + DIRECTORY;
        try
        {
            DirectoryInfo di = Directory.CreateDirectory(path);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e.ToString());
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnPostRender()
    {
        deltaTime += Time.deltaTime;
        if (deltaTime > 1.0f)
        {
            SaveFrameAsImg();
            deltaTime = 0.0f;
        }
    }

    void SaveFrameAsImg()
    {
        // Create a texture the size of the screen, RGB24 format
        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // Read screen contents into the texture
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.Destroy(tex);

        // Take a for orignal and another for changed to compare them
        // string path_original = Application.dataPath + "/../Screenshot/original/";
        // string path_changed = Application.dataPath + "/../Screenshot/changed/";

        // if(first)
        // {
        //     File.WriteAllBytes(path_original+frameCount+".png", bytes);
        //     UnityEngine.Debug.Log("Saved Screenshot to: "+path_original);
        //     first = !first;
        // }
        // else
        // {
        //     File.WriteAllBytes(path_changed+frameCount+".png", bytes);
        //     UnityEngine.Debug.Log("Saved Screenshot to: "+path_changed);
        //     frameCount++;
        //     first = !first;
        // }

        // Write to a file in the project folder
        File.WriteAllBytes(path + frameCount + ".png", bytes);
        UnityEngine.Debug.Log("Saved Screenshot to: " + path);
        frameCount++;
    }

    // void onDestroy()
    // {
    //     ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "python "+Application.dataPath+"/../Screenshot/analyse.py");
    //     // processInfo.CreateNoWindow = true;
    //     // processInfo.UseShellExecute = false;

    //     Process process = Process.Start(processInfo);

    //     // process.WaitForExit();
    //     // process.Close();
    // }
}
