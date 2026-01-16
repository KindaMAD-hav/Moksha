Shader "Moksha/FloorRadialDissolveURP"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _EdgeColor ("Edge Color", Color) = (1,0.5,0,1)

        _DissolveWidth ("Dissolve Width", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            AlphaToMask On

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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _BaseMap;
            sampler2D _NoiseTex;

            float4 _BaseMap_ST;
            float4 _EdgeColor;

            float3 _CollapseCenter;
            float _CollapseRadius;
            float _DissolveWidth;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Base color
                half4 baseCol = tex2D(_BaseMap, i.uv);

                // World-space radial distance (XZ only)
                float2 worldXZ = i.worldPos.xz;
                float2 centerXZ = _CollapseCenter.xz;

                float dist = distance(worldXZ, centerXZ);

                // Dissolve mask
                float dissolve = (dist - _CollapseRadius) / _DissolveWidth;
                dissolve = saturate(dissolve);

                // Noise
                float2 noiseUV = worldXZ * 0.25;
                float noise = tex2D(_NoiseTex, noiseUV).r;
                dissolve += (noise - 0.5) * 0.5;
                dissolve = saturate(dissolve);

                // Alpha clip
                clip(dissolve - 0.01);

                // Edge glow
                float edge = 1.0 - dissolve;
                baseCol.rgb += _EdgeColor.rgb * edge;

                return baseCol;
            }
            ENDHLSL
        }
    }
}
