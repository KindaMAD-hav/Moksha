Shader "UI/ShatteredGlass"
{
    Properties
    {
        _MainTex ("Screenshot", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (1,1,1,0.3)
        _EdgeWidth ("Edge Width", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _EdgeColor;
            float _EdgeWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Add subtle edge highlight
                float2 uvDist = min(i.uv, 1.0 - i.uv);
                float edge = smoothstep(0, _EdgeWidth, min(uvDist.x, uvDist.y));
                col.rgb = lerp(_EdgeColor.rgb, col.rgb, edge);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Unlit/Texture"
}
