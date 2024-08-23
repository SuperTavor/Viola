# 💜Viola 💜 
*Version 1.3.0*

Modern LEVEL5 modding made easy!

## Special thanks to
**onepiecefreak3** for helping me, **Tinifan** for allowing me to use his CfgBin library, and finally, **Light** and **Plasma** for testing Viola extensively!

## What is Viola?
Viola is an all-in-one tool to manage, create, and analyze modern LEVEL5 games. You see, these newer LEVEL5 games all use various CPK files to store the game's files, and they keep track of which file is in which CPK in the cpk_list.cfg.bin file, along with their sizes.

## Why is Viola even needed?
Viola can dump these pesky, CPK-filled filesystems into clean and coherent filesystems, and instead of distributing the entire CPK you are editing every time you want to release a mod - You can pack your mod with Viola and only distribute the files you edited - no CPKS involved! This results in a better development experience and a **massive file size decrease!**

## Quickstart

### Creating a mod using Viola - from 0 to a playable
This section will guide you creating a mod from 0 to a finished sample mod with screenshots and explanations. If you have any questions at all, please visit the [Yo-kai Watch Modding Discord Server](https://discord.gg/N4bn5XRWdK). This guide is going to be showcasing using the GUI version, however, you can follow along using the CLI version if you'd like.

### 1. Getting your game files
**For Nintendo Switch games:**

Load your game up in Ryujinx, and click `Extract data -> RomFS`, and choose a folder to keep your dump in.

![img](https://i.imgur.com/S3k8t8P.png)

**For PC games:**

Simply find the `data` folder in the game's directory and copy it to a fresh folder.

![img](https://i.imgur.com/fnYaaXY.png)

### 2. Backing up your Cpk_List
In your game files, you can find `cpk_list.cfg.bin` in the data folder. Copy it to a safe place and remove it from the game files, we'll need it later!
### 3. Dumping your game files
As you may have noticed when browsing through the game files, many of the files are `.CPK` files, archive files that contain other files. In order to get a completely clean game files folder with all of the CPKs extracted, we are going to use Viola's `dump` function.

To get started, Click `Dump` in Viola's main menu.

![img](https://i.imgur.com/wksNdxq.png)

After doing so, an explorer window will open asking you to open the folder you want to dump. In this step, please choose a folder that contains ONLY the `data` folder we dumped earlier, like this:

![img](https://i.imgur.com/f37P6Ea.png)

After that, Viola will open yet another explorer window, this time asking you where you want to put your dumped output. You can create a new folder anywhere you'd like, just make sure you remember where it is!

Viola will now dump your game files. This may take a few minutes, so do not panic if it takes a while!

![img](https://i.imgur.com/hbSnXJf.png)

### 4. Editing and injecting your edited files
I'm not going to go into detail on how to mod specific stuff, there are many guides out there for that. For this example, I'm going to showcase injecting a simple texture I already edited.

For this example, I decided to edit the `data/nx/chr/c00050010/c00050010.g4tx` file in Yo-kai Watch 4 (Taken from our dumped game files, obviously). As previously mentioned, I'm not gonna teach you how to edit these files, there are plenty of guides out there. In order to inject this file into the game using Viola, we need to create a matching folder structure for it.

This essentially means replicating the path leading to the file in a completely seperate folder. For example, this could be our folder structure for this mod:
```
- MyMod
    -data
        -nx
            -chr
                -c00050010
                    --c00050010.g4tx
```

You can include as many edited files as you'd like in this folder structure we created. Now, time to pack the mod and inject the files!

### 5. Packing our mod
To get started, Select the `Pack` option in Viola's main menu.

![img](https://i.imgur.com/crnODkx.png)

Viola will ask you the following. Say **Yes**.

![img](https://i.imgur.com/LEgfOsJ.png)

Select the Cpk_List file we backed up in Step 2.

After doing so, Viola will prompt you to choose the target platform. Choose your target platform.

![img](https://i.imgur.com/HxEsy4p.png)

Viola will then ask you to select the folder you want to pack. For me, that would be `MyMod`, from the previous step.

![img](https://i.imgur.com/SF4HMz8.png)

Now you'll be prompted to select your output folder. You can choose wherever you'd like, just remember where it is!

### 6. Installing our mod
This is a very simple process!

**For Switch games (Ryujinx):**

in Ryujinx, Right click on your game and click `Open mods directory`. Copy your output folder here.

**For PC games:**

Traverse into your output folder and copy the `data` folder into the game's installation directory. If asked to merge files, click yes.

--------------

Congrats! You've created your first mod using Viola!

![img](https://i.imgur.com/pgULNjv.png)

## For PC modders - How can you edit the encrypted Criware files?
If you are modding any supported LEVEL5 PC game, you may have noticed that the Criware files (AWB, ACB, etc) are **encrypted**. Viola already decrypts the CPKs for you when dumping, but the other files need to be decrypted manually. In this section, I'll show you how to decrypt these files and how to re-encrypt them later using Viola!

### 1. Decrypting your file
To get started, open Viola and click `Decrypt Criware`.

![img](https://i.imgur.com/rNqgSQy.png)

An explorer window will open and you'll be prompted to select the file you want to decrypt. Please do so!

![img](https://i.imgur.com/cMhidhJ.png)

You will now be asked to choose where to put the output file. This can be wherever you'd like, just remember where it is.

After waiting a few moments, your decrypted file will appear in the folder and the following will be written in the Viola console:

![img](https://i.imgur.com/NwJmPME.png)

 ## **REMEMBER THE KEY VIOLA GIVES YOU. IF YOU FORGET IT, YOU WONT BE ABLE TO RE-ENCRYPT LATER!**

### 2. Re-encrypting your file
So after editing you decrypted file, how would you encrypt it? Well, to get started, Click the `Encrypt Criware` option in Viola's main menu.

![img](https://i.imgur.com/FsVQfH1.png)

You'll be asked to enter an encryption key. Enter the key you got when decrypting.

![img](https://i.imgur.com/HQMYyUb.png)

Now you'll be asked to select the decrypted edited file. Please do so.


After that, you'll be prompted to select the location of your new encrypted file. This can be anywhere you'd like.


**Congrats! You've just edited your first encrypted Criware file!**

### How to merge multiple mods
But wait - you may have seen some awesome mods made with Viola you want to try out, but putting all of the mods together doesn't work! Here's you how can fix it:

To get started, click the `Merge`  option in Viola's main menu.

![img](https://i.imgur.com/aoWSZqz.png)

After doing so, you'll be prompted to enter the amount of mods you want to merge. please do so.

![img](https://i.imgur.com/41vtTao.png)

You'll now be prompted to select all the mods you want to merge, in order of most important to least important.

After selecting your mods, you'll be asked to select an output folder to put your merged mod in. 

Now, you'll be asked to select your vanilla cpk_list file. You can learn how to get that [here](https://github.com/SuperTavor/Viola?tab=readme-ov-file#2-backing-up-your-cpk_list).

After that, you'll have to select your target platform. Please do so.

![img](https://i.imgur.com/HxEsy4p.png)




If you see this, it means all went well!

![img](https://i.imgur.com/bPHa3M1.png)

**Congrats! You've just merged your mods. You can install your mod using [this](https://github.com/SuperTavor/Viola?tab=readme-ov-file#2-backing-up-your-cpk_list) part of the guide.**