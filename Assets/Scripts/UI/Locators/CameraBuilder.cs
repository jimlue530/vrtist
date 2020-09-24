﻿using UnityEngine;

namespace VRtist
{
    public class CameraBuilder : GameObjectBuilder
    {
        public override GameObject CreateInstance(GameObject source, Transform parent = null, bool isPrefab = false)
        {
            GameObject newCamera = GameObject.Instantiate(source, parent);
            RenderTexture renderTexture = new RenderTexture(1920 / 2, 1080 / 2, 24, RenderTextureFormat.Default);
            if (null == renderTexture)
                Debug.LogError("CAMERA FAILED");
            renderTexture.name = "Camera RT";

            newCamera.GetComponentInChildren<Camera>(true).targetTexture = renderTexture;
            newCamera.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", renderTexture);

            VRInput.DeepSetLayer(newCamera, 5);

            newCamera.GetComponentInChildren<CameraController>(true).CopyParameters(source.GetComponentInChildren<CameraController>(true));

            if (!GlobalState.Settings.displayGizmos)
                GlobalState.SetGizmoVisible(newCamera, false);

            // Add UI
            if (isPrefab)
            {
                Transform uiRoot = newCamera.transform.Find("Rotate/UI");

                // Focal slider
                UISlider focalSlider = UISlider.Create(new UISlider.CreateArgs
                {
                    parent = uiRoot,
                    widgetName = "Focal",
                    caption = "Focal",
                    currentValue = 35f,
                    sliderBegin = 0.15f,
                    sliderEnd = 0.86f,
                    relativeLocation = new Vector3(-0.30f, -0.03f, -UISlider.default_thickness),
                    width = 0.3f,
                    height = 0.02f
                });
                focalSlider.DataCurve = GlobalState.Settings.focalCurve;
                focalSlider.SetLightLayer(2);

                // In front button
                UIButton inFrontButton = UIButton.Create(new UIButton.CreateButtonParams
                {
                    parent = uiRoot,
                    widgetName = "InFront",
                    caption = "Always in Front",
                    buttonContent = UIButton.ButtonContent.ImageOnly,
                    icon = UIUtils.LoadIcon("back"),
                    width = 0.02f,
                    height = 0.02f,
                    iconMarginBehavior = UIButton.IconMarginBehavior.UseIconMargin,
                    iconMargin = 0.002f,
                    relativeLocation = new Vector3(-0.30f, -0.005f, -UIButton.default_thickness)
                });
                inFrontButton.isCheckable = true;
                inFrontButton.baseSprite = UIUtils.LoadIcon("back");
                inFrontButton.checkedSprite = UIUtils.LoadIcon("front");
                inFrontButton.SetLightLayer(2);

                // Lock button
            }

            return newCamera;
        }
    }
}
