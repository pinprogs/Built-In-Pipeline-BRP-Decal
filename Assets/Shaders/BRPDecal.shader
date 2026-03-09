Shader "Custom/BRPDecal"
{
    Properties
    {
        _BlitTex ("Blit Tex", 2D) = "white" {}                          
        _DecalCount ("Decal Count", Int) = 0                            
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
            sampler2D _BlitTex;                    
            sampler2D _CameraDepthTexture;        

            // 카메라 변환 행렬
            float4x4 _InverseProjectionMatrix;
            float4x4 _InverseViewMatrix;

            // 데칼 데이터
            float4x4 _WorldToLocalArray[100];       
            int _DecalCount;                        
            float4 _DecalColors[100];               

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
