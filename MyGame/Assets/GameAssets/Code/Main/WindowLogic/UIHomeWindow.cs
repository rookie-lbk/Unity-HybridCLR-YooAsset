using System.Collections;
using System.Collections.Generic;
using UniFramework.Window;
using UnityEngine;
using UnityEngine.UI;

public class UIHomeWindow : UIWindow
{
    private Text _version;
    private GameObject _aboutView;

    public override void OnCreate()
    {
        _version = this.transform.Find("version").GetComponent<Text>();
        _aboutView = this.transform.Find("AboutView").gameObject;

        var loginBtn = this.transform.Find("PlayGameButton").GetComponent<Button>();
        loginBtn.onClick.AddListener(OnClickPlayGameBtn);

        var aboutBtn = this.transform.Find("AboutButton").GetComponent<Button>();
        aboutBtn.onClick.AddListener(OnClicAboutBtn);

        var maskBtn = this.transform.Find("AboutView/mask").GetComponent<Button>();
        maskBtn.onClick.AddListener(OnClickMaskBtn);
    }
    public override void OnDestroy()
    {
    }
    public override void OnRefresh()
    {
        var package = YooAsset.YooAssets.GetPackage("DefaultPackage");
        _version.text = "Version : " + package.GetPackageVersion();
    }
    public override void OnUpdate()
    {
    }

    private void OnClickPlayGameBtn()
    {
        SceneEventDefine.ChangeToBattleScene.SendEventMessage();
    }
    private void OnClicAboutBtn()
    {
        _aboutView.SetActive(true);
    }
    private void OnClickMaskBtn()
    {
        _aboutView.SetActive(false);
    }
}