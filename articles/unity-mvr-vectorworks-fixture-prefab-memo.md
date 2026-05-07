---
title: "MVR/VectorWorksからUnityへ配灯を持ち込む: 灯体Prefab置換とMagicQ CSVメモ"
emoji: "🗺️"
type: "idea"
topics: ["unity", "mvr", "vectorworks", "magicq", "lighting"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

ステージ制作でMVRやVectorWorksを調べていた理由は、立派な図面ワークフローを作りたいからではなく、灯体の位置と回転をUnityへ戻したかったからです。

実践リポジトリはVLiveKit系として整理中です。配灯再現まわりは、ArtNet受信PrefabやDMX RECとつながる形で整理したいです。

## この記事の持ち帰り: MVRは手置きを減らすための位置・回転データとして見る

- MVR/FBX/CSVは、Unity内の灯体Prefabへ位置と回転を戻すための入口になる。
- VectorWorksからFBX/MVRを出し、BlenderやMA/MagicQを挟んで、位置・回転をUnityへ戻す流れを試した。
- 最終的には、配灯、灯体Prefab、DMX RECをひとつながりで扱えると強い。

## 結論: 灯体モデルを持ち込むより、Prefab置換できることが大事

Unityで欲しいのは、必ずしもVectorWorksから出した灯体モデルそのものではありません。
欲しいのは、灯体がどこにあり、どの向きを向いているかです。

なので流れとしては、MVRやFBXから位置・回転を抜き出し、Unity側ではArtNet受信やライト表現を仕込んだPrefabへ置換するのが扱いやすいです。
モデルをそのまま使うより、ライブ用に制御しやすいPrefabへ差し替える。
この考え方にすると、配灯データとUnity側の表現を分けられます。

MVRからMA3へ持っていく流れは、このあたりも入口になります。

https://yoshi-lights.com/mvr-to-ma3/

## VectorWorksからFBX/MVRを出し、MVRやCSVからTransformを拾う

検証では、VectorWorksからFBXを出すと、灯体モデルと光源が分離された階層として取れ、位置はかなり素直に見えました。
一般灯体系のTransformのPosition/Rotationが使えそうだったので、Unity側で拾う対象としてはかなり良かったです。

一方で、MVRをMagicQで直接開こうとすると落ちるケースがありました。
ここは `MA3で読む必要ありそう`、`Blenderを挟めそう` くらいの温度で見ています。
MVRを直接Unityで読む前提に寄せすぎず、まずは確実に取れる経路を探すのが良さそうです。

Blender側にはMVR importの流れもあります。

https://extensions.blender.org/approval-queue/io-scene-mvr/

UnityにFBXとして入った後、位置・回転情報が残っていれば、そのTransformを読み取って灯体Prefabへ置換できます。

自分の中で一番分かりやすかった流れは、次の形です。

- VectorWorksからFBXとMVRを出す。
- FBXにはUnity側のシステムを仕込み、Prefab化する。
- MA側からCSVを出し、位置・回転を同期する。
- 必要なら、MVRをBlenderへインポートしてFBXとしてUnityへ戻す。
- Unity側では、ArtNet受信を仕込んだ灯体Prefabへ置換する。

別ルートとして、`VectorWorks(.mvr)->Blender(.fbx)->Unity(.fbx)` で灯体位置をCSVに焼き込むベースコードも試しています。
このへんは、まだ「一本化された正解」ではなく、読める経路を複数持っておくための検証です。

## CSVは灯体位置を焼き込み、Unityで一気に発生させる中間形式になる

MVRを直接扱うのが不安定な場合、CSVを中間形式にするのも良さそうです。
VectorWorks、MA、MagicQ、Unityの間で、何を直接読ませるかを無理に一本化せず、位置・回転の表として扱う。

Unity側のCSV読み込みには、こういうアセットも候補になります。

https://assetstore.unity.com/packages/tools/integration/csv-serialize-135763?locale=ja-JP

最終的にやりたいのは、CSVを読んだら灯体Prefabが一気に並び、ArtNet受信やTimeline再現までつながる状態です。
配灯、灯体Prefab、DMX RECがつながると、ライブ用のシーンを毎回ゼロから組まなくて済みます。

手元の検証ファイルでは、`LightingCSVReader.cs`、`ObjectPositionCSVExporter.cs`、`TestMagicQ.csv` あたりがこの話に近いです。
`LightingCSVReader.cs` はCSVから灯体情報を読む入口、`ObjectPositionCSVExporter.cs` は位置をCSVへ焼き込む入口として整理できます。
この2つがArtNet受信Prefabとつながると、図面データ、UnityのPrefab、DMX RECの距離がかなり縮まります。

## MagicQ CSVは読めるところから扱い、MVR直読みには寄せすぎない

MagicQまわりでは、MVRを読もうとして落ちる一方で、CSV自体は読み書きできそう、という見方をしていました。
なので、最初からMVRを完全に正とするより、CSVで灯体ID、位置、回転、Addressを扱えるかを見る方が早い場面があります。

CSVを読む。
Prefabを出す。
位置と回転を同期する。
ArtNet受信済みの灯体Prefabと置換する。

この粒度まで落とすと、どのツールが途中に入っても、Unity側の実装はあまり揺れません。

## まだ詰めるところ: どこを正とするか

FBX、MVR、CSVを全部使えるようにすると便利ですが、同時に「どれが正しい位置なのか」が曖昧になります。
Unity側で最終的に使う座標、回転、灯体ID、Addressをどこで確定するかは、まだ設計ポイントです。

記事としては、まず「MVR/FBX/CSVはPrefab置換のために見る」という入口だけ固定しておきます。

## 関連記事

- [Unity ArtNet受信はPrefabに寄せるメモ](./unity-artnet-prefab-receiver-dmx-rec-memo)
- [MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ](./magicq-lighting-speedmaster-workflow-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
