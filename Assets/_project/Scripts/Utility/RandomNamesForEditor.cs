#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KitchenDesign.Utility
{
    public static class RandomNamesForEditor
    {
        private const string NamesPath = "Assets/AssetsMelcher/Editor/RandomNames.txt";
        private const string PlacesPath = "Assets/AssetsMelcher/Editor/RandomPlaces.txt";

        private static List<string> s_Names;
        private static List<string> s_Places;

        public static string RandomName()
        {
            s_Names ??= GetLinesFromAsset(NamesPath);
            return s_Names[UnityEngine.Random.Range(0, s_Names.Count)];
        }

        public static string RandomPlace()
        {
            s_Places ??= GetLinesFromAsset(PlacesPath);
            return s_Places[UnityEngine.Random.Range(0, s_Places.Count)];
        }

        private static List<string> GetLinesFromAsset(string assetPath)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

            string[] lines = asset.text.Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            return new List<string>(lines);
        }
    }
}
#endif
