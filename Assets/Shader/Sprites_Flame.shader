// 逐行还原自原版底包 SFS1.4_mod_Pro\原始底包\2.apk (Unity 2017.4.1f1) 的 Sprites/Flame 编译产物
// 原版片元(hlslcc 还原, 各平台变体一致):
//   u_xlat0.x = uv.y * _Multiplier;  u_xlat0.y = u_xlat0.x * 0.4;  u_xlat0.x = _Time.w;
//   noise = texture(_PerlinMap, u_xlat0.xy).x * 2.0 - 1.0;          // 噪声 -> [-1,1]
//   x = (uv.x*4 - 2) + noise*3*(1 - uv.y);                          // 水平扰动, 越靠上越不抖
//   x = x * (uv.y*0.8 + 0.2);   x = (x + 1) * 0.5;
//   c = texture(_MainTex, vec2(x, uv.y)) * COLOR;
//   a = c.w * (uv.y + 0.2);
//   SV_Target0.xyz = a * c.xyz;  SV_Target0.w = a;                  // 预乘
// 原版结构: 属性 3 项 (无 _Color/_AlphaTex); Blend One OneMinusSrcAlpha; ZWrite Off; Cull Off
// 此前重建版误写成 c.a *= saturate(_PerlinMap.r * _Multiplier), 会把尾焰侵蚀残缺。
Shader "Sprites/Flame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _PerlinMap ("Shade Texture", 2D) = "white" {}
        _Multiplier ("Multiplier", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _PerlinMap;
            float _Multiplier;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;                 // 原版顶点不做 _MainTex_ST 变换
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 噪声: 横向位置用 _Time.w 随时间滚动, 纵向随 uv.y 拉伸
                float2 noiseUv = float2(_Time.w, i.uv.y * _Multiplier * 0.4);
                float noise = tex2D(_PerlinMap, noiseUv).x * 2.0 - 1.0;   // 映射到 [-1,1]
                // 水平扰动: 火焰根部抖动大、顶部逐渐收拢
                float x = (i.uv.x * 4.0 - 2.0) + noise * 3.0 * (1.0 - i.uv.y);
                x = x * (i.uv.y * 0.8 + 0.2);
                x = (x + 1.0) * 0.5;
                fixed4 c = tex2D(_MainTex, float2(x, i.uv.y)) * i.color;
                // 纵向淡出: 底部保留 0.2
                fixed a = c.a * (i.uv.y + 0.2);
                return fixed4(c.rgb * a, a);   // 预乘输出
            }
        ENDCG
        }
    }
}
