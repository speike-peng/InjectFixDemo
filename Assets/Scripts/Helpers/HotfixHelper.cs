using System;
using System.IO;
using IFix.Core;
using UnityEngine;


public class HotfixHelper
{
    /// <summary>
    /// 需要热更的DLL
    /// </summary>
    public static string[] assemblys = new[]
    {
    "Assembly-CSharp",
};

    public static void InitInjectFix()
    {
        try
        {
            VirtualMachine.Info = (s) => DebugHelper.Log(s);

            foreach (var assembly in assemblys)
            {
                InjectPatch($"IFixPatches/{assembly}.patch.bytes");
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogError(e.ToString());
        }
    }

    //====================================================================
    static void InjectPatch(string targetPath)
    {
        var patch = AssetHelper.Load<TextAsset>(targetPath);
        if (patch != null)
        {
            DebugHelper.LogEvent($"IFix loading {targetPath} ...");
            PatchManager.Load(new MemoryStream(patch.bytes));
            AssetHelper.Unload(targetPath, true);
        }
    }

}

