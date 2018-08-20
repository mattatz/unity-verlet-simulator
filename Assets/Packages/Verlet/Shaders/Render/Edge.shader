Shader "Verlet/Edge"
{

	Properties
	{
    _Color ("Color", Color) = (1, 1, 1, 1)
	}

  CGINCLUDE

  #include "UnityCG.cginc"
  #include "../Common/Node.cginc"
  #include "../Common/Edge.cginc"

  struct appdata
  {
    float4 vertex : POSITION;
    uint vid : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct v2f
  {
    float4 position : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  StructuredBuffer<Node> _Nodes;
  StructuredBuffer<Edge> _Edges;
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

    Edge e = _Edges[iid];
    Node na = _Nodes[e.a];
    Node nb = _Nodes[e.b];

    float3 pa = na.position;
    float3 pb = nb.position;

    float3 position = lerp(pa, pb, IN.vid);

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
