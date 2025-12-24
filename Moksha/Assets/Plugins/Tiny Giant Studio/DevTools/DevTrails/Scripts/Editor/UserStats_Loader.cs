//This is commented out instead of deleting the file,
//Because users who previously downloaded this will keep it and do a double load
//Causing stats to reset
//So, until a grace period has passed when all old users have updated,
//It will stay here

//using UnityEditor;

//namespace TinyGiantStudio.DevTools.DevTrails
//{
//    // Auto-load when Unity loads
//    [InitializeOnLoad]
//    public static class UserStats_Loader
//    {
//        static UserStats_Loader()
//        {
//            //Debug.Log("Loading user stats");

//            UserStats_Global.instance.LoadFromDisk();
//            UserStats_Today.instance.LoadFromDisk();


//            //Commented out because this is unnecessary. Things are saved already when required.
//            //// Auto-save when domain reload / editor shutdown
//            //EditorApplication.quitting += () => UserStats_Global.instance.SaveToDisk();
//            //EditorApplication.quitting += () => UserStats_Today.instance.SaveToDisk();
//        }
//    }
//}