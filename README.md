# Rofi-volume

this handy piece of software is desiged to be a rofi application to change the system volume.

## Dependencies:
- rofi
- amixer

## Instalation
- ### from source:
    install [dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) <br>
    run the following on the root of the repository: <br>
    `dotnet publish --no-self-contained -r linux-x64 -o .` <br>
    and then you can run the program using: <br>
    `rofi -show volume -modi "volume:reporoot/rofi-volume"` <br>
