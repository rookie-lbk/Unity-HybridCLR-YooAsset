using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using Cysharp.Threading.Tasks;
using YooAsset;

/// <summary>
/// 清理未使用的缓存文件
/// </summary>
internal class FsmClearCache : IStateNode
{
	private StateMachine _machine;

	async UniTask IStateNode.OnCreate(StateMachine machine)
	{
		_machine = machine;
	}
	async UniTask IStateNode.OnEnter()
	{
		PatchEventDefine.PatchStatesChange.SendEventMessage("清理未使用的缓存文件！");
		var package = YooAssets.GetPackage(PublicData.PackageName);
		await package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
		_machine.ChangeState<FsmPatchDone>();
	}
	async UniTask IStateNode.OnUpdate()
	{
	}
	async UniTask IStateNode.OnExit()
	{
	}
}