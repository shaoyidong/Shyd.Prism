using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace Shyd.Prism.Notifications;

/// <summary>
/// 通知窗口基类：替子类实现全部「非外观」逻辑——
/// <see cref="SetContent"/> 内容注入（可绑定属性）、整卡点击触发 OnClick 并延迟关窗
/// （规避输入事件链内同步 Close 的 PlatformImpl null 警告）、<see cref="IToastWindow"/> 驱动成员。
/// <para>
/// 子类只负责界面外观：XAML 里绑定 <see cref="ToastTitle"/> / <see cref="ToastMessage"/> /
/// <see cref="ToastHint"/>（提示行用 <see cref="HasHint"/> 控制可见性），并可随意绑定宿主
/// 主题资源 / 皮肤样式类；code-behind 通常只剩构造函数。
/// </para>
/// </summary>
public abstract class ToastWindowBase : Window, IToastWindow
{
    /// <summary>规定显示内容之一：通知标题。</summary>
    public static readonly StyledProperty<string> ToastTitleProperty =
        AvaloniaProperty.Register<ToastWindowBase, string>(nameof(ToastTitle), string.Empty);

    /// <summary>规定显示内容之一：通知正文。</summary>
    public static readonly StyledProperty<string> ToastMessageProperty =
        AvaloniaProperty.Register<ToastWindowBase, string>(nameof(ToastMessage), string.Empty);

    /// <summary>规定显示内容之一：提示行文案（可选项，由 <see cref="HasHint"/> 控制可见性）。</summary>
    public static readonly StyledProperty<string?> ToastHintProperty =
        AvaloniaProperty.Register<ToastWindowBase, string?>(nameof(ToastHint));

    /// <summary>是否有提示行内容（绑定到提示行 IsVisible）。</summary>
    public static readonly StyledProperty<bool> HasHintProperty =
        AvaloniaProperty.Register<ToastWindowBase, bool>(nameof(HasHint));

    /// <summary>规定显示内容之一：通知标题。</summary>
    public string ToastTitle
    {
        get => GetValue(ToastTitleProperty);
        set => SetValue(ToastTitleProperty, value);
    }

    /// <summary>规定显示内容之一：通知正文。</summary>
    public string ToastMessage
    {
        get => GetValue(ToastMessageProperty);
        set => SetValue(ToastMessageProperty, value);
    }

    /// <summary>规定显示内容之一：提示行文案（null / 空 = 不显示提示行）。</summary>
    public string? ToastHint
    {
        get => GetValue(ToastHintProperty);
        set => SetValue(ToastHintProperty, value);
    }

    /// <summary>提示行是否可见（HasHint 内容非空时为 true）。</summary>
    public bool HasHint
    {
        get => GetValue(HasHintProperty);
        set => SetValue(HasHintProperty, value);
    }

    private Action? _onClick;
    private bool _clickHandled;

    /// <summary>预览器 / 工厂用的无参构造。</summary>
    protected ToastWindowBase()
    {
        // 子类的 XAML 用 {Binding ToastTitle}/{Binding ToastMessage}/{Binding ToastHint}
        // 绑定本类规定的内容属性；Window 的 DataContext 默认是 null，不设为自身的话
        // 这些绑定全部取不到值，表现就是「通知卡片弹出来了但内容一片空白」。
        // 在基类构造里设好，框架内置 ToastWindow 与所有自定义子类一并生效。
        DataContext = this;

        // 整卡点击 → OnClick → 延迟关窗（时序规避见 OnPointerReleased）
        PointerReleased += OnPointerReleased;
    }

    /// <summary>工厂 / 预览器用：构造时直接注入内容。</summary>
    protected ToastWindowBase(ToastContent content) : this()
    {
        SetContent(content);
    }

    /// <inheritdoc />
    public void SetContent(ToastContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        ToastTitle = content.Title;
        ToastMessage = content.Message;
        ToastHint = content.Hint;
        HasHint = !string.IsNullOrEmpty(content.Hint);
        _onClick = content.OnClick;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 一次按下只允许触发一次（多指触摸 / 连续输入不会重复激活）。
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
