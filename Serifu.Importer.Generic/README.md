> [!IMPORTANT]
> The "generic importer" is used for games that require prior extraction & conversion using external tools that aren't practical to run from or port to C#. Mostly, this refers to visual novels, but The Witcher 3's quotes also use this method: see [**README_WITCHER.md**](README_WITCHER.md) for details. **This repo does not contain any code for extracting or decrypting VN assets.**

# VN Importer Notes

- [Tools](#tools)
- [G-senjou no Maou](#g-senjou-no-maou)
  - [Parsing .ks files](#parsing-ks-files)
    - [G-senjou custom macros](#g-senjou-custom-macros)
  - [Matching the lines \& excluding H scenes](#matching-the-lines--excluding-h-scenes)
  - [Speaker names](#speaker-names)
- [Nekopara Vol. 1 \& 2, Senren Banka, Maitetsu](#nekopara-vol-1--2-senren-banka-maitetsu)
  - [.scn / PSB format](#scn--psb-format)
  - [Speaker names \& voice file](#speaker-names--voice-file)
  - [Formatting](#formatting)
    - [Regex to match %-formatting and furigana](#regex-to-match--formatting-and-furigana)
    - [Escaping](#escaping)
  - [Useful commands for analyzing scn JSON](#useful-commands-for-analyzing-scn-json)

## Tools

- [KrkrExtract](https://github.com/xmoezzz/KrkrExtract/releases) (v4) for extracting xp3 files (Kirikiri engine)
  - Run in Sandbox. Place exe & dll in game folder and drop the game exe onto krkr. May need to use Locale Emulator: "copy as path" the game exe, right click on krkrextract, and use a LE application profile to run it with the game exe in the arguments field.
- [KirikiriTools](https://github.com/arcusmaximus/KirikiriTools)'s KirikiriDescrambler.exe, for "scrambled" .ks files (Kirikiri engine)
- [FreeMoteToolkit](https://github.com/UlyssesWu/FreeMote)'s PsbDecompile.exe, to convert .scn files to a usable JSON format (details below).

Note: commands in this document make use of [fd](https://github.com/sharkdp/fd) and [fx](https://github.com/antonmedv/fx).

## G-senjou no Maou

_Kirikiri engine. Use KrkrExtract & KirikiriTools._

### Parsing .ks files

Labels in the form of `*name|` (or `*name|Display name`) at the start of a line serve as save points & jump targets. This means each dialogue line _should_ have a label, so any text following a label up until the next label is a single quote. The asterisk seems to always accompany the name wherever it's used (this convention seems to have been carried over into the .ks.scn format used in newer games, but those are used to label scenes rather than individual lines).

Dialogue text are lines that do not start with `*`, `@`, or `;`. (Leading tab characters are ignored, so that means `^\t*(?![\t*@;])(.+)`.)

Left brackets are escaped by doubling (`[[`).

Need to check if any dialogue lines can have multiple lines of text.

Kirikiri/KAG docs: https://kirikirikag.sourceforge.net/contents/ (re. the two names: "kirikiri base system interprets TJS scripting language, and KAG, which is written in TJS, interprets scenario file"; people tend to just call it Kirikiri though)

#### G-senjou custom macros

Verbal lines start with e.g. `[nm t="水羽" s=miz_20316]` where `t` is the speaker name (always Japanese, see below) and `s` is an optional voice file name. It would appear that quotes are optional, like HTML; however closing tags don't appear to be a thing (rather than "tags", they're more like function calls; `@foo` and `[foo]` are equivalent, so we should check if there are any `nm`'s in the @-command form).

`[np]` is an extended version of `[p]` (new page). We can use this as a dialogue line separator in case the labels aren't sufficient.


### Matching the lines & excluding H scenes

The English version's files in data/scenario are censored: mostly, this means removing the lines that call the h scenes, but there are a couple places in the JP & patched English versions where the h scene spills over into the main scenario files, so it's not enough to just exclude the h files. However we can easily exclude all H scenes by using the censored English version (i.e. don't include patch2) and skipping lines whose labels don't exist in both. (There are a couple spots where the patch changed existing lines in the regular file, but not in any meaningful way, e.g. a space after an ellipsis, so we don't need to worry about the JP versions not matching the EN versions for the lines that have matching labels.)

Need to confirm if there are any other lines that don't match though (e.g. a JP line was split into two EN lines, or the EN line translates two JP lines). In those cases, we'll need to configure explicit line mappings like:

```json
{
  "LineMappings": {
    "file.ks": [
      {
        "English": ["*p100a", "*p100b"],
        "Japanese": ["*page100"]
      }
    ]
  }
}
```

...and then any remaining lines get matched by name.
  
Note: Japanese version uses `*pageXX`, English uses `*pXX`. Ignore those differences when matching lines.

### Speaker names

G-senjou's English version has two files containing name translations:

- #EnglishNameList.tjs
- config_nameList.tjs

The `nm` macro uses the former to translate the name. The Japanese version displays the `t` param as-is.

The latter has the full names for a limited number of characters. We should probably show full names including for Japanese, so we'll want to configure name mappings of our own for both languages. Grep for all t's to make sure we don't miss any. Maybe see if any in #EnglishNameList have full names we can use besides the ones in config_nameList.

## Nekopara Vol. 1 & 2, Senren Banka, Maitetsu

_Kirikiri engine. Use KrkrExtract & FreeMoteToolkit._

### .scn / PSB format

These games also use the Kirikiri engine, but their scenario text is in .scn files. This is not a Kirikiri format, but appears to be a proprietary format called "Packaged Struct Binary" (PSB) and developed by M2 Co., Ltd which made the E-mote system that Nekopara uses. For some reason, the PSB format is used not only as an export format for E-Mote character/motion data, but also for potentially any VN assets, it seems, with different file extensions and internal structures [for different purposes](https://github.com/UlyssesWu/FreeMote/wiki/PSB-Shells,-Types,-Platforms#types), despite most of that not having anything to do with E-mote. I can't find any official information on why the E-mote company developed a format to replace .ks files, or how it even relates to the .ks format -- the .scn files containing the text all have the extension .ks.scn but the open source FreeMote toolkit decompiles them not to Kirikiri/KAG scenario files but to JSON (also the markup is different, so I'm guessing the ".ks" in the filenames is a throwback; Maitetsu's are all ".txt.scn" instead). And interestingly, the JSON includes multiple languages, something .ks doesn't support. That might be why other games like Senren Banka also use the PSB format for their scenario text despite not using E-mote. There's a psbfile.dll plugin which I _assume_ is responsible for reading and somehow integrating them into Kirikiri... although as I said I can't find any documentation on M2's site. A search for "site:mtwo.co.jp scn" turns up nothing, and "psb" only turns up the E-mote export option and some forum posts. What little information exists, even the "Packaged Struct Binary" name, comes from FreeMote and a few other tools, which I assume reverse-engineered the format. My guess is M2 worked directly with studios to develop it.

### Speaker names & voice file

`.scenes[].texts[][0]` appears to be the default speaker name (null for the narrator / inner thoughts), `.scenes[].texts[][1]` optionally replaces it with a different name for story purposes (the "Box" below, "???", "Raillord" in Maitetsu's prologue when it's actually Hachiroku but we don't know that yet, etc.), and `.scenes[].texts[][2][][0]` overrides that with the translated name.

The voice file is referenced in `[3]`, which may be null.

```json
[
  "ショコラ",
  "ダンボール",
  [
    [
      "ダンボール",
      "「にゃ、にゃ～お……？」"
    ],
    [
      "Box",
      "%fSourceHanSansCN-M;「%f;M-Me~ow...?%fSourceHanSansCN-M;」"
    ]
  ],
  [
    {
      "name": "ショコラ",
      "pan": 0,
      "type": 0,
      "voice": "syo_0003"
    }
  ]
]
```

Note that there could be _multiple_ voice files, when multiple people are speaking over each other. In this example the "default speaker name" is also not representative of who's speaking:

```json
[
  "ショコラ",
  "一同",
  [
    [
      "一同",
      "『「おーっ♪」』"
    ],
    [
      "All",
      "%fSourceHanSansCN-M;「%f;Yeah~ %fSourceHanSansCN-M;♪%fSourceHanSansCN-M;」"
    ]
  ],
  [
    {
      "name": "バニラ",
      "pan": 0,
      "type": 0,
      "voice": "bani_0964"
    },
    {
      "name": "アズキ",
      "pan": 0,
      "type": 0,
      "voice": "azu_0033"
    },
    {
      "name": "メイプル",
      "pan": 0,
      "type": 0,
      "voice": "mei_0035"
    },
    {
      "name": "ココナツ",
      "pan": 0,
      "type": 0,
      "voice": "koko_0039"
    },
    {
      "name": "シナモン",
      "pan": 0,
      "type": 0,
      "voice": "shina_0038"
    },
    {
      "name": "ショコラ",
      "pan": 0,
      "type": 0,
      "voice": "syo_1066"
    }
  ]
]
```

Maybe we should follow the same logic as the Skyrim importer and only use one speaker / voice file for a given line...? Although there are also lines where _each person's line_ is shown in the text:

```json
[
  "レナ・小春・廉太郎",
  null,
  [
    [
      null,
      "「ムラサメちゃんが人に戻ったでありますか？！」\\n「ムラサメ様が人に戻ったの？！」\\n「ムラサメ様が人に戻った？！」",
      "「ムラサメちゃんが人に戻ったでありますか？！」「ムラサメ様が人に戻ったの？！」「ムラサメ様が人に戻った？！」",
      "「ムラサメちゃんが人に戻ったでありますか？！」「ムラサメ様が人に戻ったの？！」「ムラサメ様が人に戻った？！」"
    ],
    [
      "Lena, Koharu, Rentarou",
      "Murasame-chan has returned to being human!?\\nMurasame-sama's human again!?\\nMurasame-sama's human again!?",
      "Murasame-chan has returned to being human!?Murasame-sama's human again!?Murasame-sama's human again!?",
      "Murasame-chan has returned to being human!?Murasame-sama's human again!?Murasame-sama's human again!?"
    ]
  ],
  [
    {
      "name": "小春",
      "pan": 0,
      "type": 0,
      "voice": "koh308_001"
    },
    {
      "name": "廉太郎",
      "pan": 0,
      "type": 0,
      "voice": "ren308_001"
    },
    {
      "name": "レナ",
      "pan": 0,
      "type": 0,
      "voice": "len308_001"
    }
  ]
]
```

Probably consider either:

1. Skip any lines with multiple voice files (are any of them substantial, anyway?)
   - It doesn't look like it, no: `fx All\ text\ arrays.json 'x => x.filter(x => x[3] != null && x[3].length > 1)' '.[][2][][1]' 'map(x => x.replace(/(?<!\\)(%(l[^;]*;)?[^;]*;|\[[^\]]*\])/g, ""))' | fx`
2. When there's multiple voice files, use the name _as it would be displayed_ OR concatenate the names used in the voice file objects, mapped to full names as usual (this might get long though), and then use ffmpeg to overlay the voices.

Command to list the ones with multiple voice files:

`fx All\ text\ arrays.json 'x => x.filter(x => x[3] != null && x[3].length > 1)' | fx`

For lines with a single speaker, at least, the name in `[0]` is always the same as the name on the sound file:

`fx All\ text\ arrays.json 'x => x.filter(x => x[3]?.length == 1 && x[3][0].name != x[0])'`

### Formatting

Aside from the `%f` for font, seen above, Maitetsu links words in the glossary using the syntax `%l<key>;#<color>;`, with empty key & color acting as an apparent reset. **This means %-tags can take multiple parameters.**

There's also a bracket syntax for furigana. `[バン]板` becomes <ruby>板<rt>バン</rt></ruby>, `[イビ,1]伊日川` becomes <ruby>伊日<rt>イビ</rt></ruby>川, and so on. In both cases, the marked-up line is followed by two additional strings without the markup. The last one looks to be the one we want:

```jsonc
[
  "ポーレット",
  null,
  [
    [
      null,
      "「[ハチロクニイマル,3]8620に[ゴーハチロクニイサン,4]58623の%l台枠;#00ffc040;台枠%l;#;が移植出来ない場合にも――\\nそれなら、%l台枠;#00ffc040;台枠%l;#;一つの往復輸送費だけで済みますから」",
      "「ハチロクニイマルにゴーハチロクニイサンの台枠が移植出来ない場合にも――それなら、台枠一つの往復輸送費だけで済みますから」",
      "「8620に58623の台枠が移植出来ない場合にも――それなら、台枠一つの往復輸送費だけで済みますから」"
    ],
    [
      "Paulette",
      "\"If 58623's %l台枠;#00ffc040;frame%l;#; cannot be transferred to 8620... we only need to pay round-trip shipping for one frame.\"",
      "\"If 58623's frame cannot be transferred to 8620... we only need to pay round-trip shipping for one frame.\"",
      "\"If 58623's frame cannot be transferred to 8620... we only need to pay round-trip shipping for one frame.\""
    ]
  ],
  // ...
]
```

Another example, showing both %l and furigana as well as overridden display name (from the prologue flashback):

```jsonc
[
  "ハチロク",
  "レイルロオド",
  [
    [
      "レイルロオド",
      "「%l粘着力;#00ffc040;粘着力%l;#;が著しく低下しています。\\n[すなま,1]砂撒きを進言します」",
      "「粘着力が著しく低下しています。すなまきを進言します」",
      "「粘着力が著しく低下しています。砂撒きを進言します」"
    ],
    [
      "Raillord",
      "\"The %l粘着力;#00ffc040;adhesion%l;#;'s been significantly reduced. I would strongly advise scattering the sand.\"",
      "\"The adhesion's been significantly reduced. I would strongly advise scattering the sand.\"",
      "\"The adhesion's been significantly reduced. I would strongly advise scattering the sand.\""
    ]
  ],
  // ...
]
```

The "plain text" versions are present whenever there's a newline, too, but it appears that just because one language has the additional strings doesn't mean the other language will too, if it doesn't have any markup or newlines:

```json
[
  [
    null,
    "最悪に備えることもまた、\\n経営者の資質であり、責務だろうと理解する。",
    "最悪に備えることもまた、経営者の資質であり、責務だろうと理解する。",
    "最悪に備えることもまた、経営者の資質であり、責務だろうと理解する。"
  ],
  [
    null,
    "I realize that preparing for the worst-case scenario is the nature and responsibility of a business owner. "
  ]
]
```

These were seen in Senren, too, so the extra "plain text" versions isn't unique to Maitetsu. But since Nekopara doesn't have them, what we could do is take the third string if present, and otherwise strip the formatting from the first string.

However, we may want to strip formatting ourselves anyway, since it removes newlines without adding a space in English text:

```json
[
  "Lena, Koharu, Rentarou",
  "Murasame-chan has returned to being human!?\\nMurasame-sama's human again!?\\nMurasame-sama's human again!?",
  "Murasame-chan has returned to being human!?Murasame-sama's human again!?Murasame-sama's human again!?",
  "Murasame-chan has returned to being human!?Murasame-sama's human again!?Murasame-sama's human again!?"
]
```

There are also a lot of numbered percent formats, like `%123;`. Unconfirmed, but these are probably text styles. It can also be `%;`, which seems to be a reset: `%84;……ぶつぶつ……%;`.

The last formatting type seen in these games is `%D`. No idea what it does, but it takes one argument (ends with the first semicolon):

```json
[
  [
    null,
    "ポトッ。"
  ],
  [
    null,
    "%D$vl1;plop"
  ]
]
```

Maitetsu's `%l` is the only one seen so far that takes multiple arguments.

#### Regex to match %-formatting and furigana

```regexp
(?<!\\) # Not preceeded by a backslash escape
(
    # Percent formatting (%l takes an extra argument)
    %(l[^;]*;)?[^;]*; |
    # Furigana
    \[[^\]]*\]
)
```

[Test](https://regex101.com/r/y3Tchn/1) | Use RegexOptions.IgnorePatternWhitespace

We'll want to check for unknown %-formattings since they may take a different number of arguments like %l: `(?<!\\)%[^\d;dfl]`.

#### Escaping

Percent signs are escaped with a backslash, which is removed in the plain text version (unknown how literal brackets would be escaped, perhaps also a backslash):

```json
[
  null,
  "%lカマ;#00ffc040;カマ%l;#;は良好、ボイラー水位が80\\%強。、\\n蒸気は機関に行き渡り、いつでも発車できる状態だ。",
  "カマは良好、ボイラー水位が80%強。、蒸気は機関に行き渡り、いつでも発車できる状態だ。",
  "カマは良好、ボイラー水位が80%強。、蒸気は機関に行き渡り、いつでも発車できる状態だ。"
]
```

Newlines are also escaped (the JSON contains a literal backslash and `n`).

All of the character escapes seen so far:

```
❯ jq -r .[] All\ lines\ with\ formatting.json | grep -Po '\\.' | sort -u
\%
\&
\n
```

Ampersand being escaped implies that perhaps HTML entities are supported, but there are no unescaped ampersands in the current data set to verify this. Should log a warning if we encounter one. Also log a warning (or throw) if we see an unknown escape (need to confirm if it should print a literal or something else; shouldn't assume it uses standard C escapes).

### Useful commands for analyzing scn JSON

Extract all text from a scn:

`fx 'Maitetsu scene files/共通07_鍵.txt.json' 'x => x.scenes?.flatMap(x => x.texts?.flatMap(x => x[2].flatMap(x => x.slice(1))) ?? []) ?? []'`

Only the last string in each line's first two languages (assuming Japanese, English):

`fx Nekopara\ 1\ scene\ files/01_01.ks.json 'x => x.scenes?.flatMap(x => x.texts?.flatMap(x => x[2].slice(0, 2).flatMap(x => x[x.length - 1])) ?? []) ?? []'`

From all scn files:

`fd '\.json$' *scene\ files* -x fx {} 'x => x.scenes?.flatMap(x => x.texts?.flatMap(x => x[2].slice(0, 2).flatMap(x => x[x.length - 1])) ?? []) ?? []' | jq -s add`

Extracting all of the text arrays for all scn files, limiting to the first four elements, and only the first two languages of the third element:

`fd '\.json$' *scene\ files* -x fx {} 'x => x.scenes?.flatMap(x => x.texts?.map(x => [x[0],x[1],x[2].slice(0,2),x[3]]) ?? []) ?? []' | jq -s add > All\ text\ arrays.json`

Grepping all of the used % formattings (with "from all scn files" modified to use `x[1]` instead of `x[x.length - 1]`):

`jq -r .[] All\ lines\ with\ formatting.json | grep -Po '(?<!\\)%[^;]*' | sort -u`
