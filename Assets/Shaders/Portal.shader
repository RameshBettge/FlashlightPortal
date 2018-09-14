// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "_MyShaders/"
{
	Properties
	{
		[Header(Scanner)]
		_PortalStrength("PortalStrength", Range(0, 1)) = 0.2

		_RenderTex("RenderTexture", 2D) = "white" {}

		_DiffuseCol("Main Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "Queue" = "Geometry"}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, -1 //Avoids z-Fighting

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"		

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD0;
			};

			float _PortalStrength;

			fixed4 _DiffuseCol;

			sampler2D _RenderTex;


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			//Transparent but still occupies depth buffer
				fixed4 invis = (0,0,0,0);

				fixed4 renderSample = tex2D(_RenderTex, i.screenPos.xy / i.screenPos.w);

				fixed4 output = lerp(_DiffuseCol, renderSample, _PortalStrength);

				return output;
			}
			ENDCG
		}
	}
}
