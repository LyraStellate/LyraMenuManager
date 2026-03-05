#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Lyra.Editor{
    public partial class MenuManager{
        private string GetOriginalName(MenuEntry e){
            if (e.SourceMenuItem != null) return e.SourceMenuItem.gameObject.name;
            if (e.SourceInstaller != null) return e.SourceInstaller.gameObject.name;
            if (e.SourceAsset != null && e.SourceIndex >= 0 && e.SourceIndex < e.SourceAsset.controls.Count){
                return e.SourceAsset.controls[e.SourceIndex].name;
            }
            return "New Folder";
        }

        private VRCAvatarDescriptor GrabAvatarFromDrag(){
            foreach (var o in DragAndDrop.objectReferences)
                if (o is GameObject go){
                    var d = go.GetComponent<VRCAvatarDescriptor>();
                    if (d != null) return d;
                }
            return null;
        }

        private bool InRing(Vector2 p, Vector2 c){
            float d = (p - c).magnitude;
            return d >= INNER_RADIUS && d <= WHEEL_RADIUS;
        }

        private Texture2D GetIcon(string iconName){
            if (_iconCache == null) _iconCache = new Dictionary<string, Texture2D>();
            if (_iconCache.TryGetValue(iconName, out var cached) && cached != null) return cached;

            string assetName = System.IO.Path.GetFileNameWithoutExtension(iconName);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Lyra/EditorTool/MenuManager/Icon/{iconName}");
            if (tex == null){
                var guids = AssetDatabase.FindAssets($"{assetName} t:Texture2D");
                foreach (var g in guids){
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.EndsWith(iconName, StringComparison.OrdinalIgnoreCase)){
                        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                        if (p.Contains("Lyra")) break;
                    }
                }
            }
            _iconCache[iconName] = tex;
            return tex;
        }

        private int CalcSlice(Vector2 p, Vector2 c, int n, float startA){
            var v = p - c;
            float a = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg;
            if (a < 0) a += 360f;
            float off = ((startA - a) % 360f + 360f) % 360f;
            return Mathf.FloorToInt(off / (360f / n)) % n;
        }

        private int CalcNearestBorder(Vector2 p, Vector2 c, int sliceCount, float startAngle, float step, int maxEntries){
            var v = p - c;
            float a = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg;
            if (a < 0) a += 360f;
            float off = ((startAngle - a) % 360f + 360f) % 360f;
            float withinSlice = off % step;
            int sliceIdx = Mathf.FloorToInt(off / step) % sliceCount;

            float threshold = step * 0.18f;
            int border = -1;
            if (withinSlice < threshold)
                border = sliceIdx;
            else if (withinSlice > step - threshold)
                border = (sliceIdx + 1) % sliceCount;

            if (border > maxEntries) border = -1;

            return border;
        }

        private Color TypeColor(VRCExpressionsMenu.Control.ControlType t){
            switch (t){
                case VRCExpressionsMenu.Control.ControlType.Toggle: return C_TOGGLE;
                case VRCExpressionsMenu.Control.ControlType.Button: return C_BUTTON;
                case VRCExpressionsMenu.Control.ControlType.SubMenu: return ACCENT_SUB;
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet: return C_RADIAL;
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet: return C_2AXIS;
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet: return C_4AXIS;
                default: return TEXT_SEC;
            }
        }

        private string TypeShort(VRCExpressionsMenu.Control.ControlType t){
            switch (t){
                case VRCExpressionsMenu.Control.ControlType.Toggle: return "Toggle";
                case VRCExpressionsMenu.Control.ControlType.Button: return "Button";
                case VRCExpressionsMenu.Control.ControlType.SubMenu: return "Sub ▶";
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet: return "Radial";
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet: return "2Axis";
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet: return "4Axis";
                default: return "?";
            }
        }

        private void DrawTypeIcon(Vector2 c, MenuEntry e, float s = 26){
            var tc = TypeColor(e.Type);
            var r = new Rect(c.x - s / 2, c.y - s / 2, s, s);
            EditorGUI.DrawRect(r, tc * 0.3f);
            DrawBorder(r, tc * 0.6f, 1f);

            string ico;
            switch (e.Type){
                case VRCExpressionsMenu.Control.ControlType.Toggle: ico = "◉"; break;
                case VRCExpressionsMenu.Control.ControlType.Button: ico = "◎"; break;
                case VRCExpressionsMenu.Control.ControlType.SubMenu: ico = "▶"; break;
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet: ico = "◐"; break;
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet: ico = "✛"; break;
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet: ico = "✦"; break;
                default: ico = "?"; break;
            }
            var oldTC = _sCenter.normal.textColor;
            var oldSize = _sCenter.fontSize;
            _sCenter.normal.textColor = tc;
            _sCenter.fontSize = (int)(14 * (s / 26f));
            GUI.Label(r, ico, _sCenter);
            _sCenter.fontSize = oldSize;
            _sCenter.normal.textColor = oldTC;
        }

        private void DrawDisc(Vector2 c, float r, Color col){
            Handles.BeginGUI();
            Handles.color = col;
            Handles.DrawSolidDisc(c, Vector3.forward, r);
            Handles.EndGUI();
        }

        private void DrawWireDisc(Vector2 c, float r, Color col, float t){
            Handles.BeginGUI();
            Handles.color = col;
            Handles.DrawWireDisc(c, Vector3.forward, r, t);
            Handles.EndGUI();
        }

        private void DrawSlice(Vector2 c, float ri, float ro, float a0, float a1, Color col){
            if (Event.current.type != EventType.Repaint) return;
            Handles.BeginGUI();
            Handles.color = col;
            Vector3 from = new Vector3(Mathf.Cos(a0 * Mathf.Deg2Rad), -Mathf.Sin(a0 * Mathf.Deg2Rad), 0);
            Handles.DrawSolidArc(c, Vector3.forward, from, (a0 - a1), ro);
            Handles.EndGUI();
        }

        private void DrawHandleLine(Vector2 a, Vector2 b, Color col, float t){
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            var prevColor = GUI.color;
            var prevMatrix = GUI.matrix;
            
            GUI.color = col;
            Vector2 dir = b - a;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float length = dir.magnitude;
            
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - t / 2f, length, t), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            
            GUI.matrix = prevMatrix;
            GUI.color = prevColor;
        }

        private void DrawBorder(Rect r, Color c, float t){
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        private bool CheckProLimit(int depth){
            return MenuManagerAuthGuard.GuardedNavInto(depth);
        }
    }
}

#endif
