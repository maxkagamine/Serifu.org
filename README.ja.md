<h1>
  <a href="README.md"><img src=".github/images/nav-english.svg" height="45" alt="English" align="right" /></a>
  <a href="https://serifu.org"><img src=".github/images/serifu-light-dark.svg" height="45" alt="Serifu.org" /></a>
</h1>

Serifu.orgは、興味のあるものなら言語を学ぶのがより早いという考えに基づくオープンソースの語学学習ツールです。30万以上のゲームのセリフを検索して、日本語の単語や表現が英語でどのように翻訳されているか、またはその逆を調べることができます。それに機械学習 (AI) による単語アラインメントを使って、検索した言葉に対応する翻訳部分がハイライトされます。[Tatoeba](https://tatoeba.org/ja)と[Reverso Context](https://context.reverso.net/%E7%BF%BB%E8%A8%B3/)に触発されました。

私は1ヶ月間ほぼ毎日ゲーセンに行って艦これをプレイしてから、「[sun](https://serifu.org/%E7%BF%BB%E8%A8%B3/concentrated%20power%20of%20the%20sun)」を日本語で何と言うか知らないのに「[魚雷](https://serifu.org/%E7%BF%BB%E8%A8%B3/%E9%AD%9A%E9%9B%B7)」とか「[軽空母](https://serifu.org/%E7%BF%BB%E8%A8%B3/%E8%BB%BD%E7%A9%BA%E6%AF%8D)」とかを覚えたことに気づいたら、このプロジェクトを始めました。どうやら瑞鳳がどの教科書よりも良い先生です！

## スクリーンショット

<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/home-ja_desktop@1.5x.avif"><img src=".github/images/screenshots/home-ja_desktop@1.5x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/about-ja_desktop-full@1.5x.avif"><img src=".github/images/screenshots/about-ja_desktop-full@1.5x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/results-zuihou-ja_desktop@1.5x.avif"><img src=".github/images/screenshots/results-zuihou-ja_desktop@1.5x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/results-haru_mobile@2x.avif"><img src=".github/images/screenshots/results-haru_mobile@2x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/error-500-ja_desktop@1.5x.avif"><img src=".github/images/screenshots/error-500-ja_desktop@1.5x_thumb.avif" height="180" /></a>

## アーキテクチャ図

[![](.github/images/architecture-diagram-japanese.svg)](https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/architecture-diagram-japanese.svg)

## インポーター

- [**Serifu.Importer.Skyrim**](./Serifu.Importer.Skyrim) — コードベースの約25%になります。セリフをNPCに関連付け、引用を正しく帰属させるために[どれほどの努力](./Serifu.Importer.Skyrim/README.md)が必要だったかを考えると、ショーの主役とも言えます（これが[意外](./Serifu.Importer.Skyrim/SkyrimImporter.cs)と[難しい](./Serifu.Importer.Skyrim/Resolvers/ConditionsResolver.cs)です）

- [**Serifu.Importer.Generic**](./Serifu.Importer.Generic) — ファイル形式の解析を中心としたインポーターの迅速な開発を可能にするモジュラーなフレームワークです。[吉里吉里とCatSystem2ベースのノベルゲーム](./Serifu.Importer.Generic/README.md)に加えて[ウィッチャー3 ワイルドハント](./Serifu.Importer.Generic/README_WITCHER.md)と[バルダーズ・ゲート3](./Serifu.Importer.Generic/README_BG3.md)からのセリフを追加します（それぞれのメモにリンクします）

- [**Serifu.Importer.Kancolle**](./Serifu.Importer.Kancolle) — [ウィキ](https://en.kancollewiki.net/Kancolle_Wiki)からセリフを抽出します。ささやかながら、検証チェックやデータを後でクエリできることのお陰で、私はいくつかの修正をウィキにお返しできました。

## 機械学習

- [**Serifu.ML**](./Serifu.ML) — 以下に示す「対応する言葉のハイライト」機能は単語アラインメントを使用して達成されて、それについて[ここで読んだり自分で試してみることができます](https://github.com/maxkagamine/word-alignment-demo/blob/master/README.ja.md)：

  [![](.github/images/ryuujou-japanese.avif)](https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/ryuujou-japanese.avif)

  結果を表示する時、ハイライトされた検索語が事前生成されたアラインメントに対して比較されます。アラインメントの検索言語側がハイライトと交差する場合、その翻訳言語側もハイライトされます：

  [![](.github/images/ryuujou-alignment-light-dark.svg)](https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/ryuujou-alignment-light-dark.svg)

  _`docker run -it --rm -p 5000:5000 kagamine/word-alignment-demo`を実行してインタラクティブなビジュアライザーを起動し（Docker無しの指示は上のリンクにある）その後[ここにクリックするとこのセリフが読み込まれます](http://localhost:5000/#from=I%27m+the+light+carrier%2C+Ryuujou.+Don%27t+I+have+a+unique+silhouette%3F+But+I%27m+a+proper+carrier+that+can+launch+planes+in+succession.+Look+forward+to+it%21&to=%E8%BB%BD%E7%A9%BA%E6%AF%8D%E3%80%81%E9%BE%8D%E9%A9%A4%E3%82%84%E3%80%82%E7%8B%AC%E7%89%B9%E3%81%AA%E3%82%B7%E3%83%AB%E3%82%A8%E3%83%83%E3%83%88%E3%81%A7%E3%81%97%E3%82%87%EF%BC%9F%E3%81%A7%E3%82%82%E3%80%81%E8%89%A6%E8%BC%89%E6%A9%9F%E3%82%92%E6%AC%A1%E3%80%85%E7%B9%B0%E3%82%8A%E5%87%BA%E3%81%99%E3%80%81%E3%81%A1%E3%82%83%E3%83%BC%E3%82%93%E3%81%A8%E3%81%97%E3%81%9F%E7%A9%BA%E6%AF%8D%E3%81%AA%E3%82%93%E3%82%84%E3%80%82%E6%9C%9F%E5%BE%85%E3%81%97%E3%81%A6%E3%82%84%EF%BC%81&result=CAANAAAAAQAOABUAAQADABcAHgAEAAYAIAAlAAoAEwAmACwAEAATAC8ANQAIABMANgBAAAoAEwBCAEUAFAAWAEYASQArAC4ATABSACIAKQBTAFoAKQArAGAAYwAdACEAZABxABcAGgByAH8AGwAhAHUAfwAiACkAgQCNAC8ANAA%3D&wspThreshold=0.1&wspSymmetric=true&wspSymmetricMode=OR&awesomeModel=bert-base-multilingual-cased&dark=true&palette=material)。_

  Serifu.MLライブラリはその概念実証のC#の実装で、Python.NETを使用します。CPythonランタイムと直接相互運用して、TransformersとPyTorchのインプロセス実行が可能になってなかなか面白いと思いました。（それにおそらく私が今まで見た中で`dynamic`キーワードの唯一の正当な使い方です。）

## フロントエンド

- [**Serifu.Web**](./Serifu.Web) — ASP.NET Core、Razor、TypeScript、Viteで作られました。近年は普通にReactやAngularのプロジェクトに働きますが、今回はシンプルにするために、あえて伝統的なMVCにしました。それでも、ここで最新のウェブプラットフォームの機能がいくつか採用されています。重要な点は：
  - JS無しでHTML[ポップオーバー](https://developer.mozilla.org/ja/docs/Web/API/Popover_API)と[アンカー位置指定](https://developer.mozilla.org/ja/docs/Web/CSS/CSS_anchor_positioning/Using)を使って実装された注釈ポップアップやリンクメニュー
  - 実際普通のウェブサイトのように作られたのに、SPAのように感じさせるために[ドキュメント間のビュー遷移](https://developer.chrome.com/docs/web-platform/view-transitions/cross-document?hl=ja)を使用すること
  - レスポンシブなモバイル用レイアウトのためにCSSグリッドの使用
  - リンちゃんをフレーム内でヘッダーの下に収めるように、（二次元）線形補間の使う細心の背景位置の調整（そのヘッダーも、固定ブレークポイントと違ってスムーズに補間されます）
  - それにかっこいい検索ボックスのローディングアニメーション：

  <p align="center">
    <a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/popovers.avif"><img src=".github/images/popovers.avif" height="295" /></a>
    <a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/view-transitions.avif"><img src=".github/images/view-transitions.avif" height="295" /></a>
    <a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/search-box-loading-animation.avif"><img src=".github/images/search-box-loading-animation.avif" width="791" /></a>
    <br />
    <sup>
      1. 純粋なHTMLとCSSのポップオーバー、ここにJavaScriptがないよ！<br />
      2. CSSビュー遷移によってシームレスに隠されているフルページナビゲーション<br />
      3. @property型CSS変数をキーフレームしてSVGのstroke-dasharrayを制御することで実現した（ほぼ）純粋なCSSローディングアニメーション
    </sup>
  </p>

## ライセンス

Copyright © 鏡音マックス

このリポにあるプログラムはフリーソフトウェアです。あなたはこれを、フリーソフトウェア財団によって発行されたGNU Affero General Public Licenseのバージョン3が定める条件の下で再頒布または改変することができます。

そのプログラムは有用であることを願って頒布されますが、全くの無保証です。商業可能性の保証や特定目的への適合性は、言外に示されたものも含め、全く存在しません。詳しくは[GNU Affero General Public License](LICENSE.txt)をご覧ください。
