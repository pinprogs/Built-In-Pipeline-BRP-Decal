using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-100)] // BRP Decal 보다 먼저 실행되도록 설정
public class BRPDecalManager : MonoBehaviour
{
    /// <summary> BRP Decal Manager 싱글톤 객체 </summary>
    private static BRPDecalManager instance;
    /// <summary> BRP Decal Manager 싱글톤 객체 </summary>
    public static BRPDecalManager Instance => instance;

    /// <summary> CMD 개장 데칼 허용량 </summary>
    private const int MAX_DECALS_PER_CMD = 100;
    /// <summary> 데칼 material 기본 템플릿, cmd 마다 복사되어 사용 </summary>
    [SerializeField] private Material decalMaterialTempltate;

    /// <summary> 등록된 데칼 리스트 </summary>
    private List<BRPDecal> decals = new();
    /// <summary> 생성된 cmd 리스트 </summary>
    private List<CommandBuffer> commandBuffers = new();
    /// <summary> 생성된 material 리스트 </summary>
    private readonly List<Material> materials = new();

    /// <summary> 메인 카메라 </summary>
    private Camera cam;
    /// <summary> 임시 렌더텍스처 </summary>
    private RenderTexture tempRT;
    /// <summary> 화면 가로, 세로 크기 해상도 변화에 따른 cmd 대응 위함 </summary>
    private int screenWidth, screenHeight;


    void Awake()
    {
        // 싱글톤 
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        // Camera Setting
        cam = Camera.main;
        cam.depthTextureMode = DepthTextureMode.Depth;
    }

    void OnDisable()
    {
        Cleanup();
    }

    void Update()
    {
        UpdateScreenSize();
        UpdateCameraMatrices();
    }

    /// <summary>
    /// 새로운 BRP Decal을 등록
    /// </summary>
    /// <param name="bRPDecal"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Register(BRPDecal decal)
    {
        if (!decals.Contains(decal))
        {
            decals.Add(decal);
            RebuildAll();
        }
    }

    /// <summary>
    /// 기존 BRP Decal 삭제
    /// </summary>
    /// <param name="bRPDecal"></param>
    /// <exception cref="NotImplementedExceptiond"></exception>
    public void Unregister(BRPDecal decal)
    {
        if (decals.Remove(decal))
        {
            RebuildAll();
        }
    }

    /// <summary>
    /// 데칼 재구성 요청
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void RequestRebuild()
    {
        RebuildAll();
    }

    /// <summary>
    /// 매프레임, 카메라 투영 정보를 mateiral에 전달
    /// </summary>
    private void UpdateCameraMatrices()
    {
        if (materials.Count == 0) return;

        Matrix4x4 invProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false).inverse;
        Matrix4x4 invView = cam.cameraToWorldMatrix;

        foreach (var mat in materials)
        {
            mat.SetMatrix("_InverseProjectionMatrix", invProj);
            mat.SetMatrix("_InverseViewMatrix", invView);
        }
    }

    /// <summary>
    /// 화면 비율이 바뀔 때 마다 Render Texture 및 cmd 업데이트
    /// </summary>
    private void UpdateScreenSize()
    {
        bool changed = screenWidth != Screen.width || screenHeight != Screen.height;
        if (!changed) return;

        screenWidth = Screen.width;
        screenHeight = Screen.height;

        if (tempRT != null)
            RenderTexture.ReleaseTemporary(tempRT);

        tempRT = RenderTexture.GetTemporary(screenWidth, screenHeight, 0, RenderTextureFormat.Default);

        RebuildAll();
    }

    /// <summary>
    /// 데칼 그리기 재구성
    /// </summary>
    private void RebuildAll()
    {
        Cleanup();

        if (decals.Count == 0 || cam == null) return;

        if (tempRT == null)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            tempRT = RenderTexture.GetTemporary(screenWidth, screenHeight, 0, RenderTextureFormat.Default);
        }

        int groupCount = Mathf.CeilToInt(decals.Count / (float)MAX_DECALS_PER_CMD);

        Matrix4x4 invProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false).inverse;
        Matrix4x4 invView = cam.cameraToWorldMatrix;

        for (int g = 0; g < groupCount; g++)
        {
            // Material Property Setting 
            int start = g * MAX_DECALS_PER_CMD;
            int count = Mathf.Min(MAX_DECALS_PER_CMD, decals.Count - start);

            var worldToLocal = new Matrix4x4[MAX_DECALS_PER_CMD];
            var colors = new Vector4[MAX_DECALS_PER_CMD];

            for (int i = 0; i < count; i++)
            {
                var decal = decals[start + i];
                worldToLocal[i] = decal.worldToLocalMatrix;
                colors[i] = decal.color;
            }

            var mat = new Material(decalMaterialTempltate);
            mat.SetInt("_DecalCount", count);
            mat.SetMatrixArray("_WorldToLocalArray", worldToLocal);
            mat.SetVectorArray("_DecalColors", colors);
            mat.SetTexture("_BlitTex", tempRT);
            mat.SetMatrix("_InverseProjectionMatrix", invProj);
            mat.SetMatrix("_InverseViewMatrix", invView);
            materials.Add(mat);

            var cmd = new CommandBuffer { name = $"BRP Decal Pass [{g}]" };
            cmd.Blit(BuiltinRenderTextureType.CameraTarget, tempRT);
            cmd.Blit(tempRT, BuiltinRenderTextureType.CameraTarget, mat);
            cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cmd);
            commandBuffers.Add(cmd);
        }
    }

    /// <summary>
    /// 등록된 CMD 전체 해제
    /// </summary>
    private void Cleanup()
    {
        if (cam == null || commandBuffers == null || commandBuffers.Count == 0) return;

        foreach (var cmd in commandBuffers)
        {
            cam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, cmd);
            cmd.Release();
        }

        commandBuffers.Clear();

        foreach (var mat in materials)
        {
            Destroy(mat);
        }
        materials.Clear();

        if (tempRT != null)
        {
            RenderTexture.ReleaseTemporary(tempRT);
            tempRT = null;
        }
    }

}
