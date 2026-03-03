using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lyra{
    public class MenuLayoutDataAsset : ScriptableObject {
        public List<MenuLayoutData.ItemLayout> Items = new List<MenuLayoutData.ItemLayout>();

        public string LastSavedAt;
    }
}
