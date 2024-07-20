# Viola 💜
Modern LEVEL5 modding made easy!

## What is Viola?
Viola is an all-in-one tool to manage, create, and modern LEVEL5 games. You see, these newer LEVEL5 games all use various CPK files to store the game's files, and they keep track of which file is in which CPK in the cpk_list.cfg.bin file, along with their sizes.

## Why is Viola even needed?
Viola can dump these pesky, CPK-filled filesystems into clean and coherent filesystems, and instead of distributing the entire CPK you are editing every time you want to release a mod - You can pack your mod with Viola and only distribute the files you edited - no CPKS involved!

## How can I play multiple mods at the same time?
Before Viola - you couldn't, if the mods edited the same CPK, but with Viola, it's no longer an issue - simply merge your mods using Viola's merge function.


## Alright, how do I even use Viola?
Viola can be used both as a GUI and as a CLI. To run the GUI version, simply run `viola gui` in the command line or double click the included `.bat` file.
This guide works for both the GUI and CLI versions.

Viola has many features, so let's go over them:

### Dumping
Dumping can clean up a messy filesystem riddled with CPKs into a nice and clean filesystem.

**on the CLI**: `viola -m dump -i <Path to the folder you want to dump> -o <Where you want to put your dumped output>`

**On the GUI:** Select the dump option.

### Packing

Packing can pack your modded dumped filesystem into a tiny mod folder only containing the files you edited.
*Note: to speed up packing speeds, try deleting the files you are not editing from your dump.*

**Make sure you have a HashCache file called HashCache.bin for your game residing next to Viola's EXE. You can get some premade HashCaches [here](https://github.com/SuperTavor/Viola/tree/main/HashCache%20library), or you can create one yourself with the `cache` function (explained later in the guide)**

**on the CLI**: `viola -m pack -i <Modded dump to pack> -o <Where you want to put your packed output>`

**on the GUI**: Select the pack option.


### Merging
the merge function can merge multiple Viola-packed mods into 1 mod you can simply play!

**NOTE: You need to pack this function's output using the pack function before playing**

**on the CLI**: `viola -m merge <FirstModToBeMerged> <SecondModToBeMerged> <ThirdModToBeMerged> <Etc..> -o <Where you want to put your merged output>`

**on the GUI**: Select the merge option and follow the steps on screen.

### Caching
the cache function can be used to create a HashCache file. a HashCache file is a file that tells Viola how the unmodified files of a game are structured, and thus Viola can infer if they're modified or not. 

**on the CLI**: `viola -m cache -i <PathToTheUnmodifiedDumpedGameFiles> -o <Where you want to put your HashCache>`

**on the GUI**: Select the cache option and follow the steps on screen.

### Decrypting
This function can decrypt files encrypted with LEVEL5's new encryption scheme for Criware formats.

**on the CLI**: `viola -m decrypt -i <File to decrypt> -o <Where you want to put your decrypted output>`

**on the GUI**: Select the decrypt option and follow the steps on screen.

### Encrypting
This function can encrypt files encrypted with LEVEL5's new encryption scheme for Criware formats.
**NOTE: To re-encrypt files, you need the key gotten from decrypting them with the decrypt function.**

**on the CLI**: `viola -m encrypt -i <File to encrypt> -o <Where you want to put your encrypted output> -key <your key>`

**on the GUI**: Select the encrypt option and follow the steps on screen.