# 新版日志模块使用文档

## 概述

日志模块已完成容器化改造，原有的静态 `LogManager` 类已被移除，新的日志服务通过 `ILogManager` 接口提供，由 **VContainer** 进行依赖注入管理。

新架构的核心优势：

- **依赖注入**：日志服务作为容器管理的单例，可以轻松注入到任何需要日志记录的类中。
- **配置化**：通过 `LogManagerController` 或代码动态配置日志分类、级别、颜色等，支持运行时切换。
- **模块化**：每个模块（System、Localization、Resource 等）可以独立启用/禁用日志，并拥有不同的颜色标识。
- **条件编译**：Verbose、Debug、Info 等低级别日志在非开发版本中自动剥离（通过 `[Conditional]` 属性）。

## 核心组件

### 1. ILogManager 接口

位于 `TradeGame.Runtime.Logging.ILogManager`，定义了日志记录和配置的所有方法。

主要方法：

- **日志记录**：`Verbose`、`Debug`、`Info`、`Warning`、`Error`（均支持 `LogCategory` 分类和可选的 `UnityEngine.Object` 上下文）。
- **配置**：
  - `SetEnabledCategories` / `EnableCategory` / `DisableCategory` – 控制哪些分类的日志可以输出。
  - `SetMinLogLevel` – 设置全局最低日志级别（低于该级别的日志不会输出）。
  - `SetGlobalEnabled` – 全局启用/禁用日志。
  - `GetEnabledCategories`、`GetMinLogLevel`、`IsCategoryEnabled` – 查询当前配置。
- **预设配置**：`PresetImportantOnly`、`PresetCoreOnly`、`PresetDebugAll`、`PresetSilent`、`PresetRelease` 等快速设置方法。
- **调试工具**：`PrintCurrentConfig`（仅在 Editor 中有效）打印当前配置状态。

### 2. LogManagerService

`ILogManager` 的默认实现，位于 `TradeGame.Runtime.Logging.LogManagerService`。

功能特点：

- 内部维护分类启用位掩码、最低日志级别、全局开关。
- 每条日志都会根据分类、级别和当前配置决定是否输出。
- 输出到 Unity Console，并自动附加分类前缀和颜色（颜色可在 `LoggingConfig` 中配置）。
- 支持条件编译：`Verbose`、`Debug`、`Info` 仅在 `UNITY_EDITOR` 或 `DEVELOPMENT_BUILD` 下有效，发布版本中会被编译器移除。

### 3. LogManagerController

MonoBehaviour 组件，用于在 Inspector 中可视化配置日志模块。

用途：

- 挂载到场景中任意 GameObject（建议在启动场景）。
- 提供 Inspector 界面，可勾选需要启用的分类、选择最低日志级别、一键应用预设。
- 配置变化会立即生效（通过 `ILogManager` 实例）。

**注意**：`LogManagerController` 本身不产生任何日志，它只是配置界面。真正的日志输出由 `LogManagerService` 完成。

### 4. LoggingServiceRegistrar

服务注册器，负责将 `ILogManager` 注册到 VContainer 容器。

注册方式：

- **自动注册**：如果 Bootstrapper 的 `Service Registrars` 列表为空，系统会自动查找并调用所有实现了 `IServiceRegistrar` 的组件（包括 `LoggingServiceRegistrar`）。
- **手动注册**：在 Bootstrapper 的 `Service Registrars` 列表中手动添加 `LoggingServiceRegistrar` 组件（推荐，显式控制注册顺序）。

注册的生命周期为 **Singleton**，保证整个应用只有一个日志服务实例。

### 5. LoggingConfig

ScriptableObject 资源，用于定义各日志分类的颜色、缩写等外观配置。

位置：`Assets/TradeGame/Scripts/Runtime/Logging/LoggingConfig.asset`

可以通过编辑器修改，运行时加载。当前 `LogManagerService` 会读取该配置来决定输出颜色。

## 快速开始

### 步骤1：确保容器已注册日志服务

检查 Bootstrapper 所在场景是否包含 `LoggingServiceRegistrar`（或已通过自动发现注册）。如果没有，请执行以下任一操作：

1. **手动添加**：在 Bootstrapper GameObject 上添加 `LoggingServiceRegistrar` 组件，并将其拖入 Bootstrapper 的 `Service Registrars` 列表。
2. **依赖注入**：如果使用纯代码注册，可以在自定义 Installer 中调用：
   ```csharp
   builder.Register<LogManagerService>(Lifetime.Singleton).As<ILogManager>();
   ```

### 步骤2：在需要日志的类中注入 ILogManager

**推荐做法（构造函数注入）**：

```csharp
using TradeGame.Runtime.Logging;
using VContainer;

public class MyService
{
    private readonly ILogManager _logger;

    public MyService(ILogManager logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.Info(LogCategory.System, "工作开始");
        // ...
    }
}
```

**对于 MonoBehaviour 组件（字段注入）**：

```csharp
using TradeGame.Runtime.Logging;
using UnityEngine;
using VContainer.Unity;

public class MyComponent : MonoBehaviour
{
    [Inject] private ILogManager _logger;

    private void Start()
    {
        _logger.Info(LogCategory.System, "组件启动", this);
    }
}
```

### 步骤3：配置日志输出

**方式一：通过 LogManagerController（运行时）**

