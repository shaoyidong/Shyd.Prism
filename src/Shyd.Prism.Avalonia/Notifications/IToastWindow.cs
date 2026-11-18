using Avalonia;
using Avalonia.Controls;

namespace Shyd.Prism.Notifications;

/// <summary>
/// 通知窗口契约：规定通知窗口必须能显示什么（<see cref="SetContent"/>），
/// 以及 ToastService 驱动它所需的窗口操作（定位、显示、关闭）。
/// <para>
/// 一般不直接实现本接口，而是继承 <see cref="ToastWindowBase"/>：它已实现内容注入
/// （Title / Message / Hint 可绑定属性）与点击行为（点卡片 → OnClick → 延迟关窗），
/// 子类只需要 XAML 设计外观并绑定这些字段。实现本接口则完全自主。
/// </para>
/// </summary>
public interface IToastWindow
{
    /// <summary>
    /// 设置要显示的内容（ToastService 在显示前调用一次）：
    /// <paramref name="content"/> 里的 <c>Title</c> / <c>Message</c> 必须展示，
    /// <c>Hint</c> 可选（无内容时不显示提示行），<c>OnClick</c> 在用户点击通知时触发。
    /// </summary>
    void SetContent(ToastContent content);

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
