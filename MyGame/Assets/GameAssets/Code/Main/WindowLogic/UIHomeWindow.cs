using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHomeWindow : MonoBehaviour
{
    private Text _version;
    private GameObject _aboutView;

    [SerializeField]
    private string _termUrl = "https://www.google.com";
    [SerializeField]
    private string _policyUrl = "https://www.google.com";

    private void Awake()
    {
        _version = this.transform.Find("version").GetComponent<Text>();
        _aboutView = this.transform.Find("AboutView").gameObject;

        var loginBtn = this.transform.Find("PlayGameButton").GetComponent<Button>();
        loginBtn.onClick.AddListener(OnClickPlayGameBtn);

        var aboutBtn = this.transform.Find("AboutButton").GetComponent<Button>();
        aboutBtn.onClick.AddListener(OnClicAboutBtn);

        var maskBtn = this.transform.Find("AboutView/mask").GetComponent<Button>();
        maskBtn.onClick.AddListener(OnClickMaskBtn);

        var termBtn = this.transform.Find("TermButton").GetComponent<Button>();
        termBtn.onClick.AddListener(OnClickTermBtn);

        var policyBtn = this.transform.Find("PolicyButton").GetComponent<Button>();
        policyBtn.onClick.AddListener(OnClickPolicyBtn);
    }
    private void Start()
    {
        var package = YooAsset.YooAssets.GetPackage("DefaultPackage");
        _version.text = "Version : " + package.GetPackageVersion();
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
    private void OnClickTermBtn()
    {
        Application.OpenURL(_termUrl);
    }
    private void OnClickPolicyBtn()
    {
        Application.OpenURL(_policyUrl);
    }
}