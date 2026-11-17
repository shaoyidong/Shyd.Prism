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
    private bool _clickHandled;

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

        // 用 Released（而不是 Pressed）判定点击：回调常会激活主窗口 / 弹出新窗口，
        // 在 Pressed 阶段做这些会打断当前手势，表现为「点一次没反应、要点两次」。
        PointerReleased += OnPointerReleased;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 一次按下只允许触发一次（多指触摸 / 连续输入不会重复激活主窗口）。
        if (_clickHandled || e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        _clickHandled = true;

        // Close 不能在输入事件处理链内同步执行：TopLevel.PlatformImpl 被置空后，
        // 本次手势余下的原始输入事件会找不到实现，Avalonia 打
        // "PlatformImpl is null, couldn't handle input." 警告。
        // 先执行回调（它自身也会 Post 激活主窗口 / 弹窗），再 Post 关闭卡片，
        // 两者同在 Normal 优先级按入队顺序执行，避免 Background 在 UI 繁忙时被延后。
        _onClick?.Invoke();

        Dispatcher.UIThread.Post(() =>
        {
            if (IsVisible)
            {
                Close();
            }
        });
    }
}
