Shader "Unlit/LightField2"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
        LOD 200

		Pass
		{
			CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
            #pragma exclude_renderers d3d11 gles
			#pragma target 5.0
			#pragma only_renderers glcore
            #pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			uniform StructuredBuffer<float4> muXList; //cameraPos(x,y) + pixelPos(x,y)
			uniform StructuredBuffer<float4> muYnPiList; // color(x,y,z) + pi
			uniform StructuredBuffer<float4x4> coMatrixInvList; // -1/2 coMatrix^(-1)
			uniform StructuredBuffer<float> determinantList; // 1/sqrt(determinat(coMatrix))
			
			uniform int kernels;
			uniform float mouseX;
			uniform float mouseY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			float rand(float2 co){
    			return frac(sin(_Time.x*dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
			}

			float getNormalDist(float4 x, int k)
			{	
				// e^(-0.5*(x-muX)^T*coMatrix*(x-muX)) / sqrt(det(coMatrix))
				float4 x_min_mu = x-muXList[k];
				float4 tempvector = mul(x_min_mu,coMatrixInvList[k]);
				float exponent = dot(tempvector,x_min_mu);
				float result = exp(exponent);
				result *= determinantList[k];
				return result;
			}

			v2f vert(appdata v)
			{ // UV is screen space (clipPos.xyz / clipPos.w)
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv.y=1-o.uv.y;
				return o;
			}

            float4 frag (v2f v) : SV_Target
            {
				// float4 x = float4(-7.0,-7.0, v.uv.x, v.uv.y);
                // float4 x = float4(mouseX,mouseY, v.uv.x, v.uv.y);
				float4 x = float4(2,2, v.vertex.x/_ScreenParams.x, 1-v.vertex.y/_ScreenParams.y);

				// Calculate sum of all weights
				float weightSum = 0.0;
                float weight = 0.0;
				float3 colorSum = float3(0.0,0.0,0.0);
				for (int i = 0; i < kernels; i++)
				{
					weight = muYnPiList[i].w*getNormalDist(x, i);
					weightSum += weight;
					// Calculate weight * muY(color)
					colorSum += (weight*muYnPiList[i].xyz);
				}
				// Calculate colorSum/sum of all weights
				colorSum /= weightSum;
				float4 result = float4(colorSum, 1.0);
                // result = tex2D(_MainTex,col);
                return result;
            }
            ENDCG
        }
    }
}
