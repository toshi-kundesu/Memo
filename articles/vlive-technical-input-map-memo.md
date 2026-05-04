---
title: "個人でバーチャルライブを作るときの技術メモ: ArtNet/NDI/Timecode/SSS"
emoji: "🧩"
type: "idea"
topics: ["vlive", "unity", "artnet", "ndi", "shader"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

個人でバーチャルライブを作るとき、最初からコード単位で追うより、「この名前を知っていると何を分けて考えやすくなるか」を先に見た方が整理しやすい。
なぜなら、最初はキーワードが頭に浮かんでいないと何から手を付けていいかよくわからないからです。

テスト用コードやリポジトリはあとからVLiveKit系として整理していく予定です。
この記事では、まず `ArtNet`、`NDI`、`Timecode`、`TouchDesigner`、`SSS`、`異方性反射` あたりを、個人制作でどこを見るための名前として扱うかだけまとめます。

## この記事の持ち帰り: 個人制作では信号/映像/時間/質感に分ける

- `ArtNet/DMX/OSC` は、Unityの外から値を入れるための入口として見る。
- `Timecode/LTC` は、音、照明、Timeline、Recorderを同じ時間基準で見るための入口になる。
- `NDI/Spout/KodeLife` は、Unity外の映像をLED/VJ面へ持ち込むための入口になる。
- `SSS/異方性反射/Bloom/Custom Pass` は、キャラの肌、髪、輪郭をライブ照明の中で調整する入口になる。

## 実装記事の前に「何を分けたいか」を見る

気になる名前をそのまま並べると、Unityの実装タスクが無限に増えます。
でも、見方としてはもう少し単純に分けられる。

- 信号: `ArtNet`、`DMX`、`OSC`
- 時間: `Timecode`、`LTC`、`Timeline`、`Recorder`
- 映像: `NDI`、`Spout`、`KodeLife`、`RenderTexture`
- 質感: `SSS`、`異方性反射`、`Bloom`、`Stencil`、`Custom Pass`

大事なのは、全部をUnityに実装することではない。
Unityが受けるもの、Unityの外で作るもの、同期の基準にするもの、見た目を調整するものを分けておくこと。

この分け方を持っていると、詰まったときに「コードが足りない」のか、「信号の流れが見えていない」のか、「キャラの質感を見ていない」のかを切り分けやすい。

## ArtNet/DMXはライトだけでなくUnityの値を動かす入口

`ArtNet` や `DMX` は、照明用語として見ると身構える。
ただ、Unity側から見ると「外から数値を受けて、PrefabやMaterialやVFXを動かすための入口」と考えると分かりやすい。

ここでいう灯体は、ライブ照明で使うライト機材のことです。
Unity内では、Moving Light、PAR、スポットライト、発光する灯具モデル、ビームを出すPrefabのようなものとして考えると入りやすいです。

最初に見たいのはこのあたり。

- 灯体Prefabの色や発光を外から変える。
- VFX GraphやLED面のパラメータを外から変える。
- 受けた値をTimelineへ戻して、あとで再生できるようにする。
- Universe、Address、Patchのような照明側の名前を検索できるようにする。

自分のGitHubでは、ArtNetでRGBWの値を受けてライトと発光マテリアルを動かす検証を置いています。

https://github.com/toshi-kundesu/Sketch-231206-ArtNet

個別の実装メモは、ArtNet受信PrefabとDMX RECの記事に分けています。

[Unity ArtNet受信はPrefabに寄せる: DMX RECとTimeline再現メモ](./unity-artnet-prefab-receiver-dmx-rec-memo)

## Timecodeは音/照明/Timeline/Recorderの時間基準として見る

`Timecode` は、単にUnityのTimelineを再生するためのものではなく、音、照明、映像書き出しを同じ時間基準で見るための名前として置いておく。

ここで大事なのは、Unityの中で全部を抱えないことです。
LTC音声をUnityで直接解析しようとすると、信号が来ていないのか、解析が詰まっているのか、Unity側のTimeline制御が詰まっているのかが混ざりやすい。

なので、まずはこう分ける。

- LTCやTimecode信号を見る場所を作る。
- 変換したtimeをOSCでUnityへ送る。
- Unity側はOSC受信、表示、Timeline制御、Recorder確認に寄せる。
- MagicQなど照明側もTimecodeで動かす前提を早めに触る。

Timecode Generatorは、同期の考え方を小さく試す入口として置いています。
iOSにインストールしておくと、iPhoneやiPadからTimecodeを送れて便利です。

https://apps.apple.com/jp/app/timecode-generator/id1517410509

詳しい切り分けは、Timecode/TouchDesigner/OSCの記事に分けています。

[UnityはLTCを直接読まずOSCで受ける: TouchDesigner/Timecode同期メモ](./unity-timecode-touchdesigner-osc-memo)

## TouchDesignerは信号を見えるようにしてUnityへ渡す場所

`TouchDesigner` は、Unityと競合する制作ツールというより、信号の中継と可視化のために見ると入りやすい。

Timecodeなら、LTCを受けてtimeへ変換する。
OSCなら、値が来ているかを見てからUnityへ渡す。
ArtNet Timecodeなら、TouchDesigner側で読めるかを調べる。

この見方にすると、Unity側の責務をかなり小さくできます。

- 信号を見る: TouchDesigner
- 値を送る: OSC
- 表示する: Unity
- 記録する: Timeline / Recorder

ArtNet Timecodeについては、TouchDesigner側で読む入口としてこのフォーラムも見ていました。

https://forum.derivative.ca/t/artnet-timecode/266469

## NDI/Spout/KodeLifeはUnity外の映像をLED/VJへ入れる入口

LEDやVJをUnityだけで全部作る必要はない。
`KodeLife` でGLSL映像を作る、`Spout` や `NDI` で映像をUnityへ入れる、Unity側ではLED面やステージとの見え方を確認する、という分担ができます。

ここで見たいのは、映像素材の作り込みより先に「映像をUnityへ入れる道があるか」です。

- KodeLifeで作った映像をLED面に出せるか。
- SpoutやNDIで外部映像をUnityへ持ち込めるか。
- LED面がただの発光板ではなく、ベゼルや黒い隙間を持つ物体に見えるか。
- ArtNetやDMXで、LED/VJの変化を照明と同じ時間軸に乗せられるか。

After Effects側にNDIを絡める入口として、この公式ドキュメントも置いています。

https://docs.ndi.video/all/using-ndi/ndi-tools/plugins/ndi-for-after-effects

LED/VJの見方は、別記事に分けています。

[Unity LED/VJ検証: Spout/NDI/KodeLife/DMXで背景映像を扱うメモ](./vlive-led-vj-information-memo)

## SSSは肌を盛るより影境界と逆光を見るための技術

キャラルック側では、`SSS` を「肌を明るくする処理」として見ると危ない。
Toonルック、中でもセルルックにおいて過剰に入れると、蝋人形っぽく見えます。

自分の検証では、軽量SSSは逆光時のじわっとしたグラデーションや、影境界の硬さを和らげるための入口として見ています。

見るポイントはこのあたり。

- 逆光で肌にじわっとしたグラデーションが乗るか。
- 多灯時に、どこが影になっているかを軽く近似できるか。
- SSS、Bloom、リムが同じ場所に重なりすぎていないか。
- 影境界の彩度だけが上がり、輝度が上がりすぎていないか。

参考にしていた軽量SSSの入口です。

https://johnaustin.io/articles/2020/fast-subsurface-scattering-for-the-unity-urp

## 異方性反射は髪のハイライトを方向で見るための入口

髪の見え方は、リムライトだけだと整理しにくい。
髪には流れがあり、ハイライトにも方向があります。

そこで `異方性反射` を、髪の流れに沿ったハイライトを見るための入口として置いています。

- 髪の流れに沿ってハイライトが動くか。
- リムライトと異方性反射が同じ役割になっていないか。
- 逆光で髪だけ不自然に光っていないか。
- Toonルックから外れてPBR髪に寄りすぎていないか。

異方性反射の入口として見ていたリンクです。

https://marina.sys.wakayama-u.ac.jp/~tokoi/?date=20051019

HDRPの異方性反射マップについては、このリンクも見ていました。

https://3dcg-school.pro/unity-hdrp-anisotropy-map/

SSSと髪反射の細かいメモは、別記事に分けています。

[Unityキャラルック検証: 軽量SSS/髪の異方性反射/肌Bloomメモ](./unity-lightweight-sss-hair-anisotropy-memo)

## まずはコードより確認項目を決める

このあたりは、いずれコードやテスト用リポジトリを挟んだ方が分かりやすい。
ただ、先に実装だけ見ると「何を確認するコードなのか」がぼやけます。

まずは、こういう確認項目を置いておく。

- ArtNetでUnityの何を動かしたいか。
- Timecodeで何と何を同期したいか。
- TouchDesignerでどの信号を見えるようにしたいか。
- NDI/Spoutでどの映像をUnityへ入れたいか。
- SSSで肌のどの境界を柔らかくしたいか。
- 異方性反射で髪のどのハイライトを出したいか。

コードは、その確認項目に名前を付けるために整理する。
この順番の方が、あとでVLiveKit系としてリポジトリを用意するときにも記事とつなげやすい。

## 関連記事

- [Unityライブ照明をArtNet/DMXで外部制御するメモ](./vlive-lighting-main-subject-memo)
- [UnityはLTCを直接読まずOSCで受ける: TouchDesigner/Timecode同期メモ](./unity-timecode-touchdesigner-osc-memo)
- [Unity LED/VJ検証: Spout/NDI/KodeLife/DMXで背景映像を扱うメモ](./vlive-led-vj-information-memo)
- [Unityキャラルック検証: 軽量SSS/髪の異方性反射/肌Bloomメモ](./unity-lightweight-sss-hair-anisotropy-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
