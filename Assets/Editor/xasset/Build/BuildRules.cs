using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    [CreateAssetMenu(menuName = "ScriptableObject/BuildRules")]
    public class BuildRules : ScriptableObject
    {
        private readonly List<string> _duplicated = new List<string>();
        private readonly Dictionary<string, string[]> _conflicted = new Dictionary<string, string[]>();
        private readonly Dictionary<string, HashSet<string>> _tracker = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, string> _asset2Bundles = new Dictionary<string, string>(); 

        [Header("Patterns")]
        public string searchPatternDir = "*";
        public string searchPatternFile = "*.*";

        [Tooltip("构建的版本号")]  
        public int version;
        [Tooltip("是否把资源名字哈希处理")]
        public bool nameByHash = false;
        [Tooltip("打包选项")]
        public BuildAssetBundleOptions buildBundleOptions = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle|BuildAssetBundleOptions.DisableWriteTypeTree;
        [Tooltip("BuildPlayer 的时候被打包的场景")] 
        public SceneAsset[] scenesInBuild = new SceneAsset[0];
        [Tooltip("打包的来源"), Header("可以在文件夹上右键操作")]
        public BuildRule[] rules = new BuildRule[0];

        [Tooltip("所有要打包的资源")]
        private AssetBuild[] assets = new AssetBuild[0]; 
        [Tooltip("所有打包的bundle")]
        private BundleBuild[] bundles = new BundleBuild[0];
        
        #region API

        public void GroupAsset(string path, GroupBy groupBy = GroupBy.Filename)
        { 
            var asset = ArrayUtility.Find(assets, build => build.path.Equals(path));
            if (asset != null)
            {
                asset.groupBy = groupBy; 
                return;
            }
            ArrayUtility.Add(ref assets, new AssetBuild()
            {
                path = path,
                groupBy = groupBy, 
            }); 
        } 
        
        public void PatchAsset(string path)
        { 
            var asset = ArrayUtility.Find(assets, bundleAsset => bundleAsset.path.Equals(path));
            if (asset != null)
            {
                return;
            }
            ArrayUtility.Add(ref assets, new AssetBuild()
            {
                path = path,
            }); 
        } 

        public int AddVersion()
        {
            return version;
        }

        public void Build()
        {
            Clear();
            CollectAssets();
            AnalysisAssets();
            OptimizeAssets();
            Save();
        }

        public AssetBundleBuild[] GetBuilds()
        {
            return Array.ConvertAll(bundles, input => input.ToBuild());
        }

        #endregion

        #region Private

        private string GetBundle(AssetBuild assetBuild)
        {
            if (assetBuild.path.EndsWith(".shader"))
            {
                return RuledAssetBundleName("shaders");
            }
            switch (assetBuild.groupBy)
            {
                case GroupBy.Explicit: 
                    return RuledAssetBundleName(assetBuild.group);
                case GroupBy.Filename: 
                    return RuledAssetBundleName(Path.Combine(Path.GetDirectoryName(assetBuild.path), Path.GetFileNameWithoutExtension(assetBuild.path)));
                case GroupBy.Directory: 
                    return RuledAssetBundleName(Path.GetDirectoryName(assetBuild.path));
                default: return string.Empty;
            }
        }

        internal static bool ValidateAsset(string asset)
        {
            if (!asset.StartsWith("Assets/")) return false;

            var ext = Path.GetExtension(asset).ToLower();
            return !string.IsNullOrEmpty(ext) && ext != ".dll" && ext != ".cs" && ext != ".meta" && ext != ".js" && ext != ".boo";
        }

        private bool IsScene(string asset)
        {
            return asset.EndsWith(".unity");
        }

        private string RuledAssetBundleName(string assetName)
        {
            if (nameByHash)
            {
#if UNITY_EDITOR_OSX
                return Utility.ComputeHash(assetName) + ".bundle";
#else
                return Utility.ComputeHash(assetName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".bundle";
#endif
            }

            return assetName.Replace("\\", "/").ToLower() + ".bundle";
        }

        private string RuledAssetBundleName(string asset, string assetName, int subBundleIndex = 0)
        {
            if (asset.EndsWith(".shader"))
                return RuledAssetBundleName("shaders");
            else
            {
                if (subBundleIndex > 0)
                {
                    return RuledAssetBundleName(assetName + subBundleIndex);
                }
                else
                {
                    return RuledAssetBundleName(assetName);
                }
            }
                
        }

        private void Track(string asset, string bundle)
        {
            if (! _asset2Bundles.ContainsKey(asset))
            {
                _asset2Bundles[asset] = Path.GetFileNameWithoutExtension(bundle) + "_children" + ".bundle";
            }
            
            HashSet<string> hashSet;
            if (!_tracker.TryGetValue(asset, out hashSet))
            {
                hashSet = new HashSet<string>();
                _tracker.Add(asset, hashSet);
            }
            
            hashSet.Add(bundle);
            
            if (hashSet.Count > 1)
            {
                string bundleName;
                _asset2Bundles.TryGetValue(asset, out bundleName);
                if (string.IsNullOrEmpty(bundleName))
                {
                    _duplicated.Add(asset);
                }
            }
        }

        private Dictionary<string, List<string>> GetBundles()
        {
            var dictionary = new Dictionary<string, List<string>>();
            foreach (var item in _asset2Bundles)
            {
                var bundle = item.Value;
                List<string> list;
                if (!dictionary.TryGetValue(bundle, out list))
                {
                    list = new List<string>();
                    dictionary[bundle] = list;
                }

                if (!list.Contains(item.Key)) list.Add(item.Key);
            }

            return dictionary;
        }

        private void Clear()
        {
            _tracker.Clear();
            _duplicated.Clear();
            _conflicted.Clear();
            _asset2Bundles.Clear();
        }

        private void Save()
        {
            var getBundles = GetBundles();

            bundles = new BundleBuild[getBundles.Count];
            var i = 0;
            foreach (var item in getBundles)
            {
                bundles[i] = new BundleBuild
                {
                    assetBundleName = item.Key,
                    assetNames = item.Value.ToArray()
                };
                i++;
            }

            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void OptimizeAssets()
        {
            int i = 0, max = _conflicted.Count;
            foreach (var item in _conflicted)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("优化冲突{0}/{1}", i, max), item.Key,
                    i / (float) max)) break;
                var list = item.Value;
                foreach (var asset in list)
                    if (!IsScene(asset))
                        _duplicated.Add(asset);
                i++;
            }

            for (i = 0, max = _duplicated.Count; i < max; i++)
            {
                var item = _duplicated[i];
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("优化冗余{0}/{1}", i, max), item,
                    i / (float) max)) break;
                OptimizeAsset(item);
            }
            
            string shadersBundleName = RuledAssetBundleName("shaders");
            i = 0;
            max = _asset2Bundles.Count;

            List<string> needChangeList = new List<string>(100);
            foreach (var kv in _asset2Bundles)
            {
                if (kv.Key.EndsWith(".shader"))
                {
                    if (kv.Value != shadersBundleName)
                    {
                        needChangeList.Add(kv.Key);
                    }
                }
                
                i++;
            }
            
            i = 0;
            max = needChangeList.Count;
            foreach (var str in needChangeList)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("shaders提取到同一bundle {0}/{1}", i, max), str,
                    i / (float) max)) break;
                _asset2Bundles[str] = shadersBundleName;
                
                i++;
            }
            
        }

        private void AnalysisAssets()
        {
            var getBundles = GetBundles();
            int i = 0, max = getBundles.Count;
            foreach (var item in getBundles)
            {
                var bundle = item.Key;
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("分析依赖{0}/{1}", i, max), bundle,
                    i / (float) max)) break;
                var assetPaths = getBundles[bundle];
                if (assetPaths.Exists(IsScene) && !assetPaths.TrueForAll(IsScene))
                    _conflicted.Add(bundle, assetPaths.ToArray());
                var dependencies = AssetDatabase.GetDependencies(assetPaths.ToArray(), true);
                if (dependencies.Length > 0)
                    foreach (var asset in dependencies)
                        if (ValidateAsset(asset))
                            Track(asset, bundle);
                i++;
            }
        }

        private void CollectAssets()
        {
            for (int i = 0, max = rules.Length; i < max; i++)
            {
                var rule = rules[i];
                ApplyRule(rule);
            }

            var list = new List<AssetBuild>();
            foreach (var item in _asset2Bundles)
                list.Add(new AssetBuild
                {
                    path = item.Key,
                    bundle = item.Value
                });
            list.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));
            assets = list.ToArray();
        }

        private void OptimizeAsset(string asset)
        {
            if (asset.EndsWith(".shader"))
                _asset2Bundles[asset] = RuledAssetBundleName("shaders");
            else
                _asset2Bundles[asset] = RuledAssetBundleName(asset);
        }

        private void ApplyRule(BuildRule rule)
        {
            var assets = rule.GetAssets();
            List<BundleAsset> bundleList = rule.GetBundleListAssets();

            switch (rule.groupBy)
            {
                case GroupBy.Explicit:
                    {
                        if (bundleList != null)
                        {
                            for (int i = 0; i < bundleList.Count; ++i)
                            {
                                foreach (var asset in bundleList[i].assetNames)
                                    _asset2Bundles[asset] = RuledAssetBundleName(asset, rule.assetBundleName, bundleList[i].index);
                            }
                        }
                        else
                            foreach (var asset in assets) _asset2Bundles[asset] = RuledAssetBundleName(asset, rule.assetBundleName);

                        break;
                    }
                case GroupBy.Filename:
                    {
                        foreach(var asset in assets)
                        {
                            _asset2Bundles[asset] = RuledAssetBundleName(asset, Path.Combine(Path.GetDirectoryName(asset), Path.GetFileNameWithoutExtension(asset)));
                        }
                        break;
                    }
                case GroupBy.Path:
                    {
                        if (bundleList != null)
                        {
                            for (int i = 0; i < bundleList.Count; ++i)
                            {
                                foreach (var asset in bundleList[i].assetNames)
                                    _asset2Bundles[asset] = RuledAssetBundleName(asset, asset, bundleList[i].index);
                            }
                        }
                        else
                            foreach (var asset in assets) _asset2Bundles[asset] = RuledAssetBundleName(asset, asset);

                        break;
                    }
                case GroupBy.Directory:
                    {
                        // 有自动分包
                        if (bundleList != null)
                        {
                            for(int i=0; i < bundleList.Count; ++i)
                            {
                                foreach (var asset in bundleList[i].assetNames)
                                    _asset2Bundles[asset] = RuledAssetBundleName(asset, Path.GetDirectoryName(asset), bundleList[i].index);
                            }
                        }
                        else
                        {
                            foreach (var asset in assets)
                                _asset2Bundles[asset] = RuledAssetBundleName(asset, Path.GetDirectoryName(asset));
                        }

                        break;
                    }
                case GroupBy.TopDirectory:
                    {
                        var startIndex = rule.searchPath.Length;
                        foreach (var asset in assets)
                        {
                            var dir = Path.GetDirectoryName(asset);
                            dir = dir.Replace("\\", "/");
                            if (!string.IsNullOrEmpty(dir))
                                if (!dir.Equals(rule.searchPath))
                                {
                                    var pos = dir.IndexOf("/", startIndex + 1, StringComparison.Ordinal);
                                    if (pos != -1) 
                                        dir = dir.Substring(0, pos);
                                }

                            _asset2Bundles[asset] = RuledAssetBundleName(asset, dir);
                        }

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#endregion
    }
}