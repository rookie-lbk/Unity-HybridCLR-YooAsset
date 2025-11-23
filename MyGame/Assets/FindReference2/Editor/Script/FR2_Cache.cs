//#define FR2_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using AssetState = vietlabs.fr2.FR2_Asset.AssetState;

namespace vietlabs.fr2
{
    internal partial class FR2_Cache : ScriptableObject
    {

        [SerializeField] private bool _autoRefresh;
        [SerializeField] private string _curCacheVersion;

        [SerializeField] public List<FR2_Asset> AssetList;
        [SerializeField] internal FR2_Setting setting = new FR2_Setting();

        // ----------------------------------- INSTANCE -------------------------------------

        [SerializeField] public int timeStamp;
        [NonSerialized] internal Dictionary<string, FR2_Asset> AssetMap;

        // Track the current asset being processed
        [NonSerialized] internal string currentAssetName;

        private int frameSkipped;

        internal int GC_CountDown = 5;
        [NonSerialized] internal List<FR2_Asset> queueLoadContent;

        internal bool ready;
        [NonSerialized] internal int workCount;
        [NonSerialized] internal ProcessingState currentState = ProcessingState.Idle;

        internal static string CacheGUID
        {
            get
            {
                if (!string.IsNullOrEmpty(_cacheGUID)) return _cacheGUID;

                if (_cache != null)
                {
                    _cachePath = AssetDatabase.GetAssetPath(_cache);
                    _cacheGUID = AssetDatabase.AssetPathToGUID(_cachePath);
                    return _cacheGUID;
                }

                return null;
            }
        }

        internal static string CachePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_cachePath)) return _cachePath;

                if (_cache != null)
                {
                    _cachePath = AssetDatabase.GetAssetPath(_cache);
                    return _cachePath;
                }

                return null;
            }
        }

        [SerializeField] private bool _hasChanged;
        public bool HasChanged 
        { 
            get => _hasChanged; 
            private set => _hasChanged = value; 
        }
        
        internal static bool hasChanges => Api != null && Api.workCount > 0;

        public static void Reload()
        {
            DelayCheck4Changes();
        }
        
        internal static FR2_Cache Api
        {
            get
            {
                if (_cache != null) return _cache;
                if (!_triedToLoadCache) TryLoadCache();
                return _cache;
            }
        }

        internal static bool isReady
        {
            get
            {
                if (FR2_SettingExt.disable) return false;
                if (!_triedToLoadCache) TryLoadCache();
                return (_cache != null) && _cache.ready;
            }
        }

        internal static bool hasCache
        {
            get
            {
                if (!_triedToLoadCache) TryLoadCache();

                return _cache != null;
            }
        }

        internal float progress
        {
            get
            {
                int n = workCount - queueLoadContent.Count;
                return workCount == 0 ? 1 : n / (float)workCount;
            }
        }
    }

    
    internal enum ProcessingState
    {
        Idle,           // Not processing anything
        ReadingContent, // Currently reading asset content
        BuildingUsedBy  // Currently building usedBy relationships
    }
    internal static class FR2_LOG
    {
        public static void Log(object message)
        {
#if FR2_DEBUG || FR2_DEV
            UnityEngine.Debug.Log(message);
#endif
        }

        public static void Log(object message, UnityEngine.Object context)
        {
#if FR2_DEBUG || FR2_DEV
            UnityEngine.Debug.Log(message, context);
#endif
        }

        public static void LogWarning(object message)
        {
#if FR2_DEBUG || FR2_DEV
            UnityEngine.Debug.LogWarning(message);
#endif
        }

        public static void LogWarning(object message, UnityEngine.Object context)
        {
#if FR2_DEBUG || FR2_DEV
            UnityEngine.Debug.LogWarning(message, context);
#endif
        }

        public static void LogError(object message)
        {
#if FR2_DEBUG || FR2_DEV
            UnityEngine.Debug.LogError(message);
#endif
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
#if FR2_DEBUG || FR2_DEV
            UnityEngine.Debug.LogError(message, context);
#endif
        }
    }

    [CustomEditor(typeof(FR2_Cache))]
    internal class FR2_CacheEditor : Editor
    {
        private static string inspectGUID;
        private static int index;

        public override void OnInspectorGUI()
        {
            var c = (FR2_Cache)target;

            GUILayout.Label("Total : " + c.AssetList.Count);

            // FR2_Cache.DrawPriorityGUI();

            UnityObject s = Selection.activeObject;
            if (s == null) return;

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s));

            if (inspectGUID != guid)
            {
                inspectGUID = guid;
                index = c.AssetList.FindIndex(item => item.guid == guid);
            }

            if (index != -1)
            {
                if (index >= c.AssetList.Count) index = 0;

                serializedObject.Update();
                SerializedProperty prop = serializedObject.FindProperty("AssetList").GetArrayElementAtIndex(index);
                prop.isExpanded = true;
                EditorGUILayout.PropertyField(prop, true);
            }
        }
    }
}
