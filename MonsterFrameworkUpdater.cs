using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Plugins
{
    [Info("MonsterFrameworkUpdater", "RickySpanish", "1.0.0")]
    [Description("Installs the Monster Framework extension and keeps it up to date")]
    public class MonsterFrameworkUpdater : RustPlugin
    {
        private const string FileName = "Oxide.Ext.MonsterFramework.dll";
        private const string RuntimeType = "Oxide.Ext.MonsterFramework.MonsterRuntime";
        private const string ReleaseUrl = "https://raw.githubusercontent.com/RickySpanish343/MonsterFramework/main/release.json";
        private const string DownloadUrl = "https://github.com/RickySpanish343/MonsterFramework/releases/download/v{0}/" + FileName;

        private Coroutine routine;
        private UnityWebRequest request;

        private class Release
        {
            public string Version;
            public string Sha256;
        }

        private void OnServerInitialized()
        {
            routine = ServerMgr.Instance.StartCoroutine(Run());
        }

        private void Unload()
        {
            if (routine != null && ServerMgr.Instance != null) ServerMgr.Instance.StopCoroutine(routine);
            routine = null;
            CloseRequest();
        }

        private IEnumerator Run()
        {
            request = UnityWebRequest.Get(ReleaseUrl);
            request.timeout = 30;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                PrintWarning($"Could not check for the latest Monster Framework: {request.error}");
                CloseRequest();
                routine = null;
                yield break;
            }
            Release release = JsonConvert.DeserializeObject<Release>(request.downloadHandler.text);
            CloseRequest();

            Version latest = Short(new Version(release.Version));
            string path = Path.Combine(Interface.Oxide.ExtensionDirectory, FileName);
            Version installed = File.Exists(path) ? Short(AssemblyName.GetAssemblyName(path).Version) : null;

            if (installed != null && installed >= latest)
            {
                if (installed == Running()) Puts($"Monster Framework {installed} is up to date.");
                else Puts($"Monster Framework {installed} is installed. Restart the server to load it.");
                routine = null;
                yield break;
            }

            Puts(installed == null ? $"Downloading Monster Framework {latest}..." : $"Monster Framework {latest} is available (installed {installed}). Downloading...");
            request = UnityWebRequest.Get(string.Format(DownloadUrl, release.Version));
            request.timeout = 120;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                PrintWarning($"Could not download Monster Framework {latest}: {request.error}");
                CloseRequest();
                routine = null;
                yield break;
            }
            byte[] data = request.downloadHandler.data;
            CloseRequest();
            routine = null;

            if (!string.Equals(Hash(data), release.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                PrintWarning($"The Monster Framework {latest} download did not match its published checksum. Nothing was installed.");
                yield break;
            }

            if (!Write(path, data, installed != null)) yield break;
            Puts($"Monster Framework {latest} installed. Restart the server to load it.");
        }

        private bool Write(string path, byte[] data, bool replace)
        {
            string incoming = path + ".download";
            try
            {
                File.WriteAllBytes(incoming, data);
                if (replace) File.Delete(path);
                File.Move(incoming, path);
                return true;
            }
            catch (Exception ex)
            {
                PrintWarning($"Could not write {path}: {ex.Message}");
                return false;
            }
        }

        private void CloseRequest()
        {
            if (request == null) return;
            request.Dispose();
            request = null;
        }

        private static Version Short(Version version) => new Version(version.Major, version.Minor, Math.Max(version.Build, 0));

        private static Version Running()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType(RuntimeType) != null)
                    return Short(assembly.GetName().Version);
            return null;
        }

        private static string Hash(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", string.Empty);
        }
    }
}
