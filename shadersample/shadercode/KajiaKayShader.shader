Shader "BoyuanDemo/KajiaKayShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShiftTex ("AnistropicShiftTexture", 2D) = "white" {}
        _BaseColor("BaseColor",Color)=(1,1,1,1)
        _SpecColor("SpecularColor", Color) = (1,1,1,1)
        _Shininess("Shininess", Range(0.03, 300)) = 0.078125
        _Shift("Shift", Range(-1, 1)) = 0.1
    }
    SubShader
    {
        Tags {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalRenderPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend One Zero
            ZTest LEqual
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            
            TEXTURE2D (_MainTex);
            SAMPLER (sampler_MainTex);
            TEXTURE2D (_ShiftTex);
            SAMPLER (sampler_ShiftTex);
            half4 _BaseColor;
            half4 _SpecColor;
            float _Shininess;
            float _Shift;
            float4 _MainTex_ST;
            
            struct a2v
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            v2f vert (a2v v)
            {
                v2f o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                //o.viewDirWS = normalize(TransformObjectToWorldDir(-v.positionOS.xyz));
                o.viewDirWS =normalize(_WorldSpaceCameraPos.xyz-TransformObjectToWorld(v.positionOS.xyz));
                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _BaseColor;
                half shift = SAMPLE_TEXTURE2D(_ShiftTex, sampler_ShiftTex, i.uv).r;
                // Blinn-Phong lighting
                half3 normal = normalize(i.normalWS);
                half3 tangent = normalize(i.tangentWS);
                
                half3 bitangent = normalize(i.bitangentWS);
                half3 newtangent= normalize(bitangent+normal*_Shift*shift);
                half3 viewDir = normalize(i.viewDirWS);
                Light mylight=GetMainLight();
                half3 lightDir = normalize(mylight.direction);
                half3 halfDir = normalize(lightDir + viewDir);

                half NdotL = dot(normal, lightDir)*0.5+0.5;
                half TdotH = dot(newtangent, halfDir);
                half TsinH=sqrt(1-TdotH*TdotH);
                half3 diffuse = NdotL * col.rgb;
                half3 specular = pow(TsinH, _Shininess) * _SpecColor.rgb;
                //half3 specular = pow(NdotH, _Shininess) * col.rgb;
                half3 finalColor = diffuse + specular;
                return half4(finalColor, col.a);
            }
            ENDHLSL
        }
    }
}