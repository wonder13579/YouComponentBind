using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YouComponentBind;

// 此文件为生成代码的参考模板，指示生成目标
public partial class FirstWindow : YouBindBase
{
    // 序列化出来的控件，统一放view里，保持类字段整洁
    // 私有的，不要直接访问其他界面的控件！
    // @字段定义
    private FirstWindowView view;

    public virtual void Reset()
    {
        FillView();
    }

    [ContextMenu("FillView 给所有引用组件赋值")]
    public void FillView()
    {
        view.btnExitButton = transform.Find("a/b/c").GetComponent<Button>();
        view.tglAutoToggle = transform.Find("a/b/c").GetComponent<Toggle>();
    }

    public virtual void OnEnable()
    {
        view.btnExitButton.onClick.AddListener(OnClickExitButton);
        view.tglAutoToggle.onValueChanged.AddListener(OnValueChangedAutoToggle);
        YouEventCenter.AddListener("OnPlayerLogin", OnPlayerLogin);
    }

    public virtual void OnDisable()
    {
        view.btnExitButton.onClick.RemoveListener(OnClickExitButton);
        YouEventCenter.RemoveListener("OnPlayerLogin", OnPlayerLogin);
    }
}

public partial class FirstWindowView
{
    public int a;
    public float b;
    public Button btnExitButton;
    public Toggle tglAutoToggle;
}

public interface IFirstWindowEventFunction
{
    void OnClickExitButton();
    void OnValueChangedAutoToggle(bool value);
    void OnPlayerLogin();
}