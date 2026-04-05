using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace YouBindCollector
{
    [DisallowMultipleComponent]
    public partial class YouBindCollector : MonoBehaviour
    {
#if UNITY_EDITOR
        public string targetClassName;
        public List<BindObjectInfo> bindInfoList = new List<BindObjectInfo>();
        public SortOrder sortOrder = SortOrder.TypeAndName;

        [NonSerialized]
        private readonly HashSet<Object> _joinedObjectSet = new HashSet<Object>();
        public HashSet<Object> joinedObjectSet => GetJoinedObjectSet();

        [NonSerialized]
        private readonly HashSet<Transform> _joinedTransformSet = new HashSet<Transform>();
        public HashSet<Transform> joinedTransformSet => GetJoinedTransformSet();

        public HashSet<Object> GetJoinedObjectSet()
        {
            if (_joinedObjectSet.Count <= 0 && bindInfoList.Count > 0)
            {
                bindInfoList.ForEach(p =>
                {
                    if (p?.bindObject != null)
                        _joinedObjectSet.Add(p.bindObject);
                });
            }
            return _joinedObjectSet;
        }

        public HashSet<Transform> GetJoinedTransformSet()
        {
            if (_joinedTransformSet.Count <= 0 && bindInfoList.Count > 0)
            {
                bindInfoList.ForEach(p =>
                {
                    if (!p.genCode)
                        return;
                    if (p?.bindObject == null)
                        return;
                    var tf = (p?.bindObject as Component)?.transform ?? (p?.bindObject as GameObject)?.transform;
                    if (tf == null)
                        return;
                    _joinedTransformSet.Add(tf);
                });
            }
            return _joinedTransformSet;
        }
        public void ClearJoinedTransformSet()
        {
            _joinedTransformSet.Clear();
        }
        public enum SortOrder : int
        {
            TypeAndName = 0,//先按类型然后是字段名
            Name = 1,//字段名
            JoinOrder = 2,//按加入顺序
            Custom = 3,//不自动排序
        }
#endif
    }

#if UNITY_EDITOR
    // 想保存到prefab上的数据，不能放Editor
    [Serializable]
    public class BindObjectInfo
    {
        public string fieldName;

        [NonSerialized] private Type _bindType;
        public Type bindType
        {
            get { return GetBindType(); }
            set { SetBindType(value); }
        }// type似乎不会被序列化，我们需要存类型名
        [SerializeField]
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
    }
#endif
}