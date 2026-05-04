---
title: "Unity Cinemachineでライブカメラを整理する: Timeline/Recorder技術メモ"
emoji: "🎥"
type: "idea"
topics: ["unity", "cinemachine", "timeline", "recorder", "camera"]
published: true
---

Unity上のライブカメラは、実Cameraを増やすより、CinemachineのVirtual Cameraとして整理した方が扱いやすいと感じています。

ライブっぽいカメラの方向性や構図の話は別記事に分けました。
この記事では、Cinemachine、Timeline、スイッチング、ディゾルブ、RecorderやFBX書き出しとのつながりくらいに絞ります。

関連:
- [バーチャルライブのカメラワークを決めカットと音で考えるメモ](./live-camera-direction-memo)

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

実践リポジトリはVLiveKit系として整理中です。公開リンクがあるコードやサンプルは、本文中の関連する項目に挟んでいます。

## この記事の持ち帰り: Cinemachineはカメラ状態を整理するレイヤーとして使う

- 実Cameraを増やすより、Virtual Cameraでセンター、上手、下手、望遠、寄り、引きを整理する。
- Main Cameraを1つに寄せると、Visual Compositor、Recorder、書き出し後の確認が考えやすい。
- Timelineに固定できる形にすると、ランダムに良かった動きを後から直しやすい。

## 自動スイッチングは実CameraよりVirtual Cameraで整理する

最初は、自動でカメラスイッチングする仕組みを作りたかった。

最初に試していたのは、画面全体に貼ったRawImageを使って、複数のカメラをフェード・カット切り替えできるスクリプトだった。
切り替えたい拍数を入れると、その拍数でランダムにカメラが切り替わる、くらいのもの。

そこに、パーリンノイズで少しカメラを揺らして、LookAtするスクリプトを足せば、簡易的なオートカメラスイッチャーになると考えていた。

この時点ではCinemachineに全部寄せていたわけではない。
ただ、やりたかったことはかなりCinemachine向きだった。

Cinemachineのディゾルブやカメラ切り替えを考える入口として、このポストは技術側の棚に入れている。

https://x.com/toki_prpn/status/1645282581250998272

自分のGitHubにも、VLiveKit系として整理中のカメラ切り替えや揺れの検証コードがあります。

