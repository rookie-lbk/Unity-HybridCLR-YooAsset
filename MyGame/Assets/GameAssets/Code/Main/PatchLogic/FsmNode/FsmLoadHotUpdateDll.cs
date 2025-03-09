using Cysharp.Threading.Tasks;
using HybridCLR;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UniFramework.Machine;
using UniFramework.Singleton;
using UnityEngine;
using YooAsset;
using Newtonsoft.Json;
public class FsmLoadHotUpdateDll : IStateNode
{
    private StateMachine _machine;

    async UniTask IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }

    async UniTask IStateNode.OnEnter()
    {
        // ������Ϸ������
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
            Debug.Log("����ȡʧ��");
        }
        var handle = package.LoadRawFileAsync("AOTDLLList");
        await handle.ToUniTask();
        var data = handle.GetRawFileText();
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
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllData, mode);
            // Debug.Log($"LoadMetadataForAOTAssembly:{name}. mode:{mode} ret:{err}");
        }
        Debug.Log("LoadMetadataForAOTAssemblies------End");
    }

    async UniTask LoadHotUpdateAssemblies()
    {
        var package = YooAssets.TryGetPackage(PublicData.PackageName);
        if (package == null)
        {
            Debug.Log("����ȡʧ��");
        }
        var handle = package.LoadRawFileAsync("HotUpdateDLLList");
        await handle.ToUniTask();
        var data = handle.GetRawFileText();
        var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
        foreach (var DllName in dllNames)
        {
            var dataHandle = package.LoadRawFileAsync(DllName);
            await dataHandle.ToUniTask();
            if (dataHandle.Status != EOperationStatus.Succeed)
            {
                Debug.Log("��Դ����ʧ��" + DllName);
                return;
            }
            var dllData = dataHandle.GetRawFileData();
            if (dllData == null)
            {
                Debug.Log("��ȡDll����ʧ��");
                return;
            }
            Assembly assembly = Assembly.Load(dllData);
            HotUpdateManager.Instance.HotUpdateAssemblies.Add(DllName, assembly);
            Debug.Log(assembly.GetTypes());
            Debug.Log($"�����ȸ���Dll:{DllName}");
        }
    }
}
