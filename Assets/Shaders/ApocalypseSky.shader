Shader "Custom/ApocalypseSky"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.2, 0.1, 0.05, 1)
        _HorizonColor ("Horizon Color", Color) = (0.3, 0.15, 0.05, 1)
        _BottomColor ("Bottom Color", Color) = (0.05, 0.05, 0.05, 1)
        
        _SmogColor ("Smog Color", Color) = (0.4, 0.3, 0.1, 0.5)
        _SmogSpeed ("Smog Speed", Float) = 0.5
        _SmogDensity ("Smog Density", Range(0, 10)) = 4.0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            fixed4 _SmogColor;
            float _SmogSpeed;
            float _SmogDensity;

            // Simple pseudo-random noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                            lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * noise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.texcoord);
                float gradient = dir.y;

                // Base Sky Gradient
                fixed4 skyColor = lerp(_HorizonColor, _TopColor, clamp(gradient * 2.0, 0.0, 1.0));
                skyColor = lerp(skyColor, _BottomColor, clamp(-gradient * 2.0, 0.0, 1.0));

                // Smog Layer (Procedural Noise)
                float2 uv = i.texcoord.xz / (i.texcoord.y + 0.2); // Project to "ceiling"
                float time = _Time.y * _SmogSpeed;
                float n = fbm(uv * _SmogDensity + float2(time * 0.1, time * 0.05));
                
                // Mask smog to horizon and up
                float smogMask = smoothstep(0.0, 0.5, gradient); // Only above horizon
                fixed4 smog = _SmogColor * n * smogMask;

                return skyColor + smog;
            }
            ENDCG
        }
    }
}
