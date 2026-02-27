Shader "Raven/Fishing/OceanSkyGradient"
{
    Properties
    {
        _SurfaceY ("Surface Y", Float) = 0
        _SkyHeightMeters ("Sky Height (Meters)", Float) = 50
        _OceanDepthMeters ("Ocean Depth (Meters)", Float) = 5000
        _SkyLightColor ("Sky Light Color", Color) = (0.28, 0.34, 0.46, 1.00)
        _SkyDarkColor ("Sky Dark Color", Color) = (0.015, 0.02, 0.07, 1.00)
        _SkyHorizontalInfluence ("Sky Horizontal Influence", Range(0, 1)) = 0.95
        _SkyWavelengthMeters ("Sky Wavelength (Meters)", Float) = 260
        _SkyCycleSeconds ("Sky Cycle Seconds", Float) = 600
        _OceanShallowColor ("Ocean Shallow Color", Color) = (0.08, 0.16, 0.28, 1.00)
        _OceanDeepColor ("Ocean Deep Color", Color) = (0, 0.005, 0.015, 1.00)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _SurfaceY;
            float _SkyHeightMeters;
            float _OceanDepthMeters;
            float4 _SkyLightColor;
            float4 _SkyDarkColor;
            float _SkyHorizontalInfluence;
            float _SkyWavelengthMeters;
            float _SkyCycleSeconds;
            float4 _OceanShallowColor;
            float4 _OceanDeepColor;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float y = i.worldPos.y;
                float3 color;

                if (y >= _SurfaceY)
                {
                    float skyHeight = max(0.001, _SkyHeightMeters);
                    float skyT = saturate((y - _SurfaceY) / skyHeight);
                    float3 verticalSkyColor = lerp(_SkyLightColor.rgb, _SkyDarkColor.rgb, skyT);

                    float skyCycleSeconds = max(1.0, _SkyCycleSeconds);
                    float wavelength = max(1.0, _SkyWavelengthMeters);
                    float cycleT = frac(_Time.y / skyCycleSeconds);
                    float phase = ((i.worldPos.x / wavelength) + cycleT) * 6.28318530718;
                    float horizontalT = 0.5 + (0.5 * sin(phase));
                    float3 horizontalSkyColor = lerp(_SkyLightColor.rgb, _SkyDarkColor.rgb, horizontalT);

                    color = lerp(
                        verticalSkyColor,
                        horizontalSkyColor,
                        saturate(_SkyHorizontalInfluence));
                }
                else
                {
                    float oceanDepth = max(0.001, _OceanDepthMeters);
                    float oceanT = saturate((_SurfaceY - y) / oceanDepth);
                    color = lerp(_OceanShallowColor.rgb, _OceanDeepColor.rgb, oceanT);
                }

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
}
