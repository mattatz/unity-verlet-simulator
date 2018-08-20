Shader "Verlet/Node"
{

	Properties
	{
    _Size ("Size", Range(0.0, 1.0)) = 0.25
    _Color ("Color", Color) = (1, 1, 1, 1)
	}

  CGINCLUDE

  #include "UnityCG.cginc"
  #include "../Common/Node.cginc"

  struct appdata
  {
    float4 vertex : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct v2f
  {
    float4 position : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  StructuredBuffer<Node> _Nodes;

  float _Size;
  float4 _Color;

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

    Node n = _Nodes[iid];
    float3 position = n.position.xyz + IN.vertex.xyz * _Size;
    float4 vertex = float4(position, 1);
    OUT.position = UnityObjectToClipPos(vertex);
    return OUT;
  }

  fixed4 frag(v2f IN) : SV_Target
  {
    return _Color;
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
	}
}