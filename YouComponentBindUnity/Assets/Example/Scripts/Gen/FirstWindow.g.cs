using UnityEngine;
using UnityEngine.UI;

// 此文件由YouBindCollector生成，请勿修改。可参考YouBindCollectorWindow
public partial class FirstWindow
{
    [SerializeField]
    private FirstWindowView view = new FirstWindowView();

    public virtual void Reset()
    {
        InitializeView();
    }

    [ContextMenu("为view上引用的字段赋值"), ExecuteInEditMode]
    public void InitializeView()
    {
        view.Text_tip23424 = transform.Find("AllType/tip23424")?.GetComponent<global::UnityEngine.UI.Text>();
        view.Raw_RawImage = transform.Find("AllType/RawImage")?.GetComponent<global::UnityEngine.UI.RawImage>();
        view.Button_Button = transform.Find("AllType/Button")?.GetComponent<global::UnityEngine.UI.Button>();
        view.Text_Text2 = transform.Find("AllType/tip23424")?.GetComponent<global::UnityEngine.UI.Text>();
        view.Toggle_Toggle = transform.Find("AllType/Toggle")?.GetComponent<global::UnityEngine.UI.Toggle>();
        view.Text_2 = transform.Find("AllType/Toggle/2")?.GetComponent<global::UnityEngine.UI.Text>();
        view.Text_Label = transform.Find("AllType/Dropdown/Label")?.GetComponent<global::UnityEngine.UI.Text>();
        view.Toggle_Item = transform.Find("AllType/Dropdown/Template/Viewport/Content/Item")?.GetComponent<global::UnityEngine.UI.Toggle>();
        view.Text_Placeholder = transform.Find("AllType/InputField/Placeholder")?.GetComponent<global::UnityEngine.UI.Text>();
        view.Text_Text = transform.Find("AllType/InputField/Text")?.GetComponent<global::UnityEngine.UI.Text>();
        view.GO_Scrollbar = transform.Find("AllType/Scrollbar")?.gameObject;
        view.Text_Text222 = transform.Find("Text444 (1)")?.GetComponent<global::UnityEngine.UI.Text>();
        view.Text_Text333 = transform.Find("Text555")?.GetComponent<global::UnityEngine.UI.Text>();
    }

    public virtual void OnEnable()
    {
        view.Button_Button.onClick.AddListener(OnButton_ButtonClick);
        view.Toggle_Toggle.onValueChanged.AddListener(OnToggle_ToggleValueChanged);
        view.Toggle_Item.onValueChanged.AddListener(OnToggle_ItemValueChanged);
    }

    public virtual void OnDisable()
    {
        view.Button_Button.onClick.RemoveListener(OnButton_ButtonClick);
        view.Toggle_Toggle.onValueChanged.RemoveListener(OnToggle_ToggleValueChanged);
        view.Toggle_Item.onValueChanged.RemoveListener(OnToggle_ItemValueChanged);
    }
}

[System.Serializable]
public partial class FirstWindowView
{
    public global::UnityEngine.UI.Text Text_tip23424;
    public global::UnityEngine.UI.RawImage Raw_RawImage;
    public global::UnityEngine.UI.Button Button_Button;
    public global::UnityEngine.UI.Text Text_Text2;
    public global::UnityEngine.UI.Toggle Toggle_Toggle;
    public global::UnityEngine.UI.Text Text_2;
    public global::UnityEngine.UI.Text Text_Label;
    public global::UnityEngine.UI.Toggle Toggle_Item;
    public global::UnityEngine.UI.Text Text_Placeholder;
    public global::UnityEngine.UI.Text Text_Text;
    public global::UnityEngine.GameObject GO_Scrollbar;
    public global::UnityEngine.UI.Text Text_Text222;
    public global::UnityEngine.UI.Text Text_Text333;
}

public interface IFirstWindowEventFunction
{
    void OnButton_ButtonClick();
    void OnToggle_ToggleValueChanged(bool value);
    void OnToggle_ItemValueChanged(bool value);
}
