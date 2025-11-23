using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace vietlabs.fr2
{
    internal partial class FR2_Cache
    {
        internal List<List<string>> ScanSimilar(Action IgnoreWhenScan, Action IgnoreFolderWhenScan)
        {
            if (AssetMap == null) Check4Changes(true);

            var dict = new Dictionary<string, List<FR2_Asset>>();
            foreach (KeyValuePair<string, FR2_Asset> item in AssetMap)
            {
                if (item.Value == null) continue;
                if (item.Value.IsMissing || item.Value.IsFolder) continue;
                if (item.Value.inPlugins) continue;
                if (item.Value.inEditor) continue;
                if (item.Value.IsExcluded) continue;
                if (!item.Value.assetPath.StartsWith("Assets/")) continue;
                if (FR2_Setting.IsTypeExcluded(FR2_AssetGroupDrawer.GetIndex(item.Value.extension)))
                {
                    if (IgnoreWhenScan != null) IgnoreWhenScan();
                    continue;
                }

                string hash = item.Value.fileInfoHash;
                if (string.IsNullOrEmpty(hash))
                {
                    FR2_LOG.LogWarning("Hash can not be null! ");
                    continue;
                }

                if (!dict.TryGetValue(hash, out List<FR2_Asset> list))
                {
                    list = new List<FR2_Asset>();
                    dict.Add(hash, list);
                }

                list.Add(item.Value);
            }

            return dict.Values
                .Where(item => item.Count > 1)
                .OrderByDescending(item => item[0].fileSize)
                .Select(item => item.Select(asset => asset.assetPath).ToList())
                .ToList();
        }

        internal List<FR2_Asset> ScanUnused(bool recursive = true)
        {
            if (AssetMap == null) Check4Changes(false);

            // Get Addressable assets
            HashSet<string> addressable = FR2_Addressable.isOk ? FR2_Addressable.GetAddresses()
                .SelectMany(item => item.Value.assetGUIDs.Union(item.Value.childGUIDs))
                .ToHashSet() : new HashSet<string>();

            var result = new List<FR2_Asset>();
            var unusedAssets = new HashSet<string>();
            
            // First pass: find directly unused assets (level 1)
            foreach (KeyValuePair<string, FR2_Asset> item in AssetMap)
            {
                FR2_Asset v = item.Value;
                if (v.IsMissing || v.inEditor || v.IsScript || v.inResources || v.inPlugins || v.inStreamingAsset || v.IsFolder) continue;

                if (!v.assetPath.StartsWith("Assets/")) continue; // ignore built-in / packages assets
                if (v.forcedIncludedInBuild) continue; // ignore assets that are forced to be included in build
                if (v.assetName == "LICENSE") continue; // ignore license files

                // --- Ignore assets in ignored folders or exact ignored paths ---
                bool isIgnored = vietlabs.fr2.FR2_Setting.IgnoreAsset.Any(ignore =>
                    v.assetPath.Equals(ignore, StringComparison.OrdinalIgnoreCase) ||
                    (v.assetPath.StartsWith(ignore + "/", StringComparison.OrdinalIgnoreCase))
                );
                if (isIgnored) continue;

                // --- Ignore assets with unknown or no extension ---
                string ext = System.IO.Path.GetExtension(v.assetPath);
                Type assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(v.assetPath);
                if (string.IsNullOrEmpty(ext) || assetType == typeof(DefaultAsset))
                {
                    continue;
                }

                if (SPECIAL_USE_ASSETS.Contains(v.assetPath)) continue; // ignore assets with special use (can not remove)
                if (SPECIAL_EXTENSIONS.Contains(v.extension)) continue;

                if (v.type == FR2_Asset.AssetType.DLL) continue;
                if (v.type == FR2_Asset.AssetType.SCRIPT) continue;
                if (v.type == FR2_Asset.AssetType.UNKNOWN) continue;
                if (addressable.Contains(v.guid)) continue;

                // special handler for .spriteatlas
                if (v.extension == ".spriteatlas")
                {
                    var isInUsed = false;
                    List<string> allSprites = v.UseGUIDs.Keys.ToList();
                    foreach (string spriteGUID in allSprites)
                    {
                        FR2_Asset asset = Api.Get(spriteGUID);
                        if (asset.UsedByMap.Count <= 1) continue; // only use by this atlas

                        isInUsed = true;
                        break; // this one is used by other assets
                    }

                    if (isInUsed) continue;
                }

                if (v.IsExcluded)
                {
                    // Debug.Log($"Excluded: {v.assetPath}");
                    continue;
                }

                if (!string.IsNullOrEmpty(v.AtlasName)) continue;
                if (!string.IsNullOrEmpty(v.AssetBundleName)) continue;
                if (!string.IsNullOrEmpty(v.AddressableName)) continue;

                if (v.UsedByMap.Count == 0) //&& !FR2_Asset.IGNORE_UNUSED_GUIDS.Contains(v.guid)
                {
                    result.Add(v);
                    unusedAssets.Add(v.guid);
                }
            }
            
            // If not recursive, return the level 1 results
            if (!recursive)
            {
                result.Sort((item1, item2) => item1.extension == item2.extension
                    ? string.Compare(item1.assetPath, item2.assetPath, StringComparison.Ordinal)
                    : string.Compare(item1.extension, item2.extension, StringComparison.Ordinal));
                    
                return result;
            }
            
            // Recursive scan for higher level unused assets
            bool foundNewUnused = true;
            while (foundNewUnused)
            {
                foundNewUnused = false;
                var newUnusedAssets = new HashSet<string>();
                
                foreach (KeyValuePair<string, FR2_Asset> item in AssetMap)
                {
                    FR2_Asset v = item.Value;
                    
                    // Skip if already in result or doesn't meet basic criteria
                    if (unusedAssets.Contains(v.guid)) continue;
                    if (v.IsMissing || v.inEditor || v.IsScript || v.inResources || v.inPlugins || v.inStreamingAsset || v.IsFolder) continue;
                    if (!v.assetPath.StartsWith("Assets/")) continue;
                    if (v.forcedIncludedInBuild) continue;
                    if (v.assetName == "LICENSE") continue;
                    // --- Ignore assets in ignored folders or exact ignored paths ---
                    bool isIgnored = vietlabs.fr2.FR2_Setting.IgnoreAsset.Any(ignore =>
                        v.assetPath.Equals(ignore, StringComparison.OrdinalIgnoreCase) ||
                        (v.assetPath.StartsWith(ignore + "/", StringComparison.OrdinalIgnoreCase))
                    );
                    if (isIgnored) continue;
                    // --- Ignore assets with unknown or no extension ---
                    string ext = System.IO.Path.GetExtension(v.assetPath);
                    Type assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(v.assetPath);
                    if (string.IsNullOrEmpty(ext) || assetType == typeof(DefaultAsset))
                    {
                        continue;
                    }
                    if (SPECIAL_USE_ASSETS.Contains(v.assetPath)) continue;
                    if (SPECIAL_EXTENSIONS.Contains(v.extension)) continue;
                    if (v.type == FR2_Asset.AssetType.DLL) continue;
                    if (v.type == FR2_Asset.AssetType.SCRIPT) continue;
                    if (v.type == FR2_Asset.AssetType.UNKNOWN) continue;
                    if (addressable.Contains(v.guid)) continue;
                    if (v.IsExcluded) continue;
                    if (!string.IsNullOrEmpty(v.AtlasName)) continue;
                    if (!string.IsNullOrEmpty(v.AssetBundleName)) continue;
                    if (!string.IsNullOrEmpty(v.AddressableName)) continue;
                    // Check if this asset is only used by already identified unused assets
                    if (v.UsedByMap.Count > 0)
                    {
                        bool onlyUsedByUnusedAssets = true;
                        foreach (var usedBy in v.UsedByMap)
                        {
                            if (!unusedAssets.Contains(usedBy.Key))
                            {
                                onlyUsedByUnusedAssets = false;
                                break;
                            }
                        }
                        
                        if (onlyUsedByUnusedAssets)
                        {
                            result.Add(v);
                            newUnusedAssets.Add(v.guid);
                            foundNewUnused = true;
                        }
                    }
                }
                
                // Add newly found unused assets to the master list
                unusedAssets.UnionWith(newUnusedAssets);
            }

            result.Sort((item1, item2) => item1.extension == item2.extension
                ? string.Compare(item1.assetPath, item2.assetPath, StringComparison.Ordinal)
                : string.Compare(item1.extension, item2.extension, StringComparison.Ordinal));

            return result;
        }
    }
} 