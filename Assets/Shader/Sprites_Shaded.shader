Shader "Sprites/Shaded"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShadeTex ("Shade Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 3)) = 1.45
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _ShadeTex;
            float4 _MainTex_ST;
            float4 _ShadeTex_ST;
            float _Intensity;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
                float3 uv1 : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 uv : TEXCOORD0;
                float3 uv1 : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float2 u0 = TRANSFORM_TEX(v.uv.xy, _MainTex);
                float2 u1 = TRANSFORM_TEX(v.uv1.xy, _ShadeTex);
                o.uv = float3(u0, v.uv.z);
                o.uv1 = float3(u1, v.uv1.z);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 原版透视校正: uv.xy / (uv.z + 1)
                float2 uv0 = i.uv.xy / (i.uv.z + 1.0);
                float2 uv1 = i.uv1.xy / (i.uv1.z + 1.0);
                float shadeAlpha = tex2D(_ShadeTex, uv1).a;
                float shadeFactor = 1.0 - shadeAlpha * _Intensity;
                fixed4 c = tex2D(_MainTex, uv0);
                fixed4 col = c * i.color;
                fixed4 result;
                // 可见性规则:
                //  - UV 在 [0,1] 内 (绝大多数零件): 纹理 alpha 当遮罩 -> 图集透明边距天然隐形
                //  - UV 越界 (整流罩背板 uv=3 -> V=1.059, 超出 Tex_White 50px 矩形):
                //    越界处取不到有效纹素, 纹理 alpha 不可信 -> 回退用顶点 alpha
                fixed inRange = step(0.0, uv0.x) * step(uv0.x, 1.0)
                              * step(0.0, uv0.y) * step(uv0.y, 1.0);
                fixed baseAlpha = lerp(i.color.a, col.a, inRange);
                // 阴影只变深、不改透明度: alpha 不乘 shadeFactor
                result.rgb = shadeFactor * col.rgb * baseAlpha;   // 预乘 rgb
                result.a   = baseAlpha;
                return result;
            }
            ENDCG
        }
    }
}
