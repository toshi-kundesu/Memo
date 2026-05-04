---
title: "VRM/MMDをBlenderとUnityで往復する: Humanoid/AutoRigPro/揺れものメモ"
emoji: "🧩"
type: "idea"
topics: ["vrm", "mmd", "blender", "unity", "animation"]
published: false
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

VRMやMMDモデルをライブ検証に使うとき、詰まりは「Unityで読み込めるか」だけではありません。
Blender、Humanoid、MToon、AutoRigPro、UnityFbxExport、Root Bone、揺れもののどこで崩れるかを分けて見る必要があります。

実践リポジトリはVLiveKit系として整理中です。モデル往復まわりは、検証シーンへ差し込める手順として整理したいです。

## この記事の持ち帰り: モデル変換は見た目、骨、揺れものを分ける

- VRM/MMDをBlenderで触るとき、Outline系Modifierが重さの原因になることがある。
- MMDモーションをUnityへ戻すなら、Humanoid化とRoot Bone設定を見る。
- MToon系の見た目、揺れもの、アニメーションを同時に崩さないように切り分ける。

## 結論: 読めたかより、ライブ検証に戻せるかを見る

VRMやMMDをUnityに読み込めたとしても、ライブ検証で使えるとは限りません。
見た目が変わる。
Rootがずれる。
揺れものが死ぬ。
表情が消える。
Humanoid設定でモーションが崩れる。

なので、変換手順は「読み込み成功」ではなく、「Unity内の照明、カメラ、Timelineで使える状態へ戻るか」で見る必要があります。

VRMをBlenderで扱う入口としては、こういう記事も見ていました。

https://styly.cc/ja/tips/blender-modeling-vrm/

## Blenderで重いときはOutline系を疑う

VRMをBlenderで触ると、モデルがかなり重くなることがあります。
Backface Outline系のModifierが入っている場合、それが表示負荷になっていることがあります。

ライブ用の検証では、まず編集しやすい状態にすることが大事です。
見た目を完全維持したまま触ろうとして重くなるなら、Outlineを一時的に非表示にして切り分ける方が早いです。

## MMDモーションはBlender/rigify/Humanoid/Animatorのどこで崩れるかを見る

MMDモーションをUnity側で試すなら、MMDモデル、Blender、RigifyやAutoRigPro、Humanoid、FBX exportの流れを見ることになります。

MMDをBlenderへ入れる。
骨を整理する。
Humanoidへ寄せる。
FBXでUnityへ戻す。
VRMやキャラモデルにAnimatorを割り当てる。

この流れのどこかでRoot BoneやJaw、目線、揺れものがずれるので、1つずつ見る必要があります。

ざっくり流れを書くと、こうです。

- MMD model/motionをBlenderへ入れる。
- rigifyやMMD bonesまわりで骨を整理する。
- Humanoid mappingできる形へ寄せる。
- FBXとしてUnityへ戻す。
- Unity側でVRMやキャラモデルにAnimatorを割り当てる。
- Jaw、目線、頭、揺れもの、Rootのずれを見る。

ここで「Unityで再生できた」だけだとまだ足りません。
ライブ検証では、カメラ寄り、照明、表情、揺れものまで戻っているかを見ます。

MMD/Blenderまわりでは、こういう記事や配布物を入口にしていました。

https://note.com/361yohen/n/n95c6d4cba3a2

https://mmd15gyuunyuu.blog.jp/archives/11193436.html

## Root Boneの選択で位置がずれる

UnityFbxExportまわりでは、SkinnedMeshRendererのRoot Bone選択がずれると、モデルや揺れものの位置が変に見えることがあります。
Root Boneを正しく選ぶだけで直るケースがあるので、変換後に位置がずれたらまず見る場所です。

ここを見落とすと、モーションやRig側の問題に見えてしまいます。
原因を広げる前に、SkinnedMeshRendererのRoot Boneを確認する方が良いです。

位置ずれで見たい順番はこのくらいです。

- SkinnedMeshRendererのRoot Boneが正しいか。
- UnityFbxExportでRootまわりが変わっていないか。
- HumanoidのHip/Rootが意図した位置にあるか。
- 揺れものの基準位置がずれていないか。
- Animatorを割り当てた後、キャラ全体が浮いたり沈んだりしていないか。

## MToonのshadetex/outlinecol/widthを戻し、HDRPで見た目を崩さない

VRM/MMD系は、MToonやシェーダ設定が見た目に大きく効きます。
ライブ用にHDRPへ持っていくなら、shadetex、outlinecol、outline widthのような見た目の情報をどこまで維持するかも見たいです。

手元では、MMD/MToon/VRMを、ARP rigging、Blender retarget、UnityFbxExport、MToon変換と組み合わせて見ていました。
目的は、ただ変換することではなく、VRM/MMDをHDRPの既存セットアップへアニメーション付きで戻すことです。
見た目、骨、揺れもの、表情を同時に崩さないために、工程ごとに確認する方が良さそうです。

AutoRigProやUnityFbxExportの周辺アセットも、往復の入口として見ていました。

https://booth.pm/ja/items/5448887

https://booth.pm/ja/items/3226395

## MMD to VRMはJaw削除、MToon変換、目/頭コピー、constraintsを分けて見る

MMDからVRMへ寄せるときは、細かい作業が一気に出ます。
Humanoid化、Jaw削除、ShaderのMToon変換、Blenderでのeye/head copy、constraints、ARP export。

このへんを一つの魔法手順にしない方がいいです。
Jawで詰まっているのか。
Shaderで見た目が変わっているのか。
Head/eyeの追従が違うのか。
constraintsやexportで骨が崩れているのか。
どこで壊れたかを分けると戻りやすいです。

## 関連記事

- [HDRP Toonで顔の影と髪影を調整する: Custom Light/HairShadow/Stencilメモ](./hdrp-toon-custom-light-hairshadow-memo)
- [キャラライブの修正ポイント: 揺れもの/めり込み/雑に見える処理メモ](./vlive-character-care-minus-points-memo)
- [キャラライブのアクトをダンス量だけで考えない: 客席煽り/手拍子/移動メモ](./vlive-act-is-not-only-dance-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
