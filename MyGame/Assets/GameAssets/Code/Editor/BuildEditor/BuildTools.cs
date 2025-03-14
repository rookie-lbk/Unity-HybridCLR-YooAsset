using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

public class BuildTools
{
    public static string ProjectPath = Application.dataPath.Replace("Assets", "");
    public static string DllPath = string.Format("{0}/HybridCLRData/HotUpdateDlls/Android/", ProjectPath);
    public static string PackageExportPath = string.Format("{0}/BuildPackage/", ProjectPath);
    public static string HotUpdateAssetsPath = string.Format("{0}/ResourcesAB/", Application.dataPath);
    public static string HotUpdateDllPath = string.Format("{0}/ResourcesAB/code/dlls/", Application.dataPath);

    public static string buildoutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
    public static string streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();

    [MenuItem("Test/Test")]
    public static void Test()
    {
        BuiltinBuildParameters buildParametersExt = new()
        {
            BuildOutputRoot = buildoutputRoot,
            BuildinFileRoot = streamingAssetsRoot,
            BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString(),
            BuildBundleType = (int)EBuildBundleType.AssetBundle,
            BuildTarget = BuildTarget.Android,
            PackageName = "DefaultPackage",
            PackageVersion = "1.0.0",
            PackageNote = "DefaultPackage",
            ClearBuildCacheFiles = false,
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
    }


    public class FileStreamEncryption : IEncryptionServices
    {
        public EncryptResult Encrypt(EncryptFileInfo fileInfo)
        {
            throw new System.NotImplementedException();
        }
    }
}
