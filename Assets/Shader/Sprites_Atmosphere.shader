// 重建: Sprites/Atmosphere
// 【来源】已按 APK 中该着色器编译后字节码逐行还原
//   (m_ParsedForm -> compressedBlob -> LZ4 解压 -> GLES3 GLSL)
// 网格 UV 由 PlanetManager.CreateAtmosphere 写入:
//   uv[i] = (i, 1)  (i = 顶点序号, 即环绕角度), uv[0] = (0,0)
//   三角扇插值后: uv.y = 径向比例(圆心0 -> 边缘1), uv.x = 角度序号
// 渲染状态取自同一处 m_ParsedForm
Shader "Sprites/Atmosphere"
{
    Properties
    {
        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _GradientMultiplier ("Gradient Multiplier", Float) = 1
        _CouldTex ("Cloud Texture", 2D) = "white" {}
        _CloudStartY ("Cloud Start Y", Float) = 0
        _CloudSizeY ("Cloud Size Y", Float) = 1
        _CloudSizeX ("Cloud Size X", Float) = 1
        _CloudColor ("Cloud Color", Color) = (1,1,1,1)
        _PosX ("Position X", Float) = 0
        _Alpha ("Alpha", Float) = 1
        _Fog ("Atmosphere Color", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha
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
                fixed4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                fixed4 color : COLOR;
            };
            sampler2D _GradientTex;
            sampler2D _CouldTex;
            float4 _CloudColor;
            float _GradientMultiplier;
            float _CloudStartY;
            float _CloudSizeY;
            float _CloudSizeX;
            float _PosX;
            float _Alpha;
            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 back;
                // 渐变: (0, uv.y*(_GradientMultiplier+1) - _GradientMultiplier)
                float2 gUV;
                gUV.x = 0.0;
                gUV.y = ((i.uv.y * (_GradientMultiplier + 1.0)) - _GradientMultiplier);
                fixed4 grad = tex2D(_GradientTex, gUV);
                back.w = grad.w;

                // 云: x = (uv.x/uv.y)*_CloudSizeX + _PosX ; y = (uv.y*(_CloudStartY+1) - _CloudStartY)*_CloudSizeY
                float2 cUV;
                cUV.x = (((i.uv.x / i.uv.y) * _CloudSizeX) + _PosX);
                cUV.y = (((i.uv.y * (_CloudStartY + 1.0)) - _CloudStartY) * _CloudSizeY);
                fixed4 cloud = tex2D(_CouldTex, cUV) * _CloudColor;
                cloud.w = cloud.w * _Alpha;

                // 合成: 渐变 * ((1-云.a)*渐变.a) + 云.rgb*云.a
                back.xyz = ((grad.xyz * ((1.0 - cloud.w) * grad.w)) + (cloud.xyz * cloud.w));
                back *= i.color;
                return back;
            }
        ENDCG
        }
    }
}
