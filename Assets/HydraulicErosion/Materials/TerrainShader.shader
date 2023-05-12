Shader "Custom/TerrainShader"
{
	Properties
	{
		_BaseColour ("Base Colour", Color) = (0.415,0.212,0.103,1)
		_FlatColour ("Flat Colour", Color) = (0.318,0.368,0.081,1)
		_SlopeThreshold ("Slope Threshold", Range(0,1)) = 0.25
		_BlendAmount ("Blend Amount", Range (0,1)) = 0.6
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		//LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input
		{
			float3 worldNormal;
		};

		half _SlopeThreshold;
		half _BlendAmount;
		fixed4 _FlatColour;
		fixed4 _BaseColour;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			float slope = 1 - IN.worldNormal.y; // Slope = 0 when terrain is completely flat
			float blendHeight = _SlopeThreshold * (1 - _BlendAmount);
			float flatWeight = 1 - saturate((slope - blendHeight) / (_SlopeThreshold - blendHeight));
			o.Albedo = _FlatColour * flatWeight + _BaseColour * (1 - flatWeight);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
