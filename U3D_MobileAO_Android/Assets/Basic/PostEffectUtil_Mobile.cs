using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostEffectUtil_Mobile
{

    #region //手机等级划分
    public enum MobileQuality
    {
        Low = 0,    //对标MaliT830：低端
        Middle = 1, //对标Adreno 530：中端，前几年的旗舰机
        High = 2,   //对标？？：高端，旗舰机
    }
    public static RenderTextureFormat GetFormat(MobileQuality level)
    {
        switch (level)
        {
            case MobileQuality.Low:
                return RenderTextureFormat.ARGB32;
            case MobileQuality.Middle:
                return RenderTextureFormat.ARGBHalf;
            case MobileQuality.High:
                return RenderTextureFormat.ARGBHalf;
            default:
                return RenderTextureFormat.ARGBHalf;
        }

    }

    public static float GetResolutionRatio(MobileQuality level)
    {
        switch (level)
        {
            case MobileQuality.Low:
                return 1.25f;
            case MobileQuality.Middle:
                return 1;
            case MobileQuality.High:
                return 1;
            default:
                return 1;
        }
    }

    public static int GetDepthRatio(MobileQuality level)
    {
        switch (level)
        {
            case MobileQuality.Low:
                return 1;
            case MobileQuality.Middle:
                return 4;
            case MobileQuality.High:
                return 1;
            default:
                return 2;
        }
    }
#endregion

}
