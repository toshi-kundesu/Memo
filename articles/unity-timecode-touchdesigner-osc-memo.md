---
title: "UnityはLTCを直接読まずOSCで受ける: TouchDesigner/Timecode同期メモ"
emoji: "⏱️"
type: "idea"
topics: ["unity", "timecode", "touchdesigner", "osc", "vlive"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

Timecode同期をUnityで扱うとき、LTC音声をUnity内で直接解析するより、TouchDesignerなどで受けてOSC化し、Unityは変換済みのtimeを受ける方が切り分けやすいです。

実践リポジトリはVLiveKit系として整理中です。Timecodeまわりは、Unity側をレンダリングとTimeline制御に寄せる形で整理したいです。

## この記事の持ち帰り: 信号確認とUnity制御を分ける

- LTC音声の解析をUnity内に抱え込むと、信号確認とTimeline制御が混ざる。
- TouchDesignerでLTCを受け、OSCでUnityへtimeだけ送ると、信号とUnity動作を分けて確認できる。
- Voicemeeter Bananaは、Windows上でLTC音声の送り先を作るときに役立つ。

## 結論: UnityはLTC解析ではなく、timeを受ける側にする

Timecode同期で一番避けたいのは、「信号が来ていないのか」「Unity側の処理が動いていないのか」が分からなくなることです。

なので、LTCをTouchDesignerで受ける。
TouchDesigner上で信号を見えるようにする。
変換したtimeをOSCでUnityへ送る。
Unityは受け取ったtimeでTimelineやSequenceを動かす。

この分け方にすると、詰まったときの確認場所が明確になります。

TouchDesignerでTimecodeを扱う入口としては、このあたりが見やすいです。

https://qiita.com/komakinex/items/34659355cba77042263b

手元の検証ファイルでは、`OSCTimecodeReceiver.cs`、`TimecodeOSC.zip`、`TimecodeOSCSample.toe` がこの検証に近いです。
関連する検証名として、`TimeCodeOSCSender.toe`、`ReceiveArtNetTC.toe`、`SendArtNetTC.toe` もあります。
公開リポジトリとしては整理中なので、ここでは「何を分けたいか」だけ先に書いておきます。

## MagicQ側のTimecodeも早めに見て、Unityだけで同期を抱え込まない

照明側もTimecodeで動かすなら、Unityだけで完結させず、MagicQ側のTimecode受信も早めに触っておくと良いです。
Unityと照明卓が別々に走っている状態から合わせるより、最初からTimecode前提にした方が、あとで同期の考え方がぶれにくいです。

MagicQのTimecodeまわりは、動画で流れを見ておくと入りやすいです。

https://www.youtube.com/watch?v=BmE-bCAvvLs

LTCの揺れやジッターっぽい挙動は、音声として扱っている以上、完全に気持ちよくならないことがあります。
この話は、LTCを信号として見るうえでかなり大事です。

https://qiita.com/sunfish-shogi/items/4dc020954d46f0e998a6

## Voicemeeter BananaでLTCの送り先を作り、TDからOSC化する

Windows上でTimeCode Generatorを使うとき、アプリ側の出力先設定に引っ張られて、OS側の音声出力だけでは思った通りにルーティングできないことがあります。

その回避として、Voicemeeter Bananaで仮想デバイスを作り、そこへLTCを送り、さらにTouchDesignerで受ける流れを試しました。
TouchDesignerで受けたtimeをOSC化してUnityへ送る。
同じTimecodeに同期した照明側からArtNetをUnityへ送る。
この形にすると、UnityはLTC解析ではなく受信と描画に集中できます。

流れとしては、こう分けると考えやすいです。

- TimeCode GeneratorでLTC音を出す。
- Voicemeeter BananaでLTC音の出力先を作る。
- TouchDesignerでLTCを受け、timeへ変換する。
- OSCでUnityへtimeを送る。
- UnityはOSC受信、表示、Timeline制御、レンダリングに寄せる。
- 照明卓側はTimecode同期した状態でArtNetをUnityへ送る。

ArtNet TimecodeをTouchDesignerで読む話は、まだ整理途中です。

https://forum.derivative.ca/t/artnet-timecode/266469

Timecode Generatorアプリの入口も置いておきます。

https://apps.apple.com/jp/app/timecode-generator/id1517410509

## ArtNet TimecodeはTDで読めそうだが、送る側はまだ未整理

ArtNet Timecodeは、TouchDesigner側で読めそうな感触があります。
`dmx in` を `Info CHOP` で見るとtimeらしき中身が見える、というところまでは触っています。
一方で、送る側は開発が必要そうで、ここはまだ未整理です。

LTC音の解析だと、どうしても音のラグや解析ラグが気になります。
なので、長期的には音声として解析するより、信号として扱える方向も見たいです。
ただし、この記事ではまず `LTC -> TD -> OSC -> Unity` の切り分けを入口にします。

## Unity側で見るのはOSC受信とTimeline制御

Unity側は、OSCで受けたtimeを表示する、Timelineへ入れる、必要ならRecorderと合わせる、くらいに役割を絞ると見通しが良いです。

LTC音声の解析、信号の安定性、デバイスルーティングまでUnityに抱え込むと、個人検証でも一気に重くなります。
Timecode同期は、Unityの機能としてではなく、周辺ツールも含めた信号の流れとして見た方が良さそうです。

## 関連記事

- [MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ](./magicq-lighting-speedmaster-workflow-memo)
- [Unity ArtNet受信はPrefabに寄せるメモ](./unity-artnet-prefab-receiver-dmx-rec-memo)
- [UnityRecorderの仕様についてのメモ](./unity-recorder-audio-frame-sync-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
