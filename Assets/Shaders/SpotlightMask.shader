Shader "UI/SpotlightMask"
{
    Properties
    {
        _OverlayColor ("Overlay Color", Color) = (0, 0, 0, 0.7)
        _Center ("Center (viewport UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.12
        _Feather ("Feather", Float) = 0.03
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _OverlayColor;
            float4 _Center;
            float _Radius;
            float _Feather;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Correct for aspect ratio so the hole is circular, not elliptical
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv = i.uv;
                float2 center = _Center.xy;

                float2 diff = uv - center;
                diff.x *= aspect;

                float dist = length(diff);
                float adjustedRadius = _Radius * aspect; // scale radius too

                // Smooth edge: fully transparent inside, overlay outside
                float alpha = smoothstep(adjustedRadius - _Feather, adjustedRadius + _Feather, dist);

                return float4(_OverlayColor.rgb, _OverlayColor.a * alpha);
            }
            ENDCG
        }
    }
}
