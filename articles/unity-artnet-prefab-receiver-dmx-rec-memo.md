---
title: "Unity ArtNet受信はPrefabに寄せる: DMX RECとTimeline再現メモ"
emoji: "🔌"
type: "idea"
topics: ["unity", "artnet", "dmx", "timeline", "vlive"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

UnityでArtNetを受けるとき、最初に詰まりやすいのは「信号を受けられるか」よりも、「受けた信号をどうPrefabやTimelineへ戻すか」だと思っています。

実践リポジトリはVLiveKit系として整理中です。ArtNetまわりは、まず既存の検証リポジトリから辿れる状態にしています。

## この記事の持ち帰り: ArtNet受信はシーン参照ではなくPrefab側へ寄せる

- 灯体PrefabごとにArtNet Receiverを持たせると、シーン内Receiverへの手登録を減らせる。
- IP/PortごとにArtNetサーバーを共有すると、同じ受信口を使うPrefabを増やしやすい。
- DMX RECは、色遷移や動きをDMX形式で録って、Timeline上で再現するための入口になる。

## 結論: 受信できることより、増やしても破綻しないことを先に見る

ArtNet受信は、1個のCubeやLightを動かせたところで終わりではないです。
ライブっぽい絵を作ろうとすると、灯体、エフェクト、カメラ、スクリーン、デバッグ表示など、信号を受けたい対象がすぐ増えます。

そのときに、シーン内のReceiverを各Prefabへ手で差し込む設計だと、数が増えた瞬間につらくなる。
なので、制御したいPrefabにReceiver系コンポーネントを付ければ終わる形へ寄せるのが良さそうです。

自分のGitHubでは、ArtNet受信でライトを動かす検証をこのあたりに置いています。

https://github.com/toshi-kundesu/Sketch-231206-ArtNet

具体的なライト制御のコードはこのファイルが入口です。

https://github.com/toshi-kundesu/Sketch-231206-ArtNet/blob/main/Assets/Scripts/ArtNet/RGBWLightController.cs

## IP/Portごとのサーバー共有でPrefabを増やしやすくする

灯体PrefabごとにReceiverを持たせると、次に問題になるのがUDP受信の重複です。
同じIP/Portを複数コンポーネントが開こうとすると、実装によっては受信口が衝突します。

ここは、IP/PortをキーにしてArtNetサーバーインスタンスを共有する方向が扱いやすかったです。
Prefab側は「自分が見たいUniverseやAddress」を持ち、低レイヤーの受信口は共有する。
これで、同じPortを見る灯体Prefabを大量に置いても、受信口の管理を毎回考えなくて済みます。

この時点で欲しい実装の単位は、だいたいこうです。

- ArtNetを受けるUDP/Processor。
- Universe/AddressをDMX Channelへ戻す変換。
- 灯体やエフェクト側で受け取るReceiverコンポーネント。
- 色や値の変化を残すDMX Recorder。
- 録ったデータをTimelineへ戻す仕組み。

手元の古い検証ファイルには `DMXChannel.cs`、`DMXRecorder.cs`、`DMXChannelList.cs`、`UDPReceiver.cs`、`ArtNetProcessor.cs`、`ArtNetToDMXChannel.cs`、`ArtNetUtility.cs` という名前で残っています。
このへんはそのまま記事に出すというより、VLiveKit系へ整理するための移植元として見ています。

## DMX RECはTimelineと外部制御を行ったり来たりするために見る

ArtNetで動かせるようになると、次に欲しくなるのは「今つくった動きを残す」ことです。
DMX RECを入れると、外部から作った色遷移や動きをUnity側のデータとして残し、Timelineで再現する方向に進めます。

ここで大事なのは、DMX RECを「録画っぽい便利機能」として見るより、外部制御とTimeline編集を行き来するための変換点として見ることです。
DMXでざっくり動きを作る、録る、Timelineへ乗せる、Unity側で微調整する。
この流れにできると、照明データや配灯データをアセットとして扱いやすくなります。

この検証では、`universe同時RECマージ` と `timelineでのArtNet RECデータ利用` はOKとして扱っていました。
さらに進めると、DMX制御とTimeline制御をワンポチで行ったり来たりする、という発想になります。
配灯データと動きが一緒に管理できると、灯体の置き方だけではなく、動きも再利用しやすくなります。

ArtNetを触り始めた頃の自分のポストも、入口としてはこの検証に近いです。

https://x.com/toshikun_0112/status/1732333088284516617

## エフェクトやカメラもArtNet対象にできるが、最初は灯体で固める

ArtNetで制御したいものにコンポーネントを付ければ設定終わり、という形にできると、灯体以外も対象にできます。
エフェクト、カメラ、スクリーンの値も、同じ考え方で外部から動かせます。

ただ、最初から全部を対象にすると見通しが悪くなります。
まずは灯体Prefabで、受信、Channel変換、Recorder、Timeline再現までを固める。
そのあとエフェクトやカメラへ広げる方が、どこで壊れたか見やすいです。

## まだ詰めるところ: 使っている分だけArtNetを送出する

サーバー共有やDMX RECは比較的整理しやすい一方で、「使っている分だけのArtNet送出」はまだ面倒なところです。
不要なUniverseやAddressまで処理しないようにすると軽くなりそうですが、設定UI、Prefab側の宣言、送受信の責務が絡みます。

軽量化には効きそうだけど、単純な受信サンプルとは別の設計が必要です。
VLiveKit系へ整理するときも、ここは「受信できる」から一段先の課題として分けて扱いたいです。

## 関連記事

- [MVR/VectorWorksからUnityへ配灯を持ち込む: 灯体Prefab置換とMagicQ CSVメモ](./unity-mvr-vectorworks-fixture-prefab-memo)
- [UnityはLTCを直接読まずOSCで受けるメモ](./unity-timecode-touchdesigner-osc-memo)
- [MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ](./magicq-lighting-speedmaster-workflow-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
