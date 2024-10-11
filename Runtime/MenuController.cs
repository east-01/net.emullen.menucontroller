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
    public abstract class MenuController : MonoBehaviour
    {

        private static BLogChannel logSettings;
        public static BLogChannel LogSettings { get {
            if(logSettings != null) {
                logSettings = ScriptableObject.CreateInstance<BLogChannel>();
                logSettings.color = new Color(0.125f, 0.5f, 0.777f);
                logSettings.enable = true;
                logSettings.verbosity = 5;
                logSettings.logName = "MenuCont";
            }
            return logSettings;
        } }

        [SerializeField]
        protected OpenCloseType openCloseType;

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
            } else if(parentMenu != null) {
                usedISUIM = "On parent->" + parentMenu.usedISUIM;
                return parentMenu.InputSystemUIInputModule;
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
            else if(parentMenu != null) {
                return parentMenu.EventSystem;
            } else if(MenuControllerSettings.Instance != null) {
                return MenuControllerSettings.Instance.EventSystem;
            } else {
                Debug.LogError("Failed to get EventSystem");
                return null;
            }
        } }

        [SerializeField]
        protected Selectable firstSelect;
        /// <summary>
        /// Automatically focus on player one when they are available.
        /// Does not happen if there's already a focused player.
        /// </summary>
        [SerializeField]
        private bool autoFocusOnPlayerOne;
        [SerializeField]
        private bool hidesParent = true;
        [SerializeField]
        private bool hidesSiblings = true;
        /// <summary>
        /// This field is for MenuControllers who are sitting open in a scene, think a TitleScreen
        ///   MenuController that is the first thing you see when you start the game.
        /// If this field is set to true we will call Open for this instance on Start
        /// </summary>
        [SerializeField]
        private bool autoOpen = false;

        [SerializeField]
        protected List<SubMenuData> subMenus;
        [SerializeField]
        protected List<ToolTip> tooltips;

        protected LocalPlayer focusedPlayer;
        private string focusedPlayerInitialActionMap; // Stores the action map of the focused player so we can revert them to it once they become unfocused.
        protected MenuController parentMenu;
        protected CanvasGroup canvasGroup;
        protected bool allowInputEvents = true;
        private bool menuControllerLoadedProperly = false;

        protected void Awake() 
        {
            if(openCloseType == OpenCloseType.CANVAS_GROUP && !TryGetComponent(out canvasGroup))
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if(TryGetComponent(out Canvas canvas) && !canvas.enabled) {
                canvas.enabled = true;
                Close();
            }

            InitializeSubMenus();

            menuControllerLoadedProperly = true;
        }

        protected void Start() 
        {
            if(autoOpen)
                Open();
        }

        protected void OnEnable() 
        {
            PlayerManager.Instance.LocalPlayerJoinedEvent += PlayerManager_LocalPlayerJoined;
        }

        protected void OnDestroy() 
        {
            PlayerManager.Instance.LocalPlayerJoinedEvent -= PlayerManager_LocalPlayerJoined;
            if(focusedPlayer != null)
                RemoveFocus();
        }

        protected void OnDisable() 
        {
            if(focusedPlayer != null)
                RemoveFocus();
        }

        protected void LateUpdate() 
        {
            if(focusedPlayer == null && autoFocusOnPlayerOne && PlayerManager.Instance != null && PlayerManager.Instance.LocalPlayers[0] != null)
                SetFocus(PlayerManager.Instance.LocalPlayers[0]);

            if(focusedPlayer != null && EventSystem.currentSelectedGameObject == null && firstSelect != null && focusedPlayer.Input.currentControlScheme != "KeyboardMouse")
                EventSystem.SetSelectedGameObject(firstSelect.gameObject);

            if(!menuControllerLoadedProperly)
                Debug.LogError($"MenuController script \"{this}\" on \"{gameObject.name}\" wasn't loaded properly. Make sure you call base.Awake() if you're overriding it.");
        }

    #region Focus
        public void SetFocus(LocalPlayer playerObj) 
        {
            /* Passing focus */
            if(focusedPlayer != null) {
                if(focusedPlayer.UID == playerObj.UID) {
                    BLog.Log($"{this}: Maintaining focus on {playerObj.Input.playerIndex}", LogSettings, 4);
                    return;
                } else {
                    BLog.Log($"{this}: Removing focus from {focusedPlayer.Input.playerIndex} and placing it on {playerObj.Input.playerIndex}", LogSettings, 4);
                    RemoveFocus();
                }
            } else {
                BLog.Log($"{this}: No focus existing, placing focus on {playerObj.Input.playerIndex}", LogSettings, 4);
            }

            /* Assign focus */
            focusedPlayer = playerObj;
            focusedPlayer.Input.onActionTriggered += PlayerInput_ActionTriggered;
            focusedPlayer.Input.onControlsChanged += PlayerInput_OnControlsChanged;

            focusedPlayerInitialActionMap = focusedPlayer.Input.currentActionMap.name;

            // The key is that the player should never be assigned to a InputSystemUIInputModule when they're not in ui action map
            if(focusedPlayer.Input.currentActionMap.name != "UI")
                focusedPlayer.Input.SwitchCurrentActionMap("UI");
            
            focusedPlayer.Input.uiInputModule = InputSystemUIInputModule;
            /* REVIEW: We have to use default actions here. (fuck new input system for real, if you're going to make something as convolouted as possible why wouldn't you print warnings for stuff like this.)
            The problem: Whenever we try to change focus the InputSystemUIInputModule
            decides to lose all its bindings to the UI input actions and the reference
            to the PlayerControls file gets messed up.
            There are no warning messages why this happens and why it happens isn't clear.
            If we can somehow maintain our bindings when switching focus then the problem
            will be fixed.
            */
            InputSystemUIInputModule.AssignDefaultActions();

            // BLog.Highlight($"##### SETTING FOCUS FOR PLAYER {focusedPlayer.PlayerIndex} on {this.GetType()} #####");
            // BLog.Highlight($"UIInModule: {InputSystemUIInputModule.GetInstanceID()} EventSystem: {EventSystem.GetInstanceID()}");
            // BLog.Highlight($"Used: {usedISUIM}");
            // BLog.Highlight($"Action map: {focusedPlayer.Input.currentActionMap.name} prev AM: {focusedPlayerInitialActionMap}");
            // BLog.Highlight($"Actions asset: {InputSystemUIInputModule.actionsAsset.actionMaps}");

            tooltips.ForEach(tt => tt.SetObservedInput(focusedPlayer.Input));

            if(ShouldSelect && firstSelect != null) {
                if(EventSystem != null) {
                    EventSystem.SetSelectedGameObject(firstSelect.gameObject);
                } else
                    Debug.LogWarning($"MenuController \"{this}\" failed to find an EventSystem. This may be a misconfiguration, ensure that an EventSystem is assigned on this script or in a parent MenuController.");
            }
        }

        public void RemoveFocus() 
        {
            if(focusedPlayer == null)
                return;

            if(focusedPlayer.Input != null) {
                focusedPlayer.Input.uiInputModule = null;
                if(focusedPlayer.Input.enabled)
                    focusedPlayer.Input.SwitchCurrentActionMap(focusedPlayerInitialActionMap);
            }

            // BLog.Highlight($"##### REMOVED FOCUS FOR PLAYER {focusedPlayer.PlayerIndex} #####");

            focusedPlayer.Input.onActionTriggered -= PlayerInput_ActionTriggered;
            focusedPlayer = null;
            focusedPlayerInitialActionMap = null;
        }
    #endregion

    #region Events
        private void PlayerManager_LocalPlayerJoined(LocalPlayer obj) 
        {
            if(IsOpen && obj.Input.playerIndex == 0 && autoFocusOnPlayerOne && focusedPlayer == null)
                SetFocus(obj);
        }

        /// <summary>
        /// Input events from the currently focused player.
        /// </summary>
        protected void PlayerInput_ActionTriggered(InputAction.CallbackContext context) 
        {
            if(!allowInputEvents)
                return;
                
            if(context.action.name != "Point")
                BLog.Log($"MenuController \"{this}\" (focus: \"{(focusedPlayer != null ? focusedPlayer.Input.playerIndex : "-")}\") recieved input event \"{context.action.name}\"", LogSettings, 5);
            if(context.performed && context.action.name == MenuControllerSettings.Instance.CancelAction.name) {
                SendMenuBack();
            }

            Child_PlayerInput_ActionTriggered(context);
        }

        private void PlayerInput_OnControlsChanged(PlayerInput input)
        {
            
        }

        /// <summary>
        /// More Input Action events called from the MenuController class so that children can recieve them.
        /// i.e. If you have a PlayerSelect MenuController and you want to recieve input action events, you'll
        ///   override this method to get those events instead of subscribing to the playerInput directly.
        /// </summary>
        protected virtual void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context) {}
    #endregion

    #region Open and Close
        public void Open(LocalPlayer focus = null) 
        {
            if(hidesParent && parentMenu != null)
                parentMenu.Close();
            else if(parentMenu != null)
                parentMenu.RemoveFocus();

            if(!gameObject.activeSelf) 
                gameObject.SetActive(true);

            if(openCloseType == OpenCloseType.GAME_OBJECT_ENABLE_DISABLE)
                gameObject.SetActive(true);
            else if(openCloseType == OpenCloseType.CANVAS_GROUP) {
                canvasGroup.alpha = 1.0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if(focus != null)
                SetFocus(focus);
            else if(autoFocusOnPlayerOne && PlayerManager.Instance.LocalPlayers[0] != null)
                SetFocus(PlayerManager.Instance.LocalPlayers[0]);
            else
                RemoveFocus();

            if(EventSystem != null)
                EventSystem.SetSelectedGameObject(null);

            BLog.Log($"MenuController \"{this}\" opened with {(focusedPlayer != null ? $"focus \"{focusedPlayer.Input.playerIndex}\"" : "no focus")}", LogSettings, 0);
            Opened();
        }

        /// <summary>
        /// Callback for when this MenuController was opened after the focus is set.
        /// </summary>
        protected virtual void Opened() {}

        public void Close() 
        {
            Closed();
            BLog.Log($"MenuController \"{this}\" closed", LogSettings, 2);
            RemoveFocus();
        
            if(openCloseType == OpenCloseType.GAME_OBJECT_ENABLE_DISABLE)
                gameObject.SetActive(false);
            else if(openCloseType == OpenCloseType.CANVAS_GROUP) {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if(EventSystem != null)
                EventSystem.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Callback for when this MenuController was closed, BEFORE we lose focus and it is disabled.
        /// </summary>
        protected virtual void Closed() {}
    #endregion

    #region Navigation
        /// <summary>
        /// Send the current menu back to the one before it.
        /// Default implementation will send a submenu to a parent menu if it exists.
        /// </summary>
        protected virtual void SendMenuBack() 
        {
            BLog.Highlight("Default sendmenuback implementation");
            Close();

            if(parentMenu != null)
                parentMenu.Open();
        }

        public void SendMenuBackPublic() { SendMenuBack(); } // how to never get hired

        public void OpenSubMenu(string id, LocalPlayer focus = null) 
        {
            MenuController subMenu = GetSubMenu(id);
            if(subMenu == null) {
                Debug.LogError($"MenuController \"{this}\" failed to open SubMenu id \"{id}\" (subMenu is null).");
                return;
            }

            if(subMenu.hidesSiblings) {
                subMenus.ForEach(smd => {
                    MenuController sm = GetSubMenu(smd.id);
                    if(sm.IsOpen)
                        sm.Close();
                });
            }

            BLog.Log($"Menu \"{this}\" opening submenu \"{id}\"", LogSettings, 1);
            subMenu.Open(focus);
        }
    #endregion

    #region SubMenus
        /// <summary>
        /// Cache the submenus by ID for easy reference with GetSubMenu
        /// </summary>
        private Dictionary<string, SubMenuData> cachedSubmenus;
        public bool AreSubmenusCached { get; private set; } = false;

        /// <summary>
        /// Disables all sub-menu GameObjects and sets their parent MenuController to this
        /// </summary>
        private void InitializeSubMenus() 
        {
            if(cachedSubmenus != null && cachedSubmenus.Count > 0)
                Debug.LogWarning($"Caching submenus on \"{this}\" but there's already {cachedSubmenus.Count} cached. This probably shouldn't happen.");
            
            cachedSubmenus = new();

            foreach(SubMenuData smd in subMenus) {
                if(smd.id.Length == 0) {
                    Debug.LogError($"SubMenu data on \"{this}\" has an empty string. Not caching it.");
                    continue;
                }
                if(cachedSubmenus.ContainsKey(smd.id)) {
                    Debug.LogError($"SubMenu data on \"{this}\" has an identical id ({smd.id}) already cached. Not caching it.");
                    continue;
                }

                cachedSubmenus.Add(smd.id, smd);

                MenuController subMenu = smd.menuController;

                subMenu.SetParentMenuController(this);
                subMenu.Close();
            }

            AreSubmenusCached = true;
        }

        public void SetParentMenuController(MenuController parent) { this.parentMenu = parent; }

        protected SubMenuData? GetSubMenuData(string id) 
        {
            if(!cachedSubmenus.ContainsKey(id))
                return null;
            return cachedSubmenus[id];
        }

        /// <summary>
        /// Get a sub menu controller from its string id
        /// </summary>
        public MenuController GetSubMenu(string id) 
        {
            if(!cachedSubmenus.ContainsKey(id))
                return null;
            return GetSubMenuData(id).Value.menuController;
        }
    #endregion

        public bool ShouldSelect { get {
            if(focusedPlayer == null)
                return true;
            return focusedPlayer.Input.currentControlScheme != "KeyboardMouse";
        } }

        public bool IsOpen { get { 
            if(openCloseType == OpenCloseType.GAME_OBJECT_ENABLE_DISABLE) {
                return gameObject.activeSelf;
            } else if(openCloseType == OpenCloseType.CANVAS_GROUP)
                return canvasGroup.interactable && canvasGroup.alpha == 1; 
            else
                return false;
        } }
        
        public List<MenuController> SubMenus => subMenus.Select(smStr => GetSubMenu(smStr.id)).ToList();
        public bool IsSubMenuOpen { get {
            return subMenus.Any(sm => GetSubMenu(sm.id).IsOpen);
        } }

        public LocalPlayer FocusedPlayer => focusedPlayer; 
        /// <summary> Utility to get the focused player (if it exists) or a focusedPlayer in child MenuControllers. </summary>
        public LocalPlayer FocusedPlayerIncludingChildren { get {
            if(focusedPlayer != null)
                return focusedPlayer;
            foreach(MenuController subMenu in SubMenus) {
                if(subMenu.FocusedPlayerIncludingChildren != null)
                    return subMenu.FocusedPlayerIncludingChildren;
            }
            return null;
        } }
    }

    [Serializable]
    public struct SubMenuData 
    {
        public string id;
        public MenuController menuController;   
    }

    public enum OpenCloseType { GAME_OBJECT_ENABLE_DISABLE, CANVAS_GROUP }
}