1. 在场景中创建空 GameObject，添加 `LogManagerController` 组件。
2. 在 Inspector 中勾选需要输出的分类（例如 System、Localization、Resource）。
3. 选择最低日志级别（例如 Info，则 Verbose 和 Debug 不会输出）。
4. 点击“应用配置”或直接勾选“自动应用”。

**方式二：通过代码（运行时）**

```csharp
// 从容器解析 ILogManager（如果尚未注入）
var logger = Bootstrapper.Resolve<ILogManager>();

// 只启用 System 和 Localization 分类
logger.SetEnabledCategories(LogCategory.System | LogCategory.Localization);

// 设置最低级别为 Warning（Info 及以下不输出）
logger.SetMinLogLevel(LogLevel.Warning);

// 应用“仅重要”预设（相当于仅启用 Error 级别的 System 分类）
logger.PresetImportantOnly();
```

**方式三：通过 LoggingConfig（编辑器）**

修改 `LoggingConfig.asset` 中的颜色和缩写，影响输出外观。

## 迁移指南

### 原有代码中使用 `LogManager.XXX` 的调用如何迁移？

**情况1：该类型已支持依赖注入（例如通过构造函数或字段注入）**

直接将 `LogManager.XXX` 替换为 `_logger.XXX`（其中 `_logger` 是注入的 `ILogManager` 字段）。

**情况2：该类型暂时无法注入（例如静态工具类、旧式 MonoBehaviour）**

临时方案：使用 `Bootstrapper.Resolve<ILogManager>()` 获取实例。

```csharp
// 旧代码
LogManager.Info(LogCategory.Localization, "加载完成");

// 新代码
var logger = Bootstrapper.Resolve<ILogManager>();
logger.Info(LogCategory.Localization, "加载完成");
```

**注意**：`Bootstrapper.Resolve` 要求容器已构建且已注册 `ILogManager`。在容器未构建时调用会返回 null，并回退到临时实例（仅输出 Unity Debug 日志）。

**情况3：希望完全移除日志调用（例如在发布版本中）**

若代码中的日志仅为调试用途，可将其改为条件编译：

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    _logger.Debug(LogCategory.System, "调试信息");
#endif
```

### 已更新的文件示例

以下文件已完成迁移，可作为参考：

- `Assets/TradeGame/Scripts/Runtime/Logging/LogManagerController.cs`
- `Assets/TradeGame/Scripts/Runtime/Framework/DependencyInjection/Bootstrapper.cs`
- `Assets/TradeGame/Scripts/Runtime/Localization/Core/LocalizationHelper.cs`（部分）
- `Assets/TradeGame/Scripts/Runtime/Localization/Core/ResourceLoaders/AddressablesLocalizationLoader.cs`（待更新）

## 常见问题

### 1. 日志没有输出？

- 检查容器是否已构建（Bootstrapper.Build() 是否被调用）。
- 检查 `ILogManager` 是否已注册（查看 Bootstrapper 的 Service Registrars 列表）。
- 检查当前分类是否被启用（`LogManagerController` 或代码配置）。
- 检查日志级别是否低于配置的最低级别。

### 2. 如何为自定义模块添加新的日志分类？

在 `LogEnums.cs` 的 `LogCategory` 枚举中添加新值，并确保其位数在 0~31 之间。

```csharp
[Flags]
public enum LogCategory
{
    System      = 1 << 0,
    Localization = 1 << 1,
    Resource    = 1 << 2,
    // 添加你的分类
    MyModule    = 1 << 3,
}
```

然后在 `LoggingConfig.asset` 中为该分类配置颜色和缩写。

### 3. 如何在不启动 Unity Editor 的情况下使用日志（例如单元测试）？

可以手动创建 `LogManagerService` 实例（不依赖容器）：

```csharp
var logger = new LogManagerService();
logger.EnableCategory(LogCategory.System);
logger.Info(LogCategory.System, "测试日志");
```

### 4. 为什么 Verbose/Debug/Info 在打包后消失了？

这些方法标有 `[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]`，在非开发版本中编译器会直接移除调用。若希望保留，请使用 `Warning` 或 `Error` 级别，或定义 `DEVELOPMENT_BUILD` 宏。

## 高级用法

### 自定义日志输出目标

继承 `ILogManager` 并实现自己的输出逻辑（例如写入文件、发送到服务器），然后在容器中注册你的实现即可替换默认的 `LogManagerService`。

### 动态配置热更新

通过监听 `LogManagerController` 的配置变化事件，可以在运行时动态调整日志输出，无需重启应用。

### 性能注意事项

- 日志字符串拼接在调用时进行，若分类被禁用，仍会产生拼接开销。若性能敏感，可先检查分类是否启用：
  ```csharp
  if (_logger.IsCategoryEnabled(LogCategory.MyModule))
  {
      _logger.Info(LogCategory.MyModule, $"复杂字符串拼接：{ExpensiveMethod()}");
  }
  ```

## 总结

新版日志模块完全遵循依赖注入原则，提供了灵活的配置和良好的运行时控制。移除静态 `LogManager` 后，代码的可测试性和模块化程度得到提升。

**推荐做法**：在新代码中始终通过构造函数注入 `ILogManager`，避免直接使用 `Bootstrapper.Resolve`。对于旧代码，可逐步迁移，或暂时使用 `Bootstrapper.Resolve` 作为过渡。

如需进一步帮助，请参阅项目中的集成测试（`BootstrapperIntegrationTests.cs`）或联系框架负责人。