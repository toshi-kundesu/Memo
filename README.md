# Memo

VLiveKit の制作メモ、技術メモ、Zenn 公開用記事を管理する repository です。

## 内容

- `articles/`: Zenn article Markdown
- `books/`: Zenn book 用 Markdown
- `images/`: 記事で使う画像
- `private/`: 公開前・内部向けのメモ

## Unity Zenn Window

Open the Unity helper from:

`toshi > VLiveKit > Project > Zenn Window`

The window starts the Zenn preview server, opens the browser preview, opens the current article, and shows a simple in-Editor Markdown preview for selected files under `articles/`.

## Preview

Zenn CLI で preview します。

```powershell
npm install
npx zenn preview
```

`sandbox` では `Packages/Memo` として submodule 参照されています。Unity package としての runtime asset は持たず、記事と画像の管理が目的です。

## 注意

- `node_modules/` は commit しません。
- Unity が作る `.meta` は Memo では原則不要です。
- 公開する記事は frontmatter の `published: true` を確認してください。
