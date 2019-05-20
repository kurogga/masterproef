using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class LightFieldBlocks : MonoBehaviour
{
    public class Kernel : IComparable<Kernel>
    {
        public Vector4 MuX { get; set; }
        public Vector4 MuYnPi { get; set; }
        public Matrix4x4 CoMatrix { get; set; }
        public float Determt { get; set; }
        public float MahaDist { get; set; }
        public Kernel(Vector4 muX, Vector4 muYnPi, Matrix4x4 coMatrix, float determt)
        {
            MuX = muX;
            MuYnPi = muYnPi;
            CoMatrix = coMatrix;
            Determt = determt;
        }
        public int CompareTo(Kernel other)
        {
            return this.MahaDist.CompareTo(other.MahaDist);
        }
        public override string ToString()
        {
            return "muX=" + MuX.ToString() + " muYnPi=" + MuYnPi.ToString() + " mahaDist=" + MahaDist + " \n" + CoMatrix.ToString();
        }
    }
    public TextAsset textFile;
    public Material material;
    public Vector2 mousePosition;
    public List<Kernel> kernelList;
    public Kernel[] kernelList_10b; // store precomputed kernels for each possible block size
    public Kernel[] kernelList_20b;
    public Kernel[] kernelList_40b;
    // public Dictionary<int, List<Kernel>> kernelsDict;
    public List<Vector2> mousePositionList;
    public Slider slider;
    public float deltaTime;
    public int frameCount;
    public int maxKernels;
    public int pixelsYPerBlock;
    public int dynamicKernelsPerBlock;
    public const int PIXELS_PER_BLOCK_MIN = 10;
    public const int PIXELS_PER_BLOCK_MAX = 40;
    public const int KERNELS_PER_BLOCK = 100;
    public const int FRAME_WIDTH = 623;
    public const int FRAME_HEIGHT = 432;
    public StreamWriter writer;

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

    // Start is called before the first frame update
    void Start()
    {
        Resources.UnloadUnusedAssets();

#if UNITY_ANDROID
            gyro = Input.gyro;
            gyro.enabled = true;
#endif

        muXList = new List<Vector4>();
        muYnPiList = new List<Vector4>();
        coMatrixInvList = new List<Matrix4x4>();
        determinantList = new List<float>();
        List<string> eachLine = new List<string>();
        kernelList = new List<Kernel>();
        // kernelsDict = new Dictionary<int, List<Kernel>>();
        // for (int i = 1; i <= PIXELS_PER_BLOCK_MAX / PIXELS_PER_BLOCK_MIN; i++)
        // {
        //     List<Kernel> newKernelList = new List<Kernel>();
        //     kernelsDict.Add(PIXELS_PER_BLOCK_MIN * i, newKernelList);
        // }
        kernelList_10b = new Kernel[Mathf.CeilToInt((float)FRAME_HEIGHT / 10 * FRAME_WIDTH / 10) * KERNELS_PER_BLOCK];
        kernelList_20b = new Kernel[Mathf.CeilToInt((float)FRAME_HEIGHT / 20 * FRAME_WIDTH / 20) * KERNELS_PER_BLOCK];
        kernelList_40b = new Kernel[Mathf.CeilToInt((float)FRAME_HEIGHT / 40 * FRAME_WIDTH / 40) * KERNELS_PER_BLOCK];
        mousePositionList = new List<Vector2>();
        mousePosition = new Vector2(7.5f, 7.5f);
        deltaTime = 0.0f;
        frameCount = 0;
        maxKernels = 0;
        dynamicKernelsPerBlock = KERNELS_PER_BLOCK;
        pixelsYPerBlock = PIXELS_PER_BLOCK_MIN * FRAME_HEIGHT / FRAME_WIDTH;
        slider = (Slider)FindObjectOfType(typeof(Slider));
        // 50 difference position of camera for comparisons
        float step = 1.3f;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                mousePositionList.Add(new Vector2(i * step, j * step));
            }
        }

        string theWholeFileAsOneLongString = textFile.text;
        eachLine.AddRange(theWholeFileAsOneLongString.Split("\n"[0]));
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
            determinantCM = Mathf.Sqrt(coMatrix.determinant);
            Kernel kernel = new Kernel(muX, muYnPi, coMatrix, determinantCM);
            kernelList.Add(kernel);
        }

        // Calculate all Mahalanobis distances and put best kernels in each list with dif block size
        int pixelsYBlock = 10 * FRAME_HEIGHT / FRAME_WIDTH;
        for (int i = 0; i < FRAME_HEIGHT; i += pixelsYBlock)
        {
            for (int j = 0; j < FRAME_WIDTH; j += 10)
            {
                Vector4 xVector = new Vector4(7.5f, 7.5f, j, i);
                CalculateBestKernels(xVector, 10, pixelsYBlock, kernelList_10b);
            }
        }
        pixelsYBlock = 20 * FRAME_HEIGHT / FRAME_WIDTH;
        for (int i = 0; i < FRAME_HEIGHT; i += pixelsYBlock)
        {
            for (int j = 0; j < FRAME_WIDTH; j += 20)
            {
                Vector4 xVector = new Vector4(7.5f, 7.5f, j, i);
                CalculateBestKernels(xVector, 20, pixelsYBlock, kernelList_20b);
            }
        }
        pixelsYBlock = 40 * FRAME_HEIGHT / FRAME_WIDTH;
        for (int i = 0; i < FRAME_HEIGHT; i += pixelsYBlock)
        {
            for (int j = 0; j < FRAME_WIDTH; j += 40)
            {
                Vector4 xVector = new Vector4(7.5f, 7.5f, j, i);
                CalculateBestKernels(xVector, 40, pixelsYBlock, kernelList_40b);
            }
        }

        material = GetComponent<Renderer>().sharedMaterial;
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
        material.SetInt("pixelsPerBlock", PIXELS_PER_BLOCK_MIN);
        material.SetInt("kernelsPerBlock", PIXELS_PER_BLOCK_MIN);
        material.SetInt("frameWidth", FRAME_WIDTH);
        material.SetInt("frameHeight", FRAME_HEIGHT);
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
        }
