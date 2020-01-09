using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NoiseCreatorEditor : EditorWindow  {

    //[MenuItem("Tools/NoiseCreator_Single")]
    static void NoiseCreatorWindow()
    {
        //创建窗口
        Rect wr = new Rect(0, 0, 300, 800);
        NoiseCreatorEditor window = (NoiseCreatorEditor)EditorWindow.GetWindowWithRect(typeof(NoiseCreatorEditor), wr, true, "Noise Creator");
        window.Show();
    }
    Rect texShowerRect = new Rect(20, 30, 256, 256);

    NoiseTexGenerator_Single.NoiseType t = NoiseTexGenerator_Single.NoiseType.PerilinNoise2D;
    private float scale = 5;

    private float tex3D_Z=0;

    private int width = 256;
    private int height = 256;
    private int depth = 256;

    private bool useFBM = false;
    private int octaves = 2;
    private float lacunarity = 2;
    private float gain = 0.5f;
    private float amplitude0 = 0.5f;
    private float frequency0 = 1.0f;


    private string name = "noise";
    private Material _previewTex3dMat2;

    private static Texture2D _backgroundTexture;
    private static GUIStyle _backgroundGuiStyle;
    public static Texture2D BackgroundTexture
    {
        get
        {
            if (_backgroundTexture == null)
            {
                _backgroundTexture = new Texture2D(1, 1);
                _backgroundTexture.SetPixel(0, 0, Color.gray * 0.5f);
                _backgroundTexture.Apply();
            }

            return _backgroundTexture;
        }
    }

    /// <summary>
    /// Accessor to the static background GUIStyle
    /// </summary>
    public static GUIStyle BackgroundGuiStyle
    {
        get
        {
            _backgroundGuiStyle = new GUIStyle();
            _backgroundGuiStyle.active.background = BackgroundTexture;
            _backgroundGuiStyle.focused.background = BackgroundTexture;
            _backgroundGuiStyle.hover.background = BackgroundTexture;
            _backgroundGuiStyle.normal.background = BackgroundTexture;

            return _backgroundGuiStyle;
        }
    }

    public void OnDisable()
    {
        PreviewRenderUtilityHelpers.Instance.Cleanup();
    }

    public void OnGUI()
    {
        if (GUILayout.Button("reset noise"))
        {
            NoiseTexGenerator.Init();
        }
        CustomRenderTexture noiseTex = NoiseTexGenerator_Single.GetPreViewNoiseRT(t);
        
        if(t == NoiseTexGenerator_Single.NoiseType.PerilinNoise3D || t== NoiseTexGenerator_Single.NoiseType.PerilinNoise3D_ZLoop)
        {
            PreviewTexdMat.SetTexture("_Volume", noiseTex);
            Texture t = DrawTexture3DPreview(noiseTex, texShowerRect, BackgroundGuiStyle);
           
            GUI.DrawTexture(texShowerRect, t, ScaleMode.ScaleToFit, false);
        }
        else
            GUI.DrawTexture(texShowerRect, noiseTex,ScaleMode.ScaleToFit,false);
        for (int i = 0; i <48; i++)
            EditorGUILayout.Space();
        if (t == NoiseTexGenerator_Single.NoiseType.PerilinNoise3D || t == NoiseTexGenerator_Single.NoiseType.PerilinNoise3D_ZLoop)
        {
            tex3D_Z = EditorGUILayout.Slider("Z:", tex3D_Z, -2, 2);
            PreviewTexdMat.SetFloat("_Z", tex3D_Z);
        }
        else
        {
            for (int i = 0; i < 5; i++)
                EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField("--------------NoiseParam---------------");
        t = (NoiseTexGenerator_Single.NoiseType)EditorGUILayout.EnumPopup("Noise Tpye: ", t);
        scale = EditorGUILayout.FloatField("Scale: ", scale);
        noiseTex.material.SetFloat("_Scale", scale);
        EditorGUILayout.ObjectField(noiseTex,typeof(CustomRenderTexture));
        //_previewTex3dMat2 = (Material)EditorGUILayout.ObjectField(_previewTex3dMat2, typeof(Material),false);
        useFBM = EditorGUILayout.Toggle("Use FBM:", useFBM);
        if (useFBM)
        {
            noiseTex.material.EnableKeyword("_USEFBM");
            octaves = EditorGUILayout.IntSlider("Octaves:", octaves, 1, 10);
            lacunarity = EditorGUILayout.FloatField("Lacunarity:", lacunarity);
            gain = EditorGUILayout.FloatField("Gain:", gain);
            amplitude0 = EditorGUILayout.FloatField("Amplitude0:", amplitude0);
            frequency0 = EditorGUILayout.FloatField("Frequency0:", frequency0);
            noiseTex.material.SetInt("_Octaves", octaves);
            noiseTex.material.SetFloat("_Lacunarity", lacunarity);
            noiseTex.material.SetFloat("_Gain", gain);
            noiseTex.material.SetFloat("_Amplitude0", amplitude0);
            noiseTex.material.SetFloat("_Frequency0", frequency0);
        }
        else
            noiseTex.material.DisableKeyword("_USEFBM");


        //distance = EditorGUILayout.FloatField("Distance: ", distance);
        EditorGUILayout.LabelField("--------------EXPORT---------------");
        width = EditorGUILayout.IntField("Width: ", width);
        height = EditorGUILayout.IntField("Height: ", height);
        if (t == NoiseTexGenerator_Single.NoiseType.PerilinNoise3D || t == NoiseTexGenerator_Single.NoiseType.PerilinNoise3D_ZLoop)
        {
            depth = EditorGUILayout.IntSlider("Depth: ", depth,1, Mathf.FloorToInt(SystemInfo.maxTextureSize/ (float)height));
        }

        if (t == NoiseTexGenerator_Single.NoiseType.PerilinNoise3D || t == NoiseTexGenerator_Single.NoiseType.PerilinNoise3D_ZLoop)
        {
            if (GUILayout.Button("Export Tex3D(large memory!)"))
            {
                EditorCoroutineRunner.StartEditorCoroutine(ExportTex3D(false));
            }
            if (GUILayout.Button("Export PNG"))
            {
                EditorCoroutineRunner.StartEditorCoroutine(ExportTex3D(true));
            }
        }
        else
        {
            if (GUILayout.Button("Export PNG"))
            {
                EditorCoroutineRunner.StartEditorCoroutine(Export());
            }
        }
    }
    private Texture2D tex3DPNG;
    IEnumerator Export()
    {
        CustomRenderTexture exportRT = NoiseTexGenerator_Single.GenerateNoiseRT(t, scale, width, height, depth);
        yield return null;
        NoiseTexGenerator.ExportNoiseRTToPNG(exportRT);
        exportRT.Release();
    }

    IEnumerator ExportTex3D(bool isPNG)
    {
        CustomRenderTexture exportRT = NoiseTexGenerator_Single.GenerateNoiseRT(t, scale, width, height, depth);
        yield return null;
        if(isPNG)
            NoiseTexGenerator.SaveRTToPNG(exportRT, width, height, depth);
        else
            NoiseTexGenerator.SaveToTex3D(exportRT, width, height, depth);
        NoiseTexGenerator.SetMaterial();
        exportRT.Release();

    }

    CustomRenderTexture exportRT = null;
    private static Material _previewTex3dMat;
    private static Material PreviewTexdMat
    {
        get
        {
            if (_previewTex3dMat == null)
            {
                _previewTex3dMat = new Material(Shader.Find("ChillyRoom/Noise/Sample 3D Texture"));
            }
            return _previewTex3dMat;
        }
    }
    //private Vector2 _cameraAngle;
    //private float distance = 5;
    public Texture DrawTexture3DPreview(CustomRenderTexture tex,Rect rect,GUIStyle BackgroundGuiStyle)
    {
        //_cameraAngle = PreviewRenderUtilityHelpers.DragToAngles(_cameraAngle, rect);
        PreviewRenderUtilityHelpers.Instance.BeginPreview(rect, BackgroundGuiStyle);

        PreviewRenderUtilityHelpers.Instance.DrawMesh(MeshHelpers.Plane, Matrix4x4.identity, PreviewTexdMat, 0);

        PreviewRenderUtilityHelpers.Instance.camera.transform.position = Vector2.zero;
        PreviewRenderUtilityHelpers.Instance.camera.transform.rotation = Quaternion.Euler(new Vector3(-90, -90, 0));
        PreviewRenderUtilityHelpers.Instance.camera.transform.position = PreviewRenderUtilityHelpers.Instance.camera.transform.forward * -4;
        PreviewRenderUtilityHelpers.Instance.camera.Render();
        return PreviewRenderUtilityHelpers.Instance.EndPreview();

    }
}
