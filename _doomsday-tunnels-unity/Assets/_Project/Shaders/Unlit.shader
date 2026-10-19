// Flat colour, no lighting, faded toward the sector's haze colour with distance down the tunnel, exactly as the web
// game fades everything. Vertex colour times tint times intensity; intensity above 1 is what bloom picks up.
Shader "Doomsday/Unlit"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1
        _Haze ("Haze", Color) = (0,0,0,1)
        _FogDepth ("Fog Depth", Float) = 76
        _FogPow ("Fog Power", Float) = 1.3
        _FogAmount ("Fog Amount", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float dist : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Intensity; float4 _Haze; float _FogDepth; float _FogPow; float _FogAmount;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(w);
                o.color = v.color;
                o.dist = w.z - _WorldSpaceCameraPos.z;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float a = saturate(1.0 - i.dist / _FogDepth);
                float f = (1.0 - pow(a, _FogPow)) * _FogAmount;
                float3 c = i.color.rgb * _Tint.rgb * _Intensity;
                return half4(lerp(c, _Haze.rgb, f), 1);
            }
            ENDHLSL
        }
    }
}
