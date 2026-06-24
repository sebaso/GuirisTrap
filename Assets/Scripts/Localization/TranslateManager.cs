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
        int languageColumn = (int) _defaultLanguage;
        _textsDictionary = new Dictionary<string, string>();
        TextAsset fileAsset = Resources.Load<TextAsset>("idiomas");
        // Dividimos el texto por saltos de linea
        string[] lines = fileAsset.text.Split("\n");
        for(int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] columns = line.Split(",");
            _textsDictionary.Add(columns[0], columns[languageColumn]);
        }
    }

    public string GetTextWithKey(string key)
    {
        if(!_textsDictionary.ContainsKey(key)) return "";
            return _textsDictionary[key];
    }

}
