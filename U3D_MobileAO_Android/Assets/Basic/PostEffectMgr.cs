using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostEffectMgr : MonoBehaviour
{

    private static PostEffectMgr _instance;
    public static PostEffectMgr Instance
    {
        get
        {
            return _instance;
        }
    }

    //深度预处理器
    public bool isUseDepthBuffer = true;
    public int depthBufferRatio = 4;
    public DepthBufferMgr depthBufferMgr;
    private DepthTextureMode globalDepthMode = DepthTextureMode.None;

    //后处理栈
    //public PostEffect_DofBokeh_New effect_DofBokeh;
    //public PostEffect_AdaptiveBloom effect_Bloom;

    public PostEffectUtil_Mobile.MobileQuality curQuality;

    private bool isDownResolution = false;
    private float resolutionRatio = 1.0f;

    private int screenWidth;
    private int screenHeight;

    public void SetDepthBufferRatio_1()
    {
        depthBufferMgr.isInit = false;
        depthBufferMgr.ReleaseBuffer();
        depthBufferMgr.Init(screenWidth, screenHeight, 1);
    }

    public void SetDepthBufferRatio_2()
    {
        depthBufferMgr.isInit = false;
        depthBufferMgr.ReleaseBuffer();
        depthBufferMgr.Init(screenWidth, screenHeight, 2);
    }

    public void SetDepthBufferRatio_4()
    {
        depthBufferMgr.isInit = false;
        depthBufferMgr.ReleaseBuffer();
        depthBufferMgr.Init(screenWidth, screenHeight, 4);
    }

    // Start is called before the first frame update
    void Start()
    {
        _instance = this;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        //降分辨率
        resolutionRatio = PostEffectUtil_Mobile.GetResolutionRatio(curQuality);
        isDownResolution = resolutionRatio > 1;    //Down Resolution
        if (isDownResolution)
        {
            screenWidth = (int)(screenWidth / resolutionRatio);
            screenHeight = (int)(screenHeight / resolutionRatio);
            Screen.SetResolution(screenWidth, screenHeight, true);
        }
        //使用DepthBuffer
        if (isUseDepthBuffer)
        {
            depthBufferMgr.Init(screenWidth, screenHeight, depthBufferRatio);
            globalDepthMode = DepthTextureMode.None;
        }
        else
        {
            depthBufferMgr.enabled = false;
            globalDepthMode = DepthTextureMode.Depth;
        }
        //后处理
        //if (effect_DofBokeh != null)
        //{
        //    effect_DofBokeh.Init(PostEffectUtil_Mobile.GetFormat(curQuality), globalDepthMode);
        //}
        //if (effect_Bloom != null)
        //{
        //    effect_Bloom.Init(PostEffectUtil_Mobile.GetFormat(curQuality), globalDepthMode);
        //}
    }

    private void Update()
    {
        Screen.SetResolution(screenWidth, screenHeight, true);
    }
}
