Shader "Custom/PointCloudShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_TriggerPress("triggerpress", Int) = 0 // 0 for off, 1 for on
		_DisplayNormals("hasnormals", Int) = 0 // 0 for off, 1 for on
		_DisplayRoundPoints("roundpoints", Int) = 0 // 0 for off, 1 for on
		_DisplayRadius("displayradius", Float) = 0.005
		_EditPos("editpos", Vector) = (0, 0, 0, 0)
		_EditCol("editCol", Color) = (0.95, 0.88, 0.03, 0)
		_EditRadius("editradius", Float) = 0.015
	}
		SubShader
	{
		CGINCLUDE
			#pragma target 5.0
			#include "UnityCG.cginc"
			#include "Autolight.cginc"
			uniform RWStructuredBuffer<float3> vertsBuf : register(u1);
			uniform RWStructuredBuffer<float3> normsBuf : register(u2);
			uniform RWStructuredBuffer<float4> colsBuf : register(u3);

			sampler2D _MainTex;
			int _TriggerPress;
			int _DisplayNormals;
			int _DisplayRoundPoints;
			float4 _EditPos;
			float4 _EditCol;
			float _EditRadius;
			float _DisplayRadius;
		
			struct appdata
			{
				uint ix : SV_VertexID;
			};
			// Vertex to geometry
			struct v2g 
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float4 col : COLOR;
			};
			// Geometry to fragment
			struct g2f
			{
				float4 vertex : SV_POSITION;
				float4 col : COLOR;
				float2 uv : TEXCOORD0;
			};
			v2g vert(appdata v)
			{
				v2g o;
				float4 position = float4(vertsBuf[v.ix].xyz, 0);
				float4 normal = float4(normsBuf[v.ix].xyz, 0);
				
				// Paint / mark point cloud
				if (_TriggerPress == 1)
				{
					if (distance(position.xyz, _EditPos.xyz) < _EditRadius) {
						colsBuf[v.ix] = _EditCol;
					}
				}
				o.vertex = position;
				o.normal = normal;
				o.col = colsBuf[v.ix];

				return o;
			}
			[maxvertexcount(6)]
			void geom(point v2g input[1], inout TriangleStream<g2f> triStream)
			{
				g2f v1;
				g2f v2;
				g2f v3;
				g2f v4;

				float radius = _DisplayRadius;
				float4 normal = input[0].normal;

				if (_DisplayNormals == 0)
				{
					float4 base = UnityObjectToClipPos(input[0].vertex);

					// Should fix normals later
					v1.vertex = base + float4(-radius, -radius, 0, 0);
					v2.vertex = base + float4(radius, -radius, 0, 0);
					v3.vertex = base + float4(-radius, radius, 0, 0);
					v4.vertex = base + float4(radius, radius, 0, 0);
				}
				else
				{
					float4 base = input[0].vertex;
					float3 right = normalize(cross(normal.xyz, float3(0, 1, 0)));
					float3 up = cross(right, normal);

					right *= radius;
					up *= radius;

					v1.vertex = base - float4(right.xyz, 0) - float4(up.xyz, 0);
					v2.vertex = base + float4(right.xyz, 0) - float4(up.xyz, 0);
					v3.vertex = base - float4(right.xyz, 0) + float4(up.xyz, 0);
					v4.vertex = base + float4(right.xyz, 0) + float4(up.xyz, 0);

					v1.vertex = UnityObjectToClipPos(v1.vertex);
					v2.vertex = UnityObjectToClipPos(v2.vertex);
					v3.vertex = UnityObjectToClipPos(v3.vertex);
					v4.vertex = UnityObjectToClipPos(v4.vertex);
				}

				v1.col = input[0].col;
				v2.col = input[0].col;
				v3.col = input[0].col;
				v4.col = input[0].col;

				v1.uv = float2(0, 0);
				v2.uv = float2(1, 0);
				v3.uv = float2(0, 1);
				v4.uv = float2(1, 1);

				// Add output geom
				triStream.Append(v2);
				triStream.Append(v3);
				triStream.Append(v1);
				triStream.RestartStrip();
				
				triStream.Append(v2);
				triStream.Append(v4);
				triStream.Append(v3);
				triStream.RestartStrip();
			}

			fixed4 frag(g2f i) : SV_Target
			{
				float4 col = i.col;

				if (_DisplayRoundPoints)
				{
					float x = i.uv.x;
					float y = i.uv.y;
					float dist = sqrt(pow((0.5 - x), 2) + pow((0.5 - y), 2));
					if (dist > 0.5) {
						discard;
					}
					else {
						col = i.col;
					}
				}

				return col;
			}
		ENDCG

		Pass
		{
			Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase"}
			LOD 100
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			//#pragma multi_compile_fog
			//#pragma multi_compile_fwdbase
			//#pragma shader_feature IS_LIT
			
			ENDCG
			}
	}
}