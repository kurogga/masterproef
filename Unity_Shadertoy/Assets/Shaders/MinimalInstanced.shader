// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/MinimalInstanced"
{
    Properties
    {
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            float4 vert(appdata_base v) : POSITION {
                return UnityObjectToClipPos (v.vertex);
            }

            fixed4 frag(float4 sp:WPOS) : COLOR {
                // return fixed4(sp.x/_ScreenParams.x,0.0,0.0,1.0);
                fixed2 uv = sp.xy/_ScreenParams.xy;
                uv.x *=  1 / 1;

                // background	 
                fixed3 color = fixed3(0.8 + 0.2*uv.y,0.8 + 0.2*uv.y,0.8 + 0.2*uv.y);

                // bubbles	
                [unroll(100)]
                for( int i=0; i<40; i++ )
                {
                    // bubble seeds
                    fixed pha =      sin(fixed(i)*546.13+1.0)*0.5 + 0.5;
                    fixed siz = pow( sin(fixed(i)*651.74+5.0)*0.5 + 0.5, 4.0 );
                    fixed pox =      sin(fixed(i)*321.55+4.1) * 1 / 1;

                    // buble size, position and color
                    fixed rad = 0.1 + 0.5*siz;
                    fixed2  pos = fixed2( pox, -1.0-rad + (2.0+2.0*rad)*fmod(pha+0.1*_Time.y*(0.2+0.8*siz),1.0));
                    fixed dis = length( uv - pos );
                    fixed3  col = lerp( fixed3(0.94,0.3,0.0), fixed3(0.1,0.4,0.8), 0.5+0.5*sin(fixed(i)*1.2+1.9));
                    //    col+= 8.0*smoothstep( rad*0.95, rad, dis );
                    
                    // render
                    fixed f = length(uv-pos)/rad;
                    f = sqrt(clamp(1.0-f*f,0.0,1.0));
                    color -= col.zyx *(1.0-smoothstep( rad*0.95, rad, dis )) * f;
                }

                // vigneting	
                color *= sqrt(1.5-0.5*length(uv));

                return  fixed4(color,1.0);
            }
            ENDCG
        }
    }
}
