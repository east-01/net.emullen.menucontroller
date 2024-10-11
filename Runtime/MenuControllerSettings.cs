using EMullen.Core;
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
    }
}