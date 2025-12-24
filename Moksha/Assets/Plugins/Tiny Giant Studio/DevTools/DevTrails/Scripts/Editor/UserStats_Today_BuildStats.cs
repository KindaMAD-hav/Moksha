using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.DevTools.DevTrails
{
    [FilePath("Tiny Giant Studio/UserStats_today Build Stats.asset", FilePathAttribute.Location.PreferencesFolder)]
    // ReSharper disable once InconsistentNaming
    public class UserStats_Today_BuildStats : ScriptableSingleton<UserStats_Today_BuildStats>
    {
      
        public List<BuildRecord> buildRecords = new List<BuildRecord>();
        
        #region Methods

        public void Reset(bool saveChange)
        {
            buildRecords.Clear();

            if (saveChange)
                SaveToDisk();
        }

        // Define your file path (My Documents)
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
            return Path.Combine(myPath, "Tiny Giant Studio/DevTrails", "Today's Build Stats.json");
        }

        // Load from file
        public void LoadFromDisk()
        {
            string path = GetCustomSavePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, this);
                //Debug.Log("Loaded Stats from {path}");
            }
            else
            {
                //Debug.Log("No existing Stats found at {path}, using defaults.");
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
                            using FileStream stream =
                                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                            using StreamWriter writer = new StreamWriter(stream);
                            writer.Write(json);
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

        void Save()
        {
            //Save(true);
            //SaveToDisk();
        }

        #endregion Methods
    }
}