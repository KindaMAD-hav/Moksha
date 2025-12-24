using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.DevTools.DevTrails
{
    // ReSharper disable once InconsistentNaming
    public class UserStats_Global_BuildStats : ScriptableSingleton<UserStats_Global_BuildStats>
    {
        public List<BuildRecord> buildRecords = new List<BuildRecord>();
        
        #region Methods
        static string GetCustomSavePath()
        {
#if UNITY_EDITOR_WIN
            string myPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
#elif UNITY_EDITOR_OSX
            string myPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#elif UNITY_EDITOR_LINUX
            string myPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#else
            throw new System.NotSupportedException("Unsupported platform");
#endif
            return Path.Combine(myPath, "Tiny Giant Studio/DevTrails", "Global Build Stats.json");
        }

        // Load from file
        public void LoadFromDisk()
        {
            string path = GetCustomSavePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrEmpty(json))
                        JsonUtility.FromJsonOverwrite(json, this);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Failed to load global stats. Please contact support(FerdowsurAsif@gmail.com) with details, if you have time. Exception: " + ex);
                }
            }
            else
            {
                Debug.Log("No existing Global Stats found at " + path + " for DevTrails. A new one will be created the next time something is saved.");
            }
        }

        static readonly object FileLock = new object();

        // Save to file
        public void SaveToDisk()
        {
            try
            {
                string path = GetCustomSavePath();
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Invalid or empty path.");

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(this, true);

                // Attempt to write with retry mechanism
                const int maxAttempts = 3;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        lock (FileLock)
                        {
                            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                            using (var writer = new StreamWriter(stream))
                            {
                                writer.Write(json);
                            }
                        }

                        // Success � break out of retry loop
                        break;
                    }
                    //catch (IOException e) when (attempt < maxAttempts)
                    catch (IOException) when (attempt < maxAttempts)
                    {
                        // If file is in use, wait and retry
                        System.Threading.Thread.Sleep(100); // Wait 100ms before retrying
                        continue;
                    }
                }
                //Debug.Log($"Settings saved to {path}");
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.LogError($"Couldn't save stats. Access denied: {e.Message}");
            }
            catch (IOException e)
            {
                Debug.LogError($"Couldn't save stats. IO error while saving: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't save stats. Unexpected error: {e.Message}");
            }
        }


        public void Reset()
        {

            Save();
        }

        public void Save()
        {
        }

        #endregion Methods
    }
}