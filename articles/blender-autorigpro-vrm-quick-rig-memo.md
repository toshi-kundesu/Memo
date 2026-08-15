---
title: "VRM/MMDをBlenderでAuto-Rig Pro設定するメモ：Quick RigからCopy Constraintsまで"
emoji: "🦴"
type: "tech"
topics: ["blender", "vrm", "mmd", "unity", "autorigpro"]
published: false
---

VRMやMMD由来のモデルをUnityのHumanoid animationへ戻したいとき、BlenderでのAuto-Rig Pro設定がひとつの分岐点になる。

この記事では、VRMをBlenderに入れて、Auto-Rig ProのQuick Rigでプリセットを当て、Unity側のHumanoid mappingで確認するところまでをメモする。

目的は、きれいなリグを最初から作ることではなく、Unityで使えるHumanoidとして破綻していないかを見ながら、必要なコントローラーやConstraintを載せること。

## VRMをBlenderに入れる

まずVRM importerで対象の`.vrm`を読み込む。

読み込み時は、テクスチャフォルダを上書きしない設定にしておくと戻しやすい。View Transformは`Standard`にして、色の確認をしやすくする。Armature displayは`In Front`にしておくと、メッシュの中の骨が見えるので作業しやすい。

ここでは見た目の完成度より、Rest Poseと骨位置を見る。

- T-PoseまたはA-Poseから大きく崩れていないか
- 左右の腕が入れ替わっていないか
- HipsからSpine、Chest、Headまで中心がつながっているか
- 手指の骨が残っているか
- 目やJawの骨がどう入っているか

VRMやMMD由来のモデルは、読み込めた時点で安心しがちだけど、Unityへ戻したときにHumanoid mappingやT-Poseで崩れることがある。最初に骨だけ見る時間を作る。

## Quick RigでVRoid系プリセットを試す

Auto-Rig Proの`Quick Rig`を開く。今回はVRM由来なので、Import presetはまず`VRoid`を試す。

CATS経由のVRoid系なら`VRoid (CATS fixed)`も候補になる。

プリセット読み込み時に、骨名が一致しないという警告が出ることがある。

この場合、いきなり適用して終わりにせず、`Fuzzy Match`で近い名前を拾わせてから中身を見る。今回のように`joint_LeftArm`、`joint_RightKnee`のような名前が残っているモデルでは、Fuzzy Matchでかなり拾える。

ただし、緑になったから正しいとは限らない。特に見るのはこのあたり。

- 左右の腕
- 左右の脚
- 親指
- Toes
- Jaw
- Eye

この時点で全部を完璧に直すより、UnityのHumanoid Configureで最終確認する前提で、危ない場所を覚えておく。

## Blenderの設定フォルダ権限に注意する

Auto-Rig Proのプリセットを保存しようとしたときに、次のような警告が出ることがあった。

```text
Unable to open or write bookmark file
"C:\Program Files\Blender Foundation\Blender 3.6\3.6\config\bookmarks.txt"
```

これはQuick Rigの骨設定エラーではなく、Blenderが`Program Files`配下の`config`へ書き込もうとして権限で弾かれている状態。

ここで`config`を丸ごと退避すると、Auto-Rig ProやVRM importerなどのアドオン設定が飛ぶ可能性がある。なので、既存設定を消さない。

今回のように作業を進めたいだけなら、Blenderを管理者権限で起動するのが一時回避として早かった。長期的には、Blender本体を`Program Files`以外の書き込み可能な場所に置くか、`config`や`bookmarks.txt`の権限だけ直す方が安全。

## 目や頭のコントローラーはMake Rig前に作る

Auto-Rig Proで`Make Rig`する前に、目線や頭まわりの追加コントローラーを作るなら先に作っておく。

順番はこうする。

1. Eye controllerを作る
2. eye bonesやtargetへConstraintを設定する
3. `Make Rig`で`Copy Constraints`を有効にする
4. 生成Rig側で目線が動くか確認する

あとからコントローラーを足すと、元Armatureから生成RigへConstraint関係をコピーするタイミングを逃しやすい。目線、頭追従、髪、アクセサリの補助制御は、Make Rig前に作る方が迷いにくい。

## Make Rigの設定

今回はAuto-Rig Proの`Make Rig`で、次のような方向にした。

