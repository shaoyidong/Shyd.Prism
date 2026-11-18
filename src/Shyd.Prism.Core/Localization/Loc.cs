using System;
using System.Collections.Generic;
using System.Threading;

namespace Shyd.Prism.Localization;

/// <summary>
/// 轻量本地化（可扩展多语言包）。
/// <para>
/// **以源语言原文作为 key**：源语言侧不需要任何字典（原样返回），其它语言只需提供一张
/// 「源语言 → 译文」表。源语言由使用者通过 <see cref="Initialize"/> 声明（默认英语）——
/// 中文开发者声明 zh-CN 后写 <c>Loc.Get("发送文件")</c>，英文开发者声明 en 后写
/// <c>Loc.Get("send file")</c>；查不到译文时自动回退源语言原文。
/// </para>
/// <para>
/// 译文存放在「应用数据目录 / i18n / *.json」：改 JSON 文件即可改译文，按同样格式放入新文件
/// 即可新增一套语言，语言下拉框会自动多出一项。首次访问会自动加载（无需显式初始化），
/// AOT / TrimMode=full 下也安全（只用 JsonDocument 与文件 IO，不用反射序列化）。
/// </para>
/// </summary>
public static class Loc
{
    /// <summary>源语言声明（未调用 <see cref="Initialize"/> 前的默认值为英语；由 Initialize 覆盖）。</summary>
    private static SourceLanguage _source = new("en", "English");

    private static readonly List<ILanguagePack> _packs = new();
    private static readonly Dictionary<string, ILanguagePack> _byCode =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock _initGate = new();
    private static bool _filePacksLoaded;
    private static ILanguagePack _current = null!;

    static Loc()
    {
        // 注册声明的源语言（Map 为空：key 即源语言文本，无需翻译表）；
        // 其它语言由 i18n/*.json 或宿主注入的种子在启动时动态注册
        Register(new SourceLanguagePack(_source.Code, _source.DisplayName));
        _current = _byCode[_source.Code];
    }

    /// <summary>声明的源语言代码（key 语言；未注册语言的回退目标）。</summary>
    public static string SourceCode => _source.Code;

    /// <summary>
    /// 从外部语言包文件初始化。新增语言只需在「数据目录 / i18n」放入一个符合格式的 JSON 文件，
    /// 不需要改代码、也不需要重新编译（AOT 发布后同样适用）。
    /// <para><paramref name="source"/>：声明源语言（key 语言 + 下拉框展示名）。
    /// 英文开发者用 <c>new SourceLanguage("en-US", "English (US)")</c>，之后代码里直接写
    /// <c>loc:Tr 'send file'</c>，后续为其它语言补翻译 JSON 即可。</para>
    /// <para>与懒加载 <see cref="EnsurePacksLoaded"/> 共用 <see cref="_initGate"/> 互斥：
    /// 否则并发场景下懒初始化会在显式 Initialize 之后把加载目录指回默认数据目录，
    /// 后续 ReloadPacks 读错目录（测试套件并行时实际发生过）。</para>
    /// <para><paramref name="builtInPacks"/>：宿主应用自带的内置语言包种子（通常来自宿主自己的嵌入资源），框架不内置任何翻译内容。</para>
    /// </summary>
    public static void Initialize(
        string dataDirectory,
        IEnumerable<BuiltInLanguagePack>? builtInPacks = null,
        SourceLanguage? source = null)
    {
        lock (_initGate)
        {
            if (source is not null)
            {
                SwitchSource(source);
            }

            LanguagePackLoader.Initialize(dataDirectory, builtInPacks);
            _filePacksLoaded = true;
        }
    }

    /// <summary>替换声明的源语言；若当前语言正是旧源语言，同步切到新源语言包。</summary>
    private static void SwitchSource(SourceLanguage source)
    {
        var previousSourceCode = _source.Code;
        _source = source;

        // 静态构造按框架默认源语言（en/English）预注册了一个空 Map 占位包，仅供
        // "未显式声明源语言就调用 Loc.Get" 时兜底。宿主一旦声明了不同的源语言
        // （如 zh-CN），这个占位包就失去意义；若留在 _packs 里，语言下拉框会凭空
        // 多出一个空的 English，与真实的 en-US 语言包重复。
        // 只移除确实是框架占位（SourceLanguagePack）的旧包，不动文件/宿主注入的真实包。
        if (!string.Equals(previousSourceCode, source.Code, StringComparison.OrdinalIgnoreCase) &&
            _byCode.TryGetValue(previousSourceCode, out var previousPack) &&
            previousPack is SourceLanguagePack)
        {
            _packs.Remove(previousPack);
            _byCode.Remove(previousSourceCode);
        }

        Register(new SourceLanguagePack(source.Code, source.DisplayName));

        if (string.Equals(_current.Code, previousSourceCode, StringComparison.OrdinalIgnoreCase))
        {
            _current = _byCode[_source.Code];
        }
    }

