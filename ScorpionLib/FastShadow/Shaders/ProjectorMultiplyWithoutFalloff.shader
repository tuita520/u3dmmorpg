Shader "FastShadowReceiver/Projector/Multiply Without Falloff" {
	Properties {
		_ShadowTex ("Cookie", 2D) = "gray" {}
		_ClipScale ("Near Clip Sharpness", Float) = 100
		_Alpha ("Shadow Darkness", Range (0, 1)) = 1.0
		_Offset ("Offset", Range (-1, -10)) = -1.0
	}
	Subshader {
		Tags{ "Queue" = "Geometry-1" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Stencil {
			Ref 1
			Comp NotEqual
			Pass Replace
		}
		Pass {
			ZWrite Off
			Fog { Mode Off }
			ColorMask RGB
			Cull Off
			Lighting Off
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -5, -1
 
			CGPROGRAM
			#pragma vertex fsr_vert_projector_nearclip
			#pragma fragment fsr_frag_projector_shadow_nearclip
			#pragma multi_compile FSR_PROJECTOR FSR_RECEIVER
			#pragma multi_compile_fog
			#include "FastShadowReceiver.cginc"
			ENDCG
		}
	}
}
