---
title: "バーチャルライブ制作で使う便利ツールまとめ: ArtNet/Timecode/照明/VJメモ"
emoji: "🧰"
type: "idea"
topics: ["vlive", "artnet", "dmx", "timecode", "touchdesigner"]
published: false
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

バーチャルライブ制作まわりで、信号確認、照明打ち込み、Timecode検証、VJ寄りの実験に使っている・使いたい便利ツールを一回まとめておきます。

ここに置いているのは、全部を本番システムに入れるためのリストというより、「いま信号が来ているか」「照明卓から何が出ているか」「Unityへ渡す前にどこで切り分けるか」を見るための入口です。

Unityだけで全部を抱えるより、ArtNet、DMX、OSC、Timecode、映像生成をそれぞれ見られるツールを外に置いておくと、個人制作でも詰まりどころを分けやすいです。

## この記事の持ち帰り: ツールは制作ではなく切り分けのために置く

- ProtokolやArtNetViewは、Unityへ入る前の信号確認に使う。
- TimeCode Generatorは、Timecode前提の流れを小さく試す入口にする。
- MagicQとQLC+は、照明卓・照明ソフト側からDMX/ArtNetを出すために見る。
- TouchDesignerは、Timecode、OSC、ArtNet、映像の中継・変換・可視化に使う。

## まず用途別に分ける

