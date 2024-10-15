using EMullen.Core;
using EMullen.PlayerMgmt;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace EMullen.MenuController 
{
    /// <summary>
    /// A singleton class that holds the settings for all MenuController instances.
    /// </summary>
    public class MenuControllerSettings : MonoBehaviour 
    {

        public static MenuControllerSettings Instance { get; private set;}

        [Header("Global objects")]
        [SerializeField]
        private EventSystem eventSystem;
        public EventSystem EventSystem => eventSystem;
        [SerializeField]
        private InputSystemUIInputModule inputSystemUIInputModule;
        public InputSystemUIInputModule InputSystemUIInputModule => inputSystemUIInputModule;

        [Header("Settings")]
        [SerializeField]
        private InputActionReference cancelAction;
        public InputActionReference CancelAction => cancelAction;

        private bool hasCheckedPlayerConfiguration = false;

        private void Awake() 
        {
            if(Instance != null) {
                Destroy(gameObject);
                Debug.Log($"Destroyed newly spawned MenuControllerSettings since singleton Instance already exists.");
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update() 
        {
            if(!hasCheckedPlayerConfiguration && PlayerManager.Instance != null) {
                if(PlayerManager.Instance.PlayerInputManager.playerPrefab.GetComponent<PlayerInput>().notificationBehavior != PlayerNotifications.InvokeCSharpEvents)
                    Debug.LogWarning($"<color=#FF3333><b>SEVERE warning for PlayerInputManager on the PlayerManager:</b></color> playerPrefab's PlayerInput#notificationBehaviour is NOT \"Invoke C# Events\" this WILL cause problems for MenuControllers.");

                hasCheckedPlayerConfiguration = true;
            }
        }
    }
}