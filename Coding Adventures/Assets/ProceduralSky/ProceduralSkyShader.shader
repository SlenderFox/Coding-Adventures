Shader "Hidden/ProceduralSkyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DepthMax ("Depth Max", Range(1, 100)) = 20
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            //static const float PI = 3.14159265359;
            //static const float TAU = PI * 2;
            //static const float maxFloat = 3.402823466e+38;

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float _DepthMax;
            float atmosphereRadius;
            float3 planetCentre;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVector : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVector : TEXCOORD1;
            };

            // Returns vector (dstToSphere, dstThroughSphere)
            // If ray origin is inside sphere, dstToSphere = 0
            // If ray misses sphere, dstToSphere = maxValue; dstThroughSphere = 0
            float2 raySphere(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir)
            {
                float3 offset = rayOrigin - sphereCentre;
                float a = 1; // Set to dot(rayDir, rayDir) if rayDir might not be normalised
                float b = 2 * dot(offset, rayDir);
                float c = dot(offset, offset) - sphereRadius * sphereRadius;
                float d = b * b - 4 * a * c; // Discriminant from quadratic formula

                // Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
                if (d > 0)
                {
                    float s = sqrt(d);
                    float dstToSphereNear = max(0, (-b - s) / (2 * a));
                    float dstToSphereFar = (-b + s) / (2 * a);

                    // Ignore intersections that occur behind the ray
                    if (dstToSphereFar >= 0)
                        return float2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
                }
                // Ray did not intersect sphere
                return float2(3.402823466e+38, 0);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.viewVector = v.viewVector;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 originalCol = tex2D(_MainTex, i.uv);
                float sceneDepthNonLinear = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                //float sceneDepth = LinearEyeDepth(sceneDepthNonLinear) * length(i.viewVector);
                float sceneDepth = Linear01Depth(sceneDepthNonLinear) * _ProjectionParams.z * (1 / _DepthMax);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.viewVector);

                float3 yeah = (0, 0, 0);
                float2 hitInfo = raySphere(yeah, atmosphereRadius, rayOrigin, rayDir);
                float distToAtmosphere = hitInfo.x;
                float distThroughAtmosphere = hitInfo.y;

                //return distThroughAtmosphere / (atmosphereRadius * 2);
                return sceneDepth;
            }
            ENDCG
        }
    }
}
