Shader "Verlet/Demo/Tentacles"
{

	Properties
	{
    _Color ("Color", Color) = (1, 1, 1, 1)
    _Thickness ("Thickness", Float) = 0.25
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
  float _Thickness;

  StructuredBuffer<Node> _Nodes;

  int _TentaclesCount, _DivisionsCount;
  float _InvTubularCount, _InvRadialsCount;

  float4x4 _World2Local, _Local2World;
  float3 _LocalCameraDirection;

  void setup() {
    unity_ObjectToWorld = _Local2World;
    unity_WorldToObject = _World2Local;
  }

  v2f vert(appdata IN, uint iid : SV_InstanceID)
  {
    v2f OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

    float t = IN.vid * _InvRadialsCount;
    float ft = frac(t);
    int division_id = IN.uv.y * _DivisionsCount;

    int idx = iid * _DivisionsCount + division_id;
    float3 cur = _Nodes[idx].position;

    float3 up = (0).xxx;
    float3 right = (0).xxx;
    if (division_id == 0) {
      // head
      float3 prev = _Nodes[idx + 1].position.xyz;
      up = normalize(cur - prev);
      right = normalize(cross(up, _LocalCameraDirection)) * 0.5 * _Thickness;
    }
    else if (division_id == _DivisionsCount - 1) {
      // tail
      float3 next = _Nodes[idx - 1].position.xyz;
      up = normalize(next - cur);
      right = normalize(cross(up, _LocalCameraDirection)) * 0.5 * _Thickness;
    }
    else {
      // middle
      float3 prev = _Nodes[idx + 1].position.xyz;
      float3 next = _Nodes[idx - 1].position.xyz;
      float3 dir10 = normalize(cur - prev);
      float3 dir21 = normalize(next - cur);
      up = ((dir10 + dir21) * 0.5f);
      float d = saturate((dot(dir10, dir21) + 1.0) * 0.5);
      right = normalize(cross(up, _LocalCameraDirection)) * lerp(1, 0.5, d) * _Thickness;
    }

    float u = IN.uv.x * UNITY_PI * 2;

    float3 binormal = cross(up, right);
    float3 offset = right * cos(u) + binormal * sin(u);
    float3 position = cur.xyz + offset;

    OUT.position = UnityObjectToClipPos(float4(position, 1));
    OUT.normal = normalize(offset);
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
