# Lethal Insurance
A mod focused around adding insurance coverage for various events/scenarios that requires credits to purchase.

![terminalextender](https://github.com/domkalan/lethalinsurance/raw/main/images/example1.png)

## Some Notes
* Mod is started a proof of concept and is my first attempt at modding the game. Source code for the mod may not be in the best state.
* Mod currently does not use any save state, insurance will have to be purchased again at the start of every game session.

## Todo

- [ ] Add insurance coverage for the following events
    - [ ] Scrap lost on full team death
    - [ ] Employee death with unrecoverable body
    - [ ] Equipment left behind on map (shotguns, ladders, etc)
- [ ] Add cooldown, certian insurance should only be able to be purchased
- [ ] Add save state so insurance rolls over from play sessions
- [ ] Add base cost instead of most prices being based off % of quota.

## Installing
This mod uses BepinEx and Harmony 2 for mod injection, they are included in the DLL file. You can find the latest build of the mod in the [GitHub releases section], then extract the mod and add it to your BepinEx plugin folder. Please be sure to include the `lethalinsurance` and `lethalinsurance.manifest` file. These are Unity NetCode for GameObjects NetObjects that are responsible for ServerRPC and ClientRPC.

## Usage
To purchase insurance, use the in game terminal and type in insurance. All coverage plans offered at the time will be displayed on the terminal. You can then type in `quota` for example to purchase quota insurance. **Please note that the insurance command is an interactive command, and will function diffrently from other in game commands. Subcommands of the insurance command will only be accessable in the insurance application.**

## External Dependencies Used
* [BepinEX](https://docs.bepinex.dev/index.html)
* [Harmony 2](https://harmony.pardeike.net/)
* [LethalAPI.GamesLib](https://github.com/dhkatz/LethalAPI.GameLibs)
    * Can also be downloaded from [NuGet](https://www.nuget.org/packages/LethalAPI.GameLibs)
* [Lethal TerminalExtender](https://github.com/domkalan/LethalTerminalExtender)

# Credit
* [tinyhoot's ShipLoot](https://github.com/tinyhoot/ShipLoot) for refrence implementation of how to count total scrap inside ship.

## New to Lethal Company Modding?
Check out this [Lethal Company Modding Wiki](https://lethal.wiki/) which has some good resources on how modding the game works, and even guides to add multiplayer to your mod.

## License
Feel free to fork, submit pull requests, or use this mod in any way shape or form as long as you include the LICENSE file which is above in the repo. The source code uses the open source friendly MIT License.
