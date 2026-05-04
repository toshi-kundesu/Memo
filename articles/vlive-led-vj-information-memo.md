---
title: "Unity LED/VJ検証: Spout/NDI/KodeLife/DMXで背景映像を扱うメモ"
emoji: "📺"
type: "idea"
topics: ["unity", "vj", "ndi", "spout", "led"]
published: false
---

バーチャルライブの個人制作・個人検証でLEDやVJを触るとき、最初は「背景に映像を流す板」として見がちだった。

でも実際に触る入口としては、`KodeLife`、`GLSL`、`Spout`、`NDI`、`ArtNet`、`DMX`、`ピクセルマッピング` みたいな名前を知っておく方が効いた。
LED/VJは背景板ではなく、外部映像と制御信号をUnityに入れる受け皿として見る。

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

実践リポジトリはVLiveKit系として整理中です。公開リンクがあるコードやサンプルは、本文中の関連する項目に挟んでいます。

## この記事の持ち帰り: LED/VJは映像素材とDMX制御が交わる接点として見る

- KodeLife、GLSL、Spout、NDIは、Unity外のVJ映像をUnityに入れる入口になる。
- LED面は発光板ではなく、ピクセル、ベゼル、黒い隙間、マット感を持つ物体として見る。
- ピクセルマッピングやArtNetで、LED/VJを照明や電飾と同じ時間軸に乗せられる。

## 結論: LED/VJは背景板ではなく映像と信号の接点

LED面に派手な映像を出すだけなら、ただの背景になりやすい。
ライブっぽく扱うなら、映像、照明、信号、ステージの奥行きがつながる場所として見る。

たとえば、

- KodeLifeからVJ映像を出す
- UnityのLEDVisionに映像を入れる
- MagicQで照明を制御する
- ArtNetでLEDや電飾のオンオフを動かす
- GLSL素材をLED面に表示する
- SpoutやNDIで外部映像を持ち込む

このへんがつながると、LEDは背景ではなくライブシステムの一部として見える。

## KodeLife/GLSL/Spout/NDIはUnity外から映像を入れる名前

VJ素材をUnity内だけで作り込む必要はない。
外部で作った映像やGLSLをUnityに入れる入口を先に知っておくと、検証がかなり楽になる。

入口になる名前はこのへん。

- `KodeLife`
- `GLSL`
- `Spout`
- `NDI`
- `RenderTexture`
- `LEDVision`
- `VJ`

KodeLifeで作った映像をLEDVisionに入れる。
外部映像をSpoutやNDIでUnityに持ち込む。
Unity側では、その映像がステージや照明と合っているかを見る。

映像素材そのものをUnityで全部作るより、Unityをプレビューと合成の場所として扱う方が切り分けやすい。

動画素材や色処理をUnityに持ち込む入口として、keijiroさんのKlakHapやGammaも棚に入る。

https://github.com/keijiro/KlakHap

https://github.com/keijiro/Gamma

## LEDVisionは分割表示/曲げ/DMX電飾/ピクセル感を分けて見る

LEDVisionシェーダでは、いくつか見ておきたい要素がある。

- 映像を分割表示できるか
- 面を曲げられるか
- DMX制御で電飾のように使えるか
- ピクセル感やベゼル感が出るか
- 板ポリでも厚みのあるLED面に見えるか

LEDはただ発光する板ではない。
ベゼル、ノーマル、テクスチャ、ピクセルピッチ、黒い隙間があると、LEDの物体感が出る。

モデルが板ポリでも、LEDが重なっている感じや分厚さが出ると画面が少し重くなる。
逆に、発光だけだと軽く見えやすい。

自分のGitHubでは、LEDVisionの検証を公開しています。古い単体スケッチと、VLiveKit系として整理しているリポジトリを分けて置いています。

https://github.com/toshi-kundesu/Sketch-231217-LEDVision

@[card](https://github.com/toshi-kundesu/VLiveKit_LEDVision_Sketch250304)

LEDVisionのShader Graph本体も、記事から辿れるように置いておく。

@[card](https://github.com/toshi-kundesu/VLiveKit_LEDVision_Sketch250304/blob/main/VLiveKit_LEDVision_Sketch250304/Assets/toshi.VLiveKit/LEDVision/LED/LEDVision/LEDVision.shadergraph)

動いている状態は自分のポストにも残しています。

https://x.com/toshikun_0112/status/1736342100478931373

## RGB分け/黒い隙間/マット感でLED面の実在感を見る

LEDは常にビカビカしているだけではない。
実際のLED面は意外とマットに見えることもあるし、スポットの影が入ることもある。

見た目として効くのはこのへん。

- 白黒系素材にRGB分けを入れる
- LEDの間に黒い隙間を残す
- ピクセルを全部均一に見せすぎない
- 発光面をマットに寄せる
- スポットや影が乗る余地を残す

RGBをぱきっと分けたり、電飾の間の黒を残したりすると、映像がただのテクスチャではなくLED面に見えやすい。

床面LEDやVJ面の見え方は、実際の映像を見てピクセル感、ベゼル感、足元との関係を分ける。

https://www.youtube.com/watch?v=hvoOatrFcKI

https://www.youtube.com/watch?v=THjekE5p2aw

https://www.youtube.com/watch?v=DD3utxriGhY

LEDビジョンのピッチは、遠景と寄りでどれくらいピクセルが見えるかを考える入口になる。

https://led.led-tokyo.co.jp/news/ledvision_pitch/

## ピクセルマッピングはシェーダの動きをArtNetでオンオフする入口

ピクセルマッピングは、現実のライブ表現から逸脱しすぎずにLEDや電飾を扱う入口になる。

シェーダで電飾の動きを作る。
ArtNetでオンオフや色を管理する。
LED面や電飾が照明制御の一部になる。

ここまで来ると、LED/VJと照明が分かれたものではなくなる。
映像、照明、電飾、ステージの見え方を同じ時間軸で動かせる。

最初は小さくていい。
短い尺で、LED面の映像と照明の色が一緒に変わるだけでも、かなり見方が変わる。

ピクセルマッピングは、LEDテープや電飾の考え方から入るとUnity側にも戻しやすい。

https://qiita.com/taisuke0430/items/af25dbc1d4b0642a439e

https://www.youtube.com/watch?v=vFDHL_5t7RA

## おわり

LED/VJは、背景を埋める板ではなく、外部映像とDMX制御の接点として見る。

`KodeLife`、`GLSL`、`Spout`、`NDI`、`LEDVision`、`ArtNet`、`ピクセルマッピング`。
このへんの名前を知っておくと、Unity内で全部作る以外の道が見える。

LED面は、映像、発光、ピクセル感、黒い隙間、マット感、照明の影まで含めて見る。
ただ派手にするより、ステージ上の物体として扱う方がライブ画面に馴染みやすい。

## 関連記事

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
