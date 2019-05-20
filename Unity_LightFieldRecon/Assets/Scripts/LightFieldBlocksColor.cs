using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class LightFieldBlocksColor : MonoBehaviour
{
    public class Kernel : IComparable<Kernel>
    {
        public Vector4 MuX { get; set; }
        public Vector4 MuYnPi { get; set; }
        public Matrix4x4 CoMatrix { get; set; }
        public Matrix4x4 Gradient { get; set; }
        public float Determt { get; set; }
        public float MahaDist { get; set; }
        public Kernel(Vector4 muX, Vector4 muYnPi, Matrix4x4 coMatrix, Matrix4x4 gradient, float determt)
        {
            MuX = muX;
            MuYnPi = muYnPi;
            CoMatrix = coMatrix;
            Gradient = gradient;
            Determt = determt;
        }
        public int CompareTo(Kernel other)
        {
            return this.MahaDist.CompareTo(other.MahaDist);
        }
        public override string ToString()
        {
            return "muX=" + MuX.ToString() + " muYnPi=" + MuYnPi.ToString() + " mahaDist=" + MahaDist + " \n" + CoMatrix.ToString() + " \n" + Gradient.ToString();
        }
        public Kernel DeepCopy()
        {
            Kernel kernel = new Kernel(this.MuX, this.MuYnPi, this.CoMatrix, this.Gradient, this.Determt);
            kernel.MahaDist = this.MahaDist;
            return kernel;
        }
    }
    public TextAsset textFile;
    Material material;
    public Vector2 mousePosition;
    List<Kernel> kernelList;
    Dictionary<int, List<Kernel>> kernelsPerBlock;
    List<Vector2> mousePositionList;
    Slider slider;
    float deltaTime;
    int frameCount;
    int givenKernelsAmount;
    int pixelsInBlockY;
    int totalBlocks;
    int withGradient;
    public int PIXELS_PER_BLOCK = 16;
    public int KERNELS_PER_BLOCK = 80;
    public int FRAME_WIDTH = 1024;
    public int FRAME_HEIGHT = 1024;
    StreamWriter writer;
    // Buffer to store data and pass to shader
    ComputeBuffer muXBuffer;
    ComputeBuffer muYnPiBuffer;
    ComputeBuffer coMatrixInvBuffer;
    ComputeBuffer determinantBuffer;
    ComputeBuffer gradientBuffer;

    // List to store data from file and pass to buffer
    List<Vector4> muXList; //cameraposition en pixelposition
    List<Vector4> muYnPiList; // color en pi
    List<Matrix4x4> coMatrixInvList; // -1/2 coMatrix^(-1)
    List<Matrix4x4> gradientList;
    List<float> determinantList; // 1/sqrt(determinant(coMatrix))
    // public List<float> mahaDistList;
    // Gyroscope for mobile
    Gyroscope gyro;

    // Start is called before the first frame update
    void Start()
    {
        InitializeVariables();
        ReadDataFromFile();

        // writer = new StreamWriter("bestkernels10p.txt", false);
        Task[] taskArray = new Task[totalBlocks];
        int taskIndex = 0;
        Stopwatch stopWatch = new Stopwatch();
        UnityEngine.Debug.Log("StartTime");
        stopWatch.Start();
        for (int i = 0; i < FRAME_HEIGHT; i += pixelsInBlockY)
        {
            for (int j = 0; j < FRAME_WIDTH; j += PIXELS_PER_BLOCK)
            {
                Vector4 xVector = new Vector4(8.5f, 8.5f, j, i);
                // CalculateBestKernelsAsync(xVector);
                taskArray[taskIndex] = Task.Factory.StartNew(() =>
                {
                    CalculateBestKernelsAsync(xVector);
                });
                taskIndex++;
            }
        }
        Task.WaitAll(taskArray);
        TimeSpan ts = stopWatch.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        UnityEngine.Debug.Log("EndTime: " + elapsedTime);

        SetDataInBuffer();
    }

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
        }
#else
        mousePosition.x = Input.mousePosition.x / Screen.width * 20f;
        mousePosition.y = (Screen.height - Input.mousePosition.y) / Screen.height * 20f;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
