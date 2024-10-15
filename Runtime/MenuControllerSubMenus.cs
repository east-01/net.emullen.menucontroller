using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using EMullen.PlayerMgmt;
using EMullen.Core;

namespace EMullen.MenuController 
{
    public partial class MenuController : MonoBehaviour 
    {
        public MenuController ParentMenu { get; private set; }

        [SerializeField]
        protected List<SubMenuData> subMenus;
        public List<MenuController> SubMenus => subMenus.Select(smStr => GetSubMenu(smStr.id)).ToList();
        public bool IsSubMenuOpen => SubMenus.Any(sm => sm.IsOpen);

        /// <summary>
        /// Cache the submenus by ID for easy reference with GetSubMenu
        /// </summary>
        private Dictionary<string, SubMenuData> _cachedSubmenus;
        private Dictionary<string, SubMenuData> cachedSubmenus { get {
            if(_cachedSubmenus == null)
                InitializeSubMenus();
            return _cachedSubmenus;
        } }
        public bool AreSubmenusCached { get; private set; } = false;

        [SerializeField]
        private InputSystemUIInputModule inputSystemUIInputModule;
        /// <summary>
        /// The UIInputModule that this MenuController will use. If null, we will try to use the parent UIInputModule, this
        ///   search happens recursively until we reach an existing one on a parent.
        /// </summary>
        public InputSystemUIInputModule InputSystemUIInputModule { get { 
            if(inputSystemUIInputModule != null) {
                usedISUIM = "On self: " + inputSystemUIInputModule.GetInstanceID();
                return inputSystemUIInputModule;
            } else if(ParentMenu != null) {
                usedISUIM = "On parent->" + ParentMenu.usedISUIM;
                return ParentMenu.InputSystemUIInputModule;
            } else if(MenuControllerSettings.Instance != null) {
                usedISUIM = "On settings instance";
                return MenuControllerSettings.Instance.InputSystemUIInputModule;
            } else  {
                Debug.LogError("Couldn't assingn an InputSystemUIInputModule");
                return null;
            }
        } }
        public string usedISUIM = "";

        [SerializeField]
        private EventSystem eventSystem;
        /// <summary>
        /// The EventSystem this MenuController will use. If null, we will try to use the parent EventSystem, this search happens
        ///   recursively until we reach an existing one on a parent.
        /// </summary>
        public EventSystem EventSystem { get { 
            if(eventSystem != null)
                return eventSystem;
            else if(ParentMenu != null) {
                return ParentMenu.EventSystem;
            } else if(MenuControllerSettings.Instance != null) {
                return MenuControllerSettings.Instance.EventSystem;
            } else {
                Debug.LogError("Failed to get EventSystem");
                return null;
            }
        } }

        /// <summary>
        /// Get a sub menu controller from its string id
        /// </summary>
        public MenuController GetSubMenu(string id) 
        {
            SubMenuData? subMenuData = GetSubMenuData(id);
            if(!subMenuData.HasValue)
                return null;
            return subMenuData.Value.menuController;
        }

        protected SubMenuData? GetSubMenuData(string id) 
        {
            if(!cachedSubmenus.ContainsKey(id))
                return null;
            return cachedSubmenus[id];
        }

        /// <summary>
        /// Send the current menu back to the one before it.
        /// Default implementation will send a submenu to a parent menu if it exists.
        /// </summary>
        public virtual void SendMenuBack() 
        {
            BLog.Highlight("Default sendmenuback implementation");
            Close();

            if(ParentMenu != null)
                ParentMenu.Open();
        }

        /// <summary>
        /// Disables all sub-menu GameObjects and sets their parent MenuController to this
        /// </summary>
        private void InitializeSubMenus() 
        {
            if(_cachedSubmenus != null && _cachedSubmenus.Count > 0)
                Debug.LogWarning($"Caching submenus on \"{this}\" but there's already {cachedSubmenus.Count} cached. This probably shouldn't happen.");
            
            _cachedSubmenus = new();

            foreach(SubMenuData smd in subMenus) {
                if(smd.id.Length == 0) {
                    Debug.LogError($"SubMenu data on \"{this}\" has an empty string. Not caching it.");
                    continue;
                }
                if(_cachedSubmenus.ContainsKey(smd.id)) {
                    Debug.LogError($"SubMenu data on \"{this}\" has an identical id ({smd.id}) already cached. Not caching it.");
                    continue;
                }

                _cachedSubmenus.Add(smd.id, smd);

                MenuController subMenu = smd.menuController;

                subMenu.ParentMenu = this;
                subMenu.Close();
            }
        }
    }
}