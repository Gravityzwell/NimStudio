##NimStudio

Visual Studio support for the [Nim](https://github.com/Araq/Nim) language.

###*NOTE*: This project is in the early stages of development, and is **not yet functional**.

####Requirements 
---
- Visual Studio 2013 - Community Edition, or Pro, or Ultimate
- Net Framework 4.51+
- Compiled nimsuggest.exe (must be in the `nim\bin` folder)

####Getting Started
---
- A compiled VSIX is included in the repository `Install` folder. Download and run.

- Open a .nim file in Visual Studio via `File->Open`, or drag and drop.

- Symbol name completion and highlighting work as expected with any `.nim` file.

- To generate a procedure list, type a period after a symbol, and press the member completion shortcut (normally Ctrl+J).

- See screenshots in the Wiki. 

####Working Features
---
* Procedure completion
* Symbol name completion
* Symbol highlighting

####Planned Features
---
* Project support
* Build/compile support
* Debugger watch variables
* Symbol tooltips

####Notes
---

####VSIX Installer
---
Files are installed to a sub folder in `%LocalAppData%\Microsoft\VisualStudio\12.0\Extensions`. The sub folder name is generated randomly.

Example: `c:\Users\YourName\AppData\Local\Microsoft\VisualStudio\12.0\Extensions\jhkg4gqi.nu1`

Files installed:
* NimStudio.dll
* NimStudio.pkgdef
* NimStudio.vsixmanifest

