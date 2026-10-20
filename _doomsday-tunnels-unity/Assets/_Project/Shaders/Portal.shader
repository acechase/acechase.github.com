// The exit, drawn on a quad across the end of the tunnel, in one of several styles (_Style):
//   0 iris: blades that kaleidoscope open onto a vortex of grid rings and spokes
//   1 membrane: a honeycomb energy curtain that dissolves from the centre outward
//   2 doors: hangar doors that slide apart on a white-hot tunnel of light
//   3 hyperspace: streaks of light racing out of the centre
// _Open runs 0 (shut) to 1. _Flow is distance flown, so motion keeps step with the tunnel's own ribs.
Shader "Doomsday/Portal"
{
    Properties
    {
        _Color ("Line colour", Color) = (0.2,0.85,1,1)
        _Bright ("Bright colour", Color) = (1,1,1,1)
        _Open ("Open", Range(0,1)) = 0
        _Spin ("Spin", Float) = 0
        _Flow ("Flow", Float) = 0
        _Blades ("Blades", Float) = 8
        _Style ("Style", Float) = 0
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
                float4 _Color; float4 _Bright; float _Open; float _Spin; float _Flow; float _Blades; float _Style;
            CBUFFER_END
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; return o; }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            // how far inside its hexagonal cell a point is: 0 on the cell's edge, about 0.5 at its centre
            float hexCell(float2 p, float s)
            {
                float2 q = p / s; float2 rr = float2(1, 1.73);
                float2 a = fmod(abs(q), rr) - rr * 0.5, b = fmod(abs(q) - rr * 0.5, rr) - rr * 0.5;
                float2 c = dot(a, a) < dot(b, b) ? a : b; c = abs(c);
                return 0.5 - max(dot(c, normalize(float2(1, 1.73))), c.x);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;                 // -1..1 across the opening
                float r = length(p), a = atan2(p.y, p.x);
                float open = smoothstep(0.0, 1.0, _Open);
                float lr = log(max(r, 0.02));
                float3 col; float alpha = 1.0;
                if (_Style < 0.5)
                {   // iris and vortex
                    float edge = open * 1.6 * (1.0 + 0.07 * sin(_Blades * a - _Spin * 3.0));
                    float inside = smoothstep(edge, edge - 0.03, r);
                    float rings = smoothstep(0.40, 0.47, abs(frac(lr * 2.2 - _Flow) - 0.5));
                    float spokes = smoothstep(0.43, 0.49, abs(frac(a / 6.2831853 * 12.0) - 0.5));
                    float grid = max(rings, spokes) * smoothstep(0.0, 0.25, r);
                    float core = smoothstep(0.3, 0.0, r);
                    float3 vortex = _Color.rgb * (0.12 + grid * 1.5) + _Bright.rgb * core * 0.7;
                    float seam = smoothstep(0.46, 0.5, abs(frac((a + _Spin) / 6.2831853 * _Blades) - 0.5));
                    float rim = smoothstep(0.06, 0.0, abs(r - edge));
                    float3 blade = _Color.rgb * 0.12 + _Color.rgb * seam * 0.6 + _Bright.rgb * rim * 1.5;
                    col = lerp(blade, vortex, inside);
                }
                else if (_Style < 1.5)
                {   // energy membrane: honeycomb cells, a pulse running outward, cells winking out from the centre as it opens
                    float cell = hexCell(p, 0.16);
                    float wire = smoothstep(0.0, 0.06, cell);             // 0 on the cell walls
                    float pulse = 0.5 + 0.5 * sin(r * 14.0 - _Flow * 1.5);
                    float hole = open * 1.5;
                    float gone = smoothstep(hole - 0.08, hole + 0.02, r + 0.1 * hash(floor(p / 0.16)));
                    float rim = smoothstep(0.12, 0.0, abs(r - hole)) * step(0.01, open);
                    float3 mem = _Color.rgb * (0.3 + 0.4 * pulse) * wire + _Bright.rgb * (1.0 - wire) * 1.1;
                    col = mem * gone + _Bright.rgb * rim * 1.6;
                    alpha = saturate(0.8 * gone + rim);
                }
                else if (_Style < 2.5)
                {   // hangar doors: two leaves sliding apart, a hazard band along each edge, light and streaks behind
                    float gap = open * 1.1;
                    float leaf = step(gap, abs(p.x));
                    float lx = abs(p.x) - gap;
                    float band = step(lx, 0.16);
                    float stripe = step(0.5, frac((p.y - lx) * 4.0));
                    float panel = smoothstep(0.02, 0.0, abs(frac(p.y * 2.5 + 0.5) - 0.5) - 0.47) + smoothstep(0.02, 0.0, abs(frac(lx * 3.0) - 0.5) - 0.47);
                    float3 door = _Color.rgb * 0.16 + _Color.rgb * panel * 0.5 + lerp(_Bright.rgb * 0.9, _Color.rgb * 0.25, stripe) * band;
                    door += _Bright.rgb * smoothstep(0.04, 0.0, lx) * 1.2;
                    float streak = smoothstep(0.42, 0.5, abs(frac(a / 6.2831853 * 28.0 + hash(float2(floor(a * 4.4), 1.0)) * 0.3) - 0.5)) * smoothstep(0.05, 0.6, r);
                    float3 light = _Bright.rgb * (0.9 + streak * 0.8) * smoothstep(1.3, 0.2, r) + _Color.rgb * 0.3;
                    col = lerp(light, door, leaf);
                }
                else
                {   // hyperspace: a dark field, streaks racing out of the centre, longer and more of them as it opens
                    float n = 36.0;
                    float lane = floor(a / 6.2831853 * n + 0.5);
                    float h = hash(float2(lane, 3.0)), h2 = hash(float2(lane, 7.0));
                    float along = frac(lr * 0.9 - _Flow * 0.6 + h);
                    float len = 0.25 + 0.6 * open;
                    float on = smoothstep(0.0, 0.05, along) * smoothstep(len, len - 0.15, along) * step(h2, 0.25 + 0.7 * open);
                    float width = smoothstep(0.45, 0.5, abs(frac(a / 6.2831853 * n + 0.5) - 0.5));
                    float core = smoothstep(0.45, 0.0, r) * open;
                    col = _Color.rgb * 0.08 + lerp(_Color.rgb, _Bright.rgb, 0.6) * on * width * (1.2 + 0.5 * open) + _Bright.rgb * core * 1.2;
                }
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
