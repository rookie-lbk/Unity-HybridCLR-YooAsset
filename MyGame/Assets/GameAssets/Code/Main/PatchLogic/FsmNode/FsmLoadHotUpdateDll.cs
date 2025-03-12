using Cysharp.Threading.Tasks;
using HybridCLR;
using System.Collections.Generic;
using System.Reflection;
using UniFramework.Machine;
using UniFramework.Singleton;
using UnityEngine;
using YooAsset;
using Newtonsoft.Json;
using System;
public class FsmLoadHotUpdateDll : IStateNode
{
    private StateMachine _machine;

    async UniTask IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }

    async UniTask IStateNode.OnEnter()
    {
        // 创建游戏管理器
        UniSingleton.CreateSingleton<HotUpdateManager>();
        await LoadMetadataForAOTAssemblies();
        await LoadHotUpdateAssemblies();
        _machine.ChangeState<FsmClearCache>();
    }

    async UniTask IStateNode.OnExit()
    {

    }

    async UniTask IStateNode.OnUpdate()
    {
        ;
    }

    public async UniTask LoadMetadataForAOTAssemblies()
    {
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        var package = YooAssets.TryGetPackage(PublicData.PackageName);
        if (package == null)
        {
            Debug.LogError("包获取失败");
        }
        var handle = package.LoadAssetAsync<TextAsset>("AOTDll/AOTDLLList.txt");
        await handle.ToUniTask();
        var data = handle.GetAssetObject<TextAsset>().text;
        var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
        foreach (var name in dllNames)
        {
            Debug.Log("LoadMetadataForAOTAssemblies:" + name);
            var dataHandle = package.LoadAssetAsync<TextAsset>($"AOTDll/{name}.bytes");
            await dataHandle.ToUniTask();
            var dllData = dataHandle.GetAssetObject<TextAsset>().bytes;
            if (dllData == null)
            {
                continue;
            }
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllData, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{name}. mode:{mode} ret:{err}");
        }
    }

    async UniTask LoadHotUpdateAssemblies()
    {
        var package = YooAssets.TryGetPackage(PublicData.PackageName);
        if (package == null)
        {
            Debug.LogError("包获取失败");
        }
        var handle = package.LoadAssetAsync<TextAsset>("HotUpdateDll/HotUpdateDLLList.txt");
        await handle.ToUniTask();
        var data = handle.GetAssetObject<TextAsset>().text;
        var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
        foreach (var DllName in dllNames)
        {
            Debug.Log($"加载热更新Dll:{DllName}");
            var dataHandle = package.LoadAssetAsync<TextAsset>($"HotUpdateDll/{DllName}.bytes");
            await dataHandle.ToUniTask();
            if (dataHandle.Status != EOperationStatus.Succeed)
            {
                Debug.LogError("资源加载失败" + DllName);
                return;
            }
            var dllData = dataHandle.GetAssetObject<TextAsset>().bytes;
            if (dllData == null)
            {
                Debug.LogError("获取Dll数据失败");
                return;
            }
            try
            {
                Assembly assembly = Assembly.Load(dllData);
                HotUpdateManager.Instance.HotUpdateAssemblies.Add(DllName, assembly);
                Debug.Log($"加载热更新Dll:{DllName}成功");
            }
            catch (Exception e)
            {
                Debug.LogError($"DLL加载或执行过程中发生错误: {e.Message}");
                Debug.LogError($"详细错误信息: {e.StackTrace}");
                throw;
            }
        }
    }
}
