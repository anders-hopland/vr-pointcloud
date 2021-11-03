Shader "Custom/PointCloudShader"
{
	Properties
	{
		_EditPos("editpos", Vector) = (0, 0, 0, 0)
		_EditRadius("editradius", Float) = 1.0
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

			float4 _EditPos;
			float _EditRadius;

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
			};
			v2g vert(appdata v)
			{
				v2g o;
				float4 position = float4(vertsBuf[v.ix].xyz, 0);
				float4 normal = float4(normsBuf[v.ix].xyz, 0);
				
				// Will later be used for "painting"
				if (distance(position.xyz, _EditPos.xyz) > _EditRadius) {
					colsBuf[v.ix] = float4(1, 0, 0, 0);
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

				float radius = 0.06f;

				float4 base = UnityObjectToClipPos(input[0].vertex);

				// Should fix normals later
				v1.vertex = base + float4(-radius, -radius, 0, 0);
				v2.vertex = base + float4(radius, -radius, 0, 0);
				v3.vertex = base + float4(-radius, radius, 0, 0);
				v4.vertex = base + float4(radius, radius, 0, 0);

				v1.col = input[0].col;
				v2.col = input[0].col;
				v3.col = input[0].col;
				v4.col = input[0].col;

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
				fixed4 col = i.col;
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