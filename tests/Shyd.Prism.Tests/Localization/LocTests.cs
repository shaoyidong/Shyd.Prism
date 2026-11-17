using Shyd.Prism.Localization;
using Xunit;

namespace Shyd.Prism.Tests.Localization;

/// <summary>
/// i18n 机制测试：中英切换、缺失回退、模板格式化、XAML 标记扩展。
/// 语言包由 <see cref="LocalizationFixture"/> 预载（框架不内置翻译内容）。
/// </summary>
[Collection("Localization")]
public class LocTests
{
    private readonly LocalizationFixture _fixture;

    public LocTests(LocalizationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void English_TranslatesKnownKeys()
    {
        var previous = Loc.Language;
        try
        {
            Loc.SetLanguage("en-US");

            Assert.Equal("Send", Loc.Get("发送"));
            Assert.Equal("Cancel", Loc.Get("取消"));
        }
        finally
        {
            Loc.SetLanguage(previous);
        }
    }

    [Fact]
    public void Chinese_ReturnsOriginalText()
    {
        var previous = Loc.Language;
        try
        {
            Loc.SetLanguage("zh-CN");

            Assert.Equal("发送", Loc.Get("发送"));
        }
        finally
        {
            Loc.SetLanguage(previous);
        }
    }

    [Fact]
    public void UnknownKey_FallsBackToChinese()
    {
        var previous = Loc.Language;
        try
        {
            Loc.SetLanguage("en-US");

            Assert.Equal("这条文案没有英文", Loc.Get("这条文案没有英文"));
        }
        finally
        {
            Loc.SetLanguage(previous);
        }
    }

    [Fact]
    public void Format_FillsTemplatePlaceholders()
    {
        var previous = Loc.Language;
        try
        {
            Loc.SetLanguage("zh-CN");
            Assert.Equal("正在发送 (1/3): a.txt", Loc.F("正在发送 ({0}/{1}): {2}", 1, 3, "a.txt"));

            Loc.SetLanguage("en-US");
            Assert.Equal("Sending (1/3): a.txt", Loc.F("正在发送 ({0}/{1}): {2}", 1, 3, "a.txt"));
        }
        finally
        {
            Loc.SetLanguage(previous);
        }
    }

    [Fact]
    public void Format_WithFormatSpecifier_KeepsPrecision()
    {
        var previous = Loc.Language;
        try
        {
            Loc.SetLanguage("en-US");

            var text = Loc.F("正在传输: {0} ({1:F1}%)", "a.bin", 42.34);
            Assert.Equal("Transferring: a.bin (42.3%)", text);
        }
        finally
        {
            Loc.SetLanguage(previous);
        }
    }

    [Fact]
    public void LanguageChanged_FiresOnlyWhenChanged()
    {
        var previous = Loc.Language;
        var count = 0;
        void Handler() => count++;

        Loc.LanguageChanged += Handler;
        try
        {
            Loc.SetLanguage(previous);
            Assert.Equal(0, count);

            Loc.SetLanguage(previous == "zh-CN" ? "en-US" : "zh-CN");
            Assert.Equal(1, count);
        }
        finally
        {
            Loc.LanguageChanged -= Handler;
            Loc.SetLanguage(previous);
        }
    }

    [Fact]
    public void TrExtension_ReturnsLocalizedText()
    {
        var previous = Loc.Language;
        try
        {
            Loc.SetLanguage("en-US");
            var extension = new TrExtension("发送文件（可多选）");

            Assert.Equal("Send files (multi-select)", extension.ProvideValue(null!));
        }
        finally
        {
            Loc.SetLanguage(previous);
        }
    }

    [Fact]
    public void SourceLanguage_IsConfigurable_KeysPassThroughUntranslated()
    {
        // 源语言由使用者声明：英文开发者声明 en 后，key 就是英文文本，
        // 未翻译的 key 原样返回，未注册语言的回退目标也是声明的源语言
        Loc.Initialize(_fixture.DataDirectory, _fixture.TestPacks, new SourceLanguage("en-US", "English (US)"));
        try
        {
            Assert.Equal("en-US", Loc.SourceCode);

            Loc.SetLanguage("not-a-language");
            Assert.Equal("en-US", Loc.Language);

            Assert.Equal("send file", Loc.Get("send file"));
            Assert.Equal("send file (1/2)", Loc.F("send file ({0}/{1})", 1, 2));
        }
        finally
        {
            _fixture.Reset();
        }
    }

    [Fact]
    public void Reinitialize_WithoutPacks_FallsBackToDataDirectoryFiles()
    {
        var previous = Loc.Language;
        var tempDir = Path.Combine(Path.GetTempPath(), "shyd-prism-i18n-noseed-" + Guid.NewGuid().ToString("N"));
        try
        {
            // 无种子初始化：fixture 预载的 en-US.json 已被种进 fixture 数据目录，
            // 无种子重初始化后按数据目录文件为准（fixture 目录的文件仍可读）
            Loc.Initialize(_fixture.DataDirectory);

            Loc.SetLanguage("en-US");
            Assert.Equal("Send", Loc.Get("发送"));
        }
        finally
        {
            Loc.SetLanguage(previous);
            _fixture.Reset();
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
