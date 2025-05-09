using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using EMullen.PlayerMgmt;
using EMullen.Core;
using FishNet.Component.Prediction;

namespace EMullen.MenuController 
{
    [RequireComponent(typeof(CanvasGroup))]
    public partial class MenuController : MonoBehaviour
    {

        private static BLogChannel logSettings;
        public static BLogChannel LogSettings { get {
            if(logSettings != null) {
                logSettings = ScriptableObject.CreateInstance<BLogChannel>();
                logSettings.color = new Color(0.125f, 0.5f, 0.777f);
                logSettings.enable = true;
                logSettings.verbosity = 4;
                logSettings.logName = "MenuCont";
            }
            return logSettings;
        } }

        /// <summary>
        /// Is the MenuController open. Updated each frame in LateUpdate() so only one
        ///   MenuController can have IsOpen as true each frame.
        /// </summary>
        public bool IsOpen { get; private set; }

        [SerializeField]
        protected Selectable firstSelect;
        /// <summary>
        /// Should the MenuController select a Selectable (used for firstSelect)
        /// </summary>
        public bool ShouldSelect { get {
            if(FocusedPlayer == null)
                return true;
            if(FocusedPlayer.Input.currentControlScheme == null)
                return true;
            string currentControlScheme = FocusedPlayer.Input.currentControlScheme.ToLower();
            return !currentControlScheme.Contains("mouse");
        } }

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
        private MenuStartBehaviour startBehaviour = MenuStartBehaviour.CLOSED;

        [SerializeField]
        protected List<ToolTip> tooltips;

        private CanvasGroup _canvasGroup;
        protected CanvasGroup canvasGroup { get {
            if(!TryGetComponent(out _canvasGroup))
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            return _canvasGroup;
        } }
        protected bool allowInputEvents = true;
        private bool menuControllerLoadedProperly = false;

        protected void Awake() 
        {
            InitializeSubMenus();

            menuControllerLoadedProperly = true;
        }

        protected void OnDestroy() 
        {            
            if(FocusedPlayer != null)
                RemoveFocus();
        }

        protected void OnEnable() 
        {
            PlayerManager.Instance.LocalPlayerJoinedEvent += PlayerManager_LocalPlayerJoined;
        }

        protected void OnDisable() 
        {
            PlayerManager.Instance.LocalPlayerJoinedEvent -= PlayerManager_LocalPlayerJoined;
            if(FocusedPlayer != null)
                RemoveFocus();
        }

        protected void Start() 
        {
            if(startBehaviour == MenuStartBehaviour.OPEN)
                Open();
            else if(startBehaviour == MenuStartBehaviour.CLOSED)
                Close();
        }

        protected void LateUpdate() 
        {
            IsOpen = canvasGroup.interactable && canvasGroup.alpha == 1;  

            if(IsOpen && 
            FocusedPlayer == null && 
            autoFocusOnPlayerOne && 
            PlayerManager.Instance != null && 
            PlayerManager.Instance.LocalPlayers != null &&
            PlayerManager.Instance.LocalPlayers[0] != null)
                SetFocus(PlayerManager.Instance.LocalPlayers[0]);

            if(EventSystem.currentSelectedGameObject != null) {
                if(!ShouldSelect && EventSystem.currentSelectedGameObject.GetComponent<Selectable>() is Button)
                    EventSystem.SetSelectedGameObject(null);
            } else {
                if(ShouldSelect && firstSelect != null)
                    EventSystem.SetSelectedGameObject(firstSelect.gameObject);
            }

            if(!menuControllerLoadedProperly)
                Debug.LogError($"MenuController script \"{this}\" on \"{gameObject.name}\" wasn't loaded properly. Make sure you call base.Awake() if you're overriding it.");
        }

#region Open and Close
        public void Open(LocalPlayer focus = null) 
        {
            if(ParentMenu != null) {
                if(hidesParent)
                    ParentMenu.Close();
                else
                    ParentMenu.RemoveFocus();

                if(hidesSiblings) {
                    ParentMenu.SubMenus.ForEach((Action<MenuController>)(sm => {
                        if(sm.IsOpen)
                            sm.Close();
                    }));
                }
            }

            if(!gameObject.activeSelf) 
                gameObject.SetActive(true);

            canvasGroup.alpha = 1.0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if(focus != null)
                SetFocus(focus);
            else if(autoFocusOnPlayerOne && PlayerManager.Instance.LocalPlayers[0] != null)
                SetFocus(PlayerManager.Instance.LocalPlayers[0]);
            else
                RemoveFocus();

            if(EventSystem != null)
                EventSystem.SetSelectedGameObject(null);

            BLog.Log($"MenuController \"{this}\" opened with {(FocusedPlayer != null ? $"focus \"{FocusedPlayer.Input.playerIndex}\"" : "no focus")}", LogSettings, 0);
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
        
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if(EventSystem != null)
                EventSystem.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Callback for when this MenuController was closed, BEFORE we lose focus and it is disabled.
        /// </summary>
        protected virtual void Closed() {}
#endregion

    }

    [Serializable]
    public struct SubMenuData 
    {
        public string id;
        public MenuController menuController;   
    }

    public enum MenuStartBehaviour { CLOSED, OPEN, NONE }
}