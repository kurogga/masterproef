using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;

// [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
 public class DrawQuad : MonoBehaviour 
 {
    //  MeshFilter filter;

    //  // Use this for initialization
    //  void Start ()
    //  {
    //      Mesh mesh = new Mesh();
     
    //      Vector3[] vertices = new Vector3[4];
    //      vertices[0] = new Vector3(0,0,0); //top-left
    //      vertices[1] = new Vector3(2,0,0); //top-right
    //      vertices[2] = new Vector3(0,-2,0); //bottom-left
    //      vertices[3] = new Vector3(2,-2,0); //bottom-right
         
    //      mesh.vertices = vertices;
         
    //      int[] triangles = new int[6]{0,1,2,3,2,1};
    //      mesh.triangles = triangles;
         
    //      //this is also acceptable!
    //      //mesh.SetTriangleStrip(new int[4]{0,1,2,3}, 0);
     
    //      Vector2[] uvs = new Vector2[4];
    //      uvs[0] = new Vector2(0,1); //top-left
    //      uvs[1] = new Vector2(1,1); //top-right
    //      uvs[2] = new Vector2(0,0); //bottom-left
    //      uvs[3] = new Vector2(1,0); //bottom-right
     
    //      mesh.uv = uvs;
     
    //      Vector3[] normals = new Vector3[4]{Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward};
    //      mesh.normals = normals;
     
    //      //you could also call this instead...
    //      //mesh.RecalculateNormals();
     
     
    //      //grab our filter.. set the mesh
    //     filter = GetComponent<MeshFilter>();
    //     filter.mesh = mesh;
     
    //      //you can do your material stuff here...
    //      MeshRenderer r = GetComponent<MeshRenderer>();
    //      r.material = new Material(Shader.Find("starswirl"));
     
    //  }

    public TextAsset textFile;
    public Material material;
    public Vector2 mousePosition;
    public int kernels;
    // Buffer to store data and pass to shader
    public ComputeBuffer muXBuffer;
    public ComputeBuffer muYnPiBuffer;
    public ComputeBuffer coMatrixInvBuffer;
    public ComputeBuffer determinantBuffer;

    // List to store data from file and pass to buffer
    public List<Vector4> muXList; //cameraposition en pixelposition
    public List<Vector4> muYnPiList; // color en pi
    public List<Matrix4x4> coMatrixInvList; // coMatrix^(-1)
    public List<float> determinantList; // sqrt(determinant(coMatrix))
    public List<Matrix4x4> transformList = new List<Matrix4x4>();

    // void OnPostRender()
    // {
    //     if (!material)
    //     {
    //         Debug.LogError("Please Assign a material on the inspector");
    //         return;
    //     }
    //     GL.PushMatrix();
    //     material.SetPass(0);
    //     GL.LoadOrtho();
    //     GL.Begin(GL.QUADS);
    //     float x = 0;
    //     float y = 0;
    //     float z = 0;
    //     float increment = 0.005f;
    //     for(int i=0; i<100; i++)
    //     {
    //         for(int j=0;j<100;j++)
    //         {
    //             GL.Vertex3(x+increment*i,y+increment*j,z);
    //             GL.Vertex3(x+increment*i,y+increment*(j+1),z);
    //             GL.Vertex3(x+increment*(i+1),y+increment*(j+1),z);
    //             GL.Vertex3(x+increment*(i+1),y+increment*j,z);
    //         }
    //     }
    //     GL.End();
    //     GL.PopMatrix();
    // }
     

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
        List<string> eachLine = new List<string>();
        float twoPi = (float) (2 * Mathf.PI);

        string theWholeFileAsOneLongString = textFile.text;
        eachLine.AddRange(theWholeFileAsOneLongString.Split("\n"[0]));
        kernels = eachLine.Count-1;
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
            determinantCM = (float) Mathf.Sqrt(Mathf.Pow(twoPi,4)*coMatrix.determinant);
            determinantCM = 1 / determinantCM;
            // determinantCM = Mathf.Sqrt(coMatrix.determinant);

            //-1/2 * inv matrix
            Matrix4x4 invMatrix = coMatrix.inverse;
            for(int j=0; j<16; j++){
                invMatrix[j]/= -2;
            }

            // Adding each values to correct list
            muXList.Add(muX);
            muYnPiList.Add(muYnPi);
            coMatrixInvList.Add(invMatrix);
            determinantList.Add(determinantCM);
        }

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
        material.SetInt("kernels", kernels);
        Debug.Log("Finished initialiazing");
        
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        // Draw quads
        for(int x = 0; x < 10; x++)
        {
            for(int y = 0; y < 100; y++)
            {
               
                //We will assume you want to create your cube of cubes at 0,0,0
                Vector3 position = new Vector3(0, 0, 0);

                float increment = 0.5f;
                //Take the origin position, and apply the offsets
                position.x += (increment*x);
                position.y += (increment*y);

                //Create a matrix for the position created from this iteration of the loop
                Matrix4x4 matrix = new Matrix4x4();

                //Set the position/rotation/scale for this matrix
                matrix.SetTRS(position, Quaternion.Euler(Vector3.zero), Vector3.one);

                //Add the matrix to the list, which will be used when we use DrawMeshInstanced.
                transformList.Add(matrix);
                
            }
        }
        //After the for loops are finished, and transformList has several matrices in it, simply pass DrawMeshInstanced the mesh, a material, and the list of matrices containing all positional info.
        Graphics.DrawMeshInstanced(mesh, 0, material, transformList);

    }

    // Update is called once per frame
    void Update()
    {
        mousePosition = new Vector2(Input.mousePosition.x/Screen.width*20,Input.mousePosition.y/Screen.height*20);
        material.SetFloat("mouseX",mousePosition.x);
        material.SetFloat("mouseY",mousePosition.y);
        
        if(Input.GetKeyDown(KeyCode.Space)){

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
    } 
 }