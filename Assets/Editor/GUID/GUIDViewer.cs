using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GUIDViewer: EditorWindow
{
    private double lastSelectionTime;
    private Vector2 scrollPos;
    private readonly List<SelectMetaInfo> infos = new();
    
    [MenuItem("Tools/Windows/Guid Viewer")]
    [MenuItem("Assets/Guid Viewer", false, 2000)]
    public static void OpenWindow()
    {
        var window = GetWindow<GUIDViewer>("Guid Viewer");
        window.minSize = new Vector2(600, 300);
    }

    void UpdateSelectionInfos()
    {
        infos.Clear();
        
        var objects = Selection.objects;
        if (objects == null || objects.Length == 0)
        {
            return;
        }

        foreach (var obj in objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath))
            {
                infos.Add(new SelectMetaInfo()
                {
                    Name = obj.name,
                    Type = null,
                    AssetPath = "{ Invalid asset path. }",
                    MetaPath = "{ Invalid .meta path. }",
                    Guid = "{ N/A }",
                    ExistMeta = false,
                });
                continue;
            }

            string fullAssetPath = Path.GetFullPath(assetPath);
            string fullMetaPath = fullAssetPath + ".meta";
            string guid = "(not found)";

            bool exist = File.Exists(fullMetaPath);
            if (exist)
            {
                try
                {
                    var lines = File.ReadAllLines(fullMetaPath);
                    var guidLine = lines.FirstOrDefault(line => line.TrimStart().StartsWith("guid:"));
                    if (!string.IsNullOrEmpty(guidLine))
                    {
                        var splits = guidLine.Split(':');
                        guid = splits.Length > 1 ? splits[1].Trim() : guidLine.Trim();
                    }
                    else
                    {
                        var idx = string.Join("\n", lines).IndexOf("guid:");
                        if (idx >= 0)
                        {
                            guid = "(found but parse failed)";
                        }
                    }
                }
                catch (Exception e)
                {
                    guid = "(read error: " + e.Message + ")";
                }    
            }
            
            infos.Add(new SelectMetaInfo()
            {
                Name = obj.name,
                Type = obj.GetType(),
                AssetPath = assetPath,
                MetaPath = exist ? fullMetaPath : "{ .meta not found. }",
                Guid = guid,
                ExistMeta = exist,
            });
        }
    }

    private void OnEnable()
    {
        UpdateSelectionInfos();
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        UpdateSelectionInfos();
        lastSelectionTime = EditorApplication.timeSinceStartup;
        
        Repaint();
    }

    private void Update()
    {
        if (EditorApplication.timeSinceStartup - lastSelectionTime > 1f)
        {
            UpdateSelectionInfos();
            lastSelectionTime = double.MaxValue;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Selected Count: " + Selection.objects.Length);
        EditorGUILayout.Space();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if (infos.Count == 0)
        {
            EditorGUILayout.HelpBox("未选中任何 资源（文件或文件夹）， 在 Project 窗口选中查看。", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < infos.Count; ++i)
            {
                var info = infos[i];
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(info.Name + $" ({info.Type.Name})", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("asset: ", GUILayout.Width(80));
                if (GUILayout.Button("Ping", GUILayout.Height(20)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(info.AssetPath);
                    if(obj)
                        EditorGUIUtility.PingObject(obj);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("meta path: ", GUILayout.Width(80));
                EditorGUILayout.SelectableLabel(info.MetaPath, GUILayout.Height(20));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("guid: ", GUILayout.Width(80));
                EditorGUILayout.SelectableLabel(info.Guid, GUILayout.Height(20));
                if (GUILayout.Button("Copy", GUILayout.Height(20)))
                {
                    EditorGUIUtility.systemCopyBuffer = info.Guid;
                    ShowNotification(new GUIContent("Copy Successful.", GetIcon(), string.Empty), 1f);
                }
                if (!info.ExistMeta)
                {
                    if (GUILayout.Button("Reveal", GUILayout.Height(20)))
                    {
                        var abs = Path.GetFullPath(info.AssetPath);
                        EditorUtility.RevealInFinder(abs);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUILayout.Height(28), GUILayout.Width(300)))
        {
            UpdateSelectionInfos();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        var text = "提示: 选择 Project 窗口中的资源（文件或文件夹），窗口会显示对应的 Guid。";
        var style = EditorStyles.miniLabel;
        style.normal.textColor = Color.yellow;
        var size = style.CalcSize(new GUIContent(text));
        EditorGUILayout.LabelField(text, style, GUILayout.Width(size.x));
        EditorGUILayout.EndHorizontal();
    }
    

    Texture GetIcon()
    {
        var icon = (Texture)EditorGUIUtility.Load("Icons/Collab.Check.01.png");
        return icon;
    }
    

    class SelectMetaInfo
    {
        public string Name;
        public Type Type;
        public string AssetPath;
        public string MetaPath;
        public string Guid;
        public bool ExistMeta;
    }
}


