using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Shadertoy : MonoBehaviour
{
    Material mat;
    int count = 0;
    Shader[] shaders;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Starting now!");
        mat = GetComponent<Renderer>().sharedMaterial;
        shaders = new Shader[5];
        Shader tempShader = Shader.Find("Unlit/Sphere Shader");
        shaders[0] = tempShader;
        tempShader = Shader.Find("ShaderToyConverter/starswirl");
        shaders[1] = tempShader;
        tempShader = Shader.Find("ShaderMan/Flame");
        shaders[2] = tempShader;
        tempShader = Shader.Find("ShaderMan/Bubbles");
        shaders[3] = tempShader;
        tempShader = Shader.Find("ShaderMan/PlasmaGlobe");
        shaders[4] = tempShader;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) || Input.touchCount == 1){
            print("Shader nr "+count);
            count++;
            if(count == 5){
                count = 0;
            }
            mat.shader = shaders[count];
        }
        if(Input.GetKeyDown(KeyCode.Escape)){
            Application.Quit();
        }
    }
}
