using System.Collections.Generic;
using UnityEngine;

namespace EMullen.MenuController.Tests 
{
    public class TestMenuController1 : MenuController 
    {
        [SerializeField]
        private float testField;
        [SerializeField]
        private List<string> testList;

        public void OpenSubMenu1() => GetSubMenu("SubMenu1").Open(FocusedPlayer);
        public void OpenSubMenu2() => GetSubMenu("SubMenu2").Open(FocusedPlayer);

    }
}