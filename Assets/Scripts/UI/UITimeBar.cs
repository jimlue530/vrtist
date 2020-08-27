﻿using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UITimeBar : UIElement
    {
        private static readonly string default_widget_name = " New TimeBar";
        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.03f;
        private static readonly float default_thickness = 0.001f;
        private static readonly int default_min_value = 0;
        private static readonly int default_max_value = 250;
        private static readonly int default_current_value = 0;
        private static readonly string default_background_material_name = "UIBase";
        //private static readonly Color default_color = UIElement.default_background_color;
        private static readonly string default_text = "TimeBar";

        [SpaceHeader("TimeBar Base Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float thickness = 0.001f;
        
        [SpaceHeader("TimeBar Values", 6, 0.8f, 0.8f, 0.8f)]
        public int minValue = 0;
        public int maxValue = 250;
        public int currentValue = 0;
        
        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public IntChangedEvent onSlideEvent = new IntChangedEvent();
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        [SerializeField] private Transform knob = null;

        public int MinValue { get { return GetMinValue(); } set { SetMinValue(value); UpdateTimeBarPosition(); } }
        public int MaxValue { get { return GetMaxValue(); } set { SetMaxValue(value); UpdateTimeBarPosition(); } }
        public int Value { get { return GetValue(); } set { SetValue(value); UpdateTimeBarPosition(); } }

        public override void RebuildMesh()
        {
            // TIME TICKS ??
            // ...

            // BASE
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(width, height, thickness);
            theNewMesh.name = "UITimeBar_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateTimeBarPosition();
        }

        private void UpdateColliderDimensions()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            if (meshFilter != null && coll != null)
            {
                Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                {
                    coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                    coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                }
                else
                {
                    coll.center = initColliderCenter;
                    coll.size = initColliderSize;
                }
            }
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_thickness = 0.001f;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (thickness < min_thickness)
                thickness = min_thickness;
            if (currentValue < minValue)
                currentValue = minValue;
            if (currentValue > maxValue)
                currentValue = maxValue;

            // Realign button to parent anchor if we change the thickness.
            if (-thickness != relativeLocation.z)
                relativeLocation.z = -thickness;

            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                // NOTE: I do all these things because properties can't be called from the inspector.
                try
                {
                    RebuildMesh();
                    UpdateLocalPosition();
                    UpdateAnchor();
                    UpdateChildren();
                    UpdateTimeBarPosition();
                    ResetColor();
                }
                catch(Exception e)
                {
                    Debug.Log("Exception: " + e);
                }

                NeedsRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(0.0f, 0.0f, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width, 0.0f, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(0.0f, -height, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width, -height, -0.001f));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        public override void ResetMaterial()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = BaseColor;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material material = UIUtils.LoadMaterial("UIPanel");
                Material materialInstance = Instantiate(material);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIPanel_Instance_for_UITimebar";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }

        private void UpdateTimeBarPosition()
        {
            float pct = (float)(currentValue - minValue) / (float)(maxValue - minValue);

            float startX = 0.0f;
            float endX = width;
            float posX = startX + pct * (endX - startX);

            Vector3 knobPosition = new Vector3(posX, 0.0f, 0.0f);

            knob.localPosition = knobPosition;
        }

        private int GetMinValue()
        {
            return minValue;
        }

        private void SetMinValue(int intValue)
        {
            minValue = intValue;
        }

        private int GetMaxValue()
        {
            return maxValue;
        }

        private void SetMaxValue(int intValue)
        {
            maxValue = intValue;
        }

        private int GetValue()
        {
            return currentValue;
        }

        private void SetValue(int intValue)
        {
            currentValue = intValue;
        }

        #region ray

        // --- RAY API ----------------------------------------------------

        public override void OnRayEnter()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayEnterClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = true;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayHover()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public override void OnRayHoverClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayExit()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayExitClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true; // exiting while clicking shows a hovered button.
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayClick()
        {
            if (IgnoreRayInteraction())
                return;

            onClickEvent.Invoke();

            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayReleaseInside()
        {
            if (IgnoreRayInteraction())
                return;

            onReleaseEvent.Invoke();

            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public override void OnRayReleaseOutside()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            ResetColor();
        }

        public override bool OverridesRayEndPoint() { return true; }
        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint)
        {
            bool triggerJustClicked = false;
            bool triggerJustReleased = false;
            VRInput.GetInstantButtonEvent(VRInput.rightController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

            // Project ray on the widget plane.
            Plane widgetPlane = new Plane(-transform.forward, transform.position);
            float enter;
            widgetPlane.Raycast(ray, out enter);
            Vector3 worldCollisionOnWidgetPlane = ray.GetPoint(enter);

            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCollisionOnWidgetPlane);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            if (IgnoreRayInteraction())
            {
                // return endPoint at the surface of the widget.
                rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                return;
            }

            float startX = 0;
            float endX = width;

            float currentValuePct = (float)(Value - minValue) / (float)(maxValue - minValue);
            float currentKnobPositionX = startX + currentValuePct * (endX - startX);

            // TODO: apply drag directly on the Value and previous Value.

            // DRAG

            if (!triggerJustClicked)
            {
                localProjectedWidgetPosition.x = Mathf.Lerp(currentKnobPositionX, localProjectedWidgetPosition.x, GlobalState.Settings.RaySliderDrag);
            }

            // CLAMP

            if (localProjectedWidgetPosition.x < startX)
                localProjectedWidgetPosition.x = startX;

            if (localProjectedWidgetPosition.x > endX)
                localProjectedWidgetPosition.x = endX;

            // Compute closest int for snapping.
            float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);
            float fValue = (float)minValue + pct * (float)(maxValue - minValue);
            int roundedValue = Mathf.RoundToInt(fValue);

            // SNAP X to closest int
            localProjectedWidgetPosition.x = startX + ((float)roundedValue - minValue) * (endX - startX) / (float)(maxValue - minValue);
            // SNAP Y to middle of knob object. TODO: use actual knob dimensions
            localProjectedWidgetPosition.y = -height + 0.02f;
            // SNAP Z to the thickness of the knob
            localProjectedWidgetPosition.z = -0.005f;

            // SET

            Value = roundedValue; // will replace the slider knob.
            onSlideEvent.Invoke(roundedValue);

            // Haptic intensity as we go deeper into the widget.
            //float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            //intensity *= intensity; // ease-in

            //VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        // --- / RAY API ----------------------------------------------------

        #endregion

        #region create

        //
        // CREATE
        //

        public class CreateArgs
        {
            public Transform parent = null;
            public string widgetName = UITimeBar.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UITimeBar.default_thickness);
            public float width = UITimeBar.default_width;
            public float height = UITimeBar.default_height;
            public float thickness = UITimeBar.default_thickness;
            public int min_slider_value = UITimeBar.default_min_value;
            public int max_slider_value = UITimeBar.default_max_value;
            public int cur_slider_value = UITimeBar.default_current_value;
            public Material background_material = UIUtils.LoadMaterial(UITimeBar.default_background_material_name);
            public ColorVar background_color = UIOptions.BackgroundColorVar;
            public string caption = UITimeBar.default_text;
        }

        public static void Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (input.parent)
            {
                UIElement elem = input.parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UITimeBar uiTimeBar = go.AddComponent<UITimeBar>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiTimeBar.relativeLocation = input.relativeLocation;
            uiTimeBar.transform.parent = input.parent;
            uiTimeBar.transform.localPosition = parentAnchor + input.relativeLocation;
            uiTimeBar.transform.localRotation = Quaternion.identity;
            uiTimeBar.transform.localScale = Vector3.one;
            uiTimeBar.width = input.width;
            uiTimeBar.height = input.height;
            uiTimeBar.thickness = input.thickness;
            uiTimeBar.minValue = input.min_slider_value;
            uiTimeBar.maxValue = input.max_slider_value;
            uiTimeBar.currentValue = input.cur_slider_value;
            uiTimeBar.baseColor.useConstant = false;
            uiTimeBar.baseColor.reference = input.background_color;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // TODO: new mesh, with time ticks texture
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(input.width, input.height, input.thickness);
                uiTimeBar.Anchor = Vector3.zero;
                BoxCollider coll = go.GetComponent<BoxCollider>();
                if (coll != null)
                {
                    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                    if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                    {
                        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                    }
                    else
                    {
                        coll.center = initColliderCenter;
                        coll.size = initColliderSize;
                    }
                    coll.isTrigger = true;
                }
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.background_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(input.background_material);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 4; // "LightLayer 3"

                uiTimeBar.SetColor(input.background_color.value);
            }

            // KNOB
            GameObject K = new GameObject("Knob");
            uiTimeBar.knob = K.transform;

            UIUtils.SetRecursiveLayer(go, "UI");
        }
        #endregion
    }
}
