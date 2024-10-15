using EMullen.Bootstrapper;
using UnityEngine;

namespace EMullen.MenuController.Tests 
{
    public class TestSuiteBootstrapper : MonoBehaviour, IBootstrapComponent
    {
        public bool IsLoadingComplete()
        {
            return MenuControllerSettings.Instance != null;
        }
    }
}