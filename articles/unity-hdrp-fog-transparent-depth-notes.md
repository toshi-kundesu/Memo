---
title: "Unity HDRPのFogとTransparent Depthで詰まったときのメモ"
emoji: "🌫️"
type: "tech"
topics: ["unity", "hdrp", "fog", "shader", "transparent"]
published: true
---

HDRPでキャラクターを描いていると、Fogを入れた瞬間に「透明の面が透けているように見える」「シルエットだけがFogに出る」「背景があると見え方が変わる」といったことがあります。

これは単純なアルファの問題ではなく、HDRPのFogがどの段階で、どの深度を見て処理されるかが関係しています。

この記事は、HDRPのFogとTransparent Depthまわりで詰まったときに見るポイントのメモです。

## ざっくり結論

- HDRPのFogは、Opaque向けの深度ベース処理とTransparent材質側の処理を分けて考える。
- Transparent材質が深度を書くと、Fogや後段処理から「そこに面がある」と見える。
- 見た目が不透明でも、Transparentパスで描かれるならレンダリング上はTransparentとして扱われる。
- Transparent Depth Prepass / Postpass / Depth Writeは便利だが、Fogのシルエット問題を起こすことがある。
- Fogの問題は、alphaではなくRenderQueue、ZWrite、Depth Prepass、Receive Fogの組み合わせで起こることが多い。

## HDRP Fogは一枚岩ではない

HDRPのFogはVolumeで設定します。
Fog、Volumetric Fog、Max Fog Distanceなどの設定を通じて、シーン全体に大気感を足します。

ただし、レンダリングの観点では、Fogを一枚岩の処理として見ると混乱しやすいです。
おおまかには、次の2つを分けて考える必要があります。

1. Opaqueの深度を読んで後段で合成されるFog。
2. Transparent材質が自分のforward pass内で受けるFog。

Opaque向けのFogは、カメラから見た深度を読んで、どの距離に面があるかを判断します。
一方、Transparent材質は、材質側の設定によってFogを受けるかどうかが変わります。

この違いが、透明材質のシルエット問題につながります。

## Transparentが深度を書くとFogに見える

Transparent材質でも、Depth WriteやTransparent Depth Prepassを使うと、深度バッファに面を書けます。
これは透明同士の前後関係を安定させるために便利です。

しかし、深度を書いた時点で、後段の処理からは「そこに面がある」と見えます。
そのため、Fogがその深度を読んだ場合、透明の面そのものではなく、透明面のシルエットがFog上に出ることがあります。

見た目としては、次のような症状になります。

- 半透明の髪や袖の形がFogに浮かぶ。
- 背景にオブジェクトがあるかどうかでFogの見え方が変わる。
- 透明面の奥にあるキャラクターが不自然にFogで区切られる。
- alphaを変えてもシルエットが消えない。
- 材質はOpaqueに近い見た目なのにFogだけTransparentっぽく振る舞う。

このとき、原因は「透明度」ではなく「深度を書いていること」である場合があります。

## Receive FogとDepth Writeは別物

HDRPのTransparent材質には、Fogを受けるかどうかの設定があります。
ただし、Receive Fogを切ることと、深度を書かないことは別です。

たとえば、Transparent材質自身がFogを受けないようにしても、その材質がDepth Prepassで深度を書いていれば、Opaque向けFogや後段の深度参照処理に影響することがあります。

見るべき設定はこのあたりです。

- Surface Type
- Blend Mode
- Receive Fog
- Depth Write
- Transparent Depth Prepass
- Transparent Depth Postpass
- RenderQueue
- ZTest

Fogがおかしいときは、Receive Fogだけを見ても足りません。
「その材質がどのパスで深度を書いているか」まで見る必要があります。

## alphaが1でもTransparentはTransparent

Transparent Surface Typeで描かれている場合、alphaが1に近くても、描画パスとしてはTransparentです。
見た目がほぼ不透明でも、Opaqueと同じ扱いになるわけではありません。

