---
title: "Unityライブ照明をArtNet/DMXで外部制御するメモ"
emoji: "💡"
type: "idea"
topics: ["unity", "artnet", "dmx", "lighting", "touchdesigner"]
published: true
---

バーチャルライブの個人制作・個人検証でUnity上の照明を触るとき、最初から「照明っぽい絵作り」を全部Unity内で完結させようとすると重くなる。

まず効いたのは、`DMX`、`ArtNet`、`Timecode`、`OSC`、`TouchDesigner`、`MagicQ` みたいな名前を先に知っておくことだった。
照明をライトの見た目だけで見ずに、外からUnityのパラメータを動かす仕組みとして見られるようになる。

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

実践リポジトリはVLiveKit系として整理中です。公開リンクがあるコードやサンプルは、本文中の関連する項目に挟んでいます。

## この記事の持ち帰り: Unity照明はライトの見た目より何を信号で動かすかを見る

- ArtNet/DMXはライトだけでなく、VFX、LED、カメラ切り替え、任意パラメータを動かす入口になる。
- FogやVolumetricは全体に盛るより、どの灯体だけビームを見せるかで切ると扱いやすい。
- 照明が強くても、顔、視線、口元、輪郭を潰すならライブ画面としては弱くなる。

## 結論: Unityは照明卓より信号を受けるレンダリング側に寄せる

Unityの中に照明卓っぽいUIを全部作るより、外部から信号を受けてレンダリングする側として扱う方が整理しやすい場面がある。

ArtNetで受ける対象は、ライトだけに限らない。

- 灯体の色
- 灯体の発光
- カメラ切り替え
- VFXGraphの特効
- LED面のパラメータ
- 任意の演出パラメータ

こう見ると、Unityは「ライブ演出を全部管理する巨大アプリ」ではなく、「照明卓やTouchDesignerやTimecodeから動かされる表示側」として切れる。
最初から大きい制御システムを作る前に、この分担を知っておくとかなり迷いにくい。

## ArtNet/DMXはライト以外も動かせる入口として見る

ArtNetやDMXは、照明の専門用語として見ると身構える。
でも個人検証の入口では、「外からUnityの値を変えるための名前」くらいで置いておくと触りやすい。

たとえば、`ArtNetChannelsReceiver` や `RGBWLightController` のような受け口を作り、灯体Prefab側で色や発光を受ける。
その先で、MovingLight、PAR、RGBW、Fixture、Patch、Universe みたいな名前が出てくる。

ここで大事なのは、最初から仕様を全部読むことではない。
ライトをPrefabとして置き、外部信号で値が変わる状態を短い尺で作る。
それだけでも、Unityの照明を「手でキーフレームを打つもの」から「外部制御で動くもの」として見られる。

自分のGitHubでは、この入口に近いArtNet検証を置いています。まずはArtNetでRGBWの値を受けて、ライトと発光マテリアルを動かすくらいの粒度から見ると分かりやすい。

https://github.com/toshi-kundesu/Sketch-231206-ArtNet

https://github.com/toshi-kundesu/Sketch-231206-ArtNet/blob/main/Assets/Scripts/ArtNet/RGBWLightController.cs

動いている状態は自分のポストにも残しています。

https://x.com/toshikun_0112/status/1732333088284516617

## FogやVolumetricは全体に盛るよりライト側で見る

FogやVolumetric Lightは、ライブっぽさに効く。
ただ、全体Fogを重くしていくと、Updateや描画の重さが気になりやすい。

最初は画面全体を霧で包むより、ライト側のvolumetric multiplier的な調整で見た方が切り分けやすい。

- どの灯体だけビームを見せたいか
- 全体が白くなりすぎていないか
- 主役や顔がFogで弱くなっていないか
- 負荷が増えたときにどこが詰まっているか

Fogは「ライブ感を足すもの」ではあるけど、強くすると何を見せたい画なのかも曇る。
まずはライト単位で効き方を見る方が扱いやすい。

ライブハウス照明の基本的な見え方は、言葉で読むより動画で見た方が早い。

https://www.youtube.com/watch?v=J73YuUzgT6A

Unity側でビーム感やVolumetricを触る入口としては、このあたりも見ていた。

https://assetstore.unity.com/packages/vfx/shaders/volumetric-lights-2-234539

Houdiniのビームライト表現も、ライトの見え方を分ける参考になる。

https://x.com/trit_techne/status/1411221322936455177

## MagicQ/TouchDesigner/Timecodeは同期の入口になる

照明をUnityだけで閉じずに見ると、`MagicQ`、`TouchDesigner`、`Timecode` あたりが入口になる。

MagicQ側でBPM sync、FX preset、Playback再利用を使うと、照明作業の組み立て方をUnityだけで考えなくて済む。
TouchDesignerは、ArtNet Timecodeを見たり、外部信号をUnityに渡したりする中継役として考えやすい。
Timecodeは、音、照明、Timeline、Recorderを同じ時間基準で見るための名前になる。

最初は、1曲全部でやらなくていい。
10秒くらいで、音に合わせてライト、LED、特効のどれかが変わるだけでも十分に入口になる。

MagicQやArtNetまわりは、まずこういう公開資料やポストを入口にしていた。

https://x.com/orangecafe_/status/1729870376262615535

https://x.com/denkituna/status/1077054654897213440

https://www.tokyobs.co.jp/tokyobs-t/sales_pdf/ChamSys_MagicQ_man.pdf

https://www.youtube.com/watch?v=CYT6xpOVs6I

Timecodeを単体で試すなら、スマホアプリから触ってみるだけでも同期の考え方に入りやすい。

https://apps.apple.com/jp/app/timecode-generator/id1517410509

## 主役を食う照明は強くても弱い

照明、背景、エフェクトは強くできる。
ただ、主役を食うとライブとしては弱く見える。

特にキャラクターライブでは、顔、視線、口、肩、揺れもの、めり込みの減点が大きい。
照明だけが派手でも、寄ったときに主役が見えないと画が成立しにくい。

なので、照明を足すときは「何を強くするか」と同じくらい「何を見えなくしないか」を見る。

- 顔が沈んでいないか
- 口元や視線が読めるか
- 背景LEDに主役が負けていないか
- FogやBloomで輪郭が溶けていないか
- サビで上げたときも主役の情報が残るか

照明は単体で勝つより、主役を成立させる範囲で効かせる方が扱いやすい。

## おわり

Unityライブ照明の入口は、ライトをきれいに置くことだけではなく、外から何を動かすかを決めることだった。

まずは `DMX`、`ArtNet`、`OSC`、`Timecode`、`TouchDesigner`、`MagicQ`。
次に、MovingLight、PAR、RGBW、Fixture、Patch。
そのうえで、Fog、Volumetric、特効、LED、カメラをどこまで信号で動かすかを見る。

Unityを全部入りの照明卓にするより、信号を受けるレンダリング側として切る。
この見方を持っておくと、照明、LED、VJ、特効、書き出しがかなり整理しやすくなる。

## 関連記事

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
