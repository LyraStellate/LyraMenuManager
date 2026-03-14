using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Lyra.Editor{
    [CustomEditor(typeof(MenuManagerItemProxy))]
    [CanEditMultipleObjects]
    public class MenuManagerItemProxyEditor : UnityEditor.Editor{
        private GUIStyle _sCrumbNorm;
        private GUIStyle _sCrumbBold;
        private GUIStyle _sCrumbSep;

        private void InitStyles(){
            if (_sCrumbNorm != null) return;

            _sCrumbNorm = new GUIStyle(EditorStyles.label) {
                fontSize = 11,
                padding = new RectOffset(4, 4, 2, 2),
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            _sCrumbBold = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 11,
                padding = new RectOffset(4, 4, 2, 2),
                normal = { textColor = Color.white }
            };

            _sCrumbSep = new GUIStyle(EditorStyles.miniLabel) {
                fontSize = 10,
                padding = new RectOffset(0, 0, 2, 2),
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
        }

        public override void OnInspectorGUI(){
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "外部スクリプトによってビルド時に生成されるメニューアイテムのプロキシです。\n" +
                "レイアウト設定を適用するために、生成後のアイテム情報を設定してください。", 
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("アイテム設定", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)){
                EditorGUILayout.PropertyField(serializedObject.FindProperty("menuItemName"), new GUIContent("Menu Item Name", "ビルド後のアイテム名(label)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("controlType"), new GUIContent("Control Type", "ビルド後のアイテムタイプ"));
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("取得パス", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)){
                var pathProp = serializedObject.FindProperty("parentFolderPath");
                
                DrawPathPreview(pathProp);

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(pathProp, new GUIContent("Folder Path List", "ルートからのフォルダ階層を順に指定"), true);
            }

            EditorGUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPathPreview(SerializedProperty pathProp){
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview:");

            GUILayout.Label("Root", _sCrumbNorm);

            if (pathProp.arraySize > 0){
                for (int i = 0; i < pathProp.arraySize; i++){
                    GUILayout.Label(">", _sCrumbSep);
                    string folderName = pathProp.GetArrayElementAtIndex(i).stringValue;
                    if (string.IsNullOrEmpty(folderName)) folderName = "(Empty)";
                    GUILayout.Label(folderName, _sCrumbNorm);
                }
            }

            var nameProp = serializedObject.FindProperty("menuItemName");
            string itemName = string.IsNullOrEmpty(nameProp.stringValue) ? "Item" : nameProp.stringValue;
            GUILayout.Label(">", _sCrumbSep);
            GUILayout.Label(itemName, _sCrumbBold);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            Rect lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(lastRect.x, lastRect.yMax, lastRect.width, 1), new Color(1, 1, 1, 0.1f));
        }
    }
}
