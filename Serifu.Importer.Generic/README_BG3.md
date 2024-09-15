# Baldur's Gate 3 Importer Notes

1. Switch game language to Japanese (in GOG Galaxy)
2. Use [LSLib](https://github.com/Norbyte/lslib/releases) to unpack the following (all into the same directory):
   - Localization/Japanese/Japanese.pak
   - Localization/English.pak
     - These contain the dialogue text, located in Localization/{English,Japanese}/\*.loca
   - Localization/Voice.pak
     - Contains the (English-only) voice files, located in Mods/Gustav/Localization/English/Soundbanks/\*.wem
   - Localization/VoiceMeta.pak
     - The meta files map audio filenames to dialogue IDs, located in Localization/English/Soundbanks/\*.lsf and Mods/Gustav/Localization/English/Soundbanks/\*.lsf
   - Shared.pak
   - Gustav.pak
     - These contain most of the game data, which we need to identify speaker names. We'll delete the irrelevant files later.
   - Patch*.pak (extract last if present)
3. Use the Localization tab to convert english.loca & japanese.loca to XML
   - The gender variants seem to be all player choices, not voiced dialogue. We can ignore them.
4. Use the LSX/etc. tab to batch convert all LSX, LSB, and LSF files to LSJ
   - Hold Escape to dismiss all the errors — _not_ Enter, else you'll hit the Convert button again since it doesn't disable itself while conversion is in progress...
5. Use [vgmstream-cli](https://github.com/vgmstream/vgmstream/releases) to convert the .wem files to a usable format (see [README_WITCHER.md](README_WITCHER.md#extractsh), same deal)

## Identifying quotes & speaker names

1. Iterate through all .lsj files
   - For those that that contain `.save.regions.Templates.GameObjects` or `.save.regions.Origins.Origin`, index `MapKey` (GameObjects) | `GlobalTemplate` (Origin) => { `DisplayName`, `TemplateName`, `Name` }.
     - These are always guids.
     - There's also a `ParentTemplateId` in RootTemplates
   - ~~For those that contain `.save.regions.dialog`, index `.save.regions.dialog.nodes[].node[].TaggedTexts[].TaggedText[].TagTexts[].TagText[].TagText.handle` => `.save.regions.dialog.speakerlist[].speaker[].list.value` with index corresponding to `.save.regions.dialog.nodes[].node[].speaker.value` (falling back to `.save.regions.dialog.DefaultSpeakerIndex`), splitting the speaker list by semicolon.~~
     - ~~Speaker index may not have a match... seems like the narrator is always index -666, and not in `speakerlist`? It probably doesn't matter, since in the meta files the MapKey is "NARRATOR" so we can special-case it there instead.~~
     - Upon further review, it seems that the same string ID can be used for multiple voice lines (spoken by different people). In the dialog files, the speaker list is all of the speaker IDs that can say the line, and each speaker has their own meta file, mapping the same string ID to their own audio file (usually named `v{speakerId}_{stringId}.wem`). So we could in theory start with either the dialog files **or** the meta files. The latter seem easier to deal with, so we'll go with that.
2. Read and index both localization XML files (string ID => text)
   - It's not obvious at first glance, but the string IDs are also guids, just with the dashes replaced with the letter "g". Many have an underscore and a number at the end, too, but none of those are dialogue.
3. Iterate through the meta files
4. For each `.save.regions.VoiceMetaData.VoiceSpeakerMetaData[].MapValue[].VoiceTextMetaData[]`:
   - `.MapValue[0].Source.value` is the audio file (replace .wem with .opus; maybe throw if MapValue is not a single element?)
   - `.MapKey.value` is the string ID
   - `.save.regions.VoiceMetaData.VoiceSpeakerMetaData[].MapKey.value` is the speaker ID
5. If the speaker ID is "NARRATOR", use `h0fd7e77ag106bg47d5ga587g6cfeed742d5d`. Otherwise, look up the speaker ID in the Templates index. If there's no DisplayName, follow TemplateName.
   - The MapKeys (speaker IDs) here are always either a guid or "NARRATOR".
   - Note that these aren't _necessarily_ the name shown in-game when the line is spoken. For example Mods/GustavDev/Story/RawFiles/Goals/ORI_Gale_Elminster.txt calls SetStoryDisplayName to change Elminster's name from "Weary Traveller" to "Elminster Aumar". The only way to handle this would be to actually parse the Story scripts and follow various references around.
   - If when resolving speaker IDs / templates we encounter `Name.StartsWith("S_Player_GenericOrigin")`, stop and use `ha0b302dag3025g44e2gaf63g7fd9ec937241` (Tav) instead. Otherwise it'll land on "Human" etc.
6. Finally, look up the DisplayName string ID in the localization XML.

## Formatting

- Make sure the XML parser decodes HTML entities (`&lt;` etc.)
- Replace `<br>` with a space if English
- Strip HTML tags (keep their contents)
- Trim wrapping parenthesis and asterisks
- The strings appear to use square brackets for string interpolation

## Cleaning up unnecessary files

```sh
find . -type f -not \( -name '*.lsj' -o -name '*.xml' -o -name '*.opus' \) -delete && find . -depth -type d -empty -delete
```

## Tav voices

"These boots have seen everything" has three string IDs:

- `h9f01aa67g70d7g4fb8gbf3eg526194a368df` (Normal)
- `h472a3f55gfc77g4e31g9a21g8aa78dd4858c` (Alternate)
- `h4184ab70g2a86g42b5g8243g7d283ae6b747` (Whispered)

Which lead to the eight different player voice/speaker IDs:

- `24247531-c432-4f0f-8f35-6c90c4844aa8` (Voice 1, Male)
- `869248f0-468a-4747-82d7-e8efc3bbcacf` (Voice 2, Female)
- `4df6dba0-8574-4704-a9fb-a500da38e1e1` (Voice 3, Male)
- `3347e417-d7ad-4088-bdd6-d5565efcd815` (Voice 4, Female)
- `f5b335b2-9cd9-4b38-a4c1-458b846ab499` (Voice 5, Male)
- `b9b26a44-943b-4427-9890-d81c7e81a75b` (Voice 6, Female)
- `2d206fda-0d4f-457f-b4ea-0fc18866f5dd` (Voice 7, Male)
- `fb6b5353-8d14-4507-9222-ceaec990fce9` (Voice 8, Female)

Sadly, the iconic boots line appears to be missing a Japanese translation. In fact, of the 6,033 "point-and-click" lines, for Tav or any origin character, only two are translated as of v4.1.1.5849914:

```sh
readarray -t strings < <(grep -Porh 'h[0-9a-f]{8}g[0-9a-f]{4}g[0-9a-f]{4}g[0-9a-f]{4}g[0-9a-f]{12}' Mods/Gustav/Story/DialogsBinary/Global/BG_PointAndClick)
for s in "${strings[@]}"; do if grep -qF "$s" Localization/Japanese/japanese.xml; then grep -F "$s" Localization/English/english.xml; fi; done
```
