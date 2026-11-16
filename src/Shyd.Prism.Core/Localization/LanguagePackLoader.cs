using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Shyd.Prism.Localization;

/// <summary>
/// 内置语言包种子：宿主应用自带的语言包内容（通常来自宿主自己的嵌入资源）。
/// <para>框架只提供加载机制，不内置任何翻译内容——内容由使用它的 UI 层决定。</para>
/// </summary>
/// <param name="FileName">数据目录 i18n 下的文件名（如 en-US.json）。</param>
/// <param name="Json">语言包 JSON 全文。</param>
public sealed record BuiltInLanguagePack(string FileName, string Json);

/// <summary>
/// 语言包文件加载器（AOT 安全：只用 <see cref="JsonDocument"/> 与文件 IO，不依赖反射序列化）。
/// <para>
/// 语言包存放于「应用数据目录 / i18n / *.json」，格式：
/// <code>
/// { "code": "en-US", "displayName": "English", "translations": { "中文原文": "English text" } }
/// </code>
/// 初始化时把宿主注入的内置语言包种子复制到数据目录（不覆盖用户文件）；
/// 之后一律以数据目录的文件为准，且不会覆盖用户已修改过的文件。
/// 因此应用发布后可以直接改文件改翻译，或按同一格式放入新文件来新增一套语言。
/// </para>
/// </summary>
public static class LanguagePackLoader
{
    public const string PackFolderName = "i18n";

    private static readonly string TemplateDirectory =
        Path.Combine(AppContext.BaseDirectory, "Localization", "packs");

    private static string _packsDirectory = string.Empty;

    /// <summary>当前语言包目录（数据目录 / i18n）。</summary>
    public static string PacksDirectory => _packsDirectory;

    /// <summary>
    /// 初始化：准备目录 → 种入宿主注入的内置语言包（不覆盖用户文件）→ 加载全部语言包。
    /// </summary>
    /// <param name="dataDirectory">应用数据目录（i18n 语言包文件所在）。</param>
    /// <param name="builtInPacks">宿主自带的内置语言包种子；null 表示宿主无内置包（仅使用数据目录里已有的文件）。</param>
    public static void Initialize(string dataDirectory, IEnumerable<BuiltInLanguagePack>? builtInPacks = null)
    {
        _packsDirectory = Path.Combine(
            string.IsNullOrWhiteSpace(dataDirectory) ? AppContext.BaseDirectory : dataDirectory,
            PackFolderName);

        Directory.CreateDirectory(_packsDirectory);
        SeedBuiltInPacks(builtInPacks);
        Reload();
    }

    /// <summary>把宿主注入的内置语言包种入数据目录（已存在的用户文件不覆盖，只补缺失 key）。</summary>
    private static void SeedBuiltInPacks(IEnumerable<BuiltInLanguagePack>? builtInPacks)
    {
        // 宿主注入的种子（通常来自宿主应用的嵌入资源）
        if (builtInPacks is not null)
        {
            foreach (var pack in builtInPacks)
            {
                if (string.IsNullOrWhiteSpace(pack.FileName) || string.IsNullOrEmpty(pack.Json))
                {
                    continue;
                }

                // 种子文件名不允许带路径分隔符，防止写到数据目录之外
                var fileName = Path.GetFileName(pack.FileName);
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                SeedPackFile(Path.Combine(_packsDirectory, fileName), pack.Json);
            }
        }

        // 兼容直接从输出目录 Localization/packs 复制（普通发布形态）
        try
        {
            if (!Directory.Exists(TemplateDirectory))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(TemplateDirectory, "*.json"))
            {
                SeedPackFile(Path.Combine(_packsDirectory, Path.GetFileName(file)), File.ReadAllText(file));
            }
        }
        catch (Exception)
        {
            // 复制失败时仍允许直接读取数据目录里已有的语言包
        }
    }

    /// <summary>种入单个语言包：目标不存在则写入，已存在则只合并缺失的译文 key（不覆盖用户改动）。</summary>
    private static void SeedPackFile(string target, string json)
    {
        try
        {
            if (!File.Exists(target))
            {
                File.WriteAllText(target, json);
            }
            else
            {
                // 版本升级后内置包新增了译文 key：只补齐缺失项，不覆盖用户改过的译文
                SeedInto(target, json);
            }
        }
        catch (Exception)
        {
            // 单个种子写入失败不影响其它语言包
        }
    }

    /// <summary>
    /// 把内置包里「本地文件缺少」的译文合并进去（保留用户已有译文与自定义字段）。
    /// 这样发布新版本后新增的界面文案也能自动获得翻译，不必要求用户删掉旧语言包。
    /// <para>AOT 安全：只用 JsonDocument + Utf8JsonWriter，不用反射序列化。</para>
    /// </summary>
    private static void SeedInto(string target, string builtInJson)
    {
        try
        {
            using var builtIn = JsonDocument.Parse(builtInJson);
            using var local = JsonDocument.Parse(File.ReadAllText(target));
            if (!builtIn.RootElement.TryGetProperty("translations", out var builtInTranslations) ||
                builtInTranslations.ValueKind != JsonValueKind.Object ||
                !local.RootElement.TryGetProperty("translations", out var localTranslations) ||
                localTranslations.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            var merged = new Dictionary<string, string>();
            foreach (var property in localTranslations.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    merged[property.Name] = property.Value.GetString() ?? string.Empty;
                }
            }

            var changed = false;
            foreach (var property in builtInTranslations.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String && !merged.ContainsKey(property.Name))
                {
                    merged[property.Name] = property.Value.GetString() ?? string.Empty;
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                foreach (var property in local.RootElement.EnumerateObject())
                {
                    if (!property.NameEquals("translations"))
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WritePropertyName("translations");
                writer.WriteStartObject();
                foreach (var (key, value) in merged)
                {
                    writer.WriteString(key, value);
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            File.WriteAllText(target, Encoding.UTF8.GetString(buffer.ToArray()));
        }
        catch (Exception)
        {
            // 本地文件损坏 / 不可写时保持原样，Reload 会自行跳过
        }
    }

    /// <summary>重新扫描语言包文件并注册；返回成功加载的文件数。</summary>
    public static int Reload()
    {
        if (string.IsNullOrEmpty(_packsDirectory) || !Directory.Exists(_packsDirectory))
        {
            return 0;
        }

        var loaded = 0;
        foreach (var file in Directory.EnumerateFiles(_packsDirectory, "*.json"))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                // 严格格式校验：必须是带 translations 字典的语言包。
                // 主题配色包（colors / base）以及数据目录里残留的其它 JSON 不该混进语言列表。
                if (root.TryGetProperty("colors", out _) || root.TryGetProperty("base", out _) ||
                    !root.TryGetProperty("translations", out var translations) ||
                    translations.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var code = ReadString(root, "code") ?? Path.GetFileNameWithoutExtension(file);
                var displayName = ReadString(root, "displayName") ?? code;
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                var map = new Dictionary<string, string>();
                foreach (var property in translations.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        map[property.Name] = property.Value.GetString() ?? string.Empty;
                    }
                }

                Loc.Register(new JsonLanguagePack(code, displayName, map));
                loaded++;
            }
            catch (Exception)
            {
                // 单个损坏 / 格式不符的文件不影响其它语言包
            }
        }

        return loaded;
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
