
Shader "Unlit/Sphere Shader"
{
	Properties{
		
	}

    SubShader {

    	Pass {
    		CGPROGRAM

    		#pragma vertex VertexProgram
			#pragma fragment FragmentProgram

			#include "UnityCG.cginc"

			struct v2f {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 screenCoord : TEXCOORD1;
			};

			struct appdata {
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			v2f VertexProgram (appdata v) {
				v2f i;
				i.position = UnityObjectToClipPos(v.position);
				i.uv = v.uv;
				i.screenCoord.xy = ComputeScreenPos(i.position);
				return i;
			}

			#define R3 1.732051

			float4 HexCoords(float2 uv) {
				float2 s = float2(1, R3);
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
				float t = _Time.y*.5; //iTime = 1.0
				float a = sin(d*seed+t)+sin(d*seed*seed*10.+t*2.);
				return a/2. +.5;
			}

			float2x2 Rot(float a) {
				float s = sin(a);
				float c = cos(a);
				return float2x2(c, -s, s, c);
			}

			float Hexagon(float2 uv, float r, float2 offs) {
				
				uv = mul(uv, Rot(lerp(0., 3.1415, r)));
				
				r /= 1./sqrt(2.);
				uv = float2(-uv.y, uv.x);
				uv.x *= R3;
				uv = abs(uv);
				
				float2 n = normalize(float2(1,1));
				float d = dot(uv, n)-r;
				d = max(d, uv.y-r*.707);
				
				d = smoothstep(.06, .02, abs(d));
				
				d += smoothstep(.1, .09, abs(r-.5))*sin(_Time.y); // iTime = 1.0
				return d;
			}

			float Xor(float a, float b) {
				return a+b;
				//return a*(1.-b) + b*(1.-a);
			}

			float Layer(float2 uv, float s) {
				float4 hu = HexCoords(uv*2.);

				float d = Hexagon(hu.xy, GetSize(hu.zw, s), 0);
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
				float3 f = normalize(lookat-p);
				float3 r = normalize(cross(float3(0,1,0), f));
				float3 u = cross(f, r);
				float3 c = p+f*zoom;
				float3 i = c + uv.x*r + uv.y*u;
				float3 d = normalize(i-p);
				return d;
			}


			float4 FragmentProgram (v2f i) : SV_TARGET {
				//iResolution.xy = (1,1) => 1
				//vermenigvuldiging volgorde: float3 += float * float3
				//							->float3 = float*float3 + float3
				// IMPORTANT: i.position.xy/_ScreenParams.xy => position in the screen 
				//                               (needed when rendering multiple quads)
				float xvalue = i.position.x/_ScreenParams.x;
				float yvalue = i.position.y/_ScreenParams.y;
				float2 xyvalues = float2(xvalue,yvalue);
				float2 uv = (xyvalues.xy-.5*1.)/1.;
				float2 UV = xyvalues.xy/1.-.5;
				float duv= dot(UV, UV);
				float2 m = 1./1.-.5;
				
				float t = _Time.y*.2+m.x*10.+5.; // iTime = 1.0
				
				float y = sin(t*.5);//+sin(1.5*t)/3.;
				float3 ro = float3(0, 20.*y, -5);
				float3 lookat = float3(0,0,-10);
				float3 rd = GetRayDir(uv, ro, lookat, 1.);
				
				float3 col = 0;
				
				float3 p = ro+rd*(ro.y/rd.y);
				float dp = length(p.xz);
				
				if((ro.y/rd.y)>0.)
					col *= 0.;
				else {
					uv = p.xz*.1;

					uv *= lerp(1., 5., sin(t*.5)*.5+.5);

					uv = mul(uv, Rot(t));
					m = mul(m, Rot(t));

					uv.x *= R3;
					
					col = float3(1,1,1);
					for(float j=0.; j<1.; j+=1./3.) {
						float id = floor(j+t);
						t = frac(j+t);
						float z = lerp(5., .1, t);
						float fade = smoothstep(0., .3, t)*smoothstep(1., .7, t);

						col += (fade*t*Layer(uv*z, N(j+id))*Col(id,duv));
					}
				}
				col *= 2.;
				
				if(ro.y<0.) col = 1.-col;
				
				col *= smoothstep(18., 6., dp);
				col *= 0.95-duv*1.9;
				return float4(col,1.0);
			}

			ENDCG
    	}
    }
}
