Shader "_MyShaders/WorldspaceUnwrap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{

		Pass
		{


			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				//Get the vertex positions in model space
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				//SV_POSITION is the position of the vertex in projection Space
				float4 vertex : SV_POSITION;
				float3 worldPos : Texcoord0;
				float3 normal : NORMAL;
			};

			sampler2D _MainTex;
			//Needed for  TRANSFORM_TEX. ST stands for ScaleTranslate
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;

				//Transforms the vertex' position into projection space
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.normal = UnityObjectToWorldNormal(v.normal);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target //SV_target is the target. Without it the pixels won't be drawn.
			{
				//// sample the texture
				float2 uv = float2(0, 0);

				if(abs(i.normal.x) > 0.5)	
				{
				uv = i.worldPos.yz;
				}
				else if(abs(i.normal.y) > 0.5)
				{
					uv = i.worldPos.xz;
				}
				else
				{
					uv = i.worldPos.xy;
				}
				
				uv = TRANSFORM_TEX(uv, _MainTex);

				fixed4 col = tex2D(_MainTex, uv);

				return col;
			}

			ENDCG
		}
	}
}
