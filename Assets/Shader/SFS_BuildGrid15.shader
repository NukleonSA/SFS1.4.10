// 建造网格 + 中心线 —— 用 1.5 的原版贴图 Build Background，4 顶点四边形
//
// 为什么不用 SpriteRenderer(Tiled)：那个模式按图块生成顶点，2048x2048 需要 52 万图块，
// 超 65535 顶点上限直接崩。本着色器把同一张贴图按世界坐标采样，4 个顶点就够。
//
// 中心线也在这里画：它和网格在同一个四边形、同一个材质里，
// 保证随蓝图一起缩放（之前用 GUI 屏幕空间画 -> 线宽不随缩放变化，是错的）。
Shader "SFS/BuildGrid15"
{
    Properties
    {
        _MainTex ("1.5 Build Background", 2D) = "white" {}
        _TexWorldSize ("Texture World Size", Float) = 4
        _Tint ("Tint", Color) = (1,1,1,1)

        // 中心线
        _CenterX ("Center X (world)", Float) = 0
        _CenterWidth ("Center Line Width (world)", Float) = 3
        _CenterColor ("Center Line Color", Color) = (0,0,0,0.22)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _TexWorldSize;
            fixed4 _Tint;
            float _CenterX;
            float _CenterWidth;
            fixed4 _CenterColor;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 world : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 纯色模式（_TexWorldSize <= 0）
                fixed4 c;
                if (_TexWorldSize <= 0.0)
                {
                    c = _Tint;
                }
                else
                {
                    c = tex2D(_MainTex, i.world / max(0.0001, _TexWorldSize)) * _Tint;
                }

                // 中心线：半透明黑，直接叠上去（1.5 的做法）
                if (_CenterWidth > 0.0001)
                {
                    float halfW = _CenterWidth * 0.5;
                    if (i.world.x >= _CenterX - halfW && i.world.x <= _CenterX + halfW)
                    {
                        float a = _CenterColor.a;
                        c.rgb = lerp(c.rgb, _CenterColor.rgb, a);
                        c.a = max(c.a, a);
                    }
                }
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
