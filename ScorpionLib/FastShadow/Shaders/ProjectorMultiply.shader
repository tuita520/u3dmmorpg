Shader "FastShadowReceiver/Projector/Multiply" {
	Properties {
		_ShadowTex ("Cookie", 2D) = "gray" {}
		_Color ("Main Color", Color) = (1,1,1,1)
		_Alpha ("Shadow Darkness", Range (0, 1)) = 1.0
		_Offset ("Offset", Range (-1, -10)) = -1.0
		_FillPercent ("_FillPercent", Float) = 0
		_FillColor ("Fill Color", Color) = (1,1,1,1)
		_Orgin ("_Orgin", Vector) = (0.5,0,0,0)
	}
	Subshader {
		Tags {"Queue"="Transparent-1"}
		Pass {
			ZWrite Off
			Fog { Color (1, 1, 1) }
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, [_Offset]
 
			CGPROGRAM
			#pragma vertex fsr_vert_projector
			#pragma fragment fsr_frag_projector_shadow_falloff
			#pragma multi_compile FSR_PROJECTOR FSR_RECEIVER
			#pragma multi_compile_fog
			#include "FastShadowReceiver.cginc"
			ENDCG
		}
	}
	Fallback "FastShadowReceiver/Projector/Multiply Without Falloff"
}
