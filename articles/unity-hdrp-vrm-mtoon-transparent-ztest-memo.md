---
title: "VRM 0.xのMToonをHDRP Toonへ変換するときに見た半透明とZTestのメモ"
emoji: "🎭"
type: "tech"
topics: ["unity", "hdrp", "vrm", "shader", "mtoon"]
published: false
---

[バーチャルライブ制作メモ目次](./vlive-production-memo-index) に戻る。

VRM 0.x の MToon material を HDRP 向けの Toon shader に差し替えるとき、見た目が崩れる原因は「値を移せていない」だけではありませんでした。

今回見た問題は、おおきく分けると次の 4 つです。

- MToon の material parameter を shader swap 後にどこまで保つか
- `Cull` の `Off` / `Front` / `Back` を shader 側がどう解釈しているか
- 目、眉、髪などの alpha material が `ZTest` と `ZWrite` でどう並ぶか
- HDRP の transparent fog / atmospheric scattering が半透明にどう乗るか

結論から言うと、VRM 0.x の変換では「shader を差し替えてから見た目を作り直す」より、「MToon の状態をできるだけそのまま LiveToon 側に通す」ほうが安全でした。

## まずバックアップを作って比較できるようにする

変換ツールには、元 material のバックアップを残す処理を入れておくとかなり楽になります。

今回も、見た目がおかしくなったときに、変換前後の `.asset` を並べて見ました。

確認した値はだいたいこのあたりです。

- `_BlendMode`
- `_CullMode`
- `_SrcBlend`
- `_DstBlend`
- `_ZWrite`
- `_ZTeForLiOpa`
- `_Color`
- `_MainTex`
- `_ShadeTexture`
- `RenderType`
- `renderQueue`
- `_ALPHATEST_ON`
- `_ALPHABLEND_ON`
- `_ENABLE_FOG_ON_TRANSPARENT`

「目が真っ黒」「顔の一部が裏返って見える」「眉が前に来る」みたいな症状は、スクリーンショットだけだと原因が似て見えます。

でも material backup と比較すると、値が失われた問題なのか、shader 側の描画パスの問題なのかを切り分けやすくなります。

## shader swap は LoadModel くらい素朴に始める

昔の LoadModel 的な変換は、意外とよい出発点でした。

やっていたことはかなり単純です。

```csharp
material.shader = shaderToUse;

if (material.GetTexture("_MainTex") != null && material.GetTexture("_ShadeTexture") == null)
{
    material.SetTexture("_ShadeTexture", material.GetTexture("_MainTex"));
}

switch (material.GetInt("_BlendMode"))
{
    case 0:
        material.renderQueue = 2225;
        break;
    case 1:
        material.renderQueue = 2450;
        break;
    default:
        material.renderQueue = 3000;
        break;
}
```

このくらい素朴なほうが、MToon 側の値を壊しにくいです。

変換時にいきなり shader 独自の見た目へ寄せすぎると、何が原因で崩れたのかわからなくなります。まずは MToon の `_BlendMode`, `_CullMode`, blend factor, `_ZWrite`, `_Color`, texture を残します。

そのあと、必要な派生状態だけを補います。

- `_BlendMode` から `RenderType` tag を戻す
- `_BlendMode` から alpha keyword を戻す
- `_ShadeTexture` が空なら `_MainTex` を入れる
- LiveToon 側の Forward pass 用 `ZTest` を設定する

## Cull が逆に見えるときも、まず値を変えない

VRM の見た目が裏返って見えると、つい `Front` と `Back` を converter 側で逆にしたくなります。

でも最初にやるべきなのは、値が同じかどうかの確認でした。

HLSL/ShaderLab の `Cull` は基本的にこうです。

- `Cull Off`: 両面描画
- `Cull Front`: 表面を捨てる
- `Cull Back`: 裏面を捨てる

Unity の material 側の値は `Off = 0`, `Front = 1`, `Back = 2` として扱われます。

今回の見た目崩れは、最終的には cull 値そのものではなく、Forward/GBuffer/depth の使われ方が絡んでいました。converter が cull を勝手に反転すると、別の material でまた壊れます。

なので、まずは `_CullMode` をそのまま移す。見た目が反転しているなら、shader 側でどの pass が描いているかを見る。これが安全でした。

## Transparent の ZTest は Equal だと目が消えやすい

LiveToon には `_ZTeForLiOpa` という Forward pass 用の `ZTest` property がありました。

ここが `Equal` のままだと、半透明の目 material が深度バッファと完全一致しない限り描かれません。結果として、目が白く抜けたり、見えたり見えなかったりしました。

Transparent は `ZWrite = 0` なので、Forward pass では `LEqual` のほうが安定しました。

```csharp
// Transparent
material.SetOverrideTag("RenderType", "Transparent");
material.DisableKeyword("_ALPHATEST_ON");
material.EnableKeyword("_ALPHABLEND_ON");
material.SetFloat("_ZTeForLiOpa", 4f); // LEqual
material.renderQueue = 3000;
```

`LEqual` は Unity の `CompareFunction.LessEqual` です。数値では `4` です。

## Cutout も Equal 固定だと眉や髪が前に出ることがある