@[card](https://github.com/toshi-kundesu/VLiveKit_LEDVision_Sketch250304/blob/main/VLiveKit_LEDVision_Sketch250304/Assets/toshi.VLiveKit/Photography/VLiveCamera/CameraCrossFade.cs)

@[card](https://github.com/toshi-kundesu/VLiveKit_LEDVision_Sketch250304/blob/main/VLiveKit_LEDVision_Sketch250304/Assets/toshi.VLiveKit/Photography/VLiveCamera/Scripts/SimpleCameraSwitcher.cs)

@[card](https://github.com/toshi-kundesu/VLiveKit_LEDVision_Sketch250304/blob/main/VLiveKit_LEDVision_Sketch250304/Assets/toshi.VLiveKit/Photography/VLiveCamera/PerlinNoizeMotion.cs)

## Cinemachineはカメラワーク作成よりカメラ状態の整理に効く

ライブシーンでは、Cameraコンポーネントをたくさん扱うより、Cinemachineでカメラ状態を整理する方が破綻しにくい。

エンジニア的にはCameraコンポーネントを直接触った方が分かりやすい場面もある。
でも、Visual CompositorやRecorderへつなぐこと、最終的に1つのMain Cameraから出すことを考えると、実カメラを増やすより、Virtual Cameraを切り替える方が扱いやすい。

ライブ用のカメラシステムとして見ると、カメラの種類もいくつか欲しい。

- センターハンディ
- 客席後ろの望遠
- 下手、上手のハンディ
- クレーンっぽい望遠
- 足元や手元を抜くカメラ

こういうカメラを、ひとつずつ実Cameraとして持つより、Virtual Cameraとして置いておいて、Timelineやスクリプトで切り替える方が整理しやすい。

## Main Cameraを1つにすると書き出しと切り替えを確認しやすい

作っていると、Cameraコンポーネントは1つに寄せたい、という方向になってきた。

Visual CompositorやRecorderへつなぐことを考えると、複数の実Cameraを増やすより、Main Cameraを1つにして、Cinemachine側でVirtual Cameraを切り替える方が扱いやすい。

エンジニア的にはCameraコンポーネントを直接触った方が拡張しやすい場面もある。
ただ、ライブシーン全体としては、実カメラを増やすほど書き出しや切り替え時の扱いが複雑になる。

なので、Cinemachineは「カメラワーク作成ツール」としてだけでなく、「カメラ状態を整理するレイヤー」として見ると分かりやすい。

Main Cameraを1本に寄せたあと、映像としてどう確認するかを見るならVisual CompositorのTimeline連携も入口になる。

https://docs.unity3d.com/ja/Packages/com.unity.visual-compositor@0.16/manual/timeline.html

## Timelineに固定するとランダムな良さを後から直せる

ライブっぽいカメラを考えると、Cinemachine単体ではなくTimelineもかなり関係してくる。

やりたかったのは、TimelineでCinemachineのカメラワークを作ったシーンに、ディゾルブ用のPrefabを入れるだけで、複数のVirtual Cameraをフェード切り替えできる状態だった。

また、オートカメラも完全なランダムではなく、Timelineで決定論的に扱える方が直しやすい。
良い動きが出たところを固定して、部分的にアニメーションで直す。
それをいくつか作って、スイッチング素材として使う、みたいな考え方。

Cinemachine、Timeline、Recorderがつながると、短い尺でもライブ映像っぽい流れを作りやすくなる。

TimelineやPlayable APIをどう見ればいいかは、勉強会資料の入口があると戻りやすい。

https://unity-fully-understood.connpass.com/event/315521/?utm_campaign=event_participate_to_follower&utm_source=notifications&utm_medium=twitter

Timeline上のBindingを扱う話では、TimelineBindingResolverも調べる棚に入る。

https://x.com/trit_techne/status/1789254933608374292

https://github.com/tanitta/TimelineBindingResolver

## 超望遠jitterとディゾルブは早めに検証対象にする

Cinemachineまわりで、まだちゃんと詰めたいものも残っている。

- クロスディゾルブの扱い
- Dolly Track
- Aim位置予測
- BPMやTimecodeとの同期
- 超望遠で狙ったときのjitter
- RecorderやFBX書き出しとの連携

特に超望遠でキャラを狙うとjitterが出やすいところが気になっている。
キャラライブだと望遠で撮りたい気持ちがあるので、ここはまだ要検証。

## Recorder/FBX Exporterまで見ると書き出し後の確認材料になる

Cinemachineで作ったカメラをFBX化する話も考えていた。

たとえば、Unity上で作ったカメラを書き出して、画面に映る範囲や動きの確認対象を絞れないか、という発想。

このあたりはまだ検証途中だけど、Cinemachine、Timeline、Recorder、FBX Exporterがつながると、カメラを作って終わりではなく、書き出し後にどこを見直すかの判断材料として使える。

RecorderやFBX Exporterまわりは、書き出しを試す前に公式ページを確認しておくと、どこまでUnity側で渡せるかの見通しが立ちやすい。

https://docs.unity3d.com/ja/Packages/com.unity.formats.fbx@4.1/manual/recorder.html

Unity Virtual Cameraは、ライブカメラ的な入力や操作感を考えるときの別入口として置いている。

https://apps.apple.com/jp/app/unity-virtual-camera/id1478175507

## おわり

Cinemachineは、単にカメラをいい感じに動かすためのものというより、Unity内のライブカメラを整理するためのレイヤーとして便利だった。

センター、下手、上手、望遠、ハンディ、クレーン、寄り、引き。
そういうカメラの役割をVirtual Cameraとして置いて、Timelineやスイッチングと合わせて考える。

Cinemachineを学ぶというより、複数のカメラ状態をどう扱うかを考える入口として見ると分かりやすい。

## 関連記事

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
