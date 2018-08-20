Shader "Verlet/Demo/Tips"
{

	Properties
	{
    _Color ("Color", Color) = (1, 1, 1, 1)
    _Size ("Size", Float) = 0.5
	}

  CGINCLUDE

  #include "UnityCG.cginc"
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
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  float4 _Color;
  float _Size;

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
    float3 tip = _Nodes[idx].position;

    float3 position = tip.xyz + IN.vertex.xyz * _Size;
    OUT.position = UnityObjectToClipPos(float4(position, 1));
    OUT.normal = UnityObjectToWorldNormal(IN.normal);
    OUT.uv = IN.uv;
    return OUT;
  }

  fixed4 frag(v2f IN) : SV_Target
  {
    // return _Color;
    // return float4(IN.uv, 0, 1);
    float3 normal = normalize(IN.normal);
    return float4((normal + 1.0) * 0.5, 1);
  }

  ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
      #pragma instancing_options procedural:setup
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
      ENDCG
    }

	}
}
