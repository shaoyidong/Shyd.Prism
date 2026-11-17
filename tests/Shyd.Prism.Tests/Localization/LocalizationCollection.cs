using Shyd.Prism.Localization;
using Xunit;

namespace Shyd.Prism.Tests.Localization;

/// <summary>
/// 测试用内置语言包（替代宿主应用的嵌入资源：框架本身不内置任何翻译内容）。
/// </summary>
public static class TestLanguagePacks
{
    /// <summary>覆盖 LocTests / LanguagePackLoaderTests 用到的全部文案 key。</summary>
    public static IReadOnlyList<BuiltInLanguagePack> BuiltIn { get; } =
    [
        new("en-US.json", """
            {"code":"en-US","displayName":"English","translations":{
              "发送":"Send",
              "取消":"Cancel",
              "复制":"Copy",
              "发送文件（可多选）":"Send files (multi-select)",
              "正在发送 ({0}/{1}): {2}":"Sending ({0}/{1}): {2}",
              "正在传输: {0} ({1:F1}%)":"Transferring: {0} ({1:F1}%)"
            }}
            """),
    ];
}

/// <summary>Localization 测试集：串行执行（Loc 是进程级静态单例）并共享预载好的语言包状态。</summary>
[CollectionDefinition("Localization")]
public sealed class LocalizationCollection : ICollectionFixture<LocalizationFixture>;

/// <summary>一次性临时数据目录 + 测试语言包预载；测试结束后清理。</summary>
public sealed class LocalizationFixture : IDisposable
{
    private readonly string _dataDirectory =
        Path.Combine(Path.GetTempPath(), "shyd-prism-i18n-" + Guid.NewGuid().ToString("N"));

    public LocalizationFixture()
    {
        Directory.CreateDirectory(_dataDirectory);
        Loc.Initialize(_dataDirectory, TestLanguagePacks.BuiltIn, TestSourceLanguage);
    }

    /// <summary>fixture 声明的源语言（测试 key 为中文，与 TestPacks 的 key 语言一致）。</summary>
    public static SourceLanguage TestSourceLanguage { get; } = new("zh-CN", "简体中文");

    /// <summary>预载数据目录（fixture 存活期间不删除；测试篡改状态后用它恢复）。</summary>
    public string DataDirectory => _dataDirectory;

    public IReadOnlyList<BuiltInLanguagePack> TestPacks => TestLanguagePacks.BuiltIn;

    /// <summary>把 Loc 恢复到 fixture 的预载状态（测试改了目录 / 语言 / 源语言后调用）。</summary>
    public void Reset() => Loc.Initialize(_dataDirectory, TestLanguagePacks.BuiltIn, TestSourceLanguage);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
        catch
        {
            // 临时目录清理失败不影响测试结果
        }
    }
}
