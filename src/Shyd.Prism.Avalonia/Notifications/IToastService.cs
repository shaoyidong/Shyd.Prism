using System;

namespace Shyd.Prism.Notifications;

/// <summary>一条通知的内容：标题 / 正文 / 提示行文案 / 点击回调。</summary>
/// <param name="Title">标题。</param>
/// <param name="Message">正文。</param>
/// <param name="Hint">卡片底部提示行文案；null 时不显示提示行（文案由调用方决定，可用 i18n 翻译）。</param>
/// <param name="OnClick">点击卡片的回调（一般用来把主窗口唤到前面）。</param>
public sealed record ToastContent(string Title, string Message, string? Hint, Action? OnClick);

/// <summary>
/// 轻量「系统通知」：在工作区右下角弹出一个自绘小卡片，几秒后自动消失，点击可回到主窗口。
/// <para>不依赖平台 Toast API（Windows/macOS/Linux 差异大且部分环境不可用），
/// 因此跨平台行为一致。通知窗口可实现 <see cref="IToastWindow"/> 契约自定义（默认用
/// 框架自带的 <see cref="ToastWindow"/>）。</para>
/// </summary>
public interface IToastService
{
    /// <summary>显示一条通知（新的会替换旧的）。</summary>
    /// <param name="title">标题。</param>
    /// <param name="message">正文。</param>
    /// <param name="onClick">点击卡片的回调（一般用来把主窗口唤到前面）。</param>
    /// <param name="hint">
    /// 卡片底部的提示行文案（如「点击查看（6 秒后自动关闭）」）；null 时不显示提示行。
    /// 文案由调用方决定（可用 i18n 翻译），框架不内置任何语言。
    /// </param>
    void Show(string title, string message, Action? onClick = null, string? hint = null);
}
