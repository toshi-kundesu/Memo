using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Toshi.Memo.Editor
{
    public sealed class MemoEditorWindow : EditorWindow
    {
        const string PackageName = "com.toshi.memo";
        const string DefaultEmoji = "📝";
        const string DefaultTopics = "unity,zenn,memo";
        const int PreviewPort = 8003;

        string articleTitle = "Unityの検証メモ";
        string slug = "";
        string emoji = DefaultEmoji;
        string topics = DefaultTopics;
        bool published;
        Vector2 markdownScrollPosition;
        GUIStyle markdownH1Style;
        GUIStyle markdownH2Style;
        GUIStyle markdownH3Style;
        GUIStyle markdownBodyStyle;
        GUIStyle markdownCodeStyle;

        [MenuItem("toshi/VLiveKit/Project/Zenn Window")]
        static void OpenWindow()
        {
            var window = GetWindow<MemoEditorWindow>("Zenn Window");
            window.minSize = new Vector2(520, 520);
            window.slug = CreateSlug(window.articleTitle);
        }

        static void OpenPreview()
        {
            Application.OpenURL($"http://localhost:{PreviewPort}");
        }

        static void OpenCurrentArticlePreview()
        {
            var path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : "";
            if (!TryGetArticleSlug(path, out var articleSlug))
            {
                EditorUtility.DisplayDialog("Memo", "articles/*.md を選択してから実行してください。", "OK");
                return;
            }

            Application.OpenURL($"http://localhost:{PreviewPort}/articles/{articleSlug}");
        }

        static void OpenMemoFolder()
        {
            EditorUtility.RevealInFinder(GetPackageRoot());
        }

        static void OpenZennDashboard()
        {
            Application.OpenURL("https://zenn.dev/dashboard");
        }

        static void StartPreview()
        {
            var root = GetPackageRoot();
            if (!Directory.Exists(root))
            {
                EditorUtility.DisplayDialog("Memo", $"Memo package が見つかりません: {root}", "OK");
                return;
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var npmCache = Path.Combine(projectRoot, ".npm-cache");
            Directory.CreateDirectory(npmCache);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c npx --yes --package zenn-cli zenn preview --port {PreviewPort}",
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.EnvironmentVariables["npm_config_cache"] = npmCache;

            try
            {
                Process.Start(startInfo);
                Debug.Log($"Zenn preview starting: http://localhost:{PreviewPort}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Memo", $"Zenn preview の起動に失敗しました。\n{ex.Message}", "OK");
            }
        }

        void OnGUI()
        {
            EnsureMarkdownStyles();
            DrawPreviewControls();
            DrawMarkdownPreview();
        }

        void DrawPreviewControls()
        {
            EditorGUILayout.LabelField("Zenn Window", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Start the local Zenn preview server, open the browser preview, or inspect the selected Markdown article inside Unity.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Start Zenn Preview", GUILayout.Height(30)))
                {
                    StartPreview();
                }

                if (GUILayout.Button("Open Preview", GUILayout.Height(30)))
                {
                    OpenPreview();
                }

                if (GUILayout.Button("Open Current Article", GUILayout.Height(30)))
                {
                    OpenCurrentArticlePreview();
                }
            }

            EditorGUILayout.Space(8);
        }

        void DrawMarkdownPreview()
        {
            EditorGUILayout.LabelField("Markdown Preview", EditorStyles.boldLabel);

            var assetPath = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : "";
            if (!TryGetArticleSlug(assetPath, out var articleSlug))
            {
                EditorGUILayout.HelpBox("Select a Markdown file under Packages/Memo/articles to preview it here.", MessageType.None);
                return;
            }

            var fullPath = ToProjectPath(assetPath);
            if (!File.Exists(fullPath))
            {
                EditorGUILayout.HelpBox("Selected article file was not found.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Article", articleSlug, EditorStyles.miniLabel);
            markdownScrollPosition = EditorGUILayout.BeginScrollView(markdownScrollPosition);
            DrawMarkdown(File.ReadAllText(fullPath, Encoding.UTF8));
            EditorGUILayout.EndScrollView();
        }

        void DrawMarkdown(string markdown)
        {
            var inCodeBlock = false;
            var lines = StripFrontMatter(markdown).Replace("\r\n", "\n").Split('\n');

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    inCodeBlock = !inCodeBlock;
                    continue;
                }

                if (inCodeBlock)
                {
                    EditorGUILayout.LabelField(line, markdownCodeStyle);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    EditorGUILayout.Space(6);
                    continue;
                }

                if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    EditorGUILayout.LabelField(line.Substring(4), markdownH3Style);
                }
                else if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    EditorGUILayout.LabelField(line.Substring(3), markdownH2Style);
                }
                else if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    EditorGUILayout.LabelField(line.Substring(2), markdownH1Style);
                }
                else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
                {
                    EditorGUILayout.LabelField("- " + line.Substring(2), markdownBodyStyle);
                }
                else
                {
                    EditorGUILayout.LabelField(line, markdownBodyStyle);
                }
            }
        }

        void EnsureMarkdownStyles()
        {
            if (markdownBodyStyle != null)
            {
                return;
            }

            markdownBodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            markdownH1Style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, wordWrap = true };
            markdownH2Style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, wordWrap = true };
            markdownH3Style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, wordWrap = true };
            markdownCodeStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        }

        static string StripFrontMatter(string markdown)
        {
            if (string.IsNullOrEmpty(markdown) || !markdown.StartsWith("---", StringComparison.Ordinal))
            {
                return markdown ?? "";
            }

            var normalized = markdown.Replace("\r\n", "\n");
            var endIndex = normalized.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (endIndex < 0)
            {
                return markdown;
            }

            var contentStart = normalized.IndexOf('\n', endIndex + 4);
            return contentStart < 0 ? "" : normalized.Substring(contentStart + 1);
        }

        static string ToProjectPath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        void DrawArticleCreator()
        {
            EditorGUILayout.LabelField("Create Zenn Article", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Packages/Memo/articles に Zenn 記事を作ります。node_modules は Unity package 内に置かない運用です。", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            articleTitle = EditorGUILayout.TextField("Title", articleTitle);
            if (EditorGUI.EndChangeCheck() && string.IsNullOrWhiteSpace(slug))
            {
                slug = CreateSlug(articleTitle);
            }

            slug = EditorGUILayout.TextField("Slug", slug);
            emoji = EditorGUILayout.TextField("Emoji", emoji);
            topics = EditorGUILayout.TextField("Topics", topics);
            published = EditorGUILayout.Toggle("Published", published);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(articleTitle) || string.IsNullOrWhiteSpace(slug)))
            {
                if (GUILayout.Button("Create Article"))
                {
                    CreateArticle();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Preview"))
                {
                    OpenPreview();
                }

                if (GUILayout.Button("Start Preview"))
                {
                    StartPreview();
                }
            }
        }

        void CreateArticle()
        {
            var articleSlug = CreateSlug(slug);
            if (string.IsNullOrWhiteSpace(articleSlug))
            {
                articleSlug = $"memo-{DateTime.Now:yyyyMMdd-HHmmss}";
            }

            var articlesDir = Path.Combine(GetPackageRoot(), "articles");
            Directory.CreateDirectory(articlesDir);

            var path = Path.Combine(articlesDir, $"{articleSlug}.md");
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Memo", $"すでに記事があります。\n{path}", "OK");
                return;
            }

            File.WriteAllText(path, BuildArticle(articleSlug), new UTF8Encoding(false));
            AssetDatabase.Refresh();

            var assetPath = ToAssetPath(path);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            Selection.activeObject = asset;

            Debug.Log($"Created Zenn article: {assetPath}");
            Application.OpenURL($"http://localhost:{PreviewPort}/articles/{articleSlug}");
        }

        string BuildArticle(string articleSlug)
        {
            var normalizedTopics = string.Join(", ",
                topics.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => $"\"{x}\""));

            if (string.IsNullOrWhiteSpace(normalizedTopics))
            {
                normalizedTopics = "\"unity\", \"zenn\"";
            }

            return $@"---
