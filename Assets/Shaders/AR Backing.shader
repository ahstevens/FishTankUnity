Shader "CCOM/AR Backing" {

Properties {
    [HDR] _Color        ("Main Color", Color) = (1,1,1,1)
    [Enum(None,0,Add,1,Multiply,2, Subtract,3)] _Blend("Blend mode subset", Int) = 0
    _MainTex            ("Main Texture", 2D) = "white" {}
    _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
}

SubShader
{

    Tags
    {
        "RenderType" = "Opaque"
    }

    LOD 100
    Color[_Color]

    Lighting Off
    Cull Off
    ZWrite On
    ZTest Always

    Pass
    {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        // make fog work
        #pragma multi_compile_fog
        #include "UnityCG.cginc"


        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            UNITY_FOG_COORDS(1)
            float4 vertex : SV_POSITION;
        };

        sampler2D _MainTex;
        float4 _MainTex_ST;
        float4 _Color;
        float _Blend;
        float _Cutoff;


        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            UNITY_TRANSFER_FOG(o,o.vertex);
            return o;
        }

        half4 frag(v2f i, out float depth : SV_Depth) : SV_Target
        {
            depth = UNITY_NEAR_CLIP_VALUE;

            // sample the texture
            fixed4 col = tex2D(_MainTex, i.uv);

            col *= UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);

            clip(col.a - _Cutoff);

            switch (_Blend) {
                case 1:
                    col = tex2D(_MainTex, i.uv) + _Color;
                    return col;
                case 2:
                    col = tex2D(_MainTex, i.uv) * _Color;
                    return col;
                case 3:
                    col = tex2D(_MainTex, i.uv) - _Color;
                    return col;
            }

            // apply fog
            UNITY_APPLY_FOG(i.fogCoord, col); 

            return col;
        }


        ENDCG
    }
}
}
