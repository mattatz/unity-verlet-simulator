Shader "Verlet/Demo/Ground" {

	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0

    _Height ("Height", Float) = 1
    _Scale ("Scale", Float) = 1
    _Speed ("Speed", Float) = 1
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
		#pragma target 3.0

    #include "./SimplexNoise3D.cginc"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
      float3 worldNormal;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

    float _Height, _Scale, _Speed;

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

    void vert(inout appdata_full v) {
      float2 uv = v.texcoord.xy * _Scale;
      float x = snoise(float3(uv.x, _Time.y * _Speed, uv.y)).x;
      v.vertex.xyz += v.normal * _Height * x;
    }

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// fixed4 c = fixed4((IN.worldNormal + 1) * 0.5, 1) * _Color;
			fixed4 c = _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
