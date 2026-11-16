using System;

namespace Shyd.Prism.Notifications;

/// <summary>
/// 轻量「系统通知」：在工作区右下角弹出一个自绘小卡片，几秒后自动消失，点击可回到主窗口。
/// <para>不依赖平台 Toast API（Windows/macOS/Linux 差异大且部分环境不可用），
/// 因此跨平台行为一致。</para>
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
