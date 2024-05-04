# Viola 💜
LEVEL5 modding on Switch made easy!

## Key features
* A fantastic dev experience: Viola can generate a clean, CPKless RomFS that's as easy to edit as any game that doesn't use CPKs, then compile it into a highly efficient loose mod folder with only the files you need! It's super easy to use, and it eliminates the need to worry for CPKs at all! You won't be encountering them at all while using Viola.

* Easier multi-mod support: You can merge as many mod folders as you'd like with a robust priority system.

* EXTREMELY SMALL MOD BINARIES! After compiling with Viola, your mods will be about **70%** smaller on average, as they don't require you to ship the full CPKs you are editing, only the files you actually edited.

* Backwards compatibility: You can dump your CPK mods, then pack them with Viola, so you don't need to do any work on your own!

* No overhead on the user side. To use Viola generated mods, the end user DOES NOT have to install any mod loader, it simply works out of the box on both emulator and real hardware. *Note: When merging mods, a copy of Viola will be required.*


Big thanks to Tinifan for letting me use his CfgBin logic, and big thanks to Light for testing this tool extensively!

## Usage
```
Viola.exe <mode> <additional_arguments>\

Modes:

* pack - packs a dumped mod folder. Example: `Viola.exe pack MyModFolder'

* merge - Merges multiple dumped mod folders. Example: `Viola.exe merge MyFirstMod MySecondMod`

* dump - Dumps a specified RomFS into a Cpk-less directory structure. You can specify `--cache` before your folder name to create a HashCache. Example without creating hashcache: 'Viola.exe dump MyRomfs MyDumpedRomFs'. Example with creating a hashcache: `Viola.exe dump --cache MyRomfs MyDumpedRomfs'
```
