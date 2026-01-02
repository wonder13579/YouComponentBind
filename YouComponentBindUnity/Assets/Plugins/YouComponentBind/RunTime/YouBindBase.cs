using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YouComponentBind
{
    public partial class YouBindBase : MonoBehaviour
    {
#if UNITY_EDITOR
        public List<ComponentBindInfo> bindInfoList = new List<ComponentBindInfo>();
#endif
    }

#if UNITY_EDITOR
    [Serializable]
    public class ComponentBindInfo
    {
        public string fieldName;

        [NonSerialized] private Type _bindType;
        public Type bindType
        {
            get { return GetBindType(); }
            set { SetBindType(value); }
        }// type似乎不会被序列化，我们需要存类型名
        private string typeFullName;
        public Object bindObject;// 可能是component也可能是GameObject
        public bool genCode;

        public string relativePath;
        public List<BindEventInfo> eventInfoList = new List<BindEventInfo>();
        [NonSerialized] public int searchPriority; // 仅给编辑界面使用，存储搜索权重
        [NonSerialized] public bool foldout = false; // 仅给编辑界面使用，存储是否展开

        public Transform GetTransform()
        {
            return (bindObject as Component)?.transform ?? (bindObject as GameObject)?.transform;
        }

        public Type GetBindType()
        {
            if (_bindType == null)
                _bindType = Type.GetType(typeFullName);
            return _bindType;
        }

        public void SetBindType(Type bindType)
        {
            _bindType = bindType;
            typeFullName = bindType.AssemblyQualifiedName;
        }
    }

    [Serializable]
    public class BindEventInfo
    {
        public bool genCode;
        public string eventName; // onClick
        [NonSerialized] public ComponentBindInfo targetInfo;
    }
#endif
}