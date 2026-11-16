using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;

namespace Shyd.Prism.Notifications;

/// <summary>
/// 通知卡片窗口（无边框、置顶、点击回调）。
/// 提示行文案由调用方传入（可经 i18n 翻译），框架不内置语言。
/// </summary>
public partial class ToastWindow : Window
{
    private readonly Action? _onClick;

    public ToastWindow()
        : this("Toast", string.Empty, null, null)
    {
    }

    public ToastWindow(string title, string message, string? hint, Action? onClick)
    {
        InitializeComponent();

        _onClick = onClick;
        TitleText.Text = title;
        MessageText.Text = message;

        if (!string.IsNullOrEmpty(hint))
        {
            HintText.Text = hint;
            HintText.IsVisible = true;
        }

        PointerPressed += OnPointerPressed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _onClick?.Invoke();

        // 与 DialogService 同套路：Close 不能在输入事件处理链内同步执行，
        // 否则 TopLevel.PlatformImpl 被置空后，本次手势余下的输入事件（指针抬起等）
        // 会打 "PlatformImpl is null, couldn't handle input." 警告。
        // Background 优先级排在待处理 Input 之后：先排空输入再关窗。
        Dispatcher.UIThread.Post(() =>
        {
            if (IsVisible)
            {
                Close();
            }
        }, DispatcherPriority.Background);
    }
}