#endif
        mousePosition.x = Mathf.Clamp(mousePosition.x, 0f, 15f);
        mousePosition.y = Mathf.Clamp(mousePosition.y, 0f, 15f);
        material.SetFloat("mouseX", mousePosition.x);
        material.SetFloat("mouseY", mousePosition.y);
        material.SetInt("dynamicKernels", (int)slider.value);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            withGradient = (withGradient == 0 ? 1 : 0);
            material.SetInt("withGradient", withGradient);
            // Recalc
            // Task[] taskArray = new Task[totalBlocks];
            // int taskIndex = 0;
            // Stopwatch stopWatch = new Stopwatch();
            // UnityEngine.Debug.Log("StartTime");
            // stopWatch.Start();
            // for (int i = 0; i < FRAME_HEIGHT; i += PIXELS_PER_BLOCK)
            // {
            //     for (int j = 0; j < FRAME_WIDTH; j += PIXELS_PER_BLOCK)
            //     {
            //         Vector4 xVector = new Vector4(mousePosition.x, mousePosition.y, j, i);
            //         taskArray[taskIndex] = Task.Factory.StartNew(() =>
            //         {
            //             CalculateBestKernelsAsync2(xVector, PIXELS_PER_BLOCK);
            //         });
            //         taskIndex++;
            //     }
            // }
            // Task.WaitAll(taskArray);
            // TimeSpan ts = stopWatch.Elapsed;
            // string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            // UnityEngine.Debug.Log("Done calc blocks: " + elapsedTime);

            // muXBuffer.SetData(muXList);
            // muYnPiBuffer.SetData(muYnPiList);
            // coMatrixInvBuffer.SetData(coMatrixInvList);
            // gradientBuffer.SetData(gradientList);
            // determinantBuffer.SetData(determinantList);
            // material.SetBuffer("muXList", muXBuffer);
            // material.SetBuffer("muYnPiList", muYnPiBuffer);
            // material.SetBuffer("coMatrixInvList", coMatrixInvBuffer);
            // material.SetBuffer("gradientList", gradientBuffer);
            // material.SetBuffer("determinantList", determinantBuffer);
        }


        // Set mouse position for Screenshots
        // if (frameCount >= 100)
        // {
        //     EditorApplication.isPlaying = false;
        // }
        // deltaTime += Time.deltaTime;
        // // if (deltaTime > 1.0f)
        // // {
        // material.SetFloat("mouseX", mousePositionList[frameCount].x);
        // material.SetFloat("mouseY", mousePositionList[frameCount].y);
        // // deltaTime = 0.0f;
        // frameCount++;

    }

    void OnDestroy()
    {
        muXBuffer.Release();
        muYnPiBuffer.Release();
        coMatrixInvBuffer.Release();
        gradientBuffer.Release();
        determinantBuffer.Release();
    }

    void CalculateBestKernelsAsync(Vector4 xVector)
    {
        // Using mahalanobis distance as measure, the closer the better
        // Calculate the average of four anchor points
        int pixelsYPerBlock = PIXELS_PER_BLOCK * FRAME_HEIGHT / FRAME_WIDTH;
        float halfLengthX = (xVector.z + PIXELS_PER_BLOCK / 2) > FRAME_WIDTH ? (FRAME_WIDTH - xVector.z) / 2 : PIXELS_PER_BLOCK / 2;
        float halfLengthY = (xVector.w + pixelsYPerBlock / 2) > FRAME_HEIGHT ? (FRAME_HEIGHT - xVector.w) / 2 : pixelsYPerBlock / 2;
        float midPixelX = xVector.z + halfLengthX;
        float midPixelY = xVector.w + halfLengthY;
        Vector4 newXVector = new Vector4(xVector.x, xVector.y, midPixelX, midPixelY);
        int blockX = Mathf.FloorToInt(midPixelX / PIXELS_PER_BLOCK);
        int blockY = Mathf.FloorToInt(midPixelY / pixelsYPerBlock);
        int amountBlocksInARow = Mathf.CeilToInt((float)FRAME_WIDTH / PIXELS_PER_BLOCK);
        int startBlock = blockX + (blockY * amountBlocksInARow);
        Kernel[] newKernelArray = kernelsPerBlock[startBlock].ToArray();

        float maxDistance = 0f;
        for (int i = 0; i < givenKernelsAmount; i++)
        {
            // float lowerBoundX = midPixelX - halfLengthX * halfLengthX;
            // float upperBoundX = midPixelX + halfLengthX * halfLengthX;
            // float lowerBoundY = midPixelY - halfLengthY * halfLengthX;
            // float upperBoundY = midPixelY + halfLengthY * halfLengthX;
            // if (lowerBoundX < newKernelArray[i].MuX.z && upperBoundX > newKernelArray[i].MuX.z && lowerBoundY < newKernelArray[i].MuX.w && upperBoundY > newKernelArray[i].MuX.w)
            // {
            Vector4 x_min_mu = newXVector - newKernelArray[i].MuX;
            Matrix4x4 newCoMatrix = newKernelArray[i].CoMatrix;
            newCoMatrix[0] += (xVector.x * xVector.x * 10.5f);
            newCoMatrix[5] += (xVector.y * xVector.y * 10.5f);
            newCoMatrix[10] += (halfLengthX * halfLengthX * 0.5f);
            newCoMatrix[15] += (halfLengthY * halfLengthY * 0.5f);
            Vector4 tempVector = newCoMatrix.inverse * x_min_mu;
            float exponent = Vector4.Dot(x_min_mu, tempVector);
            float result = newKernelArray[i].MuYnPi.w * Mathf.Exp(-0.5f * (exponent * exponent));
            newKernelArray[i].MahaDist = result;
            if (maxDistance < result)
            {
                maxDistance = result;
            }
            // }
        }
        Array.Sort(newKernelArray, (x, y) => y.MahaDist.CompareTo(x.MahaDist));
        maxDistance = maxDistance / 1000f;
        int bufferIndex = startBlock * KERNELS_PER_BLOCK;
        // UnityEngine.Debug.Log(bufferIndex);
        for (int i = 0; i < KERNELS_PER_BLOCK; i++)
        {
            Vector4 newMuYnPi = newKernelArray[i].MuYnPi;
            // if (kernelList[i].MahaDist < maxDistance)
            // {
            //     newMuYnPi.w = 0.0f;
            // }
            // mahaDistList[bufferIndex + i] = newKernelArray[i].MahaDist;
            muXList[bufferIndex + i] = newKernelArray[i].MuX;
            muYnPiList[bufferIndex + i] = newMuYnPi;
            coMatrixInvList[bufferIndex + i] = newKernelArray[i].CoMatrix.inverse;
            gradientList[bufferIndex + i] = newKernelArray[i].Gradient;
            determinantList[bufferIndex + i] = newKernelArray[i].Determt;
        }
    }

    void InitializeVariables()
    {
        Resources.UnloadUnusedAssets();
#if UNITY_ANDROID
            gyro = Input.gyro;
            gyro.enabled = true;
#endif
        pixelsInBlockY = PIXELS_PER_BLOCK * FRAME_HEIGHT / FRAME_WIDTH;
        int amountOfBlocksPerRow = Mathf.CeilToInt((float)(FRAME_WIDTH + 0.1f) / PIXELS_PER_BLOCK);
        int amountOfBlocksPerColumn = Mathf.CeilToInt((float)(FRAME_HEIGHT + 0.1f) / pixelsInBlockY);
        totalBlocks = amountOfBlocksPerRow * amountOfBlocksPerColumn;
        int totaalListSize = totalBlocks * KERNELS_PER_BLOCK;
        // int totaalListSize = totalBlocks * KERNELS_PER_BLOCK + 2000 * KERNELS_PER_BLOCK;
        // muXList = new List<Vector4>(new Vector4[totalBlocks * KERNELS_PER_BLOCK]);
        // muYnPiList = new List<Vector4>(new Vector4[totalBlocks * KERNELS_PER_BLOCK]);
        // coMatrixInvList = new List<Matrix4x4>(new Matrix4x4[totalBlocks * KERNELS_PER_BLOCK]);
        // gradientList = new List<Matrix4x4>(new Matrix4x4[totalBlocks * KERNELS_PER_BLOCK]);
        // determinantList = new List<float>(new float[totalBlocks * KERNELS_PER_BLOCK]);
        // mahaDistList = new List<float>(new float[totalBlocks * KERNELS_PER_BLOCK]);
        muXList = new List<Vector4>(totaalListSize);
        muYnPiList = new List<Vector4>(totaalListSize);
        coMatrixInvList = new List<Matrix4x4>(totaalListSize);
        determinantList = new List<float>(totaalListSize);
        gradientList = new List<Matrix4x4>(totaalListSize);
        // mahaDistList = new List<float>(totaalListSize);
        for (int amount = 0; amount < totaalListSize; amount++)
        {
            // UnityEngine.Debug.Log(" " + amount);
            muXList.Add(Vector4.one);
            muYnPiList.Add(Vector4.one);
            coMatrixInvList.Add(Matrix4x4.identity);
            determinantList.Add(1f);
            gradientList.Add(Matrix4x4.identity);
            // mahaDistList.Add(1f);
        }
        // UnityEngine.Debug.Log("list size: " + gradientList.Count);
        kernelList = new List<Kernel>();
        mousePositionList = new List<Vector2>();
        mousePosition = new Vector2(7.5f, 7.5f);
        deltaTime = 0.0f;
        frameCount = 0;
        withGradient = 1;
        slider = (Slider)FindObjectOfType(typeof(Slider));
        material = GetComponent<Renderer>().sharedMaterial;
        // 50 difference position of camera for comparisons
        float step = 1.3f;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                mousePositionList.Add(new Vector2(i * step, j * step));
            }
        }
    }

    void ReadDataFromFile()
    {
        List<string> eachLine = new List<string>();
        string theWholeFileAsOneLongString = textFile.text;
        eachLine.AddRange(theWholeFileAsOneLongString.Split("\n"[0]));
        givenKernelsAmount = eachLine.Count - 1;
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
            for (int j = 0; j < 16; j++)
            {
                float matrixValue = Convert.ToSingle(nrs[8 + j]);
                coMatrix[j] = matrixValue;
            }
            Matrix4x4 gradient = Matrix4x4.zero;
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    float matrixValue = Convert.ToSingle(nrs[24 + k + (j * 3)]);
                    gradient[j + (k * 4)] = matrixValue / 300f;
                }
            }
            // for (int j = 0; j < 4; j++)
            // {
            //     for (int k = 0; k < 3; k++)
            //     {
            //         float matrixValue = Convert.ToSingle(nrs[24 + k + (j * 3)]);
            //         gradient[k + (j * 4)] = matrixValue / 1000f;
            //     }
            // }
            determinantCM = Mathf.Sqrt(coMatrix.determinant);
            Kernel kernel = new Kernel(muX, muYnPi, coMatrix, gradient, determinantCM);
            kernel.MahaDist = 0f;
            kernelList.Add(kernel);
        }

        // Deep copy kernellist in dictionary once for each block, 
        // so that each block can works concurrently
        kernelsPerBlock = new Dictionary<int, List<Kernel>>();
        for (int i = 0; i < totalBlocks; i++)
        {
            List<Kernel> newKernelList = new List<Kernel>();
            for (int j = 0; j < givenKernelsAmount; j++)
            {
                newKernelList.Add(kernelList[j].DeepCopy());
            }
            kernelsPerBlock.Add(i, newKernelList);
        }
    }

    void SetDataInBuffer()
    {
        muXBuffer = new ComputeBuffer(muXList.Count, 16);
        muYnPiBuffer = new ComputeBuffer(muYnPiList.Count, 16);
        coMatrixInvBuffer = new ComputeBuffer(coMatrixInvList.Count, 64);
        gradientBuffer = new ComputeBuffer(gradientList.Count, 64);
        determinantBuffer = new ComputeBuffer(determinantList.Count, 4);
        muXBuffer.SetData(muXList);
        muYnPiBuffer.SetData(muYnPiList);
        coMatrixInvBuffer.SetData(coMatrixInvList);
        gradientBuffer.SetData(gradientList);
        determinantBuffer.SetData(determinantList);
        material.SetBuffer("muXList", muXBuffer);
        material.SetBuffer("muYnPiList", muYnPiBuffer);
        material.SetBuffer("coMatrixInvList", coMatrixInvBuffer);
        material.SetBuffer("gradientList", gradientBuffer);
        material.SetBuffer("determinantList", determinantBuffer);
        material.SetInt("pixelsPerBlock", PIXELS_PER_BLOCK);
        material.SetInt("kernelsPerBlock", KERNELS_PER_BLOCK);
        material.SetInt("frameWidth", FRAME_WIDTH);
        material.SetInt("frameHeight", FRAME_HEIGHT);
        material.SetInt("dynamicKernels", KERNELS_PER_BLOCK);
        material.SetInt("withGradient", withGradient);
    }

}