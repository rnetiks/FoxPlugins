using BepInEx;
using BepInEx.Configuration;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHYK
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class TOHYK : BaseUnityPlugin
    {
        public const string GUID = "org.fox.TOHYK";
        public const string PluginName = "TOHYK";
        public const string Version = "1.1";

        public enum TransformMode
        {
            None,
            Move,
            Rotate,
            Scale
        }

        public enum AxisConstraint
        {
            Free,
            AxisX,
            AxisY,
            AxisZ,
            PlaneXY,
            PlaneXZ,
            PlaneYZ,
        }

        public enum ConstraintSpace
        {
            Global,
            Local
        }

        public enum PivotMode
        {
            MedianPoint,
            ActiveElement,
            IndividualOrigins,
        }

        public static ConfigEntry<KeyCode> CfgKeyMove { get; private set; }
        public static ConfigEntry<KeyCode> CfgKeyRotate { get; private set; }
        public static ConfigEntry<KeyCode> CfgKeyScale { get; private set; }
        public static ConfigEntry<KeyCode> CfgKeyAxisX { get; private set; }
        public static ConfigEntry<KeyCode> CfgKeyAxisY { get; private set; }
        public static ConfigEntry<KeyCode> CfgKeyAxisZ { get; private set; }
        public static ConfigEntry<KeyCode> CfgKeyPivotCycle { get; private set; }
        public static ConfigEntry<KeyCode> CfgKeySnapCycle { get; private set; }
        public static ConfigEntry<PivotMode> CfgPivotMode { get; private set; }

        public static ConfigEntry<float> CfgSnapDistance { get; private set; }
        public static ConfigEntry<float> CfgSnapAngle { get; private set; }
        public static ConfigEntry<float> CfgSnapScale { get; private set; }
        public static ConfigEntry<bool> CfgSurfaceSnap { get; private set; }
        public static ConfigEntry<bool> CfgSurfaceAlignNormal { get; private set; }
        public static ConfigEntry<float> CfgSurfaceSnapRadius { get; private set; }
        public static ConfigEntry<float> CfgRotateSensitivity { get; private set; }

        private TransformMode _mode = TransformMode.None;
        private AxisConstraint _constraint = AxisConstraint.Free;
        private ConstraintSpace _space = ConstraintSpace.Global;
        private bool _snapping;

        private Dictionary<int, GuideObject> _targets = new Dictionary<int, GuideObject>();
        private Dictionary<int, Vector3> _initPos = new Dictionary<int, Vector3>();
        private Dictionary<int, Vector3> _initRot = new Dictionary<int, Vector3>();
        private Dictionary<int, Vector3> _initScale = new Dictionary<int, Vector3>();

        private Vector3 _pivotWorld;
        private GuideObject _activeTarget;

        private Vector2 _startMouseScreen;
        private Vector3 _startMousePlane;
        private Vector3 _startMouseAxis;
        private float _startAngle;
        private float _startDist;
        private Material _glMat;

        private const int MESH_CACHE_INTERVAL = 100;
        private int _meshCacheFrame = -MESH_CACHE_INTERVAL;
        private MeshFilter[] _cachedMeshFilters;
        private SkinnedMeshRenderer[] _cachedSkinnedRenderers;

        private GameObject _guideObjectWorkplace;

        private void Awake()
        {
            CfgKeyMove = Config.Bind("Hotkeys", "Move", KeyCode.G,
                "Press to enter Move mode.");
            CfgKeyRotate = Config.Bind("Hotkeys", "Rotate", KeyCode.R,
                "Press to enter Rotate mode.");
            CfgKeyScale = Config.Bind("Hotkeys", "Scale", KeyCode.S,
                "Press to enter Scale mode.");
            CfgKeyAxisX = Config.Bind("Hotkeys", "X Axis", KeyCode.X);
            CfgKeyAxisY = Config.Bind("Hotkeys", "Y Axis", KeyCode.Y);
            CfgKeyAxisZ = Config.Bind("Hotkeys", "Z Axis", KeyCode.Z);
            CfgKeyPivotCycle = Config.Bind("Hotkeys", "Cycle Pivot", KeyCode.Period,
                "Press to cycle through pivot modes (Median / Active / Individual).");
            CfgKeySnapCycle = Config.Bind("Hotkeys", "Toggle Snap", KeyCode.Comma);

            CfgPivotMode = Config.Bind("Pivot", "Pivot Mode", PivotMode.MedianPoint,
                "Transform pivot point. Median Point = center of selection, Active Element = active object, Individual Origins = each object's own origin.");

            CfgSnapDistance = Config.Bind("Snapping", "Snap Distance", 0.1f,
                "Grid snap increment for position (hold Ctrl).");
            CfgSnapAngle = Config.Bind("Snapping", "Snap Angle", 5f,
                "Grid snap increment for rotation in degrees (hold Ctrl).");
            CfgSnapScale = Config.Bind("Snapping", "Snap Scale", 0.1f,
                "Grid snap increment for scale (hold Ctrl).");
            CfgSurfaceSnap = Config.Bind("Snapping", "Surface Snap", false,
                "When enabled during Move, raycast from camera and snap to mesh surfaces and colliders.");
            CfgSurfaceAlignNormal = Config.Bind("Snapping", "Align To Surface Normal", false,
                "When surface-snapping, align the object's up direction to the hit normal.");
            CfgSurfaceSnapRadius = Config.Bind("Snapping", "Surface Snap Max Distance", 100f,
                "Maximum raycast distance for surface snapping.");

            CfgRotateSensitivity = Config.Bind("Sensitivity", "Rotate Sensitivity", 1f,
                "Multiplier for rotation speed in free (trackball) mode.");

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                _glMat = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _glMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _glMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _glMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _glMat.SetInt("_ZWrite", 0);
                _glMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }

            Camera.onPostRender += OnCameraPostRender;
        }

        private void OnDestroy()
        {
            Camera.onPostRender -= OnCameraPostRender;
            if (_glMat != null)
                Destroy(_glMat);
            SetGuideObjectWorkplaceActive(true);
        }

        private void Update()
        {
            if (!Singleton<GuideObjectManager>.IsInstance())
                return;

            if (Input.GetKeyDown(CfgKeyPivotCycle.Value))
            {
                CyclePivotMode();
                if (_mode != TransformMode.None)
                    RefreshMouseReferences();
            }

            if (Input.GetKeyDown(CfgKeySnapCycle.Value))
            {
                CfgSurfaceSnap.Value = !CfgSurfaceSnap.Value;
            }

            if (_mode != TransformMode.None)
            {
                UpdateActiveMode();
            }
            else
            {
                CheckModeEntry();
            }
        }

        private void CyclePivotMode()
        {
            switch (CfgPivotMode.Value)
            {
                case PivotMode.MedianPoint:
                    CfgPivotMode.Value = PivotMode.ActiveElement;
                    break;
                case PivotMode.ActiveElement:
                    CfgPivotMode.Value = PivotMode.IndividualOrigins;
                    break;
                case PivotMode.IndividualOrigins:
                    CfgPivotMode.Value = PivotMode.MedianPoint;
                    break;
            }
        }

        private void SetGuideObjectWorkplaceActive(bool active)
        {
            if (_guideObjectWorkplace == null)
                _guideObjectWorkplace = GameObject.Find("StudioScene/GuideObjectWorkplace");

            if (_guideObjectWorkplace != null)
                _guideObjectWorkplace.SetActive(active);
        }

        private void CheckModeEntry()
        {
            var selected = Singleton<GuideObjectManager>.Instance.selectObjects;
            if (selected == null || selected.Length == 0) return;

            if (GUIUtility.keyboardControl != 0) return;

            TransformMode newMode = TransformMode.None;

            if (Input.GetKeyDown(CfgKeyMove.Value))
                newMode = TransformMode.Move;
            else if (Input.GetKeyDown(CfgKeyRotate.Value))
                newMode = TransformMode.Rotate;
            else if (Input.GetKeyDown(CfgKeyScale.Value))
                newMode = TransformMode.Scale;

            if (newMode == TransformMode.None) return;

            _targets.Clear();
            _initPos.Clear();
            _initRot.Clear();
            _initScale.Clear();

            foreach (var go in selected)
            {
                bool valid;
                switch (newMode)
                {
                    case TransformMode.Move:
                        valid = go.enablePos;
                        break;
                    case TransformMode.Rotate:
                        valid = go.enableRot;
                        break;
                    case TransformMode.Scale:
                        valid = go.enableScale;
                        break;
                    default:
                        valid = false;
                        break;
                }
                if (!valid) continue;

                _targets[go.dicKey] = go;
                _initPos[go.dicKey] = go.changeAmount.pos;
                _initRot[go.dicKey] = go.changeAmount.rot;
                _initScale[go.dicKey] = go.changeAmount.scale;
            }

            if (_targets.Count == 0) return;

            _activeTarget = GetActiveTarget();
            _pivotWorld = ComputePivot();

            _startMouseScreen = Input.mousePosition;
            _startMousePlane = GetMouseWorldOnPlane(_pivotWorld, GetCameraForward());
            _startAngle = 0f;
            _startDist = 0f;

            if (newMode == TransformMode.Rotate)
            {
                var pivotScreen = GetCamera().WorldToScreenPoint(_pivotWorld);
                _startAngle = Mathf.Atan2(
                    _startMouseScreen.y - pivotScreen.y,
                    _startMouseScreen.x - pivotScreen.x);
            }

            if (newMode == TransformMode.Scale)
            {
                var pivotScreen = GetCamera().WorldToScreenPoint(_pivotWorld);
                _startDist = Vector2.Distance(_startMouseScreen, new Vector2(pivotScreen.x, pivotScreen.y));
                if (_startDist < 1f) _startDist = 1f;
            }

            _mode = newMode;
            _constraint = AxisConstraint.Free;
            _space = ConstraintSpace.Global;
            _snapping = false;

            SetGuideObjectWorkplaceActive(false);
        }

        private void Confirm()
        {
            switch (_mode)
            {
                case TransformMode.Move:
                    PushMoveUndo();
                    break;
                case TransformMode.Rotate:
                    PushRotateUndo();
                    break;
                case TransformMode.Scale:
                    PushScaleUndo();
                    break;
            }

            _mode = TransformMode.None;
            SetGuideObjectWorkplaceActive(true);
        }

        private void Cancel()
        {
            foreach (var kvp in _targets)
            {
                var go = kvp.Value;
                switch (_mode)
                {
                    case TransformMode.Move:
                        go.changeAmount.pos = _initPos[kvp.Key];
                        go.transformTarget.localPosition = _initPos[kvp.Key];
                        break;
                    case TransformMode.Rotate:
                        go.changeAmount.pos = _initPos[kvp.Key];
                        go.transformTarget.localPosition = _initPos[kvp.Key];
                        go.changeAmount.rot = _initRot[kvp.Key];
                        go.transformTarget.localEulerAngles = _initRot[kvp.Key];
                        break;
                    case TransformMode.Scale:
                        go.changeAmount.scale = _initScale[kvp.Key];
                        go.transformTarget.localScale = _initScale[kvp.Key];
                        go.changeAmount.pos = _initPos[kvp.Key];
                        go.transformTarget.localPosition = _initPos[kvp.Key];
                        break;
                }
            }

            _mode = TransformMode.None;
            SetGuideObjectWorkplaceActive(true);
        }

        private void UpdateActiveMode()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                Cancel();
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                Confirm();
                return;
            }

            HandleConstraintInput();

            _snapping = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (_targets.Count == 0)
            {
                Cancel();
                return;
            }

            switch (_mode)
            {
                case TransformMode.Move:
                    UpdateMove();
                    break;
                case TransformMode.Rotate:
                    UpdateRotate();
                    break;
                case TransformMode.Scale:
                    UpdateScale();
                    break;
            }
        }

        private void HandleConstraintInput()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(CfgKeyAxisX.Value))
            {
                if (shift)
                    SetConstraint(AxisConstraint.PlaneYZ, ConstraintSpace.Global);
                else
                    CycleAxisConstraint(AxisConstraint.AxisX);
                RefreshMouseReferences();
            }
            else if (Input.GetKeyDown(CfgKeyAxisY.Value))
            {
                if (shift)
                    SetConstraint(AxisConstraint.PlaneXZ, ConstraintSpace.Global);
                else
                    CycleAxisConstraint(AxisConstraint.AxisY);
                RefreshMouseReferences();
            }
            else if (Input.GetKeyDown(CfgKeyAxisZ.Value))
            {
                if (shift)
                    SetConstraint(AxisConstraint.PlaneXY, ConstraintSpace.Global);
                else
                    CycleAxisConstraint(AxisConstraint.AxisZ);
                RefreshMouseReferences();
            }
        }

        private void CycleAxisConstraint(AxisConstraint axis)
        {
            if (_constraint == axis && _space == ConstraintSpace.Global)
            {
                _constraint = axis;
                _space = ConstraintSpace.Local;
            }
            else if (_constraint == axis && _space == ConstraintSpace.Local)
            {
                _constraint = AxisConstraint.Free;
                _space = ConstraintSpace.Global;
            }
            else
            {
                _constraint = axis;
                _space = ConstraintSpace.Global;
            }
        }

        private void SetConstraint(AxisConstraint constraint, ConstraintSpace space)
        {
            if (_constraint == constraint && _space == space)
            {
                _constraint = AxisConstraint.Free;
                _space = ConstraintSpace.Global;
            }
            else
            {
                _constraint = constraint;
                _space = space;
            }
        }

        private void RefreshMouseReferences()
        {
            _startMouseScreen = Input.mousePosition;
            _startMousePlane = GetMouseWorldOnPlane(_pivotWorld, GetPlaneNormal());
            _startMouseAxis = GetMouseOnAxis(_pivotWorld, GetConstraintAxisDir());

            foreach (var kvp in _targets)
            {
                var go = kvp.Value;
                switch (_mode)
                {
                    case TransformMode.Move:
                        go.changeAmount.pos = _initPos[kvp.Key];
                        go.transformTarget.localPosition = _initPos[kvp.Key];
                        break;
                    case TransformMode.Rotate:
                        go.changeAmount.pos = _initPos[kvp.Key];
                        go.transformTarget.localPosition = _initPos[kvp.Key];
                        go.changeAmount.rot = _initRot[kvp.Key];
                        go.transformTarget.localEulerAngles = _initRot[kvp.Key];
                        break;
                    case TransformMode.Scale:
                        go.changeAmount.scale = _initScale[kvp.Key];
                        go.transformTarget.localScale = _initScale[kvp.Key];
                        break;
                }
            }

            _pivotWorld = ComputePivot();

            if (_mode == TransformMode.Rotate)
            {
                var pivotScreen = GetCamera().WorldToScreenPoint(_pivotWorld);
                _startAngle = Mathf.Atan2(
                    _startMouseScreen.y - pivotScreen.y,
                    _startMouseScreen.x - pivotScreen.x);
            }

            if (_mode == TransformMode.Scale)
            {
                var pivotScreen = GetCamera().WorldToScreenPoint(_pivotWorld);
                _startDist = Vector2.Distance(_startMouseScreen, new Vector2(pivotScreen.x, pivotScreen.y));
                if (_startDist < 1f) _startDist = 1f;
            }
        }

        private void UpdateMove()
        {
            if (CfgSurfaceSnap.Value)
            {
                UpdateMoveSurfaceSnap();
                return;
            }

            Vector3 delta = ComputeMoveDelta();

            if (_snapping)
            {
                float snap = CfgSnapDistance.Value;

                if (_constraint == AxisConstraint.AxisX || _constraint == AxisConstraint.AxisY || _constraint == AxisConstraint.AxisZ)
                {
                    Vector3 axisDir = GetConstraintAxisDir();
                    float mag = Vector3.Dot(delta, axisDir);
                    mag = Mathf.Round(mag / snap) * snap;
                    delta = axisDir * mag;
                }
                else
                {
                    delta.x = Mathf.Round(delta.x / snap) * snap;
                    delta.y = Mathf.Round(delta.y / snap) * snap;
                    delta.z = Mathf.Round(delta.z / snap) * snap;
                }
            }

            foreach (var kvp in _targets)
            {
                var go = kvp.Value;
                if (!go.enablePos) continue;

                go.transformTarget.position = InitialWorldPos(kvp.Key) + delta;
                go.changeAmount.pos = go.transformTarget.localPosition;
            }
        }

        private Vector3 ComputeMoveDelta()
        {
            switch (_constraint)
            {
                case AxisConstraint.Free:
                {
                    Vector3 planeNormal = GetCameraForward();
                    Vector3 current = GetMouseWorldOnPlane(_pivotWorld, planeNormal);
                    return current - _startMousePlane;
                }

                case AxisConstraint.AxisX:
                case AxisConstraint.AxisY:
                case AxisConstraint.AxisZ:
                {
                    Vector3 axisDir = GetConstraintAxisDir();
                    Vector3 startProj = _startMouseAxis;
                    Vector3 currentProj = GetMouseOnAxis(_pivotWorld, axisDir);
                    return currentProj - startProj;
                }

                case AxisConstraint.PlaneXY:
                case AxisConstraint.PlaneXZ:
                case AxisConstraint.PlaneYZ:
                {
                    Vector3 normal = GetPlaneNormal();
                    Vector3 current = GetMouseWorldOnPlane(_pivotWorld, normal);
                    return current - _startMousePlane;
                }

                default:
                    return Vector3.zero;
            }
        }

        private void UpdateMoveSurfaceSnap()
        {
            var cam = GetCamera();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (RaycastScene(ray, CfgSurfaceSnapRadius.Value, out Vector3 hitPoint, out Vector3 hitNormal))
            {
                Vector3 delta = hitPoint - _pivotWorld;
                delta = ApplyConstraintMask(delta);

                foreach (var kvp in _targets)
                {
                    var go = kvp.Value;
                    if (!go.enablePos) continue;

                    Vector3 offset = InitialWorldPos(kvp.Key) - _pivotWorld;
                    go.transformTarget.position = InitialWorldPos(kvp.Key) + delta;
                    go.changeAmount.pos = go.transformTarget.localPosition;

                    if (CfgSurfaceAlignNormal.Value && go.enableRot)
                    {
                        Quaternion initWorldRot = InitialWorldRot(kvp.Key);
                        Quaternion alignRot = Quaternion.FromToRotation(Vector3.up, hitNormal) *
                                              Quaternion.Euler(0f, initWorldRot.eulerAngles.y, 0f);
                        go.transformTarget.rotation = alignRot;
                        go.changeAmount.rot = go.transformTarget.localEulerAngles;
                    }
                }
            }
        }

        private Vector3 ApplyConstraintMask(Vector3 delta)
        {
            if (_constraint == AxisConstraint.Free)
                return delta;

            bool local = _space == ConstraintSpace.Local && _activeTarget != null;
            Transform t = local ? _activeTarget.transformTarget : null;

            if (local)
                delta = t.InverseTransformDirection(delta);

            switch (_constraint)
            {
                case AxisConstraint.AxisX:   delta = new Vector3(delta.x, 0f, 0f); break;
                case AxisConstraint.AxisY:   delta = new Vector3(0f, delta.y, 0f); break;
                case AxisConstraint.AxisZ:   delta = new Vector3(0f, 0f, delta.z); break;
                case AxisConstraint.PlaneXY: delta = new Vector3(delta.x, delta.y, 0f); break;
                case AxisConstraint.PlaneXZ: delta = new Vector3(delta.x, 0f, delta.z); break;
                case AxisConstraint.PlaneYZ: delta = new Vector3(0f, delta.y, delta.z); break;
            }

            if (local)
                delta = t.TransformDirection(delta);

            return delta;
        }

        private void UpdateRotate()
        {
            if (_constraint == AxisConstraint.Free)
                UpdateRotateFree();
            else
                UpdateRotateConstrained();
        }

        private void UpdateRotateFree()
        {
            Vector2 currentMouse = Input.mousePosition;
            Vector2 deltaPx = currentMouse - _startMouseScreen;

            float sensitivity = CfgRotateSensitivity.Value * 0.5f;
            float angleX = -deltaPx.y / Screen.height * 360f * sensitivity;
            float angleY = deltaPx.x / Screen.width * 360f * sensitivity;

            if (_snapping)
            {
                float snap = CfgSnapAngle.Value;
                angleX = Mathf.Round(angleX / snap) * snap;
                angleY = Mathf.Round(angleY / snap) * snap;
            }

            Camera cam = GetCamera();
            Vector3 camRight = cam.transform.right;
            Vector3 camUp = cam.transform.up;

            Quaternion rotation = Quaternion.AngleAxis(angleY, camUp) *
                                  Quaternion.AngleAxis(angleX, camRight);

            bool isIndividual = CfgPivotMode.Value == PivotMode.IndividualOrigins;

            foreach (var kvp in _targets)
            {
                var go = kvp.Value;
                if (!go.enableRot) continue;

                Quaternion initWorldRot = InitialWorldRot(kvp.Key);
                go.transformTarget.rotation = rotation * initWorldRot;
                go.changeAmount.rot = go.transformTarget.localEulerAngles;

                if (go.enablePos && !isIndividual)
                {
                    Vector3 initWorld = InitialWorldPos(kvp.Key);
                    go.transformTarget.position = rotation * (initWorld - _pivotWorld) + _pivotWorld;
                    go.changeAmount.pos = go.transformTarget.localPosition;
                }
            }
        }

        private void UpdateRotateConstrained()
        {
            Vector3 axis = GetConstraintAxisDir();

            Camera cam = GetCamera();
            Vector3 pivotScreen = cam.WorldToScreenPoint(_pivotWorld);
            Vector2 currentMouse = Input.mousePosition;

            float currentAngle = Mathf.Atan2(
                currentMouse.y - pivotScreen.y,
                currentMouse.x - pivotScreen.x);

            float deltaAngle = (currentAngle - _startAngle) * Mathf.Rad2Deg;
            
            // Attempt to fix angle jagger, doesn'T seem to work well, maybe Marco con help?
            // Possibly Euler issue?
            while (deltaAngle > 180f) deltaAngle -= 360f;
            while (deltaAngle < -180f) deltaAngle += 360f;

            if (_constraint == AxisConstraint.PlaneXY || _constraint == AxisConstraint.PlaneXZ || _constraint == AxisConstraint.PlaneYZ)
                axis = GetPlaneNormal();

            if (_snapping)
            {
                float snap = CfgSnapAngle.Value;
                deltaAngle = Mathf.Round(deltaAngle / snap) * snap;
            }

            Quaternion rotation = Quaternion.AngleAxis(deltaAngle, axis);

            bool isIndividual = CfgPivotMode.Value == PivotMode.IndividualOrigins;

            foreach (var kvp in _targets)
            {
                var go = kvp.Value;
                if (!go.enableRot) continue;

                Quaternion initWorldRot = InitialWorldRot(kvp.Key);
                go.transformTarget.rotation = rotation * initWorldRot;
                go.changeAmount.rot = go.transformTarget.localEulerAngles;

                if (go.enablePos && !isIndividual)
                {
                    Vector3 initWorld = InitialWorldPos(kvp.Key);
                    go.transformTarget.position = rotation * (initWorld - _pivotWorld) + _pivotWorld;
                    go.changeAmount.pos = go.transformTarget.localPosition;
                }
            }
        }

        private void UpdateScale()
        {
            Camera cam = GetCamera();
            Vector2 currentMouse = Input.mousePosition;

            Vector3 pivotScreen = cam.WorldToScreenPoint(_pivotWorld);
            Vector2 pivotScreen2D = new Vector2(pivotScreen.x, pivotScreen.y);
            float currentDist = Vector2.Distance(currentMouse, pivotScreen2D);
            float ratio = currentDist / _startDist;

            if (_snapping)
            {
                float snap = CfgSnapScale.Value;
                ratio = Mathf.Round(ratio / snap) * snap;
                if (ratio < snap) ratio = snap;
            }

            Vector3 scaleFactor = ComputeScaleFactor(ratio);

            bool isIndividual = CfgPivotMode.Value == PivotMode.IndividualOrigins;

            foreach (var kvp in _targets)
            {
                var go = kvp.Value;
                if (!go.enableScale) continue;

                Vector3 initScale = _initScale[kvp.Key];
                go.transformTarget.localScale = Vector3.Scale(initScale, scaleFactor);
                go.changeAmount.scale = go.transformTarget.localScale;

                if (go.enablePos && !isIndividual)
                {
                    Vector3 initWorld = InitialWorldPos(kvp.Key);
                    Vector3 offset = initWorld - _pivotWorld;
                    go.transformTarget.position = _pivotWorld + Vector3.Scale(offset, scaleFactor);
                    go.changeAmount.pos = go.transformTarget.localPosition;
                }
            }
        }
        private Vector3 ComputeScaleFactor(float ratio)
        {
            switch (_constraint)
            {
                case AxisConstraint.Free:
                    return Vector3.one * ratio;
                case AxisConstraint.AxisX:
                    return new Vector3(ratio, 1f, 1f);
                case AxisConstraint.AxisY:
                    return new Vector3(1f, ratio, 1f);
                case AxisConstraint.AxisZ:
                    return new Vector3(1f, 1f, ratio);
                case AxisConstraint.PlaneXY:
                    return new Vector3(ratio, ratio, 1f);
                case AxisConstraint.PlaneXZ:
                    return new Vector3(ratio, 1f, ratio);
                case AxisConstraint.PlaneYZ:
                    return new Vector3(1f, ratio, ratio);
                default:
                    return Vector3.one;
            }
        }

        private void PushMoveUndo()
        {
            var infos = _targets.Select(kvp => new GuideCommand.EqualsInfo
            {
                dicKey = kvp.Key,
                oldValue = _initPos[kvp.Key],
                newValue = kvp.Value.changeAmount.pos,
            }).ToArray();

            Singleton<UndoRedoManager>.Instance.Push(
                new GuideCommand.MoveEqualsCommand(infos));
        }

        private void PushRotateUndo()
        {
            var rotInfos = _targets.Select(kvp => new GuideCommand.EqualsInfo
            {
                dicKey = kvp.Key,
                oldValue = _initRot[kvp.Key],
                newValue = kvp.Value.changeAmount.rot,
            }).ToArray();

            Singleton<UndoRedoManager>.Instance.Push(
                new GuideCommand.RotationEqualsCommand(rotInfos));

            bool anyMoved = _targets.Any(kvp =>
                kvp.Value.enablePos && _initPos[kvp.Key] != kvp.Value.changeAmount.pos);

            if (anyMoved)
            {
                var posInfos = _targets
                    .Where(kvp => kvp.Value.enablePos)
                    .Select(kvp => new GuideCommand.EqualsInfo
                    {
                        dicKey = kvp.Key,
                        oldValue = _initPos[kvp.Key],
                        newValue = kvp.Value.changeAmount.pos,
                    }).ToArray();

                Singleton<UndoRedoManager>.Instance.Push(
                    new GuideCommand.MoveEqualsCommand(posInfos));
            }
        }

        private void PushScaleUndo()
        {
            var infos = _targets.Select(kvp => new GuideCommand.EqualsInfo
            {
                dicKey = kvp.Key,
                oldValue = _initScale[kvp.Key],
                newValue = kvp.Value.changeAmount.scale,
            }).ToArray();

            Singleton<UndoRedoManager>.Instance.Push(
                new GuideCommand.ScaleEqualsCommand(infos));

            var anyMoved = _targets.Any(kvp => kvp.Value.enablePos && _initPos[kvp.Key] != kvp.Value.changeAmount.pos);

            if (anyMoved)
            {
                var posInfos = _targets.Where(kvp => kvp.Value.enablePos)
                    .Select(kvp => new GuideCommand.EqualsInfo()
                    {
                        dicKey = kvp.Key,
                        oldValue = _initPos[kvp.Key],
                        newValue = kvp.Value.changeAmount.pos,
                    }).ToArray();
                
                Singleton<UndoRedoManager>.Instance.Push(
                    new GuideCommand.MoveEqualsCommand(posInfos));
            }
        }

        private Camera GetCamera()
        {
            if (Singleton<Studio.Studio>.IsInstance())
                return Singleton<Studio.Studio>.Instance.cameraCtrl.mainCmaera;
            return Camera.main;
        }

        private Vector3 GetCameraForward()
        {
            var cam = GetCamera();
            return cam != null ? cam.transform.forward : Vector3.forward;
        }

        private GuideObject GetActiveTarget()
        {
            var mgr = Singleton<GuideObjectManager>.Instance;
            if (mgr.operationTarget != null) return mgr.operationTarget;
            if (mgr.selectObject != null) return mgr.selectObject;
            return _targets.Values.FirstOrDefault();
        }

        private Vector3 ComputePivot()
        {
            if (_targets.Count == 0) return Vector3.zero;

            switch (CfgPivotMode.Value)
            {
                case PivotMode.ActiveElement:
                {
                    if (_activeTarget != null)
                        return _activeTarget.transformTarget.position;
                    goto case PivotMode.MedianPoint;
                }

                case PivotMode.IndividualOrigins:
                    goto case PivotMode.MedianPoint;

                case PivotMode.MedianPoint:
                default:
                {
                    Vector3 sum = Vector3.zero;
                    foreach (var go in _targets.Values)
                        sum += go.transformTarget.position;
                    return sum / _targets.Count;
                }
            }
        }

        private Vector3 InitialWorldPos(int dicKey)
        {
            var go = _targets[dicKey];
            var parent = go.transformTarget.parent;
            if (parent != null)
                return parent.TransformPoint(_initPos[dicKey]);
            return _initPos[dicKey];
        }

        private Quaternion InitialWorldRot(int dicKey)
        {
            var go = _targets[dicKey];
            var parent = go.transformTarget.parent;
            Quaternion localRot = Quaternion.Euler(_initRot[dicKey]);
            if (parent != null)
                return parent.rotation * localRot;
            return localRot;
        }

        private Vector3 GetMouseWorldOnPlane(Vector3 planePoint, Vector3 planeNormal)
        {
            var cam = GetCamera();
            if (cam == null) return planePoint;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(planeNormal.normalized, planePoint);

            if (plane.Raycast(ray, out float enter))
                return ray.GetPoint(enter);

            return ray.GetPoint(100f);
        }

        private Vector3 GetMouseOnAxis(Vector3 axisOrigin, Vector3 axisDir)
        {
            var cam = GetCamera();
            if (cam == null) return axisOrigin;

            Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
            return ClosestPointOnLineToRay(axisOrigin, axisDir.normalized, mouseRay);
        }

        private static Vector3 ClosestPointOnLineToRay(Vector3 lineOrigin, Vector3 lineDir, Ray ray)
        {
            Vector3 w = lineOrigin - ray.origin;
            float a = Vector3.Dot(lineDir, lineDir);
            float b = Vector3.Dot(lineDir, ray.direction);
            float c = Vector3.Dot(ray.direction, ray.direction);
            float d = Vector3.Dot(lineDir, w);
            float e = Vector3.Dot(ray.direction, w);

            float denom = a * c - b * b;
            if (Mathf.Abs(denom) < 1e-8f)
                return lineOrigin;

            float t = (b * e - c * d) / denom;
            return lineOrigin + lineDir * t;
        }

        private Vector3 GetConstraintAxisDir()
        {
            Vector3 dir;
            switch (_constraint)
            {
                case AxisConstraint.AxisX: dir = Vector3.right; break;
                case AxisConstraint.AxisY: dir = Vector3.up; break;
                case AxisConstraint.AxisZ: dir = Vector3.forward; break;
                default: return GetCameraForward();
            }

            if (_space == ConstraintSpace.Local && _activeTarget != null)
                dir = _activeTarget.transformTarget.TransformDirection(dir);

            return dir.normalized;
        }

        private Vector3 GetPlaneNormal()
        {
            Vector3 normal;
            switch (_constraint)
            {
                case AxisConstraint.PlaneXY: normal = Vector3.forward; break;
                case AxisConstraint.PlaneXZ: normal = Vector3.up; break;
                case AxisConstraint.PlaneYZ: normal = Vector3.right; break;
                default: return GetCameraForward();
            }

            if (_space == ConstraintSpace.Local && _activeTarget != null)
                normal = _activeTarget.transformTarget.TransformDirection(normal);

            return normal.normalized;
        }

        private void RefreshMeshCache()
        {
            if (Time.frameCount - _meshCacheFrame < MESH_CACHE_INTERVAL)
                return;

            _meshCacheFrame = Time.frameCount;
            _cachedMeshFilters = FindObjectsOfType<MeshFilter>();
            _cachedSkinnedRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
        }

        private HashSet<Transform> GetTargetTransformSet()
        {
            var set = new HashSet<Transform>();
            foreach (var go in _targets.Values)
            {
                foreach (var t in go.transformTarget.GetComponentsInChildren<Transform>())
                    set.Add(t);
            }
            return set;
        }

        private bool RaycastScene(Ray ray, float maxDist, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitPoint = Vector3.zero;
            hitNormal = Vector3.up;
            float closest = maxDist;
            bool found = false;

            RefreshMeshCache();

            var excluded = GetTargetTransformSet();

            if (_cachedMeshFilters != null)
            {
                foreach (var filter in _cachedMeshFilters)
                {
                    if (filter == null) continue;

                    var renderer = filter.GetComponent<Renderer>();
                    if (renderer == null || !renderer.enabled) continue;

                    if (excluded.Contains(filter.transform)) continue;

                    if (!renderer.bounds.IntersectRay(ray, out float boundsD) || boundsD > closest)
                        continue;

                    var mesh = filter.sharedMesh;
                    if (mesh == null) continue;

                    if (RaycastMesh(ray, mesh, filter.transform, closest, out float d, out Vector3 n))
                    {
                        closest = d;
                        hitPoint = ray.GetPoint(d);
                        hitNormal = n;
                        found = true;
                    }
                }
            }

            if (_cachedSkinnedRenderers != null)
            {
                foreach (var smr in _cachedSkinnedRenderers)
                {
                    if (smr == null || !smr.enabled) continue;
                    if (excluded.Contains(smr.transform)) continue;

                    if (!smr.bounds.IntersectRay(ray, out float boundsD) || boundsD > closest)
                        continue;

                    var bakedMesh = new Mesh();
                    smr.BakeMesh(bakedMesh);

                    if (RaycastMesh(ray, bakedMesh, smr.transform, closest, out float d, out Vector3 n))
                    {
                        closest = d;
                        hitPoint = ray.GetPoint(d);
                        hitNormal = n;
                        found = true;
                    }

                    DestroyImmediate(bakedMesh);
                }
            }

            var disabledColliders = new List<Collider>();
            foreach (var go in _targets.Values)
            {
                foreach (var col in go.transformTarget.GetComponentsInChildren<Collider>())
                {
                    if (col.enabled)
                    {
                        col.enabled = false;
                        disabledColliders.Add(col);
                    }
                }
            }

            if (Physics.Raycast(ray, out RaycastHit physHit, closest))
            {
                closest = physHit.distance;
                hitPoint = physHit.point;
                hitNormal = physHit.normal;
                found = true;
            }

            foreach (var col in disabledColliders)
                col.enabled = true;

            return found;
        }

        private static bool RaycastMesh(Ray ray, Mesh mesh, Transform transform,
            float maxDist, out float hitDist, out Vector3 hitNormal)
        {
            hitDist = maxDist;
            hitNormal = Vector3.up;
            bool hit = false;

            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Vector3 localOrigin = worldToLocal.MultiplyPoint3x4(ray.origin);
            Vector3 localDir = worldToLocal.MultiplyVector(ray.direction).normalized;
            Ray localRay = new Ray(localOrigin, localDir);

            var verts = mesh.vertices;
            var tris = mesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = verts[tris[i]];
                Vector3 v1 = verts[tris[i + 1]];
                Vector3 v2 = verts[tris[i + 2]];

                if (RayTriangle(localRay, v0, v1, v2, out float t) && t > 0f)
                {
                    Vector3 localHit = localRay.GetPoint(t);
                    Vector3 worldHit = transform.TransformPoint(localHit);
                    float worldDist = Vector3.Distance(ray.origin, worldHit);

                    if (worldDist < hitDist)
                    {
                        hitDist = worldDist;
                        Vector3 localNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                        hitNormal = transform.TransformDirection(localNormal).normalized;
                        hit = true;
                    }
                }
            }

            return hit;
        }

        private static bool RayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
        {
            t = 0f;
            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.direction, e2);
            float a = Vector3.Dot(e1, h);

            if (a > -1e-6f && a < 1e-6f) return false;

            float f = 1f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false;

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.direction, q);
            if (v < 0f || u + v > 1f) return false;

            t = f * Vector3.Dot(e2, q);
            return t > 1e-6f;
        }

        private void OnCameraPostRender(Camera cam)
        {
            if (_mode == TransformMode.None) return;
            if (cam != GetCamera()) return;
            if (_glMat == null) return;

            _glMat.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            if (CfgPivotMode.Value == PivotMode.IndividualOrigins)
            {
                foreach (var go in _targets.Values)
                    DrawCrossAt(go.transformTarget.position, 0.03f, new Color(1f, 1f, 1f, 0.5f));
            }

            DrawCrossAt(_pivotWorld, 0.05f, Color.white);

            if (_constraint != AxisConstraint.Free)
                DrawConstraintVisual();

            GL.PopMatrix();
        }

        private void DrawCrossAt(Vector3 pos, float size, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);

            GL.Vertex(pos + Vector3.right * size);
            GL.Vertex(pos - Vector3.right * size);
            GL.Vertex(pos + Vector3.up * size);
            GL.Vertex(pos - Vector3.up * size);
            GL.Vertex(pos + Vector3.forward * size);
            GL.Vertex(pos - Vector3.forward * size);

            GL.End();
        }

        private void DrawConstraintVisual()
        {
            float lineLength = 1000f;

            switch (_constraint)
            {
                case AxisConstraint.AxisX:
                case AxisConstraint.AxisY:
                case AxisConstraint.AxisZ:
                {
                    Vector3 dir = GetConstraintAxisDir();
                    Color col = GetAxisColor(_constraint);

                    GL.Begin(GL.LINES);
                    GL.Color(col);
                    GL.Vertex(_pivotWorld - dir * lineLength);
                    GL.Vertex(_pivotWorld + dir * lineLength);
                    GL.End();
                    break;
                }

                case AxisConstraint.PlaneXY:
                case AxisConstraint.PlaneXZ:
                case AxisConstraint.PlaneYZ:
                {
                    Vector3 a1, a2;
                    Color c1, c2;
                    GetPlaneAxes(out a1, out a2, out c1, out c2);

                    GL.Begin(GL.LINES);
                    GL.Color(c1);
                    GL.Vertex(_pivotWorld - a1 * lineLength);
                    GL.Vertex(_pivotWorld + a1 * lineLength);
                    GL.Color(c2);
                    GL.Vertex(_pivotWorld - a2 * lineLength);
                    GL.Vertex(_pivotWorld + a2 * lineLength);
                    GL.End();
                    break;
                }
            }
        }

        private Color GetAxisColor(AxisConstraint axis)
        {
            switch (axis)
            {
                case AxisConstraint.AxisX:
                    return new Color(1f, 0.2f, 0.2f, 0.9f);
                case AxisConstraint.AxisY:
                    return new Color(0.2f, 1f, 0.2f, 0.9f);
                case AxisConstraint.AxisZ:
                    return new Color(0.3f, 0.3f, 1f, 0.9f);
                default:
                    return Color.white;
            }
        }

        private void GetPlaneAxes(out Vector3 a1, out Vector3 a2, out Color c1, out Color c2)
        {
            switch (_constraint)
            {
                case AxisConstraint.PlaneXY:
                    a1 = Vector3.right;
                    a2 = Vector3.up;
                    c1 = GetAxisColor(AxisConstraint.AxisX);
                    c2 = GetAxisColor(AxisConstraint.AxisY);
                    break;
                case AxisConstraint.PlaneXZ:
                    a1 = Vector3.right;
                    a2 = Vector3.forward;
                    c1 = GetAxisColor(AxisConstraint.AxisX);
                    c2 = GetAxisColor(AxisConstraint.AxisZ);
                    break;
                case AxisConstraint.PlaneYZ:
                    a1 = Vector3.up;
                    a2 = Vector3.forward;
                    c1 = GetAxisColor(AxisConstraint.AxisY);
                    c2 = GetAxisColor(AxisConstraint.AxisZ);
                    break;
                default:
                    a1 = Vector3.right;
                    a2 = Vector3.up;
                    c1 = c2 = Color.white;
                    break;
            }

            if (_space == ConstraintSpace.Local && _activeTarget != null)
            {
                a1 = _activeTarget.transformTarget.TransformDirection(a1);
                a2 = _activeTarget.transformTarget.TransformDirection(a2);
            }
        }

        private void OnGUI()
        {
            if (_mode == TransformMode.None) return;

            string modeStr;
            switch (_mode)
            {
                case TransformMode.Move:   modeStr = "MOVE";   break;
                case TransformMode.Rotate: modeStr = "ROTATE"; break;
                case TransformMode.Scale:  modeStr = "SCALE";  break;
                default:                   modeStr = "";        break;
            }

            string constraintStr;
            switch (_constraint)
            {
                case AxisConstraint.Free:    constraintStr = "Free";          break;
                case AxisConstraint.AxisX:   constraintStr = $"{_space} X";   break;
                case AxisConstraint.AxisY:   constraintStr = $"{_space} Y";   break;
                case AxisConstraint.AxisZ:   constraintStr = $"{_space} Z";   break;
                case AxisConstraint.PlaneXY: constraintStr = $"{_space} XY";  break;
                case AxisConstraint.PlaneXZ: constraintStr = $"{_space} XZ";  break;
                case AxisConstraint.PlaneYZ: constraintStr = $"{_space} YZ";  break;
                default:                     constraintStr = "";              break;
            }

            string pivotStr;
            switch (CfgPivotMode.Value)
            {
                case PivotMode.MedianPoint:       pivotStr = "Median";     break;
                case PivotMode.ActiveElement:      pivotStr = "Active";     break;
                case PivotMode.IndividualOrigins:  pivotStr = "Individual"; break;
                default:                           pivotStr = "";           break;
            }

            string snapStr = _snapping ? "  [SNAP]" : "";
            string surfaceStr = (_mode == TransformMode.Move && CfgSurfaceSnap.Value) ? "  [SURFACE]" : "";

            string text = $"{modeStr},  {constraintStr},  Pivot: {pivotStr}{snapStr}{surfaceStr}";

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft,
                normal = { textColor = Color.white },
            };

            float padding = 800f;
            var shadowStyle = new GUIStyle(style) { normal = { textColor = Color.black } };
            var calcSize = style.CalcSize(new GUIContent(text));
            Rect rect = new Rect(Screen.width / 2 - calcSize.x / 2, Screen.height - 40, 600, 30);
            Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);

            GUI.Label(shadowRect, text, shadowStyle);
            GUI.Label(rect, text, style);
        }
    }
}