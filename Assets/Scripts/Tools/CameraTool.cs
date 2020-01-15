﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class CameraTool : Selector
    {
        // Start is called before the first frame update
        public GameObject cameraPrefab;
        public Transform cameraContainer;
        public Material screenShotMaterial;
        public Transform world;
        public Transform backgroundFeedback;
        public TextMeshProUGUI tm;
        public float filmHeight = 24f;  // mm
        public float zoomSpeed = 1f;
        private float focal;
        private float cameraFeedbackScale = 1f;
        private float cameraFeedbackScaleFactor = 1.1f;
        private GameObject UIObject = null;
        public RenderTexture renderTexture = null;
        private bool feedbackPositioning = false;
        private Transform focalSlider = null;

        public float Focal
        {
            get { return focal; }
            set 
            { 
                focal = value;
                foreach (KeyValuePair<int, GameObject> data in Selection.selection)
                {
                    GameObject gobject = data.Value;
                    CameraParameters cameraParameters = gobject.GetComponent<CameraController>().GetParameters() as CameraParameters;
                    if (cameraParameters != null)
                        cameraParameters.focal = value;
                }
                UpdateText(); 
            }
        }

        public float deadZone = 0.8f;
        private float frameScaleFactor = 1.03f;

        private Transform controller;
        private Vector3 cameraPreviewDirection = new Vector3(0, 1, 1);

        private void OnEnable()
        {
            foreach(Camera camera in SelectedCameras())
                ComputeFocal(camera);
            /*
            SliderComp focalSlider = panel.Find("FocalLengthSlider").GetComponent<SliderComp>();
            focalSlider.blockSignals = true;
            focalSlider.Value = Mathf.RoundToInt(focal);
            focalSlider.blockSignals = false;
            */
        }
        private void OnDisable()
        {
            feedbackPositioning = false;
        }

        void DisableUI()
        {
            focalSlider.gameObject.SetActive(false);
        }
        void Start()
        {
            Init();
            ToolsUIManager.Instance.OnToolParameterChangedEvent += OnChangeParameter;
            ToolsUIManager.Instance.OnBoolToolParameterChangedEvent += OnBoolChangeParameter;

            foreach (Camera camera in SelectedCameras())
                ComputeFocal(camera);

            cameraPreviewDirection = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

            focalSlider = panel.Find("Focal");
            DisableUI();
            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        void UpdateText()
        {
            string focalStr = "f: " + Mathf.RoundToInt(focal);
            /*
            tm.text = focalStr;
            */
        }

        protected void UpdateCameraFeedback(Vector3 position, Vector3 direction)
        {
            List<Camera> cameras = SelectedCameras();
            if (cameras.Count > 0)
            {
                float far = Camera.main.farClipPlane * 0.8f;
                backgroundFeedback.position = position + direction.normalized * far;
                backgroundFeedback.rotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, 180, 0);
                float scale = far * Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f) * 0.5f * cameraFeedbackScale;

                Camera cam = cameras[0];
                backgroundFeedback.gameObject.SetActive(true);
                backgroundFeedback.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", cam.targetTexture);
                backgroundFeedback.localScale = new Vector3(scale * cameras[0].aspect, scale, scale);
            }
            else
            {
                backgroundFeedback.gameObject.SetActive(false);
            }

        }

        public override void OnUIObjectEnter(int gohash)
        {
            feedbackPositioning = false;
            UIObject = ToolsUIManager.Instance.GetUI3DObject(gohash);
        }

        public override void OnUIObjectExit(int gohash)
        {
            UIObject = null;
        }

        public void OnCheckFeedbackPositionning(bool value)
        {
            feedbackPositioning = value;
        }

        // DEPRECATED
        private void OnBoolChangeParameter(object sender, BoolToolParameterChangedArgs args)
        {
            if (args.toolName != "Camera")
                return;
            feedbackPositioning = args.value;
        }

        private List<Camera> SelectedCameras()
        {
            var selection = Selection.selection.Values;
            List<Camera> selectedCameras = new List<Camera>();
            foreach (var selectedItem in selection)
            {
                Camera cam = selectedItem.GetComponentInChildren<Camera>();               
                if (!cam)
                    continue;
                selectedCameras.Add(cam);
            }
            return selectedCameras;
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (UIObject)
                {
                    GameObject newCamera = Utils.CreateInstance(cameraPrefab, cameraContainer);
                    if (newCamera)
                    {
                        new CommandAddGameObject(newCamera).Submit();

                        Matrix4x4 matrix = cameraContainer.worldToLocalMatrix * transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(0.05f, 0.05f, 0.05f));
                        newCamera.transform.localPosition = matrix.GetColumn(3);
                        newCamera.transform.localRotation = Quaternion.AngleAxis(180, Vector3.forward) * Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                        newCamera.transform.localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

                        ClearSelection();
                        AddToSelection(newCamera);
                    }
                }
                OnStartGrip();
            },
           () =>
           {
               OnEndGrip();
           });
        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            // Update feedback position and scale
            bool trigger = false;
            if (feedbackPositioning
                && VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton ))
            {
                cameraPreviewDirection = transform.forward;
                trigger = true;
            }
            UpdateCameraFeedback(transform.parent.parent.position, cameraPreviewDirection);
            if(trigger)
            {
                // Cam feedback scale
                Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                if (joystickAxis.y > deadZone)
                    cameraFeedbackScale *= cameraFeedbackScaleFactor;
                if (joystickAxis.y < -deadZone)
                    cameraFeedbackScale /= cameraFeedbackScaleFactor;
            }

            // change camera focal
            if (!feedbackPositioning)
            {
                Vector2 val = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                foreach (Camera cam in SelectedCameras())
                {
                    if (Mathf.Abs(val.x) > deadZone && Mathf.Abs(val.y) <= deadZone)
                    {
                        float fov = cam.fieldOfView - val.x * zoomSpeed;
                        cam.fieldOfView = fov;
                        float f = ComputeFocal(cam);
                        if (f < 10)
                        {
                            Focal = 10;
                            ComputeFOV(cam);
                        }
                        if (f > 300)
                        {
                            Focal = 300;
                            ComputeFOV(cam);
                        }
                    }
                    else
                    {
                        ComputeFocal(cam);
                    }
                }
            }

            if (!feedbackPositioning)
            {
                base.DoUpdate(position, rotation);
            }
        }

        protected override void ShowTool(bool show)
        {
            Transform sphere = gameObject.transform.Find("Sphere");
            if (sphere != null)
            {
                sphere.gameObject.SetActive(show);
            }
        }

        private float ComputeFocal(Camera cam)
        {
            Focal = filmHeight / (2f * Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2f));
            return Focal;
        }

        private float ComputeFOV(Camera cam)
        {
            cam.fieldOfView = 2f * Mathf.Atan(filmHeight / (2f * Focal)) * Mathf.Rad2Deg;
            return cam.fieldOfView;
        }

        public void OnChangeFocal(float value)
        {
            Focal = value;
            foreach (Camera cam in SelectedCameras())
                ComputeFOV(cam);
        }

        private void OnChangeParameter(object sender, ToolParameterChangedArgs args)
        {
            // update selection parameters from UI
            if (args.toolName != "Camera")
                return;
            Focal = args.value;
            foreach(Camera cam in SelectedCameras())
                ComputeFOV(cam);
        }

        void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if (null == cameraController)
                    continue;
                CameraParameters cameraParameters = cameraController.GetParameters() as CameraParameters;
                if (cameraParameters != null)
                {
                    UISlider sliderComp = focalSlider.GetComponent<UISlider>();
                    if (sliderComp != null)
                    {
                        sliderComp.Value = cameraParameters.focal;
                        focalSlider.gameObject.SetActive(true);
                        return;
                    }
                    break;
                }
            }
            focalSlider.gameObject.SetActive(false);
        }
    }
}