﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
//using UnityEngine.UI.Extensions.ColorPicker;

namespace VRtist
{
    public class Paint : ToolBase
    {
        [Header("Paint Parameters")]
        [SerializeField] private Transform paintContainer;
        [SerializeField] private Transform paintBrush;
        [SerializeField] private Material paintMaterial;
        // Paint tool
        Vector3 paintPrevPosition;
        GameObject currentPaintLine;
        float brushSize = 0.01f;
        float brushFactor = 2f;//0.01f;  // factor between the brush size and the line renderer width
        enum PaintTools { Pencil = 0, FlatPencil }
        PaintTools paintTool = PaintTools.Pencil;
        LineRenderer paintLineRenderer;
        int paintId = 0;
        Color paintColor = Color.blue;
        bool paintOnSurface = false;

        FreeDraw freeDraw;

        protected override void Awake()
        {
            ToolsManager.Instance.registerTool(gameObject, true);
        }

        // Start is called before the first frame update
        void Start()
        {
            paintLineRenderer = transform.gameObject.GetComponent<LineRenderer>();
            if (paintLineRenderer == null) { Debug.LogWarning("Expected a line renderer on the paintItem game object."); }
            else { paintLineRenderer.startWidth = 0.005f; paintLineRenderer.endWidth = 0.005f; }

            freeDraw = new FreeDraw();
            updateButtonsColor();

            brushSize = paintBrush.localScale.x;
            OnPaintColor(paintColor);

            /*
            Transform picker = panel.Find("Picker 2.0");
            ColorPickerControl pickerControl = picker.GetComponent<ColorPickerControl>();
            pickerControl.CurrentColor = new Color(0.25f, 0.25f, 1f);
            */

            // Create tooltips
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Trigger, "Trigger");
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Primary, "Primary");
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Secondary, "Secondary");
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Joystick, "Joystick left / right");
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Grip, "Grip");
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Pointer, "Pointer");
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.System, "The System Button");
        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            // Manage over UI
            /*
            if (uiTools.isOverUI())
            {
                paintLineRenderer.enabled = false;
                return;
            }*/

            switch (paintTool)
            {
                case PaintTools.Pencil: UpdateToolPaintPencil(position, rotation, false); break;
                case PaintTools.FlatPencil: UpdateToolPaintPencil(position, rotation, true); break;
                //case PaintTools.Eraser: UpdateToolPaintEraser(position, rotation); break;
            }
        }

        protected override void ShowTool(bool show)
        {
            Transform sphere = gameObject.transform.Find("Brush");
            if (sphere != null)
            {
                sphere.gameObject.SetActive(show);
            }
        }

        private void UpdateToolPaintPencil(Vector3 position, Quaternion rotation, bool flat)
        {
            // Activate a new paint            
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
            {
                // Create an empty game object with a mesh
                currentPaintLine = Utils.CreatePaint(paintContainer, paintColor);
                ++paintId;
                paintPrevPosition = Vector3.zero;
                freeDraw = new FreeDraw();
                freeDraw.matrix = currentPaintLine.transform.worldToLocalMatrix;

                /*
                freeDraw.matrix = Matrix4x4.identity;
                freeDraw.AddControlPoint(new Vector3(0, 0, 20), 1f);
                freeDraw.AddControlPoint(new Vector3(10, 0, 20), 1f);
                freeDraw.AddControlPoint(new Vector3(10, 10, 20), 1f);                
                freeDraw.AddControlPoint(new Vector3(20, 10, 20), 1f);              
                freeDraw.AddControlPoint(new Vector3(20, 0, 20), 1f);
                */
            },            
             () =>
             {
                // Bake line renderer into a mesh so we can raycast on it
                if (currentPaintLine != null)
                {
                     MeshCollider collider = currentPaintLine.AddComponent<MeshCollider>();
                     PaintParameters paintParameters = currentPaintLine.GetComponent<PaintController>().GetParameters() as PaintParameters;
                     paintParameters.color = paintColor;
                     paintParameters.controlPoints = freeDraw.controlPoints;
                     paintParameters.controlPointsRadius = freeDraw.controlPointsRadius;
                     new CommandAddGameObject(currentPaintLine).Submit();
                     currentPaintLine = null;
                     //OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
                     //VRInput.rightController.StopHaptics();
                }
            });
            float triggerValue = VRInput.GetValue(VRInput.rightController, CommonUsages.trigger);

            // Change brush size
            Vector2 val = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
            if (val != Vector2.zero)
            {
                if (val.y > 0.3f) { brushSize += 0.001f; }
                if (val.y < -0.3f) { brushSize -= 0.001f; }
                brushSize = Mathf.Clamp(brushSize, 0.001f, 0.5f);
                paintBrush.localScale = new Vector3(brushSize, brushSize, brushSize);
            }

            paintLineRenderer.enabled = false;
            Vector3 penPosition = paintBrush.position;
            if (paintOnSurface)
            {
                paintLineRenderer.enabled = true;
                paintLineRenderer.SetPosition(0, transform.position);
                paintLineRenderer.SetPosition(1, transform.position + transform.forward * 10f);
                RaycastHit hitInfo;
                Vector3 direction = transform.forward; // (paintItem.position - centerEye.position).normalized;
                bool hit = Physics.Raycast(transform.position, direction, out hitInfo, Mathf.Infinity);
                if (hit)
                {
                    penPosition = hitInfo.point - 0.001f * direction;
                    paintLineRenderer.SetPosition(1, penPosition);
                }
            }

            // Draw
            float deadZone = VRInput.deadZoneIn;
            if (triggerValue >= deadZone && position != paintPrevPosition && currentPaintLine != null)
            {
                // Add a point (the current world position) to the line renderer                        

                float ratio = 0.5f * (triggerValue - deadZone) / (1f - deadZone);

                // make some vibration feedback
                //OVRInput.SetControllerVibration(1, ratio, OVRInput.Controller.RTouch);
                //VRInput.rightController.SendHapticImpulse(0, ratio, 1f);                       

                if (flat)
                    freeDraw.AddFlatLineControlPoint(penPosition, -transform.forward, brushSize * ratio);
                else
                    freeDraw.AddControlPoint(penPosition, brushSize * ratio);
                // set mesh components
                MeshFilter meshFilter = currentPaintLine.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.mesh;
                mesh.Clear();
                mesh.vertices = freeDraw.vertices;
                mesh.normals = freeDraw.normals;
                mesh.triangles = freeDraw.triangles;
            }

            paintPrevPosition = position;
        }        

        public void OnPaintColor(Color color)
        {
            paintColor = color;
            paintBrush.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", color);
        }

        public void OnCheckPaintOnSurface(bool value)
        {
            paintOnSurface = value;
        }

        void updateButtonsColor()
        {
            if (!panel)
                return;

            for (int i = 0; i < panel.childCount; i++)
            {
                GameObject child = panel.GetChild(i).gameObject;
                // en supposant qu'il n'y a que les boutons comme enfant avec des images
                Image image = child.GetComponentInChildren<Image>();
                if (image)
                {
                    image.color = Selection.UnselectedColor;
                    if (child.name == "Pencil" && paintTool == PaintTools.Pencil)
                        image.color = Selection.SelectedColor;
                    if (child.name == "Flat" && paintTool == PaintTools.FlatPencil)
                        image.color = Selection.SelectedColor;
                }
            }
        }

        public void PaintSelectPencil()
        {
            paintTool = PaintTools.Pencil;
            updateButtonsColor();
        }

        //public void PaintSelectEraser()
        //{
        //    paintTool = PaintTools.Eraser;
        //    updateButtonsColor();
        //}

        public void PaintSelectFlatPencil()
        {
            paintTool = PaintTools.FlatPencil;
            updateButtonsColor();
        }
    }
}