#else
        mousePosition.x = Input.mousePosition.x / Screen.width * 20f;
        mousePosition.y = (Screen.height - Input.mousePosition.y) / Screen.height * 20f;

        // Switch best kernels every frame and send to buffer
        // Check whether the block needs to be split
        dynamicKernelsPerBlock = (int)slider.value;
        muXList.Clear();
        muYnPiList.Clear();
        coMatrixInvList.Clear();
        determinantList.Clear();
        for (int i = 0; i < FRAME_HEIGHT; i += PIXELS_PER_BLOCK_MAX * FRAME_HEIGHT / FRAME_WIDTH)
        {
            for (int j = 0; j < FRAME_WIDTH; j += PIXELS_PER_BLOCK_MAX)
            {
                Vector4 xVector = new Vector4(mousePosition.x, mousePosition.y, j, i);
                SwitchKernelsToBuffer(xVector);
            }
        }
        muXBuffer.SetData(muXList);
        muYnPiBuffer.SetData(muYnPiList);
        coMatrixInvBuffer.SetData(coMatrixInvList);
        determinantBuffer.SetData(determinantList);
        material.SetBuffer("muXList", muXBuffer);
        material.SetBuffer("muYnPiList", muYnPiBuffer);
        material.SetBuffer("coMatrixInvList", coMatrixInvBuffer);
        material.SetBuffer("determinantList", determinantBuffer);
        material.SetInt("kernelsPerBlock", dynamicKernelsPerBlock);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
#endif
        // material.SetFloat("mouseX", mousePosition.x);
        // material.SetFloat("mouseY", mousePosition.y);

#if UNITY_EDITOR
            if (frameCount >= 100)
            {
                EditorApplication.isPlaying = false;
            }
            deltaTime += Time.deltaTime;
            if (deltaTime > 1.0f)
            {
                material.SetFloat("mouseX", mousePositionList[frameCount].x);
                material.SetFloat("mouseY", mousePositionList[frameCount].y);
                deltaTime = 0.0f;
                frameCount++;
            }
