# AFramework AI Skills 知识库

> **目标**: 让 GitHub Copilot 通过阅读这些文档，深度理解项目架构和代码规范

## 📚 Skills 文档清单

| 文件 | 用途 | 优先级 |
|------|------|--------|
| [01-Framework-Overview.md](./01-Framework-Overview.md) | 框架总览、技术栈、设计原则 | ⭐⭐⭐ 必读 |
| [02-Code-Examples.md](./02-Code-Examples.md) | 代码示例库、常用模式 | ⭐⭐⭐ 必读 |
| [03-API-Reference.md](./03-API-Reference.md) | API 快速参考手册 | ⭐⭐ 推荐 |

## 🔧 C# 模板文件（可直接复制）

| 模板文件 | 用途 | 包含功能 |
|----------|------|----------|
| [TaskSchedulerTemplate.cs](./Templates/TaskSchedulerTemplate.cs) | 任务调度完整流程 | RunSequential、Run、CancellationToken、EventBus |
| [EventBusUITemplate.cs](./Templates/EventBusUITemplate.cs) | 事件驱动 UI 更新 | Subscribe、DOTween、UniRx、CompositeDisposable |
| [VContainerServiceTemplate.cs](./Templates/VContainerServiceTemplate.cs) | 依赖注入配置 | Register、Resolve、Singleton、Transient |
| [UniTaskAsyncTemplate.cs](./Templates/UniTaskAsyncTemplate.cs) | 异步编程模式 | Delay、LoadScene、WhenAll、CancellationToken |
| [UniRxObservableTemplate.cs](./Templates/UniRxObservableTemplate.cs) | 响应式编程 | Timer、Interval、EveryUpdate、Subject |
| [DOTweenAnimationTemplate.cs](./Templates/DOTweenAnimationTemplate.cs) | 动画系统 | DOMove、DOFade、Sequence、Tweener 管理 |

## 🎯 如何使用 Skills

### 方式1: GitHub Copilot 自动读取

GitHub Copilot 会自动扫描以下位置：
- `.github/copilot-instructions.md` - 全局指令
- `docs/AI-Skills/*.md` - 技能文档
- `README*.md` - 项目说明

**无需额外配置**，Copilot 会在生成代码时参考这些文档。

### 方式2: 直接复制模板文件

复制 `docs/AI-Skills/Templates/` 目录下的 C# 模板文件：

```csharp
// 1. 复制 TaskSchedulerTemplate.cs
// 2. 重命名为 MyController.cs
// 3. 修改类名和具体任务逻辑
// 4. 保留结构（依赖注入、取消令牌、清理代码）
```

### 方式3: 主动引用

在代码注释中引用模板：

```csharp
// @template Templates/TaskSchedulerTemplate.cs
// 创建一个类似的任务调度流程
public class MyController : MonoBehaviour
{
    // Copilot 会参考模板代码生成
}
```

### 方式3: Chat 提问

在 Copilot Chat 中提问时引用：

```
@workspace 根据 docs/AI-Skills/02-Code-Examples.md 中的 EventBus 模式，帮我创建一个玩家数据更新的事件系统
```

## 🧠 Copilot 学习检查点

创建代码后，检查 AI 是否理解了以下要点：

### ✅ 基础理解
- [ ] 使用 `VContainer` 解析依赖而非单例
- [ ] 使用 `UniTask` 而非 `Coroutine`
- [ ] 异步方法命名带 `Async` 后缀

### ✅ 架构理解
- [ ] 区分工具层（TaskScheduler）和业务层（ProcedureManager）
- [ ] 通过 EventBus 解耦 UI 和业务逻辑
- [ ] 使用 LogManager 而非 Debug.Log

### ✅ 生命周期理解
- [ ] UniRx 订阅使用 `CompositeDisposable`
- [ ] DOTween 动画保存 `Tweener` 引用
- [ ] 提供 `OnDestroy()` 清理代码

### ✅ 模式理解
- [ ] 顺序任务用 `RunSequential()`
- [ ] 并行任务用 `Run()`
- [ ] 取消令牌用 `CancellationTokenSource`

## 📖 学习路径建议

