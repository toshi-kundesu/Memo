---
title: "Unity HDRP Custom PassでキャラだけBloomするメモ"
emoji: "✨"
type: "tech"
topics: ["unity", "hdrp", "shader", "postprocess", "zenn"]
published: false
---

Unity HDRP の Custom Pass で、画面全体ではなく「キャラクターだけ」に Bloom やリムライトをかけるためのメモです。

この記事は特定のパッケージ紹介ではなく、Custom Pass、ステンシル、レイヤーマスク、RTHandle、ポストプロセスの順序まわりで詰まったことをまとめたものです。キャラライブや配信画面で、背景の照明や UI には影響させずに、キャラだけを少し光らせたいときの話です。

## このメモの持ち帰り

- キャラだけ Bloom したいなら、まず「Bloom させたい source」をキャラだけに分離する。
- ステンシルは便利だが、最終合成をステンシルで切ると Bloom のにじみまで切れてしまう。
- ぼかす前の source をマスクするのが基本。ぼかした後の glow は画面へ普通に足す。
- HDRP の `RTHandle` は「実際の RenderTexture サイズ」と「現在の viewport サイズ」がずれるので、そのまま `rt.width` だけで考えない。
- カメラ depth と一緒に一時 color RT を bind するなら、color/depth の物理サイズを合わせる必要がある。
- キャラをもう一回 Lit 描画して Bloom source にすると、HDR 値や露出の揺れで白飛びが暴れることがある。必要なら輝度を clamp する。
- マスク用の mesh pass と、合成用の fullscreen pass は vertex shader を分ける。

## やりたいこと

HDRP 標準の Bloom は、基本的には画面全体の明るい部分を拾います。これは自然ですが、ライブ画面では困ることがあります。

たとえば、

- LED 背景や照明は光らせたいが、キャラだけ別の強さで光らせたい
- 背景の白い UI や床反射を Bloom source にしたくない
- キャラの輪郭だけを After Effects のように少しずらして白いリムにしたい
- カメラ、照明、背景が変わってもキャラの存在感を安定させたい

こういうときは、画面全体に Bloom をかけるよりも、キャラだけのマスクや source buffer を作ってから、そこにだけ blur / composite をかけるほうが扱いやすいです。

## 基本構成

大きく分けると、処理はこの順番です。

1. 対象キャラだけを別 RT に描く
2. その RT を Bloom source または mask として使う
3. source を threshold / blur する
4. blur 結果を camera color に足す

Custom Pass で書くと、ざっくりこういう形になります。

```csharp
protected override void Execute(CustomPassContext ctx)
{
    SyncRenderTextures(ctx);

    RenderCharacterSource(ctx); // layer/stencil/depth を使ってキャラだけ描く
    ApplyBloom(ctx);            // prefilter -> blur -> composite
}
```

大事なのは、Bloom をかける対象を「最終画面」から取るのではなく、先にキャラだけの source に分けることです。

## ステンシルで考えるときの注意

ステンシルを使うと、「このピクセルはキャラです」という印を depth/stencil buffer に残せます。そこまではかなり便利です。

ただし、Bloom は blur で外側へ広がる効果です。最終合成までステンシルで切ると、キャラの外へ広がった glow が消えてしまいます。

つまり、ステンシルの使いどころは主にここです。

- source をキャラだけにする
- マスクを描く
- 透け表現や see-through のように、最終的にも領域を切りたい効果に使う

キャラ Bloom では、最終 glow はキャラの外に出てほしいので、最終合成はステンシルで縛りすぎないほうが自然です。

## マスク texture を作る方式

今回いちばん扱いやすかったのは、ステンシルだけに寄せず、キャラだけの mask texture を作る方式でした。

キャラの layer を指定して、Custom Pass 内で白い override material で描きます。

```csharp
var rendererList = new RendererListDesc(shaderTags, ctx.cullingResults, ctx.hdCamera.camera)
{
    renderQueueRange = RenderQueueRange.all,
    layerMask = targetLayer,
    overrideMaterial = maskMaterial,
    overrideMaterialPassIndex = maskPass,
    stateBlock = new RenderStateBlock(RenderStateMask.Depth)
    {
        depthState = new DepthState(false, CompareFunction.LessEqual)
    },
};

CoreUtils.DrawRendererList(ctx.cmd, ctx.renderContext.CreateRendererList(rendererList));
```

