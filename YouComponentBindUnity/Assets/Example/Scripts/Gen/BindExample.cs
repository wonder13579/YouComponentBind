using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 收集器组件
public class BindCollector : MonoBehaviour
{
    public List<Object> bindObjectList = new List<Object>();
    public Dictionary<string, Object> bindObjectDict = new Dictionary<string, Object>();
}

// view 基类
public partial class BaseView
{
    public Text Text1;
    public Text Text2;
}

// view 的继承支持
// 检测到继承后，通过反射检查基类的东西，跳过基类的字段。
// 子类应该是基类的变体。利用变体获取基类的引用列表，直接复制到子类中。子类可修改引用实现重载。
public partial class BindCollectView : BaseView
{
    public Text Text3;
    public Text Text4;
    public Button button;

#if UNITY_EDITOR
    // 应该在编辑器下用某个东西直接给他赋值好，做到运行时零开销
    public void InitializeView(BindCollector collector)
    {
        // 路径式，优点在对不同prefab使用时，能根据路径找到对应节点。抗节点替换。
        Text1 = collector.transform.Find("AllType/tip23424")?.GetComponent<Text>();
        // 引用式，优点在抗节点移动改名。
        Text1 = collector.bindObjectList[0] as Text;
        // 引用式的另一种方案，对排序修改不敏感
        Text2 = collector.bindObjectDict["Text_tip23424"] as Text;
        // 引用式第三种方案，使用反射， view 不生成绑定代码，能放在 BindCollector 中
        BindCollectView view = new();
        view.GetType().GetField("Text_tip23424").SetValue(view, collector.bindObjectList[0] as Text);
    }
#endif

    // 用接口定义，要接受事件的东西直接实现接口，然后把自己赋值过来。
    private IFirstWindowEventFunction eventReceiver;
    void RegisterEvent()
    {
        if (eventReceiver == null) return;
        button.onClick.AddListener(eventReceiver.OnButton_ButtonClick);
    }
}