    /// <summary>
    /// 懒加载语言包文件（未显式调用 Initialize 的场景也能正常工作，兜底目录为程序基目录、无内置种子）。
    /// <para>只扫描已存在的语言包文件，不创建目录：一次普通的 <see cref="Get"/>/<see cref="Packs"/>
    /// 访问绝不该在应用根目录凭空留下一个空 i18n 文件夹（宿主真正的数据目录由 Initialize 指定）。</para>
    /// </summary>
    private static void EnsurePacksLoaded()
    {
        if (_filePacksLoaded)
        {
            return;
        }

        lock (_initGate)
        {
            if (_filePacksLoaded)
            {
                return;
            }

            LanguagePackLoader.DiscoverExisting(AppContext.BaseDirectory);
            _filePacksLoaded = true;
        }
    }

    /// <summary>重新读取语言包文件（发布后直接改文件 / 新增语言文件后可即时生效）。</summary>
    public static int ReloadPacks()
    {
        EnsurePacksLoaded();
        var loaded = LanguagePackLoader.Reload();

        // 当前语言包可能已被替换，回退到声明的源语言并通知界面重建
        if (!_byCode.ContainsKey(_current.Code))
        {
            _current = _byCode[_source.Code];
        }
        LanguageChanged?.Invoke();

        return loaded;
    }

    /// <summary>已注册的语言包（按注册顺序，用于生成语言下拉框）。</summary>
    public static IReadOnlyList<ILanguagePack> Packs
    {
        get
        {
            EnsurePacksLoaded();
            return _packs.AsReadOnly();
        }
    }

    /// <summary>当前语言的 BCP-47 代码。</summary>
    public static string LanguageCode => _current.Code;

    /// <summary>当前语言（返回 BCP-47 代码；保留该属性名以兼容既有调用点/测试）。</summary>
    public static string Language => _current.Code;

    /// <summary>语言切换事件（切换后需要重建界面才能生效）。</summary>
    public static event Action? LanguageChanged;

    /// <summary>注册（或替换）一个语言包。</summary>
    public static void Register(ILanguagePack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        if (_byCode.TryGetValue(pack.Code, out var existing))
        {
            var index = _packs.IndexOf(existing);
            _packs[index] = pack;
            _byCode[pack.Code] = pack;
            if (ReferenceEquals(_current, existing))
            {
                _current = pack;
            }
        }
        else
        {
            _packs.Add(pack);
            _byCode.Add(pack.Code, pack);
        }
    }

    /// <summary>按 BCP-47 code 切换语言；未注册的 code 回退到声明的源语言。</summary>
    public static void SetLanguage(string code)
    {
        EnsurePacksLoaded();

        var normalized = !string.IsNullOrWhiteSpace(code) && _byCode.ContainsKey(code)
            ? code
            : _source.Code;

        if (string.Equals(_current.Code, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _current = _byCode[normalized];
        LanguageChanged?.Invoke();
    }

    /// <summary>
    /// 取译文：当前语言包存在映射时返回译文，否则返回源语言原文（即 key 本身）。
    /// </summary>
    public static string Get(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        EnsurePacksLoaded();
        return _current.Map.TryGetValue(key, out var translated) ? translated : key;
    }

    /// <summary>
    /// 带参数的译文：模板里用 <c>{0}</c>、<c>{1}</c> 占位。
    /// </summary>
    public static string F(string template, params object?[] args)
    {
        var translated = Get(template);

        try
        {
            return args.Length == 0 ? translated : string.Format(translated, args);
        }
        catch (FormatException)
        {
            // 译文模板占位符与参数不匹配时，退回原始拼接，避免界面崩溃
            return template;
        }
    }
}
