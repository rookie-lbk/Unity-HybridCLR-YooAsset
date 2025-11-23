using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AssetState = vietlabs.fr2.FR2_Asset.AssetState;

namespace vietlabs.fr2
{
    internal partial class FR2_Cache
    {
        internal void ReadFromCache()
        {
            if (FR2_SettingExt.disable)
            {
                FR2_LOG.LogWarning("Something wrong??? FR2 is disabled!");
            }

            if (AssetList == null) AssetList = new List<FR2_Asset>();

            FR2_Unity.Clear(ref queueLoadContent);
            FR2_Unity.Clear(ref AssetMap);

            // Create a new filtered list for critical assets only
            var filteredAssetList = new List<FR2_Asset>();

            for (var i = 0; i < AssetList.Count; i++)
            {
                FR2_Asset item = AssetList[i];
                item.state = AssetState.CACHE;

                string path = AssetDatabase.GUIDToAssetPath(item.guid);
                if (string.IsNullOrEmpty(path))
                {
                    item.type = FR2_Asset.AssetType.UNKNOWN; // to make sure if GUIDs being reused for a different kind of asset
                    item.state = AssetState.MISSING;
                    AssetMap.Add(item.guid, item);
                    
                    // Only keep critical assets in AssetList
                    if (item.IsCriticalAsset())
                    {
                        filteredAssetList.Add(item);
                    }
                    continue;
                }

                if (AssetMap.ContainsKey(item.guid))
                {
					FR2_LOG.LogWarning("Something wrong, cache found twice <" + item.guid + ">");
                    continue;
                }

                AssetMap.Add(item.guid, item);
                
                // Only keep critical assets in AssetList
                if (item.IsCriticalAsset())
                {
                    filteredAssetList.Add(item);
                }
            }
            
            // Replace AssetList with filtered list containing only critical assets
            AssetList = filteredAssetList;
        }

        internal void ClearCacheCompletely()
        {
            // FR2_LOG.Log("=== ClearCacheCompletely START ===");
            // FR2_LOG.Log($"Before Clear - AssetList: {AssetList?.Count ?? 0}, AssetMap: {AssetMap?.Count ?? 0}, queueLoadContent: {queueLoadContent?.Count ?? 0}");
            
            // Clear all cache data structures
            if (AssetList != null) AssetList.Clear();
            else AssetList = new List<FR2_Asset>();
            
            if (AssetMap != null) AssetMap.Clear();
            else AssetMap = new Dictionary<string, FR2_Asset>();
            
            if (queueLoadContent != null) queueLoadContent.Clear();
            else queueLoadContent = new List<FR2_Asset>();
            
            // Reset state
            ready = false;
            workCount = 0;
            cacheStamp = 0;
            HasChanged = false;
            currentState = ProcessingState.Idle;
            
            System.GC.Collect();
        }

        internal void ReadFromProject(bool force)
        {
            if (AssetMap == null || AssetMap.Count == 0) ReadFromCache();
            foreach (string b in FR2_Asset.BUILT_IN_ASSETS)
            {
                if (AssetMap.ContainsKey(b)) continue;
                var asset = new FR2_Asset(b);
                AssetMap.Add(b, asset);
                
                // Only add built-in assets to AssetList if they are critical
                if (asset.IsCriticalAsset())
                {
                    AssetList.Add(asset);
                }
            }

            string[] paths = AssetDatabase.GetAllAssetPaths();
            cacheStamp++;
            workCount = 0;
            if (queueLoadContent != null) queueLoadContent.Clear();

            // Check for new assets
            int validPaths = 0;
            int newAssets = 0;
            int existingAssets = 0;
            foreach (string p in paths)
            {
                bool isValid = FR2_Unity.StringStartsWith(p, "Assets/", "Packages/", "Library/", "ProjectSettings/");
                if (!isValid)
                {
                    continue; // Skip invalid paths silently to avoid log spam
                }
                
                validPaths++;
                string guid = AssetDatabase.AssetPathToGUID(p);
                if (!FR2_Asset.IsValidGUID(guid)) 
                {
                    continue;
                }

                if (!AssetMap.TryGetValue(guid, out FR2_Asset asset))
                {
                    newAssets++;
                    AddAsset(guid, force);
                } else
                {
                    existingAssets++;
                    asset.refreshStamp = cacheStamp; // mark this asset so it won't be deleted
                    if (!asset.IsCriticalAsset()) continue; // not something we can handle
                    if (!asset.isDirty && !force) continue;
                    if (force) asset.MarkAsDirty(true, true);
                    if (!asset.IsExcluded && (force || _cacheJustCreated || FR2_SettingExt.isAutoRefreshEnabled))
                    {
                        workCount++;
                        queueLoadContent.Add(asset);    
                    }
                }
            }

            // Check for deleted assets
            for (int i = AssetList.Count - 1; i >= 0; i--)
            {
                if (AssetList[i].refreshStamp != cacheStamp) RemoveAsset(AssetList[i]);
            }
        }

