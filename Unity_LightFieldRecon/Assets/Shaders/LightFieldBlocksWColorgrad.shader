Shader "Unlit/LightFieldBlocksWColorgrad"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
        LOD 200

		Pass
		{
			CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
            // #pragma exclude_renderers d3d11 gles
			#pragma target 5.0
			// #pragma only_renderers glcore
            #pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			uniform StructuredBuffer<float4> muXList; //cameraPos(x,y) + pixelPos(x,y)
			uniform StructuredBuffer<float4> muYnPiList; // color(x,y,z) + pi
			uniform StructuredBuffer<float4x4> coMatrixInvList; // coMatrix^(-1)
			uniform StructuredBuffer<float4x4> gradientList;
			uniform StructuredBuffer<float> determinantList; // 1/sqrt(determinat(coMatrix))

			uniform int pixelsPerBlock;
			uniform int kernelsPerBlock;
			uniform int dynamicKernels;
			uniform int frameWidth;
			uniform int frameHeight;
			uniform float mouseX;
			uniform float mouseY;
			uniform int withGradient;

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

			float getNormalDist(float4 x, int k)
			{	
				float4 x_min_mu = x-muXList[k];
				float4 tempvector = mul(coMatrixInvList[k],x_min_mu);
				float exponent = dot(x_min_mu,tempvector);
				float result = exp(-0.5*exponent);
				result /= determinantList[k];
				return result;
			}

			v2f vert(appdata v)
			{ 
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv.y = 1 - o.uv.y; // y position start at top
				return o;
			}

            float4 frag (v2f v) : SV_Target
            {
				float pixelX = v.uv.x*frameWidth;
				float pixelY = v.uv.y*frameHeight;
                float4 x = float4(mouseX,mouseY, pixelX, pixelY);

				int pixelsYPerBlock = pixelsPerBlock * frameHeight/frameWidth; 
				// pixelsYPerBlock = pixelsPerBlock;
				int blockX = floor(pixelX/pixelsPerBlock);
				int blockY = floor(pixelY/pixelsYPerBlock);
				int amountBlocksInARow = ceil((float)frameWidth/pixelsPerBlock);
				int startBlock = blockX + (blockY*amountBlocksInARow);

				float weightSum = 0.0;
                float weight = 0.0;
				float3 colorSum = float3(0.0,0.0,0.0);

				for (int i = startBlock*kernelsPerBlock; i < startBlock*kernelsPerBlock + dynamicKernels; i++)
				{
					float4 x_min_mu = x-muXList[i];
					float4 tempvector = mul(coMatrixInvList[i],x_min_mu);
					float exponent = dot(x_min_mu,tempvector);
					float result = exp(-0.5*exponent);
					result /= determinantList[i];
					float4 colorweight = mul(gradientList[i],tempvector);

					weight = muYnPiList[i].w*result;
					weightSum += weight;
					float3 regressor = muYnPiList[i].xyz + colorweight.xyz;
					if(withGradient == 0)
						regressor = muYnPiList[i].xyz;
					colorSum += (weight*regressor);
				}
				
				colorSum /= weightSum;
				float4 result = float4(colorSum, 1.0);
                return result;
            }
            ENDCG
        }
    }
}
