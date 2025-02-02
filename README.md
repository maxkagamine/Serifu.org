<h1>
  <a href="README.ja.md"><img src=".github/images/nav-japanese.svg" height="45" alt="日本語" align="right" /></a>
  <a href="https://serifu.org"><img src=".github/images/serifu-light-dark.svg" height="45" alt="Serifu.org" /></a>
</h1>

Serifu.org is an open source language learning tool based on the idea that it’s easier to learn a language through something you’re interested in. Search over 300,000 quotes from video games to see how an English word or idiom appears in its Japanese translation or vice versa, using machine learning to highlight the relevant part of the translation. Inspired by [Tatoeba](https://tatoeba.org/en) and [Reverso Context](https://context.reverso.net/translation/).

I started this project after realizing that, after going to the arcade nearly every day for a month to play Kancolle, I’d learned words like [<ruby>魚雷<rp>（</rp><rt>ぎょらい</rt><rp>）</rp></ruby>](https://serifu.org/translate/%E9%AD%9A%E9%9B%B7) and [<ruby>軽空母<rp>（</rp><rt>けいくうぼ</rt><rp>）</rp></ruby>](https://serifu.org/translate/%E8%BB%BD%E7%A9%BA%E6%AF%8D) before I even knew the word for “[sun](https://serifu.org/translate/concentrated%20power%20of%20the%20sun).” Turns out, Zuihou is a better language teacher than any textbook!

## Screenshots

<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/home-en_desktop@1.5x.avif"><img src=".github/images/screenshots/home-en_desktop@1.5x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/about-en_desktop-full@1.5x.avif"><img src=".github/images/screenshots/about-en_desktop-full@1.5x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/results-zuihou-en_desktop@1.5x.avif"><img src=".github/images/screenshots/results-zuihou-en_desktop@1.5x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/results-haru_mobile@2x.avif"><img src=".github/images/screenshots/results-haru_mobile@2x_thumb.avif" height="180" /></a>
<a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/screenshots/error-500-en_desktop@1.5x.avif"><img src=".github/images/screenshots/error-500-en_desktop@1.5x_thumb.avif" height="180" /></a>

## Architecture diagram

[![](.github/images/architecture-diagram.svg)](https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/architecture-diagram.svg)

## Importers

- [**Serifu.Importer.Skyrim**](./Serifu.Importer.Skyrim) — Accounts for about 25% of the codebase. One could argue it's the star of the show considering [how much effort](./Serifu.Importer.Skyrim/README.md) went into figuring out a way to associate dialogue with NPCs in order to properly attribute quotes (it's [surprisingly](./Serifu.Importer.Skyrim/SkyrimImporter.cs) [complicated](./Serifu.Importer.Skyrim/Resolvers/ConditionsResolver.cs)).

- [**Serifu.Importer.Generic**](./Serifu.Importer.Generic) — A modular framework that allows for rapid development of importers centered on parsing file formats. Adds quotes from [_Kirikiri_ and _CatSystem2_-based visual novels](./Serifu.Importer.Generic/README.md) as well as [_The Witcher 3: Wild Hunt_](./Serifu.Importer.Generic/README_WITCHER.md) and [_Baldur's Gate 3_](./Serifu.Importer.Generic/README_BG3.md) (links to their respective notes).

- [**Serifu.Importer.Kancolle**](./Serifu.Importer.Kancolle) — Extracts quotes from the [wiki](https://en.kancollewiki.net/Kancolle_Wiki). I've been able to contribute a number of fixes back to the wiki thanks to its validation checks and the ability to query the data afterwards.

## Machine learning

- [**Serifu.ML**](./Serifu.ML) — The “matching word highlighting” shown below is accomplished using word alignment [which you can read about and try for yourself here](https://github.com/maxkagamine/word-alignment-demo#readme):

  [![](.github/images/ryuujou-english.avif)](https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/ryuujou-english.avif)

  When displaying results, the highlighted search terms are compared against precomputed alignments. If an alignment's search-language-side intersects with the highlight, its translation-language-side is also highlighted:

  [![](.github/images/ryuujou-alignment-light-dark.svg)](https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/ryuujou-alignment-light-dark.svg)

  _Run `docker run -it --rm -p 5000:5000 kagamine/word-alignment-demo` to fire up the interactive visualizer (non-docker instructions in the above link) then [click here to load in this quote](http://localhost:5000/#from=I%27m+the+light+carrier%2C+Ryuujou.+Don%27t+I+have+a+unique+silhouette%3F+But+I%27m+a+proper+carrier+that+can+launch+planes+in+succession.+Look+forward+to+it%21&to=%E8%BB%BD%E7%A9%BA%E6%AF%8D%E3%80%81%E9%BE%8D%E9%A9%A4%E3%82%84%E3%80%82%E7%8B%AC%E7%89%B9%E3%81%AA%E3%82%B7%E3%83%AB%E3%82%A8%E3%83%83%E3%83%88%E3%81%A7%E3%81%97%E3%82%87%EF%BC%9F%E3%81%A7%E3%82%82%E3%80%81%E8%89%A6%E8%BC%89%E6%A9%9F%E3%82%92%E6%AC%A1%E3%80%85%E7%B9%B0%E3%82%8A%E5%87%BA%E3%81%99%E3%80%81%E3%81%A1%E3%82%83%E3%83%BC%E3%82%93%E3%81%A8%E3%81%97%E3%81%9F%E7%A9%BA%E6%AF%8D%E3%81%AA%E3%82%93%E3%82%84%E3%80%82%E6%9C%9F%E5%BE%85%E3%81%97%E3%81%A6%E3%82%84%EF%BC%81&result=CAANAAAAAQAOABUAAQADABcAHgAEAAYAIAAlAAoAEwAmACwAEAATAC8ANQAIABMANgBAAAoAEwBCAEUAFAAWAEYASQArAC4ATABSACIAKQBTAFoAKQArAGAAYwAdACEAZABxABcAGgByAH8AGwAhAHUAfwAiACkAgQCNAC8ANAA%3D&wspThreshold=0.1&wspSymmetric=true&wspSymmetricMode=OR&awesomeModel=bert-base-multilingual-cased&dark=true&palette=material)._

  The Serifu.ML library is a C# implementation of that proof of concept and uses Python.NET, which I found rather interesting as it allows for executing Transformers/PyTorch in-process by directly interoping with the CPython runtime (also quite possibly the only _appropriate_ use of the `dynamic` keyword I've encountered).

## Frontend

- [**Serifu.Web**](./Serifu.Web) — Built using ASP.NET Core, Razor, TypeScript, and Vite. While I tend to work on React and Angular projects these days, I deliberately went old-school with MVC for this one in the interest of keeping it simple. That said, there are a number of modern web platform features in use here. Key points include:
  - Notes popup and link menu implemented with zero JS using HTML [popovers](https://developer.mozilla.org/en-US/docs/Web/API/Popover_API) and [anchor positioning](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_anchor_positioning/Using);
  - [Cross-document view transitions](https://developer.chrome.com/docs/web-platform/view-transitions/cross-document) to make the app feel like a SPA despite being built like a regular website;
  - Use of CSS grids for an adaptive mobile layout;
  - Meticulous background image positioning using (bi)linear interpolation to keep Rin in frame and below the header (which also smoothly interpolates, as opposed to fixed breakpoints);
  - And a slick search box loading animation:

  <p align="center">
    <a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/popovers.avif"><img src=".github/images/popovers.avif" height="295" /></a>
    <a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/view-transitions.avif"><img src=".github/images/view-transitions.avif" height="295" /></a>
    <a href="https://github.com/maxkagamine/Serifu.org/raw/refs/heads/master/.github/images/search-box-loading-animation.avif"><img src=".github/images/search-box-loading-animation.avif" width="791" /></a>
    <br />
    <sup>
      1. Pure HTML & CSS popovers, no JavaScript here!<br />
      2. Full page navigation being seamlessly masked by CSS view transitions.<br />
      3. An (almost) pure CSS loading animation accomplished by keyframing @property-typed CSS variables controlling an SVG stroke-dasharray.
    </sup>
  </p>

## License

Copyright © Max Kagamine

The programs in this repo are free software: you can redistribute and/or modify them under the terms of version 3 of the GNU Affero General Public License as published by the Free Software Foundation.

These programs are distributed in the hope that they will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the [GNU Affero General Public License](LICENSE.txt) for more details.
