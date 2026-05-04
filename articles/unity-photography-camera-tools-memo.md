---
title: "Unityライブカメラ検証メモ: CameraCrossFade/ViewFinder/Composite"
emoji: "📷"
type: "idea"
topics: ["unity", "camera", "cinemachine", "visualcompositor", "vlive"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

ライブカメラをUnityで試すとき、いきなり最高のカメラワークを作ろうとすると重いです。
先に、カメラ切り替え、アングルハント、実行中のカメラ保存、Composite用マスクを小さい道具に分けると、検証がかなり速くなります。

実践リポジトリはVLiveKit系として整理中です。Photographyまわりは、カメラ補助とComposite補助を分けて整理したいです。

## この記事の持ち帰り: カメラ検証は絵作りと補助ツールを分ける

- `CameraCrossFade.cs` は、カメラの自動スイッチングとフェード/カット切り替えの検証に使う。
- `CamSettingSaver.cs` は、ランタイムで調整した位置、角度、FOVを保持するための補助になる。
- `DirectorsViewFinder.cs` 系は、シーンビューとゲームビューを同期してアングルハントしやすくする。
- Compositeは、VisualCompositorが使える場合と、Stencil白黒マスクで分ける場合を両方見る。

## 結論: カメラワークの前に、カメラを試す足場を作る

ライブカメラの記事では、寄り、引き、足元、バックショット、決めカットの話をしました。
ただ、実際にUnityで試す段階では、カメラワーク以前に「検証しやすい足場」が必要になります。

欲しいのは、だいたい次の小道具です。

- 複数カメラを一定拍で切り替える。
- フェードとカットを切り替える。
- ランタイムで動かしたカメラ設定を保持する。
- シーンビューで探したアングルをゲームビューへ合わせる。
- Cinemachine版でも同じようにアングルを合わせる。
- キャラマスクやComposite用の出力を作る。

この足場がないと、毎回「いいカットを探す」前に、Unity上の操作で疲れます。

## CameraCrossFadeは、拍数ベースでカットのリズムを試す

`CameraCrossFade.cs` は、画面全体に貼ったRawImageと複数カメラを使って、自動でカメラスイッチングしつつフェード/カットを切り替えるためのスクリプトとして作っていました。

発想はかなり単純です。

- 画面全体に貼ったRawImageを登録する。
- 切り替えたいカメラを複数登録する。
- 切り替えたい拍数を入れる。
- 入れた拍数からランダムに選んで、その拍数で切り替える。

ライブカメラは、カメラ位置だけでなく「いつ切り替わるか」でかなり印象が変わります。
拍数ベースのランダム切り替えがあると、手でTimelineを詰める前に、曲に対してどのくらいのスイッチング密度が合いそうかを眺められます。

## VLiveCameraManは、オートカメラのよく使う要素を合体させる棚

`VLiveCameraMan.cs` は、オートカメラでよく使うものを合体させるWIPとして置いていました。
カメラにアタッチして、Timeline同期もできる方向です。

ここで大事なのは、完全自動のカメラマンを作ることではなく、毎回使う小さい挙動をひとまとめにしておくことです。

- 同じ動きを毎回再現したい。
- Timelineと同期したい。
- 手でカメラを置く前に、ざっくりした動きを見たい。
- ループカメラ的な挙動を短時間で試したい。

ライブカメラは、最終的には決めカットの設計が大事です。
ただ、検証段階では、オートカメラで雑に回しながら「ここは寄りが良い」「ここは引きが欲しい」を探す方が速い場面があります。

## CamSettingSaverとViewFinderでアングル探しの手戻りを減らす

`CamSettingSaver.cs` は、ランタイムのカメラ位置、角度、FOV変更を保持する便利スクリプトとして残しています。
Play中に探した良いアングルが、Play停止で消えるとつらいです。
カメラ検証では、こういう小さい保持機能がかなり効きます。

`DirectorsViewFinder.cs` は、シーンビューとゲームビューを同期して、アングルハントするとき用のコードです。
あとからCinemachine対応版の `DirectorsViewFinderCinemachine.cs` も置いています。

見る順番としてはこうです。

- シーンビューで良い角度を探す。
- ゲームビューのカメラへ同期する。
- FOVや位置を微調整する。
- 必要ならランタイムの設定を保持する。
- Cinemachineを使う場合はCinemachine版で同じことをする。

アングル探しは、地味だけど制作速度にかなり効きます。
カメラの技術記事はCinemachineの話に寄りがちですが、その前に「見つけた角度を失わない」ことも大事です。

## VisualCompositorは、UI余白、すりガラス、キャラBloomの実験台になる

Unity側のCompositeでは、VisualCompositorがかなり便利でした。
UI用の上下レターボックスだけでなく、すりガラス的な処理、オフセットリム、キャラBloomのような処理も試しやすいです。

VisualCompositorのパッケージを入れるメモとしては、この行を残しています。

```json
"com.unity.visual-compositor": "0.30.7-preview"
```

リアルタイムコンポジットの参考として、この動画も見ていました。

https://youtu.be/DD3utxriGhY?si=o_HZ0HyJVoUTAHJD

VisualCompositorで全部解決するというより、Unity内で「レイヤーを分けて合成する」発想を試しやすくなるのが大きいです。
キャラ、背景、UI、リム、Bloom、ぼかしを一度分けて見ると、ポスプロやAE側へ渡す素材設計にもつながります。

## VisualCompositorが難しいときは、Stencil白黒マスクを作る

Composite用マスクは、VisualCompositorが使えるならそれが楽です。
ただし、Unityバージョンや構成で難しい場合は、Stencil判定の白黒マスクを作る方向もあります。

メモとして残していた手順はこうです。

- ジオメトリモデルをコピーする。
- コピーしたものを `charMask` のようなMask用にする。
- マテリアルをStencil書き込みシェーダに差し替える。
- MASSやHDRがついている場合は、うまくいかないことがあるので外して見る。
- アウトラインありモデルでは、アウトラインパスにもStencil書き込みを忘れない。

キャラマスクは、あとでAEや別DCCへ素材を渡すときにも効きます。
なので、カメラ記事の範囲ではありますが、RecorderやCompositeの記事とも強くつながります。

## 関連記事

- [Unity Cinemachineでライブカメラを整理する: Timeline/Recorder技術メモ](./unity-cinemachine-live-camera-memo)
- [バーチャルライブのカメラワークを決めカットと音で考えるメモ](./live-camera-direction-memo)
- [UnityRecorderの仕様についてのメモ](./unity-recorder-audio-frame-sync-memo)
- [Unity/After Effectsポスプロ検証: ライトラップ/グレイン/色収差メモ](./vlive-postprocess-purpose-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
