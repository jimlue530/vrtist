﻿using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UISliderKnob : MonoBehaviour
    {
        public float radius;
        public float depth;

        public ColorReference _color = new ColorReference();
        public Color Color { get { return _color.Value; } set { _color.Value = value; ResetColor(); } }

        public void RebuildMesh(float newKnobRadius, float newKnobDepth)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            // Make a cylinder using RoundedBox
            Mesh theNewMesh = UIUtils.BuildRoundedBox(2.0f * newKnobRadius, 2.0f * newKnobRadius, newKnobRadius, newKnobDepth);
            theNewMesh.name = "UISliderKnob_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            radius = newKnobRadius;
            depth = newKnobDepth;
        }

        public void ResetColor()
        {
            SetColor(Color);
        }

        private void SetColor(Color c)
        {
            GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", c);
        }

        public class CreateArgs
        {
            public Transform parent;
            public string widgetName;
            public Vector3 relativeLocation;
            public float radius;
            public float depth;
            public Material material;
            public ColorVar c = UIOptions.SliderKnobColorVar;
        }

        public static UISliderKnob Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";
            go.layer = LayerMask.NameToLayer("CameraHidden");

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

            UISliderKnob uiSliderKnob = go.AddComponent<UISliderKnob>();
            uiSliderKnob.transform.parent = input.parent;
            uiSliderKnob.transform.localPosition = parentAnchor + input.relativeLocation;
            uiSliderKnob.transform.localRotation = Quaternion.identity;
            uiSliderKnob.transform.localScale = Vector3.one;
            uiSliderKnob.radius = input.radius;
            uiSliderKnob.depth = input.depth;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(2.0f * input.radius, 2.0f * input.radius, input.radius, input.depth);
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(input.material);
                uiSliderKnob._color.useConstant = false;
                uiSliderKnob._color.reference = input.c;
                meshRenderer.sharedMaterial.SetColor("_BaseColor", uiSliderKnob.Color);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiSliderKnob;
        }
    }
}