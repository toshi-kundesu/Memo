---
title: "Unity の UPM パッケージを npm の Scoped Registry で配るメモ"
emoji: "📦"
type: "tech"
topics: ["unity", "upm", "npm", "githubactions", "zenn"]
published: false
---

Unity の自作 UPM パッケージを、Git URL ではなく **Scoped Registry** から入れられるようにしたメモです。

最終的にやりたかったことは、Unity Package Manager の `My Registries` に自分のパッケージが出て、別プロジェクトでは `manifest.json` にバージョンを書くだけで入れられる状態です。

```json
{
  "scopedRegistries": [
    {
      "name": "toshi",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.toshi"
      ]
    }
  ],
  "dependencies": {
    "com.toshi.vlivekit.testassetscontainer": "0.0.8"
  }
}
```

今回は `com.toshi.vlivekit.testassetscontainer` という Unity package を npm registry に publish しました。

## Git URL 配布と Scoped Registry 配布は別物

最初は GitHub Actions でタグを打ったら `git subtree split` して、`upm` ブランチを作る方式を考えていました。

これはこれで、Unity Package Manager の `Add package from git URL...` から入れられます。

```text
https://github.com/toshi-kundesu/VLiveKit_TestAssetsContainer.git#upm/v0.0.2
```

ただし、これは **Git URL 配布** です。
Unity の `My Registries` に並ぶ Scoped Registry 配布ではありません。

Scoped Registry にしたい場合は、Unity が読める npm 互換 registry に package を publish する必要があります。
Unity 公式の説明でも、Scoped Registry は npm 互換 registry の URL と、`com.example` のような scope を対応させる仕組みとして扱われています。

https://docs.unity3d.com/ja/current/Manual/upm-scoped.html

## package.json は Unity package として書く

配布対象の package root に `package.json` を置きます。
Unity package として重要なのは、`name` が reverse domain 形式になっていることです。

```json
{
  "name": "com.toshi.vlivekit.testassetscontainer",
  "version": "0.0.8",
  "displayName": "VLiveKit Test Assets Container",
  "description": "Container for temporary, test, and proxy assets used during virtual live production.",
  "unity": "2022.3",
  "author": {
    "name": "toshi"
  },
  "repository": {
    "type": "git",
    "url": "git+https://github.com/toshi-kundesu/VLiveKit_TestAssetsContainer.git"
  },
  "license": "Unlicense"
}
```

npm の `@scope/package` 形式ではなく、Unity では `com.toshi...` のような名前をそのまま package 名にします。

GitHub Packages の npm registry は `@owner/package` 形式との相性が強いので、Unity の `com.toshi...` をそのまま使うなら npmjs の `https://registry.npmjs.org` に publish するのが素直でした。

## GitHub Actions から npm publish する

npm 側で Access Token を作り、GitHub の repository secret に `NPM_TOKEN` として保存します。

npm token は GitHub Actions から publish するために使います。
2FA を有効にしている場合は、automation 用に使える token を作る必要があります。

workflow はタグ push をトリガーにします。

```yaml
name: Release UPM Package

on:
  push:
    tags:
      - "v*"

permissions:
  contents: write

env:
  PACKAGE_ROOT: Assets/toshi.VLiveKit/TestAssetsContainer
  PACKAGE_WORKDIR: /tmp/upm-package

jobs:
  release:
    name: Publish scoped registry package
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup Node.js for npm registry
        uses: actions/setup-node@v4
        with:
          node-version: 20
          registry-url: https://registry.npmjs.org

      - name: Resolve release version
        id: version
        shell: bash
        run: |
          tag="${GITHUB_REF_NAME}"
          version="${tag#v}"
          echo "tag=$tag" >> "$GITHUB_OUTPUT"
          echo "version=$version" >> "$GITHUB_OUTPUT"

      - name: Prepare package contents
        shell: bash
        run: |
          test -f "$PACKAGE_ROOT/package.json"

          rm -rf "$PACKAGE_WORKDIR"
          mkdir -p "$PACKAGE_WORKDIR"
          cp -a "$PACKAGE_ROOT"/. "$PACKAGE_WORKDIR"/

          node -e '
            const fs = require("fs");
            const path = process.env.PACKAGE_WORKDIR + "/package.json";
            const packageJson = JSON.parse(fs.readFileSync(path, "utf8"));
            packageJson.version = process.env.RELEASE_VERSION;
            fs.writeFileSync(path, JSON.stringify(packageJson, null, 2) + "\n");
          '
        env:
          RELEASE_VERSION: ${{ steps.version.outputs.version }}

      - name: Pack UPM tarball
        id: pack
        shell: bash
        working-directory: ${{ env.PACKAGE_WORKDIR }}
        run: |
          npm pack --json > /tmp/npm-pack.json
          tarball="$(node -e 'const fs = require("fs"); const pack = JSON.parse(fs.readFileSync("/tmp/npm-pack.json", "utf8")); console.log(pack[0].filename);')"
          echo "tarball_path=$PACKAGE_WORKDIR/$tarball" >> "$GITHUB_OUTPUT"

      - name: Publish to npm registry
        shell: bash
        run: |
          npm publish "${{ steps.pack.outputs.tarball_path }}" --access public
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

ポイントは、`npm publish` に directory ではなく `npm pack` でできた `.tgz` を渡すことです。

一度、`npm pack` で作った `.tgz` が同じ directory に残った状態で `npm publish` したら、その `.tgz` 自体まで package に含めようとして巨大化しました。
`npm publish path/to/package.tgz` にすると、その事故を避けられます。

## main から publish して、upm ブランチは作らない

途中で `git subtree split` を使って `upm` ブランチを作る方式も試しました。
Git URL 配布ではよくある構成です。

ただ、Scoped Registry がメインなら `upm` ブランチはなくても困りません。

`upm` や `upm-v0.0.6` のような配布用ブランチを作ると、Git graph が main から横に分かれて見えます。
壊れているわけではないのですが、自分の用途では少しノイズでした。

最終的には、main の中にある package root を `/tmp/upm-package` にコピーして、その一時 directory から `npm pack` / `npm publish` する形にしました。
これなら release のたびにブランチが増えません。

## 重い asset は .npmignore で外す

今回一番詰まったのは package size でした。

TestAssetsContainer には検証用の HDRI などの重い素材が入っていて、そのまま publish すると tarball がかなり大きくなりました。
実際、HDRI だけで 800MB 以上ありました。

Scoped Registry に載せる package としては重すぎるので、npm に publish するものからは外しました。

```text
HDRI/
HDRI.meta
*.tgz
```

これを package root の `.npmignore` に置きます。

```text
Assets/toshi.VLiveKit/TestAssetsContainer/.npmignore
```

この状態で `npm pack --dry-run` すると、package size は 20MB 程度まで下がりました。

重い素材は Git repo や GitHub Release、必要なら LFS で管理し、Scoped Registry には最低限の package と軽いサンプルだけを出すのが扱いやすそうです。

## Samples タブに出すには Samples~ と package.json

Unity Package Manager の Samples タブに出したい場合は、package root に `Samples~` を作り、`package.json` に `samples` を書きます。

Unity 公式マニュアルでも、サンプルは `Samples~` 以下に置き、`package.json` の `samples` 配列で指定する形になっています。

https://docs.unity3d.com/ja/current/Manual/cus-samples.html

```text
package.json
Runtime/
Editor/
Samples~/
  Tiny Sample/
    TinySample.unity
