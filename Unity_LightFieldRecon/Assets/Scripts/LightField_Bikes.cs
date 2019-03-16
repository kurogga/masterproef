using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;

public class LightField_Bikes : MonoBehaviour
{
    public string path = "Assets/Resources/Bikes_fixed.csv";
    public TextAsset textFile;
    public Material material;
    public Vector2 mousePosition;
    public const int KERNELS = 19173;

    // Buffer to store data and pass to shader
    public ComputeBuffer muXBuffer;
    public ComputeBuffer muYnPiBuffer;
    public ComputeBuffer coMatrixInvBuffer;
    public ComputeBuffer determinantBuffer;
    public ComputeBuffer weightBuffer; // For shader to store calculated weight

    // List to store data from file and pass to buffer
    public List<Vector4> muXList; //cameraposition en pixelposition
    public List<Vector4> muYnPiList; // color en pi
    public List<Matrix4x4> coMatrixInvList; // coMatrix^(-1)
    public List<float> determinantList; // sqrt(determinant(coMatrix))
    // public List<float> weightList;
    // public float[] weightList;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start: Initializing all data into buffers");
        Resources.UnloadUnusedAssets();
        // Initialize all variables and read file
        muXList = new List<Vector4>();
        muYnPiList = new List<Vector4>();
        coMatrixInvList = new List<Matrix4x4>();
        determinantList = new List<float>();
        // weightList = new List<float>();
        // weightList = new float[KERNELS];
        List<string> eachLine = new List<string>();
        float twoPi = (float) (2 * Mathf.PI);
        // StreamWriter writer = new StreamWriter(path, true);

        string theWholeFileAsOneLongString = textFile.text;
        eachLine.AddRange(theWholeFileAsOneLongString.Split("\n"[0]));
        for (int i = 0; i < eachLine.Count - 1; i++)
        {
            string[] nrs = eachLine[i].Split(',');
            float cameraX = Convert.ToSingle(nrs[1]);
            float cameraY = Convert.ToSingle(nrs[2]);
            float pixelX = Convert.ToSingle(nrs[3])/623.0f;
            float pixelY = Convert.ToSingle(nrs[4])/432.0f;
            float rvalue = Convert.ToSingle(nrs[5]);
            float gvalue = Convert.ToSingle(nrs[6]);
            float bvalue = Convert.ToSingle(nrs[7]);
            Vector4 muX = new Vector4(cameraX, cameraY, pixelX, pixelY);
            Vector4 muYnPi = new Vector4(Convert.ToSingle(nrs[5]), Convert.ToSingle(nrs[6]), Convert.ToSingle(nrs[7]), Convert.ToSingle(nrs[0]));
            Matrix4x4 coMatrix = new Matrix4x4();
            float determinantCM = 0.0f;
            // string result = nrs[0] + "," + cameraX + "," + cameraY + "," + pixelX + "," + pixelY + "," + rvalue + "," + gvalue + "," + bvalue;
            // Calculate each coMatrix here instead of in GPU
            for (int j = 0; j < 16; j++)
            {
                float matrixValue = Convert.ToSingle(nrs[8 + j]);
                coMatrix[j] = matrixValue;
                // result = result + "," + nrs[8+j]; 
            }
            // writer.WriteLine(result);

            // Get and save sqrt(2pi^k *determinant)
            determinantCM = (float) Mathf.Sqrt(Mathf.Pow(twoPi,4)*coMatrix.determinant);
            // determinantCM = Mathf.Sqrt(coMatrix.determinant);

            // Adding each values to correct list
            muXList.Add(muX);
            muYnPiList.Add(muYnPi);
            coMatrixInvList.Add(coMatrix.inverse);
            determinantList.Add(determinantCM);
        }
        // writer.Close();

        // Get the material and pass the lists to the shader
        material = GetComponent<Renderer>().sharedMaterial;

        // Save data to computebuffer and send to material
        muXBuffer = new ComputeBuffer(muXList.Count, 16);
        muYnPiBuffer = new ComputeBuffer(muYnPiList.Count, 16);
        coMatrixInvBuffer = new ComputeBuffer(coMatrixInvList.Count, 64);
        determinantBuffer = new ComputeBuffer(determinantList.Count, 4);
        // weightBuffer = new ComputeBuffer(determinantList.Count,4);

        muXBuffer.SetData(muXList);
        muYnPiBuffer.SetData(muYnPiList);
        coMatrixInvBuffer.SetData(coMatrixInvList);
        determinantBuffer.SetData(determinantList);
        // weightBuffer.SetData(weightList);

        material.SetBuffer("muXList", muXBuffer);
        material.SetBuffer("muYnPiList", muYnPiBuffer);
        material.SetBuffer("coMatrixInvList", coMatrixInvBuffer);
        material.SetBuffer("determinantList", determinantBuffer);
        material.SetInt("kernels", KERNELS);
        // material.SetBuffer("weightList", weightBuffer);
        Debug.Log("Finished initialiazing");
    }

    // Update is called once per frame
    void Update()
    {
        mousePosition = new Vector2(Input.mousePosition.x/Screen.width*30.0f,Input.mousePosition.y/Screen.height*30.0f);
        material.SetFloat("mouseX",mousePosition.x);
        material.SetFloat("mouseY",mousePosition.y);
        
        if(Input.GetKeyDown(KeyCode.Space)){
            // weightBuffer.GetData(weightList);
        }
        if(Input.GetKeyDown(KeyCode.Escape)){
            Application.Quit();
        }
    }

    void OnDestroy()
    {
        Debug.Log("OnDestroy: Releasing all buffers");
        muXBuffer.Release();
        muYnPiBuffer.Release();
        coMatrixInvBuffer.Release();
        determinantBuffer.Release();
        // weightBuffer.Release();
    }
}
