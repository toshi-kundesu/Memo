---
title: "MMDMechanimからMToonへシェーダを変換する: ShadowColor/Outline/SphereMapメモ"
emoji: "🎭"
type: "idea"
topics: ["unity", "mmd", "vrm", "mtoon", "shader"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

MMDモデルをUnityやVRM側へ持っていくとき、メッシュやモーションだけ移せても、元のPMXシェーディング情報が落ちると見た目が変わります。
MMDMechanimのシェーディングをMToonへ寄せるコンバータを作ると、MMDの見た目を捨てずにUnity側の検証へ持っていきやすくなります。

実践リポジトリはVLiveKit系として整理中です。MMD/MToonまわりは、変換手順とコンバータを分けて整理したいです。

## この記事の持ち帰り: MMD移行はメッシュだけでなく、シェーディング情報を見る

- Blender経由で変換しない場合、フェイシャルリグを扱いにくい一方で、MMDMechanimは揺れもの設定の手間を減らせる。
- PMXのシェーディング情報を切り捨てると、MToon側で見た目を作り直すことになる。
- `ConvertMMDtoMToon.cs` / `MMD2MToon.cs` は、ShadowColor、Outline、SphereMapをMToon側へ寄せる検証になる。
- ToonTexはMToon側に同じ仕様がないため、移植できないものとして分けて扱う。

## 結論: MMDからVRM/HDRPへ行くなら、MToon変換を途中に置く

MMDをUnityで扱う入口として、MMDMechanimを見ていました。

https://stereoarts.jp/

MMDMechanimは、UnityでMMDを使うときのアセットとして便利です。
Blender経由でコンバートしない場合、フェイシャルリグを使いにくいのでアニメーションをいじるときはつらい。
一方で、MMDMechanimの場合は揺れものを設定する手間が省ける、という見方をしていました。

このとき問題になるのが、見た目です。
メッシュだけ移行できても、PMX側のシェーディングを切り捨てると、MMDっぽい見え方が消えます。
なので、MMDMechanimのシェーダをMToonへ寄せるコンバータを作る方向を見ています。

## 変換対象は、まずShadowColor、Outline、SphereMap

最初のWIPでは、左にMMDMechanim、右にMToonを置いて、シェーディングをVRMに移す方向を試していました。
この段階で見ていたのは、ShadowColorとOutlineです。

メモとしては、ShadowColorがVRMのデフォルトだとピンク寄りになるため、MMD側の影色を移したい、という見方でした。
さらに、アウトラインの有無や色も判定できそう、と見ていました。

その後の改良では、次の対応まで進めています。

- ShadowColorの調整。
- Outlineの移植。
- SphereMapの移植。
- ToonTexはMToon側に同じ仕様がないため、移植しない。

`MMD2MToon.cs` のメモでは、アウトライン、スフィアマップの移植対応まで入れています。
ToonTexは、MToonにその仕様がないので移植できないが、明るくなるくらいの差として見ていました。

## PMXシェーディングを捨てると、あとでルックを作り直すことになる

調べて出てくる変換手順が、元のPMXシェーディング情報を切り捨ててメッシュだけ移行するものだと、ライブ検証では少しつらいです。

理由は単純で、キャラルックを見たいときに、毎回MToon側で見た目を作り直すことになるからです。
MMD側である程度成立している影色やアウトラインがあるなら、それをMToonへ寄せた方が速い。

見るべき項目はこうです。

- Base Colorが変わりすぎていないか。
- ShadowColorがピンクや暗すぎに寄っていないか。
- Outlineの有無、色、太さが近いか。
- SphereMapの寄与が消えていないか。
- ToonTexが移植できない分、全体が明るくなりすぎていないか。

完璧な互換よりも、一発変換で「十分近い」状態に寄せることを先に見ます。
そこから手で微調整した方が、検証に入るまでが速いです。

## MMDからVRM利用手順の中では、シェーダ変換を早めに挟む

MMDからVRMへ持っていく手順としては、Humanoid変換、Jaw削除、MToon変換、BlenderでのEye/Head調整、AutoRigPro Exportなどを見ていました。

AutoRigProまわりはこのあたりも入口です。

https://booth.pm/ja/items/5448887

UnityFbxExportはこちらです。

https://booth.pm/ja/items/3226395

この流れの中で、シェーダ変換は早めに挟む方が見え方を確認しやすいです。

手順の棚としてはこうです。

- MMDMechanimやBlender経由でモデルを扱う。
- Humanoid変換する。
- Jawなど不要な扱いを整理する。
- MMDMechanimのシェーダをMToonへ変換する。
- Blender側でHead/Eyeや視線追従を調整する。
- ARP ExportやUnityFbxExportでUnityへ戻す。
- Unity側でVRM/MToonとしてルックとモーションを確認する。

ここでの主題は、全部の手順を固定することではありません。
見た目が崩れた状態でリギングやモーションに進むと、後から何が原因か分かりにくくなるので、MToon変換を早めに見る、という話です。

## 関連記事

- [VRM/MMDをBlenderとUnityで往復するメモ](./vrm-mmd-blender-unity-animation-workflow-memo)
- [HDRP Toonで顔の影と髪影を調整する: Custom Light/HairShadow/Stencilメモ](./hdrp-toon-custom-light-hairshadow-memo)
- [HDRPでVRM/MToonルックを調整する: ライブ照明向けキャラシェーダメモ](./vlive-character-shader-design-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
