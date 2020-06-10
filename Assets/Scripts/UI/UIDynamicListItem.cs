﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [RequireComponent(typeof(BoxCollider))]
    public class UIDynamicListItem : UIElement
    {
        public UIDynamicList list;
        private Transform content = null;
        public Transform Content { get { return content; } set { content = value; value.parent = transform; AdaptContent(); } }

        [CentimeterFloat] public float depth = 1.0f;
        public float Depth { get { return depth; } set { depth = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }

        public GameObjectHashChangedEvent onObjectClickedEvent = new GameObjectHashChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private BoxCollider boxCollider = null;

        private void Start()
        {
        }

        public void AdaptContent()
        {
            if (content != null)
            {
                Vector3 childExtents = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents; // TODO: what is many meshFilters?
                float w = (width / 2.0f) / childExtents.x;
                float h = (height / 2.0f) / childExtents.y;
                float d = (depth / 2.0f) / childExtents.z;

                content.transform.localScale = new Vector3(w, h, d);

                // adapt collider to the new mesh size (in local space)
                boxCollider = GetComponent<BoxCollider>();

                Vector3 e = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents;
                float colliderZ = Mathf.Max(2.0f * e.z, 0.5f);
                boxCollider.center = transform.InverseTransformVector(content.transform.TransformVector(new Vector3(0f, 0f, colliderZ/2.0f-e.z)));
                boxCollider.size = transform.InverseTransformVector(content.transform.TransformVector(new Vector3(2.0f * e.x, 2.0f * e.y, colliderZ)));
                boxCollider.isTrigger = true;
            }
        }

        public override bool HandlesCursorBehavior() { return true; }
        public override void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform)
        {
            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCursorColliderCenter);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            // Haptic intensity as we go deeper into the widget.
            float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            intensity *= intensity; // ease-in
            VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
            
            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            cursorShapeTransform.position = worldProjectedWidgetPosition;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();

                list.FireItem(Content);
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {

            }
        }
    }
}