Shader "Aetherlands/Coastal Surface" {
Properties { _MainTex("Pixel surface",2D)="white"{} }
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
Pass {Tags {"LightMode"="UniversalForward"} Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 c:COLOR;};
struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 n:TEXCOORD1;float3 w:TEXCOORD2;float4 c:COLOR;};
V vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.c=a.c;return o;}
half4 frag(V i):SV_Target {Light l=GetMainLight(TransformWorldToShadowCoord(i.w));float light=saturate(dot(normalize(i.n),l.direction));half3 tint=i.c.rgb;
#ifdef UNITY_COLORSPACE_GAMMA
#else
tint=SRGBToLinear(tint);
#endif
return half4(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb*tint*(half3(.55,.61,.69)+light*l.shadowAttenuation*.52),1);}
ENDHLSL }
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
UsePass "Universal Render Pipeline/Lit/DepthOnly"
UsePass "Universal Render Pipeline/Lit/DepthNormals"
} }
