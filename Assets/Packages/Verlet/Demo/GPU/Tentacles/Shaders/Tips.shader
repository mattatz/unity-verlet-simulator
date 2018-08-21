Shader "Verlet/Demo/Tips"
{

	Properties
	{
    [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
    [HDR] _Emission ("Emission", Color) = (0, 0, 0, 0)
    _Size ("Size", Float) = 0.5

    [Space] _Glossiness ("Smoothness", Range(0, 1)) = 0.5
    [Gamma] _Metallic ("Metallic", Range(0, 1)) = 0
	}

  CGINCLUDE

  #include "UnityCG.cginc"
  #include "UnityGBuffer.cginc"
  #include "UnityStandardUtils.cginc"

  #include "Assets/Packages/Verlet/Shaders/Common/Node.cginc"

  struct appdata
  {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    uint vid : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct v2f
  {
    float4 position : SV_POSITION;
#if defined(UNITY_PASS_SHADOWCASTER)
#else
    float3 normal : NORMAL;
    half3 ambient : TEXCOORD0;
    float3 wpos : TEXCOORD1;
    float4 emission : TEXCOORD2;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  float4 _Color, _Emission;
  float _Size;
  half _Glossiness, _Metallic;

  StructuredBuffer<Node> _Nodes;

  int _TentaclesCount, _DivisionsCount;

  float4x4 _World2Local, _Local2World;

  void setup() {
    unity_ObjectToWorld = _Local2World;
    unity_WorldToObject = _World2Local;
  }

  v2f vert(appdata IN, uint iid : SV_InstanceID)
  {
    v2f OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

    int idx = iid * _DivisionsCount + _DivisionsCount - 1;
    Node node = _Nodes[idx];
    float3 tip = node.position;

    float3 position = tip.xyz + IN.vertex.xyz * _Size;

    float3 wpos = mul(unity_ObjectToWorld, float4(position, 1)).xyz;
    float3 wnrm = UnityObjectToWorldNormal(IN.normal);

    #if defined(UNITY_PASS_SHADOWCASTER)
      float scos = dot(wnrm, normalize(UnityWorldSpaceLightDir(wpos.xyz)));
      wpos.xyz -= wnrm * unity_LightShadowBias.z * sqrt(1 - scos * scos);
      OUT.position = UnityApplyLinearShadowBias(UnityWorldToClipPos(float4(wpos.xyz, 1)));
    #else
      OUT.position = UnityWorldToClipPos(float4(wpos.xyz, 1));
      OUT.normal = wnrm;
      OUT.ambient = ShadeSHPerVertex(wnrm, 0);
      OUT.wpos = wpos.xyz;
      OUT.emission = lerp(float4(0, 0, 0, 0), _Emission, saturate(1.0 - node.decay));
    #endif

    return OUT;
  }

  #if defined(UNITY_PASS_SHADOWCASTER)

    half4 frag() : SV_Target
    {
      return 0;
    }

  #else

    void frag(v2f IN, out half4 outGBuffer0 : SV_Target0, out half4 outGBuffer1 : SV_Target1, out half4 outGBuffer2 : SV_Target2, out float4 outEmission : SV_Target3)
    {
      half3 albedo = _Color.rgb;

      half3 c_diff, c_spec;
      half refl10;
      c_diff = DiffuseAndSpecularFromMetallic(
        albedo, _Metallic,
        c_spec, refl10
      );

      UnityStandardData data;
      data.diffuseColor = c_diff;
      data.occlusion = 1.0;
      data.specularColor = c_spec;
      data.smoothness = _Glossiness;
      data.normalWorld = normalize(IN.normal);
      UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

      half3 sh = ShadeSHPerPixel(data.normalWorld, IN.ambient, IN.wpos);
      outEmission = IN.emission + half4(sh * c_diff, 1);
    }

  #endif

  ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 100

		Pass
		{
		  Tags { "LightMode" = "Deferred" }
			CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
      #pragma instancing_options procedural:setup
      #pragma multi_compile_prepassfinal noshadowmask nodynlightmap nodirlightmap nolightmap
			ENDCG
		}

    Pass
    {
      Tags{ "LightMode" = "ShadowCaster" }
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
      #pragma instancing_options procedural:setup
      #pragma multi_compile_shadowcaster noshadowmask nodylightmap nodirlightmap nolightmap
      #define UNITY_PASS_SHADOWCASTER
      ENDCG
    }

	}

}
