using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Noise2DCreatorEditor : EditorWindow  {

    static Noise2DCreatorEditor win;

    [MenuItem("Tools/Noise2DCreator")]
    static void NoiseCreatorWindow()
    {
        //创建窗口
        Rect wr = new Rect(0, 0, 1000, 600);
        win = (Noise2DCreatorEditor)EditorWindow.GetWindowWithRect(typeof(Noise2DCreatorEditor), wr, true, "Noise2D Creator");
        NoiseTexGenerator.Init();
        win.Show();
    }


    //Noise Tex
    Rect texShowerRect = new Rect(20, 30, 256, 256);

    NoiseTexGenerator.Noise2DType[] t = new NoiseTexGenerator.Noise2DType[NoiseTexGenerator.NOISE_NUM] {
        NoiseTexGenerator.Noise2DType.PerilinNoise2D, NoiseTexGenerator.Noise2DType .PerilinNoise2D};
    NoiseTexGenerator.Noise2DType[] oldType = new NoiseTexGenerator.Noise2DType[NoiseTexGenerator.NOISE_NUM];
    private float[] scale = new float[NoiseTexGenerator.NOISE_NUM];

    private bool[] useFBM = new bool[NoiseTexGenerator.NOISE_NUM];
    private int[] octaves = new int[NoiseTexGenerator.NOISE_NUM];
    private float[] lacunarity = new float[NoiseTexGenerator.NOISE_NUM];
    private float[] gain = new float[NoiseTexGenerator.NOISE_NUM];
    private float[] amplitude0 = new float[NoiseTexGenerator.NOISE_NUM];
    private float[] frequency0 = new float[NoiseTexGenerator.NOISE_NUM];


    //export
    private int width = 256;
    private int height = 256;

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
        NoiseTexGenerator.Clear();
    }

    public CustomRenderTexture DrawNoiseArea(int noiseID)
    {
        CustomRenderTexture noiseTex = NoiseTexGenerator.GetPreViewNoise2DRT(t[noiseID],noiseID, oldType[noiseID] != t[noiseID]);
        oldType[noiseID] = t[noiseID];
        GUI.DrawTexture(texShowerRect, noiseTex, ScaleMode.ScaleToFit, false);

        GUILayout.BeginArea(new Rect(10, 300, 280, 500));
        for (int i = 0; i < 3; i++)
            EditorGUILayout.Space();

        EditorGUILayout.LabelField("--------------NoiseParam---------------");
        t[noiseID] = (NoiseTexGenerator.Noise2DType)EditorGUILayout.EnumPopup("Noise Tpye: ", t[noiseID]);
        scale[noiseID] = EditorGUILayout.Slider("Scale: ", scale[noiseID], 1, 10);
        noiseTex.material.SetFloat("_Scale", scale[noiseID]);
        useFBM[noiseID] = EditorGUILayout.Toggle("Use FBM:", useFBM[noiseID]);
        if (useFBM[noiseID])
        {
            noiseTex.material.EnableKeyword("_USEFBM");
            octaves[noiseID] = EditorGUILayout.IntSlider("Octaves:", octaves[noiseID], 1, 10);
            lacunarity[noiseID] = EditorGUILayout.FloatField("Lacunarity:", lacunarity[noiseID]);
            gain[noiseID] = EditorGUILayout.FloatField("Gain:", gain[noiseID]);
            amplitude0[noiseID] = EditorGUILayout.FloatField("Amplitude0:", amplitude0[noiseID]);
            frequency0[noiseID] = EditorGUILayout.FloatField("Frequency0:", frequency0[noiseID]);
            noiseTex.material.SetInt("_Octaves", octaves[noiseID]);
            noiseTex.material.SetFloat("_Lacunarity", lacunarity[noiseID]);
            noiseTex.material.SetFloat("_Gain", gain[noiseID]);
            noiseTex.material.SetFloat("_Amplitude0", amplitude0[noiseID]);
            noiseTex.material.SetFloat("_Frequency0", frequency0[noiseID]);
        }
        else
            noiseTex.material.DisableKeyword("_USEFBM");

        EditorGUILayout.ObjectField(noiseTex, typeof(CustomRenderTexture));
        GUILayout.EndArea();
        return noiseTex;
    }
    public Material blendMaterial;
    public void OnGUI()
    {
        if (GUILayout.Button("reset noise"))
        {
            NoiseTexGenerator.Init();
        }
        CustomRenderTexture[] crtList = new CustomRenderTexture[NoiseTexGenerator.NOISE_NUM];
        for (int i = 0; i < NoiseTexGenerator.NOISE_NUM; i++)
        {
            GUILayout.BeginArea(new Rect(10 + 310 * i, 10, 300, 800));
            crtList[i] = DrawNoiseArea(i);
            GUILayout.EndArea();
        }
        GUILayout.BeginArea(new Rect(10 + 310 * NoiseTexGenerator.NOISE_NUM, 10, 550, 800));
        CustomRenderTexture noiseTex = NoiseTexGenerator.t_Blend2D;
        for (int i = 0; i < NoiseTexGenerator.NOISE_NUM; i++) {
            noiseTex.material.SetTexture("_MainTex" + i, crtList[i]);
        }
        GUI.DrawTexture(texShowerRect, noiseTex, ScaleMode.ScaleToFit, false);
        GUILayout.BeginArea(new Rect(10, 300, 280, 500));
        blendMaterial = EditorGUILayout.ObjectField(blendMaterial, typeof(Material),false) as Material;
        if (blendMaterial != null)
            noiseTex.material = blendMaterial;        else
        {
            blendMaterial = noiseTex.material;
        }
        EditorGUILayout.ObjectField(noiseTex, typeof(CustomRenderTexture));
        EditorGUILayout.LabelField("-------------------------------------------EXPORT------------------------------------------");
        width = EditorGUILayout.IntField("Width: ", width);
        height = EditorGUILayout.IntField("Height: ", height);

        {
            if (GUILayout.Button("Export PNG"))
            {
                EditorCoroutineRunner.StartEditorCoroutine(Export());
            }
        }
        GUILayout.EndArea();
        GUILayout.EndArea();
    }
    IEnumerator Export()
    {
        CustomRenderTexture exportRT = NoiseTexGenerator.GenerateNoise2DBlendRT(blendMaterial,width, height);
        yield return null;
        NoiseTexGenerator.ExportNoiseRTToPNG(exportRT);
        exportRT.Release();
        for (int i = 0; i < NoiseTexGenerator.NOISE_NUM; i++)
            oldType[i] = NoiseTexGenerator.Noise2DType.NeedChange;
        win.Repaint();
        
    }

    CustomRenderTexture exportRT = null;
    private static Material[] _previewTex3dMat;
    private static Material[] PreviewTex3dMat
    {
        get
        {
            if (_previewTex3dMat == null)
            {
                _previewTex3dMat = new Material[NoiseTexGenerator.NOISE_NUM+1];
                for (int i = 0; i < NoiseTexGenerator.NOISE_NUM+1; i++) {
                    _previewTex3dMat[i] = new Material(Shader.Find("ChillyRoom/Noise/Sample 3D Texture"));
                }
            }
            return _previewTex3dMat;
        }
    }
    //private Vector2 _cameraAngle;
    //private float distance = 5;
    public Texture DrawTexture3DPreview(CustomRenderTexture tex,Rect rect,GUIStyle BackgroundGuiStyle,Material previewM)
    {
        //_cameraAngle = PreviewRenderUtilityHelpers.DragToAngles(_cameraAngle, rect);
        PreviewRenderUtilityHelpers.Instance.BeginPreview(rect, BackgroundGuiStyle);
        PreviewRenderUtilityHelpers.Instance.DrawMesh(MeshHelpers.Plane, Matrix4x4.identity, previewM, 0);

        PreviewRenderUtilityHelpers.Instance.camera.transform.position = Vector2.zero;
        PreviewRenderUtilityHelpers.Instance.camera.transform.rotation = Quaternion.Euler(new Vector3(-90, -90, 0));
        PreviewRenderUtilityHelpers.Instance.camera.transform.position = PreviewRenderUtilityHelpers.Instance.camera.transform.forward * -4;
        PreviewRenderUtilityHelpers.Instance.camera.Render();
        return PreviewRenderUtilityHelpers.Instance.EndPreview();
    }
}
