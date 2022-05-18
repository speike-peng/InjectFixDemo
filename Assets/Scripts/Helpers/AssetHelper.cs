using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using xasset;
using Object = UnityEngine.Object;


public class AssetHelper
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

        return false;

#else
            return true;
#endif
    }
    //===================================================================
    static Object LoadAsset(string path, Type type)
    {

#if UNITY_EDITOR
        var ret = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
        return ret;
#else
            Asset _asset = Asset.Load(path, type);
            if (_asset == null) return null;

            return _asset.asset;
#endif
    }


}


