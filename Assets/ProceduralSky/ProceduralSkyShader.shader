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

            static const float PI = 3.14159265359;
            static const float TAU = PI * 2;
            static const float maxFloat = 3.402823466e+38;

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            float3 dirToSun;
            float3 planetCentre;
            float planetRadius;
            float atmosphereRadius;

            // Variables
            int numInScatterPoints;
            int numOpticalDepthPoints;
            float densityFalloff;
            float intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVector : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // Invert the perspective projection of the view-space position
                // Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
				o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
                return o;
            }

            // Returns vector (dstToSphere, dstThroughSphere)
            // If ray origin is inside sphere, dstToSphere = 0
            // If ray misses sphere, dstToSphere = maxValue; dstThroughSphere = 0
            float2 raySphere(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir)
            {
                float3 offset = rayOrigin - sphereCentre;
                float a = dot(rayDir, rayDir); // Set to dot(rayDir, rayDir) if rayDir might not be normalised
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
                return float2(maxFloat, 0);
            }

            float densityAtPoint(float3 densitySamplePoint)
            {
                float heightAboveSurface = length(densitySamplePoint - planetCentre) - planetRadius;
                float height01 = heightAboveSurface / (atmosphereRadius - planetRadius);
                float localDensity = exp(-height01 * densityFalloff) * (1 - height01);
                return localDensity;
            }
            
            // Bad, could be optimised to use the angle + ray position to calculate the depth
            float opticalDepth(float3 rayOrigin, float3 rayDir, float rayLength)
            {
                float3 densitySamplePoint = rayOrigin;
                float stepSize = rayLength / (numOpticalDepthPoints - 1);
                float opticalDepth = 0;

                for (int i = 0;  i < numOpticalDepthPoints; ++i)
                {
                    float localDensity = densityAtPoint(densitySamplePoint);
                    opticalDepth += localDensity * stepSize;
                    densitySamplePoint += rayDir * stepSize;
                }
                return opticalDepth;
            }

            float3 calculateLight(float3 rayOrigin, float3 rayDir, float rayLength, float3 originalCol)
            {
                float3 inScatterPoint = rayOrigin;
                float stepSize = rayLength / (numInScatterPoints - 1);
                float inScatteredLight = 0;
                float viewRayOpticalDepth = 0;

                for (int i = 0;  i < numInScatterPoints; ++i)
                {
                    float sunRayLength = raySphere(planetCentre, atmosphereRadius, inScatterPoint, dirToSun).y;
                    float sunRayOpticalDepth = opticalDepth(inScatterPoint, dirToSun, sunRayLength);
                    float localDensity = densityAtPoint(inScatterPoint);
                    viewRayOpticalDepth = opticalDepth(inScatterPoint, -rayDir, stepSize * i);
                    float3 transmittance = exp(-(sunRayOpticalDepth + viewRayOpticalDepth));

                    inScatteredLight += localDensity * transmittance;
                    inScatterPoint += rayDir * stepSize;
                }

                //return inScatteredLight;
                return originalCol * (1 - inScatteredLight) + inScatteredLight;

                // // Attenuate brightness of original col (i.e light reflected from planet surfaces)
                // // This is a hacky mess, TODO: figure out a proper way to do this
                // const float brightnessAdaptionStrength = 0.15;
                // const float reflectedLightOutScatterStrength = 3;
                // float brightnessAdaption = dot(inScatteredLight, 1) * brightnessAdaptionStrength;
                // float brightnessSum = viewRayOpticalDepth * intensity * reflectedLightOutScatterStrength + brightnessAdaption;
                // float reflectedLightStrength = exp(-brightnessSum);
                // float hdrStrength = saturate(dot(originalCol, 1) / 3 - 1);
                // reflectedLightStrength = lerp(reflectedLightStrength, 1, hdrStrength);
                // float3 reflectedLight = originalCol * reflectedLightStrength;

                // float3 finalCol = reflectedLight + inScatteredLight;

                // return finalCol;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 originalCol = tex2D(_MainTex, i.uv);
                float sceneDepthNonLinear = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float sceneDepth = LinearEyeDepth(sceneDepthNonLinear) * length(i.viewVector);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.viewVector);

                float2 hitInfo = raySphere(planetCentre, atmosphereRadius, rayOrigin, rayDir);
                float dstToAtmosphere = hitInfo.x;
                float dstThroughAtmosphere = min(hitInfo.y, sceneDepth - dstToAtmosphere);

                if (dstThroughAtmosphere > 0)
                {
                    const float epsilon = 0.0001;
                    float3 pointInAtmosphere = rayOrigin + rayDir * (dstToAtmosphere + epsilon);
                    float3 light = calculateLight(pointInAtmosphere, rayDir, dstThroughAtmosphere - epsilon * 2, originalCol);
                    return float4(light, 1);
                    //return originalCol * (1 - light) + light;
                }
                return originalCol;
            }
            ENDCG
        }
    }
}
