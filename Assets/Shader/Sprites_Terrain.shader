// 重建: Sprites/Terrain
// 【来源】本文件已按 APK 中该着色器的编译后字节码逐行还原
//   (APK -> Shader.m_ParsedForm -> compressedBlob -> LZ4 解压 -> GLES3 GLSL, 完整可读)
// 渲染状态亦取自同一处 m_ParsedForm:
//   QUEUE=Transparent, RenderType=Opaque, IGNOREPROJECTOR=true, PreviewType=Plane
//   Blend One Zero, ZWrite Off, ZTest LEqual, Cull Off, Lighting Off
Shader "Sprites/Terrain"
{
    Properties
    {
        _PlanetTexture ("Planet Texture", 2D) = "white" {}
        _PlanetTextureCutout ("Planet Texture Cutout", Float) = 1
        _TextureA ("Texture A", 2D) = "white" {}
        _TextureB ("Texture B", 2D) = "white" {}
        _TextureTerrain ("Texture Terrain", 2D) = "white" {}
        _RepeatA ("Repeat A", Vector) = (1,1,1,1)
        _RepeatB ("Repeat B", Vector) = (1,1,1,1)
        _RepeatTerrain ("Repeat Terrain", Vector) = (1,1,1,1)
        _SurfaceSize ("Texture Transition", Float) = 1
        _Min ("Min fade", Float) = 0
        _Max ("Max fade", Float) = 1
        _ShadowSize ("Transition shade", Float) = 1
        _ShadowIntensity ("Shade intensity", Float) = 1
        _Fog ("Atmosphere Color", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Opaque" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend One Zero
        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float2 uv2    : TEXCOORD1;
                float2 uv3    : TEXCOORD2;
                fixed4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 t0  : TEXCOORD0;   // mesh.uv
                float2 t1  : TEXCOORD1;   // mesh.uv2
                float2 t2  : TEXCOORD2;   // mesh.uv3 (x 已乘 _ShadowIntensity)
            };
            sampler2D _PlanetTexture;
            float _PlanetTextureCutout;
            sampler2D _TextureA;
            float4 _RepeatA;
            sampler2D _TextureB;
            float4 _RepeatB;
            sampler2D _TextureTerrain;
            float4 _RepeatTerrain;
            float _SurfaceSize;
            float _Max;
            float _Min;
            float _ShadowSize;
            float _ShadowIntensity;
            float4 _Fog;
            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.t0 = v.uv;
                o.t1 = v.uv2;
                o.t2.x = v.uv3.x * _ShadowIntensity;   // 原版: 顶点阶段就乘上
                o.t2.y = v.uv3.y;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                // 原版: 纹理坐标由 mesh.uv 派生 (x 按 (1-y) 归一, y 保留)
                float2 tc;
                tc.x = i.t0.x / (1.0 - i.t0.y);
                tc.y = i.t0.y;
                // 过渡量(基于 uv.y)
                float t4 = clamp(i.t0.y * _SurfaceSize, _Min, _Max);

                fixed4 A = tex2D(_TextureA, tc * _RepeatA.xy);
                fixed4 B = tex2D(_TextureB, tc * _RepeatB.xy);
                fixed4 T = tex2D(_TextureTerrain, tc * _RepeatTerrain.xy);
                // 星球贴图: uv2 * cutout + 0.5
                fixed4 P = tex2D(_PlanetTexture, i.t1 * _PlanetTextureCutout + 0.5);

                // 地表混合(原版表达式逐项对应)
                float mixv = ((A.r * (1.0 - i.t2.y) + B.r * i.t2.y) * (1.0 - t4)
                              + T.r * (0.95 * t4)) * 2.0
                             * (1.0 + i.t2.x * (1.0 - min(1.0, i.t0.y * _ShadowSize)));

                fixed3 col = P.rgb * mixv;
                col = col * (1.0 - _Fog.w) + _Fog.xyz * _Fog.w;
                return fixed4(col, 1.0);
            }
        ENDCG
        }
    }
}
