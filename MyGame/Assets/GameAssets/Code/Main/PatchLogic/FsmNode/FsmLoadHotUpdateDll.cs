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
            Debug.Log("包获取失败");
        }
#if UNITY_EDITOR
        var handle = package.LoadRawFileAsync("AOTDLLList");
        await handle.ToUniTask();
        var data = handle.GetRawFileText();
#else
        var handle = package.LoadAssetAsync<TextAsset>("AOTDLLList");
        await handle.ToUniTask();
        var data = handle.GetAssetObject<TextAsset>().text;
#endif
        Debug.Log(data);
        var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
        Debug.Log("LoadMetadataForAOTAssemblies------Start");
        foreach (var name in dllNames)
        {
            Debug.Log("LoadMetadataForAOTAssemblies:" + name);
            var dataHandle = package.LoadRawFileAsync(name);
            await dataHandle.ToUniTask();
            var dllData = dataHandle.GetRawFileData();
            if (data == null)
            {
                continue;
            }
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllData, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{name}. mode:{mode} ret:{err}");
        }
        Debug.Log("LoadMetadataForAOTAssemblies------End");
    }

    async UniTask LoadHotUpdateAssemblies()
    {
        var package = YooAssets.TryGetPackage(PublicData.PackageName);
        if (package == null)
        {
            Debug.Log("包获取失败");
        }
#if UNITY_EDITOR
        var handle = package.LoadRawFileAsync("HotUpdateDLLList");
        await handle.ToUniTask();
        var data = handle.GetRawFileText();
#else
        var handle = package.LoadAssetAsync<TextAsset>("HotUpdateDLLList");
        await handle.ToUniTask();
        var data = handle.GetAssetObject<TextAsset>().text;
#endif
        var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
        foreach (var DllName in dllNames)
        {
            Debug.Log($"加载热更新Dll:{DllName}");

            // string url = Application.streamingAssetsPath + "/" + DllName + ".bytes";
            // UnityWebRequest www = UnityWebRequest.Get(url);
            // await www.SendWebRequest();
            // if (www.result != UnityWebRequest.Result.Success)
            // {
            //     Debug.LogError("加载热更新Dll失败" + DllName);
            //     continue;
            // }

            // byte[] dllData = www.downloadHandler.data;

            var dataHandle = package.LoadRawFileAsync(DllName);
            await dataHandle.ToUniTask();
            if (dataHandle.Status != EOperationStatus.Succeed)
            {
                Debug.Log("资源加载失败" + DllName);
                return;
            }
            var dllData = dataHandle.GetRawFileData();
            if (dllData == null)
            {
                Debug.Log("获取Dll数据失败");
                return;
            }
            try
            {
                Debug.Log("LoadHotUpdateAssemblies:---1------------");
                Assembly assembly = Assembly.Load(dllData);
                Debug.Log($"DLL加载成功: {assembly.FullName}");
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
