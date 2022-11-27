using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Util
{
    public static class PersistentDataUtil
    {
        public static bool TryLoad<T>(string relPath, out T data, Action<Exception> onException = null)
        {
            data = default;
            try
            {
                string path = MakePath(relPath);
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(json);
                return true;
            }
            catch (Exception e)
            {
                onException?.Invoke(e);
                return false;
            }
        }
        public static bool TryLoad(string relPath, object data, Action<Exception> onException = null)
        {
            try
            {
                string path = MakePath(relPath);
                string json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, data);
                return true;
            }
            catch (Exception e)
            {
                onException?.Invoke(e);
                return false;
            }
        }

        public static bool TrySave<T>(string relPath, T data, Action<Exception> onException = null)
        {
            try
            {
                string path = MakePath(relPath);
                string json = JsonUtility.ToJson(data, false);
                File.WriteAllText(path, json);
                FlushWrite();
                return true;
            }
            catch (System.Exception e)
            {
                onException?.Invoke(e);
                return false;
            }
        }

        public static bool TryDelete(string relPath, Action<Exception> onException = null)
        {
            try
            {
                string path = MakePath(relPath);
                if (!File.Exists(path))
                {
                    File.Delete(path);
                    FlushWrite();
                }
                return true;
            }
            catch (System.Exception e)
            {
                onException?.Invoke(e);
                return false;
            }
        }

        private static string MakePath(string relPath)
        {
            return Path.Combine(Application.persistentDataPath, relPath);
        }

        private static void FlushWrite()
        {
            //Application.ExternalEval("_JS_FileSystem_Sync();");
            SyncIndexedDB();
        }

        [DllImport("__Internal")]
        private static extern void SyncIndexedDB();
    }
}