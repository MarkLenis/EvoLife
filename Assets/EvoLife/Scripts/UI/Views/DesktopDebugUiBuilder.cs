using System;
using UnityEngine;
using UnityEngine.UI;
using EvoLife.Common;

namespace EvoLife.UI
{
    public sealed class DesktopDebugUiWidgets
    {
        public GameObject Root;
        public Text SimStatus;
        public Text Dashboard;
        public Text Inspector;
        public Text Charts;
        public Button Pause;
        public Button Resume;
        public Button ReloadScene;
        public Button Focus;
        public Button FreeCamera;
        public Button ToggleAiDebug;
    }

    /// <summary>
    /// Builds a Screen Space Overlay canvas at runtime so scenes do not need baked UI prefabs.
    /// </summary>
    public static class DesktopDebugUiBuilder
    {
        public static DesktopDebugUiWidgets Build(
            Transform owner,
            Action<float> onSpeed,
            Action<EnvironmentalEventKind> onEvent,
            Action onToggleAiDebug)
        {
            var root = new GameObject("EvoLifeDesktopCanvas");
            if (owner != null)
            {
                root.transform.SetParent(owner, false);
            }

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var widgets = new DesktopDebugUiWidgets { Root = root };

            widgets.SimStatus = CreateText(
                root.transform,
                "SimStatus",
                new Vector2(0.22f, 0.94f),
                new Vector2(0.78f, 1f),
                16,
                TextAnchor.MiddleCenter,
                font);

            var top = CreatePanel(root.transform, "TopBar", new Vector2(0f, 0.90f), new Vector2(1f, 0.94f), new Color(0f, 0f, 0f, 0.45f));
            widgets.Pause = CreateButton(top.transform, "Pause", "Pause", 0f, onClick: null, font);
            widgets.Resume = CreateButton(top.transform, "Resume", "Resume", 0.08f, null, font);
            CreateButton(top.transform, "1x", "1x", 0.18f, () => onSpeed?.Invoke(SimulationSpeedPresets.One), font);
            CreateButton(top.transform, "2x", "2x", 0.24f, () => onSpeed?.Invoke(SimulationSpeedPresets.Two), font);
            CreateButton(top.transform, "5x", "5x", 0.30f, () => onSpeed?.Invoke(SimulationSpeedPresets.Five), font);
            CreateButton(top.transform, "10x", "10x", 0.36f, () => onSpeed?.Invoke(SimulationSpeedPresets.Ten), font);
            widgets.Focus = CreateButton(top.transform, "Focus", "Focus (F)", 0.46f, null, font);
            widgets.FreeCamera = CreateButton(top.transform, "Free", "Free cam (C)", 0.56f, null, font);
            widgets.ToggleAiDebug = CreateButton(top.transform, "AiDebug", "AI debug (F3)", 0.68f, onToggleAiDebug, font);
            widgets.ReloadScene = CreateButton(top.transform, "Reload", "Reload scene", 0.82f, null, font);

            var dashPanel = CreatePanel(root.transform, "Dashboard", new Vector2(0f, 0.22f), new Vector2(0.34f, 0.90f), new Color(0f, 0f, 0f, 0.55f));
            widgets.Dashboard = CreateScrollingText(dashPanel.transform, "DashboardText", font, 13);

            var inspectorPanel = CreatePanel(root.transform, "Inspector", new Vector2(0.66f, 0.22f), new Vector2(1f, 0.90f), new Color(0f, 0f, 0f, 0.55f));
            widgets.Inspector = CreateScrollingText(inspectorPanel.transform, "InspectorText", font, 13);

            var chartPanel = CreatePanel(root.transform, "Charts", new Vector2(0.34f, 0.22f), new Vector2(0.66f, 0.50f), new Color(0.02f, 0.02f, 0.05f, 0.55f));
            widgets.Charts = CreateScrollingText(chartPanel.transform, "ChartText", font, 14);

            var eventPanel = CreatePanel(root.transform, "Events", new Vector2(0f, 0f), new Vector2(1f, 0.22f), new Color(0.05f, 0.02f, 0.02f, 0.55f));
            var kinds = EventPanelPresenter.TriggerableKinds;
            for (var i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                var anchor = i / (float)kinds.Length;
                CreateButton(
                    eventPanel.transform,
                    kind.ToString(),
                    EventPanelPresenter.FormatKind(kind),
                    anchor,
                    () => onEvent?.Invoke(kind),
                    font,
                    width: 1f / kinds.Length);
            }

            return widgets;
        }

        static RectTransform CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, min, max);
            var image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        static Text CreateText(
            Transform parent,
            string name,
            Vector2 min,
            Vector2 max,
            int fontSize,
            TextAnchor anchor,
            Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, min, max);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Text CreateScrollingText(Transform parent, string name, Font font, int fontSize)
        {
            var text = CreateText(parent, name, Vector2.zero, Vector2.one, fontSize, TextAnchor.UpperLeft, font);
            var rect = text.rectTransform;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);
            return text;
        }

        static Button CreateButton(
            Transform parent,
            string name,
            string label,
            float anchorMinX,
            UnityEngine.Events.UnityAction onClick,
            Font font,
            float width = 0.08f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, 0.1f);
            rect.anchorMax = new Vector2(anchorMinX + width, 0.9f);
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.28f, 0.9f);
            var button = go.AddComponent<Button>();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            Stretch(textRect, Vector2.zero, Vector2.one);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            return button;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
