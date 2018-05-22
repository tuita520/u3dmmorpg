//
// FogMacro.cginc
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

#if !defined(FSR_FOGMACRO_CGINC_DEFINED)
#define FSR_FOGMACRO_CGINC_DEFINED

#include "UnityCG.cginc"

#if !defined(UNITY_FOG_COORDS)
	#define UNITY_FOG_COORDS(idx)
#endif
#if !defined(UNITY_TRANSFER_FOG)
	#define UNITY_TRANSFER_FOG(o,outpos)
#endif
#if !defined(UNITY_APPLY_FOG_COLOR)
	#define UNITY_APPLY_FOG_COLOR(coord,col,fogCol)
#endif
#if !defined(UNITY_APPLY_FOG)
	#define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,unity_FogColor)
#endif

#endif // !defined(FSR_FOGMACRO_CGINC_DEFINED)
