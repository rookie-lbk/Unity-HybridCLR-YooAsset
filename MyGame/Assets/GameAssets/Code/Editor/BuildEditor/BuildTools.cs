using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

public class BuildTools
{
    public static BuildTarget buildTarget = BuildTarget.Android;
    public static string CDNPath = "D:/CDN/MyGame/";
    public static string PackageName = "DefaultPackage";
    public static string[] patchAOTAssemblies = new string[]{
        "mscorlib",
        "System",
        "System.Core",
        "Main",
    };
    public static string[] hotUpdateAssemblies = new string[]{
        "Game",
    };

    public static string ProjectPath = Application.dataPath.Replace("Assets", "");
    public static string BuildAOTDllsPath = $"{ProjectPath}{HybridCLRSettings.Instance.strippedAOTDllOutputRootDir}/{buildTarget}/";
    public static string BuildHotUpdateDllsPath = $"{ProjectPath}{HybridCLRSettings.Instance.hotUpdateDllCompileOutputRootDir}/{buildTarget}/";
    public static string PackageExportPath = string.Format("{0}/BuildPackage/", ProjectPath);
    public static string HotUpdateAssetsPath = string.Format("{0}/GameAssets/Res/", Application.dataPath);
    public static string AOTDllPath = string.Format("{0}/GameAssets/DLLs/AOTDll/", Application.dataPath);
    public static string HotUpdateDllPath = string.Format("{0}/GameAssets/DLLs/HotUpdateDll/", Application.dataPath);

    public static string buildoutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
    public static string streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();

    [MenuItem("BuildTools/BuildDlls")]
    public static void BuildDlls()
    {
        // 执行GenerateAll
        BuildAOTDlls();
        // 将AOT的DLL存放至指定目录，并更新列表信息
        CopyAOTDlls();
        // 将参与更新的DLL存放至指定目录，并更新列表信息
        CopyHotUpdateDlls();
    }

    [MenuItem("BuildTools/BuildApk_Debug")]
    public static void BuildApk_Debug()
    {
        BuildDlls();
        // 设置资源版本，YooAsset打包全量资源
        BuildAssetBundle();
        // 将资源包存放至CDN
        CopyAssetBundleToCDN();
        // 打包APK
        BuildAPK();
    }

    private static void BuildAOTDlls()
    {
        Debug.Log($"====== BuildTools BuildAOTDlls Start ======");
        PrebuildCommand.GenerateAll();
        Debug.Log($"====== BuildTools BuildAOTDlls End ======");
    }

    private static void CopyAOTDlls()
    {
        Debug.Log($"====== BuildTools CopyAOTDlls Start ======");
        List<string> dllNames = new();
        foreach (string dllName in patchAOTAssemblies)
        {
            string dllPath = $"{BuildAOTDllsPath}{dllName}.dll";
            if (!File.Exists(dllPath))
            {
                Debug.LogError($"{dllName}不存在");
                continue;
            }
            dllNames.Add(dllName + ".dll");
            byte[] dllData = File.ReadAllBytes(dllPath);
            string resPath = $"{AOTDllPath}{dllName}.dll.bytes";
            Debug.Log($"resPath:{resPath} dllPath:{dllPath}");
            File.WriteAllBytes(resPath, dllData);
        }
        var json = JsonConvert.SerializeObject(dllNames);
        File.WriteAllText($"{AOTDllPath}AOTDLLList.txt", json);
        AssetDatabase.Refresh();
        Debug.Log($"====== BuildTools CopyAOTDlls End ======");
    }

    private static void CopyHotUpdateDlls()
    {
        Debug.Log($"====== BuildTools CopyHotUpdateDlls Start ======");
        List<string> dllNames = new();
        foreach (string dllName in hotUpdateAssemblies)
        {
            string dllPath = $"{BuildHotUpdateDllsPath}{dllName}.dll";
            if (!File.Exists(dllPath))
            {
                Debug.LogError($"{dllName}不存在");
                continue;
            }
            dllNames.Add(dllName + ".dll");
            byte[] dllData = File.ReadAllBytes(dllPath);
            string resPath = $"{HotUpdateDllPath}{dllName}.dll.bytes";
            Debug.Log($"resPath:{resPath} dllPath:{dllPath}");
            File.WriteAllBytes(resPath, dllData);
        }
        var json = JsonConvert.SerializeObject(dllNames);
        File.WriteAllText($"{HotUpdateDllPath}HotUpdateDLLList.txt", json);
        AssetDatabase.Refresh();
        Debug.Log($"====== BuildTools CopyHotUpdateDlls End ======");
    }

