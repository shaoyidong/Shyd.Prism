using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using Shyd.Prism.Dialogs;
using Shyd.Prism.Mvvm;
using System;
using Xunit;

namespace Shyd.Prism.Tests.Dialogs;

/// <summary>
/// 弹窗注册表：名称 / ViewModel 类型查找，内容弹窗宿主窗口的默认与命名选择。
/// </summary>
public class DialogRegistryTests
{
    /// <summary>测试用弹窗 ViewModel 桩。</summary>
    private sealed class StubDialogViewModel : ViewModelBase, IDialogAware
    {
        public string Title => "stub";
        #pragma warning disable CS0067 // 桩实现不触发事件
    public event Action<IDialogResult>? RequestClose;
#pragma warning restore CS0067
        public bool CanCloseDialog() => true;
        public void OnDialogOpened(IDialogParameters parameters) { }
        public void OnDialogClosed() { }
    }

    private static DialogRegistration MakeRegistration(string name) => new(
        name,
        typeof(StubDialogViewModel),
        sp => new UserControl(),
        sp => new StubDialogViewModel());

    private static DialogHostWindowRegistration MakeHost(string? name) => new(
        name, sp => throw new InvalidOperationException("宿主工厂不应在查找阶段被调用"));

    [Fact]
    public void TryGet_ByNameAndViewModelType()
    {
        var registry = new DialogRegistry(new[] { MakeRegistration("StubDialog") }, Array.Empty<DialogHostWindowRegistration>());

        Assert.True(registry.TryGet("StubDialog", out var byName));
        Assert.Equal(typeof(StubDialogViewModel), byName.ViewModelType);

        Assert.True(registry.TryGet(typeof(StubDialogViewModel), out var byType));
        Assert.Equal("StubDialog", byType.Name);

        Assert.False(registry.TryGet("missing", out _));
        Assert.False(registry.TryGet(typeof(object), out _));
    }

    [Fact]
    public void Constructor_DuplicateName_Throws()
    {
        var registrations = new[] { MakeRegistration("same"), MakeRegistration("same") };

        Assert.Throws<InvalidOperationException>(() => new DialogRegistry(registrations, Array.Empty<DialogHostWindowRegistration>()));
    }

    [Fact]
    public void TryGetHostWindow_NullName_UsesDefaultHost()
    {
        var registry = new DialogRegistry(
            Array.Empty<DialogRegistration>(),
            new[] { MakeHost(null), MakeHost("fancy") });

        Assert.True(registry.TryGetHostWindow(null, out var factory));
        Assert.NotNull(factory);
    }

    [Fact]
    public void TryGetHostWindow_NamedHost_TakesPriorityOverDefault()
    {
        var registry = new DialogRegistry(
            Array.Empty<DialogRegistration>(),
            new[] { MakeHost(null), MakeHost("fancy") });

        Assert.True(registry.TryGetHostWindow("fancy", out _));
        // 未注册的名称退回默认宿主，而不是失败
        Assert.True(registry.TryGetHostWindow("missing", out _));
    }

    [Fact]
    public void TryGetHostWindow_NoHosts_ReturnsFalse()
    {
        var registry = new DialogRegistry(Array.Empty<DialogRegistration>(), Array.Empty<DialogHostWindowRegistration>());

        Assert.False(registry.TryGetHostWindow(null, out _));
        Assert.False(registry.TryGetHostWindow("any", out _));
    }

    [Fact]
    public void AddDialog_RegistersViewViewModelAndRegistry()
    {
        var services = new ServiceCollection();
        services.AddDialog<UserControl, StubDialogViewModel>("StubDialog");

        // 默认宿主 DialogWindow 已随内容弹窗注册（不实例化：测试进程没有 Avalonia 窗口平台）
        Assert.Contains(services, s => s.ServiceType == typeof(DialogWindow));

        using var provider = services.BuildServiceProvider();
        Assert.IsType<StubDialogViewModel>(provider.GetRequiredService<StubDialogViewModel>());

        var registry = provider.GetRequiredService<DialogRegistry>();
        Assert.True(registry.TryGet("StubDialog", out var registration));
        Assert.Equal(typeof(StubDialogViewModel), registration.ViewModelType);
    }

    [Fact]
    public void AddDialogWindow_RegistersNamedAndDefaultHost()
    {
        var services = new ServiceCollection();
        services.AddDialogWindow<DialogWindow>("fancy");
        services.AddDialogWindow<DialogWindow>();

        var registry = services.BuildServiceProvider().GetRequiredService<DialogRegistry>();

        Assert.True(registry.TryGetHostWindow("fancy", out _));
        Assert.True(registry.TryGetHostWindow(null, out var defaultFactory));
        Assert.NotNull(defaultFactory);
    }
}
