ModernMinas Launcher
====================
The launcher for the Modern Minas Minecraft modification.

[This document is subject to change.]

Requirements (source)
=====================
To build the source code, you need:
- Access to git in some way (usually by using the Git binaries or TortoiseGit on Windows)
- Microsoft.NET Framework 4.0 Client Profile/better or Mono 2.10/better
- NuGet (or use the included <code>.nuget/nuget.exe</code> from the source tree)
Additionally, you need to download the source code of log4net. As it is included as a git submodule you can just run this for proper inclusion:
	git submodule update --init
This will add log4net to the correct folder with its complete source code tree.
You will need to have package restoration enabled by using the EnableNuGetPackageRestore environment variable if compiling from command line or if you are going to use Visual Studio you need to enable package restoration for the whole solution.

Requirements (binaries)
=======================
To use the binaries you need:
- Microsoft.NET Framework 4.0 Client Profile (by default installed on every Windows 7 computer)

Downloading binaries
====================
You can get the installer for the binaries from http://update.modernminas.de/bootstrap/setup.

License
=======
The whole project is published under the terms of the GNU Affero General Public License (AGPL) Version 3.
More info about it in the LICENSE.rtf file.
