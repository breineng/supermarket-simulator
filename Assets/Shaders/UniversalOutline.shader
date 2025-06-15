Shader "Hidden/UniversalOutline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1, 0.5, 0, 1)
        _Thickness ("Outline Thickness", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "STENCIL_WRITE"
            Cull Off
            ZWrite Off
            ColorMask 0

            Stencil 
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            
            Varyings vert(Attributes input) 
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite Off
            ZTest Always
            
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert_outline
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Thickness;
            CBUFFER_END
            
            Varyings vert_outline(Attributes input)
            {
                Varyings output;

                float4 vertex = float4(input.positionOS.xyz, 1.0);
                float3 normal = normalize(input.normalOS);

                // Calculate the position of the vertex and the extruded vertex in clip space
                float4 clipPos = TransformObjectToHClip(vertex.xyz);
                float4 clipPosFront = TransformObjectToHClip(vertex.xyz + normal * 0.01);

                // Use the difference to find the normal direction in Normalized Device Coordinates
                float2 clipNormal = normalize((clipPosFront.xy / clipPosFront.w) - (clipPos.xy / clipPos.w));

                // Apply aspect ratio correction
                clipNormal.x *= _ScreenParams.y / _ScreenParams.x;

                // Offset the original vertex clip position
                // We scale the thickness to be more manageable in the inspector
                clipPos.xy += clipNormal * _Thickness * 0.02;

                output.positionCS = clipPos;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
} 