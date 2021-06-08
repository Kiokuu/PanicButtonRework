# PanicButtonRework
A VRChat Melonloader mod which aims to rework the panic mode 

## Purpose
The purpose of this [MelonLoader](https://github.com/LavaGang/MelonLoader) VRChat mod is to improve the panic mode system.

## Features
* Accidentally press the panic mode keybinds? Well now you can simply press the panic mode keybind again to restore the safety settings back!
* (Optional) Enable or Disable the functionality of Panic Mode (prevents safety settings being touched at all)

## Compatability
* Currently working on build 1102(Current)

## Installation
* Download the [latest release](https://github.com/Kiokuu/PanicButtonRework/releases/latest) of the compiled DLL and place into the "VRChat/Mods" folder.

## Known Drawbacks
* Currently the "You have enabled safetymode" message is still visible when panic mode functionality is disabled - Fixed
* The safety settings will reset to panic mode defaults upon closing the game and reopening - CURRENTLY FIXING

## Building
To build this mod, reference the following libraries from MelonLoader/Managed after assembly generation;
* Assembly-CSharp.dll
* Il2Cppmscorlib.dll
* UnhollowerBaseLib.dll
* UnhollowerRuntimeLib.dll
* UnityEngine.CoreModule.dll
* VRCCore-Standalone.dll

Additionally, reference the following library;
* MelonLoader.dll (from MelonLoader base directory)

Finally, build in your favourite IDE.

## Credits
Thanks to [Bens](https://github.com/BenjaminZehowlt) Xref functions
