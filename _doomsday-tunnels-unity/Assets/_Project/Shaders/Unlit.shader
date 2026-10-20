// Flat colour, no scene lighting, faded toward the sector's haze colour with distance down the tunnel, exactly as the
// web game fades everything. Vertex colour times tint times intensity; intensity above 1 is what bloom picks up.
// On top of that, up to eight of the game's own point lights (shots, explosions, the ship's engine, the token) are
// added per pixel: a cheap, stylised lighting pass that keeps the flat look and costs nothing on a phone.
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
        _Lit ("Takes the game's lights", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float3 world : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Intensity; float4 _Haze; float _FogDepth; float _FogPow; float _FogAmount; float _Lit;
            CBUFFER_END
            #define DL_MAX 8
            float4 _DLPos[DL_MAX];      // xyz position, w radius
            float4 _DLCol[DL_MAX];      // rgb colour, a intensity
            int _DLCount;
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                o.color = v.color;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 c = i.color.rgb * _Tint.rgb * _Intensity;
                // The game's lights: a soft falloff that lifts the surface toward the light's colour, brighter the closer it is.
                float3 lit = 0;
                [loop] for (int k = 0; k < _DLCount; k++)
                {
                    float d = distance(i.world, _DLPos[k].xyz);
                    float a = saturate(1.0 - d / _DLPos[k].w); a *= a;
                    lit += _DLCol[k].rgb * (a * _DLCol[k].a);
                }
                // brighten the surface in its own colour, with just a hint of the light's
                float lum = dot(lit, float3(0.3, 0.59, 0.11));
                c = c * (1.0 + 1.6 * lum * _Lit) + lit * (0.12 * _Lit);
                float dist = i.world.z - _WorldSpaceCameraPos.z;
                float f = (1.0 - pow(saturate(1.0 - dist / _FogDepth), _FogPow)) * _FogAmount;
                return half4(lerp(c, _Haze.rgb, f), 1);
            }
            ENDHLSL
        }
    }
}
