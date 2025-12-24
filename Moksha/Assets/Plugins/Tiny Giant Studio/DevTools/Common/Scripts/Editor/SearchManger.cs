using UnityEditor;

namespace TinyGiantStudio.DevTools
{
    public class SearchManger
    {
        static readonly string editorPrefsPrefix = "Asset_DevTask";

        public string SearchingFor() => EditorPrefs.GetString(editorPrefsPrefix + "_searchingFor");

        public void SetNewSearch(string newKeyword) => EditorPrefs.SetString(editorPrefsPrefix + "_searchingFor", newKeyword);



    }
}