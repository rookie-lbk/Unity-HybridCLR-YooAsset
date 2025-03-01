using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HybridCLR;
using HybridCLR.Editor.Settings;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    // /// <summary>
    // /// AOT
    // /// </summary>
    // private static List<string> AOTMetaAssemblyFiles { get; } = new List<string>()
    // {
    //     "mscorlib",
    //     "System",
    //     "System.Core",
    // };

    // /// <summary>
    // /// HotUpdateDll
    // /// </summary>
    // private static List<string> HotUpdateDll { get; } = new List<string>(){
    //     "Game",
    // };

    void Start()
    {
        //TODO 获取版本信息
        //TODO 下载更新资源
        //TODO 下载AOT的元数据
        //TODO 下载用于更新的DLL资源

        LoadMetadataForAOTAssemblies();
        LoadHotUpdateDll();

        //TODO 开始游戏
    }

    /// <summary>
    /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    /// </summary>
    private void LoadMetadataForAOTAssemblies()
    {
        /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
        /// 
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in HybridCLRSettings.Instance.patchAOTAssemblies)
        {
            byte[] dllBytes = ReadDllBytes(aotDllName);
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
    }

    //TODO 读取Dll资源，可以考虑从用YooAsset实现
    private byte[] ReadDllBytes(string dllName)
    {
        string fileName = $"{dllName}.dll.bytes";
        return null;
    }

    /// <summary>
    /// 加载dll
    /// </summary>
    private void LoadHotUpdateDll()
    {
        foreach (var hotUpdateDll in HybridCLRSettings.Instance.hotUpdateAssemblies)
        {
            byte[] dllBytes = ReadDllBytes(hotUpdateDll);
            Assembly.Load(dllBytes);
        }
    }
}