この違いは、次の場面で効きます。

- Opaque Fogとの関係
- Depth Pyramidとの関係
- Transparent sorting
- Transparent Depth Prepass
- 被写界深度
- SSRや反射
- Motion Vector

「alphaが255だからOpaqueと同じ見た目になるはず」と考えると、HDRPではハマりやすいです。
見た目の不透明さと、レンダリングパスの分類は分けて考える必要があります。

## 背景があるとFogの見え方が変わる理由

Fogのシルエット問題では、背景にオブジェクトがあると症状が変わることがあります。

これは、Fogが参照する深度に何が書かれているかが変わるためです。
背景に何もなければ、空や遠方として扱われる領域に対してFogが強く乗ります。
そこへ透明面の深度だけが部分的に入ると、透明面の形がFogの境界として見えます。

逆に、後ろに別のOpaqueオブジェクトがあると、その深度がFogの基準になり、透明面のシルエットが目立ちにくくなることがあります。

つまり「背景があると直る」ように見える場合でも、透明材質が正しくなったとは限りません。
深度バッファの中身が変わった結果、Fogの境界が変わっているだけのことがあります。

## Transparent Depth Prepassは万能ではない

Transparent Depth Prepassは、透明材質の深度を先に書くことで、透明ソートや後段処理を助けるための機能です。
ただし、キャラクター表現では副作用も大きいです。

起きやすい副作用は次です。

- 半透明面がFogのシルエットとして出る。
- 奥の透明面が消える。
- 前面だけが残って重なりが壊れる。
- 髪や袖の内側が不自然に隠れる。
- Depth of FieldやSSRに意図しない面が読まれる。

特に、薄い布、髪、ハイライト、MMD由来の半透明パーツのように、重なり自体が見た目の一部になっている材質では注意が必要です。

## 調査時のチェックリスト

HDRP Fogで見た目がおかしいときは、次の順に見ると切り分けやすいです。

1. 問題の材質がOpaqueかTransparentかを見る。
2. alphaではなくSurface Typeを見る。
3. RenderQueueを見る。
4. Depth Writeが有効かを見る。
5. Transparent Depth Prepass / Postpassが有効かを見る。
6. Receive Fogが有効かを見る。
7. 背景オブジェクトの有無で症状が変わるかを見る。
8. Fogを切るとシルエットが消えるかを見る。
9. 深度を書かない設定にすると症状が変わるかを見る。

## 実装や変換で気をつけること

シェーダー変換や独自シェーダー実装では、次の方針が役立ちます。

- 透明材質を安易にDepth Prepassへ入れない。
- Transparentでもalphaが1なら不透明に見える、という見た目の期待と、描画パスの扱いを分ける。
- 袖や髪のように重なりが重要な材質では、RenderQueueとOffsetをセットで見る。
- Fogを受ける処理と、深度を書く処理を分けて設計する。
- Opaque Fogの前に深度を書かせる必要が本当にあるか確認する。

透明の見た目を安定させるための設定が、Fogでは逆に悪さをすることがあります。
HDRPでは、透明、深度、Fogの3つを必ずセットで見るのが大事です。

## まとめ

HDRPのFogで透明材質のシルエットが出る問題は、alphaだけでは説明できません。
多くの場合、Transparent材質がいつ深度を書き、その深度がどのFog処理に読まれるかが原因になります。

見た目が不透明に近くても、Transparentパスで描かれていればOpaqueとは別物です。
さらにDepth WriteやTransparent Depth Prepassを使うと、Fogや後段処理からは透明面の形が見えることがあります。

Fogで詰まったら、まずSurface Type、RenderQueue、Depth Write、Transparent Depth Prepass、Receive Fogを並べて確認するのが近道です。

## 参考

- [Unity HDRP Surface Type](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition%4015.0/manual/Surface-Type.html)
- [Unity HDRP Fog Volume Override reference](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition%4016.0/manual/fog-volume-override-reference.html)

---

最終更新: 2026-05-31
