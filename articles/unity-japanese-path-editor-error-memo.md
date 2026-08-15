---
title: "Unity プロジェクトの日本語パスに注意したほうがよさそうメモ"
emoji: "🗂️"
type: "tech"
topics: ["unity", "editor", "ulipsync", "troubleshooting"]
published: true
---

Unity のプロジェクトを日本語名のフォルダ配下に置いていたら、Editor まわりで変な挙動が出ることがあった。

今回見ていた症状は、特定の Inspector 拡張が表示されなかったり、Console に `String conversion error` が出たりするもの。最終的には、プロジェクトの置き場所から日本語名のフォルダを外したら直った。

かなり地味だけど、Unity の Editor 拡張、Package Manager、Assembly 読み込みまわりで変な落ち方をするときに効く確認なのでメモしておく。

## この記事の持ち帰り: 日本語パスをまず外してみる

- Unity プロジェクトは、できれば英数字だけの短いパスに置く。
- `String conversion error: Illegal byte sequence` や `RuntimeAssembly.GetCodeBase` が出ている場合、パス文字列変換で詰まっている可能性を見る。
- `CustomEditor` を外すとデフォルト Inspector が出るなら、コンポーネント本体ではなく Editor 拡張側で止まっている可能性が高い。

## 日本語パスで出た症状

今回は、ある Editor 拡張の Inspector が期待通りに表示されなかった。
本来は波形や操作 UI が出るはずなのに、別環境ではそのあたりが表示されない。

対象になっていた Editor 拡張は、だいたいこの形。

```csharp
[CustomEditor(typeof(uLipSyncCalibrationAudioPlayer))]
public class uLipSyncCalibrationAudioPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ...
    }
}
```

この `[CustomEditor]` を一時的にコメントアウトすると、Unity のデフォルト Inspector は表示された。

つまり、少なくともコンポーネント自体が Missing になっているわけではない。
描画を差し替えている Editor 拡張側、または Editor 拡張が呼ばれるまでの Unity Editor 側で何かが止まっている、という切り分けになる。

この確認はかなり便利だった。
「Serialize された値が消えたのか」「コンポーネントが Missing なのか」「CustomEditor の描画だけが死んでいるのか」を分けられる。

## String conversion error はパス文字列を疑う

同じ環境で、Console にはこういう系のログも出ていた。

```text
ExecutionEngineException: String conversion error: Illegal byte sequence encountered in the input.
System.Reflection.RuntimeAssembly.GetCodeBase(...)
UnityEditor.QuickInstall.QuickInstaller.DetectPackagesFromLoadedAssemblies(...)
```

`QuickInstall` は、プロジェクト側の Editor 拡張ではなく、Unity Editor 側の package 検出まわりの処理っぽい。
読み込まれている Assembly 一覧を見ている途中で、どこかの `CodeBase`、つまり Assembly の場所を表す文字列変換に失敗しているように見えた。

このログが出ているときは、`uLipSync` のコードだけを疑うより、まず Unity Editor がプロジェクトパスや Assembly パスをうまく扱えているかを疑った方が早そうだった。

## Editor 設定保存の Move 失敗もパスまわりのサインになる

別のログとして、Editor の設定保存っぽいところでも Move 失敗が出ていた。

```text
Moving ... Temp/UnityTempFile-... to ... Preferences/Overlays/CanvasesSaveData.asset:
指定されたファイルが見つかりません。
```

これはプロジェクトの実装というより、Unity Editor のローカル設定保存に近い。
ただ、これも「Temp から Editor 設定ファイルへ移動する」処理なので、パスまわりや一時ファイルまわりが不安定になっているサインとして見るとよさそう。

この時点で、Inspector の Editor 拡張、Package/Assembly 検出、Editor 設定保存が同時に怪しくなっていた。
1個ずつコードを追うより、プロジェクトの置き場所を変えて検証する方が早い。

## 英数字だけの短いパスに移したら直った

最終的には、プロジェクトを日本語名のフォルダを含まない場所に移したら直った。

例としては、こういう方向。

```text
D:\UnityProjects\sample-project
```

実際にやるときは、だいたいこの順番で見る。

1. Unity を閉じる
2. プロジェクトを英数字だけの短いパスへ移す
3. Unity Hub から移動先のプロジェクトを開く
4. まだ変なら `Library` や `Library/PackageCache` を再生成させる

今回は、パスを直した時点で `uLipSyncCalibrationAudioPlayer` の Editor 拡張表示が戻った。

これだけで直るなら、少なくとも最初に追うべきは個別の Inspector 実装ではなく、Unity がその環境のパスをうまく扱えていなかった可能性になる。

## 別件の warning に引っ張られない

同じ Console に、ショートカット設定の warning も出ていた。

```text
Cannot rebind shortcut on read-only profile
```

これは Unity の Shortcut profile が read-only になっている状態で、Editor 拡張がショートカットを変更しようとして失敗している警告だった。

ログとしては目立つけど、今回の Inspector 表示欠落とは別件っぽい。
Console に複数の warning/error が並ぶと全部つながって見えるけど、スタックトレースがどのクラスから出ているかを見て分けた方が良い。

## まとめ

Unity の Editor 拡張が一部だけ表示されないとき、つい Editor スクリプトの `OnInspectorGUI()` や `CustomEditor` の中身を追いたくなる。

もちろんそこも見るべきだけど、`String conversion error`、`Illegal byte sequence`、`RuntimeAssembly.GetCodeBase`、`QuickInstall` あたりが同時に出ているなら、プロジェクトパスもかなり疑ってよさそう。

日本語パスが必ずダメという話ではない。
ただ、Unity Editor、Package Manager、古めのプラグイン、Editor 拡張、Reflection が絡むところでは、まだ地味に踏むことがある。

変な Editor 拡張トラブルで詰まったら、まず短い ASCII パスに置き直して確認する。
これだけで切り分けが一気に進むことがある。

---

最終更新: 2026-06-14

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
