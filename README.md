# Monster Framework

A server extension for Rust (Oxide and Carbon) that monster and boss plugins are built on.

- `Oxide.Ext.MonsterFramework.dll` is the framework.
- `MonsterFrameworkUpdater.cs` is an optional plugin for servers that cannot upload the DLL by hand. On server start it downloads the DLL into the extensions folder if it is missing, replaces it when a newer one is out, and tells you in the console to restart.
