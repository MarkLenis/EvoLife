using UnityEngine;
using EvoLife.Common;

namespace EvoLife.UI
{
    /// <summary>
    /// Runtime overlay for the selected creature's AI debug contract.
    /// Reuses a small LineRenderer pool. Disabled by default.
    /// </summary>
    public sealed class CreatureAiDebugVisualizer : MonoBehaviour
    {
        const int LineCount = 9;

        [SerializeField] bool drawSenseRange = true;
        [SerializeField] bool drawInteractionRange = true;
        [SerializeField] Color senseColor = new Color(0.3f, 0.8f, 1f, 0.7f);
        [SerializeField] Color foodColor = new Color(0.2f, 0.9f, 0.3f, 1f);
        [SerializeField] Color waterColor = new Color(0.2f, 0.5f, 1f, 1f);
        [SerializeField] Color herbivoreColor = new Color(0.95f, 0.85f, 0.2f, 1f);
        [SerializeField] Color predatorColor = new Color(0.95f, 0.25f, 0.2f, 1f);
        [SerializeField] Color headingColor = Color.white;
        [SerializeField] Color intentColor = new Color(1f, 0.5f, 0.1f, 1f);
        [SerializeField] Color heuristicColor = new Color(0.8f, 0.3f, 1f, 1f);

        LineRenderer[] lines;
        Transform selected;
        IReadOnlyCreatureAiDebug debug;
        Material lineMaterial;

        public void SetSelected(Transform host, IReadOnlyCreatureAiDebug aiDebug)
        {
            selected = host;
            debug = aiDebug;
            if (!AiDebugVisualizationSettings.ShouldDraw(host != null))
            {
                HideAll();
            }
        }

        void Awake()
        {
            EnsurePool();
            HideAll();
        }

        void LateUpdate()
        {
            if (!AiDebugVisualizationSettings.ShouldDraw(selected != null) || selected == null || debug == null)
            {
                HideAll();
                return;
            }

            EnsurePool();
            var origin = selected.position + Vector3.up * 0.15f;
            var index = 0;
            if (drawSenseRange)
            {
                DrawCircle(lines[index++], origin, debug.SensoryRange, senseColor);
            }
            else
            {
                Disable(lines[index++]);
            }

            if (drawInteractionRange)
            {
                DrawCircle(lines[index++], origin, debug.InteractionRange, new Color(1f, 1f, 1f, 0.35f));
            }
            else
            {
                Disable(lines[index++]);
            }

            DrawTarget(lines[index++], origin, debug.NearestFood, debug.SensoryRange, foodColor);
            DrawTarget(lines[index++], origin, debug.NearestWater, debug.SensoryRange, waterColor);
            DrawTarget(lines[index++], origin, debug.NearestHerbivore, debug.SensoryRange, herbivoreColor);
            DrawTarget(lines[index++], origin, debug.NearestPredator, debug.SensoryRange, predatorColor);

            var heading = new Vector3(debug.HeadingX, 0f, debug.HeadingZ);
            if (heading.sqrMagnitude < 0.0001f)
            {
                heading = selected.forward;
            }

            heading.y = 0f;
            heading.Normalize();
            DrawRay(lines[index++], origin, heading * Mathf.Max(1.5f, debug.SensoryRange * 0.25f), headingColor);

            var intent = selected.rotation * new Vector3(debug.Turn, 0f, debug.Forward);
            if (intent.sqrMagnitude < 0.0001f)
            {
                Disable(lines[index++]);
            }
            else
            {
                intent.y = 0f;
                DrawRay(lines[index++], origin, intent.normalized * (2f + debug.SprintOrEffort * 2f), intentColor);
            }

            if (debug.HasHeuristicTarget)
            {
                DrawTarget(GetOrIgnore(index), origin, debug.HeuristicTarget, debug.SensoryRange, heuristicColor);
            }
            else if (index < lines.Length)
            {
                Disable(lines[index]);
            }

            Debug.DrawLine(origin, origin + heading * 2f, headingColor);
        }

        LineRenderer GetOrIgnore(int index)
        {
            if (index >= 0 && index < lines.Length)
            {
                return lines[index];
            }

            return lines[lines.Length - 1];
        }

        void EnsurePool()
        {
            if (lines != null)
            {
                return;
            }

            lineMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            lines = new LineRenderer[LineCount];
            for (var i = 0; i < LineCount; i++)
            {
                var go = new GameObject("AiDebugLine_" + i);
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.material = lineMaterial;
                line.useWorldSpace = true;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.loop = false;
                line.positionCount = 0;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;
                lines[i] = line;
            }
        }

        void DrawTarget(LineRenderer line, Vector3 origin, SensedTargetDebug target, float senseRange, Color color)
        {
            if (line == null)
            {
                return;
            }

            if (!target.Present)
            {
                Disable(line);
                return;
            }

            var local = new Vector3(target.LocalDirX, 0f, target.LocalDirZ);
            if (local.sqrMagnitude < 0.0001f)
            {
                DrawRay(line, origin, Vector3.up * 0.4f, color);
                return;
            }

            local.Normalize();
            var world = selected.TransformDirection(local);
            world.y = 0f;
            world.Normalize();
            var distance = Mathf.Max(0.15f, target.NormalizedDistance * Mathf.Max(0.01f, senseRange));
            DrawRay(line, origin, world * distance, color);
            Debug.DrawLine(origin, origin + world * distance, color);
        }

        void DrawRay(LineRenderer line, Vector3 origin, Vector3 delta, Color color)
        {
            line.enabled = true;
            line.loop = false;
            line.positionCount = 2;
            line.startColor = color;
            line.endColor = color;
            line.SetPosition(0, origin);
            line.SetPosition(1, origin + delta);
        }

        void DrawCircle(LineRenderer line, Vector3 origin, float radius, Color color)
        {
            if (radius <= 0.01f)
            {
                Disable(line);
                return;
            }

            const int segments = 32;
            line.enabled = true;
            line.loop = true;
            line.positionCount = segments;
            line.startColor = color;
            line.endColor = color;
            var step = Mathf.PI * 2f / segments;
            for (var i = 0; i < segments; i++)
            {
                var angle = step * i;
                line.SetPosition(i, origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }

        void HideAll()
        {
            if (lines == null)
            {
                return;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                Disable(lines[i]);
            }
        }

        static void Disable(LineRenderer line)
        {
            if (line != null)
            {
                line.enabled = false;
                line.positionCount = 0;
            }
        }
    }
}
