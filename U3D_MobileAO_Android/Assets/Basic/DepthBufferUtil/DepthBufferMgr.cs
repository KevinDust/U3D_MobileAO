//#define FullSample
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DepthBufferMgr : MonoBehaviour
{
    private Camera _cam;
    private Camera _MainCam
    {
        get {
            if (_cam == null)
                _cam = Camera.main;
            return _cam;
        }
    }
    public RenderTexture depth;
    public RenderTexture depth2;
    public RenderTexture color;
    //public RenderTexture color2;
    //public RenderTexture finalColor;

    //public RenderTexture temp;

    public RenderTexture finalDepth;
    public int depthid = 0;
    // Start is called before the first frame update
    int depthBits = 24;

    Shader postEffectsShader;
    public Material testMat;
    private int screenW;
    private int screenH;
    public bool isInit = false;
    //public Material m_PostEffectsMaterial;
    public Material m_DepthCopyMaterial;
    public Material m_ColorCopyMaterial;

    // {
    //     get
    //     {
    //         if (postEffectsShader == null)
    //         {
    //             postEffectsShader = Shader.Find("Custom/PostEffect/Combine");
    //         }
    //         if (m_PostEffectsMaterial == null && postEffectsShader != null)
    //         {
    //             m_PostEffectsMaterial = new Material(postEffectsShader);
    //             m_PostEffectsMaterial.hideFlags = HideFlags.HideAndDontSave;
    //         }
    //         return m_PostEffectsMaterial;
    //     }
    // }
    private void OnDisable()
    {
        ReleaseBuffer();
    }
    public void ReleaseBuffer()
    {
        if(color!=null)
            RenderTexture.ReleaseTemporary(color);
        //RenderTexture.ReleaseTemporary(color2);
        //if (finalColor != null)
        //    RenderTexture.ReleaseTemporary(finalColor);
        if (depth != null)
            RenderTexture.ReleaseTemporary(depth);
        if (depth2 != null)
            RenderTexture.ReleaseTemporary(depth2);
        if (finalDepth != null)
            RenderTexture.ReleaseTemporary(finalDepth);
    }
    public void Init(int width,int height,int depthRatio)
    {
        // Application.isMobilePlatform ? 24 : 24;
        RenderTextureFormat format = _MainCam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        color = RenderTexture.GetTemporary(width, height, 0, format);
        //color2 = RenderTexture.GetTemporary(width, height, 0, format);
        //finalColor = RenderTexture.GetTemporary(width, height, 0, format);
        depth = RenderTexture.GetTemporary(width, height, depthBits, RenderTextureFormat.Depth);

        depth2 = RenderTexture.GetTemporary(width, height, depthBits, RenderTextureFormat.Depth);

#if FullSample
#else
        //RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor()
        finalDepth = RenderTexture.GetTemporary(width/ depthRatio, height/ depthRatio, depthBits, RenderTextureFormat.Depth);
#endif
        isInit = true;
    }
    private void OnPreRender()
    {
        if (!isInit)
        {
            Init(Screen.width, Screen.height, 2);
        }

#if FullSample

        //depth.DiscardContents();
        //color.DiscardContents();
        //_MainCam.SetTargetBuffers(color.colorBuffer, depth.depthBuffer);
        //Shader.SetGlobalTexture("_CameraDepthTexture", depth);

        depthid = depthid % 2;
        if (depthid == 0)
        {
            depth.DiscardContents();
            color.DiscardContents();
            _MainCam.SetTargetBuffers(color.colorBuffer, depth.depthBuffer);
        }
        else
        {
            depth2.DiscardContents();
            color.DiscardContents();
            _MainCam.SetTargetBuffers(color.colorBuffer, depth2.depthBuffer);
        }
        if (depthid == 0)
        {
            Shader.SetGlobalTexture("_CameraDepthTexture", depth);
        }
        else
        {
            Shader.SetGlobalTexture("_CameraDepthTexture", depth2);
        }
        
        depthid++;
#else
        depth.DiscardContents();
        color.DiscardContents();
        _MainCam.SetTargetBuffers(color.colorBuffer, depth.depthBuffer);
        //if (depthid == 0)
        //{
        //    depth.DiscardContents();
        //    color.DiscardContents();
        //    _MainCam.SetTargetBuffers(color.colorBuffer, depth.depthBuffer);
        //}
        //else
        //{
        //    depth2.DiscardContents();
        //    color.DiscardContents();
        //    _MainCam.SetTargetBuffers(color.colorBuffer, depth2.depthBuffer);
        //}
#endif
        _MainCam.depthTextureMode = DepthTextureMode.None;
    }

    //void OnRenderImage(RenderTexture source, RenderTexture destination)
    //{
    //    //用前一帧 会出现重影
    //    //if (depthid == 0)
    //    //{
    //    //    CopyDepth(depth, finalDepth);
    //    //    Shader.SetGlobalTexture("_CameraDepthTexture", finalDepth);
    //    //}
    //    //else
    //    //{
    //    //    CopyDepth(depth2, finalDepth);
    //    //    Shader.SetGlobalTexture("_CameraDepthTexture", finalDepth);
    //    //}
    //    //depthid++;
    //    //depthid = depthid % 2;
    //    Graphics.Blit(source, destination);
    //    //temp.DiscardContents();
    //    Graphics.SetRenderTarget(null);
    //    if (_MainCam != null)
    //    {
    //        _MainCam.SetTargetBuffers(Graphics.activeColorBuffer, Graphics.activeDepthBuffer);
    //    }
    //}


    //OnPostRender 本身 在OnRenderImage 之前调用
    private void OnPostRender()
    {
        CopyDepth(depth, finalDepth);
        Shader.SetGlobalTexture("_CameraDepthTexture", finalDepth);
        ////temp.DiscardContents();
        Graphics.SetRenderTarget(null);
        if (_MainCam != null)
        {
            _MainCam.SetTargetBuffers(Graphics.activeColorBuffer, Graphics.activeDepthBuffer);
        }

    }

    private void CopyDepth(RenderTexture _depth, RenderTexture dst)
    {
        dst.DiscardContents();
        m_DepthCopyMaterial.SetTexture("_MainDepthTexture", _depth);
        Graphics.Blit(_depth, dst, m_DepthCopyMaterial,0);
    }

    //private void CopyColor(RenderTexture color)
    //{
    //    finalColor.DiscardContents();
    //    m_ColorCopyMaterial.SetTexture("_MainColorTexture", color);
    //    Graphics.Blit(color, finalColor, m_ColorCopyMaterial, 0);
    //}
    

    //void OnGUI()
    //{
    //    var styleButton = GUI.skin.button;
    //    styleButton.fontSize = 30;
    //    GUI.DrawTexture(new Rect(10, 60, 960, 540), finalDepth);
    //}
}
