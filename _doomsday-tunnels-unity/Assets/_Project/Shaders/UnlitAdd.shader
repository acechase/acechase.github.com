// Additive glow for sparks, shots and halos: colour times alpha, added onto whatever is behind it.
Shader "Doomsday/UnlitAdd"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Intensity;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 c = i.color.rgb * _Tint.rgb * _Intensity * i.color.a * _Tint.a;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
