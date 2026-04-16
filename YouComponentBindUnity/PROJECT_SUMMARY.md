# YouComponentBind 项目总结文档

## 1. 摘要

YouComponentBind 是一个 Unity 编辑器侧的 UI 组件绑定与代码生成工具，核心目标是把 `Prefab` 上的节点/组件引用和事件注册自动化，减少手写绑定代码。当前工程以 `Unity 2021.3.8f1c1` 为基础，支持两条生成链路：

1. `C#` 生成：输出 `*.g.cs`（可覆盖）与 `*.cs`（首次生成，不覆盖）。
2. `Lua` 生成（xLua）：输出 `*.g.lua.txt`（可覆盖）与 `*.lua.txt`（首次生成，不覆盖）。

当前示例资源显示：

1. `FirstWindow`（C#）链路完整，可生成并挂载 `FirstWindow` 组件。
2. `FirstLuaWindow`（Lua）链路完整，可通过 `CommonLuaView + LuaSystem` 运行。
3. `UILuaWindow2` 已有绑定配置与预制体，但 `Lua/Gen` 下当前仅有 `.meta`，缺少对应 `.g.lua.txt` / `.lua.txt` 实体文件，属于未完成状态。

## 2. 基础方法

### 2.1 C# 绑定与生成流程

1. 在目标根节点上添加/选中 `YouBindCollector`。
2. 点击 `一键更新代码` 或工具箱中的 `新增绑定流程`。
3. 首次添加 `YouBindCollector` 时只会自动扫描，不直接生成代码，需要再次点击进行生成。
4. 生成器输出：
   - `Assets/Example/Scripts/Gen/<ClassName>.g.cs`
   - `Assets/Example/Scripts/Gen/<ClassName>.cs`（仅首次）
5. 自动尝试挂载目标脚本组件并调用 `InitializeView()` 完成引用赋值。

### 2.2 Lua 绑定与生成流程

1. 将 `YouBindCollector.codeGenerateType` 切换为 `Lua`。
2. 执行生成后输出：
   - `Assets/Example/Resources/Lua/Gen/<ClassName>.g.lua.txt`
   - `Assets/Example/Resources/Lua/Gen/<ClassName>.lua.txt`（仅首次）
3. 自动确保挂载 `CommonLuaView`，并调用 `InitializeView()` 填充 `viewList`。
4. 运行时 `CommonLuaView.Awake()` 通过 `LuaSystem` 加载 `PaneRegistry` 与对应 Lua 文件，再调用：
   - `InitView`
   - `RegisterEvent`
   - `OnEnable` / `OnDisable`

### 2.3 绑定规则与常用机制

1. 自动扫描由 `YouBindTypeConfig` 的 `autoBind` 决定，默认会自动纳入 `Text`、`RawImage`、`Button`、`Toggle`、`InputField` 等。
2. 事件默认模板同样来自类型配置（如 `Button.onClick`、`Toggle.onValueChanged`）。
3. 字段名通过 `SanitizeIdentifier` 清洗，避免非法标识符。
4. 支持排序策略：按类型+字段名、按字段名、按加入顺序、自定义拖拽顺序。
5. `genCode` 可细粒度控制某条绑定/事件是否参与生成。

## 3. 界面功能说明

### 3.1 Inspector（`YouBindCollectorEditor`）

1. `一键更新代码`：执行扫描 + 生成 +（C# 模式）挂载/初始化视图组件。
2. `打开工具箱`：打开主窗口 `YouComponentBindWindow`。

### 3.2 工具箱主窗口（`YouBindCollectorWindow`）

#### 左侧：引用组件列表

1. 显示当前收集到的绑定项，支持搜索与排序切换。
2. 支持直接改字段名，并同步对象名规范化。
3. 缺失引用会高亮，并支持拖入对象回填或 `Delete Ref` 永久删除。
4. 每条绑定下显示事件项，可开关生成并拖拽调整事件顺序。
5. 自定义排序模式下，支持拖拽整条绑定项改变顺序。

#### 右侧：工具面板（4 个页签）

1. `新增组件`：
   - 生成代码
   - 对选中节点快速改名
   - 拖入对象手动添加绑定
   - 快捷显示/切换当前选中对象可绑定组件
2. `更多功能`：
   - 新增绑定流程
   - 自动扫描
   - 生成代码
   - 删除已生成代码文件
3. `设置`：
   - 更新代码后立即刷新
   - 编辑模式显示层级红色 `*` 标记
   - 是否显示不生成代码的组件
4. `错误检测`（`YouBindCommonChecker`）：
   - 路径变更检测与一键修复
   - 引用丢失检测与按历史路径回填
   - View 空引用检测与一键修复（区分 C# / Lua 模式）

### 3.3 层级面板标记

`YouBindHierarchyMark` 会在 Prefab Stage 中给被绑定且 `genCode=true` 的节点显示红色 `*`，用于防止误删关键节点。

## 4. 代码架构

### 4.1 分层结构

1. **数据层（运行时）**
   - `RunTime/YouBindCollector.cs`
   - 定义 `BindObjectInfo`、`BindEventInfo`，序列化存储在 Prefab 上
2. **编辑器交互层**
   - `Editor/YouBindCollectorEditor.cs`
   - `Editor/YouBindCollectorWindow.cs`
   - `Editor/YouBindCollectorController.cs`
3. **规则与工具层**
   - `Editor/YouBindTypeConfig.cs`
   - `Editor/YouBindGlobalDefine.cs`
   - `Editor/YouBindUtils.cs`
   - `Editor/YouBindHierarchyMark.cs`
4. **质量检查层**
   - `Editor/YouBindCommonChecker.cs`
5. **代码生成层**
   - `Editor/CodeGenerators/YouBindCodeGenerater.cs`
   - `CSharpGenCodeFileGenerater` / `CSharpCustomCodeFileGenerater`
   - `LuaGenCodeFileGenerater` / `LuaCustomCodeFileGenerater`
6. **Lua 运行桥接层（示例）**
   - `Example/Scripts/Lua/LuaSystem.cs`
   - `Example/Scripts/Lua/CommonLuaView.cs`
   - `Example/Resources/Lua/PaneRegistry.lua.txt`
7. **示例产物层**
   - `Example/Scripts/Gen/*`
   - `Example/Resources/Lua/Gen/*`
   - `Example/PrefabsUI/*`

### 4.2 主流程（编辑器到运行时）

1. `Prefab + YouBindCollector` 保存绑定元数据。
2. `YouBindCollectorController` 扫描/维护 `bindInfoList`。
3. `YouBindCodeGenerater` 按模式调用对应生成器输出文件。
4. C# 模式：挂载目标组件并 `InitializeView()`。
5. Lua 模式：挂载 `CommonLuaView` 并填充 `viewList`。
6. 运行时由 `LuaSystem` + `PaneRegistry` 调度 Lua 面板函数。

### 4.3 当前可见的架构特征

1. 输出路径写死在 `YouBindGlobalDefine`（当前指向 `Assets/Example/...`），更偏示例工程结构。
2. `EventCenter.cs` 目前为注释状态，事件中心尚未启用。
3. 生成器采用模板拼接方式，易读易改，适合按项目规范二次定制。
