using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using UniFramework.Event;
using UniFramework.Singleton;
using YooAsset;
using Cysharp.Threading.Tasks;

public class PatchManager : SingletonInstance<PatchManager>, ISingleton
{
	private enum ESteps
	{
		None,
		Update,
		Done,
	}
	/// <summary>
	/// 运行模式
	/// </summary>
	public EPlayMode PlayMode { set; get; }

	/// <summary>
	/// 包裹的版本信息
	/// </summary>
	public string PackageVersion { set; get; }

	/// <summary>
	/// 下载器
	/// </summary>
	public ResourceDownloaderOperation Downloader { set; get; }


	private bool _isRun = false;
	private EventGroup _eventGroup = new EventGroup();
	private StateMachine _machine;
	private ESteps _steps = ESteps.None;

	async UniTask ISingleton.OnCreate(object createParam)
	{
	}
	async UniTask ISingleton.OnDestroy()
	{
		_eventGroup.RemoveAllListener();
	}
	async UniTask ISingleton.OnUpdate()
	{
		if (_steps == ESteps.None || _steps == ESteps.Done)
			return;

		if (_machine != null)
			_machine.Update();
	}

	/// <summary>
	/// 开启流程
	/// </summary>
	public void Run(EPlayMode playMode)
	{
		if (_isRun == false)
		{
			_isRun = true;
			PlayMode = playMode;

			Debug.Log("开启补丁更新流程...");
			_machine = new StateMachine(this);

			// 离线模式：跳过所有网络相关状态
			if (playMode == EPlayMode.OfflinePlayMode)
			{
				Debug.Log("离线模式：跳过网络检查和资源下载");
				_machine.AddNode<FsmInitialize>();
				_machine.AddNode<FsmLoadHotUpdateDll>();
				_machine.AddNode<FsmClearCache>();
				_machine.AddNode<FsmPatchDone>();
				_machine.Run<FsmInitialize>();
			}
			else
			{
				// 在线模式：完整流程
				// 注册监听事件
				_eventGroup.AddListener<UserEventDefine.UserTryInitialize>(OnHandleEventMessage);
				_eventGroup.AddListener<UserEventDefine.UserBeginDownloadWebFiles>(OnHandleEventMessage);
				_eventGroup.AddListener<UserEventDefine.UserTryRequestPackageVersion>(OnHandleEventMessage);
				_eventGroup.AddListener<UserEventDefine.UserTryUpdatePackageManifest>(OnHandleEventMessage);
				_eventGroup.AddListener<UserEventDefine.UserTryDownloadWebFiles>(OnHandleEventMessage);

				_machine.AddNode<FsmPatchPrepare>();
				_machine.AddNode<FsmCheckNetwork>();
				_machine.AddNode<FsmCheckVersion>();
				_machine.AddNode<FsmInitialize>();
				_machine.AddNode<FsmUpdateVersion>();
				_machine.AddNode<FsmUpdateManifest>();
				_machine.AddNode<FsmCreateDownloader>();
				_machine.AddNode<FsmDownloadFiles>();
				_machine.AddNode<FsmDownloadOver>();
				_machine.AddNode<FsmLoadHotUpdateDll>();
				_machine.AddNode<FsmClearCache>();
				_machine.AddNode<FsmPatchDone>();
				_machine.Run<FsmPatchPrepare>();
			}
		}
		else
		{
			Debug.LogWarning("补丁更新已经正在进行中!");
		}
		_steps = ESteps.Update;
	}

	/// <summary>
	/// 接收事件
	/// </summary>
	private void OnHandleEventMessage(IEventMessage message)
	{
		if (message is UserEventDefine.UserTryInitialize)
		{
			_machine.ChangeState<FsmInitialize>();
		}
		else if (message is UserEventDefine.UserBeginDownloadWebFiles)
		{
			_machine.ChangeState<FsmDownloadFiles>();
		}
		else if (message is UserEventDefine.UserTryRequestPackageVersion)
		{
			_machine.ChangeState<FsmUpdateVersion>();
		}
		else if (message is UserEventDefine.UserTryUpdatePackageManifest)
		{
			_machine.ChangeState<FsmUpdateManifest>();
		}
		else if (message is UserEventDefine.UserTryDownloadWebFiles)
		{
			_machine.ChangeState<FsmCreateDownloader>();
		}
		else
		{
			throw new System.NotImplementedException($"{message.GetType()}");
		}
	}

	public void SetFinish()
	{
		_steps = ESteps.Done;
		_eventGroup.RemoveAllListener();

		Debug.Log("开始游戏！");
		// 切换到主页面场景
		SceneEventDefine.ChangeToHomeScene.SendEventMessage();
	}
}
