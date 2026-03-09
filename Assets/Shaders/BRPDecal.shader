Shader "Custom/BRPDecal"
{
    Properties
    {
        _BlitTex ("Blit Tex", 2D) = "white" {}                          // 원본 씬 컬러 텍스처
        _DecalCount ("Decal Count", Int) = 0                            // 현재 적용할 데칼 수
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Name "DecalPass"
            ZTest Always Cull Back ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 텍스처 및 버퍼
            sampler2D _BlitTex;                     // 원본 화면
            sampler2D _CameraDepthTexture;          // 뎁스 텍스처

            // 카메라 변환 행렬
            float4x4 _InverseProjectionMatrix;
            float4x4 _InverseViewMatrix;

            // 데칼 데이터
            float4x4 _WorldToLocalArray[100];       // 데칼 로컬 변환 (AABB 계산용)
            int _DecalCount;                        // 데칼 수
            float4 _DecalColors[100];               // 데칼 컬러 (RGBA)

            // NDC → View → World 변환 함수
            float3 ReconstructWorldPosition(float2 uv)
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);

                float x = uv.x * 2 - 1;
                float y = uv.y * 2 - 1;
                float z = depth;

                float4 ndcPos = float4(x, y, z, 1);
                float4 viewPos = mul(_InverseProjectionMatrix, ndcPos);
                viewPos /= viewPos.w;
                float4 worldPos = mul(_InverseViewMatrix, viewPos);

                return worldPos.xyz;
            }

            // 데칼 적용 프래그먼트
            half4 frag(v2f_img i) : SV_Target
            {
                half4 color = tex2D(_BlitTex, i.uv); // 기본 화면 색상
                float3 worldPos = ReconstructWorldPosition(i.uv);

                // 데칼 루프
                for (int d = 0; d < _DecalCount; d++)
                {
                    float3 localPos = mul(_WorldToLocalArray[d], float4(worldPos, 1)).xyz;

                    // AABB 내에 있을 때만 처리
                    bool inside = all(abs(localPos) <= 0.5);

                    if (inside)
                    {
                        float4 decalColor = _DecalColors[d];
                        color.rgb = lerp(color.rgb, decalColor.rgb, decalColor.a);
                    }
                }

                return color;
            }

            ENDHLSL
        }
    }
}
