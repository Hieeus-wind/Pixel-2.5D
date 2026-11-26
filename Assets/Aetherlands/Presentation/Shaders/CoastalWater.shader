Shader "Aetherlands/Tidal Glass" {
SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;};
V vert(float4 p:POSITION){V o;o.w=TransformObjectToWorld(p.xyz);o.p=TransformWorldToHClip(o.w);return o;}
float h(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(h(i),h(i+float2(1,0)),f.x),lerp(h(i+float2(0,1)),h(i+1),f.x),f.y);}
half4 frag(V i):SV_Target {float2 uv=GetNormalizedScreenSpaceUV(i.p);float d=SampleSceneDepth(uv);
#if !UNITY_REVERSED_Z
d=lerp(UNITY_NEAR_CLIP_VALUE,1,d);
#endif
float3 bottom=ComputeWorldSpacePosition(uv,d,UNITY_MATRIX_I_VP);float depth=max(0,i.w.y-bottom.y);
float2 p=floor(i.w.xz*32)/32;float t=_Time.y*.32;float cloud=noise(p*.37)+.38*noise(p*1.1);
float swell=noise(p*float2(1.2,4)+float2(t*.2,t));float ridge=abs(swell-.54);
float3 c=lerp(float3(.23,.74,.68),float3(.055,.39,.48),saturate(depth*.32+cloud*.23));c+=floor(cloud*7)*.008;
float rim=(1-smoothstep(.02,.065,abs(depth-(.14+.10*sin(t*2+cloud*8)))))*smoothstep(.24,.63,swell);
float sparkle=(1-smoothstep(.006,.016,ridge))*smoothstep(.64,.85,noise(p*1.8+t*.25));
c=lerp(c,float3(.83,.96,.82),max(rim*.8,sparkle*.65));
#ifndef UNITY_COLORSPACE_GAMMA
c=SRGBToLinear(c);
#endif
return half4(c,saturate(.59+depth*.17+rim*.19+sparkle*.16));}
ENDHLSL } } }