```

```json
{
  "samples": [
    {
      "displayName": "Tiny Sample",
      "description": "A small sample scene for checking package import behavior.",
      "path": "Samples~/Tiny Sample"
    }
  ]
}
```

ただし開発中は `Samples~` が Unity から見えにくいので、開発 repo では `Samples/` として置き、release workflow の中で `Samples~` にリネームする運用にしました。

```bash
if [[ -d "$PACKAGE_WORKDIR/Samples" ]]; then
  rm -rf "$PACKAGE_WORKDIR/Samples~"
  mv "$PACKAGE_WORKDIR/Samples" "$PACKAGE_WORKDIR/Samples~"
fi
```

これで開発中は普通の folder として編集でき、publish 後は Package Manager の Samples タブに出せます。

## README.md と LICENSE の .meta

package root に repository root の `README.md` や `LICENSE` をコピーすると、Unity が immutable package 内で `.meta` がないと警告を出しました。

```text
Asset Packages/com.toshi.../README.md has no meta file, but it's in an immutable folder.
```

なので workflow 側で `README.md.meta` と `LICENSE.meta` も生成しました。

```yaml
cat > "$PACKAGE_WORKDIR/README.md.meta" <<'EOF'
fileFormatVersion: 2
guid: 61f1b63d2b074d6ba8a0c026b03f36e0
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOF
```

このあたりは、package に入れるファイルを Unity asset として扱うなら `.meta` まで揃える、という話でした。

## 古いテスト package は unpublish した

昔作った `com.toshi.vlivekit-upmtest` が npm に残っていて、Unity Package Manager の `My Registries` に出てきました。

不要だったので、GitHub Actions 上で `NPM_TOKEN` を使って unpublish しました。

```bash
npm unpublish com.toshi.vlivekit-upmtest --force
```

テスト package は残しておくと My Registries の一覧に出続けるので、消せるうちに消すか、少なくとも deprecate しておくとよさそうです。

## 使う側の manifest.json

publish が成功したら、使う側の Unity project では `Packages/manifest.json` に scoped registry と dependency を書きます。

```json
{
  "scopedRegistries": [
    {
      "name": "toshi",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.toshi"
      ]
    }
  ],
  "dependencies": {
    "com.toshi.vlivekit.testassetscontainer": "0.0.8"
  }
}
```

Package Manager の `Packages: My Registries` に切り替えると、自分の package が表示されます。

## まとめ

Scoped Registry をメインにするなら、今回の自分の結論はこうです。

- Git URL 用の `upm` ブランチは必須ではない
- main の package root から一時 directory を作って `npm pack` / `npm publish` する
- `NPM_TOKEN` を GitHub Actions secret に入れる
- 重い素材は `.npmignore` で registry から外す
- sample は `Samples~` と `package.json` の `samples` で出す
- 開発中は `Samples/`、release 時に `Samples~` へ変換すると編集しやすい
- root に置く `README.md` / `LICENSE` も package に含めるなら `.meta` を用意する

ここまでやると、Unity Package Manager から普通の registry package として入れられるようになります。
Git URL で入れるより、複数プロジェクトでバージョンを揃えやすいのがかなり良かったです。
