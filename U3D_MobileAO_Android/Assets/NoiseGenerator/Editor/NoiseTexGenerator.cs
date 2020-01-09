using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class NoiseTexGenerator {

    public enum Noise2DType
    {
        NeedChange = -1,
        PerilinNoise2D = 0,
        PerilinNoise2D_Tileable = 1,
        WorleyNoise2D_Tileable = 2
    };

    public enum Noise3DType
    {
        NeedChange = -1,
        PerilinNoise3D = 0,
        PerilinNoise3D_ZLoop = 1,
        PerilinNoise3D_Tile = 2,
        //PerilinNoise3D_Tile_Hash = 3,

        WorleyNoise3D_Tile = 4,
        WorleyNoise3D_Tile_Hash = 5,
    };

    static Dictionary<Noise2DType, string> noise2DDict = new Dictionary<Noise2DType, string>() {
        {Noise2DType.PerilinNoise2D, "ChillyRoom/Noise/PerlinNoise2D"},
        {Noise2DType.PerilinNoise2D_Tileable, "ChillyRoom/Noise/PerlinNoise2D_Tileable"},

        {Noise2DType.WorleyNoise2D_Tileable, "ChillyRoom/Noise/WorleyNoise2D_Tile_Hash"},
        
    };
    static Dictionary<Noise3DType, string> noise3DDict = new Dictionary<Noise3DType, string>()
    {
        {Noise3DType.PerilinNoise3D, "ChillyRoom/Noise/PerlinNoise3D"},
        {Noise3DType.PerilinNoise3D_ZLoop, "ChillyRoom/Noise/PerlinNoise3D_ZLoop"},
        {Noise3DType.PerilinNoise3D_Tile, "ChillyRoom/Noise/PerlinNoise3D_Tileable_Hash"},
        //{Noise3DType.PerilinNoise3D_Tile_Hash, "ChillyRoom/Noise/PerlinNoise3D_Tileable_Hash"},

        {Noise3DType.WorleyNoise3D_Tile, "ChillyRoom/Noise/WorleyNoise3D_Tile"},
        {Noise3DType.WorleyNoise3D_Tile_Hash, "ChillyRoom/Noise/WorleyNoise3D_Tile_Hash"}
    };

    static Shader m_perlin2D;
    static Shader m_perlin2D_Tile;
    static Shader m_perlin3D;
    static Shader m_perlin3D_ZLoop;
    static Shader m_perlin3D_Tile;
    static Shader m_worley3D_Tile;

    //Final Blend Texture
    static Material m_Blend2D;
    static Material m_Blend3D;

    public static ComputeShader m_Tex3DSlices;
    public static ComputeShader m_Tex3DTo2D;

    public static CustomRenderTexture t_Blend2D;
    public static CustomRenderTexture t_Blend3D;
    public const int NOISE_NUM = 2;
    public static CustomRenderTexture[] crtList = new CustomRenderTexture[NOISE_NUM];
    public static Material[] crtMatList = new Material[NOISE_NUM];

    public static void Init()
    {
        Clear();        

        m_Blend2D = new Material(Shader.Find("ChillyRoom/Noise/Blend/2DDefault"));
        m_Blend3D = new Material(Shader.Find("ChillyRoom/Noise/Blend/3DDefault"));

        m_Tex3DSlices = Resources.Load("Slices3dTexture") as ComputeShader;
        m_Tex3DTo2D = Resources.Load("Convert3DTo2D") as ComputeShader;

        t_Blend2D = GenerateNoise2DBlendRT(m_Blend2D, 128, 128);
        t_Blend3D = GenerateNoise3DBlendRT(m_Blend3D, 128, 128, 16);
        SetMaterial();

    }

    public static void Clear()
    {
        if (t_Blend2D != null)
            t_Blend2D.Release();
        if (t_Blend3D != null)
            t_Blend3D.Release();

        for (int i = 0; i < crtList.Length; i++)
        {
            if (crtList[i] != null)
            {
                crtList[i].Release();
                crtList[i] = null;
            }
        }
        for (int i = 0; i < crtMatList.Length; i++)
        {
            if (crtMatList[i] != null)
            {
                Object.DestroyImmediate(crtMatList[i]);
                crtMatList[i] = null;

            }
        }

        if (m_Blend2D != null)
            Object.DestroyImmediate(m_Blend2D);
        if (m_Blend3D != null)
            Object.DestroyImmediate(m_Blend3D);
    }
    static float[] garray = new float[16];
    static float[] g3Darray = new float[16 * 3];
    static float[] g4Darray = new float[32 * 4];
    static float[] g6Darray0 = new float[128 * 6]; //768
    static float[] g6Darray1 = new float[128 * 6]; //768
    public static void SetMaterial()
    {
        
        for (int i = 0; i < garray.Length; i++)
        {
            if (i % 2 == 0)
                garray[i] = gradients2D[i / 2].x;
            else
                garray[i] = gradients2D[i / 2].y;
            //Debug.Log(garray[i]);
        }
        
        for (int i = 0; i < g3Darray.Length; i++)
        {
            if (i % 3 == 0)
                g3Darray[i] = gradients3D[i / 3].x;
            else if (i % 3 == 1)
                g3Darray[i] = gradients3D[i / 3].y;
            else
                g3Darray[i] = gradients3D[i / 3].z;
            //Debug.Log(g3Darray[i]);
        }

        
        for (int i = 0; i < g4Darray.Length; i++)
        {
            if (i % 4 == 0)
                g4Darray[i] = gradients4D[i / 4].x;
            else if (i % 4 == 1)
                g4Darray[i] = gradients4D[i / 4].y;
            else if (i % 4 == 2)
                g4Darray[i] = gradients4D[i / 4].z;
            else
                g4Darray[i] = gradients4D[i / 4].w;
            //Debug.Log(g3Darray[i]);
        }

        //总共256个
        for (int i = 0; i < g6Darray0.Length * 2; i++)
        {
            if (i <= 128 * 6 - 1)
            {
                if (i % 6 == 0)
                    g6Darray0[i] = gradients6D[i / 6][0];
                else if (i % 6 == 1)
                    g6Darray0[i] = gradients6D[i / 6][1];
                else if (i % 6 == 2)
                    g6Darray0[i] = gradients6D[i / 6][2];
                else if (i % 6 == 3)
                    g6Darray0[i] = gradients6D[i / 6][3];
                else if (i % 6 == 4)
                    g6Darray0[i] = gradients6D[i / 6][4];
                else if (i % 6 == 5)
                    g6Darray0[i] = gradients6D[i / 6][5];
            }
            else
            {
                if (i % 6 == 0)
                    g6Darray1[i - 128 * 6] = gradients6D[i / 6][0];
                else if (i % 6 == 1)
                    g6Darray1[i - 128 * 6] = gradients6D[i / 6][1];
                else if (i % 6 == 2)
                    g6Darray1[i - 128 * 6] = gradients6D[i / 6][2];
                else if (i % 6 == 3)
                    g6Darray1[i - 128 * 6] = gradients6D[i / 6][3];
                else if (i % 6 == 4)
                    g6Darray1[i - 128 * 6] = gradients6D[i / 6][4];
                else if (i % 6 == 5)
                    g6Darray1[i - 128 * 6] = gradients6D[i / 6][5];
            }
        }
    }
    
    #region  Noise2DRtGenerator
    public static CustomRenderTexture GetPreViewNoise2DRT(Noise2DType type, int noiseID, bool isChange)
    {
        if (crtList[noiseID] == null)
        {
            CustomRenderTexture rt = GenerateNoise2DRT(type, 5, 256, 256);
            crtList[noiseID] = rt;
            crtMatList[noiseID] = rt.material;
            switch (type)
            {
                case Noise2DType.PerilinNoise2D:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G", garray);
                    break;
                case Noise2DType.PerilinNoise2D_Tileable:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G4D", g4Darray);
                    break;
            }
        }
        else if (isChange)
        {
            crtList[noiseID].material.shader = Shader.Find(noise2DDict[type]);
            switch (type)
            {
                case Noise2DType.PerilinNoise2D:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G", garray);
                    break;
                case Noise2DType.PerilinNoise2D_Tileable:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G4D", g4Darray);
                    break;
            }
        }
        return crtList[noiseID];
    }
    public static CustomRenderTexture GenerateNoise2DBlendRT(Material blendMat, int width, int height)
    {
        CustomRenderTexture noiseRT = new CustomRenderTexture(width, height);
        noiseRT.format = RenderTextureFormat.ARGB32;
        noiseRT.wrapMode = TextureWrapMode.Repeat;
        noiseRT.filterMode = FilterMode.Bilinear;
        noiseRT.updateMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationSource = CustomRenderTextureInitializationSource.Material;
        noiseRT.enableRandomWrite = true;

        noiseRT.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        noiseRT.material = blendMat;
        noiseRT.initializationMaterial = blendMat;
        noiseRT.shaderPass = 0;

        noiseRT.Initialize();
        noiseRT.Update();
        noiseRT.Create();
        return noiseRT;
    }
    public static CustomRenderTexture GenerateNoise2DRT(Noise2DType type, float scale, int width, int height, int depth = 128)
    {
        CustomRenderTexture noiseRT = new CustomRenderTexture(width, height);
        noiseRT.format = RenderTextureFormat.RFloat;
        noiseRT.wrapMode = TextureWrapMode.Repeat;
        noiseRT.filterMode = FilterMode.Bilinear;
        noiseRT.updateMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationSource = CustomRenderTextureInitializationSource.Material;
        noiseRT.enableRandomWrite = true;

        noiseRT.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        noiseRT.material = new Material(Shader.Find(noise2DDict[type]));
        noiseRT.initializationMaterial = noiseRT.material;
        noiseRT.shaderPass = 0;
        noiseRT.material.SetFloat("_Scale", scale);

        noiseRT.Initialize();
        noiseRT.Update();
        noiseRT.Create();
        return noiseRT;
    }

    #endregion

    #region  Noise3DRtGenerator
    public static CustomRenderTexture GetPreViewNoise3DRT(Noise3DType type, int noiseID, bool isChange)
    {
        if (crtList[noiseID] == null)
        {
            CustomRenderTexture rt = GenerateNoise3DRT(type, 5, 128, 128, 64);
            crtList[noiseID] = rt;
            crtMatList[noiseID] = rt.material;
            switch (type)
            {
                case Noise3DType.PerilinNoise3D:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G3D", g3Darray);
                    break;
                case Noise3DType.PerilinNoise3D_ZLoop:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G4D", g4Darray);
                    break;
                case Noise3DType.PerilinNoise3D_Tile:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G6D_0", g6Darray0);
                    crtList[noiseID].material.SetFloatArray("_G6D_1", g6Darray1);
                    break;
            }
        }
        else if (isChange)
        {
            crtList[noiseID].material.shader = Shader.Find(noise3DDict[type]);
            switch (type)
            {
                case Noise3DType.PerilinNoise3D:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G3D", g3Darray);
                    break;
                case Noise3DType.PerilinNoise3D_ZLoop:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G4D", g4Darray);
                    break;
                case Noise3DType.PerilinNoise3D_Tile:
                    crtList[noiseID].material.SetFloatArray("_PERM", permutation);
                    crtList[noiseID].material.SetFloatArray("_G6D_0", g6Darray0);
                    crtList[noiseID].material.SetFloatArray("_G6D_1", g6Darray1);
                    break;
            }
        }
        return crtList[noiseID];
    }
    public static CustomRenderTexture GenerateNoise3DBlendRT(Material blendMat, int width, int height, int depth)
    {
        CustomRenderTexture noiseRT = new CustomRenderTexture(width, height);
        noiseRT.format = RenderTextureFormat.ARGB32;
        noiseRT.wrapMode = TextureWrapMode.Repeat;
        noiseRT.filterMode = FilterMode.Bilinear;
        noiseRT.updateMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationSource = CustomRenderTextureInitializationSource.Material;
        noiseRT.enableRandomWrite = true;

        noiseRT.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        noiseRT.volumeDepth = depth;
        noiseRT.material = blendMat;
        noiseRT.initializationMaterial = blendMat;
        noiseRT.shaderPass = 0;

        noiseRT.Initialize();
        noiseRT.Update();
        noiseRT.Create();
        return noiseRT;
    }
    public static CustomRenderTexture GenerateNoise3DRT(Noise3DType type, float scale, int width, int height, int depth = 128)
    {
        CustomRenderTexture noiseRT = new CustomRenderTexture(width, height);
        noiseRT.format = RenderTextureFormat.ARGB32;
        noiseRT.wrapMode = TextureWrapMode.Repeat;
        noiseRT.filterMode = FilterMode.Bilinear;
        noiseRT.updateMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationMode = CustomRenderTextureUpdateMode.Realtime;
        noiseRT.initializationSource = CustomRenderTextureInitializationSource.Material;
        noiseRT.enableRandomWrite = true;

        noiseRT.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        noiseRT.volumeDepth = depth;
        noiseRT.material = new Material(Shader.Find(noise3DDict[type]));
        noiseRT.initializationMaterial = noiseRT.material;
        noiseRT.shaderPass = 0;
        noiseRT.material.SetFloat("_Scale", scale);

        noiseRT.Initialize();
        noiseRT.Update();
        noiseRT.Create();
        return noiseRT;
    }

    #endregion

    #region Texture2D Export
    public static Texture2D ExportNoiseRTToPNG(CustomRenderTexture noiseRT)
    {
        Texture2D t = SaveToPNG(noiseRT);
        SetMaterial();
        return t;
    }
    public static bool IsHDRFormat(RenderTextureFormat format)
    {
        return format == RenderTextureFormat.ARGBHalf ||
            format == RenderTextureFormat.RGB111110Float ||
            format == RenderTextureFormat.RGFloat ||
            format == RenderTextureFormat.ARGBFloat ||
            format == RenderTextureFormat.RFloat ||
            format == RenderTextureFormat.RGHalf ||
            format == RenderTextureFormat.RHalf;
    }
    public static Texture2D SaveToPNG(CustomRenderTexture texture)
    {
        int width = texture.width;
        int height = texture.height;
        int depth = texture.volumeDepth;

        // This has its TextureFormat helper equivalent in C++ but since we are going to try to refactor TextureFormat/RenderTextureFormat into a single type so let's not bloat Scripting APIs with stuff that will get useless soon(tm).
        bool isFormatHDR = IsHDRFormat(texture.format);
        bool isFloatFormat = (texture.format == RenderTextureFormat.ARGBFloat || texture.format == RenderTextureFormat.RFloat);

        TextureFormat format = isFormatHDR ? TextureFormat.RGBAFloat : TextureFormat.RFloat;
        int finalWidth = width;
        if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D)
            finalWidth = width * depth;
        else if (texture.dimension == UnityEngine.Rendering.TextureDimension.Cube)
            finalWidth = width * 6;

        Texture2D tex = new Texture2D(finalWidth, height, format, false);

        // Read screen contents into the texture
        if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D)
        {
            Graphics.SetRenderTarget(texture);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
        }
        else if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D)
        {
            int offset = 0;
            for (int i = 0; i < depth; ++i)
            {
                Graphics.SetRenderTarget(texture, 0, CubemapFace.Unknown, i);
                tex.ReadPixels(new Rect(0, 0, width, height), offset, 0);
                tex.Apply();
                offset += width;
            }
        }
        else
        {
            int offset = 0;
            for (int i = 0; i < 6; ++i)
            {
                Graphics.SetRenderTarget(texture, 0, (CubemapFace)i);
                tex.ReadPixels(new Rect(0, 0, width, height), offset, 0);
                tex.Apply();
                offset += width;
            }
        }
        RenderTexture.active = null;
        // Encode texture into PNG
        byte[] bytes = null;
        if (isFormatHDR)
            bytes = tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP | (isFloatFormat ? Texture2D.EXRFlags.OutputAsFloat : 0));
        else
            bytes = tex.EncodeToPNG();

        Object.DestroyImmediate(tex);

        var extension = isFormatHDR ? "exr" : "png";

        var directory = Application.dataPath;
        string assetPath = EditorUtility.SaveFilePanel("Save Custom Render Texture", directory, texture.name, extension);
        if (!string.IsNullOrEmpty(assetPath))
        {
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.Refresh();
        }
        return tex;
    }
    #endregion


    #region Texture3D Export

    private static RenderTexture Copy3DSliceToRenderTexture(int layer, int w, int h, int d, CustomRenderTexture rt3D)
    {
        RenderTexture render = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        render.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        render.enableRandomWrite = true;
        render.wrapMode = TextureWrapMode.Clamp;
        render.Create();

        int kernelIndex = m_Tex3DSlices.FindKernel("CSMain");
        m_Tex3DSlices.SetTexture(kernelIndex, "noise", rt3D);
        m_Tex3DSlices.SetInt("layer", layer);
        m_Tex3DSlices.SetTexture(kernelIndex, "Result", render);
        m_Tex3DSlices.Dispatch(kernelIndex, w, h, 1);

        return render;
    }

    private static RenderTexture Copy3DToRenderTexture(int w, int h, int d, CustomRenderTexture rt3D)
    {
        RenderTexture render = RenderTexture.GetTemporary(w*d, h, 0, RenderTextureFormat.ARGB32);
        render.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        render.enableRandomWrite = true;
        render.wrapMode = TextureWrapMode.Clamp;
        render.Create();

        int kernelIndex = m_Tex3DTo2D.FindKernel("CSMain");
        m_Tex3DTo2D.SetTexture(kernelIndex, "noise", rt3D);
        m_Tex3DTo2D.SetInt("width", w);
        m_Tex3DTo2D.SetTexture(kernelIndex, "Result", render);
        m_Tex3DTo2D.Dispatch(kernelIndex, w, h, 1);

        return render;
    }

    private static Texture2D ConvertFromRenderTexture(RenderTexture rt, int w, int h)
    {
        Texture2D output = new Texture2D(w, h);
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        output.Apply();
        return output;
    }

    public static void SaveRTToPNG(CustomRenderTexture rt3D, int w, int h, int d)
    {
        RenderTexture rt =Copy3DToRenderTexture(w, h, d, rt3D);

        //Write RenderTexture slices to static textures
        Texture2D final = ConvertFromRenderTexture(rt, w* d, h);
        RenderTexture.ReleaseTemporary(rt);
        Color[] outputPixels = final.GetPixels();
        Color[] layerPixels = final.GetPixels();
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                outputPixels[0] = layerPixels[i + j * w];
            }
        }

        byte[] bytes = null;
        bytes = final.EncodeToPNG();

        Object.DestroyImmediate(final);
        
        var directory = Application.dataPath;
        string assetPath = EditorUtility.SaveFilePanel("Save Custom Render Texture", directory, "noise3D", "png");
        if (!string.IsNullOrEmpty(assetPath))
        {
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.Refresh();
        }
    }

    public static void SaveToTex3D(CustomRenderTexture rt3D, int w, int h, int d)
    {
        //SetComputeParams(rt3D, w, h, d);
        //Slice 3D Render Texture to individual layers
        RenderTexture[] layers = new RenderTexture[d];
        for (int i = 0; i < d; i++)
            layers[i] = Copy3DSliceToRenderTexture(i, w,h,d,rt3D);

        //Write RenderTexture slices to static textures
        Texture2D[] finalSlices = new Texture2D[d];
        for (int i = 0; i < d; i++)
            finalSlices[i] = ConvertFromRenderTexture(layers[i], w, h);

        for(int i=0;i<d;i++)
            RenderTexture.ReleaseTemporary(layers[i]);

        //Build 3D Texture from 2D slices
        Texture3D output = new Texture3D(w, h, d, TextureFormat.ARGB32, true);
        output.filterMode = FilterMode.Trilinear;
        Color[] outputPixels = output.GetPixels();
        for (int k = 0; k < d; k++)
        {
            Color[] layerPixels = finalSlices[k].GetPixels();
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    outputPixels[i + j * w + k * h * w] = layerPixels[i + j * w];
                }
            }
        }
        for (int i = 0; i < d; i++)
            Object.DestroyImmediate(finalSlices[i]);

        output.SetPixels(outputPixels);
        output.Apply();

        string path = EditorUtility.SaveFilePanel("Save Custom Render Texture", Application.dataPath, "noise3D", "asset");
        path = path.Replace(Application.dataPath, "Assets");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(output, path);
            AssetDatabase.Refresh();
        }
    }


    //public static Texture2D CRT3DToTex2D(CustomRenderTexture texture)
    //{
    //    if (texture.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
    //        return null;
    //    int width = texture.width;
    //    int height = texture.height;
    //    int depth = texture.volumeDepth;

    //    // This has its TextureFormat helper equivalent in C++ but since we are going to try to refactor TextureFormat/RenderTextureFormat into a single type so let's not bloat Scripting APIs with stuff that will get useless soon(tm).
    //    bool isFormatHDR = IsHDRFormat(texture.format);
    //    bool isFloatFormat = (texture.format == RenderTextureFormat.ARGBFloat || texture.format == RenderTextureFormat.RFloat);

    //    TextureFormat format = isFormatHDR ? TextureFormat.RGBAFloat : TextureFormat.RGBA32;

    //    Texture2D tex = new Texture2D(width, height, format, false);
    //    RenderTexture.active = texture;

    //    Texture3D tex3D = new Texture3D(width, height, depth, TextureFormat.ARGB32, true);
    //    var cols = new Color[width * height * depth];
    //    int slicePixelAmount = width * height;

    //    // Read screen contents into the texture
    //    for (int i = 0; i < depth; ++i)
    //    {
    //        float d = (i + 0.5f) / depth;
    //        m_Tex3DRead.SetFloat("_Height", d);
    //        Graphics.Blit(null, texture, m_Tex3DRead);
    //        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    //        Color32[] sliceColors = tex.GetPixels32();
    //        int sliceBaseIndex = i * slicePixelAmount;
    //        for (int pixel = 0; pixel < slicePixelAmount; pixel++)
    //        {
    //            cols[sliceBaseIndex + pixel] = sliceColors[pixel];
    //        }
    //    }

    //    tex3D.SetPixels(cols);
    //    tex3D.Apply();

    //    string path = EditorUtility.SaveFilePanel("Save Custom Render Texture", Application.dataPath, "noise3D", "asset");
    //    path = path.Replace(Application.dataPath, "Assets");
    //    if (!string.IsNullOrEmpty(path))
    //    {
    //        AssetDatabase.CreateAsset(tex3D, path);
    //        AssetDatabase.Refresh();
    //    }
    //    return null;
    //}


    #endregion
















    //[MenuItem("CONTEXT/CustomRenderTexture/Export2", false)]
    //public static void SaveToDisk2(MenuCommand command)
    //{
    //    CustomRenderTexture texture = command.context as CustomRenderTexture;
    //    int width = texture.width;
    //    int height = texture.height;
    //    int depth = texture.volumeDepth;

    //    // This has its TextureFormat helper equivalent in C++ but since we are going to try to refactor TextureFormat/RenderTextureFormat into a single type so let's not bloat Scripting APIs with stuff that will get useless soon(tm).
    //    bool isFormatHDR = IsHDRFormat(texture.format);
    //    bool isFloatFormat = (texture.format == RenderTextureFormat.ARGBFloat || texture.format == RenderTextureFormat.RFloat);

    //    TextureFormat format = isFormatHDR ? TextureFormat.RGBAFloat : TextureFormat.RGBA32;
    //    int finalWidth = width;
    //    if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D)
    //        finalWidth = width * depth;
    //    else if (texture.dimension == UnityEngine.Rendering.TextureDimension.Cube)
    //        finalWidth = width * 6;

    //    Texture2D tex = new Texture2D(finalWidth, height, format, false);

    //    // Read screen contents into the texture
    //    if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D)
    //    {
    //        Graphics.SetRenderTarget(texture);
    //        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    //        tex.Apply();
    //    }
    //    else if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D)
    //    {
    //        int offset = 0;
    //        for (int i = 0; i < depth; ++i)
    //        {
    //            Graphics.SetRenderTarget(texture, 0, CubemapFace.Unknown, i);
    //            tex.ReadPixels(new Rect(0, 0, width, height), offset, 0);
    //            tex.Apply();
    //            offset += width;
    //        }
    //    }
    //    else
    //    {
    //        int offset = 0;
    //        for (int i = 0; i < 6; ++i)
    //        {
    //            Graphics.SetRenderTarget(texture, 0, (CubemapFace)i);
    //            tex.ReadPixels(new Rect(0, 0, width, height), offset, 0);
    //            tex.Apply();
    //            offset += width;
    //        }
    //    }

    //    // Encode texture into PNG
    //    byte[] bytes = null;
    //    if (isFormatHDR)
    //        bytes = tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP | (isFloatFormat ? Texture2D.EXRFlags.OutputAsFloat : 0));
    //    else
    //        bytes = tex.EncodeToPNG();

    //    Object.DestroyImmediate(tex);

    //    var extension = isFormatHDR ? "exr" : "png";

    //    var directory = Application.dataPath;
    //    string assetPath = EditorUtility.SaveFilePanel("Save Custom Render Texture", directory, texture.name, extension);
    //    if (!string.IsNullOrEmpty(assetPath))
    //    {
    //        File.WriteAllBytes(assetPath, bytes);
    //        AssetDatabase.Refresh();
    //    }
    //}

    public static Texture2D GenerateNoiseTex(int width, int height)
    {
        //创建了新的2D纹理，并指定宽和 高
        Texture2D proceduralTexture = new Texture2D(width, height);


        //遍历新建纹理的每个像素
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

                Color pixelColor = Color.black;

                proceduralTexture.SetPixel(x, y, pixelColor);
            }
        }
        //最后把像素应用到纹理
        proceduralTexture.Apply();

        //输出
        return proceduralTexture;
    }


    private static float[] permutation = { 151,160,137,91,90,15,                 // Hash lookup table as defined by Ken Perlin.  This is a randomly
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,    // arranged array of all numbers from 0-255 inclusive.
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };
    private static Vector2[] gradients2D = {
        new Vector2( 1f, 0f),
        new Vector2(-1f, 0f),
        new Vector2( 0f, 1f),
        new Vector2( 0f,-1f),
        new Vector2( 1f, 1f).normalized,
        new Vector2(-1f, 1f).normalized,
        new Vector2( 1f,-1f).normalized,
        new Vector2(-1f,-1f).normalized
    };

    private static Vector3[] gradients3D = {
        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 1f,-1f, 0f),
        new Vector3(-1f,-1f, 0f),
        new Vector3( 1f, 0f, 1f),
        new Vector3(-1f, 0f, 1f),
        new Vector3( 1f, 0f,-1f),
        new Vector3(-1f, 0f,-1f),
        new Vector3( 0f, 1f, 1f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f, 1f,-1f),
        new Vector3( 0f,-1f,-1f),

        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f,-1f,-1f)
    };

    private static Vector4[] gradients4D = {
        new Vector4(0,-1,-1,-1),
        new Vector4(0,-1,-1,1),
        new Vector4(0,-1,1,-1),
        new Vector4(0,-1,1,1),
        new Vector4(0,1,-1,-1),
        new Vector4(0,1,-1,1),
        new Vector4(0,1,1,-1),
        new Vector4(0,1,1,1),

        new Vector4(-1,-1,0,-1),
        new Vector4(-1,1,0,-1),
        new Vector4(1,-1,0,-1),
        new Vector4(1,1,0,-1),
        new Vector4(-1,-1,0,1),
        new Vector4(-1,1,0,1),
        new Vector4(1,-1,0,1),
        new Vector4(1,1,0,1),

        new Vector4(-1,0,-1,-1),
        new Vector4(1,0,-1,-1),
        new Vector4(-1,0,-1,1),
        new Vector4(1,0,-1,1),
        new Vector4(-1,0,1,-1),
        new Vector4(1,0,1,-1),
        new Vector4(-1,0,1,1),
        new Vector4(1,0,1,1),

        new Vector4(0,-1,-1,0),
        new Vector4(0,-1,-1,0),
        new Vector4(0,-1,1,0),
        new Vector4(0,-1,1,0),
        new Vector4(0,1,-1,0),
        new Vector4(0,1,-1,0),
        new Vector4(0,1,1,0),
        new Vector4(0,1,1,0)
    };
    public static float[][] gradients6D = {
        new float[]{0,1,1,1,1,1},
        new float[]{0,1,1,1,1,-1},
        new float[]{0,1,1,1,-1,1},
        new float[]{0,1,1,1,-1,-1},
        new float[]{0,1,1,-1,1,1},
        new float[]{0,1,1,-1,1,-1},
        new float[]{0,1,1,-1,-1,1},
        new float[]{0,1,1,-1,-1,-1},
        new float[]{0,1,-1,1,1,1},
        new float[]{0,1,-1,1,1,-1},
        new float[]{0,1,-1,1,-1,1},
        new float[]{0,1,-1,1,-1,-1},
        new float[]{0,1,-1,-1,1,1},
        new float[]{0,1,-1,-1,1,-1},
        new float[]{0,1,-1,-1,-1,1},
        new float[]{0,1,-1,-1,-1,-1},
        new float[]{0,-1,1,1,1,1},
        new float[]{0,-1,1,1,1,-1},
        new float[]{0,-1,1,1,-1,1},
        new float[]{0,-1,1,1,-1,-1},
        new float[]{0,-1,1,-1,1,1},
        new float[]{0,-1,1,-1,1,-1},
        new float[]{0,-1,1,-1,-1,1},
        new float[]{0,-1,1,-1,-1,-1},
        new float[]{0,-1,-1,1,1,1},
        new float[]{0,-1,-1,1,1,-1},
        new float[]{0,-1,-1,1,-1,1},
        new float[]{0,-1,-1,1,-1,-1},
        new float[]{0,-1,-1,-1,1,1},
        new float[]{0,-1,-1,-1,1,-1},
        new float[]{0,-1,-1,-1,-1,1},
        new float[]{0,-1,-1,-1,-1,-1},
        new float[]{1,0,1,1,1,1},
        new float[]{1,0,1,1,1,-1},
        new float[]{1,0,1,1,-1,1},
        new float[]{1,0,1,1,-1,-1},
        new float[]{1,0,1,-1,1,1},
        new float[]{1,0,1,-1,1,-1},
        new float[]{1,0,1,-1,-1,1},
        new float[]{1,0,1,-1,-1,-1},
        new float[]{1,0,-1,1,1,1},
        new float[]{1,0,-1,1,1,-1},
        new float[]{1,0,-1,1,-1,1},
        new float[]{1,0,-1,1,-1,-1},
        new float[]{1,0,-1,-1,1,1},
        new float[]{1,0,-1,-1,1,-1},
        new float[]{1,0,-1,-1,-1,1},
        new float[]{1,0,-1,-1,-1,-1},
        new float[]{-1,0,1,1,1,1},
        new float[]{-1,0,1,1,1,-1},
        new float[]{-1,0,1,1,-1,1},
        new float[]{-1,0,1,1,-1,-1},
        new float[]{-1,0,1,-1,1,1},
        new float[]{-1,0,1,-1,1,-1},
        new float[]{-1,0,1,-1,-1,1},
        new float[]{-1,0,1,-1,-1,-1},
        new float[]{-1,0,-1,1,1,1},
        new float[]{-1,0,-1,1,1,-1},
        new float[]{-1,0,-1,1,-1,1},
        new float[]{-1,0,-1,1,-1,-1},
        new float[]{-1,0,-1,-1,1,1},
        new float[]{-1,0,-1,-1,1,-1},
        new float[]{-1,0,-1,-1,-1,1},
        new float[]{-1,0,-1,-1,-1,-1},
        new float[]{1,1,0,1,1,1},
        new float[]{1,1,0,1,1,-1},
        new float[]{1,1,0,1,-1,1},
        new float[]{1,1,0,1,-1,-1},
        new float[]{1,1,0,-1,1,1},
        new float[]{1,1,0,-1,1,-1},
        new float[]{1,1,0,-1,-1,1},
        new float[]{1,1,0,-1,-1,-1},
        new float[]{1,-1,0,1,1,1},
        new float[]{1,-1,0,1,1,-1},
        new float[]{1,-1,0,1,-1,1},
        new float[]{1,-1,0,1,-1,-1},
        new float[]{1,-1,0,-1,1,1},
        new float[]{1,-1,0,-1,1,-1},
        new float[]{1,-1,0,-1,-1,1},
        new float[]{1,-1,0,-1,-1,-1},
        new float[]{-1,1,0,1,1,1},
        new float[]{-1,1,0,1,1,-1},
        new float[]{-1,1,0,1,-1,1},
        new float[]{-1,1,0,1,-1,-1},
        new float[]{-1,1,0,-1,1,1},
        new float[]{-1,1,0,-1,1,-1},
        new float[]{-1,1,0,-1,-1,1},
        new float[]{-1,1,0,-1,-1,-1},
        new float[]{-1,-1,0,1,1,1},
        new float[]{-1,-1,0,1,1,-1},
        new float[]{-1,-1,0,1,-1,1},
        new float[]{-1,-1,0,1,-1,-1},
        new float[]{-1,-1,0,-1,1,1},
        new float[]{-1,-1,0,-1,1,-1},
        new float[]{-1,-1,0,-1,-1,1},
        new float[]{-1,-1,0,-1,-1,-1},
        new float[]{1,1,1,0,1,1},
        new float[]{1,1,1,0,1,-1},
        new float[]{1,1,1,0,-1,1},
        new float[]{1,1,1,0,-1,-1},
        new float[]{1,1,-1,0,1,1},
        new float[]{1,1,-1,0,1,-1},
        new float[]{1,1,-1,0,-1,1},
        new float[]{1,1,-1,0,-1,-1},
        new float[]{1,-1,1,0,1,1},
        new float[]{1,-1,1,0,1,-1},
        new float[]{1,-1,1,0,-1,1},
        new float[]{1,-1,1,0,-1,-1},
        new float[]{1,-1,-1,0,1,1},
        new float[]{1,-1,-1,0,1,-1},
        new float[]{1,-1,-1,0,-1,1},
        new float[]{1,-1,-1,0,-1,-1},
        new float[]{-1,1,1,0,1,1},
        new float[]{-1,1,1,0,1,-1},
        new float[]{-1,1,1,0,-1,1},
        new float[]{-1,1,1,0,-1,-1},
        new float[]{-1,1,-1,0,1,1},
        new float[]{-1,1,-1,0,1,-1},
        new float[]{-1,1,-1,0,-1,1},
        new float[]{-1,1,-1,0,-1,-1},
        new float[]{-1,-1,1,0,1,1},
        new float[]{-1,-1,1,0,1,-1},
        new float[]{-1,-1,1,0,-1,1},
        new float[]{-1,-1,1,0,-1,-1},
        new float[]{-1,-1,-1,0,1,1},
        new float[]{-1,-1,-1,0,1,-1},
        new float[]{-1,-1,-1,0,-1,1},
        new float[]{-1,-1,-1,0,-1,-1},
        new float[]{1,1,1,1,0,1},
        new float[]{1,1,1,1,0,-1},
        new float[]{1,1,1,-1,0,1},
        new float[]{1,1,1,-1,0,-1},
        new float[]{1,1,-1,1,0,1},
        new float[]{1,1,-1,1,0,-1},
        new float[]{1,1,-1,-1,0,1},
        new float[]{1,1,-1,-1,0,-1},
        new float[]{1,-1,1,1,0,1},
        new float[]{1,-1,1,1,0,-1},
        new float[]{1,-1,1,-1,0,1},
        new float[]{1,-1,1,-1,0,-1},
        new float[]{1,-1,-1,1,0,1},
        new float[]{1,-1,-1,1,0,-1},
        new float[]{1,-1,-1,-1,0,1},
        new float[]{1,-1,-1,-1,0,-1},
        new float[]{-1,1,1,1,0,1},
        new float[]{-1,1,1,1,0,-1},
        new float[]{-1,1,1,-1,0,1},
        new float[]{-1,1,1,-1,0,-1},
        new float[]{-1,1,-1,1,0,1},
        new float[]{-1,1,-1,1,0,-1},
        new float[]{-1,1,-1,-1,0,1},
        new float[]{-1,1,-1,-1,0,-1},
        new float[]{-1,-1,1,1,0,1},
        new float[]{-1,-1,1,1,0,-1},
        new float[]{-1,-1,1,-1,0,1},
        new float[]{-1,-1,1,-1,0,-1},
        new float[]{-1,-1,-1,1,0,1},
        new float[]{-1,-1,-1,1,0,-1},
        new float[]{-1,-1,-1,-1,0,1},
        new float[]{-1,-1,-1,-1,0,-1},
        new float[]{1,1,1,1,1,0},
        new float[]{1,1,1,1,-1,0},
        new float[]{1,1,1,-1,1,0},
        new float[]{1,1,1,-1,-1,0},
        new float[]{1,1,-1,1,1,0},
        new float[]{1,1,-1,1,-1,0},
        new float[]{1,1,-1,-1,1,0},
        new float[]{1,1,-1,-1,-1,0},
        new float[]{1,-1,1,1,1,0},
        new float[]{1,-1,1,1,-1,0},
        new float[]{1,-1,1,-1,1,0},
        new float[]{1,-1,1,-1,-1,0},
        new float[]{1,-1,-1,1,1,0},
        new float[]{1,-1,-1,1,-1,0},
        new float[]{1,-1,-1,-1,1,0},
        new float[]{1,-1,-1,-1,-1,0},
        new float[]{-1,1,1,1,1,0},
        new float[]{-1,1,1,1,-1,0},
        new float[]{-1,1,1,-1,1,0},
        new float[]{-1,1,1,-1,-1,0},
        new float[]{-1,1,-1,1,1,0},
        new float[]{-1,1,-1,1,-1,0},
        new float[]{-1,1,-1,-1,1,0},
        new float[]{-1,1,-1,-1,-1,0},
        new float[]{-1,-1,1,1,1,0},
        new float[]{-1,-1,1,1,-1,0},
        new float[]{-1,-1,1,-1,1,0},
        new float[]{-1,-1,1,-1,-1,0},
        new float[]{-1,-1,-1,1,1,0},
        new float[]{-1,-1,-1,1,-1,0},
        new float[]{-1,-1,-1,-1,1,0},
        new float[]{-1,-1,-1,-1,-1,0},
        new float[]{0,1,1,1,1,1},
        new float[]{0,1,1,1,1,-1},
        new float[]{0,1,1,1,-1,1},
        new float[]{0,1,1,1,-1,-1},
        new float[]{0,1,1,-1,1,1},
        new float[]{0,1,1,-1,1,-1},
        new float[]{0,1,1,-1,-1,1},
        new float[]{0,1,1,-1,-1,-1},
        new float[]{0,1,-1,1,1,1},
        new float[]{0,1,-1,1,1,-1},
        new float[]{0,1,-1,1,-1,1},
        new float[]{0,1,-1,1,-1,-1},
        new float[]{0,1,-1,-1,1,1},
        new float[]{0,1,-1,-1,1,-1},
        new float[]{0,1,-1,-1,-1,1},
        new float[]{0,1,-1,-1,-1,-1},
        new float[]{0,-1,1,1,1,1},
        new float[]{0,-1,1,1,1,-1},
        new float[]{0,-1,1,1,-1,1},
        new float[]{0,-1,1,1,-1,-1},
        new float[]{0,-1,1,-1,1,1},
        new float[]{0,-1,1,-1,1,-1},
        new float[]{0,-1,1,-1,-1,1},
        new float[]{0,-1,1,-1,-1,-1},
        new float[]{0,-1,-1,1,1,1},
        new float[]{0,-1,-1,1,1,-1},
        new float[]{0,-1,-1,1,-1,1},
        new float[]{0,-1,-1,1,-1,-1},
        new float[]{0,-1,-1,-1,1,1},
        new float[]{0,-1,-1,-1,1,-1},
        new float[]{0,-1,-1,-1,-1,1},
        new float[]{0,-1,-1,-1,-1,-1},
        new float[]{1,0,1,1,1,1},
        new float[]{1,0,1,1,1,-1},
        new float[]{1,0,1,1,-1,1},
        new float[]{1,0,1,1,-1,-1},
        new float[]{1,0,1,-1,1,1},
        new float[]{1,0,1,-1,1,-1},
        new float[]{1,0,1,-1,-1,1},
        new float[]{1,0,1,-1,-1,-1},
        new float[]{1,0,-1,1,1,1},
        new float[]{1,0,-1,1,1,-1},
        new float[]{1,0,-1,1,-1,1},
        new float[]{1,0,-1,1,-1,-1},
        new float[]{1,0,-1,-1,1,1},
        new float[]{1,0,-1,-1,1,-1},
        new float[]{1,0,-1,-1,-1,1},
        new float[]{1,0,-1,-1,-1,-1},
        new float[]{-1,0,1,1,1,1},
        new float[]{-1,0,1,1,1,-1},
        new float[]{-1,0,1,1,-1,1},
        new float[]{-1,0,1,1,-1,-1},
        new float[]{-1,0,1,-1,1,1},
        new float[]{-1,0,1,-1,1,-1},
        new float[]{-1,0,1,-1,-1,1},
        new float[]{-1,0,1,-1,-1,-1},
        new float[]{-1,0,-1,1,1,1},
        new float[]{-1,0,-1,1,1,-1},
        new float[]{-1,0,-1,1,-1,1},
        new float[]{-1,0,-1,1,-1,-1},
        new float[]{-1,0,-1,-1,1,1},
        new float[]{-1,0,-1,-1,1,-1},
        new float[]{-1,0,-1,-1,-1,1},
        new float[]{-1,0,-1,-1,-1,-1},
    };
}
