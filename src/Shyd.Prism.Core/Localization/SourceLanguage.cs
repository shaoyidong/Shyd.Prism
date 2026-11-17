using System.Collections.Generic;

namespace Shyd.Prism.Localization;

/// <summary>
/// 源语言声明：开发时写 <c>loc:Tr</c> / <c>Loc.Get</c> 的 key 所用的那种语言。
/// <para>
/// 例如英文开发者声明 <c>new SourceLanguage("en-US", "English (US)")</c> 后，
/// 代码里可以直接写 <c>loc:Tr 'send file'</c>——key 就是源语言文本，
/// <see cref="Loc.Get"/> 对未翻译的 key 原样返回；后续为其它语言补 JSON 翻译文件即可。
/// </para>
/// </summary>
/// <param name="Code">源语言的 BCP-47 代码（未注册语言回退到它）。</param>
/// <param name="DisplayName">语言下拉框里的展示名称。</param>
public sealed record SourceLanguage(string Code, string DisplayName);

/// <summary>源语言包：Map 为空（key 即源语言文本，无需翻译表）。</summary>
internal sealed class SourceLanguagePack(string code, string displayName) : ILanguagePack
{
    public string Code { get; } = code;

    public string DisplayName { get; } = displayName;

    public IReadOnlyDictionary<string, string> Map { get; } = new Dictionary<string, string>();
}
