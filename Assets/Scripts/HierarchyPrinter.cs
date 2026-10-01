using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class HierarchyPrinter
{
    [MenuItem("Tools/Print Scene Hierarchy to Console")]
    public static void PrintActiveSceneHierarchy()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            PrintRecursive(root, "- ");
        }
        Debug.Log("--- Hierarchy Export Complete ---");
    }

    private static void PrintRecursive(GameObject obj, string indent)
    {
        Debug.Log(indent + obj.name, obj);
        foreach (Transform child in obj.transform)
        {
            PrintRecursive(child.gameObject, indent + "  ");
        }
    }
}