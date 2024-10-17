# Spin Archipelago

Archipelago Multi-World Randomizer integration for Spin Rhythm XD.

## How this works

Similarly to other rhythm games with Archipelago integration, you start with a limited subset of songs and work your 
way up. Clearing songs sends location checks and receiving items from other players unlocks new songs for you to play.

## How to use

WIP section since I didn't focus on the end-user side of things for the server-side.

Client-Side:
1. Install BepInEx on your game installation (v5, not v6)
   - You can use [this handy program](https://github.com/SRXDModdingGroup/SRXDBepInExInstaller/releases/tag/2.0.0b1)
     to install BepInEx on your game installation (**do not install v5.4.23 and later from the installer**).
   - If you are going for a manual installation, install BepInEx **v5.4.x for Mono games**, and **make sure you swap the 2
     UnityPlayer.dll files after installing**, otherwise BepInEx will not get loaded.
2. If you do not have a plugins folder, run the game once and close it.
   - Recommended: change the BepInEx config to show the logging console.
     - This is automatically done when using the BepInEx installer linked above.
3. Download [SpinCore](https://github.com/Raoul1808/SpinCore) and place both SpinCore.dll and Newtonsoft.Json.dll in
   the BepInEx plugins folder.
4. Download (or build) the mod and place it in the BepInEx plugins folder.
5. **HIGHLY RECOMMENDED**: head over to the track selection list menu and set the filters to show ONLY official songs.
6. Go to the options menu, then in the mod settings tab and open the Spin Archipelago mod settings.
7. Fill in the username, server address and password fields.
8. Click on connect
9. Profit

## Building

Pre-requisites:
- .NET Framework 4.7.2 and a modern .NET SDK (8.0 used for this project, could theoretically be any version)
- Spin Rhythm XD with BepInEx and SpinCore

Steps:
1. Clone this repo
2. Create a symlink named `srxd-dir` at the project's root that links to the game's installation directory
    - Alternatively you can modify the .csproj file to change every srxd-dir reference to the game's path
3. Build
    - The project contains post-build steps that copies the mod, its NuGet dependency and the generated pdb file
      when compiled in Debug. If you are running Windows, you might need to [convert the pdb file to a valid mono
      mdb](https://gist.github.com/jbevain/ba23149da8369e4a966f) file to be able to debug the mod in-game.
4. Profit

## License

This project is licensed under the [MIT License](LICENSE).
