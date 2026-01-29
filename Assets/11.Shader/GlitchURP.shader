Shader "Custom/GlitchURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlitchPower ("Glitch Power", Range(0, 1)) = 0.5
        _NoiseSpeed ("Noise Speed", Float) = 20.0
        _BlockSize ("Block Size", Float) = 10.0
        _Color ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "GlitchPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex;
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _GlitchPower;
                float _NoiseSpeed;
                float _BlockSize;
                float4 _Color;
            CBUFFER_END

            // Random Noise Function
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // [Vertex Jitter] GlitchPower가 높을수록 버텍스가 파르르 떨림
                float time = _Time.y * _NoiseSpeed;
                float noiseVal = random(float2(time, input.positionOS.y));
                
                // 간헐적으로 강하게 튀는 Jitter
                float jitter = step(0.8, noiseVal) * (noiseVal - 0.5) * _GlitchPower * 0.2;
                input.positionOS.x += jitter;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y * _NoiseSpeed;

                // [1. Scanline Distortion] UV를 가로로 찢음
                float scanLineNoise = random(float2(floor(uv.y * 50.0), time));
                float scanLineTrigger = step(0.98 - _GlitchPower * 0.5, scanLineNoise);
                float uvShift = (scanLineNoise - 0.5) * _GlitchPower * 0.5;
                uv.x += scanLineTrigger * uvShift;

                // [2. Base Color]
                half4 baseColor = tex2D(_MainTex, uv) * _Color;

                // [3. Block Glitch] 단색 오브젝트를 위해 색상을 직접 덮어씌움
                float2 blockUV = floor(uv * _BlockSize);
                float blockNoise = random(blockUV + float2(time, time)); // 0~1

                // 글리치 강도에 따라 블록 생성 확률 증가
                float glitchThreshold = 1.0 - (_GlitchPower * 0.7); 
                
                if (blockNoise > glitchThreshold)
                {
                    // 랜덤으로 색상 결정 (시안 / 레드 / 화이트 / 블랙)
                    float colorType = frac(blockNoise * 123.45);
                    
                    if (colorType < 0.33) baseColor.rgb = float3(0, 1, 1); // Cyan
                    else if (colorType < 0.66) baseColor.rgb = float3(1, 0, 0); // Red
                    else baseColor.rgb = float3(1, 1, 1); // White
                    
                    // 블록 부분은 아주 밝게 처리 (HDR 효과)
                    baseColor.rgb *= 1.5;
                }

                // [4. RGB Split] (약하게 적용)
                float split = _GlitchPower * 0.02 * scanLineTrigger;
                half4 texR = tex2D(_MainTex, uv + float2(split, 0));
                half4 texB = tex2D(_MainTex, uv - float2(split, 0));
                
                // 기존 색상에 미세하게 RGB 틴트 추가
                if (scanLineTrigger > 0.5)
                {
                    baseColor.r = max(baseColor.r, texR.r);
                    baseColor.b = max(baseColor.b, texB.b);
                }

                return baseColor;
            }
            ENDHLSL
        }
    }
}
