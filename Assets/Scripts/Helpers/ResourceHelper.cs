using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
public class ResourceHelper
{
    public static void Init()
    {

    }


    public static T Load<T>(string path) where T : Object
    {
        T ret = Load(path, typeof(T)) as T;
        return ret;
    }

    public static Object Load(string path, Type type)
    {
       return LoadAsset("Assets/ABundles/" + path, type);
    }

    public static bool Unload(string path, bool immediately = false)
    {
#if UNITY_EDITOR


#else
        string assetPath = libx.Assets.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(assetPath))
        {
            return UnloadAsset(assetPath, immediately);
        }
#endif
        return false;
    }
    //===================================================================
    static Object LoadAsset(string path, Type type)
    {

#if UNITY_EDITOR
        var ret = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
        return ret;
#else


#endif
    }


}
