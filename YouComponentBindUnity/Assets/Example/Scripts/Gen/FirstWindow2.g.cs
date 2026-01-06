using YouComponentBind;
using UnityEngine.UI;


// 此文件由YouComponentBind生成，请勿修改。可参考YouComponentBindWindow
public partial class FirstWindow2
{
    [UnityEngine.SerializeField]
    private FirstWindow2View view = new FirstWindow2View();

    public virtual void Reset()
    {
        view.Texttip23424 = transform.Find("AllType/tip23424")?.GetComponent<Text>();
        view.RawRawImage = transform.Find("AllType/RawImage")?.GetComponent<RawImage>();
        view.ButtonButton = transform.Find("AllType/Button")?.GetComponent<Button>();
        view.TextText = transform.Find("AllType/InputField/Text")?.GetComponent<Text>();
        view.TextText2 = transform.Find("AllType/Button/Text2")?.GetComponent<Text>();
        view.ToggleToggle = transform.Find("AllType/Toggle")?.GetComponent<Toggle>();
        view.Text2 = transform.Find("AllType/Toggle/2")?.GetComponent<Text>();
        view.TextLabel = transform.Find("AllType/Dropdown/Label")?.GetComponent<Text>();
        view.ToggleItem = transform.Find("AllType/Dropdown/Template/Viewport/Content/Item")?.GetComponent<Toggle>();
        view.TextItemLabel = transform.Find("AllType/Dropdown/Template/Viewport/Content/Item/ItemLabel")?.GetComponent<Text>();
        view.InputInputField = transform.Find("AllType/InputField")?.GetComponent<InputField>();
        view.TextPlaceholder = transform.Find("AllType/InputField/Placeholder")?.GetComponent<Text>();

    }

    public virtual void OnEnable()
    {
        view.ButtonButton.onClick.AddListener(OnButtonButtonClick);
        view.ToggleToggle.onValueChanged.AddListener(OnToggleToggleValueChanged);
        view.ToggleItem.onValueChanged.AddListener(OnToggleItemValueChanged);
        view.InputInputField.onValueChanged.AddListener(OnInputInputFieldValueChanged);
        view.InputInputField.onEndEdit.AddListener(OnInputInputFieldEndEdit);

    }

    public virtual void OnDisable()
    {
        view.ButtonButton.onClick.RemoveListener(OnButtonButtonClick);
        view.ToggleToggle.onValueChanged.RemoveListener(OnToggleToggleValueChanged);
        view.ToggleItem.onValueChanged.RemoveListener(OnToggleItemValueChanged);
        view.InputInputField.onValueChanged.RemoveListener(OnInputInputFieldValueChanged);
        view.InputInputField.onEndEdit.RemoveListener(OnInputInputFieldEndEdit);

    }
}

[System.Serializable]
public partial class FirstWindow2View
{
    public Text Texttip23424;
    public RawImage RawRawImage;
    public Button ButtonButton;
    public Text TextText;
    public Text TextText2;
    public Toggle ToggleToggle;
    public Text Text2;
    public Text TextLabel;
    public Toggle ToggleItem;
    public Text TextItemLabel;
    public InputField InputInputField;
    public Text TextPlaceholder;

}

public interface IFirstWindow2EventFunction
{
    void OnButtonButtonClick();
    void OnToggleToggleValueChanged(bool value);
    void OnToggleItemValueChanged(bool value);
    void OnInputInputFieldValueChanged(string value);
    void OnInputInputFieldEndEdit(string value);

}