#endif
    }

    void OnDestroy()
    {
        // Debug.Log("OnDestroy: Releasing all buffers");
        muXBuffer.Release();
        muYnPiBuffer.Release();
        coMatrixInvBuffer.Release();
        determinantBuffer.Release();
    }

    void SwitchKernelsToBuffer(Vector4 xVector)
    {
        // Using mahalanobis distance as measure, the closer the better
        // Calculate the average of four anchor points
        float halfLengthX = (xVector.z + PIXELS_PER_BLOCK_MAX / 2) > FRAME_WIDTH ? (FRAME_WIDTH - xVector.z) / 2 : PIXELS_PER_BLOCK_MAX / 2;
        float halfLengthY = (xVector.w + pixelsYPerBlock / 2) > FRAME_HEIGHT ? (FRAME_HEIGHT - xVector.w) / 2 : pixelsYPerBlock / 2;
        float midPixelX = xVector.z + halfLengthX;
        float midPixelY = xVector.w + halfLengthY;
        Vector4 newXVector = new Vector4(xVector.x, xVector.y, midPixelX, midPixelY);
        float maxDistance = 0f;
        for (int i = 0; i < kernelList.Count; i++)
        {
            Vector4 x_min_mu = newXVector - kernelList[i].MuX;
            Matrix4x4 newCoMatrix = kernelList[i].CoMatrix;
            newCoMatrix[0] += (xVector.x * xVector.x * 10.5f);
            newCoMatrix[5] += (xVector.y * xVector.y * 10.5f);
            newCoMatrix[10] += (halfLengthX * halfLengthX * 0.5f);
            newCoMatrix[15] += (halfLengthY * halfLengthY * 0.5f);
            Vector4 tempVector = newCoMatrix.inverse * x_min_mu;
            float exponent = Vector4.Dot(x_min_mu, tempVector);
            float result = kernelList[i].MuYnPi.w * Mathf.Exp(-0.5f * (exponent * exponent));
            kernelList[i].MahaDist = result;
            if (maxDistance < result)
            {
                maxDistance = result;
            }
        }

        // Sort all the kernels by maha dist
        kernelList.Sort((x, y) => y.MahaDist.CompareTo(x.MahaDist));

        int blockX = Mathf.FloorToInt(midPixelX / PIXELS_PER_BLOCK_MIN);
        int blockY = Mathf.FloorToInt(midPixelY / pixelsYPerBlock);
        int amountBlocksInARow = Mathf.CeilToInt((float)FRAME_WIDTH / PIXELS_PER_BLOCK_MIN);
        int startBlock = blockX + (blockY * amountBlocksInARow);

        // writer.WriteLine("StartBlock: " + startBlock + " xVector=" + newXVector.ToString() + " max=" + maxDistance / 1000f);
        // writer.WriteLine("===========");
        int amountOfKernels = 0;
        // Store the best one in data lists
        for (int i = 0; i < PIXELS_PER_BLOCK_MIN; i++)
        {
            // Debug.Log("MuX: " + kernelList[i].MuX);
            // if (kernelList[i].MahaDist < maxDistance / 1000f)
            // {
            //     Vector4 newMuYnPi = kernelList[i].MuYnPi;
            //     newMuYnPi.w = 0f;
            //     muYnPiList.Add(newMuYnPi);
            // }
            // else
            // {
            muYnPiList.Add(kernelList[i].MuYnPi);
            amountOfKernels++;
            // }
            muXList.Add(kernelList[i].MuX);
            coMatrixInvList.Add(kernelList[i].CoMatrix.inverse);
            determinantList.Add(kernelList[i].Determt);
            // writer.WriteLine(kernelList[i].ToString());
            // if (kernelList[i].MahaDist < 0.1f)
            //     amountOfKernels++;
        }
        // writer.WriteLine("Size=" + amountOfKernels);
        if (amountOfKernels > maxKernels)
            maxKernels = amountOfKernels;
    }

    void CalculateBestKernels(Vector4 xVector, int blockSizeX, int blockSizeY, Kernel[] kernelArray)
    {
        float halfLengthX = (xVector.z + blockSizeX / 2) > FRAME_WIDTH ? (FRAME_WIDTH - xVector.z) / 2 : blockSizeX / 2;
        float halfLengthY = (xVector.w + blockSizeY / 2) > FRAME_HEIGHT ? (FRAME_HEIGHT - xVector.w) / 2 : blockSizeY / 2;
        float midPixelX = xVector.z + halfLengthX;
        float midPixelY = xVector.w + halfLengthY;
        Vector4 newXVector = new Vector4(xVector.x, xVector.y, midPixelX, midPixelY);
        float maxDistance = 0f;
        for (int i = 0; i < kernelList.Count; i++)
        {
            Vector4 x_min_mu = newXVector - kernelList[i].MuX;
            Matrix4x4 newCoMatrix = kernelList[i].CoMatrix;
            newCoMatrix[0] += (xVector.x * xVector.x * 10.5f);
            newCoMatrix[5] += (xVector.y * xVector.y * 10.5f);
            newCoMatrix[10] += (halfLengthX * halfLengthX * 0.5f);
            newCoMatrix[15] += (halfLengthY * halfLengthY * 0.5f);
            Vector4 tempVector = newCoMatrix.inverse * x_min_mu;
            float exponent = Vector4.Dot(x_min_mu, tempVector);
            float result = kernelList[i].MuYnPi.w * Mathf.Exp(-0.5f * (exponent * exponent));
            kernelList[i].MahaDist = result;
            if (maxDistance < result)
            {
                maxDistance = result;
            }
        }
        kernelList.Sort((x, y) => y.MahaDist.CompareTo(x.MahaDist));
        int split = blockSizeX / PIXELS_PER_BLOCK_MIN; // 4, 3, 2, 1
        int blockX = Mathf.FloorToInt(xVector.x / PIXELS_PER_BLOCK_MIN);
        int blockY = Mathf.FloorToInt(xVector.y / pixelsYPerBlock);
        int amountBlocksInARow = Mathf.CeilToInt((float)FRAME_WIDTH / PIXELS_PER_BLOCK_MIN);
        int startBlock = blockX + (blockY * amountBlocksInARow);

        for (int k = 0; k < split; k++) // if blocksize=40, then fill the list 40/10 times
        {
            startBlock += (blockY * amountBlocksInARow);
            for (int i = 0; i < KERNELS_PER_BLOCK; i++)
            {
                // Vector4 newMuYnPi = kernelList[i].MuYnPi;
                // if (kernelList[i].MahaDist < maxDistance / 1000f)
                // {
                //     newMuYnPi.w = 0f;
                // }
                // else
                // {
                // newMuYnPi.w = kernelList[i].MuYnPi;
                // }
                Kernel kernel = new Kernel(kernelList[i].MuX, kernelList[i].MuYnPi, kernelList[i].CoMatrix.inverse, kernelList[i].Determt);
                kernelArray[startBlock + i] = kernel;
            }
        }

    }

    /* void PrintCalculation()
    {
        writer = new StreamWriter("calculations.txt", false);

        for (int i = 0; i < FRAME_HEIGHT; i += PIXELS_PER_BLOCK)
        {
            for (int j = 0; j < FRAME_WIDTH; j += PIXELS_PER_BLOCK)
            {
                Vector4 xVector = new Vector4(7.5f, 7.5f, j, i);
                float halfLengthX = (xVector.z + PIXELS_PER_BLOCK / 2) > FRAME_WIDTH ? (FRAME_WIDTH - xVector.z) / 2 : PIXELS_PER_BLOCK / 2;
                float halfLengthY = (xVector.w + PIXELS_PER_BLOCK / 2) > FRAME_HEIGHT ? (FRAME_HEIGHT - xVector.w) / 2 : PIXELS_PER_BLOCK / 2;
                float midPixelX = xVector.z + halfLengthX;
                float midPixelY = xVector.w + halfLengthY;
                xVector.z = midPixelX;
                xVector.w = midPixelY;
                int blockX = Mathf.FloorToInt(midPixelX / PIXELS_PER_BLOCK);
                int blockY = Mathf.FloorToInt(midPixelY / PIXELS_PER_BLOCK);
                int amountBlocksInARow = Mathf.FloorToInt(FRAME_WIDTH / PIXELS_PER_BLOCK) + 1;
                int startBlock = blockX + (blockY * amountBlocksInARow);

                string line = "StartBlock: " + startBlock + " xVector=" + xVector.ToString() + "\n";
                // line += "---------------\n";
                // float weightSum = 0.0f;
                // float weight = 0.0f;
                // Vector3 colorSum = Vector3.zero;
                // for (int k = startBlock * KERNELS_PER_BLOCK; k < (startBlock * KERNELS_PER_BLOCK) + KERNELS_PER_BLOCK; k++)
                // {
                //     Vector4 x_min_mu = xVector - muXList[k];
                //     line += "muX=" + muXList[k].ToString() + " x_min_mu=" + x_min_mu.ToString() + "\n";
                //     Vector4 tempvector = coMatrixInvList[k] * x_min_mu;
                //     line += "comat=" + coMatrixInvList[k].ToString() + " tempv=" + tempvector.ToString() + "\n";
                //     float exponent = Vector4.Dot(tempvector, x_min_mu);
                //     float result = Mathf.Exp(-0.5f * exponent);
                //     line += "exp=" + exponent + " efunc=" + result + "\n";
                //     result /= determinantList[k];
                //     line += "determ=" + determinantList[k] + " gauss=" + result + "\n";
                //     weight = muYnPiList[k].w * result;
                //     line += "pi=" + muYnPiList[k].w + " weight=" + weight + "\n";
                //     weightSum += weight;
                //     line += "weightsum=" + weightSum + "\n";
                //     Vector3 muY = new Vector3(muYnPiList[k].x, muYnPiList[k].y, muYnPiList[k].z);
                //     colorSum += (muY * weight);
                //     line += "muY=" + muY + " csum=" + colorSum.ToString("G5") + "\n";
                //     line += "------------\n";
                // }
                // line += "====> " + colorSum.ToString("G5") + " / " + weightSum + " = ";
                // colorSum /= weightSum;
                // line += colorSum.ToString("G5") + "\n";
                line += "======================\n";
                writer.WriteLine(line);
            }
        }

        writer.Close();


    }
*/
}