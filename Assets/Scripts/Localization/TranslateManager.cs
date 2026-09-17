using System.Collections.Generic;
using NUnit.Framework.Internal;
using UnityEngine;

public class TranslateManager : MonoBehaviour
{
    public enum Language
    {
        Spanish = 1,
        English = 2
    }
    [SerializeField]
    private Language _defaultLanguage = Language.Spanish;
    [SerializeField]
    private TextAsset[] _textFiles;

    private Dictionary<string, string> _textsDictionary;

    private static TranslateManager _instance;
    public static TranslateManager Instance => _instance;

    #if UNITY_EDITOR
    // TEST
    public string testKey;
    [ContextMenu("Load Key")]
    public void LoadTestKey()
    {
        LoadLanguage();
        string text = GetTextWithKey(testKey);
        Debug.Log(text);
    }
    #endif
    void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            LoadLanguage();
        }
        else
        {
            Destroy(this);
        }
    }
    [ContextMenu("Load Language")]
    private void LoadLanguage()
    {
        int languageColumn = (int)_defaultLanguage;
        _textsDictionary = new Dictionary<string, string>();

        foreach (TextAsset fileAsset in _textFiles)
        {
            if (fileAsset == null)
            {
                Debug.LogWarning("Hay un archivo de textos vacío en la lista.");
                continue;
            }

            string[] lines = fileAsset.text.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = line.Split("/");
                if (!_textsDictionary.ContainsKey(columns[0]))
                    _textsDictionary.Add(columns[0], columns[languageColumn]);
                else
                    Debug.LogWarning($"Clave duplicada: {columns[0]} en {fileAsset.name}");
            }
        }
    }

    public string GetTextWithKey(string key)
    {
        if(!_textsDictionary.ContainsKey(key)) return "";
            return _textsDictionary[key];
    }

}
