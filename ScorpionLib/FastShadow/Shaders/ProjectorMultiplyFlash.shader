Shader "FastShadowReceiver/Projector/MultiplyFlash" {
	Properties {
		_ShadowTex ("Cookie", 2D) = "gray" {}
		_Color ("Main Color", Color) = (1,1,1,1)
		_Alpha ("Shadow Darkness", Range (0, 1)) = 1.0
		_Offset ("Offset", Range (-1, -10)) = -1.0
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
			#pragma vertex fsr_vert_projector_flash
			#pragma fragment fsr_frag_projector_flash
			#pragma multi_compile FSR_PROJECTOR FSR_RECEIVER
			#pragma multi_compile_fog
			#include "FastShadowReceiver.cginc"
			ENDCG
		}
	}
	Fallback "FastShadowReceiver/Projector/Multiply Without Falloff"
}