この mask texture を使うと、AE の「マスクを少しずらして輪郭だけ出す」ような処理もできます。

```hlsl
float original = SampleMask(uv);
float shifted  = SampleMask(uv - offsetPixels * maskTexelSize);
float rim = saturate(shifted - original);
```

`shifted - original` にすると、ずらしたマスクがはみ出した部分だけが残ります。まず白が出ればいい段階なら、これをそのまま camera color に足すだけでリムライトになります。

## キャラをもう一回描く方式

Bloom source として、キャラを通常 material のままもう一回描く方法もあります。

この方式の良いところは、alpha や texture、material の見た目を拾いやすいことです。髪の透明や服の明るい部分など、実際の見た目に近い source を作れます。

一方で、HDRP では注意が必要でした。

- Lit / Toon shader の HDR 値が思ったより大きい
- exposure や pre-exposure の影響で、静止していても source 輝度が変わることがある
- threshold が低いと、少しの揺れで Bloom が爆発する
- After Post Process で描くと、通常の描画経路と見え方がずれる場合がある

そのため、この方式では Bloom source を clamp しておくと安全です。

```hlsl
float3 ClampBrightness(float3 color, float maxBrightness)
{
    float brightness = max(color.r, max(color.g, color.b));
    if (maxBrightness > 0.0 && brightness > maxBrightness)
        color *= maxBrightness / max(brightness, 1e-5);

    return color;
}
```

キャラ Bloom の調整値としては、`threshold` と `intensity` だけでなく、`maxSourceBrightness` と `maxBloomBrightness` のような安全弁を持っておくと安心です。

## RTHandle のサイズ罠

HDRP の一時 RT を扱うとき、一番ややこしかったのが RTHandle のサイズです。

`ctx.cameraColorBuffer.rt.width` は、現在の Game View の見た目のサイズとは限りません。内部的な確保サイズ、dynamic resolution、XR、viewport scale の都合で、実際に使っている領域より大きいことがあります。

この状態で camera color を `0..1` UV でそのまま読むと、絵が左下に小さく貼り付いたように見えることがあります。

対策は、camera color を読むときに `rtHandleScale` を使うことです。

```csharp
var scale = source.rtHandleProperties.rtHandleScale;
propertyBlock.SetVector("_MainTexScaleBias", new Vector4(scale.x, scale.y, 0f, 0f));
```

shader 側ではこうします。

```hlsl
float2 uv = input.texcoord * _MainTexScaleBias.xy + _MainTexScaleBias.zw;
float4 color = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, uv);
```

一方、最終 composite 用の一時 RT は、現在の camera viewport サイズで確保するほうが安定しました。

```csharp
var colorWidth = ctx.hdCamera.actualWidth;
var colorHeight = ctx.hdCamera.actualHeight;
```

## depth と一緒に bind する RT のサイズ

キャラだけを描く source RT に camera depth を bind する場合、color surface と depth surface の物理サイズが違うと HDRP が怒ります。

たとえば、color が Game View の小さい viewport サイズで、depth が `3840x2160` の CameraDepthStencil だと、次のようなエラーになります。

```text
Dimensions of color surface do not match dimensions of depth surface
```

この場合、depth を使ってキャラを描く source RT だけは、camera depth の物理サイズに合わせる必要があります。

ただし、最終 composite RT まで depth の物理サイズに合わせると、今度は camera color の sampling や viewport がずれて、画面が灰色になったり、左下に縮んだりします。

分けて考えるのが大事です。

- depth と一緒に bind する source / mask RT: camera depth の物理サイズに合わせる
- final composite RT: 現在の camera viewport サイズに合わせる
- camera color を読む fullscreen pass: RTHandle scale をかけて読む

## mesh pass と fullscreen pass を分ける

mask を作る pass は、キャラの mesh を描きます。つまり vertex shader は object space の頂点を clip space に変換する必要があります。

```hlsl
MeshVaryings VertMask(MeshAttributes input)
{
    MeshVaryings output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    return output;
}
```

