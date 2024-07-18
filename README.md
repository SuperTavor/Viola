# Viola 💜
Modern LEVEL5 modding made easy!

## Key features
* A fantastic dev experience: Viola can generate a clean, CPKless RomFS that's as easy to edit as any game that doesn't use CPKs, then compile it into a highly efficient loose mod folder with only the files you need! It's super easy to use, and it eliminates the need to worry for CPKs at all! You won't be encountering them at all while using Viola.

* Easier multi-mod support: You can merge as many mod folders as you'd like with a robust priority system.

* EXTREMELY SMALL MOD BINARIES! After compiling with Viola, your mods will be about **70%** smaller on average, as they don't require you to ship the full CPKs you are editing, only the files you actually edited.

* Backwards compatibility: You can dump your CPK mods, then pack them with Viola, so you don't need to do any work on your own!

* No overhead on the user side. To use Viola generated mods, the end user DOES NOT have to install any mod loader, it simply works out of the box on both emulator and real hardware. *Note: When merging mods, a copy of Viola will be required.*


Big thanks to Tinifan for letting me use his CfgBin logic, big thanks to Light for testing this tool extensively and a big thanks to onepiecefreak3 for teaching me about the CfgBin structure and standing me after so many dumb mistakes I made!

## How it works
There are 4 modes: pack, merge, dump and cache

Dump extracts every CPK in your RomFS and make the structure clean and easy to mod. It can also generate a HashCache file, which is used to identify modded files

Pack packs your modified dumped RomFS into a loose mod folder. It can detect and pack only the modified files using a **HashCache**. 

Cache can generate a HashCache. 

Merge can merge multiple packed mods together, so they can be packed and then used alongside each other without any conflicts
