Shader "CardShaderGraph_Fixed"
{
    Properties
    {
        [NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
        [KeywordEnum(Regular, Polychrome, Foil, Negative)]_EDITION("Edition", Float) = 0
        _Rotation("Rotation", Vector) = (0, 0, 0, 0)
        _poly_power("poly power", Float) = 0.2
        _poly_frequency("poly frequency", Float) = 1
        _poly_brightness("poly brightness", Float) = 0.7
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

        // Added to make it work with Unity Masks:
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent"
            // DisableBatching: <None>
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalSpriteUnlitSubTarget"
        }
        
        // Added to make it work with Unity Masks:
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
    
        ColorMask [_ColorMask]

        Pass
        {
            Name "Sprite Unlit"
            Tags
            {
                "LightMode" = "Universal2D"
            }
        
        // Render State
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma shader_feature_local _EDITION_REGULAR _EDITION_POLYCHROME _EDITION_FOIL _EDITION_NEGATIVE
        
        #if defined(_EDITION_REGULAR)
            #define KEYWORD_PERMUTATION_0
        #elif defined(_EDITION_POLYCHROME)
            #define KEYWORD_PERMUTATION_1
        #elif defined(_EDITION_FOIL)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_COLOR
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_COLOR
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SPRITEUNLIT
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 color : COLOR;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 color;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 color : INTERP1;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP2;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.color.xyzw = input.color;
            output.positionWS.xyz = input.positionWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.color = input.color.xyzw;
            output.positionWS = input.positionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _poly_frequency;
        float _poly_power;
        float2 _Rotation;
        float _poly_brightness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Twirl_float(float2 UV, float2 Center, float Strength, float2 Offset, out float2 Out)
        {
            float2 delta = UV - Center;
            float angle = Strength * length(delta);
            float x = cos(angle) * delta.x - sin(angle) * delta.y;
            float y = sin(angle) * delta.x + cos(angle) * delta.y;
            Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_ChannelMixer_float (float3 In, float3 Red, float3 Green, float3 Blue, out float3 Out)
        {
        Out = float3(dot(In, Red), dot(In, Green), dot(In, Blue));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
        {
            float midpoint = pow(0.5, 2.2);
            Out =  (In - midpoint) * Contrast + midpoint;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Blend_Difference_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
        {
            Out = abs(Blend - Base);
            Out = lerp(Base, Out, Opacity);
        }
        
        struct Bindings_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float
        {
        half4 uv0;
        };
        
        void SG_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float(UnityTexture2D _CardTexture, float2 _Rotation, float _Power, float _Frequency, float _Birghtness, Bindings_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float IN, out float3 FinalResult_1)
        {
        UnityTexture2D _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.tex, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.samplerstate, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_R_4_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.r;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_G_5_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.g;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_B_6_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.b;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_A_7_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.a;
        float _Property_8e636d233dae42579d4f2349d5919086_Out_0_Float = _Birghtness;
        float4 _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4;
        Unity_Multiply_float4_float4(_SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4, (_Property_8e636d233dae42579d4f2349d5919086_Out_0_Float.xxxx), _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4);
        float _Float_3b860df3de9a40058b2bc2e8522c6497_Out_0_Float = float(0.5);
        float2 _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2 = _Rotation;
        float _Split_599fdd4022ff42c5a381a691649013f8_R_1_Float = _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2[0];
        float _Split_599fdd4022ff42c5a381a691649013f8_G_2_Float = _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2[1];
        float _Split_599fdd4022ff42c5a381a691649013f8_B_3_Float = 0;
        float _Split_599fdd4022ff42c5a381a691649013f8_A_4_Float = 0;
        float _Multiply_3e4f684074ec42e3a09e24e403b2da72_Out_2_Float;
        Unity_Multiply_float_float(_Split_599fdd4022ff42c5a381a691649013f8_G_2_Float, 0.45, _Multiply_3e4f684074ec42e3a09e24e403b2da72_Out_2_Float);
        float _Add_be5f229716fb4ca5b6c6b0026cefde67_Out_2_Float;
        Unity_Add_float(_Float_3b860df3de9a40058b2bc2e8522c6497_Out_0_Float, _Multiply_3e4f684074ec42e3a09e24e403b2da72_Out_2_Float, _Add_be5f229716fb4ca5b6c6b0026cefde67_Out_2_Float);
        float2 _Vector2_83a508db49764e3e80ffba284e64310c_Out_0_Vector2 = float2(_Add_be5f229716fb4ca5b6c6b0026cefde67_Out_2_Float, float(0.2));
        float2 _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2;
        Unity_Twirl_float(IN.uv0.xy, _Vector2_83a508db49764e3e80ffba284e64310c_Out_0_Vector2, float(4), _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2, _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2);
        float _Property_9482d3baf7cc4ecaa3b4389bbc20917a_Out_0_Float = _Frequency;
        float2 _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2, (_Property_9482d3baf7cc4ecaa3b4389bbc20917a_Out_0_Float.xx), _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2);
        float3 _Hue_a01cf3936dd34b3da403c47cc1af7b93_Out_2_Vector3;
        Unity_Hue_Normalized_float((_Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4.xyz), (_Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2).x, _Hue_a01cf3936dd34b3da403c47cc1af7b93_Out_2_Vector3);
        float4 Color_d87a525a77294c88a5ef65a8251a4bce = IsGammaSpace() ? float4(1, 0.07568422, 0, 0) : float4(SRGBToLinear(float3(1, 0.07568422, 0)), 0);
        float3 _Hue_82cfaca1d68844ffa6340c2fa72bf46d_Out_2_Vector3;
        Unity_Hue_Normalized_float((Color_d87a525a77294c88a5ef65a8251a4bce.xyz), (_Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2).x, _Hue_82cfaca1d68844ffa6340c2fa72bf46d_Out_2_Vector3);
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Out_1_Vector3;
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Red = float3 (0.81, -0.25, 0.27);
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Green = float3 (-0.12, 0.65, 0);
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Blue = float3 (0, 0, 1);
        Unity_ChannelMixer_float(_Hue_82cfaca1d68844ffa6340c2fa72bf46d_Out_2_Vector3, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Red, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Green, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Blue, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Out_1_Vector3);
        float _Property_89304fcc6dee49c1b7f63aa90e8344d0_Out_0_Float = _Power;
        float3 _Multiply_f79208242d3d4d1cb029230b78dcffb7_Out_2_Vector3;
        Unity_Multiply_float3_float3(_ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Out_1_Vector3, (_Property_89304fcc6dee49c1b7f63aa90e8344d0_Out_0_Float.xxx), _Multiply_f79208242d3d4d1cb029230b78dcffb7_Out_2_Vector3);
        float3 _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector3;
        Unity_Add_float3(_Hue_a01cf3936dd34b3da403c47cc1af7b93_Out_2_Vector3, _Multiply_f79208242d3d4d1cb029230b78dcffb7_Out_2_Vector3, _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector3);
        float3 _Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3;
        Unity_Contrast_float(_Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector3, float(1), _Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3);
        float _Split_d526804243fb4eee989df3a780b5b0b3_R_1_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[0];
        float _Split_d526804243fb4eee989df3a780b5b0b3_G_2_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[1];
        float _Split_d526804243fb4eee989df3a780b5b0b3_B_3_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[2];
        float _Split_d526804243fb4eee989df3a780b5b0b3_A_4_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[3];
        float _Multiply_1492d1fdb9ad4c719091076a30167239_Out_2_Float;
        Unity_Multiply_float_float(_Split_d526804243fb4eee989df3a780b5b0b3_G_2_Float, 2, _Multiply_1492d1fdb9ad4c719091076a30167239_Out_2_Float);
        float _OneMinus_8b2b04e6294341bea3826ddc71985aea_Out_1_Float;
        Unity_OneMinus_float(_Multiply_1492d1fdb9ad4c719091076a30167239_Out_2_Float, _OneMinus_8b2b04e6294341bea3826ddc71985aea_Out_1_Float);
        float3 _Multiply_2954b87e98434eceb5cfff96fcfe2599_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3, (_OneMinus_8b2b04e6294341bea3826ddc71985aea_Out_1_Float.xxx), _Multiply_2954b87e98434eceb5cfff96fcfe2599_Out_2_Vector3);
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Out_1_Vector3;
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Red = float3 (0.81, -0.25, 0.27);
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Green = float3 (-0.12, 0.65, 0);
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Blue = float3 (0, 0, 1);
        Unity_ChannelMixer_float(_Multiply_2954b87e98434eceb5cfff96fcfe2599_Out_2_Vector3, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Red, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Green, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Blue, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Out_1_Vector3);
        float3 _Blend_d14c5bda98d84c3fb1284902bede27e4_Out_2_Vector3;
        Unity_Blend_Difference_float3(_Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Out_1_Vector3, _Blend_d14c5bda98d84c3fb1284902bede27e4_Out_2_Vector3, float(0.2));
        FinalResult_1 = _Blend_d14c5bda98d84c3fb1284902bede27e4_Out_2_Vector3;
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        struct Bindings_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float
        {
        half4 uv0;
        };
        
        void SG_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float(UnityTexture2D _CardTexture, float2 _Rotation, float _Power, float _Frequency, float _Birghtness, Bindings_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float IN, out float3 FinalResult_1)
        {
        UnityTexture2D _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.tex, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.samplerstate, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_R_4_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.r;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_G_5_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.g;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_B_6_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.b;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_A_7_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.a;
        float _Property_8e636d233dae42579d4f2349d5919086_Out_0_Float = _Birghtness;
        float4 _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4;
        Unity_Multiply_float4_float4(_SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4, (_Property_8e636d233dae42579d4f2349d5919086_Out_0_Float.xxxx), _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4);
        float _Property_d9facea32b3141868e44c4d12ed2030f_Out_0_Float = _Frequency;
        float2 _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2 = _Rotation;
        float2 _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2;
        Unity_Twirl_float(IN.uv0.xy, float2 (0.5, 0.5), _Property_d9facea32b3141868e44c4d12ed2030f_Out_0_Float, _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2, _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2);
        float _Split_14f33f7535ba4031b5b9deb5435be679_R_1_Float = _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2[0];
        float _Split_14f33f7535ba4031b5b9deb5435be679_G_2_Float = _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2[1];
        float _Split_14f33f7535ba4031b5b9deb5435be679_B_3_Float = 0;
        float _Split_14f33f7535ba4031b5b9deb5435be679_A_4_Float = 0;
        float _Sine_c38cd02f50344dbaaea4e29b8f9d48a1_Out_1_Float;
        Unity_Sine_float(_Split_14f33f7535ba4031b5b9deb5435be679_R_1_Float, _Sine_c38cd02f50344dbaaea4e29b8f9d48a1_Out_1_Float);
        float _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Float;
        Unity_Multiply_float_float(_Sine_c38cd02f50344dbaaea4e29b8f9d48a1_Out_1_Float, 1, _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Float);
        float4 Color_d87a525a77294c88a5ef65a8251a4bce = IsGammaSpace() ? float4(0.6556604, 0.7312801, 1, 0) : float4(SRGBToLinear(float3(0.6556604, 0.7312801, 1)), 0);
        float4 _Multiply_e7e5ff26093e495189cfb36b35cc9355_Out_2_Vector4;
        Unity_Multiply_float4_float4((_Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Float.xxxx), Color_d87a525a77294c88a5ef65a8251a4bce, _Multiply_e7e5ff26093e495189cfb36b35cc9355_Out_2_Vector4);
        float4 _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector4;
        Unity_Add_float4(_Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4, _Multiply_e7e5ff26093e495189cfb36b35cc9355_Out_2_Vector4, _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector4);
        float4 _Multiply_fcbfc116f1944d249bacf2a15621a1b8_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector4, Color_d87a525a77294c88a5ef65a8251a4bce, _Multiply_fcbfc116f1944d249bacf2a15621a1b8_Out_2_Vector4);
        FinalResult_1 = (_Multiply_fcbfc116f1944d249bacf2a15621a1b8_Out_2_Vector4.xyz);
        }
        
        void Unity_InvertColors_float4(float4 In, float4 InvertColors, out float4 Out)
        {
        Out = abs(InvertColors - In);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
        {
        Hash_Tchou_2_2_float(UV, UV);
        return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
        }
        
        void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
        {
        float2 g = floor(UV * CellDensity);
        float2 f = frac(UV * CellDensity);
        float t = 8.0;
        float3 res = float3(8.0, 0.0, 0.0);
        for (int y = -1; y <= 1; y++)
        {
        for (int x = -1; x <= 1; x++)
        {
        float2 lattice = float2(x, y);
        float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
        float d = distance(lattice + offset, f);
        if (d < res.x)
        {
        res = float3(d, offset.x, offset.y);
        Out = res.x;
        Cells = res.y;
        }
        }
        }
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        struct Bindings_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float
        {
        half4 uv0;
        };
        
        void SG_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float(UnityTexture2D _CardTexture, float2 _Rotation, float _Power, float _Frequency, float _Birghtness, Bindings_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float IN, out float3 FinalResult_1)
        {
        UnityTexture2D _Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D.tex, _Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D.samplerstate, _Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_R_4_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.r;
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_G_5_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.g;
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_B_6_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.b;
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_A_7_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.a;
        float4 _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4;
        float4 _InvertColors_af90d499b00a4b99bbee5f7f6c192704_InvertColors = float4 (1, 1, 1, 0);
        Unity_InvertColors_float4(_SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4, _InvertColors_af90d499b00a4b99bbee5f7f6c192704_InvertColors, _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4);
        float3 _Contrast_02b35aa32aa448b9b1cd267a88352da2_Out_2_Vector3;
        Unity_Contrast_float((_InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4.xyz), float(0.7), _Contrast_02b35aa32aa448b9b1cd267a88352da2_Out_2_Vector3);
        float3 _Saturation_034c925dc74947b4bc10d97ce4a3c8c8_Out_2_Vector3;
        Unity_Saturation_float(_Contrast_02b35aa32aa448b9b1cd267a88352da2_Out_2_Vector3, float(2), _Saturation_034c925dc74947b4bc10d97ce4a3c8c8_Out_2_Vector3);
        float4 Color_1fbddc4b70e149988f2ebdb019993a46 = IsGammaSpace() ? float4(0.7783019, 0.8308094, 1, 1) : float4(SRGBToLinear(float3(0.7783019, 0.8308094, 1)), 1);
        float3 _Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Saturation_034c925dc74947b4bc10d97ce4a3c8c8_Out_2_Vector3, (Color_1fbddc4b70e149988f2ebdb019993a46.xyz), _Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3);
        float2 _Property_74934bffab6b41fd9e2365c4f1603617_Out_0_Vector2 = _Rotation;
        float2 _Twirl_acefa8223ea34f708a108bdc7d8ed57d_Out_4_Vector2;
        Unity_Twirl_float(IN.uv0.xy, float2 (0.5, 0.5), float(0.68), _Property_74934bffab6b41fd9e2365c4f1603617_Out_0_Vector2, _Twirl_acefa8223ea34f708a108bdc7d8ed57d_Out_4_Vector2);
        float2 _TilingAndOffset_c8f307cbd552486f942416e4cf8d0426_Out_3_Vector2;
        Unity_TilingAndOffset_float(_Twirl_acefa8223ea34f708a108bdc7d8ed57d_Out_4_Vector2, float2 (2.79, 1), float2 (2.29, 0), _TilingAndOffset_c8f307cbd552486f942416e4cf8d0426_Out_3_Vector2);
        float _Voronoi_5236437f85754c03b93496da5dcc2f4f_Out_3_Float;
        float _Voronoi_5236437f85754c03b93496da5dcc2f4f_Cells_4_Float;
        Unity_Voronoi_Deterministic_float(_TilingAndOffset_c8f307cbd552486f942416e4cf8d0426_Out_3_Vector2, float(0), float(0.28), _Voronoi_5236437f85754c03b93496da5dcc2f4f_Out_3_Float, _Voronoi_5236437f85754c03b93496da5dcc2f4f_Cells_4_Float);
        float _Power_68601748f80b49aea4791103a8461dca_Out_2_Float;
        Unity_Power_float(_Voronoi_5236437f85754c03b93496da5dcc2f4f_Out_3_Float, float(4), _Power_68601748f80b49aea4791103a8461dca_Out_2_Float);
        UnityTexture2D _Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D.tex, _Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D.samplerstate, _Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_R_4_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.r;
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_G_5_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.g;
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_B_6_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.b;
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_A_7_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.a;
        float _Multiply_b35ec2f72e4f48b1aff61d4e2fa34446_Out_2_Float;
        Unity_Multiply_float_float(_Power_68601748f80b49aea4791103a8461dca_Out_2_Float, _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_R_4_Float, _Multiply_b35ec2f72e4f48b1aff61d4e2fa34446_Out_2_Float);
        float _Multiply_77f73e3af0d54291a642525ac2d405b0_Out_2_Float;
        Unity_Multiply_float_float(_Multiply_b35ec2f72e4f48b1aff61d4e2fa34446_Out_2_Float, 0.3, _Multiply_77f73e3af0d54291a642525ac2d405b0_Out_2_Float);
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_R_1_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[0];
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_G_2_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[1];
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_B_3_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[2];
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_A_4_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[3];
        float _Power_29bd1c5c32324264af18587c2a396daf_Out_2_Float;
        Unity_Power_float(_Split_59fcdca8a05c4d1a891c0cd9b5484991_G_2_Float, float(0.5), _Power_29bd1c5c32324264af18587c2a396daf_Out_2_Float);
        float _Smoothstep_fb344812fa7745d6bf53061d6b310c23_Out_3_Float;
        Unity_Smoothstep_float(float(0.04), float(0.14), _Power_68601748f80b49aea4791103a8461dca_Out_2_Float, _Smoothstep_fb344812fa7745d6bf53061d6b310c23_Out_3_Float);
        float _Multiply_7da3a538962e4a56867592ae2a7369fc_Out_2_Float;
        Unity_Multiply_float_float(_Power_29bd1c5c32324264af18587c2a396daf_Out_2_Float, _Smoothstep_fb344812fa7745d6bf53061d6b310c23_Out_3_Float, _Multiply_7da3a538962e4a56867592ae2a7369fc_Out_2_Float);
        float _Multiply_d0a72a3dd3004c488da8214efe442bd6_Out_2_Float;
        Unity_Multiply_float_float(_Multiply_7da3a538962e4a56867592ae2a7369fc_Out_2_Float, 2, _Multiply_d0a72a3dd3004c488da8214efe442bd6_Out_2_Float);
        float _Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float;
        Unity_Add_float(_Multiply_77f73e3af0d54291a642525ac2d405b0_Out_2_Float, _Multiply_d0a72a3dd3004c488da8214efe442bd6_Out_2_Float, _Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float);
        float3 _Add_3341e561f7ee4f15ac0d10191172e1b3_Out_2_Vector3;
        Unity_Add_float3(_Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3, (_Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float.xxx), _Add_3341e561f7ee4f15ac0d10191172e1b3_Out_2_Vector3);
        float3 _Multiply_bad1e6d1f24f42fbae936111ce22defd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3, (_Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float.xxx), _Multiply_bad1e6d1f24f42fbae936111ce22defd_Out_2_Vector3);
        float3 _Saturation_64f668713ac84ecd9c86502d9a985941_Out_2_Vector3;
        Unity_Saturation_float(_Multiply_bad1e6d1f24f42fbae936111ce22defd_Out_2_Vector3, float(5), _Saturation_64f668713ac84ecd9c86502d9a985941_Out_2_Vector3);
        float3 _Add_11d62a32efc04eb08a23b4a50deb4802_Out_2_Vector3;
        Unity_Add_float3(_Add_3341e561f7ee4f15ac0d10191172e1b3_Out_2_Vector3, _Saturation_64f668713ac84ecd9c86502d9a985941_Out_2_Vector3, _Add_11d62a32efc04eb08a23b4a50deb4802_Out_2_Vector3);
        float3 _Contrast_0867fb2cf7f742bc8abdd975c30adc23_Out_2_Vector3;
        Unity_Contrast_float(_Add_11d62a32efc04eb08a23b4a50deb4802_Out_2_Vector3, float(1.1), _Contrast_0867fb2cf7f742bc8abdd975c30adc23_Out_2_Vector3);
        FinalResult_1 = _Contrast_0867fb2cf7f742bc8abdd975c30adc23_Out_2_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D.tex, _Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D.samplerstate, _Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_R_4_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.r;
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_G_5_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.g;
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_B_6_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.b;
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_A_7_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_b76623d8364b41dba8404414b7320ba3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_25cf5305c34344838a2d1df894a8a820_Out_0_Vector2 = _Rotation;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_e1d2a9307dbb487fb9b7c37d4ddfc79d_Out_0_Float = _poly_power;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_341716e1a79247f3a03b257d0af43f59_Out_0_Float = _poly_frequency;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_5bcb79b3470a45aba479092a9a87697b_Out_0_Float = _poly_brightness;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6;
            _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6.uv0 = IN.uv0;
            float3 _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6_FinalResult_1_Vector3;
            SG_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float(_Property_b76623d8364b41dba8404414b7320ba3_Out_0_Texture2D, _Property_25cf5305c34344838a2d1df894a8a820_Out_0_Vector2, _Property_e1d2a9307dbb487fb9b7c37d4ddfc79d_Out_0_Float, _Property_341716e1a79247f3a03b257d0af43f59_Out_0_Float, _Property_5bcb79b3470a45aba479092a9a87697b_Out_0_Float, _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6, _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6_FinalResult_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_cdc86a0cfaa04eea9208e082169953bc_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_7625f424b4454d5882d6af227cdea65f_Out_0_Vector2 = _Rotation;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc;
            _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc.uv0 = IN.uv0;
            float3 _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc_FinalResult_1_Vector3;
            SG_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float(_Property_cdc86a0cfaa04eea9208e082169953bc_Out_0_Texture2D, _Property_7625f424b4454d5882d6af227cdea65f_Out_0_Vector2, float(0.2), float(300), float(0.7), _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc, _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc_FinalResult_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_4623ad78d15242e2b1bd6af9bf910c67_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_639a0ec42a684ca58bf7765da16152a2_Out_0_Vector2 = _Rotation;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float _EditionNegative_26c00ba8abda475884c6c867f0ebafb6;
            _EditionNegative_26c00ba8abda475884c6c867f0ebafb6.uv0 = IN.uv0;
            float3 _EditionNegative_26c00ba8abda475884c6c867f0ebafb6_FinalResult_1_Vector3;
            SG_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float(_Property_4623ad78d15242e2b1bd6af9bf910c67_Out_0_Texture2D, _Property_639a0ec42a684ca58bf7765da16152a2_Out_0_Vector2, float(0.87), float(0.35), float(0.7), _EditionNegative_26c00ba8abda475884c6c867f0ebafb6, _EditionNegative_26c00ba8abda475884c6c867f0ebafb6_FinalResult_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(_EDITION_REGULAR)
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = (_SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.xyz);
            #elif defined(_EDITION_POLYCHROME)
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6_FinalResult_1_Vector3;
            #elif defined(_EDITION_FOIL)
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc_FinalResult_1_Vector3;
            #else
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = _EditionNegative_26c00ba8abda475884c6c867f0ebafb6_FinalResult_1_Vector3;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.tex, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.samplerstate, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_R_4_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.r;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_G_5_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.g;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_B_6_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.b;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.a;
            #endif
            surface.BaseColor = _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3;
            surface.Alpha = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteUnlitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        #pragma shader_feature_local _EDITION_REGULAR _EDITION_POLYCHROME _EDITION_FOIL _EDITION_NEGATIVE
        
        #if defined(_EDITION_REGULAR)
            #define KEYWORD_PERMUTATION_0
        #elif defined(_EDITION_POLYCHROME)
            #define KEYWORD_PERMUTATION_1
        #elif defined(_EDITION_FOIL)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _poly_frequency;
        float _poly_power;
        float2 _Rotation;
        float _poly_brightness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.tex, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.samplerstate, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_R_4_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.r;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_G_5_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.g;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_B_6_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.b;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.a;
            #endif
            surface.Alpha = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Back
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        #pragma shader_feature_local _EDITION_REGULAR _EDITION_POLYCHROME _EDITION_FOIL _EDITION_NEGATIVE
        
        #if defined(_EDITION_REGULAR)
            #define KEYWORD_PERMUTATION_0
        #elif defined(_EDITION_POLYCHROME)
            #define KEYWORD_PERMUTATION_1
        #elif defined(_EDITION_FOIL)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _poly_frequency;
        float _poly_power;
        float2 _Rotation;
        float _poly_brightness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.tex, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.samplerstate, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_R_4_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.r;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_G_5_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.g;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_B_6_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.b;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.a;
            #endif
            surface.Alpha = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Sprite Unlit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
        
        // Render State
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma shader_feature_local _EDITION_REGULAR _EDITION_POLYCHROME _EDITION_FOIL _EDITION_NEGATIVE
        
        #if defined(_EDITION_REGULAR)
            #define KEYWORD_PERMUTATION_0
        #elif defined(_EDITION_POLYCHROME)
            #define KEYWORD_PERMUTATION_1
        #elif defined(_EDITION_FOIL)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_COLOR
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_COLOR
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SPRITEFORWARD
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 color : COLOR;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 color;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 color : INTERP1;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP2;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.color.xyzw = input.color;
            output.positionWS.xyz = input.positionWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.color = input.color.xyzw;
            output.positionWS = input.positionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _poly_frequency;
        float _poly_power;
        float2 _Rotation;
        float _poly_brightness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Twirl_float(float2 UV, float2 Center, float Strength, float2 Offset, out float2 Out)
        {
            float2 delta = UV - Center;
            float angle = Strength * length(delta);
            float x = cos(angle) * delta.x - sin(angle) * delta.y;
            float y = sin(angle) * delta.x + cos(angle) * delta.y;
            Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Hue_Normalized_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_ChannelMixer_float (float3 In, float3 Red, float3 Green, float3 Blue, out float3 Out)
        {
        Out = float3(dot(In, Red), dot(In, Green), dot(In, Blue));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
        {
            float midpoint = pow(0.5, 2.2);
            Out =  (In - midpoint) * Contrast + midpoint;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Blend_Difference_float3(float3 Base, float3 Blend, out float3 Out, float Opacity)
        {
            Out = abs(Blend - Base);
            Out = lerp(Base, Out, Opacity);
        }
        
        struct Bindings_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float
        {
        half4 uv0;
        };
        
        void SG_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float(UnityTexture2D _CardTexture, float2 _Rotation, float _Power, float _Frequency, float _Birghtness, Bindings_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float IN, out float3 FinalResult_1)
        {
        UnityTexture2D _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.tex, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.samplerstate, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_R_4_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.r;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_G_5_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.g;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_B_6_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.b;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_A_7_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.a;
        float _Property_8e636d233dae42579d4f2349d5919086_Out_0_Float = _Birghtness;
        float4 _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4;
        Unity_Multiply_float4_float4(_SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4, (_Property_8e636d233dae42579d4f2349d5919086_Out_0_Float.xxxx), _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4);
        float _Float_3b860df3de9a40058b2bc2e8522c6497_Out_0_Float = float(0.5);
        float2 _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2 = _Rotation;
        float _Split_599fdd4022ff42c5a381a691649013f8_R_1_Float = _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2[0];
        float _Split_599fdd4022ff42c5a381a691649013f8_G_2_Float = _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2[1];
        float _Split_599fdd4022ff42c5a381a691649013f8_B_3_Float = 0;
        float _Split_599fdd4022ff42c5a381a691649013f8_A_4_Float = 0;
        float _Multiply_3e4f684074ec42e3a09e24e403b2da72_Out_2_Float;
        Unity_Multiply_float_float(_Split_599fdd4022ff42c5a381a691649013f8_G_2_Float, 0.45, _Multiply_3e4f684074ec42e3a09e24e403b2da72_Out_2_Float);
        float _Add_be5f229716fb4ca5b6c6b0026cefde67_Out_2_Float;
        Unity_Add_float(_Float_3b860df3de9a40058b2bc2e8522c6497_Out_0_Float, _Multiply_3e4f684074ec42e3a09e24e403b2da72_Out_2_Float, _Add_be5f229716fb4ca5b6c6b0026cefde67_Out_2_Float);
        float2 _Vector2_83a508db49764e3e80ffba284e64310c_Out_0_Vector2 = float2(_Add_be5f229716fb4ca5b6c6b0026cefde67_Out_2_Float, float(0.2));
        float2 _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2;
        Unity_Twirl_float(IN.uv0.xy, _Vector2_83a508db49764e3e80ffba284e64310c_Out_0_Vector2, float(4), _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2, _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2);
        float _Property_9482d3baf7cc4ecaa3b4389bbc20917a_Out_0_Float = _Frequency;
        float2 _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2, (_Property_9482d3baf7cc4ecaa3b4389bbc20917a_Out_0_Float.xx), _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2);
        float3 _Hue_a01cf3936dd34b3da403c47cc1af7b93_Out_2_Vector3;
        Unity_Hue_Normalized_float((_Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4.xyz), (_Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2).x, _Hue_a01cf3936dd34b3da403c47cc1af7b93_Out_2_Vector3);
        float4 Color_d87a525a77294c88a5ef65a8251a4bce = IsGammaSpace() ? float4(1, 0.07568422, 0, 0) : float4(SRGBToLinear(float3(1, 0.07568422, 0)), 0);
        float3 _Hue_82cfaca1d68844ffa6340c2fa72bf46d_Out_2_Vector3;
        Unity_Hue_Normalized_float((Color_d87a525a77294c88a5ef65a8251a4bce.xyz), (_Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Vector2).x, _Hue_82cfaca1d68844ffa6340c2fa72bf46d_Out_2_Vector3);
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Out_1_Vector3;
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Red = float3 (0.81, -0.25, 0.27);
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Green = float3 (-0.12, 0.65, 0);
        float3 _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Blue = float3 (0, 0, 1);
        Unity_ChannelMixer_float(_Hue_82cfaca1d68844ffa6340c2fa72bf46d_Out_2_Vector3, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Red, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Green, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Blue, _ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Out_1_Vector3);
        float _Property_89304fcc6dee49c1b7f63aa90e8344d0_Out_0_Float = _Power;
        float3 _Multiply_f79208242d3d4d1cb029230b78dcffb7_Out_2_Vector3;
        Unity_Multiply_float3_float3(_ChannelMixer_c9df3b54e24d46ba913ad6edbd703572_Out_1_Vector3, (_Property_89304fcc6dee49c1b7f63aa90e8344d0_Out_0_Float.xxx), _Multiply_f79208242d3d4d1cb029230b78dcffb7_Out_2_Vector3);
        float3 _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector3;
        Unity_Add_float3(_Hue_a01cf3936dd34b3da403c47cc1af7b93_Out_2_Vector3, _Multiply_f79208242d3d4d1cb029230b78dcffb7_Out_2_Vector3, _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector3);
        float3 _Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3;
        Unity_Contrast_float(_Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector3, float(1), _Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3);
        float _Split_d526804243fb4eee989df3a780b5b0b3_R_1_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[0];
        float _Split_d526804243fb4eee989df3a780b5b0b3_G_2_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[1];
        float _Split_d526804243fb4eee989df3a780b5b0b3_B_3_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[2];
        float _Split_d526804243fb4eee989df3a780b5b0b3_A_4_Float = _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4[3];
        float _Multiply_1492d1fdb9ad4c719091076a30167239_Out_2_Float;
        Unity_Multiply_float_float(_Split_d526804243fb4eee989df3a780b5b0b3_G_2_Float, 2, _Multiply_1492d1fdb9ad4c719091076a30167239_Out_2_Float);
        float _OneMinus_8b2b04e6294341bea3826ddc71985aea_Out_1_Float;
        Unity_OneMinus_float(_Multiply_1492d1fdb9ad4c719091076a30167239_Out_2_Float, _OneMinus_8b2b04e6294341bea3826ddc71985aea_Out_1_Float);
        float3 _Multiply_2954b87e98434eceb5cfff96fcfe2599_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3, (_OneMinus_8b2b04e6294341bea3826ddc71985aea_Out_1_Float.xxx), _Multiply_2954b87e98434eceb5cfff96fcfe2599_Out_2_Vector3);
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Out_1_Vector3;
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Red = float3 (0.81, -0.25, 0.27);
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Green = float3 (-0.12, 0.65, 0);
        float3 _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Blue = float3 (0, 0, 1);
        Unity_ChannelMixer_float(_Multiply_2954b87e98434eceb5cfff96fcfe2599_Out_2_Vector3, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Red, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Green, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Blue, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Out_1_Vector3);
        float3 _Blend_d14c5bda98d84c3fb1284902bede27e4_Out_2_Vector3;
        Unity_Blend_Difference_float3(_Contrast_bbcb0c062644455d8fdf2f7971111a79_Out_2_Vector3, _ChannelMixer_919fbfd1102a480dab14c9ff92036ca6_Out_1_Vector3, _Blend_d14c5bda98d84c3fb1284902bede27e4_Out_2_Vector3, float(0.2));
        FinalResult_1 = _Blend_d14c5bda98d84c3fb1284902bede27e4_Out_2_Vector3;
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        struct Bindings_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float
        {
        half4 uv0;
        };
        
        void SG_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float(UnityTexture2D _CardTexture, float2 _Rotation, float _Power, float _Frequency, float _Birghtness, Bindings_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float IN, out float3 FinalResult_1)
        {
        UnityTexture2D _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.tex, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.samplerstate, _Property_ca73e846263e410ca64dc1fb6532c91d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_R_4_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.r;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_G_5_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.g;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_B_6_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.b;
        float _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_A_7_Float = _SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4.a;
        float _Property_8e636d233dae42579d4f2349d5919086_Out_0_Float = _Birghtness;
        float4 _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4;
        Unity_Multiply_float4_float4(_SampleTexture2D_ab18546e01ff4c6d9037e27a1813e1ad_RGBA_0_Vector4, (_Property_8e636d233dae42579d4f2349d5919086_Out_0_Float.xxxx), _Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4);
        float _Property_d9facea32b3141868e44c4d12ed2030f_Out_0_Float = _Frequency;
        float2 _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2 = _Rotation;
        float2 _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2;
        Unity_Twirl_float(IN.uv0.xy, float2 (0.5, 0.5), _Property_d9facea32b3141868e44c4d12ed2030f_Out_0_Float, _Property_687afb139d254f618a293af3719d151f_Out_0_Vector2, _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2);
        float _Split_14f33f7535ba4031b5b9deb5435be679_R_1_Float = _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2[0];
        float _Split_14f33f7535ba4031b5b9deb5435be679_G_2_Float = _Twirl_0bd3840eb2614235a7cb3533b62376ad_Out_4_Vector2[1];
        float _Split_14f33f7535ba4031b5b9deb5435be679_B_3_Float = 0;
        float _Split_14f33f7535ba4031b5b9deb5435be679_A_4_Float = 0;
        float _Sine_c38cd02f50344dbaaea4e29b8f9d48a1_Out_1_Float;
        Unity_Sine_float(_Split_14f33f7535ba4031b5b9deb5435be679_R_1_Float, _Sine_c38cd02f50344dbaaea4e29b8f9d48a1_Out_1_Float);
        float _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Float;
        Unity_Multiply_float_float(_Sine_c38cd02f50344dbaaea4e29b8f9d48a1_Out_1_Float, 1, _Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Float);
        float4 Color_d87a525a77294c88a5ef65a8251a4bce = IsGammaSpace() ? float4(0.6556604, 0.7312801, 1, 0) : float4(SRGBToLinear(float3(0.6556604, 0.7312801, 1)), 0);
        float4 _Multiply_e7e5ff26093e495189cfb36b35cc9355_Out_2_Vector4;
        Unity_Multiply_float4_float4((_Multiply_140d2d6582db4e20aa09c4483de2d356_Out_2_Float.xxxx), Color_d87a525a77294c88a5ef65a8251a4bce, _Multiply_e7e5ff26093e495189cfb36b35cc9355_Out_2_Vector4);
        float4 _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector4;
        Unity_Add_float4(_Multiply_6bd877619ad245f9bd9cc8e62deef31c_Out_2_Vector4, _Multiply_e7e5ff26093e495189cfb36b35cc9355_Out_2_Vector4, _Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector4);
        float4 _Multiply_fcbfc116f1944d249bacf2a15621a1b8_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Add_cc687be6c4744e2ca4a84775a3a9fee5_Out_2_Vector4, Color_d87a525a77294c88a5ef65a8251a4bce, _Multiply_fcbfc116f1944d249bacf2a15621a1b8_Out_2_Vector4);
        FinalResult_1 = (_Multiply_fcbfc116f1944d249bacf2a15621a1b8_Out_2_Vector4.xyz);
        }
        
        void Unity_InvertColors_float4(float4 In, float4 InvertColors, out float4 Out)
        {
        Out = abs(InvertColors - In);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
        {
        Hash_Tchou_2_2_float(UV, UV);
        return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
        }
        
        void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
        {
        float2 g = floor(UV * CellDensity);
        float2 f = frac(UV * CellDensity);
        float t = 8.0;
        float3 res = float3(8.0, 0.0, 0.0);
        for (int y = -1; y <= 1; y++)
        {
        for (int x = -1; x <= 1; x++)
        {
        float2 lattice = float2(x, y);
        float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
        float d = distance(lattice + offset, f);
        if (d < res.x)
        {
        res = float3(d, offset.x, offset.y);
        Out = res.x;
        Cells = res.y;
        }
        }
        }
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        struct Bindings_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float
        {
        half4 uv0;
        };
        
        void SG_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float(UnityTexture2D _CardTexture, float2 _Rotation, float _Power, float _Frequency, float _Birghtness, Bindings_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float IN, out float3 FinalResult_1)
        {
        UnityTexture2D _Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D.tex, _Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D.samplerstate, _Property_cf20d4665d3d41ab89597215d3f1bc9b_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_R_4_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.r;
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_G_5_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.g;
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_B_6_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.b;
        float _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_A_7_Float = _SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4.a;
        float4 _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4;
        float4 _InvertColors_af90d499b00a4b99bbee5f7f6c192704_InvertColors = float4 (1, 1, 1, 0);
        Unity_InvertColors_float4(_SampleTexture2D_9bf28a1e38134b9c91059c9121828531_RGBA_0_Vector4, _InvertColors_af90d499b00a4b99bbee5f7f6c192704_InvertColors, _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4);
        float3 _Contrast_02b35aa32aa448b9b1cd267a88352da2_Out_2_Vector3;
        Unity_Contrast_float((_InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4.xyz), float(0.7), _Contrast_02b35aa32aa448b9b1cd267a88352da2_Out_2_Vector3);
        float3 _Saturation_034c925dc74947b4bc10d97ce4a3c8c8_Out_2_Vector3;
        Unity_Saturation_float(_Contrast_02b35aa32aa448b9b1cd267a88352da2_Out_2_Vector3, float(2), _Saturation_034c925dc74947b4bc10d97ce4a3c8c8_Out_2_Vector3);
        float4 Color_1fbddc4b70e149988f2ebdb019993a46 = IsGammaSpace() ? float4(0.7783019, 0.8308094, 1, 1) : float4(SRGBToLinear(float3(0.7783019, 0.8308094, 1)), 1);
        float3 _Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Saturation_034c925dc74947b4bc10d97ce4a3c8c8_Out_2_Vector3, (Color_1fbddc4b70e149988f2ebdb019993a46.xyz), _Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3);
        float2 _Property_74934bffab6b41fd9e2365c4f1603617_Out_0_Vector2 = _Rotation;
        float2 _Twirl_acefa8223ea34f708a108bdc7d8ed57d_Out_4_Vector2;
        Unity_Twirl_float(IN.uv0.xy, float2 (0.5, 0.5), float(0.68), _Property_74934bffab6b41fd9e2365c4f1603617_Out_0_Vector2, _Twirl_acefa8223ea34f708a108bdc7d8ed57d_Out_4_Vector2);
        float2 _TilingAndOffset_c8f307cbd552486f942416e4cf8d0426_Out_3_Vector2;
        Unity_TilingAndOffset_float(_Twirl_acefa8223ea34f708a108bdc7d8ed57d_Out_4_Vector2, float2 (2.79, 1), float2 (2.29, 0), _TilingAndOffset_c8f307cbd552486f942416e4cf8d0426_Out_3_Vector2);
        float _Voronoi_5236437f85754c03b93496da5dcc2f4f_Out_3_Float;
        float _Voronoi_5236437f85754c03b93496da5dcc2f4f_Cells_4_Float;
        Unity_Voronoi_Deterministic_float(_TilingAndOffset_c8f307cbd552486f942416e4cf8d0426_Out_3_Vector2, float(0), float(0.28), _Voronoi_5236437f85754c03b93496da5dcc2f4f_Out_3_Float, _Voronoi_5236437f85754c03b93496da5dcc2f4f_Cells_4_Float);
        float _Power_68601748f80b49aea4791103a8461dca_Out_2_Float;
        Unity_Power_float(_Voronoi_5236437f85754c03b93496da5dcc2f4f_Out_3_Float, float(4), _Power_68601748f80b49aea4791103a8461dca_Out_2_Float);
        UnityTexture2D _Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D = _CardTexture;
        float4 _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D.tex, _Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D.samplerstate, _Property_c5b7f0851d7d4bc29582d1211cb1f3a8_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_R_4_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.r;
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_G_5_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.g;
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_B_6_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.b;
        float _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_A_7_Float = _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_RGBA_0_Vector4.a;
        float _Multiply_b35ec2f72e4f48b1aff61d4e2fa34446_Out_2_Float;
        Unity_Multiply_float_float(_Power_68601748f80b49aea4791103a8461dca_Out_2_Float, _SampleTexture2D_6c433652438d4b8580bd8ee84bbedd62_R_4_Float, _Multiply_b35ec2f72e4f48b1aff61d4e2fa34446_Out_2_Float);
        float _Multiply_77f73e3af0d54291a642525ac2d405b0_Out_2_Float;
        Unity_Multiply_float_float(_Multiply_b35ec2f72e4f48b1aff61d4e2fa34446_Out_2_Float, 0.3, _Multiply_77f73e3af0d54291a642525ac2d405b0_Out_2_Float);
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_R_1_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[0];
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_G_2_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[1];
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_B_3_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[2];
        float _Split_59fcdca8a05c4d1a891c0cd9b5484991_A_4_Float = _InvertColors_af90d499b00a4b99bbee5f7f6c192704_Out_1_Vector4[3];
        float _Power_29bd1c5c32324264af18587c2a396daf_Out_2_Float;
        Unity_Power_float(_Split_59fcdca8a05c4d1a891c0cd9b5484991_G_2_Float, float(0.5), _Power_29bd1c5c32324264af18587c2a396daf_Out_2_Float);
        float _Smoothstep_fb344812fa7745d6bf53061d6b310c23_Out_3_Float;
        Unity_Smoothstep_float(float(0.04), float(0.14), _Power_68601748f80b49aea4791103a8461dca_Out_2_Float, _Smoothstep_fb344812fa7745d6bf53061d6b310c23_Out_3_Float);
        float _Multiply_7da3a538962e4a56867592ae2a7369fc_Out_2_Float;
        Unity_Multiply_float_float(_Power_29bd1c5c32324264af18587c2a396daf_Out_2_Float, _Smoothstep_fb344812fa7745d6bf53061d6b310c23_Out_3_Float, _Multiply_7da3a538962e4a56867592ae2a7369fc_Out_2_Float);
        float _Multiply_d0a72a3dd3004c488da8214efe442bd6_Out_2_Float;
        Unity_Multiply_float_float(_Multiply_7da3a538962e4a56867592ae2a7369fc_Out_2_Float, 2, _Multiply_d0a72a3dd3004c488da8214efe442bd6_Out_2_Float);
        float _Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float;
        Unity_Add_float(_Multiply_77f73e3af0d54291a642525ac2d405b0_Out_2_Float, _Multiply_d0a72a3dd3004c488da8214efe442bd6_Out_2_Float, _Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float);
        float3 _Add_3341e561f7ee4f15ac0d10191172e1b3_Out_2_Vector3;
        Unity_Add_float3(_Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3, (_Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float.xxx), _Add_3341e561f7ee4f15ac0d10191172e1b3_Out_2_Vector3);
        float3 _Multiply_bad1e6d1f24f42fbae936111ce22defd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_6ff4d2f214c946118913fef931c1709a_Out_2_Vector3, (_Add_28fe8b93b5f94d1a8386036e58b66d49_Out_2_Float.xxx), _Multiply_bad1e6d1f24f42fbae936111ce22defd_Out_2_Vector3);
        float3 _Saturation_64f668713ac84ecd9c86502d9a985941_Out_2_Vector3;
        Unity_Saturation_float(_Multiply_bad1e6d1f24f42fbae936111ce22defd_Out_2_Vector3, float(5), _Saturation_64f668713ac84ecd9c86502d9a985941_Out_2_Vector3);
        float3 _Add_11d62a32efc04eb08a23b4a50deb4802_Out_2_Vector3;
        Unity_Add_float3(_Add_3341e561f7ee4f15ac0d10191172e1b3_Out_2_Vector3, _Saturation_64f668713ac84ecd9c86502d9a985941_Out_2_Vector3, _Add_11d62a32efc04eb08a23b4a50deb4802_Out_2_Vector3);
        float3 _Contrast_0867fb2cf7f742bc8abdd975c30adc23_Out_2_Vector3;
        Unity_Contrast_float(_Add_11d62a32efc04eb08a23b4a50deb4802_Out_2_Vector3, float(1.1), _Contrast_0867fb2cf7f742bc8abdd975c30adc23_Out_2_Vector3);
        FinalResult_1 = _Contrast_0867fb2cf7f742bc8abdd975c30adc23_Out_2_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D.tex, _Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D.samplerstate, _Property_b5224d79d3754a6b88bfb37427a644e2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_R_4_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.r;
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_G_5_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.g;
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_B_6_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.b;
            float _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_A_7_Float = _SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_b76623d8364b41dba8404414b7320ba3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_25cf5305c34344838a2d1df894a8a820_Out_0_Vector2 = _Rotation;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_e1d2a9307dbb487fb9b7c37d4ddfc79d_Out_0_Float = _poly_power;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_341716e1a79247f3a03b257d0af43f59_Out_0_Float = _poly_frequency;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_5bcb79b3470a45aba479092a9a87697b_Out_0_Float = _poly_brightness;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6;
            _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6.uv0 = IN.uv0;
            float3 _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6_FinalResult_1_Vector3;
            SG_EditionPolychrome_d903eda9611efcb4e8fad2d69de24909_float(_Property_b76623d8364b41dba8404414b7320ba3_Out_0_Texture2D, _Property_25cf5305c34344838a2d1df894a8a820_Out_0_Vector2, _Property_e1d2a9307dbb487fb9b7c37d4ddfc79d_Out_0_Float, _Property_341716e1a79247f3a03b257d0af43f59_Out_0_Float, _Property_5bcb79b3470a45aba479092a9a87697b_Out_0_Float, _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6, _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6_FinalResult_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_cdc86a0cfaa04eea9208e082169953bc_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_7625f424b4454d5882d6af227cdea65f_Out_0_Vector2 = _Rotation;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc;
            _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc.uv0 = IN.uv0;
            float3 _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc_FinalResult_1_Vector3;
            SG_EditionFoil_b760a16755984eb47adf52a5abd8f5d2_float(_Property_cdc86a0cfaa04eea9208e082169953bc_Out_0_Texture2D, _Property_7625f424b4454d5882d6af227cdea65f_Out_0_Vector2, float(0.2), float(300), float(0.7), _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc, _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc_FinalResult_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_4623ad78d15242e2b1bd6af9bf910c67_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_639a0ec42a684ca58bf7765da16152a2_Out_0_Vector2 = _Rotation;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float _EditionNegative_26c00ba8abda475884c6c867f0ebafb6;
            _EditionNegative_26c00ba8abda475884c6c867f0ebafb6.uv0 = IN.uv0;
            float3 _EditionNegative_26c00ba8abda475884c6c867f0ebafb6_FinalResult_1_Vector3;
            SG_EditionNegative_9539dfee1dd290d4ca2857b3ebb639eb_float(_Property_4623ad78d15242e2b1bd6af9bf910c67_Out_0_Texture2D, _Property_639a0ec42a684ca58bf7765da16152a2_Out_0_Vector2, float(0.87), float(0.35), float(0.7), _EditionNegative_26c00ba8abda475884c6c867f0ebafb6, _EditionNegative_26c00ba8abda475884c6c867f0ebafb6_FinalResult_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(_EDITION_REGULAR)
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = (_SampleTexture2D_1771f947e84a4d6ea58904c36899285e_RGBA_0_Vector4.xyz);
            #elif defined(_EDITION_POLYCHROME)
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = _EditionPolychrome_0558331cc6e54de9b3c3b43d772799a6_FinalResult_1_Vector3;
            #elif defined(_EDITION_FOIL)
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = _EditionFoil_664cab05d32b49a7af128fdf92f7dbbc_FinalResult_1_Vector3;
            #else
            float3 _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3 = _EditionNegative_26c00ba8abda475884c6c867f0ebafb6_FinalResult_1_Vector3;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.tex, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.samplerstate, _Property_7b409ee67dfd45e6876db684f28e4253_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_R_4_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.r;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_G_5_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.g;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_B_6_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.b;
            float _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_RGBA_0_Vector4.a;
            #endif
            surface.BaseColor = _Edition_85956b26c4194acdb424b306efedafce_Out_0_Vector3;
            surface.Alpha = _SampleTexture2D_0c230fc5a0af43b0870692d7a168da2a_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteUnlitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}