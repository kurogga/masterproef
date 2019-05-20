using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEditor;

public class LightFieldMulti : MonoBehaviour
{
    public class Kernel : IComparable<Kernel>
    {
        public Vector4 MuX { get; set; }
        public Vector4 MuYnPi { get; set; }
        public Matrix4x4 CoMatrixInv { get; set; }
        public float Determt { get; set; }
        public float MahaDist { get; set; }
        public Kernel(Vector4 muX, Vector4 muYnPi, Matrix4x4 coMatrixInv, float determt)
        {
            MuX = muX;
            MuYnPi = muYnPi;
            CoMatrixInv = coMatrixInv;
            Determt = determt;
        }
        public int CompareTo(Kernel other)
        {
            return this.MahaDist.CompareTo(other.MahaDist);
        }
    }
    public TextAsset textFile;
    public Material material;
    public Vector2 mousePosition;
    public List<Kernel> kernelList;
    public List<Vector2> mousePositionList;
    public float deltaTime;
    public int frameCount;
    public int dynamicKernels;
    public const int PIXELS_PER_BLOCK = 200;
    public const int KERNELS_PER_BLOCK = 1000;
    public const int FRAME_WIDTH = 623;
    public const int FRAME_HEIGHT = 432;
    public const int OVERLAPPING_PIXELS = 100;

    // Buffer to store data and pass to shader
    public ComputeBuffer muXBuffer;
    public ComputeBuffer muYnPiBuffer;
    public ComputeBuffer coMatrixInvBuffer;
    public ComputeBuffer determinantBuffer;

    // List to store data from file and pass to buffer
    public List<Vector4> muXList; //cameraposition en pixelposition
    public List<Vector4> muYnPiList; // color en pi
    public List<Matrix4x4> coMatrixInvList; // -1/2 coMatrix^(-1)
    public List<float> determinantList; // 1/sqrt(determinant(coMatrix))

    // Gyroscope for mobile
    public Gyroscope gyro;

