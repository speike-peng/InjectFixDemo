using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

using xasset.editor;

public class AutoBuild
{
    private static BuildConfig buildConfig;

    private static void InitConfig()
    {
        if (buildConfig == null)
        {
            buildConfig = (BuildConfig)AssetDatabase.LoadAssetAtPath("Assets/Build Config.asset", typeof(BuildConfig));
        }


    }

    public static string GetCommandLineArg(string arg)
    {
        string result = "";
        string[] cmdLineArgs = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < cmdLineArgs.Length - 1; i++)
        {
            var argKey = cmdLineArgs[i];
            if (argKey.StartsWith(arg))
            {
                result = cmdLineArgs[i + 1];
                break;
            }
        }
        Debug.LogFormat("GetCommandLineArg:{0} val:{1}", arg, result.Trim());
        return result.Trim();
    }

    private static string GetTargetBuildDir(BuildTarget target)
    {
        string targetBuildDir = "";
        var targetBuildDirFormat = "../Build/{0}/";
        switch (target)
        {
            case BuildTarget.iOS:
                targetBuildDir = string.Format(targetBuildDirFormat, "XCodeProject");
                break;
            case BuildTarget.Android:
                targetBuildDir = string.Format(targetBuildDirFormat, "AndroidProject");
                break;
            default:
                targetBuildDir = string.Format(targetBuildDirFormat, target.ToString());
                break;
        }

        var buildNum = GetCommandLineArg("-BuildNum");
        if (string.IsNullOrEmpty(buildNum))
        {
            buildNum = buildConfig.BuildNum;
        }

        targetBuildDir += buildNum;

        return targetBuildDir;
    }

    /// <summary>
    /// 查找已启用的场景文件
    /// </summary>
    /// <returns></returns>
    private static string[] FindEnabledEditorScenes()
    {
        List<string> editorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;

            editorScenes.Add(scene.path);

            Debug.LogWarningFormat("Enabled scene:" + scene.path);
        }
        return editorScenes.ToArray();
    }

    private static void GenericBuild(string targetDir, BuildTargetGroup groupTarget, BuildTarget buildTarget,
        BuildOptions buildOptions)
    {
        //PreProcessBuild.Process(buildTarget);

        //IFix.Editor.IFixEditor.AutoInject = true;
        //IFix.Editor.IFixEditor.DeleteAllPatch();
        //if (buildTarget == BuildTarget.Android)
        //    IFix.Editor.IFixEditor.CompileToAndroid();
        //else if (buildTarget == BuildTarget.iOS)
        //    IFix.Editor.IFixEditor.CompileToIOS();
        //else
        //    IFix.Editor.IFixEditor.Patch();
        //IFix.Editor.IFixEditor.InjectAllAssemblys();

        //MSDKSetting.SetAllJsonConfig();

        //OtherBuildConfig.RestoreCustomDefine(groupTarget);

        //// 切换 DEBUG 相关配置
        //buildOptions = SwitchDebugSettings(buildOptions);

        //if (!OtherBuildConfig.BuildCustomConfg(buildTarget))
        //    return;

        //SetGenericSettings(groupTarget);

        //CleanTargetDir(targetDir);

        BuildPlayer(targetDir, buildTarget, buildOptions);

        if (buildConfig._GeneralAPK)
        {
            GeneralAPK(targetDir, "", "", "", "", false);
        }

    }

    private static void BuildPlayer(string targetDir, BuildTarget buildTarget, BuildOptions buildOptions, bool isServer = false)
    {
        string[] scenes = FindEnabledEditorScenes();
        if (!isServer)
        {
            xasset.editor.BuildRules buildRule = xasset.editor.RuleTools.GetBuildRule();

            int _len = buildRule.scenesInBuild.Length;
            if (_len != 0)
            {
                scenes = new string[_len];
                for (int i = 0; i < _len; i++)
                {
                    SceneAsset sceneAsset = buildRule.scenesInBuild[i];
                    scenes[i] = AssetDatabase.GetAssetPath(sceneAsset);
                }
            }
        }
        if (scenes != null && scenes.Length == 0)
        {
            Debug.LogError("Nothing to build.");
            return;
        }

        var buildReport = BuildPipeline.BuildPlayer(scenes, targetDir, buildTarget, buildOptions);

        if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("BuildPlayer Success");
        }
        else
        {
            Debug.LogErrorFormat("BuildPlayer Error:{0}", buildReport.summary.result.ToString());
        }
    }

    private static void GeneralAPK(string buildTarget, string buildMode, string CurTime, string BuildNum, string Version, bool IsPatch)
    {
        string assembleMode = "assembleRelease";
        string iosBuildMode = "Release";
        if (buildMode == "DEBUG")
        {
            assembleMode = "assembleDebug";
            iosBuildMode = "Debug";
        }
        string projectPath = buildTarget;
        //if (buildTarget == "iPhone")
        //{
        //    projectPath = GlobalVal.buildPath + "/XCodeProject/" + BuildNum;
        //}
        //else if (buildTarget == "Android")
        //{
        //    projectPath = GlobalVal.buildPath + "/AndroidProject/" + BuildNum;
        //}

        //    if (File.exists(GlobalVal.buildPath))
        //       Dir(GlobalVal.buildPath);
        //if (File.exists(GlobalVal.buildPath + "/ApkFiles/"))
        //    os.makedirs(GlobalVal.buildPath + "/ApkFiles/");
        //if (File.exists(GlobalVal.buildPath + "/IpaFiles/"))
        //    os.makedirs(GlobalVal.buildPath + "/IpaFiles/");

        string cmd1 = "cd " + projectPath;
        string cmd2 = "gradle " + assembleMode;
        string cmd = cmd1 + " && " + cmd2;
        CMDHelper.OnDataReceivedHandler += (log) =>
        {

        };
        CMDHelper.Call(cmd);

        string buildOutputApkPath = projectPath + "/launcher/build/outputs/apk/release/launcher-release.apk";
        if (buildMode == "DEBUG")
        {
            buildOutputApkPath = projectPath + "/launcher/build/outputs/apk/debug/launcher-debug.apk";
        }

        string basePath = projectPath + "/../../Products/";
        if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

        //拷贝APK包
        string dirPath = basePath + buildConfig.ProductName + "_" + buildConfig.BuildNum + ".apk";
        File.Copy(buildOutputApkPath, dirPath, true);
        //拷贝基础资源

        ZipBundles();

        IOTools.DeleteAllDirectory(projectPath);

        UnityEditor.EditorUtility.DisplayDialog("编译", "生成完毕", "关闭");
    }

    /// <summary>
    /// 压缩ZIP
    /// </summary>
    private static void ZipBundles()
    {
        InitConfig();

        string bundlePath = Application.dataPath + "/../Bundles";
        string dirFile = Application.dataPath + "/../../Build/Products/";

        string bundleDir = dirFile + "Bundles/Android/";
        if (!Directory.Exists(dirFile + "Bundles"))
            Directory.CreateDirectory(dirFile + "Bundles");
        if (!Directory.Exists(bundleDir))
            Directory.CreateDirectory(bundleDir);

        string[] jsonFiles = Directory.GetFiles(bundlePath, "*.json", SearchOption.AllDirectories);
        foreach (string _file in jsonFiles)
        {
            File.Copy(_file, bundleDir + Path.GetFileName(_file), true);
        }

        string[] bundleFiles = Directory.GetFiles(bundlePath, "*.bundle", SearchOption.AllDirectories);
        
        List<string> fileList = new List<string>();
        foreach (string _file in bundleFiles)
        {
            fileList.Add(Path.GetFileNameWithoutExtension(_file));
        }
        List<string> realFileList = new List<string>();
        foreach (string _file in fileList)
        {
            foreach (string _file_ in fileList)
            {
               if(_file_ != _file &&  _file_.Contains(_file))
                {
                    realFileList.Add(_file_);
                }
            }
        }
        foreach (string _file in bundleFiles)
        {
            foreach (string _file_ in realFileList)
            {
                if(_file.Contains(_file_))
                {
                    File.Copy(_file, bundleDir + Path.GetFileName(_file), true);
                }
            }   
        }

        string zipedFile = dirFile + "Bundles_" + buildConfig.BuildNum + ".zip";
        if (File.Exists(zipedFile))
            File.Delete(zipedFile);

        ZipHelper.ZipFileDirectory(dirFile + "Bundles", zipedFile);
        IOTools.DeleteAllDirectory(dirFile + "Bundles/");
    }
    //=========================================================================================
    //=========================================================================================

    /// <summary>
    /// 1、点击 Build Android APK 生成基础的APK包
    /// 2、点击 Build Increment Bundles 生成 增量包，将生成的增量包放置在服务器上，即可实现资源更新
    /// Build Rules 需要把场景添加到 Rules里，不能添加到 Scenes In Build
    /// </summary>
    [MenuItem("Build/Build Android APK", false, 5)]
    public static void AndroidBuild()
    {
        InitConfig();
        if (buildConfig.BuildNumAutoAdd)
        {
            int buildName = int.Parse(buildConfig.BuildNum.Trim());
            buildName++;
            buildConfig.BuildNum = buildName.ToString();
        }

        Settings.GetDefaultSettings().scriptPlayMode = ScriptPlayMode.Increment;

        PlayerSettings.productName = buildConfig.ProductName;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        PlayerSettings.Android.keystoreName = Application.dataPath.Replace("Assets", buildConfig.KeystoreName);

        PlayerSettings.Android.keyaliasPass = buildConfig.KeyaliasPass;
        PlayerSettings.Android.keyaliasName = buildConfig.KeyaliasName;
        PlayerSettings.Android.keystorePass = buildConfig.KeystorePass;

        EditorUserBuildSettings.exportAsGoogleAndroidProject = buildConfig.ExportAsGoogleAndroidProject;

        var targetBuildDir = GetTargetBuildDir(BuildTarget.Android);

        //清除掉所有的bundle包
        ClearBuild();

        RuleTools.rulePath = "Assets/Base Build Rules.asset";
        //编译基础资源
        _BuildRules();
        BuildScript.BuildBundles();

        GenericBuild(targetBuildDir, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);

        Settings.GetDefaultSettings().scriptPlayMode = ScriptPlayMode.Preload;
    }

    [MenuItem("Build/Build Increment Bundles", false, 100)]
    public static void BuildBundles()
    {
        Settings.GetDefaultSettings().scriptPlayMode = ScriptPlayMode.Increment;

        RuleTools.rulePath = "Assets/Build Rules.asset";
        _BuildRules();
        BuildScript.BuildBundles();

        ZipBundles();

        Settings.GetDefaultSettings().scriptPlayMode = ScriptPlayMode.Preload;
    }

    //=====================================================================================

    //[MenuItem("Build/Clear Build", false, 800)]
    private static void ClearBuild()
    {
        BuildScript.ClearBuild();
    }
    private static void _BuildRules()
    {
        BuildRules buildRules = RuleTools.GetBuildRule();
        buildRules?.Build();
    }


}
