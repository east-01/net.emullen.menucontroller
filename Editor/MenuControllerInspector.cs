using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EMullen.Core;
using EMullen.PlayerMgmt;

namespace EMullen.MenuController.Editor 
{
    [CustomEditor(typeof(MenuController), true)]
    public class MenuControllerInspector : UnityEditor.Editor 
    {

        /* Editor properties */
        private Vector2 scrollPos;
        private int selectedTab;
        private string[] tabTitles => new string[] {"Settings", "Focused player", "Submenus", "UI Components"};
        private List<MemberInfo> childClassInfo;

        private SerializedProperty sp_hidesParent;
        private SerializedProperty sp_hidesSiblings;
        private SerializedProperty sp_autoOpen;

        private SerializedProperty sp_autoFocusOnPlayerOne;

        private SerializedProperty sp_subMenus;

        private SerializedProperty sp_inputSystemUIInputModule;
        private SerializedProperty sp_eventSystem;
        private SerializedProperty sp_firstSelect;
        private SerializedProperty sp_tooltips;

        private void OnEnable() 
        {
            sp_hidesParent = serializedObject.FindProperty("hidesParent");
            sp_hidesSiblings = serializedObject.FindProperty("hidesSiblings");
            sp_autoOpen = serializedObject.FindProperty("autoOpen");

            sp_autoFocusOnPlayerOne = serializedObject.FindProperty("autoFocusOnPlayerOne");

            sp_subMenus = serializedObject.FindProperty("subMenus");

            sp_inputSystemUIInputModule = serializedObject.FindProperty("inputSystemUIInputModule");
            sp_eventSystem = serializedObject.FindProperty("eventSystem");
            sp_firstSelect = serializedObject.FindProperty("firstSelect");
            sp_tooltips = serializedObject.FindProperty("tooltips");
        }

        public override void OnInspectorGUI() 
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabTitles);

            GUILayout.Space(5);

            switch(selectedTab) {
                case 0:
                    DrawSettings();
                    break;
                case 1:
                    DrawFocusedPlayer();
                    break;
                case 2:
                    DrawSubmenus();
                    break;
                case 3:
                    DrawUIComponents();
                    break;
            }

            if(target.GetType() != typeof(MenuController)) {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
                DrawChildFields();
            }

            serializedObject.ApplyModifiedProperties();
            
        }

        private void DrawSettings() 
        {
            EditorGUILayout.PropertyField(sp_hidesParent, new GUIContent("Hides parent"));
            EditorGUILayout.PropertyField(sp_hidesSiblings, new GUIContent("Hides siblings"));
            EditorGUILayout.PropertyField(sp_autoOpen, new GUIContent("Auto-open"));
        }

        private void DrawFocusedPlayer() 
        {
            EditorGUILayout.PropertyField(sp_autoFocusOnPlayerOne, new GUIContent("Auto focus on player one"));

            GUILayout.Space(5f);

            LocalPlayer focusedPlayer = (target as MenuController).FocusedPlayer;
            if(focusedPlayer == null) {
                GUILayout.Label("No focused player.");
                return;
            }

            GUILayout.Label($"Focused on Player #{focusedPlayer.Input.playerIndex}");
            GUILayout.Label($"  Using control scheme \"{focusedPlayer.Input.currentControlScheme}\"");
            GUILayout.Label($"  Using action map \"{focusedPlayer.Input.currentActionMap.name}\"");

        }

        private void DrawSubmenus() 
        {
            EditorGUILayout.PropertyField(sp_subMenus, new GUIContent("Submenus"), true);
        }

        private void DrawUIComponents() 
        {
            EditorGUILayout.PropertyField(sp_firstSelect, new GUIContent("First select"));

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
    
            EditorGUILayout.PropertyField(sp_inputSystemUIInputModule, new GUIContent("InputSystemUIInputModule"));
            string usedISUIM = (target as MenuController).usedISUIM;
            if(usedISUIM.Length > 0)
                GUILayout.Label($"Using: {usedISUIM}");
    
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(sp_eventSystem, new GUIContent("EventSystem"));
            CustomEditorUtils.CreateNote("Each of these can be used by the MenuController, if no values are provided we will recursively follow the parent MenuControllers until a value is found.");

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(sp_tooltips, new GUIContent("Tooltips"));
        }

        private void DrawChildFields() 
        {
            GUILayout.Space(5f);
            CustomEditorUtils.CreateBigHeader(target.GetType().Name);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if(childClassInfo == null || childClassInfo.Count == 0)
                PopulateChildClassInfo();

            foreach (var info in childClassInfo)
            {
                SerializedProperty property = serializedObject.FindProperty(info.Name);
                if(property == null)
                    continue;
                EditorGUILayout.PropertyField(property);
            }

            EditorGUILayout.EndScrollView();
        }

        private void PopulateChildClassInfo() 
        {
            IEnumerable<T> GetAll<T>(TypeInfo typeInfo, Func<TypeInfo, IEnumerable<T>> accessor) {
                while (typeInfo != null) {
                    foreach (var t in accessor(typeInfo)) {
                        yield return t;
                    }

                    typeInfo = typeInfo.BaseType?.GetTypeInfo();
                }
            }

            IEnumerable<MemberInfo> GetAllMembers(TypeInfo typeInfo) 
                => GetAll(typeInfo, ti => ti.DeclaredMembers);

            childClassInfo = new(GetAllMembers(target.GetType().GetTypeInfo()));
            childClassInfo.RemoveAll(memberInfo => GetAllMembers(typeof(MenuController).GetTypeInfo()).Contains(memberInfo));
        }
        
    }
}