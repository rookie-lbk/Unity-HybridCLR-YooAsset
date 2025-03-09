using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;
using Cysharp.Threading.Tasks;

public class FsmCreateDownloader : IStateNode
{
    private StateMachine _machine;

    async UniTask IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }
    async UniTask IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStatesChange.SendEventMessage("创建资源下载器！");
        CreateDownloader();
    }
    async UniTask IStateNode.OnUpdate()
    {
    }
    async UniTask IStateNode.OnExit()
    {
    }

    void CreateDownloader()
    {
        var package = YooAssets.GetPackage(PublicData.PackageName);
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
        PatchManager.Instance.Downloader = downloader;

        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("Not found any download files !");
            _machine.ChangeState<FsmDownloadOver>();
        }
        else
        {
            // 发现新更新文件后，挂起流程系统
            // 注意：开发者需要在下载前检测磁盘空间不足
            int totalDownloadCount = downloader.TotalDownloadCount;
            long totalDownloadBytes = downloader.TotalDownloadBytes;
            PatchEventDefine.FoundUpdateFiles.SendEventMessage(totalDownloadCount, totalDownloadBytes);
        }
    }
}