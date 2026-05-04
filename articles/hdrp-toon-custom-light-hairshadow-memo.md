---
title: "HDRP Toonで顔の影と髪影を調整する: Custom Light/HairShadow/Stencilメモ"
emoji: "🌗"
type: "idea"
topics: ["unity", "hdrp", "toon", "shader", "lighting"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

HDRPでToon寄りのキャラルックを扱うとき、通常のライトだけで顔をきれいに見せようとすると詰まりやすいです。
法線、ライト方向、髪影、Stencil、Custom Passを分けて見ると、顔まわりを調整するための観点が増えます。

実践リポジトリはVLiveKit系として整理中です。HDRP Toonまわりは、キャラシェーダとCustom Passを分けて整理したいです。

## この記事の持ち帰り: 顔はライトだけでなく、影の作り方で守る

- 法線球面化だけだと、顔がのっぺりすることがある。
- ライト方向制限、レンブラントっぽい影、HairShadowを組み合わせると顔の情報量を戻しやすい。
- StencilやCustom Passは、キャラだけにBloomやPostProcessをかける入口になる。

## 結論: HDRP Toonは「明るくする」より、顔の面を崩さないことを見る

ライブ照明では、ライトが強く動くので、キャラの顔が意図せず潰れたり、影が汚くなったりしやすいです。
それがダメな理由は、やっぱりキャラクターの顔はみんなが見たいよね、という前提があるからです。
顔が暗い、影が汚い、表情が読めない、という状態になると、ライトやポスプロがどれだけ派手でも主役が弱く見えます。
HDRPでToon寄りの顔を扱うなら、ただ明るくするだけでは足りません。

顔の法線をどう扱うか。
ライト方向をどこまで制限するか。
髪の影をどう落とすか。
キャラだけにポスプロをかけられるか。

このあたりを分けて見る方が、顔のデザインを守りやすいです。

ここで見ていた要素は、`normal sphericalization`、`mask + normal invert`、`directional light direction restriction`、`Rembrandt`、`perspective cancellation`、`HairShadow`、`Stencil` です。
全部を一つの万能シェーダに入れるというより、顔のどの問題に効くかを分けて見ています。

## 法線球面化は安定するが、レンブラント影でのっぺり感を戻す

顔の法線を球面化すると、Toonっぽい安定感は出ます。
ただし、それだけだと顔の面が平たく見えることがあります。

そこで、目の下や頬にレンブラントライティングっぽい影を入れる、ライト方向を制限する、髪影を別で作る、という方向を試しています。
顔の情報量を増やすというより、のっぺり感を減らすための影を戻す感覚です。

法線球面化やライト方向制限の細かいメモは、個別記事に分けています。

[Unity Toonシェーダーギミックメモ: 法線球面化/ライト方向制限](./unity-toon-normal-light-direction-memo)

見る順番としては、まずこれです。

- 法線球面化で顔の影が安定するか。
- その結果、顔が平たくなりすぎていないか。
- 目の下や頬に、必要な影が戻せるか。
- Directional Lightの方向制限で、意図しない影を抑えられるか。
- カメラ角度が変わったときに、顔の面が崩れすぎないか。

## HairShadowは通常Shadowと分け、専用Custom Light/ShadowMapで見る

通常の影をそのまま顔へ落とすと、ジャギーや法線との相性で汚く見えることがあります。
HairShadowは、通常ライトのShadowをそのまま受けるより、専用のCustom LightやShadow Mapとして分けた方が扱いやすい可能性があります。

ここは、手元ではCustom LightとShadow投影でかなり見え方が良くなりました。
ただし実装の整理はまだ続きます。

HairShadowで見たいのは、前髪や毛束の影を顔に落とすこと自体ではなく、顔を汚くしない影として扱えるかです。
通常Shadowでジャギーが目立つなら、顔用の影として分ける。
Custom Light、ShadowMap、global variables、shader側の受けを分ける。
このあたりはまだ整理中ですが、顔まわりの影を調整する方向としてはかなり手応えがありました。

## StencilとCustom PassでキャラだけBloom/Maskを分ける

キャラだけBloomしたい、肌だけ処理したい、見えるところだけ/見えないところだけMaskしたい。
こういう処理には、HDRPのStencilやCustom Passが効いてきます。

HDRPのStencil Usageはこのドキュメントが入口になります。

https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/Stencil-Usage.html

Custom Passの実装例としては、このリポジトリを見ていました。

https://github.com/alelievr/HDRP-Custom-Passes

非Stencil系の眉毛や前髪表現では、このQiita記事も入口になります。

https://qiita.com/metaaa/items/c8968257e40dcfb7d0d8

HDRP Stencilでは、`UserStencilUsage.UserBit0` を使う方向を見ていました。
見えるところだけ、見えないところだけのMaskを分ける。
Custom PostProcess内でStencil Maskを見る。
肌だけ、キャラだけBloomする。
このへんができると、キャラの扱いを背景やLEDと分けやすくなります。

## キャラルックは照明、PostProcess、Stencilと別に閉じない

キャラシェーダ単体で良く見えても、ライブ照明の中で崩れるなら意味が薄いです。
逆に、照明だけで解決しようとしても、顔の法線や影設計が弱いと寄りカメラで苦しくなります。

HDRP Toonは、シェーダ、ライト、Custom Pass、Stencil、PostProcessを一つの画作りとして見る必要がありそうです。

最小の確認セットとしては、次を見たいです。

- 正面寄り、斜め寄り、下からのティルトで顔が崩れないか。
- ライトが動いたとき、顔の影が急に汚くならないか。
- HairShadowが顔を良くしているか、ただ汚していないか。
- BloomやDiffusionがキャラの輪郭を溶かしていないか。
- 背景やLEDにかけたいポスプロと、キャラにかけたい処理を分けられるか。

## 関連記事

- [Unityフェイシャルセレクター検証: 下瞼/口移動/左右非対称メモ](./vlive-facial-selector-shape-design-memo)
- [キャラライブの修正ポイント: 揺れもの/めり込み/雑に見える処理メモ](./vlive-character-care-minus-points-memo)
- [VRM/MMDをBlenderとUnityで往復するメモ](./vrm-mmd-blender-unity-animation-workflow-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