    private static string GetPackageVersion()
    {
        string packageVersion = File.ReadAllText($"{CDNPath}{buildTarget}/VERSION.txt");
        return packageVersion;
    }

    private static void BuildAssetBundle()
    {
        Debug.Log($"====== BuildTools BuildAssetBundle Start ======");
        string packageVersion = GetPackageVersion();
        Debug.Log($"packageVersion:{packageVersion}");
        BuiltinBuildParameters buildParametersExt = new()
        {
            BuildOutputRoot = buildoutputRoot,
            BuildinFileRoot = streamingAssetsRoot,
            BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString(),
            BuildBundleType = (int)EBuildBundleType.AssetBundle,
            BuildTarget = BuildTarget.Android,
            PackageName = PackageName,
            PackageVersion = packageVersion,
            PackageNote = "DefaultPackage",
            ClearBuildCacheFiles = true,
            UseAssetDependencyDB = true,
            EnableSharePackRule = true,
            SingleReferencedPackAlone = true,
            VerifyBuildingResult = true,
            FileNameStyle = EFileNameStyle.HashName,
            BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyByTags,
            BuildinFileCopyParams = string.Empty,
            EncryptionServices = null,
        };

        buildParametersExt.CheckBuildParameters();

        Debug.Log($"PipelineOutputDirectory:{buildParametersExt.GetPipelineOutputDirectory()}");
        Debug.Log($"PackageOutputDirectory:{buildParametersExt.GetPackageOutputDirectory()}");
        Debug.Log($"PackageRootDirectory:{buildParametersExt.GetPackageRootDirectory()}");
        Debug.Log($"BuildinRootDirectory:{buildParametersExt.GetBuildinRootDirectory()}");

        BuiltinBuildPipeline pipeline = new();
        var buildResult = pipeline.Run(buildParametersExt, true);
        if (buildResult.Success)
        {
            Debug.Log("Build Success");
        }
        else
        {
            Debug.LogError("Build Failed");
        }
        Debug.Log($"====== BuildTools BuildAssetBundle End ======");
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        // 创建目标目录
        Directory.CreateDirectory(destinationDir);
        // 复制所有文件
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);
            File.Copy(file, destFile, true);
        }

        // 递归复制所有子目录
        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            string destDir = Path.Combine(destinationDir, dirName);
            CopyDirectory(dir, destDir);
        }
    }

    private static void CopyAssetBundleToCDN()
    {
        Debug.Log($"====== BuildTools CopyAssetBundleToCDN Start ======");
        string fromPath = Path.Combine(buildoutputRoot, buildTarget.ToString(), PackageName, GetPackageVersion());
        string toPath = Path.Combine(CDNPath, buildTarget.ToString(), "CDN", GetPackageVersion());
        Debug.Log($"fromPath:{fromPath}");
        Debug.Log($"toPath:{toPath}");
        CopyDirectory(fromPath, toPath);
        Debug.Log($"====== BuildTools CopyAssetBundleToCDN End ======");
    }

    private static string[] GetBuildScenes()
    {
        List<string> names = new List<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
        {
            if (e == null)
                continue;
            if (e.enabled)
                names.Add(e.path);
        }
        return names.ToArray();
    }

    private static void BuildAPK()
    {
        Debug.Log($"====== BuildTools BuildAPK Start ======");
        string packageVersion = GetPackageVersion();
        string[] scenes = GetBuildScenes();
        string outputPath = $"{PackageExportPath}{buildTarget.ToString()}/{PlayerSettings.productName}_{packageVersion}_{DateTime.Now.ToString("yyyy_M_d_HH_mm_s")}.apk";
        Debug.Log($"outputPath:{outputPath}");
        BuildOptions options = BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging;
        UnityEditor.Build.Reporting.BuildReport result = BuildPipeline.BuildPlayer(scenes, outputPath, buildTarget, options);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("====== BuildTools BuildAPK Failed ====== result:" + result);
        }
        else
        {
            Debug.Log("====== BuildTools BuildAPK Success ======");
        }
        Debug.Log($"====== BuildTools BuildAPK End ======");
    }
}
