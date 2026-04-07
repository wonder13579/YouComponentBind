using System.Collections.Generic;
using System.Text;

namespace YouBindCollector
{
    public class CodeGeneraterBase
    {
        public string className;
        public string outputFilePath;
        protected List<string> nameSpaceList = new List<string>();
        protected StringBuilder[] codeContentBuilder = null;
        protected virtual int builderCount => 0;

        public virtual void Clear()
        {
            if (codeContentBuilder == null)
                codeContentBuilder = new StringBuilder[builderCount];
            for (int i = 0; i < builderCount; i++)
            {
                if (codeContentBuilder[i] == null)
                    codeContentBuilder[i] = new StringBuilder();
            }
            className = "";
            outputFilePath = "";
            nameSpaceList.Clear();
            nameSpaceList.Add("UnityEngine");
            foreach (var builder in codeContentBuilder)
                builder.Clear();
        }

        public virtual void AppendObject(BindObjectInfo bindInfo) { }

        public virtual void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo) { }

        public virtual void ExportCodeFile()
        {
            // 清理最后的空行
            for (int i = 0; i < builderCount; i++)
            {
                var builder = codeContentBuilder[i];
                if (builder == null)
                    continue;
                while (true)
                {
                    if (builder.Length < 1)
                        break;
                    var lastChar = builder[builder.Length - 1];
                    if (lastChar != '\n' && lastChar != '\r')
                        break;
                    builder.Remove(builder.Length - 1, 1);
                }
            }
        }
    }
}