一方、composite pass は fullscreen triangle です。

```hlsl
FullscreenVaryings VertFullscreen(FullscreenAttributes input)
{
    FullscreenVaryings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
    return output;
}
```

ここを同じ `#pragma vertex` で共有すると、mask pass が mesh を正しく描けません。白マスクが出ないときは、まずここを疑います。

## Injection Point の考え方

Custom Pass Volume の Injection Point は、見た目と安定性にかなり効きます。

### Before Post Process

ポストプロセス前に合成すると、後段の tonemapping や Bloom に自然に乗ります。

ただし、built-in Bloom や exposure の入力にも影響しやすくなります。強い加算を入れると、次の処理が過剰に反応することがあります。

### After Post Process

ポストプロセス後に合成すると、最終画面に近い場所で足せるので、白リムや軽い glow は扱いやすいです。

ただし、HDRP の dynamic resolution や camera depth の制約には注意が必要です。depth を使う source RT と、最終 composite RT のサイズを分ける必要が出ます。

## デバッグ順

キャラ Bloom やマスクリムが出ないときは、いきなり Bloom の見た目を調整するより、次の順番で見ます。

1. `Show Mask Only` でキャラが白く出ているか
2. target layer が合っているか
3. shader tag が対象 material の pass を拾えているか
4. depth compare が厳しすぎないか
5. source RT と depth RT のサイズが合っているか
6. camera color の sampling に RTHandle scale を使っているか
7. blur 前の source が明るすぎないか
8. final composite が in-place sampling になっていないか

特に、黒い、灰色、ピンク、左下に縮む、白飛びして増殖する、のような症状は、それぞれ原因が違います。

## よくある症状

### 何も出ない

まず mask だけ表示します。mask が出ないなら、Bloom 以前の問題です。

- layer が違う
- Custom Pass Volume がカメラに効いていない
- shader tag が対象 material と合っていない
- override material の mesh vertex pass が間違っている
- depth test で落ちている

### ピンクになる

shader compile error です。HDRP/Core の include 順、vertex entry point、target renderer、未定義関数を見ます。

`SpaceTransforms.hlsl` の helper を使う場合、HDRP では `ShaderVariables.hlsl` を先に include しておくと安全でした。

### 画面が灰色になる

camera color を読みながら同じ camera color に書いている、あるいは composite RT のサイズや viewport が違う可能性があります。

一度 `compositeTexture` に描いてから、`Blitter.BlitCameraTexture` で camera color に戻すほうが安全です。

### 左下に縮む

RTHandle の物理サイズ全体を `0..1` で読んでいる可能性があります。camera color を読む fullscreen pass では `rtHandleScale` を使います。

### Bloom が爆発する

source が HDR で明るすぎる、threshold が低すぎる、あるいは exposure/pre-exposure の影響を受けています。

`maxSourceBrightness` と `maxBloomBrightness` のような clamp を入れて、まず上限を決めます。キャラだけ Bloom は、見た目の演出なので、物理的に正しい無限の明るさよりも、破綻しない上限が大事です。

### Bloom がキャラの外に出ない

final composite をステンシルで切っている可能性があります。Bloom は外へにじむ効果なので、source はマスクしても、blur 後の glow は必要に応じてステンシル外にも足します。

## まとめ

Custom Pass でキャラだけ Bloom する実装は、単に「キャラ layer に Bloom」と考えるより、次の 3 つに分けると整理しやすいです。

- キャラだけの source / mask を作る
- source / mask から blur や offset rim を作る
- final camera color に安全に composite する

ステンシルは領域判定には便利ですが、Bloom のにじみまで切ると効果が死にます。マスク texture を作って、blur 前の source を分離するほうが、キャラ Bloom や AE 風の offset rim には向いていました。

HDRP では RTHandle と depth surface のサイズ、Injection Point、raw HDR 値の暴れが罠になりやすいです。`Show Mask Only`、`Show Bloom Only`、RTHandle scale、輝度 clamp を最初から入れておくと、調整がかなり楽になります。

---

最終更新: 2026-05-14

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。誰でも参加OKで、出入りも大歓迎です。
https://discord.gg/sufusTsAcJ
