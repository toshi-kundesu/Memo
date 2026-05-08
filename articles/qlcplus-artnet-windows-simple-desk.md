---
title: "QLC+でArt-Netを送ってみる：Windowsでのインストールからシンプル卓まで"
emoji: "💡"
type: "tech"
topics: ["QLCPlus", "ArtNet", "DMX", "Lighting", "Unity"]
published: false
---

## はじめに

QLC+ は、DMX / Art-Net などを扱えるオープンソースのライティングコンソールです。

この記事では、Windows に QLC+ をインストールして、シンプル卓から Art-Net を送信するところまでをメモします。

Unity 側で受け取る話も少しだけ出てきますが、そこは別記事で詳しく書く予定です。

![QLC+のトップページ](/images/qlcplus-artnet-windows-simple-desk/01-qlcplus-home.png)

## QLC+をダウンロードする

公式サイトにアクセスします。

https://www.qlcplus.org/

トップページの `Download` か、上部メニューの `Download` を開きます。

![QLC+のダウンロードページ](/images/qlcplus-artnet-windows-simple-desk/02-download-page.png)

Windows の場合は `Windows 10 or later` の行にある `Download` を押します。

この記事を書いている時点では、公式ダウンロードページには V5 系と V4 系が並んでいました。

- V5: QLC+ 5.2.1
- V4: QLC+ 4.14.4

ダウンロードが完了すると、ブラウザのダウンロード履歴などに `QLC+_5.2.1.exe` のようなインストーラーが表示されます。

![QLC+のインストーラーがダウンロードされたところ](/images/qlcplus-artnet-windows-simple-desk/03-download-history.png)

## インストールする

ダウンロードした `.exe` を実行します。

セットアップ画面が開くので、表示される内容に従って進めます。

ライセンスやインストール条件が表示されたら、内容を読んだうえで同意して進めてください。

![QLC+のインストール中](/images/qlcplus-artnet-windows-simple-desk/04-installer-progress.png)

インストールが終わったら、最後に `閉じる` を押してセットアップを終了します。

![QLC+のインストール完了画面](/images/qlcplus-artnet-windows-simple-desk/05-installer-complete.png)

## テスト用のQLC+ファイルを使う

手順どおりに設定してもよいですが、同じ状態から試したい場合はテスト用の `.qxw` ファイルを用意しておくと楽です。

この記事では `qlc_test.qxw` というファイルを使っています。GitHub に置いてあります。

https://github.com/toshi-kundesu/Memo/blob/main/samples/qlcplus-artnet-windows-simple-desk/qlc_test.qxw

GitHub の画面を開いたら、右上あたりの `Raw` ボタン、またはダウンロードアイコンから保存できます。

![GitHubからqlc_test.qxwをダウンロードする](/images/qlcplus-artnet-windows-simple-desk/19-github-qxw-download.png)

このファイルには、Art-Net の出力先として `127.0.0.1` が設定されています。

- `Universe 1`: Art-Net 出力あり
- `Universe 2`: Art-Net 出力あり
- `Universe 3` 以降: 未設定

QLC+ を起動したあと、メニューからこの `.qxw` を開くと、入出力設定を再現しやすくなります。

## QLC+を起動する

Windows の検索で `qlc` などと入力します。

検索結果には、インストーラーの `.exe` と、インストールされたアプリ本体の両方が出てくることがあります。

起動するのはアプリ本体のほうです。

![Windows検索からQ Light Controller Plusを起動する](/images/qlcplus-artnet-windows-simple-desk/06-windows-search.png)

`Q Light Controller Plus` を開きます。

起動すると、上部に `フィクスチャー・ファンクション`、`バーチャルコンソール`、`シンプル卓`、`入出力設定` などのタブが並びます。

![QLC+を起動した直後の画面](/images/qlcplus-artnet-windows-simple-desk/07-qlcplus-main.png)

## 入出力設定を開く

QLC+ が起動したら、上部のタブから `入出力設定` を開きます。

ここで Art-Net の出力を有効にします。

画面には `Universe 1`、`Universe 2` のようにユニバースが並びます。

![入出力設定でUniverseごとのArt-Net出力を見る](/images/qlcplus-artnet-windows-simple-desk/08-input-output-artnet-play.png)

各ユニバースの右側に Art-Net の出力設定があり、再生マークのようなボタンを押すと、そのユニバースの出力が有効になります。

有効になると、ボタンが停止/一時停止っぽい表示に変わります。

![Universe 1のArt-Net出力を有効にした状態](/images/qlcplus-artnet-windows-simple-desk/09-input-output-artnet-pause.png)

