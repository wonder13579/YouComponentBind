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
        view.ButtonButton = transform.Find("AllType/Button")?.GetComponent<Button>();
        view.ButtonButton2 = transform.Find("AllType/Button2")?.GetComponent<Button>();
        view.CanvasRenderer2 = transform.Find("AllType/Toggle/2")?.GetComponent<CanvasRenderer>();
        view.CanvasRendererRawImage = transform.Find("AllType/RawImage")?.GetComponent<CanvasRenderer>();
        view.GORawImage = transform.Find("AllType/RawImage")?.gameObject;
        view.InputInputField = transform.Find("AllType/InputField")?.GetComponent<InputField>();
        view.RawRawImage2 = transform.Find("AllType/RawImage2")?.GetComponent<RawImage>();
        view.Text2 = transform.Find("AllType/Toggle/2")?.GetComponent<Text>();
        view.TextItemLabel = transform.Find("AllType/Dropdown/Template/Viewport/Content/Item/ItemLabel")?.GetComponent<Text>();
        view.TextPlaceholder = transform.Find("AllType/InputField/Placeholder")?.GetComponent<Text>();
        view.TextText = transform.Find("AllType/InputField/Text")?.GetComponent<Text>();
        view.TextText2 = transform.Find("AllType/Button2/Text2")?.GetComponent<Text>();
        view.TextText3 = transform.Find("AllType/Button/Text3")?.GetComponent<Text>();
        view.Texttip23424 = transform.Find("AllType/tip23424")?.GetComponent<Text>();
        view.ToggleItem = transform.Find("AllType/Dropdown/Template/Viewport/Content/Item")?.GetComponent<Toggle>();
        view.ToggleToggle = transform.Find("AllType/Toggle")?.GetComponent<Toggle>();
    }

    public virtual void OnEnable()
    {
        view.ButtonButton.onClick.AddListener(OnButtonButtonClick);
        view.ButtonButton2.onClick.AddListener(OnButtonButton2Click);
        view.InputInputField.onValueChanged.AddListener(OnInputInputFieldValueChanged);
        view.InputInputField.onEndEdit.AddListener(OnInputInputFieldEndEdit);
        view.ToggleItem.onValueChanged.AddListener(OnToggleItemValueChanged);
        view.ToggleToggle.onValueChanged.AddListener(OnToggleToggleValueChanged);
    }

    public virtual void OnDisable()
    {
        view.ButtonButton.onClick.RemoveListener(OnButtonButtonClick);
        view.ButtonButton2.onClick.RemoveListener(OnButtonButton2Click);
        view.InputInputField.onValueChanged.RemoveListener(OnInputInputFieldValueChanged);
        view.InputInputField.onEndEdit.RemoveListener(OnInputInputFieldEndEdit);
        view.ToggleItem.onValueChanged.RemoveListener(OnToggleItemValueChanged);
        view.ToggleToggle.onValueChanged.RemoveListener(OnToggleToggleValueChanged);
    }
}

[System.Serializable]
public partial class FirstWindowView
{
    public Button ButtonButton;
    public Button ButtonButton2;
    public CanvasRenderer CanvasRenderer2;
    public CanvasRenderer CanvasRendererRawImage;
    public GameObject GORawImage;
    public InputField InputInputField;
    public RawImage RawRawImage2;
    public Text Text2;
    public Text TextItemLabel;
    public Text TextPlaceholder;
    public Text TextText;
    public Text TextText2;
    public Text TextText3;
    public Text Texttip23424;
    public Toggle ToggleItem;
    public Toggle ToggleToggle;
}

public interface IFirstWindowEventFunction
{
    void OnButtonButtonClick();
    void OnButtonButton2Click();
    void OnInputInputFieldValueChanged(string value);
    void OnInputInputFieldEndEdit(string value);
    void OnToggleItemValueChanged(bool value);
    void OnToggleToggleValueChanged(bool value);
}