### 新 AI 助手（首次使用）
1. 阅读 `.github/copilot-instructions.md` - 了解全局约定
2. 阅读 `01-Framework-Overview.md` - 理解架构分层
3. 阅读 `02-Code-Examples.md` - 学习代码模式
4. 参考示例文件:
   - `GameStartupController.cs` - TaskScheduler 完整案例
   - `StartupProgressUI.cs` - EventBus + UI 案例

### 已有基础（快速查找）
1. 需要 API → 查看 `03-API-Reference.md`
2. 需要示例 → 查看 `02-Code-Examples.md`
3. 需要理解架构 → 查看 `01-Framework-Overview.md`

## 🔍 快速查找指南

**我需要创建...**

| 需求 | 参考文档 | 示例文件 |
|------|----------|----------|
| 任务调度流程 | `02-Code-Examples.md#1️⃣` | GameStartupController.cs |
| 事件驱动 UI | `02-Code-Examples.md#2️⃣` | StartupProgressUI.cs |
| 依赖注入配置 | `02-Code-Examples.md#3️⃣` | Bootstrapper.cs |
| 平滑动画 | `02-Code-Examples.md#4️⃣` | StartupProgressUI.cs |
| 取消令牌 | `02-Code-Examples.md#5️⃣` | GameStartupController.cs |
| UniRx 订阅 | `02-Code-Examples.md#6️⃣` | StartupProgressUI.cs |

## 💡 提升 AI 理解效果

### 技巧1: 在文件顶部添加注释

```csharp
// @framework AFramework
// @pattern TaskScheduler + EventBus
// @reference docs/AI-Skills/02-Code-Examples.md#1️⃣
public class MyController : MonoBehaviour
{
    // AI 会更准确地生成符合框架的代码
}
```

### 技巧2: 使用注释引导

```csharp
// 创建一个类似 GameStartupController 的启动流程
// 包含4个阶段：检查 → 下载 → 解压 → 初始化
public class UpdateController : MonoBehaviour
{
    // Copilot 会参考 GameStartupController.cs 的结构
}
```

### 技巧3: 明确指定模式

```csharp
// 使用 EventBus 模式，参考 StartupProgressUI
// 订阅 DownloadProgressEvent 更新进度条
public class DownloadUI : MonoBehaviour
{
    // Copilot 会生成 EventBus 订阅代码
}
```

## 🚀 最佳实践

1. **保持 Skills 更新** - 新增核心功能后更新文档
2. **添加示例代码** - 在 `02-Code-Examples.md` 中补充新模式
3. **明确引用路径** - 使用 `@see` 或 `@reference` 注释
4. **测试 AI 理解** - 创建代码后检查是否符合规范

## 📝 Skills 维护规则

### 何时更新 Skills？

- ✅ 添加新的核心服务（如新的 Manager）
- ✅ 引入新的设计模式
- ✅ 发现 AI 频繁出错的场景
- ✅ 更新依赖库版本（如 VContainer 2.0）

### 更新内容包括：

1. **框架总览** - 新增服务、新增依赖
2. **代码示例** - 新模式的完整示例
3. **API 参考** - 新接口的方法列表
4. **Copilot 指令** - 新的约定和规范

## 🎓 进阶技巧

### 创建专用 Skill

为特定模块创建独立 Skill 文档：

```
docs/AI-Skills/
├── 01-Framework-Overview.md
├── 02-Code-Examples.md
├── 03-API-Reference.md
├── 04-UI-System-Skill.md        # UI 系统专项
├── 05-Network-System-Skill.md   # 网络系统专项
└── 06-Audio-System-Skill.md     # 音频系统专项
```

### 使用 Mermaid 图表

在文档中添加架构图：

```markdown
## 架构图
\```mermaid
graph TD
    A[Business Layer] --> B[Tool Layer]
    B --> C[Infrastructure]
\```
```

GitHub Copilot 可以理解图表结构。

---

## 📞 反馈与改进

发现 AI 理解不准确？

1. 检查相关 Skill 文档是否完整
2. 在对应文档中补充示例
3. 在 `.github/copilot-instructions.md` 中强调规则
4. 使用注释引导 AI 参考正确示例

---

**Happy Coding with AI! 🤖✨**
