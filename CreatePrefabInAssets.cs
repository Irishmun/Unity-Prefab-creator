#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Util.PrefabUtil
{
    public static class CreatePrefabInAssets
    {
        private const string PREFAB_FOLDER = "Assets/Project/Prefabs";//Replace this with desired folder for the prefabs to be created in
        private const string PREFAB_EXTENSION = ".prefab";
        private const string MENU_ITEM_PATH = "Prefab Utility/";
        private const int HIERARCHY_PRIORITY = 0;

        #region Menu Items
        [MenuItem(MENU_ITEM_PATH + "Create Prefab", false, HIERARCHY_PRIORITY)]
        [MenuItem("Assets/Create/Create Blank Prefab", false, HIERARCHY_PRIORITY)]
        static void CreatePrefab(MenuCommand menuCommand)
        {//create blank prefab file

            GameObject obj = new GameObject("new Prefab");
            CreatePrefabFile(obj, PREFAB_FOLDER, false);
            GameObject.DestroyImmediate(obj);
        }

        [MenuItem("GameObject/Prefab Utility/Create Prefab(s) From Selection(s)", false, HIERARCHY_PRIORITY)]
        static void CreateMultiplePrefabsFromSelection(MenuCommand menuCommand)
        {//create INDIVIDUAL prefabs from selected objects, making them unique assets
            if (IsMultipleAndFirstInContextMenu(menuCommand))
            {
                CreateMultiplePrefabsFromSelectionMenu();
            }
            else if (Selection.count == 1)
            {
                CreateMultiplePrefabsFromSelectionMenu();
            }
        }
        [MenuItem(MENU_ITEM_PATH + "Create Prefab(s) From Selection(s)", false, HIERARCHY_PRIORITY)]
        static void CreateMultiplePrefabsFromSelectionMenu()
        {
            GameObject[] objectArray = Selection.gameObjects;

            foreach (GameObject gameObject in objectArray)
            {
                //create prefabs for each object
                CreatePrefabFile(gameObject, PREFAB_FOLDER, true);
            }
        }

        [MenuItem("GameObject/Prefab Utility/Create Single Prefab From Selections", false, HIERARCHY_PRIORITY)]
        static void CreatePrefabFromSelection(MenuCommand menuCommand)
        {//create SINGLE prefab from selected objects, settings those as children
            if (IsMultipleAndFirstInContextMenu(menuCommand))
            {
                CreatePrefabFromSelectionMenu();
            }
        }
        [MenuItem(MENU_ITEM_PATH + "Create Single Prefab From Selections", false, HIERARCHY_PRIORITY)]
        static void CreatePrefabFromSelectionMenu()
        {
            GameObject[] objectArray = Selection.gameObjects;

            GameObject parentObj = new GameObject(objectArray[0].name);

            foreach (GameObject gameObject in objectArray)
            {
                //add selected objects as children
                gameObject.transform.parent = parentObj.transform;
            }
            CreatePrefabFile(parentObj, PREFAB_FOLDER, true);

        }

        //Figure out a way to either combine selected gameobjects into one object then make them all instances of the prefab
        //Make a prefab of the first selected object, then make all others an instance of that prefab (adjusted for scale and position n stuff)
        //check if selected gameobjects are the same enough to make them into a single prefab
        [MenuItem("GameObject/Prefab Utility/Create Single Prefab And Convert All To Instance", false, HIERARCHY_PRIORITY)]
        static void MakeSelectionIntoPrefabsInstances(MenuCommand menuCommand)
        {//create SINGLE prefab from selected objects, then set objects as instances of that prefab

            if (IsMultipleAndFirstInContextMenu(menuCommand))
            {
                MakeSelectionIntoPrefabsInstancesMenu();
            }
        }
        [MenuItem(MENU_ITEM_PATH + "Create Single Prefab And Convert All To Instance", false, HIERARCHY_PRIORITY)]
        static void MakeSelectionIntoPrefabsInstancesMenu()
        {
            GameObject[] objectArray = Selection.gameObjects;

            //create base object for prefabbing
            GameObject parentObj = CreatePrefabFile(objectArray[0], PREFAB_FOLDER, true);

            foreach (GameObject gameObject in objectArray)
            {
                //make objects instance of prefab, storing transform
                Transform originalTransform = gameObject.transform;
                GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(parentObj);
                //set newly instanced prefab's transform to the original object's transform
                prefabInstance.transform.position = originalTransform.position;
                prefabInstance.transform.rotation = originalTransform.rotation;
                prefabInstance.transform.localScale = originalTransform.localScale;
                prefabInstance.transform.parent = originalTransform.parent;
                //destroy original object
                GameObject.DestroyImmediate(gameObject);
            }
        }
        #endregion

        #region Validations
        [MenuItem(MENU_ITEM_PATH + "Create Prefab(s) From Selection(s)", true, HIERARCHY_PRIORITY)]
        [MenuItem("GameObject/Prefab Utility/Create Prefab(s) From Selection(s)", true, HIERARCHY_PRIORITY)]
        static bool ValidateCreateMultiplePrefabsFromSelection()
        {
            return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject);
        }

        [MenuItem(MENU_ITEM_PATH + "Create Single Prefab From Selections", true, HIERARCHY_PRIORITY)]
        [MenuItem("GameObject/Prefab Utility/Create Single Prefab From Selections", true, HIERARCHY_PRIORITY)]
        static bool ValidateCreatePrefabFromSelection()
        {
            return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject) && Selection.gameObjects.Length > 1;
        }

        [MenuItem(MENU_ITEM_PATH + "Create Single Prefab And Convert All To Instance", true, HIERARCHY_PRIORITY)]
        [MenuItem("GameObject/Prefab Utility/Create Single Prefab And Convert All To Instance", true, HIERARCHY_PRIORITY)]
        static bool ValidateCreateSelectionIntoPrefabInstance()
        {
            return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject) && Selection.gameObjects.Length > 1;
        }
        #endregion

        /// <summary>
        /// creates ".prefab" file in the prefab folder specified by given folder path
        /// </summary>
        /// <param name="obj">object to create prefab of</param>
        /// <param name="prefabFolder">folder to put prefab in. this folder will be created in <see cref="PREFAB_FOLDER"/> if it doesn't exist yet</param>
        /// <param name="makeIntoInstance">whether to make the given object into an instance of the created prefab.</param>
        private static GameObject CreatePrefabFile(GameObject obj, string prefabFolder, bool makeIntoInstance)
        {
            // Create folder Prefabs and set the path as within the Prefabs folder,
            if (!Directory.Exists(prefabFolder))
            {
                AssetDatabase.CreateFolder("Assets", prefabFolder.Replace("Assets/", ""));
            }
            string localPath = prefabFolder + "/" + obj.name + PREFAB_EXTENSION;

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                Debug.LogError($"No folder at path\"{prefabFolder}\", creating...");
            }
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

            //focus on project window 
            EditorUtility.FocusProjectWindow();

            //Create the new Prefab and log whether Prefab was saved successfully.
            bool prefabSuccess;
            GameObject prefabObject;
            if (makeIntoInstance == true)
            {
                prefabObject = PrefabUtility.SaveAsPrefabAssetAndConnect(obj, localPath, InteractionMode.UserAction, out prefabSuccess);
            }
            else
            {
                prefabObject = PrefabUtility.SaveAsPrefabAsset(obj, localPath, out prefabSuccess);
                prefabSuccess = true;
            }
            if (prefabSuccess == false)
            {
                Debug.LogError("Prefab failed to save" + prefabSuccess);
            }
            else
            {
                EditorGUIUtility.PingObject(prefabObject);
            }
            return prefabObject;
        }

        private static bool IsMultipleAndFirstInContextMenu(MenuCommand menuCommand)
        {
            if (Selection.count > 1)
            {
                if (menuCommand.context == Selection.objects[0])
                {
                    return true;
                }
            }
            return false;
        }
    }
}
#endif