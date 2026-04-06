using UnityEngine;
using UnityEngine.UI;
using YouBindCollector;

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
        view.GOtransformAndGameObject = transform.Find("AllType/transformAndGameObject")?.gameObject;
        view.InputInputField = GetComponent<global::UnityEngine.UI.InputField>();
        view.RawRawImage = transform.Find("AllType/RawImage")?.GetComponent<global::UnityEngine.UI.RawImage>();
        view.Text2 = transform.Find("AllType/Toggle/2")?.GetComponent<global::UnityEngine.UI.Text>();
        view.TextItemLabel = GetComponent<global::UnityEngine.UI.Text>();
        view.TextLabel = transform.Find("AllType/Dropdown/Label")?.GetComponent<global::UnityEngine.UI.Text>();
        view.TextPlaceholder = transform.Find("AllType/InputField/Placeholder")?.GetComponent<global::UnityEngine.UI.Text>();
        view.TextText = transform.Find("AllType/InputField/Text")?.GetComponent<global::UnityEngine.UI.Text>();
        view.TextText2 = transform.Find("AllType/Button/Text2")?.GetComponent<global::UnityEngine.UI.Text>();
        view.Texttip23424 = transform.Find("AllType/tip23424")?.GetComponent<global::UnityEngine.UI.Text>();
        view.ToggleItem = transform.Find("AllType/Dropdown/Template/Viewport/Content/Item")?.GetComponent<global::UnityEngine.UI.Toggle>();
        view.ToggleToggle = transform.Find("AllType/Toggle")?.GetComponent<global::UnityEngine.UI.Toggle>();
        view.TFtransformAndGameObject = transform.Find("AllType/transformAndGameObject");
        view.RTFRawImage = transform.Find("AllType/RawImage") as RectTransform;
        view.GORawImage = transform.Find("AllType/RawImage")?.gameObject;
        view.RTFFirstWindow = transform as RectTransform;
        view.YouBindCollectorFirstWindow = GetComponent<global::YouBindCollector.YouBindCollector>();
    }

    public virtual void OnEnable()
    {
        view.InputInputField.onValueChanged.AddListener(OnInputInputFieldValueChanged);
        view.InputInputField.onEndEdit.AddListener(OnInputInputFieldEndEdit);
        view.ToggleItem.onValueChanged.AddListener(OnToggleItemValueChanged);
        view.ToggleToggle.onValueChanged.AddListener(OnToggleToggleValueChanged);
    }

    public virtual void OnDisable()
    {
        view.InputInputField.onValueChanged.RemoveListener(OnInputInputFieldValueChanged);
        view.InputInputField.onEndEdit.RemoveListener(OnInputInputFieldEndEdit);
        view.ToggleItem.onValueChanged.RemoveListener(OnToggleItemValueChanged);
        view.ToggleToggle.onValueChanged.RemoveListener(OnToggleToggleValueChanged);
    }
}

[System.Serializable]
public partial class FirstWindowView
{
    public global::UnityEngine.GameObject GOtransformAndGameObject;
    public global::UnityEngine.UI.InputField InputInputField;
    public global::UnityEngine.UI.RawImage RawRawImage;
    public global::UnityEngine.UI.Text Text2;
    public global::UnityEngine.UI.Text TextItemLabel;
    public global::UnityEngine.UI.Text TextLabel;
    public global::UnityEngine.UI.Text TextPlaceholder;
    public global::UnityEngine.UI.Text TextText;
    public global::UnityEngine.UI.Text TextText2;
    public global::UnityEngine.UI.Text Texttip23424;
    public global::UnityEngine.UI.Toggle ToggleItem;
    public global::UnityEngine.UI.Toggle ToggleToggle;
    public global::UnityEngine.Transform TFtransformAndGameObject;
    public global::UnityEngine.RectTransform RTFRawImage;
    public global::UnityEngine.GameObject GORawImage;
    public global::UnityEngine.RectTransform RTFFirstWindow;
    public global::YouBindCollector.YouBindCollector YouBindCollectorFirstWindow;
}

public interface IFirstWindowEventFunction
{
    void OnInputInputFieldValueChanged(string value);
    void OnInputInputFieldEndEdit(string value);
    void OnToggleItemValueChanged(bool value);
    void OnToggleToggleValueChanged(bool value);
}
