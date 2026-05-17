---
title: "Unity AI Assistant MCPでシーンを触るまでの導入メモ"
emoji: "🧭"
type: "tech"
topics: ["unity", "mcp", "aiassistant", "editor", "tooling"]
published: false
---

Unity AI Assistant の MCP を、手元の VLiveKit sandbox で試したときのメモです。

元々は「AI にキャラクターを踊らせる」系のサンプルを見ていたけど、今回はそこまではやらず、まず普通に Unity のシーンを読んで、保存せずに少しだけ GameObject を置けるところまで確認しました。

入口として見ていた記事はこちら。

https://qiita.com/kumi0708/items/e4e5d864219d1693d0d2

公式ドキュメントはこのあたりを確認しました。確認日は 2026-05-17 です。

https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.6/manual/integration/unity-mcp-get-started.html

## 結論: MCPは動いたが、最初に見るべき場所がいくつかある

自分の環境では、Unity AI Assistant を入れて MCP relay に接続し、以下まではできました。

- Unity Console を MCP 経由で読む
- active scene の情報を読む
- scene hierarchy を読む
- 現在のシーンに Cube を 1 個作る
- dirty なシーンは保存しないままにする

今回の環境はこれです。

```text
Unity: 6000.3.14f1
Render Pipeline: HDRP 17.3.0
Package: com.unity.ai.assistant 2.6.0-pre.1
Project: VLiveKit sandbox
OS: Windows
```

MCP 側で見えた tool は、`Unity_ManageScene`、`Unity_ManageGameObject`、`Unity_ReadConsole`、`Unity_RunCommand` などでした。

ここまでできると、AI に「シーンの状態を読んで」「Console の赤エラーを確認して」「一時的な目印を置いて」くらいの作業を頼める。キャラクターを踊らせる前に、この地味なところが通るだけでかなり便利です。

## `com.unity.ai.assistant` を入れるだけでは終わらない

まず `Packages/manifest.json` に `com.unity.ai.assistant` を入れました。

```json
"com.unity.ai.assistant": "2.6.0-pre.1"
```

ただ、自分の環境では manifest に足しただけではすぐ解決されず、Unity 側で package resolve / refresh が必要でした。最終的には Unity Package Manager が `packages-lock.json` を更新し、relay がこのあたりに入りました。

```text
%USERPROFILE%\.unity\relay\relay_win.exe
```

ここは「ファイルを足したから終わり」ではなく、Unity Editor 側がちゃんと package を import しているかを見る必要があります。

## discovery の `project_path` がプロジェクトルートではなかった

一番引っかかったのはここです。

MCP の discovery file はこのあたりにできます。

```text
%USERPROFILE%\.unity\mcp\connections\*.json
```

この中に named pipe、editor pid、project path などが入っています。

手元では `project_path` がリポジトリルートではなく、`Assets` フォルダを指していました。

```text
E:\share_ssd\Unity\GitHub\VLiveKit_sandbox\Assets
```

最初はプロジェクトルートを `--project-path` に渡していて、relay 自体は起動するのに tool が期待通り見えない状態になりました。結局、discovery file に書かれている `project_path` と一致させると接続できました。

手で relay を叩く場合は、推測で project root を渡すより、まず discovery file を見る方が早いです。

## stdio は Content-Length ではなく行区切りJSONだった

手元で一時的な Node クライアントを書いて確認したところ、Unity relay とのやり取りは LSP 風の `Content-Length:` framing ではなく、1 行 1 JSON の newline-delimited な形で通りました。

これは普通に Codex や対応 MCP client から使う場合は気にしなくてよい話です。ただ、動作確認用に最小クライアントを書く場合はここで少し詰まりました。

## まず有効にした tool は scene edit と console read

最初から強いことをやるより、低リスクな tool から見るのが良さそうです。

今回使った中心はこのあたり。

```text
Unity_ReadConsole
Unity_ManageScene
Unity_ManageGameObject
Unity_ManageEditor
```

`Unity_RunCommand` はかなり強力で、Unity Editor 内で C# をコンパイルして実行できます。便利だけど、最初の疎通確認では read 系と小さな GameObject 作成くらいにしておく方が安心でした。

## dirty なシーンは保存しない

今回、active scene は既に dirty でした。

```text
Scene: LiveToonSample_Outdoor
Path: Packages/com.toshi.vlivekit.livetoon/Sample/Sample/LiveToonSample_Outdoor.unity
Dirty: true
```

この状態で「新規 smoke scene を作って保存」までやると、既存の作業状態を壊す可能性があります。なので、テストは保存なしで `MCP_Codex_TestCube` を 1 個置くところまでにしました。

AI/MCP にシーンを触らせるときは、最初にこれを読むのが良さそうです。

- active scene name
- scene path
- dirty state
- root hierarchy
- Console の赤エラー

特に dirty な scene は、AI が勝手に save しない前提を作っておきたい。Undo で戻せる一時 marker を置くくらいから始めるのが安全でした。

