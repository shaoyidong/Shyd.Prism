using Shyd.Prism.Localization;
using System.Text.Json;
using Xunit;

namespace Shyd.Prism.Tests.Localization;

/// <summary>验证发布后可直接修改语言文件改译文、按格式新增文件增加语言。</summary>
[Collection("Localization")]
public class LanguagePackLoaderTests
{
    private readonly LocalizationFixture _fixture;

    public LanguagePackLoaderTests(LocalizationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void BuiltInPack_NewKeysAreMerged_WithoutOverwritingUserEdits()
    {
        var dir = Path.Combine(Path.GetTempPath(), "shyd-prism-i18n-verify-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var packDir = Path.Combine(dir, "i18n");
        var previous = Loc.Language;

        try
        {
            Directory.CreateDirectory(packDir);
            // 模拟老版本遗留的语言包：用户改过译文，且没有新版本新增的 key
            File.WriteAllText(Path.Combine(packDir, "en-US.json"),
                "{\"code\":\"en-US\",\"displayName\":\"English\",\"translations\":{\"发送\":\"SendEdited\"}}");

            Loc.Initialize(dir, _fixture.TestPacks);

            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(packDir, "en-US.json")));
            var translations = document.RootElement.GetProperty("translations");
            Assert.Equal("SendEdited", translations.GetProperty("发送").GetString());
            Assert.Equal("Copy", translations.GetProperty("复制").GetString());

            Loc.SetLanguage("en-US");
            Assert.Equal("SendEdited", Loc.Get("发送"));
            Assert.Equal("Copy", Loc.Get("复制"));
        }
        finally
        {
            _fixture.Reset();
            Loc.SetLanguage(previous);
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ThemePackFiles_AreNotRegisteredAsLanguages()
    {
        var dir = Path.Combine(Path.GetTempPath(), "shyd-prism-i18n-verify-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var packDir = Path.Combine(dir, "i18n");
        var previous = Loc.Language;

        try
        {
            Loc.Initialize(dir, _fixture.TestPacks);

            // 主题配色包误落进 i18n 目录（种子来源 marker 过宽就会这样）：
            // 不能被注册成语言
            File.WriteAllText(Path.Combine(packDir, "win7.json"),
                "{\"id\":\"win7\",\"name\":\"Windows 7\",\"base\":\"win7\",\"colors\":{\"FsWindowBrush\":\"#F0F0F0\"}}");
            File.WriteAllText(Path.Combine(packDir, "not-a-language.json"),
                "{\"hello\":\"world\"}");

            Loc.ReloadPacks();

            Assert.DoesNotContain(Loc.Packs, p => p.Code == "win7");
            Assert.DoesNotContain(Loc.Packs, p => p.Code == "not-a-language");
        }
        finally
        {
            _fixture.Reset();
            Loc.SetLanguage(previous);
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ExternalLanguageFiles_CanBeModifiedAndAdded()
    {
        var dir = Path.Combine(Path.GetTempPath(), "shyd-prism-i18n-verify-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var packDir = Path.Combine(dir, "i18n");
        var previous = Loc.Language;

        try
        {
            Loc.Initialize(dir, _fixture.TestPacks);
            Assert.True(File.Exists(Path.Combine(packDir, "en-US.json")), "注入的内置英文包应被复制到数据目录");

            Loc.SetLanguage("en-US");
            Assert.Equal("Send", Loc.Get("发送"));

            // 1) 直接改文件里的译文 → 重载后生效
            var enPath = Path.Combine(packDir, "en-US.json");
            using (var document = JsonDocument.Parse(File.ReadAllText(enPath)))
            {
                var root = document.RootElement;
                var map = new Dictionary<string, string>();
                foreach (var p in root.GetProperty("translations").EnumerateObject())
                {
                    map[p.Name] = p.Value.GetString() ?? string.Empty;
                }
                map["发送"] = "SendEdited";
                File.WriteAllText(enPath, JsonSerializer.Serialize(new
                {
                    code = "en-US",
                    displayName = "English",
                    translations = map
                }));
            }

            Loc.ReloadPacks();
            Assert.Equal("SendEdited", Loc.Get("发送"));

            // 2) 按固定格式新增一个语言文件 → 语言列表自动多一项
            File.WriteAllText(Path.Combine(packDir, "fr-FR.json"),
                "{\"code\":\"fr-FR\",\"displayName\":\"Français\",\"translations\":{\"发送\":\"Envoyer\"}}");
            Loc.ReloadPacks();

            Assert.Contains(Loc.Packs, p => p.Code == "fr-FR");
            Loc.SetLanguage("fr-FR");
            Assert.Equal("Envoyer", Loc.Get("发送"));
        }
        finally
        {
            _fixture.Reset();
            Loc.SetLanguage(previous);
            Directory.Delete(dir, recursive: true);
        }
    }
}
