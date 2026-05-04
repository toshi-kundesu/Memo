---
title: "Unity LEDVisionシェーダ検証: 板ポリ/ShaderGraph/VJ/DMXメモ"
emoji: "🟦"
type: "idea"
topics: ["unity", "shadergraph", "vj", "dmx", "vlive"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

ライブっぽい背景をUnityで作るとき、LEDは「映像を貼る板」だけで済ませると軽く見えやすいです。
LEDVisionシェーダは、板ポリに映像を出すだけではなく、ベゼル、ノーマル、テクスチャ、モザイク、ライトの当たり方まで含めてLEDっぽく見せるための検証として整理しています。

実践リポジトリはVLiveKit系として整理中です。LEDVisionまわりは、既存の公開リポジトリとVLiveKit側の整理中リポジトリがあります。

## この記事の持ち帰り: LEDは映像ソースより、面の厚みと光の受け方を見る

- 板ポリでも、ベゼル、ノーマル、テクスチャで「LEDが並んでいる面」に寄せられる。
- 電飾的に使うなら、映像をそのまま出すよりモザイク感がある方が馴染むことがある。
- VJ、DMX、照明、ピンスポの青にじみ、ムービングの当たり方を一緒に見ると、背景板からライブ装置に近づく。

## 結論: LEDVisionは映像表示シェーダではなく、ライブ面を作るシェーダとして見る

LEDVisionでやりたかったのは、動画を表示するだけのScreenではなく、ライブ空間の中にあるLED面です。
なので見るべき項目は、動画が出るかどうかだけではありません。

- LEDの粒やピッチが見えるか。
- ベゼルやノーマルで面の厚みが出るか。
- 映像がモザイク化されて、電飾として見えるか。
- ピンスポやムービングが当たったときに、画面が空間側へ馴染むか。
- VJ素材、DMX制御、外部映像入力をつないでも破綻しないか。

初期の公開版はこのリポジトリです。

https://github.com/toshi-kundesu/Sketch-231217-LEDVision

このときのポストも、LEDVisionの方向性がかなりまとまっています。

https://x.com/toshikun_0112/status/1736342100478931373

VLiveKit側では整理用のリポジトリを作っています。

https://github.com/toshi-kundesu/VLiveKit_LEDVision_Sketch250304

## 板ポリ感を消すには、ベゼル、ノーマル、テクスチャを足す

LEDをPlaneに動画表示するだけだと、どうしても「背景に貼った映像」に見えやすいです。
手元では、板ポリでもベゼルやノーマル、テクスチャで「分厚いLEDが重なっている感じ」を入れると、かなりどっしりしました。

ここで重要なのは、実際のLEDパネルの完全再現ではなく、画面上でLED面として読めることです。
最初に見るなら、次の順番が分かりやすいです。

- 映像をそのまま貼る。
- RGBの分割やドット感を入れる。
- ベゼルやパネル境界を入れる。
- ノーマルで面の反応を作る。
- 近距離と引きで、モザイクの強さを見比べる。

白黒系のGLSLを流したときは、RGB分けが入るとかなり映えました。
VJ素材をそのままきれいに出すより、LEDとしての崩れ方を少し足す方が、画面の中で役割が出やすいです。

## 電飾として使うなら、モザイクありを基本に試す

LEDVisionは背景スクリーンにも使えますが、電飾的に使う場合は、映像が滑らかすぎると板っぽく見えます。
メモとしては「テクスチャをモザイク化しているところとしないところがある。電飾的に使うならモザイクあり」という方向でした。

つまり、用途で分けます。

- 背景映像として見せたい: 素材の見やすさを優先する。
- 電飾として光らせたい: モザイク、ドット、RGB分離を少し強める。
- ステージ装置として置きたい: ベゼル、ノーマル、影、ライトの当たりを入れる。

LEDピッチや視認距離の入口としては、このリンクも見ていました。

https://led.led-tokyo.co.jp/news/ledvision_pitch/

## KodeLifeやVJ素材を流すと、LEDVisionの癖が早く見える

LEDVisionのテストは、静止画だけだと判断しにくいです。
背面LEDVisionにKodeLifeからVJを接続して、MagicQで照明制御するデモを試していました。

その構成だと、次の問題が見えやすいです。

- 映像が動いたとき、RGB分離やモザイクがうるさすぎないか。
- 照明が当たったとき、LED面が背景だけで浮いていないか。
- 白黒系、明滅系、細かい線系で、見え方が破綻しないか。
- DMXで電飾として扱ったとき、映像とライトの間に役割差が出るか。

KodeLifeとUnityの入口としては、keijiroさんのGammaも見ていました。

https://github.com/keijiro/Gamma

## ピンスポの青にじみとムービングの当たりで、LEDを背景から空間へ戻す

LEDVisionは、映像だけだと画面内の別レイヤーに見えやすいです。
そこで、ピンスポの青にじみやムービングが当たる見え方も検証しています。

LEDはビカビカしているだけではなく、意外とマットに見える瞬間や、スポットの影が入る瞬間があります。
この見え方を入れると、LEDが背景素材ではなく、ステージ上にある面として読まれやすくなります。

見るポイントはこうです。

- LED自身の発光だけで絵を完結させない。
- ライトが当たったときの青にじみや色変化を見る。
- ムービングが当たったとき、LED表面にライブ照明の気配が乗るかを見る。
- 背景映像が主役を食いすぎる場合は、明度、彩度、モザイクを落とす。

## 検証ファイルとしてはLEDVision、RGBW、AverageColorを分ける

手元の整理中ファイルには、LEDVision以外にも `RGBW.shadergraph`、`RGBWLightController.cs`、`AverageColorExtracter.cs`、`ShaderGlobalValueSetter.cs` という名前が残っています。

ただし、これらを一つの記事で全部説明しようとすると散らかります。
分けるなら、こうです。

- `LEDVision.shadergraph`: LED面そのものの見た目。
- `RGBW.shadergraph` / `RGBWLightController.cs`: 灯体や電飾として色成分を扱う方向。
- `AverageColorExtracter.cs`: 映像やVJ素材の平均色を取って、照明や周辺要素に渡す方向。
- `ShaderGlobalValueSetter.cs`: 複数マテリアルへ共通値を配る補助。

LEDVisionの記事では、まず「LED面がライブ空間に見えるか」を中心にします。
制御や色抽出は別の記事に分けた方が、あとでVLiveKitとして整理しやすいです。

## 関連記事

- [Unity LED/VJ検証: Spout/NDI/KodeLife/DMXで背景映像を扱うメモ](./vlive-led-vj-information-memo)
- [Unity ArtNet受信はPrefabに寄せるメモ](./unity-artnet-prefab-receiver-dmx-rec-memo)
- [MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ](./magicq-lighting-speedmaster-workflow-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
