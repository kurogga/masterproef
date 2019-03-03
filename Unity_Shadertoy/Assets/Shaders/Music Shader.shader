// Hexagone by Martijn Steinrucken aka BigWings - 2019
// countfrolic@gmail.com
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// 
// This started as an idea to do the effect below, but with hexagons:
// https://www.shadertoy.com/view/wdlGRM
//
// Turns out that really doesn't look very nice so I just made it
// into a dance party instead ;)
//
// Music: https://soundcloud.com/buku/front-to-back

Shader "Unlit/Music Shader"
{
    Properties{
        _MainTex("MainTex", 2D) = "white"{}

    }
    SubShader {

    	Pass {
    		CGPROGRAM

    		#pragma vertex VertexProgram
			#pragma fragment FragmentProgram

			#include "UnityCG.cginc"

			float4 _Tint;
			sampler2D _MainTex;
            float4 _MainTex_ST;
			static const float R3 = 1.732051;

			struct Interpolators {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			struct VertexData {
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

            Interpolators VertexProgram (VertexData v) {
				Interpolators i;
				i.position = UnityObjectToClipPos(v.position);
				i.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return i;
			}

            float4 HexCoords(float2 uv) {
                float2 s = float2(1.0, R3);
                float2 h = .5*s;

                float2 gv = s*uv;
                
                float2 a = fmod(gv, s)-h;
                float2 b = fmod(gv+h, s)-h;
                
                float2 ab = dot(a,a)<dot(b,b) ? a : b;
                float2 st = ab;
                float2 id = gv-ab;
                
                // ab = abs(ab);
                //st.x = .5-max(dot(ab, normalize(s)), ab.x);
                st = ab;
                return float4(st, id);
            }

            float GetSize(float2 id, float seed) {
                float d = length(id);
                float t = _Time*.5;
                float a = sin(d*seed+t)+sin(d*seed*seed*10.+t*2.);
                return a/2. +.5;
            }

            float2x2 Rot(float a) {
                float s = sin(a);
                float c = cos(a);
                return float2x2(c, -s, s, c);
            }

            float Hexagon(float2 uv, float r, float2 offs) {
                
                uv = mul(Rot(lerp(0., 3.1415, r)), uv);
                
                r /= 1./sqrt(2.);
                uv = float2(-uv.y, uv.x);
                uv.x *= R3;
                uv = abs(uv);
                
                float2 n = normalize(float2(1,1));
                float d = dot(uv, n)-r;
                d = max(d, uv.y-r*.707);
                
                d = smoothstep(.06, .02, abs(d));
                
                d += smoothstep(.1, .09, abs(r-.5))*sin(_Time);
                return d;
            }

            float Xor(float a, float b) {
                return a+b;
                //return a*(1.-b) + b*(1.-a);
            }

            float Layer(float2 uv, float s) {
                float4 hu = HexCoords(uv*2.);

                float d = Hexagon(hu.xy, GetSize(hu.zw, s), float2(0,0));
                float2 offs = float2(1,0);
                d = Xor(d, Hexagon(hu.xy-offs, GetSize(hu.zw+offs, s), offs));
                d = Xor(d, Hexagon(hu.xy+offs, GetSize(hu.zw-offs, s), -offs));
                offs = float2(.5,.8725);
                d = Xor(d, Hexagon(hu.xy-offs, GetSize(hu.zw+offs, s), offs));
                d = Xor(d, Hexagon(hu.xy+offs, GetSize(hu.zw-offs, s), -offs));
                offs = float2(-.5,.8725);
                d = Xor(d, Hexagon(hu.xy-offs, GetSize(hu.zw+offs, s), offs));
                d = Xor(d, Hexagon(hu.xy+offs, GetSize(hu.zw-offs, s), -offs));
                
                return d;
            }

            float N(float p) {
                return frac(sin(p*123.34)*345.456);
            }

            float3 Col(float p, float offs) {
                float n = N(p)*1234.34;
                
                return sin(n*float3(12.23,45.23,56.2)+offs*3.)*.5+.5;
            }

            float3 GetRayDir(float2 uv, float3 p, float3 lookat, float zoom) {
                float3 f = normalize(lookat-p),
                    r = normalize(cross(float3(0,1,0), f)),
                    u = cross(f, r),
                    c = p+f*zoom,
                    i = c + uv.x*r + uv.y*u,
                    d = normalize(i-p);
                return d;
            }

			float4 FragmentProgram (Interpolators i) : SV_TARGET {
                float2 screen = float2(800.0,800.0);
				float2 uv = (i.uv-.5*_ScreenParams.xy)/_ScreenParams.y;
                float2 UV = i.uv.xy/_ScreenParams.xy-.5;
                float duv= dot(UV, UV);
                float2 m = i.uv.xy/_ScreenParams.xy-.5;
                
                float t = _Time*.2+m.x*10.+5.;
                
                float y = sin(t*.5);//+sin(1.5*t)/3.;
                float3 ro = float3(0, 20.*y, -5);
                float3 lookat = float3(0,0,-10);
                float3 rd = GetRayDir(uv, ro, lookat, 1.);
                
                float3 col = float3(0,0,0);
                
                float3 p = ro+rd*(ro.y/rd.y);
                float dp = length(p.xz);
                
                if((ro.y/rd.y)>0.)
                    col *= 0.;
                else {
                    uv = p.xz*.1;

                    uv *= lerp(1., 5., sin(t*.5)*.5+.5);

                    uv = mul(Rot(t),uv);
                    m = mul(Rot(t),m);

                    uv.x *= R3;
                    

                    for(float i=0.; i<1.; i+=1./3.) {
                        float id = floor(i+t);
                        t = frac(i+t);
                        float z = lerp(5., .1, t);
                        float fade = smoothstep(0., .3, t)*smoothstep(1., .7, t);

                        col += fade*t*Layer(uv*z, N(i+id))*Col(id,duv);
                    }
                }
                col *= 2.;
                
                if(ro.y<0.) col = 1.-col;
                
                col *= smoothstep(18., 5., dp);
                col *= 1.-duv*2.;
                float4 fragColor = float4(col,1.0);
                return fragColor;
			}

			ENDCG
    	}
    }
}