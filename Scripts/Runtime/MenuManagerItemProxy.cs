using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Lyra{
    [DisallowMultipleComponent]
    [AddComponentMenu("Lyra/Menu Manager Item Proxy")]
    [Icon("Packages/dev.lyrastellate.menu-manager/Scripts/Assets/LMM_Logo.png")]
    public class MenuManagerItemProxy : MonoBehaviour, VRC.SDKBase.IEditorOnly{
        [Tooltip("ビルド後のアイテム名(label)と一致させてください。")]
        public string menuItemName;

        [Tooltip("ビルド後のアイテムのコントロールタイプと一致させてください。")]
        public VRCExpressionsMenu.Control.ControlType controlType = VRCExpressionsMenu.Control.ControlType.Toggle;

        [Tooltip("アイテムが配置されているフォルダのパス。")]
        public List<string> parentFolderPath = new List<string>();
    }
}
