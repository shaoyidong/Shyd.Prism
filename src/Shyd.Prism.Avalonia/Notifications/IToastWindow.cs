using Avalonia;
using Avalonia.Controls;

namespace Shyd.Prism.Notifications;

/// <summary>
/// 通知窗口契约：<see cref="ToastService"/> 通过这些成员驱动通知窗口（定位、显示、关闭、二次布局校正）。
/// <para>
/// 这些成员都是 Avalonia Window 的标准成员，任何 Window 子类天然满足——
/// 想让通知卡片符合自己的主题时，继承 Window 自绘样式（可绑定宿主资源 / 皮肤类），
/// 并在注册 <see cref="IToastService"/> 时通过 <c>windowFactory</c> 提供自己的窗口：
/// <code>
/// services.AddSingleton&lt;IToastService&gt;(sp =&gt; new ToastService(ownerProvider,
///     content =&gt; new MyThemedToastWindow(content)));
/// </code>
/// 不提供工厂时使用框架内置的 <see cref="ToastWindow"/>（自包含样式，不依赖宿主主题）。
/// </para>
/// </summary>
public interface IToastWindow
{
    /// <summary>屏幕像素位置（ToastService 计算工作区右下角坐标后设置）。</summary>
    PixelPoint Position { get; set; }

    /// <summary>窗口设计宽度（首帧定位用；显示前 Bounds 可能尚未布局）。</summary>
    double Width { get; }

    /// <summary>显示通知窗口。</summary>
    void Show();

    /// <summary>关闭通知窗口（点击回调 / 到时自动关闭时由 ToastService 调用）。</summary>
    void Close();

    /// <summary>窗口当前是否可见。</summary>
    bool IsVisible { get; }

    /// <summary>窗口当前尺寸（布局完成后用于二次校正位置）。</summary>
    Rect Bounds { get; }
}
