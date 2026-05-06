---
title: "Unity の Burst エラーを BurstCache 削除で直したメモ"
emoji: "🧹"
type: "tech"
topics: ["unity", "burst", "cache", "troubleshooting"]
published: true
---

Unity で作業していたら、Console に Burst まわりの赤エラーが出るようになった。

今回出ていたのは `System.Reflection.TargetInvocationException` から `System.TypeInitializationException` につながって、`Unity.Burst.Intrinsics` や `Unity.Burst.BurstCompiler.Compile` がスタックに出てくるタイプのもの。

![Burst の TypeInitializationException が Console に出ている状態](/images/unity-burst-cache-error-memo/burst-error-console.png)

最初は package の依存関係やコード側の問題かと思ったけど、`Packages/manifest.json` の Burst / Collections / Mathematics の組み合わせ自体は特に変な状態ではなかった。

```json
{
  "com.unity.burst": "1.8.29",
  "com.unity.collections": "2.2.1",
  "com.unity.mathematics": "1.3.3"
}
```

こういうときは、Burst のキャッシュが壊れている可能性を先に疑うのが早そうだった。

## BurstCache を手動で消す

Unity を閉じてから、プロジェクトの `Library/BurstCache` を削除する。

今回のプロジェクトではここ。

```text
D:\GitHub\VLiveKit_sandbox\Library\BurstCache
```

`Library` 配下なので、これは Unity が再生成するキャッシュ。git に入れるものではないし、消してもプロジェクトのソースそのものは消えない。

手順はこれだけ。

1. Unity を閉じる
2. `Library/BurstCache` を削除する
3. Unity を開き直す

開き直したあと、Burst の赤エラーは消えた。

![BurstCache を消して開き直したあと、赤エラー数が 0 になった状態](/images/unity-burst-cache-error-memo/burst-error-count-zero.png)

## メモ

`Library` 配下は基本的に Unity の生成物なので、git に入れない。`Library/BurstCache` も同じ扱いで、詰まったときは削除して再生成させる。

今回のように `Unity.Burst.Intrinsics` や `BurstCompiler.Compile` が絡む初期化エラーで、package の依存関係が明らかに壊れていないなら、まず `Library/BurstCache` を消して確認するのが早かった。