title: ""{EscapeYamlString(articleTitle)}""
emoji: ""{EscapeYamlString(string.IsNullOrWhiteSpace(emoji) ? DefaultEmoji : emoji)}""
type: ""tech""
topics: [{normalizedTopics}]
published: {published.ToString().ToLowerInvariant()}
---

ここにメモを書きます。

---

最終更新: {DateTime.Now:yyyy-MM-dd}
";
        }

        static bool TryGetArticleSlug(string assetPath, out string articleSlug)
        {
            articleSlug = "";
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var normalized = assetPath.Replace('\\', '/');
            if (!normalized.Contains("/articles/") || !normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            articleSlug = Path.GetFileNameWithoutExtension(normalized);
            return !string.IsNullOrWhiteSpace(articleSlug);
        }

        static string GetPackageRoot()
        {
            var embeddedPath = Path.GetFullPath("Packages/Memo");
            if (Directory.Exists(embeddedPath))
            {
                return embeddedPath;
            }

            return Path.GetFullPath($"Packages/{PackageName}");
        }

        static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var rootUri = new Uri(AppendDirectorySeparator(projectRoot));
            var fileUri = new Uri(Path.GetFullPath(fullPath));
            var relative = Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString());
            return relative.Replace('\\', '/');
        }

        static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        static string CreateSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            var lower = value.Trim().ToLowerInvariant();
            lower = Regex.Replace(lower, @"\s+", "-");
            lower = Regex.Replace(lower, @"[^a-z0-9\-]+", "-");
            lower = Regex.Replace(lower, @"-+", "-");
            return lower.Trim('-');
        }

        static string EscapeYamlString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
