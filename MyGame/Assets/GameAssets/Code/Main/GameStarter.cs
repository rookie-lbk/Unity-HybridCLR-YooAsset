using Cysharp.Threading.Tasks;
using UniFramework.Event;
using UniFramework.Singleton;
using UnityEngine;
using YooAsset;

public class GameStarter : MonoBehaviour
{
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
    public int DefaultFrameRate = 60;

    void Awake()
    {
        Application.targetFrameRate = DefaultFrameRate;
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Debug.Log($"资源系统运行模式：{PlayMode}");
#if !UNITY_EDITOR
		if(PlayMode!= EPlayMode.HostPlayMode)
        {
			PlayMode = EPlayMode.HostPlayMode;
			Debug.Log($"检测到真机运行,已切换运行模式至：{PlayMode}");
		}
#endif
    }

    async UniTask Start()
    {
        UniEvent.Initalize();

        UniSingleton.Initialize();

        YooAssets.Initialize();

        YooAssets.SetOperationSystemMaxTimeSlice(30);

        UniSingleton.CreateSingleton<PatchManager>();

        PatchManager.Instance.Run(PlayMode);
    }
}
