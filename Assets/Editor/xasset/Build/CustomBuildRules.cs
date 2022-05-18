using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    /// <summary>
    /// 资源打包的分组方式
    /// </summary>
    public enum GroupBy
    {
        None, 
        Explicit,
        Filename,
        Directory,
        Path,
        TopDirectory
    }

    [Serializable]
    public class AssetBuild
    {
        public string path;
        public string bundle = string.Empty;
        public GroupBy groupBy = GroupBy.Filename; 
        public string group = string.Empty;
    }

    [Serializable]
    public class BundleBuild
    {
        public string assetBundleName;
        public string[] assetNames;
        
        public AssetBundleBuild ToBuild()
        {
            return new AssetBundleBuild()
            {
                assetBundleName = assetBundleName,
                assetNames = assetNames
            };
        }
    }

    [Serializable]
    public class BundleAsset
    {
        public int index;
        public string assetDir;
        public List<string> assetNames;
    }

    [Serializable]
    public class BuildRule
    {
        [Tooltip("搜索路径")] public string searchPath;

        [Tooltip("搜索通配符，多个之间请用,(逗号)隔开")] public string searchPattern;

        [Tooltip("命名规则")] public GroupBy groupBy = GroupBy.Directory;

        [Tooltip("Explicit 的名称")] public string assetBundleName = string.Empty;

        [Tooltip("分包大小(M)")]
        public int MaxBundleSize = 0;

        [Tooltip("所有资源列表 （纪录）")]
        public List<string> Assets = new List<string>();

        [Tooltip("自动分包情况（纪录）")]
        public BundleAsset[] bundleList = new BundleAsset[0];

        public string[] GetAssets()
        {
            var patterns = searchPattern.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (!Directory.Exists(searchPath))
            {
                if (groupBy == GroupBy.Filename)
                    return new string[1] { searchPath };
                Debug.LogWarning("Rule searchPath not exist:" + searchPath);
                return new string[0];
            }

            var getFiles = new List<string>();
            foreach (var item in patterns)
            {
                var files = Directory.GetFiles(searchPath, item, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (Directory.Exists(file)) continue;
                    var ext = Path.GetExtension(file).ToLower();
                    if ((ext == ".fbx"/* || ext == ".anim"*/) && !item.Contains(ext)) continue;
                    if (!BuildRules.ValidateAsset(file)) continue;
                    var asset = file.Replace("\\", "/");
                    getFiles.Add(asset);
                }
            }

            foreach (var file in getFiles)
            {
                if (!Assets.Contains(file))
                {
                    Assets.Add(file);
                }
            }

            return getFiles.ToArray();
        }

        public FileInfo GetAssetFile(string asset)
        {
            string projectRoot = Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            projectRoot = projectRoot.Replace('\\', '/');

            FileInfo archiveFile = new FileInfo(Path.Combine(projectRoot, asset));
            if (!archiveFile.Exists)
                return null;

            return archiveFile;
        }

        public bool IsInBundle(string asset)
        {
            foreach(var assetlist in bundleList)
            {
                if (assetlist.assetNames.Contains(asset))
                {
                    return true;
                }
            }

            return false;
        }
        

        private long GetBundleSize(List<BundleAsset> bundles, string dir)
        {
            long countSize = 0;
            if (bundles.Count > 0)
            {
                for (int i = bundles.Count - 1; i >= 0; i--)
                {
                    var lastBundle = bundles[i];

                    if (lastBundle != null && lastBundle.assetDir == dir)
                    {
                        foreach (var asset in lastBundle.assetNames)
                        {
                            FileInfo fileInfo = GetAssetFile(asset);
                            if (fileInfo!=null && fileInfo.Exists)
                            {
                                countSize += fileInfo.Length;
                            }
                        }
                        return countSize;
                    }
                }
            }

            return countSize;
        }

        private BundleAsset GetLastBundleAsset(List<BundleAsset> bundles,string dir)
        {
            for (int i = bundles.Count - 1; i >= 0; i--)
            {
                var lastBundle = bundles[i];

                if (lastBundle != null && lastBundle.assetDir == dir)
                {
                    return lastBundle;
                }
            }

            return null;
        }

        // 根据AB包设定的大小，自动分包
        public List<BundleAsset> GetBundleListAssets()
        {
            if (MaxBundleSize == 0)
            {
                return null;
            }
            
            List<BundleAsset> bundles = new List<BundleAsset>();
            // initial
            bundles.AddRange(bundleList);

            BundleAsset bundleFiles;

            long countSize = 0;

            foreach (var asset in Assets)
            {
                if (IsInBundle(asset))
                {
                    continue;
                }

                string dir = Path.GetDirectoryName(asset);
                bundleFiles = GetLastBundleAsset(bundles, dir);
                if (bundleFiles == null)
                {
                    bundleFiles = new BundleAsset
                    {
                        index = 0,
                        assetDir = dir,
                        assetNames = new List<string>()
                    };

                    bundles.Add(bundleFiles);
                }

                if (countSize == 0)
                {
                    countSize = GetBundleSize(bundles, dir);
                }

                FileInfo fileInfo = GetAssetFile(asset);
                if (fileInfo.Exists)
                {
                    bundleFiles.assetNames.Add(asset);
                    countSize += fileInfo.Length;
                }
                
                if (countSize >= MaxBundleSize * 1024 * 1024)
                {
                    // reset
                    countSize = 0;
                    int newindex = bundleFiles.index + 1;
                    bundleFiles = new BundleAsset
                    {
                        index = newindex,
                        assetDir = dir,
                        assetNames = new List<string>()
                    };

                    bundles.Add(bundleFiles);
                }
            }

            //save result
            bundleList = bundles.ToArray();

            return bundles;
        }
    }

    //===========================================================================
    //===========================================================================

    public class RuleTools
    {
        public static string rulePath = "Assets/Build Rules.asset";
        public static BuildRules GetBuildRule()
        {
            BuildRules buildRule = AssetDatabase.LoadAssetAtPath<BuildRules>(rulePath);

            return buildRule;
        }

        [MenuItem("Assets/Build Rules/Add by Filename")]
        private static void GroupByFilename()
        {
            //GroupAssets(GroupBy.Filename);
            var rules = GetBuildRule();
            AddRulesForSelection(rules, rules.searchPatternFile, GroupBy.Filename);
        }

        [MenuItem("Assets/Build Rules/Add by Directory")]
        private static void GroupByDirectory()
        {
            //GroupAssets(GroupBy.Directory);
            var rules = GetBuildRule();
            AddRulesForSelection(rules, rules.searchPatternDir, GroupBy.Directory);
        }

        [MenuItem("Assets/Build Rules/Add by TopDirectory")]
        private static void GroupByTopDirectory()
        {
            var rules = GetBuildRule();
            AddRulesForSelection(rules, rules.searchPatternDir, GroupBy.TopDirectory);
        }

        [MenuItem("Assets/Build Rules/Explicit")]
        private static void GroupByExplicitLevel1()
        {
            var rules = GetBuildRule();
            AddRulesForSelection(rules, rules.searchPatternFile, GroupBy.Explicit);
        }

        private static void AddRulesForSelection(BuildRules rules, string searchPattern, GroupBy groupBy)
        {
            foreach (var item in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(item);

                BuildRule rule = null; 

                for(int i =0, n = rules.rules.Length; i< n; i++ )
                {
                    BuildRule _rule = rules.rules[i];
                    if (_rule.searchPath == path)
                    {
                        rule = _rule;
                        break;
                    }
                }

                if(rule == null)
                {
                    rule = new BuildRule
                    {
                        searchPath = path,
                        searchPattern = searchPattern,
                        groupBy = groupBy
                    };
                }else
                {
                    rule.searchPattern = searchPattern;
                    rule.groupBy = groupBy;
                }
                
                bool result = ArrayUtility.Contains(rules.rules, rule);
                if(result == false)
                {
                    ArrayUtility.Add(ref rules.rules, rule);
                }
            }

            UnityEditor.EditorUtility.SetDirty(rules);
            AssetDatabase.SaveAssets();
        }

    }


}