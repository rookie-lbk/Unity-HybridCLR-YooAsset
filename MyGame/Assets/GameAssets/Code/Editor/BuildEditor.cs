using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

public static class BuildEditor
{
    public static string HybridCLRBuildCacheDir => Application.dataPath + "/HybridCLRBuildCache";
    public static string AssetBundleOutputDir => $"{HybridCLRBuildCacheDir}/AssetBundleOutput";
    public static string AssetBundleSourceDataTempDir => $"{HybridCLRBuildCacheDir}/AssetBundleSourceData";

    public static string GetAssetBundleOutputDirByTarget(BuildTarget target)
    {
        return $"{AssetBundleOutputDir}/{target}";
    }

    public static string GetAssetBundleTempDirByTarget(BuildTarget target)
    {
        return $"{AssetBundleSourceDataTempDir}/{target}";
    }


    //TODO 打包资源
    public static void BuildAssetBundleByTarget(BuildTarget target)
    {

    }

    [MenuItem("Tools/BuildAssetsAndCopyToStreamingAssets")]
    public static void BuildAndCopyABAOTHotUpdateDlls()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        BuildAssetBundleByTarget(target);
        // 设置AOT和HotUpdate的dll
        SetAOTHotUpdateDlls();
        // 编译dll
        CompileDllCommand.CompileDll(target);
        // 复制dll
        CopyABAOTHotUpdateDlls(target);
    }

    public static void SetAOTHotUpdateDlls()
    {
        HybridCLRSettings.Instance.patchAOTAssemblies = new string[]{
                "mscorlib",
                "System",
                "System.Core",
                "Main",
            };
        HybridCLRSettings.Instance.hotUpdateAssemblies = new string[]{
                "Game",
            };
    }

    public static void CopyABAOTHotUpdateDlls(BuildTarget target)
    {
        CopyAssetBundlesToStreamingAssets(target);
        CopyAOTAssembliesToStreamingAssets();
        CopyHotUpdateAssembliesToStreamingAssets();
    }

    //TODO 后面需要考虑将文件拷贝至指定目录，并上传CDN
    public static void CopyAssetBundlesToStreamingAssets(BuildTarget target)
    {

    }

    //TODO 目前是拷贝到StreamingAssets目录了，后面需要改成给YooAsset使用的资源目录
    public static void CopyAOTAssembliesToStreamingAssets()
    {
        //TODO 这里使用的是当前所在的平台，并非上传CDN的平台（如：Android、IOS） 
        var target = EditorUserBuildSettings.activeBuildTarget;
        string aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
        string aotAssembliesDstDir = Application.streamingAssetsPath;

        foreach (var dll in SettingsUtil.AOTAssemblyNames)
        {
            string srcDllPath = $"{aotAssembliesSrcDir}/{dll}.dll";
            if (!File.Exists(srcDllPath))
            {
                Debug.LogError($"ab中添加AOT补充元数据dll:{srcDllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                continue;
            }
            string dllBytesPath = $"{aotAssembliesDstDir}/{dll}.dll.bytes";
            File.Copy(srcDllPath, dllBytesPath, true);
            Debug.Log($"[CopyAOTAssembliesToStreamingAssets] copy AOT dll {srcDllPath} -> {dllBytesPath}");
        }
    }

    //TODO 目前是拷贝到StreamingAssets目录了，后面需要改成给YooAsset使用的资源目录
    public static void CopyHotUpdateAssembliesToStreamingAssets()
    {
        //TODO 这里使用的是当前所在的平台，并非上传CDN的平台（如：Android、IOS） 
        var target = EditorUserBuildSettings.activeBuildTarget;
        string hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        string hotfixAssembliesDstDir = Application.streamingAssetsPath;
        foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
        {
            string dllPath = $"{hotfixDllSrcDir}/{dll}";
            string dllBytesPath = $"{hotfixAssembliesDstDir}/{dll}.bytes";
            File.Copy(dllPath, dllBytesPath, true);
            Debug.Log($"[CopyHotUpdateAssembliesToStreamingAssets] copy hotfix dll {dllPath} -> {dllBytesPath}");
        }
    }
}
