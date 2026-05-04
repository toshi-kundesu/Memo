---
title: "MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ"
emoji: "🎚️"
type: "idea"
topics: ["magicq", "lighting", "dmx", "timecode", "vlive"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

MagicQを触るとき、最初は「キューをどう作るか」に目が行きます。
でもライブっぽく速く組むには、キューを増やす前に、SpeedMaster、FXプリセット、カラーパレット、フラッシュ用プレイバックの役割を分けておく方が効きそうです。

実践リポジトリはVLiveKit系として整理中です。MagicQ側の打ち込みは、UnityのArtNet受信やDMX RECとつなげて考えています。

## この記事の持ち帰り: キューを増やす前に差分を作る場所を分ける

- TC RECは、同じプレイバックへキューを積み足す入口になる。
- SpeedMasterは、動きの速さをあとから曲へ寄せるためのつまみとして残しておく。
- FXプリセット、カラーパレット、フラッシュ用プレイバックで、キューを増やしすぎず差分を作る。

## 結論: 完成形を積むより、変化させるつまみを残す

照明を作るとき、全部を細かいキューとして焼き込むと、あとから曲やカメラに合わせるのが重くなります。

まず縦に曲構成を積む。
そこへフラッシュ、色変化、FX変化、Position変化を別プレイバックで足す。
最後にSpeedMasterで速さを触れるようにしておく。

この方が、少ない操作で画の変化を作りやすいです。

## TC RECは録ったキューを残したまま追加できるのが強い

MagicQのTC RECは、同じプレイバックであれば、録ったキューを残したまま追加していけるのが便利です。
一発で完成させるより、曲を流しながら必要な変化を積む。
そのあと、足りない箇所だけ差し込む。

この使い方だと、照明の打ち込みが「全キューを設計してから置く」ではなく、「完成状態を少しずつ積み上げる」に近づきます。

MagicQのTimecodeまわりは公式ドキュメントも入口になります。

https://secure.chamsys.co.uk/docs/magicq/timecode/timecode.html#_timecode_decode

触る入口としては、`Timecode Receive`、`Art-Net`、`MIDI`、`LTC`、`Playback Synchronization` あたりの語を見ていました。
ただ、この記事で主役にしたいのは仕様説明ではなく、TC RECを「キューを積み上げるための入口」として見ることです。

## キューはリサイクルし、FX/Color/Flashで差分を作る

キューを毎回全部新しく作るより、使えるキューをリサイクルする方が速そうです。
そこに、FXプリセット、カラーパレット、フラッシュ用プレイバックを足して差分を作る。

分け方としてはこのくらいです。

- 縦キュー: 曲構成、セクション、基本の明暗を積む。
- Color Palette: 色の差分を作る。
- FX Preset: 動きの差分を作る。
- Flash Playback: 瞬間のアクセントを作る。LTPで考える。
- SpeedMaster: FXや動きの速さをあとから曲に寄せる。

`ミニマルにキューを追加・完成状態を積み上げる` という見方が、自分の中ではかなりしっくり来ています。

## フェーダー10本ならAudio/縦キュー/Flash/Color/FX/Speedを分ける

フェーダーが10本ある前提なら、ざっくりこのくらいに分けると考えやすいです。

| Fader | 役割 |
| ---: | --- |
| 1 | Audio Master |
| 2 | 曲構成を縦に積むプレイバック |
| 3 | Intensityフラッシュ単発 |
| 4 | Intensityフラッシュの連打系 |
| 5 | Color変化 |
| 6 | もう一段のColor変化 |
| 7 | FX変化またはPosition変化 |
| 8 | もう一段のFX変化またはPosition変化 |
| 9 | 空き |
| 10 | SpeedMaster |

これは固定の正解というより、考え方のメモです。
大事なのは、曲構成、瞬間のアクセント、色、動き、速さを同じ場所に詰め込まないことです。

MagicQとDMXKingの接続まわりは、このあたりも見ていました。

https://lasens.com/【接続】dmxkingとmagicq/

## Unity側へ送るなら、DMX RECまで見る

MagicQで作った照明をUnityで受けるだけならArtNet受信で足ります。
でも、後からUnity側で再現したいなら、DMX RECやTimeline化まで見ておきたいです。

MagicQで曲に合わせる。
ArtNetでUnityへ送る。
Unity側で受ける。
必要ならDMX RECしてTimelineへ戻す。

この往復が見えると、照明の打ち込みとUnity内の絵作りが分断されにくくなります。

このへんは、Unity側の記事で書いた `DMXとTimeline制御をワンポチで行ったり来たり` する話とつながります。
MagicQで作ったものを一度Unityで受け、必要なものをRECしてTimelineへ戻す。
ここまで見えると、MagicQ側の打ち込みもUnity側の絵作りも、片方だけで閉じにくくなります。

## 関連記事

- [Unity ArtNet受信はPrefabに寄せるメモ](./unity-artnet-prefab-receiver-dmx-rec-memo)
- [UnityはLTCを直接読まずOSCで受けるメモ](./unity-timecode-touchdesigner-osc-memo)
- [MVR/VectorWorksからUnityへ配灯を持ち込む: 灯体Prefab置換とMagicQ CSVメモ](./unity-mvr-vectorworks-fixture-prefab-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