    void CalculateBestKernels(Vector4 xVector)
    {
        // Using mahalanobis distance as measure, the closer the better
        // Calculate the average of four anchor points
        // float minPixelX = (xVector.z - OVERLAPPING_PIXELS) < 0 ? xVector.z : xVector.z - OVERLAPPING_PIXELS;
        // float maxPixelX = (xVector.z + OVERLAPPING_PIXELS + PIXELS_PER_BLOCK) > FRAME_WIDTH ? xVector.z + (FRAME_WIDTH - xVector.z) : xVector.z + OVERLAPPING_PIXELS + PIXELS_PER_BLOCK;
        // float minPixelY = (xVector.w - OVERLAPPING_PIXELS) < 0 ? xVector.w : xVector.w - OVERLAPPING_PIXELS;
        // float maxPixelY = (xVector.w + OVERLAPPING_PIXELS + PIXELS_PER_BLOCK) > FRAME_HEIGHT ? xVector.w + (FRAME_HEIGHT - xVector.w) : xVector.w + OVERLAPPING_PIXELS + PIXELS_PER_BLOCK;
        float midPixelX = (xVector.z + PIXELS_PER_BLOCK / 2) > FRAME_WIDTH ? xVector.z + (FRAME_WIDTH - xVector.z) / 2 : xVector.z + PIXELS_PER_BLOCK / 2;
        float midPixelY = (xVector.w + PIXELS_PER_BLOCK / 2) > FRAME_HEIGHT ? xVector.w + (FRAME_HEIGHT - xVector.w) / 2 : xVector.w + PIXELS_PER_BLOCK / 2;
        float minCam = -50.0f;
        float maxCam = 100.0f;
        for (int i = 0; i < kernelList.Count; i++)
        {
            kernelList[i].MahaDist = 0.0f;

            // // TESTING WITH VARYING PIXEL VALUES - NOT RECOMMENDED
            // // Top left anchor point
            // Vector4 newXVector = new Vector4(xVector.x, xVector.y, minPixelX, minPixelY);
            // Vector4 x_min_mu = xVector - kernelList[i].MuX;
            // Vector4 tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            // float result = Vector4.Dot(x_min_mu, tempVector);
            // result = Mathf.Sqrt(result);
            // kernelList[i].MahaDist += result;
            // // Top right anchor point
            // newXVector = new Vector4(xVector.x, xVector.y, maxPixelX, minPixelY);
            // x_min_mu = newXVector - kernelList[i].MuX;
            // tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            // result = Vector4.Dot(x_min_mu, tempVector);
            // result = Mathf.Sqrt(result);
            // kernelList[i].MahaDist += result;
            // // Bottom right anchor point
            // newXVector = new Vector4(xVector.x, xVector.y, maxPixelX, maxPixelY);
            // x_min_mu = newXVector - kernelList[i].MuX;
            // tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            // result = Vector4.Dot(x_min_mu, tempVector);
            // result = Mathf.Sqrt(result);
            // kernelList[i].MahaDist += result;
            // // Bottom left anchor point
            // newXVector = new Vector4(xVector.x, xVector.y, minPixelX, maxPixelY);
            // x_min_mu = newXVector - kernelList[i].MuX;
            // tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            // result = Vector4.Dot(x_min_mu, tempVector);
            // result = Mathf.Sqrt(result);
            // kernelList[i].MahaDist += result;
            // // Middle anchor point
            // newXVector = new Vector4(xVector.x, xVector.y, midPixelX, midPixelY);
            // x_min_mu = newXVector - kernelList[i].MuX;
            // tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            // result = Vector4.Dot(x_min_mu, tempVector);
            // result = Mathf.Sqrt(result);
            // kernelList[i].MahaDist += result;

            Vector4 newXVector = new Vector4(xVector.x, xVector.y, midPixelX, midPixelY);
            Vector4 x_min_mu = newXVector - kernelList[i].MuX;
            Vector4 tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            float result = Vector4.Dot(x_min_mu, tempVector);
            result = Mathf.Sqrt(result);
            kernelList[i].MahaDist += result;
            // Top left camera
            newXVector = new Vector4(minCam, minCam, midPixelX, midPixelY);
            x_min_mu = newXVector - kernelList[i].MuX;
            tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            result = Vector4.Dot(x_min_mu, tempVector);
            result = Mathf.Sqrt(result);
            kernelList[i].MahaDist += result;
            // Top right camera
            newXVector = new Vector4(maxCam, minCam, midPixelX, midPixelY);
            x_min_mu = newXVector - kernelList[i].MuX;
            tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            result = Vector4.Dot(x_min_mu, tempVector);
            result = Mathf.Sqrt(result);
            kernelList[i].MahaDist += result;
            // Bottom right camera
            newXVector = new Vector4(maxCam, maxCam, midPixelX, midPixelY);
            x_min_mu = newXVector - kernelList[i].MuX;
            tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            result = Vector4.Dot(x_min_mu, tempVector);
            result = Mathf.Sqrt(result);
            kernelList[i].MahaDist += result;
            // Bottom left camera
            newXVector = new Vector4(minCam, maxCam, midPixelX, midPixelY);
            x_min_mu = newXVector - kernelList[i].MuX;
            tempVector = kernelList[i].CoMatrixInv * x_min_mu;
            result = Vector4.Dot(x_min_mu, tempVector);
            result = Mathf.Sqrt(result);
            kernelList[i].MahaDist += result;

            // Get the average of all maha distances
            kernelList[i].MahaDist /= 5;
        }

        // Sort all the kernels by maha dist
        kernelList.Sort();

        // Store the best one in data lists
        for (int i = 0; i < KERNELS_PER_BLOCK; i++)
        {
            // Debug.Log("Maha Dist: " + kernelList[i].MahaDist);
            muXList.Add(kernelList[i].MuX);
            muYnPiList.Add(kernelList[i].MuYnPi);
            coMatrixInvList.Add(kernelList[i].CoMatrixInv);
            determinantList.Add(kernelList[i].Determt);
        }
        // Debug.Log("Vector:" + xVector.ToString() + " Maha Dist: " + kernelList[0].MahaDist);
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start: Initializing all data into buffers");
        Resources.UnloadUnusedAssets();
#if UNITY_ANDROID
            gyro = Input.gyro;
            gyro.enabled = true;
#endif

        // Initialize all variables and read file
        muXList = new List<Vector4>();
        muYnPiList = new List<Vector4>();
        coMatrixInvList = new List<Matrix4x4>();
        determinantList = new List<float>();
        List<string> eachLine = new List<string>();
        kernelList = new List<Kernel>();
        mousePositionList = new List<Vector2>();
        mousePosition = new Vector2(7.5f, 7.5f);
        deltaTime = 0.0f;
        frameCount = 0;
        dynamicKernels = 2; // standard
        // float twoPi = (float) (2 * Mathf.PI);

        // 50 difference position of camera for comparisons
        float step = 1.3f;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                mousePositionList.Add(new Vector2(i * step, j * step));
            }

        }

        // Store read kernels in class Kernel
        string theWholeFileAsOneLongString = textFile.text;
        eachLine.AddRange(theWholeFileAsOneLongString.Split("\n"[0]));
        // kernelCount = eachLine.Count;
        for (int i = 0; i < eachLine.Count - 1; i++)
        {
            string[] nrs = eachLine[i].Split(',');
            float cameraX = Convert.ToSingle(nrs[1]);
            float cameraY = Convert.ToSingle(nrs[2]);
            float pixelX = Convert.ToSingle(nrs[3]);
            float pixelY = Convert.ToSingle(nrs[4]);
            Vector4 muX = new Vector4(cameraX, cameraY, pixelX, pixelY);
            float rvalue = Convert.ToSingle(nrs[5]);
            float gvalue = Convert.ToSingle(nrs[6]);
            float bvalue = Convert.ToSingle(nrs[7]);
            Vector4 muYnPi = new Vector4(rvalue, gvalue, bvalue, Convert.ToSingle(nrs[0]));
            Matrix4x4 coMatrix = new Matrix4x4();
            float determinantCM = 0.0f;

            // Calculate each coMatrix here instead of in GPU
            for (int j = 0; j < 16; j++)
            {
                float matrixValue = Convert.ToSingle(nrs[8 + j]);
                coMatrix[j] = matrixValue;
            }

            // Get and save sqrt(determinant)
            determinantCM = Mathf.Sqrt(coMatrix.determinant);

            //-1/2 * inv matrix
            Matrix4x4 invMatrix = coMatrix.inverse;

            // Adding new Kernel to kernelList
            Kernel kernel = new Kernel(muX, muYnPi, invMatrix, determinantCM);
            kernelList.Add(kernel);
        }
        Debug.Log("Calculating best kernels per block");
        // Calculate all Mahalanobis distances and put best 20 kernels in datalists
        for (int i = 0; i < FRAME_HEIGHT; i += PIXELS_PER_BLOCK)
        {
            for (int j = 0; j < FRAME_WIDTH; j += PIXELS_PER_BLOCK)
            {
                Vector4 xVector = new Vector4(7.0f, 7.0f, j, i);
                CalculateBestKernels(xVector);
            }
        }
        Debug.Log("Set up buffer");
        // Get the material and pass the lists to the shader
        material = GetComponent<Renderer>().sharedMaterial;

        // Save data to computebuffer and send to material
        muXBuffer = new ComputeBuffer(muXList.Count, 16);
        muYnPiBuffer = new ComputeBuffer(muYnPiList.Count, 16);
        coMatrixInvBuffer = new ComputeBuffer(coMatrixInvList.Count, 64);
        determinantBuffer = new ComputeBuffer(determinantList.Count, 4);

        muXBuffer.SetData(muXList);
        muYnPiBuffer.SetData(muYnPiList);
        coMatrixInvBuffer.SetData(coMatrixInvList);
        determinantBuffer.SetData(determinantList);

        material.SetBuffer("muXList", muXBuffer);
        material.SetBuffer("muYnPiList", muYnPiBuffer);
        material.SetBuffer("coMatrixInvList", coMatrixInvBuffer);
        material.SetBuffer("determinantList", determinantBuffer);
        material.SetInt("pixelsPerBlock", PIXELS_PER_BLOCK);
        material.SetInt("kernelsPerBlock", KERNELS_PER_BLOCK);
        material.SetInt("frameWidth", FRAME_WIDTH);
        material.SetInt("frameHeight", FRAME_HEIGHT);
        material.SetInt("dynamicKernels", dynamicKernels);
        Debug.Log("Finished initialiazing");
    }

    // Update is called once per frame
    void Update()
    {
        // deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
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
                dynamicKernels++;
                material.SetInt("dynamicKernels", dynamicKernels);
            }
            else if(touch.phase == TouchPhase.Moved && touch.deltaPosition.x < -1 && dynamicKernels>1)
            {
                dynamicKernels--;
                material.SetInt("dynamicKernels", dynamicKernels);
            }
        }
#else
        mousePosition.x = Input.mousePosition.x / Screen.width * 20;
        mousePosition.y = (Screen.height - Input.mousePosition.y) / Screen.height * 20;
#endif

        // material.SetFloat("mouseX", mousePosition.x);
        // material.SetFloat("mouseY", mousePosition.y);

        // if (frameCount >= 100)
        // {
        //     EditorApplication.isPlaying = false;
        // }
        // deltaTime += Time.deltaTime;
        // if (deltaTime > 1.0f)
        // {
        //     material.SetFloat("mouseX", mousePositionList[frameCount].x);
        //     material.SetFloat("mouseY", mousePositionList[frameCount].y);
        //     deltaTime = 0.0f;
        //     frameCount++;
        // }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            dynamicKernels++;
            material.SetInt("dynamicKernels", dynamicKernels);
        }
        if ((Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) && dynamicKernels > 1)
        {
            dynamicKernels--;
            material.SetInt("dynamicKernels", dynamicKernels);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
    }

}