ボタンにマウスを乗せると、`出力の一時停止` のようなツールチップが出ます。

![出力ボタンのツールチップ](/images/qlcplus-artnet-windows-simple-desk/10-output-pause-tooltip.png)

今回は例として、`Universe 2` の Art-Net 出力を有効にしました。

## シンプル卓で値を送る

次に、上部タブから `シンプル卓` を開きます。

左上のユニバース選択で、送信したいユニバースを選びます。

![シンプル卓でUniverseを選び、フェーダーを動かす](/images/qlcplus-artnet-windows-simple-desk/11-simple-desk-universe1.png)

フェーダーを動かすと、そのチャンネルの値が Art-Net として送信されます。

たとえば、チャンネル 1、2、3 に値を入れると、受信側ではその値が確認できます。

## Universe番号の見え方に注意

ここで少しハマりポイントがありました。

QLC+ の画面上で `Universe 2` を選んで送ると、受信データ上では `Universe 1` として見えることがあります。

これは、QLC+ の UI 表示が人間向けに 1 始まりで表示されている一方で、Art-Net のデータ上の Universe 番号は 0 始まりとして扱われるためです。

つまり、ざっくり言うと次のように見えます。

| QLC+上の表示 | データ上のUniverse |
| --- | --- |
| Universe 1 | 0 |
| Universe 2 | 1 |
| Universe 3 | 2 |

受信側で `Universe 2 が来ない` と思ったら、まず `1` のほうを見てみるとよさそうです。

## パススルーっぽい設定について

入出力設定の Universe のところに、矢印アイコンのようなボタンがあります。

![Universeの矢印アイコン](/images/qlcplus-artnet-windows-simple-desk/17-passthrough-arrow.png)

手元で試した限りでは、このパススルーっぽい設定を有効にすると、意図した送信が止まっているように見えました。

今回の目的は「QLC+ のシンプル卓から Art-Net を送る」ことなので、この設定は触らず、再生マークの出力ボタンだけを有効にするのがよさそうです。

## Unity側で受け取る

ここから先は Unity 側の話になります。

自作の Art-Net Monitor で受信チェックをすると、QLC+ から送られているパケットを確認できます。

Unity 側では、メニューから `toshi > VLiveKit > Lighting > ArtNet Monitor` を開きました。

![UnityのメニューからArtNet Monitorを開く](/images/qlcplus-artnet-windows-simple-desk/12-unity-menu-artnet-monitor.png)

最初は `STOPPED` で、まだ受信していない状態です。

![ArtNet Monitorの停止状態](/images/qlcplus-artnet-windows-simple-desk/13-unity-artnet-monitor-stopped.png)

`Start Receiver` で受信を開始すると、QLC+ からの Art-Net パケットが見えるようになります。

![QLC+のシンプル卓とUnityのArtNet Monitorを並べた状態](/images/qlcplus-artnet-windows-simple-desk/14-unity-artnet-monitor-live.png)

受信中は `LIVE` 表示になり、パケット数、最終受信時刻、チャンネルのプレビューなどが更新されます。

![ArtNet Monitorで受信中の値を見る](/images/qlcplus-artnet-windows-simple-desk/16-unity-runtime-values.png)

確認が終わったら、`Stop Receiver` で受信を止めます。

![Stop Receiver後の状態](/images/qlcplus-artnet-windows-simple-desk/15-unity-artnet-monitor-stopped-result.png)

Unity 側には送信用のテストウィンドウもありますが、この記事では QLC+ から送るところまでにしておきます。

![Unity側のArtNet Send Testウィンドウ](/images/qlcplus-artnet-windows-simple-desk/18-unity-artnet-send-test.png)

このあたりの Unity 側の実装や確認方法については、また別の記事で書きます。

## ここまでの確認

ここまでで、QLC+ から Art-Net を送信する準備ができました。

流れをまとめると、次のようになります。

1. QLC+ を公式サイトからダウンロードする
2. インストーラーを実行してインストールする
3. `Q Light Controller Plus` を起動する
4. `入出力設定` で Art-Net の出力を有効にする
5. `シンプル卓` でユニバースを選ぶ
6. フェーダーを動かして DMX 値を送る
7. 必要なら Unity 側の Monitor で受信を確認する

## おわりに

QLC+ を使うと、実機の照明卓がなくても、PC上から Art-Net の送信テストができます。

Unity 側や自作ツール側の Art-Net 受信確認にも使えるので、ライティング系の開発をするときにかなり便利です。

---

最終更新: 2026-05-08

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
