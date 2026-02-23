Shader "Raven/Fishing/DepthDarkness"
{
    Properties
    {
        _DarknessAlpha ("Darkness Alpha", Range(0, 1)) = 0
        _HookWorldPos ("Hook World Position", Vector) = (0, 0, 0, 0)
        _LightRadius ("Light Radius", Float) = 0
        _LightSoftness ("Light Softness", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+90"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _DarknessAlpha;
            float4 _HookWorldPos;
            float _LightRadius;
            float _LightSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alpha = saturate(_DarknessAlpha);
                float radius = max(_LightRadius, 0.0);
                float softness = max(_LightSoftness, 0.001);
                float mask = 1.0;

                if (radius > 0.001)
                {
                    float2 delta = i.worldPos.xy - _HookWorldPos.xy;
                    float distanceToHook = length(delta);
                    float normalizedDistance = distanceToHook / radius;
                    float softnessScale = max(softness / radius, 0.001);
                    // Build a true center-out falloff, then blend to full darkness outside the radius.
                    float innerGradient = smoothstep(0.0, 1.0, saturate(normalizedDistance));
                    float outerBlend = smoothstep(1.0, 1.0 + softnessScale, normalizedDistance);
                    mask = lerp(innerGradient, 1.0, outerBlend);
                }

                return fixed4(0.0, 0.0, 0.0, alpha * saturate(mask));
            }
            ENDCG
        }
    }
}
