# Shyd.Prism

仿 Prism 的**精简 AOT 兼容** Avalonia 应用框架。把 Prism 最核心的使用体验（DialogService、区域导航、事件聚合、MVVM 基座）用**显式泛型注册 + 闭包工厂**重新实现——全程无反射、无程序集扫描、无字符串查类型，`PublishAot=true` + `TrimMode=full` 下零 trim 警告。

> 为什么不用 Prism 本体？Prism 依赖运行时反射，与 NativeAOT/裁剪不兼容。本项目保留它的 API 思想，牺牲动态能力换取 AOT 安全。

## 项目结构

| 项目 | 内容 | 命名空间 |
|---|---|---|
| `src/Shyd.Prism.Core` | 无 UI 依赖：MVVM 基类、键值参数、事件聚合器、i18n 机制（内容由宿主注入） | `Shyd.Prism.Mvvm` / `Parameters` / `Events` / `Localization` |
| `src/Shyd.Prism.Avalonia` | DialogService（内容/窗口弹窗）、区域导航（Region/RegionManager）、`{loc:Tr}` 标记扩展、Toast 通知 | `Shyd.Prism.Dialogs` / `Regions` / `Notifications` / `Localization` |
| `tests/Shyd.Prism.Tests` | 框架单元测试（44 个） | `Shyd.Prism.Tests.*` |

## 快速上手

### 1. 安装

```xml
<!-- 引用 NuGet 包（本地源见 pack-local.ps1，或发布到 nuget.org 后直接用） -->
<PackageReference Include="Shyd.Prism.Core" Version="1.0.0" />
<PackageReference Include="Shyd.Prism.Avalonia" Version="1.0.0" />
```

### 2. 注册弹窗 / 页面（显式泛型，无反射）

```csharp
// Program.cs / ServiceCollection
services.AddDialog<MyDialogWindow, MyDialogViewModel>();          // 窗口弹窗（自带 chrome）
services.AddDialog<MyDialogView, MyDialogViewModel>("myDialog");  // 内容弹窗（UserControl 放入 DialogWindow 宿主）
services.AddPageForNavigation<MainView, MainViewModel>("Main");   // 区域导航页面
services.AddSingleton<IEventAggregator, EventAggregator>();       // 事件聚合器
```

### 3. 弹窗 ViewModel 实现 IDialogAware

```csharp
public sealed partial class MyDialogViewModel : ViewModelBase, IDialogAware
{
    public string Title => "标题";
    public event Action<IDialogResult>? RequestClose;

    public bool CanCloseDialog() => true;
    public void OnDialogOpened(IDialogParameters parameters)
    {
        // parameters.TryGetValue<string>("key", out var value);
    }
    public void OnDialogClosed() { }
}
```

```csharp
// 打开：异步等待结果（内部自动封送 UI 线程、支持超时/宿主配置/复用激活）
var result = await dialogs.ShowDialogAsync<MyDialogViewModel>(new DialogParameters { ["key"] = value });
// 或按名称（仿 Prism 的 ShowDialog("name")）
var result = await dialogs.ShowDialogAsync("myDialog");
```

### 4. 页面 ViewModel 实现 INavigationAware + 区域声明

```xml
<!-- MainWindow.axaml -->
<ContentControl nav:Region.RegionName="MainRegion" />
```

```csharp
public sealed partial class HistoryViewModel : ViewModelBase, INavigationAware { ... }

// 导航 / 返回（导航日志）
regionManager.RequestNavigate("MainRegion", "History");
regionManager.GoBack("MainRegion");
```

### 5. i18n（中文作 key，内容归宿主）

框架只提供机制（JSON 语言包扫描、种子不覆盖用户文件、缺失 key 合并、热重载），**翻译内容由你的应用提供**：

```csharp
// 1) 应用工程放 Localization/packs/en-US.json（EmbeddedResource）
//    {"code":"en-US","displayName":"English","translations":{"发送":"Send"}}
// 2) 扫描自己的嵌入资源产出种子，初始化时注入
Loc.Initialize(dataDirectory, MyAppLanguagePacks.BuiltIn);
```

```xml
<TextBlock Text="{loc:Tr '发送文件'}" />  <!-- xmlns:loc="using:Shyd.Prism.Localization" -->
```

### 6. 事件聚合器

```csharp
var subscription = events.Subscribe<MyEvent>(e => Handle(e));  // Dispose 退订
events.Publish(new MyEvent());
```

## AOT 说明

两个包均标注 `IsAotCompatible=true`。发布 AOT 应用时使用 `dotnet publish -c Release -r <rid> -p:PublishAot=true`，本框架不产生任何 trim 警告（不使用 Activator.CreateInstance / Type.GetType / 程序集扫描；依赖注入走 Microsoft.Extensions.DependencyInjection 显式注册）。

## 版本升级（框架开发者）

```powershell
# 1) 改 src/*/csproj 里的 <Version>
# 2) 打包到本地文件夹源
.\pack-local.ps1            # 默认输出 D:\StudyNode\nuget-local
# 3) 消费项目把 PackageReference 的 Version bump 到新版本后 restore
```

## License

MIT
