<?php
$assembly = simplexml_load_file("../launcher/bin/Release/mmlaunch.application");
$version = str_replace(".", "_", (string) $assembly -> assemblyIdentity["version"]);
$path = "Application Files/mmlaunch_$version";

if(!file_exists("$path/setup.exe"))
  system(escapeshellarg("C:\\Program Files (x86)\\NSIS\\MakeNSIS.exe") . " /DCONFIGURATION=Release /DSOURCE_BIN_PATH=../launcher/bin/Release /DVERSION=$version setup\\Setup.nsi");
if(!file_exists("$path/setup.exe"))
  die(header("HTTP/1.1 404 Not Found"));
$path = str_replace(" ", "%20", $path);
header("location: http://update.modernminas.de/$path/setup.exe");