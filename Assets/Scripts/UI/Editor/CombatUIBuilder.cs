#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DungeonCrawler.Combat;
using DungeonCrawler.Combat.QTE;
using DungeonCrawler.Data;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DungeonCrawler.UI.Editor
{
    public static class CombatUIBuilder
    {
        [MenuItem("DungeonCrawler/Build Combat UI")]
        public static void BuildUI()
        {
            // 1. Ensure EventSystem exists
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                eventSystem = esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGO.AddComponent<InputSystemUIInputModule>();
#else
                esGO.AddComponent<StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }

            // 2. Find or create CombatCanvas
            GameObject canvasGO = GameObject.Find("CombatCanvas");
            if (canvasGO != null)
            {
                Undo.DestroyObjectImmediate(canvasGO);
            }

            canvasGO = new GameObject("CombatCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            CombatUI combatUI = canvasGO.AddComponent<CombatUI>();
            HUDManager hudManager = canvasGO.AddComponent<HUDManager>();

            // ================= EXPLORATION HUD ================= //
            GameObject explorationPanel = CreateUIObject("ExplorationPanel", canvasGO.transform);
            SetFullStretch(explorationPanel.GetComponent<RectTransform>());

            // Objective Box (Top-Left)
            GameObject objectiveBox = CreateUIObject("ObjectiveBox", explorationPanel.transform);
            RectTransform objBoxRect = objectiveBox.GetComponent<RectTransform>();
            SetAnchors(objBoxRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(520, 100));
            Image objBoxBg = objectiveBox.AddComponent<Image>();
            objBoxBg.color = new Color(0.05f, 0.05f, 0.08f, 0.65f);

            TMP_Text objectiveText = CreateText("ObjectiveText", objectiveBox.transform, "Objektif: Cari Kunci Dungeon di Ruang Tersegel.", 20, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            RectTransform objTextRect = objectiveText.GetComponent<RectTransform>();
            SetAnchors(objTextRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(15, -15), new Vector2(-30, 40));
            objectiveText.color = new Color(1f, 0.9f, 0.2f);

            TMP_Text keyStatusText = CreateText("KeyStatusText", objectiveBox.transform, "Kunci Dungeon: [BELUM DITEMUKAN]", 17, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            RectTransform keyTextRect = keyStatusText.GetComponent<RectTransform>();
            SetAnchors(keyTextRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(15, 15), new Vector2(-30, 30));

            // Interaction Prompt (Center-Bottom)
            TMP_Text promptText = CreateText("InteractionPromptText", explorationPanel.transform, "[E] Buka Pintu", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform promptRect = promptText.GetComponent<RectTransform>();
            SetAnchors(promptRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(700, 50));
            promptText.color = new Color(0.2f, 0.95f, 1f);
            promptText.gameObject.SetActive(false);

            // Notification Text (Center banner)
            TMP_Text notificationText = CreateText("NotificationText", explorationPanel.transform, "", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform notifRect = notificationText.GetComponent<RectTransform>();
            SetAnchors(notifRect, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 60));
            notificationText.color = Color.white;
            notificationText.gameObject.SetActive(false);

            // ================= MINIMAP AUTO-MAP WIDGET (Top-Right) ================= //
            GameObject minimapGO = CreateUIObject("MinimapWidget", explorationPanel.transform);
            RectTransform minimapRect = minimapGO.GetComponent<RectTransform>();
            SetAnchors(minimapRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-25, -25), new Vector2(160, 184));
            minimapGO.AddComponent<DungeonCrawler.Map.MinimapUI>();

            // ================= GAME OVER PANEL ================= //
            GameObject gameOverPanel = CreateUIObject("GameOverPanel", canvasGO.transform);
            SetFullStretch(gameOverPanel.GetComponent<RectTransform>());
            Image goBg = gameOverPanel.AddComponent<Image>();
            goBg.color = new Color(0.1f, 0.02f, 0.02f, 0.92f);

            TMP_Text goTitle = CreateText("GameOverTitle", gameOverPanel.transform, "KAMU GUGUR", 64, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform goTitleRect = goTitle.GetComponent<RectTransform>();
            SetAnchors(goTitleRect, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 100));
            goTitle.color = new Color(0.9f, 0.15f, 0.15f);

            TMP_Text goSub = CreateText("GameOverSubtitle", gameOverPanel.transform, "Jiwa pengelana tertelan oleh kegelapan labirin.", 22, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform goSubRect = goSub.GetComponent<RectTransform>();
            SetAnchors(goSubRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 50));
            goSub.color = new Color(0.8f, 0.8f, 0.8f);

            Button restartGoBtn = CreateButton("RestartGameOverButton", gameOverPanel.transform, "COBA LAGI", new Color(0.75f, 0.2f, 0.2f), new Vector2(0, -80), new Vector2(240, 65));
            RectTransform rgoRect = restartGoBtn.GetComponent<RectTransform>();
            SetAnchors(rgoRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(240, 65));
            gameOverPanel.SetActive(false);

            // ================= VICTORY PANEL ================= //
            GameObject victoryPanel = CreateUIObject("VictoryPanel", canvasGO.transform);
            SetFullStretch(victoryPanel.GetComponent<RectTransform>());
            Image vicBg = victoryPanel.AddComponent<Image>();
            vicBg.color = new Color(0.04f, 0.08f, 0.04f, 0.94f);

            TMP_Text vicTitle = CreateText("VictoryTitle", victoryPanel.transform, "DUNGEON SELESAI!", 68, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform vicTitleRect = vicTitle.GetComponent<RectTransform>();
            SetAnchors(vicTitleRect, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 110));
            vicTitle.color = new Color(1f, 0.85f, 0.15f);

            TMP_Text vicSub = CreateText("VictorySubtitle", victoryPanel.transform, "Abyssal Executioner telah ditaklukkan. Labirin kembali tenang.", 24, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform vicSubRect = vicSub.GetComponent<RectTransform>();
            SetAnchors(vicSubRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 50));
            vicSub.color = new Color(0.9f, 0.95f, 0.9f);

            Button restartVicBtn = CreateButton("RestartVictoryButton", victoryPanel.transform, "MAIN LAGI", new Color(0.2f, 0.75f, 0.35f), new Vector2(0, -90), new Vector2(240, 65));
            RectTransform rvicRect = restartVicBtn.GetComponent<RectTransform>();
            SetAnchors(rvicRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(240, 65));
            victoryPanel.SetActive(false);

            // ================= COMBAT PANEL ================= //
            GameObject combatPanel = CreateUIObject("CombatPanel", canvasGO.transform);
            SetFullStretch(combatPanel.GetComponent<RectTransform>());
            Image panelBg = combatPanel.AddComponent<Image>();
            panelBg.color = new Color(0.02f, 0.02f, 0.04f, 0.4f);

            // Enemy HP Bar (Top Center)
            GameObject enemyStatusGroup = CreateUIObject("EnemyStatusGroup", combatPanel.transform);
            RectTransform enemyGroupRect = enemyStatusGroup.GetComponent<RectTransform>();
            SetAnchors(enemyGroupRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(500, 80));

            TMP_Text enemyName = CreateText("EnemyNameText", enemyStatusGroup.transform, "SKELETON", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform enemyNameRect = enemyName.GetComponent<RectTransform>();
            SetAnchors(enemyNameRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(0, 32));

            Slider enemySlider = CreateSlider("EnemyHPSlider", enemyStatusGroup.transform, new Color(0.85f, 0.15f, 0.15f), new Vector2(0, -38), new Vector2(480, 24));
            TMP_Text enemyHPText = CreateText("EnemyHPText", enemySlider.transform, "50 / 50", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFullStretch(enemyHPText.GetComponent<RectTransform>());

            // Player HP Bar (Bottom Left)
            GameObject playerStatusGroup = CreateUIObject("PlayerStatusGroup", combatPanel.transform);
            RectTransform playerGroupRect = playerStatusGroup.GetComponent<RectTransform>();
            SetAnchors(playerGroupRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50, 50), new Vector2(360, 70));

            TMP_Text playerLabel = CreateText("PlayerLabel", playerStatusGroup.transform, "PLAYER STATUS", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform playerLabelRect = playerLabel.GetComponent<RectTransform>();
            SetAnchors(playerLabelRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0, 0), new Vector2(0, 28));

            Slider playerSlider = CreateSlider("PlayerHPSlider", playerStatusGroup.transform, new Color(0.15f, 0.85f, 0.35f), new Vector2(180, -35), new Vector2(360, 24));
            TMP_Text playerHPText = CreateText("PlayerHPText", playerSlider.transform, "HP: 100 / 100", 17, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFullStretch(playerHPText.GetComponent<RectTransform>());

            // Turn Status & Combat Log Texts
            TMP_Text turnStatusText = CreateText("TurnStatusText", combatPanel.transform, "GILIRANMU!", 34, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform turnStatusRect = turnStatusText.GetComponent<RectTransform>();
            SetAnchors(turnStatusRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(800, 50));
            turnStatusText.color = new Color(1f, 0.9f, 0.2f);

            TMP_Text combatLogText = CreateText("CombatLogText", combatPanel.transform, "Pertarungan Dimulai!", 22, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform combatLogRect = combatLogText.GetComponent<RectTransform>();
            SetAnchors(combatLogRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -200), new Vector2(900, 45));

            // Action Menu (Bottom Center)
            GameObject actionMenu = CreateUIObject("ActionMenuPanel", combatPanel.transform);
            RectTransform actionMenuRect = actionMenu.GetComponent<RectTransform>();
            SetAnchors(actionMenuRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(420, 90));

            Button attackBtn = CreateButton("AttackButton", actionMenu.transform, "ATTACK (Fisik)", new Color(0.85f, 0.2f, 0.2f), new Vector2(-105, 0), new Vector2(190, 60));
            Button castBtn = CreateButton("CastButton", actionMenu.transform, "CAST (Sihir)", new Color(0.2f, 0.5f, 0.9f), new Vector2(105, 0), new Vector2(190, 60));

            // Timing Bar Widget (Center)
            GameObject timingWidget = CreateUIObject("TimingBarWidget", combatPanel.transform);
            RectTransform timingWidgetRect = timingWidget.GetComponent<RectTransform>();
            SetAnchors(timingWidgetRect, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 150));
            TimingBarQTE timingBar = timingWidget.AddComponent<TimingBarQTE>();

            TMP_Text timingPrompt = CreateText("TimingPromptText", timingWidget.transform, "Tekan [SPASI] di Sweet Spot!", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform timingPromptRect = timingPrompt.GetComponent<RectTransform>();
            SetAnchors(timingPromptRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(550, 40));

            GameObject barBgGO = CreateUIObject("BarBackground", timingWidget.transform);
            RectTransform barBgRect = barBgGO.GetComponent<RectTransform>();
            SetAnchors(barBgRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 36));
            Image barBgImage = barBgGO.AddComponent<Image>();
            barBgImage.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

            // Good Zone
            GameObject goodZoneGO = CreateUIObject("GoodZone", barBgGO.transform);
            RectTransform goodZoneRect = goodZoneGO.GetComponent<RectTransform>();
            SetAnchors(goodZoneRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 36));
            Image goodZoneImage = goodZoneGO.AddComponent<Image>();
            goodZoneImage.color = new Color(1f, 0.85f, 0.2f, 0.45f);

            // Perfect Zone
            GameObject perfectZoneGO = CreateUIObject("PerfectZone", barBgGO.transform);
            RectTransform perfectZoneRect = perfectZoneGO.GetComponent<RectTransform>();
            SetAnchors(perfectZoneRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 36));
            Image perfectZoneImage = perfectZoneGO.AddComponent<Image>();
            perfectZoneImage.color = new Color(0.1f, 0.95f, 0.35f, 0.85f);

            // Cursor
            GameObject cursorGO = CreateUIObject("Cursor", barBgGO.transform);
            RectTransform cursorRect = cursorGO.GetComponent<RectTransform>();
            SetAnchors(cursorRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 48));
            Image cursorImage = cursorGO.AddComponent<Image>();
            cursorImage.color = Color.white;

            TMP_Text timingResult = CreateText("TimingResultText", timingWidget.transform, "", 32, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform timingResultRect = timingResult.GetComponent<RectTransform>();
            SetAnchors(timingResultRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 10), new Vector2(500, 45));

            // Wire up TimingBarQTE serialized fields
            SerializedObject timingSO = new SerializedObject(timingBar);
            timingSO.FindProperty("widgetRoot").objectReferenceValue = timingWidget;
            timingSO.FindProperty("barBackground").objectReferenceValue = barBgRect;
            timingSO.FindProperty("cursor").objectReferenceValue = cursorRect;
            timingSO.FindProperty("goodZone").objectReferenceValue = goodZoneRect;
            timingSO.FindProperty("perfectZone").objectReferenceValue = perfectZoneRect;
            timingSO.FindProperty("promptText").objectReferenceValue = timingPrompt;
            timingSO.FindProperty("resultText").objectReferenceValue = timingResult;
            timingSO.ApplyModifiedProperties();
            timingWidget.SetActive(false);

            // Typing Widget (Center)
            GameObject typingWidget = CreateUIObject("TypingWidget", combatPanel.transform);
            RectTransform typingWidgetRect = typingWidget.GetComponent<RectTransform>();
            SetAnchors(typingWidgetRect, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650, 160));
            TypingQTE typingQTE = typingWidget.AddComponent<TypingQTE>();

            TMP_Text typingPrompt = CreateText("TypingPromptText", typingWidget.transform, "Ketik Mantra Sihir:", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform typingPromptRect = typingPrompt.GetComponent<RectTransform>();
            SetAnchors(typingPromptRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(500, 35));

            TMP_Text spellWordText = CreateText("SpellWordText", typingWidget.transform, "I G N I S", 42, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform spellWordRect = spellWordText.GetComponent<RectTransform>();
            SetAnchors(spellWordRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(550, 60));

            Slider timerSlider = CreateSlider("TimerSlider", typingWidget.transform, new Color(0.2f, 0.8f, 1f), new Vector2(0, -42), new Vector2(400, 16));

            TMP_Text typingResult = CreateText("TypingResultText", typingWidget.transform, "", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform typingResultRect = typingResult.GetComponent<RectTransform>();
            SetAnchors(typingResultRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(500, 40));

            // Wire up TypingQTE serialized fields
            SerializedObject typingSO = new SerializedObject(typingQTE);
            typingSO.FindProperty("widgetRoot").objectReferenceValue = typingWidget;
            typingSO.FindProperty("spellWordText").objectReferenceValue = spellWordText;
            typingSO.FindProperty("promptText").objectReferenceValue = typingPrompt;
            typingSO.FindProperty("timerBar").objectReferenceValue = timerSlider;
            typingSO.FindProperty("resultText").objectReferenceValue = typingResult;
            typingSO.ApplyModifiedProperties();
            typingWidget.SetActive(false);

            // Wire up CombatUI serialized fields
            SerializedObject uiSO = new SerializedObject(combatUI);
            uiSO.FindProperty("combatPanel").objectReferenceValue = combatPanel;
            uiSO.FindProperty("actionMenuPanel").objectReferenceValue = actionMenu;
            uiSO.FindProperty("attackButton").objectReferenceValue = attackBtn;
            uiSO.FindProperty("castButton").objectReferenceValue = castBtn;
            uiSO.FindProperty("turnStatusText").objectReferenceValue = turnStatusText;
            uiSO.FindProperty("combatLogText").objectReferenceValue = combatLogText;
            uiSO.FindProperty("playerHealthSlider").objectReferenceValue = playerSlider;
            uiSO.FindProperty("playerHealthText").objectReferenceValue = playerHPText;
            uiSO.FindProperty("enemyHealthSlider").objectReferenceValue = enemySlider;
            uiSO.FindProperty("enemyHealthText").objectReferenceValue = enemyHPText;
            uiSO.FindProperty("enemyNameText").objectReferenceValue = enemyName;
            uiSO.FindProperty("timingBarQTE").objectReferenceValue = timingBar;
            uiSO.FindProperty("typingQTE").objectReferenceValue = typingQTE;
            uiSO.ApplyModifiedProperties();

            combatPanel.SetActive(false);

            // Wire up HUDManager serialized fields
            SerializedObject hudSO = new SerializedObject(hudManager);
            hudSO.FindProperty("explorationPanel").objectReferenceValue = explorationPanel;
            hudSO.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            hudSO.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            hudSO.FindProperty("keyStatusText").objectReferenceValue = keyStatusText;
            hudSO.FindProperty("notificationText").objectReferenceValue = notificationText;
            hudSO.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            hudSO.FindProperty("restartGameOverButton").objectReferenceValue = restartGoBtn;
            hudSO.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            hudSO.FindProperty("restartVictoryButton").objectReferenceValue = restartVicBtn;
            hudSO.ApplyModifiedProperties();

            // Wire CombatManager references on _Managers
            GameObject managersGO = GameObject.Find("_Managers");
            if (managersGO != null)
            {
                CombatManager cm = managersGO.GetComponent<CombatManager>();
                if (cm != null)
                {
                    SerializedObject cmSO = new SerializedObject(cm);
                    cmSO.FindProperty("combatUI").objectReferenceValue = combatUI;

                    GameObject playerGO = GameObject.Find("Player");
                    if (playerGO != null)
                    {
                        PlayerStats ps = playerGO.GetComponent<PlayerStats>();
                        if (ps != null)
                        {
                            cmSO.FindProperty("playerStats").objectReferenceValue = ps;
                        }
                    }

                    cmSO.ApplyModifiedProperties();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("[CombatUIBuilder] Built Combat & Exploration HUD with Victory/GameOver panels successfully!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static TMP_Text CreateText(string name, Transform parent, string initialText, float fontSize, FontStyles style, TextAlignmentOptions align)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = initialText;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color, Vector2 pos, Vector2 size)
        {
            GameObject btnGO = CreateUIObject(name, parent);
            RectTransform rt = btnGO.GetComponent<RectTransform>();
            SetAnchors(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

            Image img = btnGO.AddComponent<Image>();
            img.color = color;

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = color * 1.25f;
            cb.pressedColor = color * 0.8f;
            btn.colors = cb;

            TMP_Text txt = CreateText("Label", btnGO.transform, label, 18, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFullStretch(txt.GetComponent<RectTransform>());

            return btn;
        }

        private static Slider CreateSlider(string name, Transform parent, Color fillColor, Vector2 pos, Vector2 size)
        {
            GameObject sliderGO = CreateUIObject(name, parent);
            RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
            SetAnchors(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Background
            GameObject bgGO = CreateUIObject("Background", sliderGO.transform);
            SetFullStretch(bgGO.GetComponent<RectTransform>());
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

            // Fill Area
            GameObject fillAreaGO = CreateUIObject("Fill Area", sliderGO.transform);
            RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            SetFullStretch(fillAreaRect);

            // Fill
            GameObject fillGO = CreateUIObject("Fill", fillAreaGO.transform);
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            SetFullStretch(fillRect);
            Image fillImg = fillGO.AddComponent<Image>();
            fillImg.color = fillColor;

            slider.fillRect = fillRect;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;

            return slider;
        }
    }
}
#endif