## Console の赤エラーは「今回のMCP起因」かを分ける

導入中、Console にはいくつか赤エラーが出ていました。

ただし全部が MCP のせいではありませんでした。

今回見えていたものは、たとえば以下です。

- `com.unity.ai.assistant` の conversation refresh 失敗
- `.glb` / `.gltf` importer の競合
- HDRP Volume 系の `SerializedObjectNotCreatableException`
- 自分が一時的に置いた診断 script の C# 文字列ミス

このうち、本当にこちらで壊したのは一時診断 script の `CS1039` などでした。これはその script を消して解消しました。

一方で AI Assistant の conversation refresh 失敗は、MCP の tool list や scene 操作自体とは別に見えました。Assistant の cloud 側の会話取得が失敗していても、MCP relay の接続や `Unity_ReadConsole` は動く場合があります。

Console を見るときは、「今の導入で増えた compile error」と「前からある project error」を分けるのがかなり大事です。

## AIに優しいUnityプロジェクトには地図が必要

このあと、Keijiro さんの Unity AI Assistant サンプルも見ました。

https://github.com/keijiro/DungeonMatchHeroes

https://github.com/keijiro/MirrorMage

面白かったのは、UI Toolkit を使っていること自体より、AI が読める地図がプロジェクト内に置かれていることでした。

- `Guidance.md` / `Guidance.txt`
- `Project_Overview.md`
- シーン構成
- 主要コンポーネント表
- UI Toolkit の UXML/USS
- assistant log

AI に「なんとなく推測して実装して」ではなく、「このプロジェクトではこういうルールで触って」と渡すための足場がある。

さらに、より素の testbed として `UnityAI-Test` も見ました。

https://github.com/keijiro/UnityAI-Test

これは README もほぼ 1 行で、まだセットアップ中っぽい温度でした。中身としては、Unity AI Assistant 用の最小プロジェクトに近いです。

```text
Unity: 6000.3.15f1
Render Pipeline: URP 17.3.0
AI Assistant: com.unity.ai.assistant
Inference: com.unity.ai.inference
Extension: jp.keijiro.ai.assistant.extensions
UI: UI Toolkit
Scene: Assets/Main.unity
```

`Assets/Main.unity` は Camera と `UIDocument` 付きの `UI` GameObject がある程度で、`Assets/UI/Main.uxml` も空に近い状態でした。ゲームサンプルというより、AI Assistant / extensions package の受け皿を先に作っている感じです。

ひとつ気になったのは、`Packages/manifest.json` では `com.unity.ai.assistant` が `2.7.0-pre.3` なのに、`packages-lock.json` では `jp.keijiro.ai.assistant.extensions` の依存で `2.8.0-pre.1` が解決されていたことです。pre-release の AI Assistant 周りは、manifest だけでなく lock 側も見た方が良さそうでした。

あと、`ProjectSettings/Packages/com.unity.ai.assistant/Settings.json` に `CustomInstructionsGUID` がありました。こういう GUID 参照は、AI Assistant 側の custom instructions とプロジェクト内の guidance をつなぐ入口っぽいので、今後追うならここも見ると良さそうです。

自分の sandbox でも、まず以下を足しました。

```text
Assets/Project_Overview.md
Assets/Docs/VLiveKitAIGuidance.md
toshi/VLiveKit/AI/Scene Snapshot
```

`Scene Snapshot` は UI Toolkit の小さい EditorWindow で、active scene の名前、path、dirty state、root objects を AI に渡しやすい形でコピーするためのものです。保存はしません。

## 導入時の注意点まとめ

次にやるなら、この順番で見ます。

1. Unity の version と AI Assistant package version を確認する
2. `com.unity.ai.assistant` が本当に import されているか見る
3. `%USERPROFILE%\.unity\mcp\connections\*.json` を見る
4. discovery file の `project_path` を relay に渡す
5. まず `tools/list` と `Unity_ReadConsole` を試す
6. active scene が dirty なら save/create/load 系は避ける
7. 一時 object は分かりやすい名前で作る
8. Console の C# compile error だけを先に潰す

MCP は「AI に全部任せる魔法」というより、Unity Editor に対して安全に観測点と小さい操作点を作る仕組みとして見ると扱いやすいです。

特に VLiveKit みたいに sample scene、package submodule、HDRP 設定、生成物が多い sandbox では、いきなり大きな変更を頼むより、まず「読む」「Console を見る」「一時 marker を置く」まで通すのが良さそうでした。

## まだ怪しいところ

まだ整理中なのはこのあたり。

- Unity AI Assistant package のバージョン更新で tool 名や設定保存場所が変わる可能性
- `Unity_RunCommand` の discovery freshness 判定
- AI Assistant の conversation refresh error と MCP relay の関係
- 既存 Console error が多い project での安全な切り分け

ここはもう少し使ってみてから追記したいです。

---

最終更新: 2026-05-17

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