| ツール | 見るもの | 使いたい場面 |
| --- | --- | --- |
| [Protokol](https://hexler.net/protokol) | MIDI、OSC、Art-Netなどのログ | UnityやTouchDesignerへ入れる前に、制御信号が出ているか見る |
| [ArtNetView](https://artnetview.com/) | Art-NetのUniverse、Channel、送信元IP | 照明卓やQLC+からArtNetが流れているか見る |
| [TimeCode Generator](https://timecodesync.com/generator/) | LTC、MIDI、ArtNet Timecode | Timecode同期の入口を作る |
| [MagicQ](https://chamsyslighting.com/pages/magicq-downloads) | 照明卓、Cue、Timecode、ArtNet/DMX | ライブ照明の打ち込みや本番寄りの制御を試す |
| [QLC+](https://www.qlcplus.org/) | フリーの照明制御、DMX/ArtNet/sACN/OSC/MIDI | 軽めの照明制御や検証用のDMXソースを作る |
| [TouchDesigner](https://derivative.ca/) | OSC、ArtNet、Timecode、映像、CHOP/TOP | 信号変換、可視化、VJ、Unityへ渡す前の中継に使う |

## Protokolは、まず信号が出ているかを見るために使う

Protokolは、MIDI、OSC、Art-Netなどの制御信号をログとして見たいときに便利です。

Unity側の受信コードを書いていると、「送信側が出していないのか」「Unityの受信が間違っているのか」が分からなくなることがあります。
その前段でProtokolを挟むと、少なくともネットワーク上に信号が見えているかを切り分けやすいです。

特にOSCの検証では、Unityに入れる前にアドレス、値、送信元を見たいです。
ArtNetでも、照明卓やTouchDesignerから出した信号を先に眺めておくと、Unity側の実装確認が楽になります。

## ArtNetViewは、ArtNetだけを素直に見る窓として使う

ArtNetViewは、Art-Netの流れを見たいときのシンプルな確認窓として置いておきたいです。

ProtokolでもArt-Netは見られますが、ArtNetViewはUniverseやチャンネルの動きを見る用途に寄せやすいです。
照明卓、QLC+、TouchDesigner、UnityのどこかがArtNetを出しているとき、まずArtNetViewで送信元IPとUniverseを確認する。
そのうえでUnity側のUniverse設定やチャンネル割り当てを見る、という順番にすると迷子になりにくいです。

ArtNet系は、IPアドレス、サブネット、Universe表記、送信先の違いでよく詰まります。
なので、Unityの見た目が動かないときほど、先にArtNetViewのような外部ビューアで確認したいです。

## TimeCode Generatorは、同期の入口を作るために使う

TimeCode Generatorは、LTC、MIDI、ArtNet Timecodeを小さく出したいときの入口として見ています。

Timecode同期は、いきなりUnity、照明卓、音声ルーティング、映像書き出しまでまとめると重いです。
まずTimeCode GeneratorでTimecodeを出す。
TouchDesignerやMagicQで受ける。
必要ならOSC化してUnityへ渡す。
このくらいに分けると、「Timecodeを出す」「受ける」「Unityを動かす」を別々に確認できます。

過去のTimecode検証では、LTC音声をUnityで直接読むより、TouchDesignerで受けてOSC化し、Unityは変換済みのtimeを受ける方が扱いやすいと考えています。

## MagicQは、本番寄りの照明打ち込みとTimecode確認に使う

MagicQは、照明卓としてCue、Playback、FX、Timecode、ArtNet/DMXまわりを触るための中心に置きたいです。

個人制作でUnity内のライトだけを動かしていると、照明の考え方がUnityのInspectorやTimelineに閉じがちです。
MagicQを触ると、Cue、Playback、SpeedMaster、FX、Timecode同期など、照明卓側の単位で考えられます。

Unityを受信側にして、MagicQからArtNetを送る。
あるいはTimecodeでMagicQのCueを進めつつ、Unityも同じTimecodeで動かす。
この形を見ると、Unityだけでライブ制御を抱え込まない設計に寄せやすいです。

## QLC+は、軽めの照明制御・検証用DMXソースとして見る

QLC+は、フリーで触れる照明制御ソフトとして、検証用のDMX/ArtNetソースを作るのに便利そうです。

MagicQほど本番卓寄りに構えず、まずFixtureを置いて、SceneやChaserを作って、ArtNetやsACNで出す。
UnityやTouchDesignerが受ける側の検証なら、最初のDMXソースとして使いやすい場面がありそうです。

小さい照明セットや、Unity側のDMX受信テストでは、QLC+から一定の値や簡単な動きを出せるだけでも助かります。
本番卓のワークフローを学ぶ用途はMagicQ、軽く値を出して受信確認する用途はQLC+、くらいに分けて見ると良さそうです。

## TouchDesignerは、変換・可視化・映像側の実験場にする

TouchDesignerは、Timecode、OSC、ArtNet、映像をまたいで扱うための実験場としてかなり便利です。

LTCを受けてCHOPで見る。
OSCでUnityへ送る。
ArtNetを受けて値を見る。
映像を作ってNDIやSpoutで渡す。
こういう「Unityに入れる前に一回見たい」「信号を別の形へ変換したい」場面で使いやすいです。

個人制作では、Unityを全部入りの制御アプリにするより、TouchDesignerを信号と映像の中継地点にして、UnityはレンダリングやTimeline制御に寄せる方が整理しやすいことがあります。

## ざっくりした接続イメージ

最初は、このくらいの分け方で考えると見通しが良いです。

```txt
TimeCode Generator
  -> LTC / MIDI TC / ArtNet TC
  -> TouchDesigner or MagicQ
  -> OSC / ArtNet
  -> Unity

MagicQ or QLC+
  -> ArtNet / DMX / sACN
  -> ArtNetView or Protokolで確認
  -> Unity or TouchDesigner

TouchDesigner
  -> 信号確認 / 変換 / 可視化 / VJ
  -> OSC / NDI / Spout / ArtNet
  -> Unity
```

大事なのは、最初から全部をUnityへ直結しないことです。
外部ツールで見える状態を作ってからUnityへ入れると、詰まったときに戻る場所が分かりやすくなります。

## 今後足したい候補

いったん今回のリストは、Protokol、ArtNetView、TimeCode Generator、MagicQ、QLC+、TouchDesignerまでにしておきます。

あとで足すなら、このあたりも同じ記事に追記したいです。

- Voicemeeter Bananaなど、Windows上の音声ルーティング用ツール。
- NDI ToolsやSpout系の映像入出力確認ツール。
- Open Stage ControlやTouchOSCなど、OSCの操作UIを作るツール。
- ResolumeやKodeLifeなど、VJ/映像生成寄りのツール。

## 関連記事

- [個人でバーチャルライブを作るときの技術メモ: ArtNet/NDI/Timecode/SSS](./vlive-technical-input-map-memo)
- [Unity ArtNet受信はPrefabに寄せる: DMX RECとTimeline再現メモ](./unity-artnet-prefab-receiver-dmx-rec-memo)
- [UnityはLTCを直接読まずOSCで受ける: TouchDesigner/Timecode同期メモ](./unity-timecode-touchdesigner-osc-memo)
- [MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ](./magicq-lighting-speedmaster-workflow-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-07

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
