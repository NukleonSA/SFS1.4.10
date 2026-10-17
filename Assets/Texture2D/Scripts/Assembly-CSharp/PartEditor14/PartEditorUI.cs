using System.Collections.Generic;
using System.Globalization;
using NewBuildSystem;
using UnityEngine;

namespace PartEditor14
{
    public class PartEditorUI : MonoBehaviour
    {
        private delegate void ButtonAction();
        private delegate void StringAction(string s);

        private const float BallSize = 150f;
        private const float PanelW = 620f;
        private const float PanelH = 1080f;
        private const float TitleH = 70f;
        private const string BallXKey = "PartEditor14.BallX";
        private const string BallYKey = "PartEditor14.BallY";
        private const string PanelXKey = "PartEditor14.PanelX";
        private const string PanelYKey = "PartEditor14.PanelY";
        private const string PosStepKey = "PartEditor14.PosStep";
        private const string AngleStepKey = "PartEditor14.AngleStep";
        private const string ScaleStepKey = "PartEditor14.ScaleStep";

        private static readonly float[] PosSteps = { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.5f, 1f };
        private static readonly float[] AngleSteps = { 1f, 5f, 15f, 30f, 45f, 90f };
        private static readonly float[] ScaleSteps = { 0.05f, 0.1f, 0.25f, 0.5f, 1f };

        private enum EditField
        {
            None,
            PosX,
            PosY,
            AngleZ,
            PosStep,
            AngleStep,
            ScaleStep,
            ScaleX,
            ScaleY
        }

        public static PartEditorUI Current { get; private set; }

        private Build _build;
        private PlacedPart _target;
        private Rect _ballRect = new Rect(10f, 10f, BallSize, BallSize);
        private Rect _panelRect = new Rect(0f, 0f, PanelW, PanelH);
        private bool _panelOpen;
        private bool _draggingPanel;
        private bool _ballPressed;
        private bool _ballDragged;
        private Vector2 _ballDownPos;

        private float _posStep = 0.1f;
        private float _angleStep = 15f;
        private float _scaleStep = 0.1f;
        private EditField _editingField;
        private string _editBuffer = "";
        private readonly HashSet<PlacedPart> _knownSelected = new HashSet<PlacedPart>();

        private GUIStyle _ballStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _smallButtonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _textStyle;

