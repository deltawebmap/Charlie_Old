# "Charlie" ARK Data Extractor for Delta Web Map

Charlie is a tool designed to rip ARK data from mods and the base game and upload them as packages to the Delta Web Map server. This allows for automated mod support and updates to the base game. 

Charlie is designed for developers and is not useful to end users.

## Improvements over Old System

Charlie has many improvements over the old system. It is fully automated and can be run headless, and it only keeps one copy of ARK assets. Charlie also has better seeking techniques to find more ARK files.

## Acknowledgements

Thank you to [coldino](https://github.com/coldino/) for responding to my questions about this. His project [Purlovia](https://github.com/arkutils/Purlovia) is very similar to this one for his own needs. Thank you so much!

## Setting Up

Charlie requires my [UAsset Toolkit](https://github.com/Roman-Port/UASSET-Toolkit) and must be added as a reference. From there, copy the ``config_example.json`` and configure server credentials. At the moment, only remote servers are supported over SFTP. This may be changed in the future.
