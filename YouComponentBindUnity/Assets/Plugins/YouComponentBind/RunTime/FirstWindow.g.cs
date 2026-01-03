// @命名空间@
using UnityEngine.UI;
using YouComponentBind;

// 此文件为生成代码的参考模板，指示生成目标
// @类名改改@
public partial class ExampleWindow : YouBindBase
{
    // 序列化出来的控件，统一放view里，保持类字段整洁
    // 私有的，不要直接访问其他界面的控件！
    private FirstWindowView view;

    public virtual void Reset()
    {
        // @字段填充@
        view.btnExitButton = transform.Find("a/b/c").GetComponent<Button>();
    }

    public virtual void OnEnable()
    {
        // @事件注册@
        view.btnExitButton.onClick.AddListener(OnClickExitButton);
        view.tglAutoToggle.onValueChanged.AddListener(OnValueChangedAutoToggle);
        YouEventCenter.AddListener("OnPlayerLogin", OnPlayerLogin);
    }

    public virtual void OnDisable()
    {
        // @事件注销@
        view.btnExitButton.onClick.RemoveListener(OnClickExitButton);
        view.tglAutoToggle.onValueChanged.RemoveListener(OnValueChangedAutoToggle);
        YouEventCenter.RemoveListener("OnPlayerLogin", OnPlayerLogin);
    }
}

public partial class FirstWindowView
{
    // @字段定义@
    public Button btnExitButton;
    public Toggle tglAutoToggle;
}

public interface IExampleWindowEventFunction
{
    // @事件定义@
    void OnClickExitButton();
    void OnValueChangedAutoToggle(bool value);
    void OnPlayerLogin();
}