        public void Init(Build build)
        {
            Current = this;
            _build = build;
            _ballRect.width = BallSize;
            _ballRect.height = BallSize;
            _ballRect.x = PlayerPrefs.GetFloat(BallXKey, Screen.width - BallSize - 20f);
            _ballRect.y = PlayerPrefs.GetFloat(BallYKey, Screen.height - BallSize - 40f);
            ClampBallToScreen();

            _panelRect.width = PanelW;
            _panelRect.height = PanelH;
            _panelRect.x = PlayerPrefs.GetFloat(PanelXKey, Screen.width - PanelW - 20f);
            _panelRect.y = PlayerPrefs.GetFloat(PanelYKey, 120f);
            ClampPanelToScreen();

            _posStep = PlayerPrefs.GetFloat(PosStepKey, 0.1f);
            if (_posStep <= 0f) _posStep = 0.1f;
            _angleStep = PlayerPrefs.GetFloat(AngleStepKey, 15f);
            if (_angleStep <= 0f) _angleStep = 15f;
            _scaleStep = PlayerPrefs.GetFloat(ScaleStepKey, 0.1f);
            if (_scaleStep <= 0f) _scaleStep = 0.1f;
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        public bool IsOverUI(Vector2 touchPosPixel)
        {
            // Touch.position uses bottom-left origin, while OnGUI rects use top-left.
            Vector2 pos = new Vector2(touchPosPixel.x, Screen.height - touchPosPixel.y);
            if (_ballRect.Contains(pos))
                return true;
            return _panelOpen && _panelRect.Contains(pos);
        }

        private void Update()
        {
            if (_build == null || _build.buildGrid == null)
                return;
            List<PlacedPart> selected = _build.buildGrid.GetSelectedParts();
            if (selected == null || selected.Count == 0)
            {
                _target = null;
                _knownSelected.Clear();
                return;
            }

            // 1.4 会保留旧选中标记：只认新出现的 PlacedPart，强制单选。
            PlacedPart newTarget = null;
            foreach (PlacedPart p in selected)
            {
                if (!_knownSelected.Contains(p))
                {
                    newTarget = p;
                    break;
                }
            }

            if (newTarget == null)
                newTarget = selected.Contains(_target) ? _target : selected[0];

            if (selected.Count > 1)
                DeselectAllExcept(newTarget);

            _target = newTarget;

            _knownSelected.Clear();
            if (_build.buildGrid.parts != null)
            {
                foreach (PlacedPart p in _build.buildGrid.parts)
                {
                    if (p != null && p.selected)
                        _knownSelected.Add(p);
                }
            }
        }

        private void DeselectAllExcept(PlacedPart keep)
        {
            if (_build == null || _build.buildGrid == null || _build.buildGrid.parts == null)
                return;
            foreach (PlacedPart p in _build.buildGrid.parts)
            {
                if (p != null && p != keep && p.selected)
                    p.selected = false;
            }
        }

        private void OnGUI()
        {
            if (_build == null || _build.buildGrid == null)
                return;
            EnsureStyles();
            DrawBall();
            if (_panelOpen)
                DrawPanel();
        }

        private void EnsureStyles()
        {
            if (_ballStyle != null)
                return;

            _ballStyle = new GUIStyle(GUI.skin.button);
            _ballStyle.fontSize = 64;
            _ballStyle.alignment = TextAnchor.MiddleCenter;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.fontSize = 40;
            _panelStyle.alignment = TextAnchor.UpperCenter;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 34;
            _buttonStyle.alignment = TextAnchor.MiddleCenter;

            _smallButtonStyle = new GUIStyle(GUI.skin.button);
            _smallButtonStyle.fontSize = 32;
            _smallButtonStyle.alignment = TextAnchor.MiddleCenter;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 30;
            _labelStyle.alignment = TextAnchor.MiddleLeft;

            _sectionStyle = new GUIStyle(GUI.skin.label);
            _sectionStyle.fontSize = 30;
            _sectionStyle.alignment = TextAnchor.MiddleCenter;
            _sectionStyle.normal.textColor = Color.cyan;

            _textStyle = new GUIStyle(GUI.skin.textField);
            _textStyle.fontSize = 32;
            _textStyle.alignment = TextAnchor.MiddleCenter;
        }

        private void ClampBallToScreen()
        {
            float maxX = Mathf.Max(0f, Screen.width - _ballRect.width);
            float maxY = Mathf.Max(0f, Screen.height - _ballRect.height);
            _ballRect.x = Mathf.Clamp(_ballRect.x, 0f, maxX);
            _ballRect.y = Mathf.Clamp(_ballRect.y, 0f, maxY);
        }

        private void ClampPanelToScreen()
        {
            float maxX = Mathf.Max(0f, Screen.width - _panelRect.width);
            float maxY = Mathf.Max(0f, Screen.height - _panelRect.height);
            _panelRect.x = Mathf.Clamp(_panelRect.x, 0f, maxX);
            _panelRect.y = Mathf.Clamp(_panelRect.y, 0f, maxY);
        }

        private Rect PanelTitleRect()
        {
            return new Rect(_panelRect.x, _panelRect.y, _panelRect.width, TitleH);
        }

        private bool IsPointerOnPanelTitle(Vector2 point)
        {
            return _panelOpen && PanelTitleRect().Contains(point);
        }

        private void DrawBall()
        {
            bool moved = false;
            Vector2 mouse = Event.current.mousePosition;

            if (Event.current.type == EventType.MouseDown &&
                _ballRect.Contains(mouse) &&
                !IsPointerOnPanelTitle(mouse))
            {
                _ballPressed = true;
                _ballDragged = false;
                _ballDownPos = mouse;
            }

            if (_ballPressed && Event.current.type == EventType.MouseDrag)
            {
                _ballRect.x += Event.current.delta.x;
                _ballRect.y -= Event.current.delta.y;
                ClampBallToScreen();
                moved = true;
                if (Vector2.Distance(mouse, _ballDownPos) > 15f)
                    _ballDragged = true;
            }

            if (Event.current.type == EventType.MouseUp)
            {
                if (_ballPressed)
                {
                    if (_ballDragged)
                    {
                        moved = true;
                    }
                    else
                    {
                        _panelOpen = !_panelOpen;
                    }
                }
                _ballPressed = false;
                _ballDragged = false;
            }

            if (moved)
            {
                PlayerPrefs.SetFloat(BallXKey, _ballRect.x);
                PlayerPrefs.SetFloat(BallYKey, _ballRect.y);
                PlayerPrefs.Save();
            }

            GUI.Box(_ballRect, "PE", _ballStyle);
        }

        private void DrawPanel()
        {
            // Draw the panel at its remembered independent position.
            GUI.Box(_panelRect, "PartEditor", _panelStyle);

            // Panel dragging: use the title area as the drag handle.
            bool panelMoved = false;
            Vector2 mouse = Event.current.mousePosition;
            Rect titleRect = PanelTitleRect();
            if (Event.current.type == EventType.MouseDown && titleRect.Contains(mouse))
                _draggingPanel = true;

            if (_draggingPanel && Event.current.type == EventType.MouseDrag)
            {
                _panelRect.x += Event.current.delta.x;
                _panelRect.y -= Event.current.delta.y;
                ClampPanelToScreen();
                panelMoved = true;
            }

            if (Event.current.type == EventType.MouseUp)
            {
                if (_draggingPanel)
                    panelMoved = true;
                _draggingPanel = false;
            }

            if (panelMoved)
            {
                PlayerPrefs.SetFloat(PanelXKey, _panelRect.x);
                PlayerPrefs.SetFloat(PanelYKey, _panelRect.y);
                PlayerPrefs.Save();
            }

            float y = _panelRect.y + 80f;
            // partData/orientation 可能为 null（OnGUI 每帧多次调用），按"无选中"处理以免刷屏异常。
            if (_target == null || _target.partData == null || _target.orientation == null)
            {
                GUI.Label(new Rect(_panelRect.x + 15f, y + 10f, PanelW - 30f, 50f), "No part selected", _labelStyle);
                return;
            }

            // 1.4.10 把 PartData.name 改名成了 displayName。
            GUI.Label(new Rect(_panelRect.x + 15f, y, PanelW - 30f, 50f), _target.partData.displayName, _labelStyle);
            y += 60f;

            DrawSection(ref y, "-- Position --");
            DrawValueRow(ref y, "X", FormatPos(_target.position.x), EditField.PosX,
                () => TransformEditor.Move(_target, -_posStep, 0f, _build),
                () => TransformEditor.Move(_target, _posStep, 0f, _build),
                delegate(string s) { SetPositionX(s); });
            DrawValueRow(ref y, "Y", FormatPos(_target.position.y), EditField.PosY,
                () => TransformEditor.Move(_target, 0f, -_posStep, _build),
                () => TransformEditor.Move(_target, 0f, _posStep, _build),
                delegate(string s) { SetPositionY(s); });

            DrawSection(ref y, "-- Orientation --");
            DrawValueRow(ref y, "Z", FormatAngle(_target.orientation.z), EditField.AngleZ,
                () => TransformEditor.Rotate(_target, _build, -Mathf.RoundToInt(_angleStep)),
                () => TransformEditor.Rotate(_target, _build, Mathf.RoundToInt(_angleStep)),
                delegate(string s) { SetAngleZ(s); });

            // Flip row: two wide buttons.
            float rowY = y;
            float btnW = (PanelW - 30f - 20f) / 2f;
            if (GUI.Button(new Rect(_panelRect.x + 15f, rowY + 4f, btnW, 58f), "FlipX", _buttonStyle))
                TransformEditor.FlipX(_target, _build);
            if (GUI.Button(new Rect(_panelRect.x + 15f + btnW + 20f, rowY + 4f, btnW, 58f), "FlipY", _buttonStyle))
                TransformEditor.FlipY(_target, _build);
            y += 70f;

            DrawSection(ref y, "-- Texture --");
            DrawStepRow(ref y, "Skin", SkinLabel(_target),
                () => TransformEditor.PrevSkin(_target, _build),
                () => TransformEditor.NextSkin(_target, _build));

            DrawSection(ref y, "-- Scale 尺寸(小数) --");
            DrawValueRow(ref y, "X", FormatSize(TransformEditor.GetWidth(_target)), EditField.ScaleX,
                () => TransformEditor.SetWidth(_target, _build, TransformEditor.GetWidth(_target) - _scaleStep),
                () => TransformEditor.SetWidth(_target, _build, TransformEditor.GetWidth(_target) + _scaleStep),
                delegate(string s) { SetScaleX(s); });
            DrawValueRow(ref y, "Y", FormatSize(TransformEditor.GetHeight(_target)), EditField.ScaleY,
                () => TransformEditor.SetHeight(_target, _build, TransformEditor.GetHeight(_target) - _scaleStep),
                () => TransformEditor.SetHeight(_target, _build, TransformEditor.GetHeight(_target) + _scaleStep),
                delegate(string s) { SetScaleY(s); });

            DrawSection(ref y, "-- Steps --");
            DrawValueRow(ref y, "Pos", FormatStep(_posStep), EditField.PosStep,
                () => { _posStep = PrevStep(_posStep, PosSteps); SaveSteps(); },
                () => { _posStep = NextStep(_posStep, PosSteps); SaveSteps(); },
                delegate(string s) { SetPosStep(s); });
            DrawValueRow(ref y, "Angle", FormatStep(_angleStep), EditField.AngleStep,
                () => { _angleStep = PrevStep(_angleStep, AngleSteps); SaveSteps(); },
                () => { _angleStep = NextStep(_angleStep, AngleSteps); SaveSteps(); },
                delegate(string s) { SetAngleStep(s); });
            DrawValueRow(ref y, "Scale", FormatStep(_scaleStep), EditField.ScaleStep,
                () => { _scaleStep = PrevStep(_scaleStep, ScaleSteps); SaveSteps(); },
                () => { _scaleStep = NextStep(_scaleStep, ScaleSteps); SaveSteps(); },
                delegate(string s) { SetScaleStep(s); });
        }

        private void DrawSection(ref float y, string text)
        {
            GUI.Label(new Rect(_panelRect.x + 15f, y, PanelW - 30f, 40f), text, _sectionStyle);
            y += 48f;
        }

        private void DrawValueRow(ref float y, string label, string display, EditField field,
            ButtonAction onLeft, ButtonAction onRight, StringAction onApply)
        {
            const float margin = 15f;
            const float labelW = 90f;
            const float smallBtnW = 76f;
            const float valueW = 260f;
            const float btnH = 56f;
            const float gap = 8f;

            float panelX = _panelRect.x;
            float x = panelX + margin;
            float valueX = x + labelW + gap + smallBtnW + gap;
            float rightX = valueX + valueW + gap;

            GUI.Label(new Rect(x, y + 4f, labelW, btnH), label, _labelStyle);

            if (GUI.Button(new Rect(x + labelW + gap, y + 4f, smallBtnW, btnH), "<", _smallButtonStyle))
                onLeft();

            if (_editingField == field)
            {
                GUI.SetNextControlName("PE" + (int)field);
                _editBuffer = GUI.TextField(new Rect(valueX, y + 4f, valueW, btnH), _editBuffer, _textStyle);
                if (GUI.Button(new Rect(rightX, y + 4f, smallBtnW, btnH), "OK", _smallButtonStyle))
                {
                    onApply(_editBuffer);
                    _editingField = EditField.None;
                }
                if (Event.current.isKey &&
                    Event.current.keyCode == KeyCode.Return &&
                    GUI.GetNameOfFocusedControl() == "PE" + (int)field)
                {
                    onApply(_editBuffer);
                    _editingField = EditField.None;
                }
            }
            else
            {
                if (GUI.Button(new Rect(valueX, y + 4f, valueW, btnH), display, _smallButtonStyle))
                {
                    _editingField = field;
                    _editBuffer = display;
                }
                if (GUI.Button(new Rect(rightX, y + 4f, smallBtnW, btnH), ">", _smallButtonStyle))
                    onRight();
            }

            y += 68f;
        }

        private void DrawStepRow(ref float y, string label, string display, ButtonAction onLeft, ButtonAction onRight)
        {
            const float margin = 15f;
            const float labelW = 90f;
            const float smallBtnW = 76f;
            const float valueW = 260f;
            const float btnH = 56f;
            const float gap = 8f;

            float panelX = _panelRect.x;
            float x = panelX + margin;
            float valueX = x + labelW + gap + smallBtnW + gap;
            float rightX = valueX + valueW + gap;

            GUI.Label(new Rect(x, y + 4f, labelW, btnH), label, _labelStyle);
            if (GUI.Button(new Rect(x + labelW + gap, y + 4f, smallBtnW, btnH), "<", _smallButtonStyle))
                onLeft();
            GUI.Label(new Rect(valueX, y + 4f, valueW, btnH), display, _labelStyle);
            if (GUI.Button(new Rect(rightX, y + 4f, smallBtnW, btnH), ">", _smallButtonStyle))
                onRight();

            y += 68f;
        }

        private void SetPositionX(string s)
        {
            float v;
            if (TryParseFloat(s, out v))
                TransformEditor.Move(_target, v - _target.position.x, 0f, _build);
        }

        private void SetPositionY(string s)
        {
            float v;
            if (TryParseFloat(s, out v))
                TransformEditor.Move(_target, 0f, v - _target.position.y, _build);
        }

        private void SetAngleZ(string s)
        {
            float v;
            if (TryParseFloat(s, out v))
            {
                int target = Mathf.RoundToInt(v);
                int delta = target - _target.orientation.z;
                TransformEditor.Rotate(_target, _build, delta);
            }
        }

        private void SetPosStep(string s)
        {
            float v;
            if (TryParseFloat(s, out v) && v > 0f)
            {
                _posStep = Mathf.Clamp(v, 0.001f, 100f);
                SaveSteps();
            }
        }

        private void SetAngleStep(string s)
        {
            float v;
            if (TryParseFloat(s, out v) && v > 0f)
            {
                _angleStep = Mathf.Clamp(v, 0.1f, 360f);
                SaveSteps();
            }
        }

        private void SetScaleStep(string s)
        {
            float v;
            if (TryParseFloat(s, out v) && v > 0f)
            {
                _scaleStep = Mathf.Clamp(v, 0.01f, 10f);
                SaveSteps();
            }
        }

        private void SetScaleX(string s)
        {
            float v;
            if (TryParseFloat(s, out v))
                TransformEditor.SetWidth(_target, _build, v);
        }

        private void SetScaleY(string s)
        {
            float v;
            if (TryParseFloat(s, out v))
                TransformEditor.SetHeight(_target, _build, v);
        }

        private static string SkinLabel(PlacedPart part)
        {
            if (part == null || part.partData == null || part.partData.skins == null ||
                part.partData.skins.skins == null || part.partData.skins.skins.Length == 0)
                return "(none)";
            int count = part.partData.skins.skins.Length;
            int idx = part.skinId % count;
            if (idx < 0) idx += count;
            Skin skin = part.partData.skins.skins[idx];
            return skin != null ? skin.name : ("Skin " + (idx + 1) + "/" + count);
        }

        private static string FormatSize(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryParseFloat(string s, out float value)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string FormatPos(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatAngle(int v)
        {
            int a = v % 360;
            if (a < 0) a += 360;
            return a.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatStep(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static float NextStep(float current, float[] allowed)
        {
            for (int i = 0; i < allowed.Length; i++)
            {
                if (Mathf.Abs(allowed[i] - current) < 0.0001f)
                    return allowed[(i + 1) % allowed.Length];
            }
            float best = allowed[0];
            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] > current + 0.0001f &&
                    (best <= current + 0.0001f || allowed[i] < best))
                {
                    best = allowed[i];
                }
            }
            return best;
        }

        private static float PrevStep(float current, float[] allowed)
        {
            for (int i = 0; i < allowed.Length; i++)
            {
                if (Mathf.Abs(allowed[i] - current) < 0.0001f)
                    return allowed[(i - 1 + allowed.Length) % allowed.Length];
            }
            float best = allowed[allowed.Length - 1];
            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] < current - 0.0001f &&
                    (best >= current - 0.0001f || allowed[i] > best))
                {
                    best = allowed[i];
                }
            }
            return best;
        }

        private void SaveSteps()
        {
            PlayerPrefs.SetFloat(PosStepKey, _posStep);
            PlayerPrefs.SetFloat(AngleStepKey, _angleStep);
            PlayerPrefs.SetFloat(ScaleStepKey, _scaleStep);
            PlayerPrefs.Save();
        }
    }
}
