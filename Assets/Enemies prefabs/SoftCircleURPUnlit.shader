Shader "Custom/SoftCircleURPUnlit"
{
    Properties
    {
        _Color ("Tint", Color) = (0.2, 0.8, 1, 0.35)
        _InnerRadius ("Inner Radius", Range(0,1)) = 0.75
        _Feather ("Feather Width", Range(0.001,1)) = 0.15
        _BorderThickness ("Border Thickness", Range(0,1)) = 0.08
        _BorderBoost ("Border Boost", Range(0,3)) = 0.8
        _MainTex ("(Optional) Sprite Tex", 2D) = "white" {}
        _UseSpriteAlpha ("Use Sprite Alpha", Float) = 0
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 1.2
        _PulseAmount ("Pulse Amount", Range(0,0.2)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_SPRITE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _InnerRadius;
                float _Feather;
                float _BorderThickness;
                float _BorderBoost;
                float _UseSpriteAlpha;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 col : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv  = v.uv;
                o.col = v.color;
                return o;
            }

            float remap01(float v, float a, float b)
            {
                return saturate((v - a) / max(1e-5, (b - a)));
            }

            float4 frag (v2f i) : SV_Target
            {
                // uv במרכז
                float2 p = i.uv - 0.5;
                // לשמור על עיגול גם בספרייטים לא ריבועיים
                float aspect = 1.0; // אם צריך, אפשר להעביר מבחוץ
                p.x *= aspect;

                // פולס עדין ברדיוס
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float inner = saturate(_InnerRadius + pulse);
                float d = length(p);

                // אלפא רכה: מלא בתוך inner, דועך לאורך Feather
                float alphaFill = 1.0 - remap01(d, inner, inner + _Feather);

                // שפה/מסגרת מחוזקת
                float borderStart = inner - _BorderThickness;
                float borderMask  = remap01(d, borderStart, inner) * (1.0 - remap01(d, inner, inner+_Feather));
                float border      = pow(saturate(borderMask), 0.6) * _BorderBoost;

                // טקסטורת ספרייט (לא חובה): מכפילים אלפא אם רוצים את צורת הספרייט
                float spriteA = 1.0;
                if (_UseSpriteAlpha > 0.5)
                {
                    float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                    spriteA = tex.a;
                }

                float a = saturate(max(alphaFill, border)) * _Color.a * spriteA * i.col.a;
                float3 c = _Color.rgb * i.col.rgb;

                return float4(c, a);
            }
            ENDHLSL
        }
    }
}
