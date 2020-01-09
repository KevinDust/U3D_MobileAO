﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PostEffect_AO : MonoBehaviour
{
    public enum AOType
    {
        SSAO = 0,
        HBAO = 1
    }
    public AOType _AOType;
    private AOType oldType;
    private Material _curAOMat;
    public Material hbaoMat;
    public Material ssaoMat;
    private Camera _cam;

    [Header("------SSAO------")]
    [Range(0, 10)]
    public float radiusSSAO = 1;
    public Texture2D noiseTex;
    public float noiseScale = 1;
    public float biasSSAO = 0.3f;    //减少self shadowing
    [Range(0, 1)]
    public float minDepth = 0.3f;
    [Range(1, 100)]
    public float attenuationSSAO = 2; //衰减系数
    [Range(0, 1)]
    public float intensity = 1;	//ao亮度

    [Range(1, 30)]
    public int sampleNum = 10;
    [Header("------HBAO------")]
    [Range(1, 30)]
    public int stepNum = 10;
    public float radiusHBAO = 1;

    [Range(0, 1)]
    public float stepRadiusHBAO = 1;

    [Range(0, 2)]
    public float intensityHBAO = 1;	//ao亮度

    [Range(1, 5)]
    public int minStepPixelNumHBAO =1;

    [Range(0, 2)]
    public float biasHBAO = 0.3f;    //减少self shadowing

    [Range(0, 2)]
    public float bias2HBAO = 0.5f;

[Range(1, 10)]
    public float attenuationHBAO = 2; //衰减系数


    private int oldSampleNum = -1;
    // Start is called before the first frame update
    void Start()
    {
        _cam = Camera.main;
        InitAOData();
        oldType = _AOType;
    }

    private void OnEnable()
    {
        _cam = Camera.main;
        sampleDirs_HBAO = null;
        InitAOData();
        oldType = _AOType;
    }

    void InitAOData()
    {
        switch (_AOType)
        {
            case AOType.SSAO:
                if (oldSampleNum != sampleNum) {
                    GeneratHemiSphereNoise(sampleNum);
                    oldSampleNum = sampleNum;
                }
                _cam.depthTextureMode = DepthTextureMode.DepthNormals;

                _curAOMat = ssaoMat;
                break;
            case AOType.HBAO:
                GenerateSampleDir_HBAO();
                _curAOMat = hbaoMat;
                _cam.depthTextureMode = DepthTextureMode.Depth;

                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (oldType != _AOType)
        {
            InitAOData();
            oldType = _AOType;
        }
    }
    public Transform Light;
    public Color aoColor;
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _curAOMat.SetFloat("_Intensity", intensity);


        switch (_AOType)
        {
            case AOType.SSAO:
                _curAOMat.SetFloat("_NoiseScale", noiseScale);
                _curAOMat.SetTexture("_RandomVectorTex", noiseTex);
                _curAOMat.SetFloat("_MinDepth", minDepth);
                _curAOMat.SetInt("_SampleNum", sampleNum);
                _curAOMat.SetVectorArray("_VectorArray", kernels);
                _curAOMat.SetFloat("_Radius", radiusSSAO);
                _curAOMat.SetFloat("_Bias", biasSSAO);
                _curAOMat.SetFloat("_Attenuation", attenuationSSAO);

                Graphics.Blit(source, destination, _curAOMat);

                break;
            case AOType.HBAO:

                _curAOMat.SetInt("_StepNum", stepNum);
                _curAOMat.SetFloat("_StepRadius", stepRadiusHBAO);

                _curAOMat.SetFloat("_Radius", radiusHBAO);
                _curAOMat.SetFloat("_Bias", biasHBAO);

                _curAOMat.SetFloat("_Bias2", bias2HBAO);

                _curAOMat.SetFloat("_Attenuation", attenuationHBAO);
                Matrix4x4 P = GL.GetGPUProjectionMatrix(_cam.projectionMatrix, false);

                _curAOMat.SetMatrix("Matrix_I_P", P.inverse);
                _curAOMat.SetMatrix("Matrix_P", (_cam.projectionMatrix));
                _curAOMat.SetInt("_ArrayNum", sampleDirs_HBAO.Length);
                _curAOMat.SetVectorArray("_SampleDirArray", sampleDirs_HBAO);
                _curAOMat.SetFloat("_Intensity", intensityHBAO);
                _curAOMat.SetFloat("_MinStepPixelNum", minStepPixelNumHBAO);
                Vector3 lightDir = Vector3.one;
                if (Light != null)
                {
                    lightDir = _cam.worldToCameraMatrix.MultiplyVector(Light.forward);
                }
                _curAOMat.SetVector("_ViewLightDir", lightDir);
                _curAOMat.SetColor("_AOColor", aoColor);
                Graphics.Blit(source, destination, _curAOMat);

                break;
        }

    }

    public Vector4[] kernels;
    private void GeneratHemiSphereNoise(int num)
    {
        kernels = new Vector4[num];
        for (int i = 0; i < num; i++)
        {
            Random.InitState(i);
            kernels[i] = Random.insideUnitSphere;
            kernels[i].Normalize();
            float scale = (float)(i / sampleNum);
            scale = Mathf.Lerp(0.1f, 1.0f, scale * scale);
            kernels[i] *= scale;
        }
        Debug.Log("Set array");
    }
    private Vector4[] sampleDirs_HBAO = null;
    private void GenerateSampleDir_HBAO()
    {
        if ((sampleDirs_HBAO != null && sampleDirs_HBAO.Length>0 && sampleDirs_HBAO[0] != Vector4.zero))
            return;
        // 1 0 1 0 1 0 1 0 1
        // 0 0 0 0 0 0 0 0 0
        // 1 0 0 0 0 0 0 0 1
        // 0 0 0 0 0 0 0 0 0
        // 1 0 0 0 o 0 0 0 1
        // 0 0 0 0 0 0 0 0 0
        // 1 0 0 0 0 0 0 0 1
        // 0 0 0 0 0 0 0 0 0
        // 1 0 1 0 1 0 1 0 1

        //应该调整顺序 尽量cache hit
        // 从(4,-2)逆时针开始
        Vector2[] dirs = new Vector2[] {
            //new Vector2(4.0f,-2.0f).normalized,
            //new Vector2(4.0f,0.0f).normalized,
            //new Vector2(4.0f,2.0f).normalized,

            new Vector2(4.0f,4.0f).normalized,

            //new Vector2(2.0f,4.0f).normalized,
            new Vector2(0.0f,4.0f).normalized,
            //new Vector2(-2.0f,4.0f).normalized,

            //new Vector2(-4.0f,4.0f).normalized,

            //new Vector2(-4.0f,2.0f).normalized,
            //new Vector2(-4.0f,0.0f).normalized,
            //new Vector2(-4.0f, -2.0f).normalized,

            new Vector2(-4.0f,-4.0f).normalized,

            new Vector2(-2.0f,-4.0f).normalized,
            //new Vector2(0.0f,-4.0f).normalized,
            //new Vector2(2.0f,-4.0f).normalized,

            //new Vector2(4.0f,-4.0f).normalized
        };
        sampleDirs_HBAO = new Vector4[dirs.Length / 2];
        for (int i = 0; i < dirs.Length; i+=2)
        {
            sampleDirs_HBAO[i / 2] = new Vector4(dirs[i].x, dirs[i].y, dirs[i + 1].x, dirs[i + 1].y);
        }
    }
}
