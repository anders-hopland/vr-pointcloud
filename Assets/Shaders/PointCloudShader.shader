Shader "Custom/PointCloudShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_TriggerPress("triggerpress", Int) = 0 // 0 for off, 1 for on
		_DisplayNormals("hasnormals", Int) = 0 // 0 for off, 1 for on
		_DisplayRoundPoints("roundpoints", Int) = 0 // 0 for off, 1 for on
		_CurLabel("curlabel", Int) = 0
		_Displaymode("displaymode", Int) = 1
		_CbOffset("computebufferoffset", Int) = 0
		_DisplayRadius("displayradius", Float) = 0.005
		_EditPos("editpos", Vector) = (0, 0, 0, 0)
		_HoverCol("hoverCol", Color) = (0.99, 0.02, 0.99, 0)
		_EditRadius("editradius", Float) = 0.015

		// Params needed for height color gradient view mode
		_MinHeight("minHeight", Float) = 0
		_MaxHeight("maxHeight", Float) = 1
		_HeightCol1("heightCol1", Color) = (0.99, 0.98, 0.43, 0)
		_HeightCol2("heightCol2", Color) = (0.65, 0.45, 0.98, 0)
	}
		SubShader
		{
			ZTest LEqual 
			ZWrite On
			CGINCLUDE
				#pragma target 5.0
				#include "UnityCG.cginc"
				#include "Autolight.cginc"
			

				struct pointStruct {
					float3 vert; // Only used in geom shader
					float3 norm;
					float4 col;
					int classification;
				};

				struct appdata
				{
					uint ix : SV_VertexID;
					float4 vertex : POSITION;
				};

				// Vertex to fragment
				struct v2f
				{
					float4 vertex : POSITION;
					float4 normal : NORMAL;
					float4 col : COLOR;
					float2 uv : TEXCOORD0;
				};

				uniform RWStructuredBuffer<pointStruct> pointBuf : register(u1);
				uniform RWStructuredBuffer<float4> dataBuf : register(u2);

				sampler2D _MainTex;

				int _TriggerPress;
				int _DisplayNormals;
				int _DisplayRoundPoints;
				int _CbOffset;
				int _CurLabel;
				int _Displaymode;
				float4 _EditPos;
				float4 _EditCol;
				float4 _HoverCol;
				float _EditRadius;
				float _DisplayRadius;

				float _MinHeight;
				float _MaxHeight;
				float4 _HeightCol1;
				float4 _HeightCol2;

				v2f vert(appdata v)
				{
					v2f o;
					int ix = v.ix / 4 + _CbOffset; // 4 vertices per quad
					float radius = _DisplayRadius;
					float4 position = float4(pointBuf[ix].vert.xyz, 0);

					// Choose color based on display mode
					if (_Displaymode == 0)
					{
						// Original rgb color
						o.col = pointBuf[ix].col;
					}
					else if (_Displaymode == 1)
					{
						// Classification
						// Paint / hover / mark point cloud
						if (distance(position.xyz, _EditPos.xyz) < _EditRadius) {
							if (_TriggerPress == 1)
							{
								pointBuf[ix].classification = _CurLabel;
								o.col = dataBuf[pointBuf[ix].classification];
							}
							else
							{
								o.col = _HoverCol;
							}
						}
						else
						{
							o.col = dataBuf[pointBuf[ix].classification];
						}
					}
					else if (_Displaymode == 2)
					{
						// Color gradient based on height
						float heightLerpFac = (position.z - _MinHeight) / (_MaxHeight - _MinHeight);
						o.col = lerp(_HeightCol1, _HeightCol2, heightLerpFac);
					}

					float4 normal = float4(pointBuf[ix].norm.xyz, 0);

					if (_DisplayNormals == 0)
					{
						position = UnityObjectToClipPos(position);
						if (v.ix % 4 == 0)
						{
							o.vertex = position + float4(-radius, -radius, 0, 0); o.uv = float2(0, 0);
						}
						if (v.ix % 4 == 1)
						{
							o.vertex = position + float4(radius, -radius, 0, 0); o.uv = float2(0, 1);
						}
						if (v.ix % 4 == 2)
						{
							o.vertex = position + float4(-radius, radius, 0, 0); o.uv = float2(1, 0);
						}
						if (v.ix % 4 == 3)
						{
							o.vertex = position + float4(radius, radius, 0, 0); o.uv = float2(1, 1);
						}
					}
					else 
					{
						float3 right = normalize(cross(normal.xyz, float3(0, 1, 0)));
						float3 up = cross(right, normal);

						right *= radius;
						up *= radius;

						if (v.ix % 4 == 0)
						{
							o.vertex = position - float4(right.xyz, 0) - float4(up.xyz, 0);
							o.vertex = UnityObjectToClipPos(o.vertex);
							o.uv = float2(0, 0);
						}
						if (v.ix % 4 == 1)
						{
							o.vertex = position + float4(right.xyz, 0) - float4(up.xyz, 0);
							o.vertex = UnityObjectToClipPos(o.vertex);
							o.uv = float2(0, 1);
						}
						if (v.ix % 4 == 2)
						{
							o.vertex = position - float4(right.xyz, 0) + float4(up.xyz, 0);
							o.vertex = UnityObjectToClipPos(o.vertex);
							o.uv = float2(1, 0);
						}
						if (v.ix % 4 == 3)
						{
							o.vertex = position + float4(right.xyz, 0) + float4(up.xyz, 0);
							o.vertex = UnityObjectToClipPos(o.vertex);
							o.uv = float2(1, 1);
						}
					}
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float4 col = i.col;

					float x = i.uv.x;
					float y = i.uv.y;

					if (_DisplayRoundPoints == 1)
					{
						float dist = sqrt(pow((0.5 - x), 2) + pow((0.5 - y), 2));
						if (dist > 0.5) { discard; }

						// Black outline around circle
						//if (dist > 0.42) { col = float4(0, 0, 0, 0); }
					}
					else
					{
						// Black outline around quad
						if (x < 0.06 || x > 0.94 || y < 0.06 || y > 0.94)
							{ col = float4(0, 0, 0, 0); }
					}

					// Fake lighting mask
					float4 mask = tex2D(_MainTex, i.uv);
					col *= mask.x;
					return col;
				}
			ENDCG

			Pass
			{
				Tags { "RenderType" = "Opaque"}
				LOD 100
				CGPROGRAM
				#pragma vertex vert
				//#pragma geometry geom
				#pragma fragment frag
				ENDCG
				}
		}
}