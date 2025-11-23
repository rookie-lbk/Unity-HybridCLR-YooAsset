using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace vietlabs.fr2
{
    internal partial class FR2_WindowAll
    {
        private void DrawSettings()
        {
            if (bottomTabs == null || bottomTabs.current == -1) return;

            GUILayout.BeginVertical(FR2_Theme.Current.SettingsPanelHeight);
            {
                GUILayout.Space(2f);
                switch (bottomTabs.current)
                {
                case 0:
                    {
                        DrawMainSettings();
                        break;
                    }

                case 1:
                    {
                        DrawIgnoreSettings();
                        break;
                    }

                case 2:
                    {
                        DrawFilterSettings();
                        break;
                    }
                }
            }
            GUILayout.EndVertical();

            Rect rect = GUILayoutUtility.GetLastRect();
            rect.height = 1f;
            GUI2.Rect(rect, Color.black, 0.4f);
        }

        private void DrawMainSettings()
        {
            FR2_Setting.s.DrawSettings();

            // Add the Write Import Log toggle in the settings
            GUILayout.Space(5f);
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);

            bool writeLog = settings.writeImportLog;
            settings.writeImportLog = EditorGUILayout.Toggle("Write Import Log", settings.writeImportLog);
            if (writeLog != settings.writeImportLog)
            {
                EditorUtility.SetDirty(this);
            }
            
            // Add Git settings if applicable
            if (FR2_SettingExt.isGitProject)
            {
                DrawGitSettings();
            }
        }

        private void DrawGitSettings()
        {
            GUILayout.Space(5f);
            EditorGUILayout.LabelField("Git Settings", EditorStyles.boldLabel);
            
            if (FR2_SettingExt.gitIgnoreAdded)
            {
                EditorGUILayout.HelpBox("FR2_Cache.asset* is already in your .gitignore file.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Add FR2_Cache.asset* to .gitignore");
                if (GUILayout.Button("Apply", FR2_Theme.Current.ApplyButtonWidth))
                {
                    FR2_GitUtil.AddFR2CacheToGitIgnore();
                    FR2_SettingExt.gitIgnoreAdded = true;
                    FR2_SettingExt.hideGitIgnoreWarning = true;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawIgnoreSettings()
        {
            if (FR2_AssetGroupDrawer.DrawIgnoreFolder()) 
            {
                MarkDirty();
            }
        }

        private void DrawFilterSettings()
        {
            if (FR2_AssetGroupDrawer.DrawSearchFilter()) 
            {
                MarkDirty();
            }
        }
    }
} 