最初は Opaque/Cutout を `Equal`、Transparent を `LEqual` にしていました。

でも VRM の顔では、眉、髪、顔まわりに Cutout material が多くあります。

Cutout は `ZWrite = 1` なので、深度を書きます。ただし GBuffer/depth 側と Forward 側の alpha clip が完全に同じでないと、`Equal` はきびしくなります。

今回、眉が髪より前に来ているように見える症状が出ました。Cutout の Forward pass も `LEqual` に寄せると改善しました。

今の方針はこうです。

```csharp
switch ((int)blendMode)
{
    case 0: // Opaque
        material.SetFloat("_ZTeForLiOpa", 3f); // Equal
        break;
    case 1: // Cutout
        material.SetFloat("_ZTeForLiOpa", 4f); // LEqual
        break;
    default: // Transparent / TransparentWithZWrite
        material.SetFloat("_ZTeForLiOpa", 4f); // LEqual
        break;
}
```

Opaque は深度 prepass/GBuffer と一致させたいので `Equal` でよい場面があります。

一方、Cutout と Transparent は、alpha test や半透明合成のズレを考えると `LEqual` のほうが VRM では安全でした。

## MToon Transparent の alpha は `_Color.a * _MainTex.a` だけでは足りないことがある

目の iris や highlight は、MToon では `Transparent` でした。

最初は単純にこう考えました。

```hlsl
alpha = _MainTex.a * _Color.a;
```

でも LiveToon 側には、もともと transparent 用のしきい値処理がありました。

```hlsl
float threshold = clamp(-20.0, 1.0, _TransparentThreshold);
float thresholdAlpha = smoothstep(threshold, 1.0, mainTex.a);
float maskedAlpha = thresholdAlpha * mainTex.r;
return lerp(maskedAlpha, thresholdAlpha, _Color.a);
```

この処理は、単純な alpha というより、MToon 的な透明表現を LiveToon 側で近づけるための古い処理に見えます。

なので、いったん既存の式を残すほうにしました。

大事なのは、`_Color.a` を shader swap 前に必ず保存して、swap 後に戻すことです。`_Color` が失われると、Transparent material の濃さが変わります。

## dither clip は Forward alpha blend にそのまま持ち込まない

LiveToon の ShadowCaster 側には、transparent material に dither clip を使う処理がありました。

これは shadow/depth には向いていますが、Forward の通常半透明にそのまま入れると、半透明ではなく疑似 Cutout になります。

そのため、Forward の alpha blend では dither clip を入れず、alpha と blend state の問題として見るほうがよさそうでした。

## HDRP transparent fog は keyword で制御する

Sky 背景で透明部分が黒く見える問題もありました。

原因を追うと、LiveToon が Transparent のときに HDRP の atmospheric scattering を常に適用していました。

HDRP 標準では、transparent fog は `_ENABLE_FOG_ON_TRANSPARENT` があるときだけ適用されます。

そのため、LiveToon 側も同じように keyword で guard しました。

```hlsl
#ifdef _ENABLE_FOG_ON_TRANSPARENT
float3 volColor, volOpacity;
EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);
result.rgb = result.rgb * (1.0 - volOpacity) + volColor * result.a;
#endif
```

ただし、LoadModel 的な変換では Transparent material に fog を乗せたいので、converter 側では Transparent のときに keyword を有効化します。

```csharp
// Transparent
material.EnableKeyword("_ENABLE_FOG_ON_TRANSPARENT");
```

つまり、shader 側は「keyword がある時だけ fog」、converter 側は「Transparent なら keyword を立てる」です。

これで、Opaque/Cutout には不要な fog を乗せず、Transparent には LoadModel に近い挙動を残せます。

## renderQueue は最後にもう一度見る

今回の変換では、まず `_BlendMode` から renderQueue を決めました。

- Opaque: `2225`
- Cutout: `2450`
- Transparent: `3000`

ただし VRM では、目の highlight などが material ごとに細かい queue を持っている場合があります。

たとえば、変換前の highlight が `3500` で、変換後に `3000` へそろうと、透明同士の重なり順が変わる可能性があります。

今回の主原因は ZTest と fog でしたが、透明 material の最後の詰めでは renderQueue も比較対象に入れておいたほうがよさそうです。

## まとめ

VRM 0.x の MToon を HDRP Toon shader へ変換するときは、まず material の意味を保つのが大事でした。

- 変換前 material のバックアップを作る
- shader swap 前に MToon 互換の値を保存する
- `_Color.a` と texture を必ず戻す
- `RenderType` と alpha keyword を `_BlendMode` から復元する
- `Cull` はまず反転しない
- Cutout/Transparent の Forward `ZTest` は `LEqual` を優先する
- Transparent fog は `_ENABLE_FOG_ON_TRANSPARENT` で guard する
- Transparent には LoadModel と同じく fog keyword を立てる
- renderQueue は透明 material の重なり順として最後に比較する

見た目が崩れたときに、いきなりライティングや法線を変えないこと。

まず material state、depth state、alpha state をそろえる。そこまでそろってから、toon lighting の調整に入る。

今回の一番の学びはそこでした。
