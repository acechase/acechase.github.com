// The exit portal: an iris of blades that kaleidoscopes open as the ship nears, revealing a vortex of grid lines
// rushing inward in the sector's colour. Drawn on a quad across the end of the tunnel. _Open runs 0 (shut) to 1.
Shader "Doomsday/Portal"
{
    Properties
    {
        _Color ("Line colour", Color) = (0.2,0.85,1,1)
        _Bright ("Bright colour", Color) = (1,1,1,1)
        _Open ("Open", Range(0,1)) = 0
        _Spin ("Spin", Float) = 0
        _Blades ("Blades", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float4 _Bright; float _Open; float _Spin; float _Blades;
            CBUFFER_END
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; return o; }
            half4 frag(Varyings i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;                 // -1..1 across the quad; the tunnel's opening is the unit disc
                float r = length(p), a = atan2(p.y, p.x);
                float t = _Time.y;
                // the iris: N blades, their inner edge a scalloped circle that grows with _Open and turns as it goes
                float open = smoothstep(0.0, 1.0, _Open);
                float edge = open * 1.6 * (1.0 + 0.07 * sin(_Blades * a - _Spin * 3.0));
                float inside = smoothstep(edge, edge - 0.03, r);
                // the vortex: rings rushing inward and spokes turning, on a dark field that glows toward the centre
                float lr = log(max(r, 0.02));
                float rings = smoothstep(0.40, 0.47, abs(frac(lr * 2.2 - t * 1.4) - 0.5));          // thin lines where the sawtooth wraps
                float spokes = smoothstep(0.43, 0.49, abs(frac(a / 6.2831853 * 12.0 + t * 0.25 + lr * 0.6) - 0.5));
                float grid = max(rings, spokes) * smoothstep(0.0, 0.25, r);
                float core = smoothstep(0.3, 0.0, r);
                float3 vortex = _Color.rgb * (0.12 + grid * 1.5) + _Bright.rgb * core * 0.7;
                // the blades: dark metal, a bright rim along the inner edge, seams between blades
                float seam = smoothstep(0.46, 0.5, abs(frac((a + _Spin) / 6.2831853 * _Blades) - 0.5));
                float rim = smoothstep(0.06, 0.0, abs(r - edge));
                float3 blade = _Color.rgb * 0.12 + _Color.rgb * seam * 0.6 + _Bright.rgb * rim * 1.5;
                float3 col = lerp(blade, vortex, inside);
                float alpha = 1.0;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