- Animation: `No Animation`
- Arms: `FK`
- Legs: `IK`
- Ignore "Root" Bone: 有効
- Match to Rig: 有効
- X-Ray Display: 有効
- Same Origin: 有効
- Hide Base Armature: 有効
- Copy Constraints: 必要な場合だけ有効

MMD/VRM由来のモデルでは、Rootまわりや補助骨が多い。まずは生成Rigと元Armatureの位置関係を崩さないことを優先する。`Same Origin`と`Match to Rig`を使い、生成後にRoot、Hips、足元がずれていないかを見る。

## Unity側でHumanoid Mappingを見る

Blender側でリグができても、UnityでHumanoidとして使えるとは限らない。Unityへ戻したら、Rig設定を`Humanoid`にして`Configure`を開く。

Bodyでは次を見る。

- Hipsが骨盤中心を指しているか
- Spine、Chest、Neck、Headが中心線に沿っているか
- Shoulder、Upper Arm、Lower Arm、Handが左右逆でないか
- Upper Leg、Lower Leg、Foot、ToesがIK用の末端ではなく本体の骨を指しているか

UnityのConfigure画面は、緑になっていると安心してしまう。でも緑は「必要スロットが埋まった」だけで、動きとして正しいかまでは保証しない。

## Jawは使うかどうかを決める

Head側では、EyeとJawを見る。

VRM Exporterで`Jaw(顎)ボーンが含まれています`という警告が出ることがある。口開閉をHumanoid Jawで使わないなら、Jawは`None`にしてよい。

MMD/VRM系の表情は、BlendShapeやExpression側で持つことが多い。JawをHumanoidに入れると、顔まわりのアニメーションが想定外に動くことがある。

Jawでやるのか、BlendShape/Expressionでやるのかを分ける。

## 親指は緑でも疑う

手指は、特に親指を見る。

今回のように、左手が次のように入ることがある。

- Thumb Proximal: `LeftThumb0M`
- Thumb Intermediate: `LeftThumb1`
- Thumb Distal: `LeftThumb2`

MMD系の`親指0/1/2`としては、まず自然な対応に見える。ただ、`LeftThumb0M`が実指ではなく補助骨やメタカーパル寄りの骨として振る舞っている場合、Humanoidの親指根元として動かすとねじれる。

Unityの`Muscles & Settings`で親指だけ動かして確認する。

根元だけ大きくねじれる場合は、次も試す。

- Thumb Proximal: `LeftThumb1`
- Thumb Intermediate: `LeftThumb2`
- Thumb Distal: `None`

右手が正常で左手だけ変なら、MappingだけでなくBlender側のbone rollやRest Pose差も疑う。

左右の手を見比べると、片方だけ違和感があるケースを拾いやすい。

## T-PoseはUnityでも確認する

UnityのConfigureでは、`Pose > Enforce T-Pose`で確認する。

VRM Exporter側では、通常の`T-Poseにする`を優先する。`T-Poseにする (unity internal)`は、Avatar内部ポーズで逃がす確認用として扱う。

大事なのは、T-Poseボタンで緑になることではなく、Humanoid previewで腕、膝、足首、指が自然に曲がること。手首や膝が反対に折れるなら、BlenderかUnityのMappingへ戻る。

## VRM Export前に見るもの

Auto-Rig Pro設定後、VRMとして出す前に見るのはこのあたり。

- Root OKになっている
- Required Title、Version、Authorを入れている
- Jaw warningが不要なら消えている
- Thumbnailを入れている
- Mesh tabで不要なRendererや非表示パーツが混ざっていない
- BlendShape/Expressionが残っている

このあと、VRM変換でMToon/LiveToonへ寄ってしまったMaterialを戻す作業が必要になる場合がある。そこはUnity側で、変換前の`Materials`フォルダから差し替える。

## まとめ

今回の流れでは、Auto-Rig ProのQuick Rigを使って、VRM/MMD由来のモデルをHumanoid向けに整理できそうだった。

ただし、見る場所は決まっている。

- VRoid系プリセットで拾えるか
- Fuzzy Match後に左右が壊れていないか
- Eye/Jawをどう扱うか
- 親指がねじれていないか
- Make Rig前にCopy Constraintsしたいコントローラーを作れているか
- UnityのHumanoid previewで自然に動くか

ここまでできれば、「BlenderでAuto-Rig Pro設定して、UnityのHumanoidへ戻す」入口としてはかなり見通しがよくなる。

---

最終更新: 2026-06-08

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
