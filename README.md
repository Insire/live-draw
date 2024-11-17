# LiveDraw

A tool that allows you to draw on screen in real-time.

## Why?

When you need to draw or mark something while presenting, you may use some tools like
[Windows Ink Workspace](https://blogs.windows.com/windowsexperience/2016/10/10/windows-10-tip-getting-started-with-the-windows-ink-workspace/),
but all of them are actually **taking a screenshot** and allow you to draw on it.
That's actually annoying when you want to present something dynamic.

However, **LiveDraw is here and built for it!**

## Interface

![Compact mode](screenshots/00.png)

![Actionshot with Blender](screenshots/01.png)

![Actionshot](screenshots/02.png)

## Usage

The shortcuts that can be used:

- [ Z ] Undo, [ Y ]  Redo,
- [ E ] Eraser By Stroke, [ D ]  Eraser By Point,
- [ R ] Release or Recover interface,
- [ + ] Increase size brush, [ - ]  Decrease size brush
- [ B ] Brush mode, [ L ]  Line Mode
- [ C ] Clear

### About this fork

This was forked from [antfu/live-draw](https://github.com/antfu/live-draw) as of the time of writing this, that repository has been dormant for 3 years. Here is a short summary of the things i changed:

- updated from .NET 5 to .NET 9
- lots of refactoring and renaming to make the code more readable for me (via rider and Visual Studio)
- updated this readme
- removed unncessary files(images and settings) and dependencies (namely winforms)
- minor bugfixes in regards to proper async usage in C#
- added a shortcut (C) to clear the entire screen

I am aiming to keep this up to date with upcoming .NET releases. I am not looking for adding a lot more features. Feel free to fork this tho.

### Requirements

- a supported [Windows OS](https://learn.microsoft.com/en-us/windows/release-health/supported-versions-windows-client)
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

#### Why no download?

Providing a secure and trusted release is actually a lot of work (also costs money) and i updated this over a timespan of 24hrs for my personal use. If you feel like paying for a trusted signing certificate, i might take you up on that and add CI/CD workflow, resulting in a signed release on github.

### Features

- True transparent background (you can draw freely even when you are watching videos).
- Select colors by simply clicks.
- Adjust the size of brush.
- Draw ink with pressure (when using a pen with pen pressure).
- Auto smooth ink.
- Mini mode.
- Unlimited undo/redo.
- Freely drag the palette.
- Save and load ink to file (binary file) with color.
- Temporarily disable draw then you can be able to operate other windows.
- Fully animated.
- Keyboard Shortcuts.

## Publish

### For others to use

(embeds the .NET runtime in the exe file and makes the exe file about 100MB larger, but will basically make it work on any [supported Windows OS](https://learn.microsoft.com/en-us/windows/release-health/supported-versions-windows-client))

- dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true
- dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true

### For yourself

(makes the exe file about 1MB large, but requires the [.NET runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) to be installed)

- dotnet publish -c Release -r win-x68 --self-contained false -p:PublishSingleFile=true
- dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

## License

MIT
