# Spin Archipelago

Archipelago Multi-World Randomizer integration for Spin Rhythm XD.

## How this works

Similarly to other rhythm games with Archipelago integration, you start with a limited subset of songs and work your 
way up. Clearing songs sends location checks and receiving items from other players unlocks new songs for you to play.

## How to use

WIP section since I didn't focus on the end-user side of things for the server-side.

Client-Side:
1. Install the mod
2. **HIGHLY RECOMMENDED**: head over to the track selection list menu and set the filters to show ONLY official songs.
3. Go to the options menu, then in the mod settings tab and open the Spin Archipelago mod settings.
4. Fill in the username, server address and password fields.
5. Click on connect
6. Profit

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
