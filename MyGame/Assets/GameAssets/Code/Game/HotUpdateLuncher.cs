using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using YooAsset;

public class HotUpdateLuncher : MonoBehaviour
{
    public static void Run()
    {

    }
    // Start is called before the first frame update
    public async UniTaskVoid Start()
    {
        //await YooAssets.LoadSceneAsync("HotUpdateScene");
        await YooAssets.LoadSceneAsync("HotUpdateScene");
        //Debug.Log("热更新测试");
    }
    void Update()
    {
    }
}
