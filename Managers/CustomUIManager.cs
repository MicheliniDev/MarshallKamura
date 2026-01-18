using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KamuraPrime
{
    public static class CustomUIManager
    {
        public static void AddPauseResetBossButton()
        {
            var resumeButton = GameObject.Find("PersistableModule/MainUI/Canvas/PauseHolder/PauseUI/Resume");
            var resetButton = CreateCustomButton(resumeButton, "Reset", "Reset");

            var rect = resumeButton.GetComponent<RectTransform>();
            var pos = rect.position;
            pos.y += 97f;
            rect.position = pos;

            var uiField = GameObject.Find("PersistableModule/MainUI/Canvas/PauseHolder/PauseUI").GetComponent<PauseUI>();

            var buttonsField = Traverse.Create(uiField).Field<List<OutGameButtonUI>>("selectButtons");
            buttonsField.Value.Insert(1, resetButton);
        }

        public static void AddBossChallengeButton()
        {
            var menuUI = GameObject.Find("MainMenu").GetComponent<MainMenuUI>();

            var layoutGroup = menuUI.transform.Find("Main/MainMenuPanel").GetComponent<GridLayoutGroup>();
            var size = layoutGroup.cellSize;
            size.y -= 20f;
            layoutGroup.cellSize = size;

            var bossChallengeButton = CreateCustomButton("MainMenu/Main/MainMenuPanel/StartGame", Constants.BOSS_CHALLENGE_TEXT, Constants.BOSS_KEY);

            var buttonsField = Traverse.Create(menuUI).Field<List<OutGameButtonUI>>("selectButtons");
            buttonsField.Value.Insert(1, bossChallengeButton);
        }

        private static OutGameButtonUI CreateCustomButton(string baseButtonPath, string buttonText, string key)
        {
            var referenceButton = GameObject.Find(baseButtonPath);
            return CreateCustomButton(referenceButton, buttonText, key);
        }

        private static OutGameButtonUI CreateCustomButton(GameObject referenceButton, string buttonText, string key)
        {
            OutGameButtonUI button = null;
            button = UnityEngine.Object.Instantiate(referenceButton, referenceButton.transform.parent).GetComponent<OutGameButtonUI>();
            button.transform.SetSiblingIndex(1);
            button.Key = key;

            button.GetComponent<LocalizedTextUI>().enabled = false;
            var buttonTextComp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            buttonTextComp.text = buttonText;

            return button;
        }
    }
}
