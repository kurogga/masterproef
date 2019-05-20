Shader "Unlit/LightFieldMulti"
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
			uniform StructuredBuffer<float4x4> coMatrixInvList; // -1/2 coMatrix^(-1)
			uniform StructuredBuffer<float> determinantList; // 1/sqrt(determinat(coMatrix))
			
			uniform int pixelsPerBlock;
			uniform int kernelsPerBlock;
			uniform int frameWidth;
			uniform int frameHeight;
			uniform float mouseX;
			uniform float mouseY;
			uniform int dynamicKernels;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
            };

			float rand(float2 co){
    			return frac(sin(_Time.x*dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
			}

			float getNormalDist(float4 x, int k)
			{	
				// e^(-0.5*(x-muX)^T*coMatrix*(x-muX)) / sqrt(det(coMatrix))
				float4 x_min_mu = x-muXList[k];
				float4 tempvector = mul(coMatrixInvList[k],x_min_mu);
				float exponent = dot(tempvector,x_min_mu);
				float result = exp(min(0.0,-0.5*exponent));
				result /= determinantList[k];
				return result;
			}

			v2f vert(appdata v)
			{ // UV is screen space (clipPos.xyz / clipPos.w)
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv.y=1-o.uv.y; // y position start at top
				o.screenPos = ComputeScreenPos(v.vertex);
				return o;
			}

            float4 frag (v2f v) : SV_Target
            {
				float pixelX = v.uv.x*frameWidth;
				float pixelY = v.uv.y*frameHeight;
                float4 x = float4(mouseX,mouseY, pixelX, pixelY);
				
				// Get the block of current pixel
				int blockX = floor(pixelX/pixelsPerBlock);
				int blockY = floor(pixelY/pixelsPerBlock);
				int amountBlocksInARow = floor(frameWidth/pixelsPerBlock);
				int amountBlocksInAColumn = floor(frameHeight/pixelsPerBlock);
				int startBlock = blockX + (blockY*amountBlocksInARow);

				// Check if the block x value is on the edge
				int begin = blockX != 0 ? (startBlock*kernelsPerBlock)-kernelsPerBlock : startBlock*kernelsPerBlock;
				int end = blockX != amountBlocksInARow ? (startBlock*kernelsPerBlock) + 2*kernelsPerBlock : (startBlock*kernelsPerBlock) + kernelsPerBlock;

				// Check if upper blocks exist
				int beginUpper = blockY != 0 ? begin - amountBlocksInARow*kernelsPerBlock : 0;
				int endUpper = blockY != 0 ? end - amountBlocksInARow*kernelsPerBlock : 0;

				// Check if lower blocks exist
				int beginLower = blockY != amountBlocksInAColumn ? begin + amountBlocksInARow*kernelsPerBlock : 0;
				int endLower = blockY != amountBlocksInAColumn ? end + amountBlocksInARow*kernelsPerBlock : 0;

				// Calculate sum of all weights
				float weightSum = 0.0;
                float weight = 0.0;
				float3 colorSum = float3(0.0,0.0,0.0);

				// KERNELS SORTED IN 3 ROWS
				// Left and right block + itself
				for(int i = begin; i < end; i++)
				{
					weight = muYnPiList[i].w*getNormalDist(x, i);
					weightSum += weight;
					colorSum += (weight*muYnPiList[i].xyz);
				}

				if(pixelY > frameHeight/2-50) {
					// Upper 3 blocks
					for(int i = beginUpper; i < endUpper; i+=3)
					{
						weight = muYnPiList[i].w*getNormalDist(x, i);
						weightSum += weight;
						colorSum += (weight*muYnPiList[i].xyz);
					}

					// Lower 3 blocks
					for(int i = beginLower; i < endLower; i+=3)
					{
						weight = muYnPiList[i].w*getNormalDist(x, i);
						weightSum += weight;
						colorSum += (weight*muYnPiList[i].xyz);
					}
				}
		
				// TESTING WITH FILTER
				// for (int i = startBlock*kernelsPerBlock; i < (startBlock*kernelsPerBlock)+kernelsPerBlock; i++)
				// {
				// 	weight = muYnPiList[i].w*getNormalDist(x, i);
				// 	weightSum += weight;
				// 	// Calculate weight * muY(color)
				// 	colorSum += (weight*muYnPiList[i].xyz);
				// }

				// int dynamicRange = kernelsPerBlock/4;
				// if(blockX != 0)
				// {	// Kernels of left block
				// 	for (int i = startBlock*kernelsPerBlock-kernelsPerBlock; i < (startBlock*kernelsPerBlock)-dynamicRange; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }
				// if(blockX != amountBlocksInARow)
				// {	// Kernels of right block
				// 	for (int i = startBlock*kernelsPerBlock+kernelsPerBlock; i < (startBlock*kernelsPerBlock)+kernelsPerBlock+dynamicRange; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }
				// if(blockY != amountBlocksInAColumn)
				// {	// Kernels of upper block
				// 	for (int i = startBlock*kernelsPerBlock+amountBlocksInARow*kernelsPerBlock; i < (startBlock*kernelsPerBlock)+dynamicRange+amountBlocksInARow*kernelsPerBlock; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }
				// if(blockY != 0)
				// {	// Kernels of lower block
				// 	for (int i = startBlock*kernelsPerBlock-amountBlocksInARow*kernelsPerBlock; i < (startBlock*kernelsPerBlock)+dynamicRange-amountBlocksInARow*kernelsPerBlock; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }
				// dynamicRange=kernelsPerBlock/2;
				// if(blockX != 0 && blockY != 0)
				// {	// Kernels of top left block
				// 	int middlePos = (startBlock*kernelsPerBlock)-amountBlocksInARow*kernelsPerBlock;
				// 	for(int i = middlePos-kernelsPerBlock; i < middlePos+dynamicRange; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }
				// if(blockX != 0 && blockY != amountBlocksInAColumn)
				// {	// Kernels of bottom left block
				// 	int middlePos = (startBlock*kernelsPerBlock)+amountBlocksInARow*kernelsPerBlock;
				// 	for(int i = middlePos-kernelsPerBlock; i < middlePos+dynamicRange; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }
				// if(blockX != amountBlocksInARow && blockY != 0)
				// {	// Kernels of top right block
				// 	int middlePos = (startBlock*kernelsPerBlock)+kernelsPerBlock-amountBlocksInARow*kernelsPerBlock;
				// 	for(int i = middlePos; i < middlePos+dynamicRange; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }
				// if(blockX != amountBlocksInARow && blockY != amountBlocksInAColumn)
				// {	// Kernels of bottom right block
				// 	int middlePos = (startBlock*kernelsPerBlock)+kernelsPerBlock+amountBlocksInARow*kernelsPerBlock;
				// 	for(int i = middlePos; i < middlePos+dynamicRange; i++)
				// 	{
				// 		weight = muYnPiList[i].w*getNormalDist(x, i);
				// 		weightSum += weight;
				// 		colorSum += (weight*muYnPiList[i].xyz);
				// 	}
				// }

				// Calculate colorSum/sum of all weights
				colorSum /= weightSum;
				float4 result = float4(colorSum, 1.0);
                return result;
            }
            ENDCG
        }
    }
}
