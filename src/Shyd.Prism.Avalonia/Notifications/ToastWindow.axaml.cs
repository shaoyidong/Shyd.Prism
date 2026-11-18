using Avalonia.Controls;

namespace Shyd.Prism.Notifications;

/// <summary>
/// 框架内置的通知卡片窗口（无边框、置顶、自包含样式）。
/// 继承 <see cref="ToastWindowBase"/>：内容注入与点击行为全部在基类，
/// 本类只剩 XAML 外观——想要自己的主题样式，照这个样子换一套绑定即可。
/// </summary>
public partial class ToastWindow : ToastWindowBase
{
    /// <summary>预览器 / 默认工厂用的无参构造。</summary>
    public ToastWindow()
    {
        InitializeComponent();
    }

    /// <summary>构造时直接注入内容。</summary>
    public ToastWindow(ToastContent content) : base(content)
    {
        InitializeComponent();
    }
}
