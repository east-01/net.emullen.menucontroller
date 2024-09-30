using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EMullen.MenuController 
{
    /// <summary>
    /// A singleton class that holds the settings for all MenuController instances.
    /// </summary>
    public class MenuControllerSettings : MonoBehaviour 
    {

        public static MenuControllerSettings Instance { get; private set;}

        [SerializeField]
        private BLogChannel logSettings;

        [SerializeField]
        private InputAction cancelAction;
        public InputAction CancelAction => cancelAction;

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