        internal void RefreshAsset(string guid, bool force)
        {
            if (!AssetMap.TryGetValue(guid, out FR2_Asset asset)) return;
            RefreshAsset(asset, force);
        }

        internal void RefreshSelection()
        {
            string[] list = FR2_Unity.Selection_AssetGUIDs;
            for (var i = 0; i < list.Length; i++)
            {
                RefreshAsset(list[i], true);
            }

            Check4Work();
        }

        internal void RefreshAsset(FR2_Asset asset, bool force)
        {
            asset.MarkAsDirty(true, force);
            
            // If we're currently processing and this asset isn't already in the queue, add it
            if (currentState != ProcessingState.Idle && !queueLoadContent.Contains(asset))
            {
                workCount++;
                queueLoadContent.Add(asset);
            }
            
            DelayCheck4Changes();
        }

        internal void AddAsset(string guid, bool force = false)
        {
            if (AssetMap.ContainsKey(guid))
            {
                FR2_LOG.LogWarning("guid already exist <" + guid + ">");
                return;
            }

            var asset = new FR2_Asset(guid);
            asset.LoadPathInfo();
            asset.refreshStamp = cacheStamp;
            AssetMap.Add(guid, asset);

            // Do not load content for FR2_Cache asset
            if (guid == CacheGUID) return;

            if (!asset.IsCriticalAsset()) return;

            // Critical assets (even if ignored) should be added to AssetList
            AssetList.Add(asset);
            
            // CRITICAL FIX: Always queue new assets for content loading when force=true
            bool shouldQueue = !asset.IsExcluded && (force || _cacheJustCreated || FR2_SettingExt.isAutoRefreshEnabled || currentState != ProcessingState.Idle);
                    // FR2_LOG.Log($"AddAsset: {asset.assetPath} - shouldQueue: {shouldQueue} (IsExcluded: {asset.IsExcluded}, force: {force}, _cacheJustCreated: {_cacheJustCreated}, autoRefresh: {FR2_SettingExt.isAutoRefreshEnabled}, currentState: {currentState})");
            
            if (shouldQueue)
            {
                workCount++;
                queueLoadContent.Add(asset);
                        // FR2_LOG.Log($"QUEUED new asset for content loading: {asset.assetPath}");
            }
            else
            {
                // When content loading is skipped, mark as ready but dirty for future scans
                asset.MarkAsDirty(true, false);
                        // FR2_LOG.Log($"SKIPPED new asset: {asset.assetPath} - marked as dirty for future scan");
            }
        }

        internal void RemoveAsset(string guid)
        {
            if (!AssetMap.ContainsKey(guid)) return;

            RemoveAsset(AssetMap[guid]);
        }

        internal void RemoveAsset(FR2_Asset asset)
        {
            AssetList.Remove(asset);

            // Deleted Asset : still in the map but not in the AssetList
            asset.state = AssetState.MISSING;
        }
    }
} 