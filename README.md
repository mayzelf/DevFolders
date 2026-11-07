# DevFolders

Folder icons for developers on Windows. DevFolders puts a project's own icon (its favicon, app
icon or logo) in the corner of its folder in Explorer, so projects are recognizable at a glance.

## Features

Right-click a folder and open the **DevFolders** menu:

- **Use project icon**: finds the project's icon (`favicon.ico`, `public/favicon.png`, `icon.svg`,
  `app.ico`, an `.exe` named after the folder, ...). One match is applied right away; if there
  are several, a picker opens.
- **Choose icon...**: pick from the icons found in the folder, search the
  [Iconify](https://iconify.design) library, or browse for any image, `.ico` or `.exe`.
- **Mark as dev folder**: gives a folder that holds projects its own blue look and offers to
  badge every project inside it.
- **Reset to default**: restores the folder exactly as it was.

Folders are also colored to match their icon, so projects stand apart even in the details view
where icons are small. The picker has a **Color the folder** option to keep the standard yellow.

## Requirements

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build

## Install

```powershell
dotnet publish src/DevFolders -c Release -o "$env:LOCALAPPDATA\Programs\DevFolders"
& "$env:LOCALAPPDATA\Programs\DevFolders\DevFolders.exe" install
```

`install` adds the menu for the current user only (no admin rights needed). Run
`DevFolders.exe` without arguments to see customized folders and to add or remove the menu.

On Windows 11 the menu appears under **Show more options** (or Shift + right-click).

## Uninstall

```powershell
& "$env:LOCALAPPDATA\Programs\DevFolders\DevFolders.exe" uninstall
```

This removes the menu and resets every folder DevFolders changed.

## How it works

Icons are applied the way Windows supports natively: DevFolders renders a multi-size `.ico`
(the Windows folder plus the badge) and points the folder's `desktop.ini` at it. Nothing has to
keep running for the icons to show, and they survive restarts.

- Generated icons and the icon library cache live in `%LOCALAPPDATA%\DevFolders`.
- Every change is recorded in `%LOCALAPPDATA%\DevFolders\state.json`, so a reset puts back any
  `desktop.ini` settings and folder attributes that were there before.
- Inside git repositories, `desktop.ini` is added to `.git/info/exclude`, which keeps it out of
  `git status` without touching the project's `.gitignore`.
- Folders that need admin rights (such as games under `Program Files`) trigger a UAC prompt.

## Command line

```
DevFolders.exe apply <folder>       Use project icon
DevFolders.exe choose <folder>      Open the icon picker
DevFolders.exe mark-dev <folder>    Mark as dev folder
DevFolders.exe reset <folder>       Reset to default
DevFolders.exe install              Add the Explorer menu
DevFolders.exe uninstall            Remove the menu and reset all folders
```
