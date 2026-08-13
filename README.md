# mkLink

Make symbolic links, hard links, and directory junctions from the context menu

mkLink creates multiple types of symbolic links. Specifically, it simplifies the use of creating symbolic links by providing a GUI interface and access from the context menu. Normally advanced symbolic links, such as Hard Links and Directory Junctions can only be created through the command prompt.

Please &#9733; this repo if you found it useful &#9733; &#9733; &#9733;

![](assets/mklink.png)

## Features
<!------------------------------------------------------->

Pick one of three link types and mkLink runs the matching MKLINK command for
you. Creating any of them needs administrator rights, so the app asks for
elevation when it starts.

| Type | MKLINK | Target | Notes |
| --- | --- | --- | --- |
| Symbolic Link | none or `/D` | file or folder | Points at the target by path. `/D` is added automatically for a folder. |
| Hard Link | `/H` | file | A second name for the same file. Both names must be on one volume. |
| Directory Junction | `/J` | folder | A second path to the same folder. Local volumes only. |

The target has to exist before a link can be made. The link path has to be a
valid path, inside a folder that exists, with nothing already sitting there.
When any of that is not true the reason appears at the bottom of the window
and the Create Link button stays disabled. If MKLINK itself refuses, its own
message is shown rather than an empty dialog.


## Building
<!------------------------------------------------------->

Requires .NET Framework 4.5.2 or newer. Open `mkLink.sln` in Visual Studio, or
build from a command prompt:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe mkLink.sln /p:Configuration=Release
```

The executable lands in `Application\bin\Release\mkLink.exe` and can be run
directly.


## Tests
<!------------------------------------------------------->

`tests\mkLink.Tests` covers `CommandLine`, which is what keeps text-box input
from turning into `cmd.exe` syntax. That file has no dependency beyond `System`
and is compiled straight into the test project, so the tests run anywhere the
.NET SDK does, not only on Windows:

```
dotnet test tests/mkLink.Tests
```

The project is deliberately left out of `mkLink.sln`, which is a .NET Framework
solution and cannot load an SDK-style project.


## Usage
<!------------------------------------------------------->

1. Run the installer

2. Right click on the file or folder you want to create a symbolic link with and select "mkLink" from the context menu

`Resources\mkLink Installer.exe` is the 1.0.1 installer and predates the fixes
in the source tree, so it installs the older build. Build from source for the
current behaviour.


## Support
<!------------------------------------------------------->

Submit an [issue](https://gitlab.com/bitspur/misc/mklink/-/issues/new)


## Screenshots
<!------------------------------------------------------->

Sorry, I don't have any screenshots


## Buy Me Coffee
<!------------------------------------------------------->

A ridiculous amount of coffee was consumed in the process of building this project.

[Add some fuel](https://jamrizzi.com/#!/buy-me-coffee) if you'd like to keep me going!


## Contributing
<!------------------------------------------------------->

1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -m 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a merge request :D


## License
<!------------------------------------------------------->

[MIT License](LICENSE)

[Jam Risser](https://jamrizzi.com) &copy; 2017


## Credits
<!------------------------------------------------------->

* [Jam Risser](https://jamrizzi.com) - Author
* [mklink command reference](https://learn.microsoft.com/windows-server/administration/windows-commands/mklink)


## Changelog
<!------------------------------------------------------->

Unreleased
* Text-box input can no longer add its own commands to the MKLINK line
* Hard Link and Directory Junction are separate choices instead of one entry that guessed from the target
* Errors from MKLINK are shown instead of being discarded with standard error
* Bad input is explained at the bottom of the window instead of silently disabling Create Link
* Fixed a crash when a link path named a folder that does not exist

1.0.1 (2017-04-28)
* Changed license to MIT

1.0.0 (2016-02-16)
* Initial release
