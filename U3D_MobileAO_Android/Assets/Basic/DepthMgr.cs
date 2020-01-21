using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class DepthMgr : MonoBehaviour
{
    public RenderTexture depth;
    public RenderTexture depth2;
    public RenderTexture color;
    public RenderTexture color2;
    public RenderTexture finalDepth;
    public int depthid = 0;
    // Start is called before the first frame update
    int depthBits = 32;
    public Camera cam;
    private void OnDisable()
    {
        RenderTexture.ReleaseTemporary(color);
        RenderTexture.ReleaseTemporary(color2);
        RenderTexture.ReleaseTemporary(depth);
        RenderTexture.ReleaseTemporary(depth2);
    }

    void OnEnable()
    {
        cam = this.GetComponent<Camera>();
        cam.allowHDR = false;
        // Application.isMobilePlatform ? 24 : 24;
        color = RenderTexture.GetTemporary(1920, 1080, 0, RenderTextureFormat.DefaultHDR);
        color2 = RenderTexture.GetTemporary(1920, 1080, 0, RenderTextureFormat.DefaultHDR);
        depth = RenderTexture.GetTemporary(1920, 1080, depthBits, RenderTextureFormat.Depth);
        depth2 = RenderTexture.GetTemporary(1920, 1080, depthBits, RenderTextureFormat.Depth);
        finalDepth = RenderTexture.GetTemporary(depth.width / 2, depth.height / 2, 0, RenderTextureFormat.RHalf);
    }
    private void OnPreRender()
    {
        depthid = depthid % 2;
        if (depthid == 0)
        {
            depth.DiscardContents();
            color.DiscardContents();
            cam.SetTargetBuffers(color.colorBuffer, depth.depthBuffer);
        }
        else
        {
            depth.DiscardContents();
            color.DiscardContents();
            cam.SetTargetBuffers(color.colorBuffer, depth2.depthBuffer);
        }

    }
    Shader postEffectsShader;
    //public Material m_PostEffectsMaterial;
    public Material PostEffectsMaterial;
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
    public Material testMat;



    private void OnPostRender()
    {
        if (UseFloatRGBA)
        {
            PostEffectsMaterial.EnableKeyword("FLOATRGBA");
        }
        else
        {
            PostEffectsMaterial.DisableKeyword("FLOATRGBA");
        }
        finalDepth.DiscardContents();
        if (depthid == 0)
        {
            CopyDepth(depth2);
        }
        else
        {
            CopyDepth(depth);
        }
        depthid++;
        Shader.SetGlobalTexture("_CameraDepthTexture", finalDepth);
        if (depthid == 0)
            Graphics.Blit(color, (RenderTexture)null);
        else
            Graphics.Blit(color, (RenderTexture)null);
        Graphics.SetRenderTarget(null);
        if (cam != null)
        {
            cam.SetTargetBuffers(Graphics.activeColorBuffer, Graphics.activeDepthBuffer);
        }
    }

    private void CopyDepth(RenderTexture depth)
    {
        PostEffectsMaterial.SetTexture("_MainDepthTexture", depth);
        Graphics.Blit((RenderTexture)null, finalDepth,PostEffectsMaterial,0);
    }
    // Update is called once per frame
    void Update()
    {
        cam.depthTextureMode = DepthTextureMode.None;
        if (UseFloatRGBA && finalDepth.format != RenderTextureFormat.ARGB32)
        {
            RenderTexture.ReleaseTemporary(finalDepth);
            finalDepth = RenderTexture.GetTemporary(depth.width / 2, depth.height / 2, depthBits, RenderTextureFormat.ARGB32);
        }
        else
        {
            if (!UseFloatRGBA && finalDepth.format != RenderTextureFormat.RFloat)
            {
                RenderTexture.ReleaseTemporary(finalDepth);
                finalDepth = RenderTexture.GetTemporary(depth.width / 2, depth.height / 2, depthBits, RenderTextureFormat.RFloat);
            }
        }
    }

    public bool UseFloatRGBA = false;

    void OnGUI()
    {
        var styleButton = GUI.skin.button;
        styleButton.fontSize = 30;
        if (UseFloatRGBA == false)
        {
            if (GUILayout.Button("FloatRGBA OFF", GUILayout.Width(250), GUILayout.Height(50)))
            {
                UseFloatRGBA = true;
            }
        }
        else
        {
            if (GUILayout.Button("FloatRGBA ON", GUILayout.Width(250), GUILayout.Height(50)))
            {
                UseFloatRGBA = false;
            }
        }
        GUI.DrawTexture(new Rect(10, 60, 960, 540), color);
    }
}
