# Translation preparation

Prototype UI strings are English source messages passed through Godot `Tr()` (including the shared Text/Button helpers). Dynamic messages use format placeholders. No tutorial text is embedded in images.

Add PO translation resources through Godot's Localization settings and add a language selector when needed. Preserve placeholders such as `{0}` and line breaks. Change the initial locale selection in `ParkingGame._Ready()` and select the corresponding .NET numeric culture there. English is currently explicit and is not inherited from the Windows language.

Controller glyph labels describe generic input positions. Third-party asset licences stay in documentation and do not need in-game translation.
