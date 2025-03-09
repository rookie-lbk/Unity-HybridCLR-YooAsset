using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"UniTask.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<HotUpdateLuncher.<Start>d__1>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<HotUpdateLuncher.<Start>d__1>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<object>
	// System.Action<object>
	// System.Func<int>
	// }}

	public void RefMethods()
	{
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,HotUpdateLuncher.<Start>d__1>(Cysharp.Threading.Tasks.UniTask.Awaiter&,HotUpdateLuncher.<Start>d__1&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<HotUpdateLuncher.<Start>d__1>(HotUpdateLuncher.<Start>d__1&)
		// Cysharp.Threading.Tasks.UniTask.Awaiter Cysharp.Threading.Tasks.EnumeratorAsyncExtensions.GetAwaiter<object>(object)
	}
}