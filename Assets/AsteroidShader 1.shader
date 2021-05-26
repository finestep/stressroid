Shader "Custom/AsteroidShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("DebugColor", 2D) = "white" {}
	}
		SubShader
	{

		Tags { "RenderType" = "Opaque" "PreviewType" = "Plane" }
		pass
		{
			Name "VertexColor"
			CGPROGRAM
			#pragma vertex wfiVertCol
			#pragma fragment passThrough

			#pragma target 2.0
			#include "UnityCG.cginc"

			 struct VertOut
			 {
				 float4 position : POSITION;
				 float4 color : COLOR;
			 };

			struct VertIn
			{
				float4 vertex : POSITION;
				int4 color : COLOR;
			};

			 VertOut wfiVertCol(VertIn input, float3 normal : NORMAL)
			 {
				 VertOut output;
				 output.position = UnityObjectToClipPos(input.vertex);
				 output.color = input.color;
				 return output;
			 }

			 struct FragOut
			 {
				 int4 color : COLOR;
			 };


			 float4 _Color;


			 FragOut passThrough(VertOut v)
			 {
				 FragOut output;
				 output.color = v.color;
				 return output;
			 }

			 ENDCG
		 }
	pass
		{
			Name "DebugTex"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0


			#include "UnityCG.cginc"
				#include "UnityUI.cginc"

			sampler2D _MainTex;
			fixed4 _Color;
			float4 _MainTex_ST;

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
			};

			struct VertIn
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			 v2f vert(VertIn input, float3 normal : NORMAL)
			 {
				 v2f output;
				 output.vertex = UnityObjectToClipPos(input.vertex);
				 output.color = input.color*_Color;
				 output.texcoord = TRANSFORM_TEX(input.uv, _MainTex);
				 return output;
			 }



			 fixed4 frag(v2f IN) : SV_Target
			 {
				 half4 color = (tex2D(_MainTex, IN.texcoord) );

				 return color;
			 }

			 ENDCG
		 }
	}
}
