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

        public LocalPlayer FocusedPlayer { get; protected set; }
        /// <summary> 
        /// Get the focused player (if it exists) or a focusedPlayer in child MenuControllers. 
        /// </summary>
        public LocalPlayer FocusedPlayerIncludingChildren { get {
            if(FocusedPlayer != null)
                return FocusedPlayer;
            foreach(MenuController subMenu in SubMenus) {
                if(subMenu.FocusedPlayerIncludingChildren != null)
                    return subMenu.FocusedPlayerIncludingChildren;
            }
            return null;
        } }
        private string focusedPlayerInitialActionMap; // Stores the action map of the focused player so we can revert them to it once they become unfocused.

        /// <summary>
        /// Automatically focus on player one when they are available.
        /// Does not happen if there's already a focused player.
        /// </summary>
        [SerializeField]
        private bool autoFocusOnPlayerOne;

        public void SetFocus(LocalPlayer playerObj) 
        {
            /* Passing focus */
            if(FocusedPlayer != null) {
                if(FocusedPlayer.UID == playerObj.UID) {
                    BLog.Log($"{this}: Maintaining focus on {playerObj.Input.playerIndex}", LogSettings, 4);
                    return;
                } else {
                    BLog.Log($"{this}: Removing focus from {FocusedPlayer.Input.playerIndex} and placing it on {playerObj.Input.playerIndex}", LogSettings, 4);
                    RemoveFocus();
                }
            } else {
                BLog.Log($"{this}: No focus existing, placing focus on {playerObj.Input.playerIndex}", LogSettings, 4);
            }

            /* Assign focus */
            FocusedPlayer = playerObj;
            FocusedPlayer.Input.onActionTriggered += PlayerInput_ActionTriggered;
            FocusedPlayer.Input.onControlsChanged += PlayerInput_OnControlsChanged;

            focusedPlayerInitialActionMap = FocusedPlayer.Input.currentActionMap.name;

            // The key is that the player should never be assigned to a InputSystemUIInputModule when they're not in ui action map
            if(FocusedPlayer.Input.currentActionMap.name != "UI")
                FocusedPlayer.Input.SwitchCurrentActionMap("UI");
            
            FocusedPlayer.Input.uiInputModule = InputSystemUIInputModule;
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

            tooltips.ForEach(tt => tt.SetObservedInput(FocusedPlayer.Input));

            if(ShouldSelect && firstSelect != null) {
                if(EventSystem != null) {
                    EventSystem.SetSelectedGameObject(firstSelect.gameObject);
                } else
                    Debug.LogWarning($"MenuController \"{this}\" failed to find an EventSystem. This may be a misconfiguration, ensure that an EventSystem is assigned on this script or in a parent MenuController.");
            }
        }

        public void RemoveFocus() 
        {
            if(FocusedPlayer == null)
                return;

            if(FocusedPlayer.Input != null) {
                FocusedPlayer.Input.uiInputModule = null;
                if(FocusedPlayer.Input.enabled)
                    FocusedPlayer.Input.SwitchCurrentActionMap(focusedPlayerInitialActionMap);
            }

            // BLog.Highlight($"##### REMOVED FOCUS FOR PLAYER {focusedPlayer.PlayerIndex} #####");

            FocusedPlayer.Input.onActionTriggered -= PlayerInput_ActionTriggered;
            FocusedPlayer = null;
            focusedPlayerInitialActionMap = null;
        }
    }
}