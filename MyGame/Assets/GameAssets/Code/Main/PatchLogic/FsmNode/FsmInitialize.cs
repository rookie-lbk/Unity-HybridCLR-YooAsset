using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using UniFramework.Singleton;
using YooAsset;
using Cysharp.Threading.Tasks;

/// <summary>
/// 初始化资源包
/// </summary>
internal class FsmInitialize : IStateNode
{
    private StateMachine _machine;

    async UniTask IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }
    async UniTask IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStatesChange.SendEventMessage("初始化资源包！");
        await InitPackage();
    }
    async UniTask IStateNode.OnUpdate()
    {
    }
    async UniTask IStateNode.OnExit()
    {
    }

    private async UniTask InitPackage()
    {

        var playMode = PatchManager.Instance.PlayMode;

        // 创建默认的资源包
        string packageName = PublicData.PackageName;
        var package = YooAssets.TryGetPackage(packageName);
        if (package == null)
        {
            package = YooAssets.CreatePackage(packageName);
            YooAssets.SetDefaultPackage(package);
        }

        // 编辑器下的模拟模式
        InitializationOperation initializationOperation = null;
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var createParameters = new EditorSimulateModeParameters();
            var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
            var packageRoot = buildResult.PackageRootDirectory;
            createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            initializationOperation = package.InitializeAsync(createParameters);
        }

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var createParameters = new OfflinePlayModeParameters();
            createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            initializationOperation = package.InitializeAsync(createParameters);
        }

        // 联机运行模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var createParameters = new HostPlayModeParameters();
            createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            initializationOperation = package.InitializeAsync(createParameters);
        }

        await initializationOperation.ToUniTask();
        if (package.InitializeStatus == EOperationStatus.Succeed)
        {
            // 离线模式需要更新本地清单
            if (PatchManager.Instance.PlayMode == EPlayMode.OfflinePlayMode)
            {
                Debug.Log("离线模式：初始化成功，更新本地资源清单");
                await UpdateOfflineManifest(package);
            }
            else
            {
                // 在线模式继续更新版本
                _machine.ChangeState<FsmUpdateVersion>();
            }
        }
        else
        {
            Debug.LogWarning($"{initializationOperation.Error}");
            PatchEventDefine.InitializeFailed.SendEventMessage();
        }
    }

    /// <summary>
    /// 获取资源服务器地址
    /// </summary>
    private string GetHostServerURL()
    {
        string hostServerIP = HttpHelper.HttpHost;
        string appVersion = PublicData.Version;

#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            return $"{hostServerIP}Android/CDN/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            return $"{hostServerIP}IOS/CDN/{appVersion}";
        else
            return $"{hostServerIP}Win/CDN/{appVersion}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{hostServerIP}Android/CDN/{appVersion}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{hostServerIP}IOS/CDN/{appVersion}";
        else
            return $"{hostServerIP}Win/CDN/{appVersion}";
#endif
    }

    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }

    /// <summary>
    /// 离线模式更新本地资源清单
    /// </summary>
    private async UniTask UpdateOfflineManifest(ResourcePackage package)
    {
        // 离线模式使用内置的资源版本
        var operation = package.UpdatePackageManifestAsync(PublicData.Version);
        await operation.ToUniTask();

        if (operation.Status == EOperationStatus.Succeed)
        {
            Debug.Log("离线模式：资源清单更新成功，开始加载热更新 DLL");
            _machine.ChangeState<FsmLoadHotUpdateDll>();
        }
        else
        {
            Debug.LogError($"离线模式：资源清单更新失败 - {operation.Error}");
            PatchEventDefine.InitializeFailed.SendEventMessage();
        }
    }

    private class WebDecryption : IWebDecryptionServices
    {
        public const byte KEY = 64;

        public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
        {
            byte[] copyData = new byte[fileInfo.FileData.Length];
            Buffer.BlockCopy(fileInfo.FileData, 0, copyData, 0, fileInfo.FileData.Length);

            for (int i = 0; i < copyData.Length; i++)
            {
                copyData[i] ^= KEY;
            }

            WebDecryptResult decryptResult = new WebDecryptResult();
            decryptResult.Result = AssetBundle.LoadFromMemory(copyData);
            return decryptResult;
        }
    }
}
