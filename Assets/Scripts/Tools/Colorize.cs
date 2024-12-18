﻿/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Colorize : ToolBase
    {
        private UISlider roughnessSlider;
        private UISlider metallicSlider;

        private UIButton colorizeButton;
        private UIButton pickButton;
        private UIButton updateSelectionButton;

        private MeshRenderer previewRenderer;

        private bool uiInitialized = false;
        private bool selectionHasChanged = false;

        private GameObject triggerTooltip;

        private enum ColorOp
        {
            Colorize,
            Pick,
            UpdateSelection
        };

        private ColorOp colorOp = ColorOp.Colorize;
        private ColorOp previousColorOp = ColorOp.Colorize;

        void Start()
        {
            Init();

            roughnessSlider = panel.Find("Roughness").gameObject.GetComponent<UISlider>();
            metallicSlider = panel.Find("Metallic").gameObject.GetComponent<UISlider>();
            colorizeButton = panel.Find("ColorizeButton").gameObject.GetComponent<UIButton>();
            pickButton = panel.Find("PickButton").gameObject.GetComponent<UIButton>();
            updateSelectionButton = panel.Find("SetSelectionButton").gameObject.GetComponent<UIButton>();
            previewRenderer = panel.Find("ColorPreview").gameObject.GetComponent<MeshRenderer>();

            // Realtime update: preview only
            roughnessSlider.onSlideEvent.AddListener((float value) => UpdatePreview());
            metallicSlider.onSlideEvent.AddListener((float value) => UpdatePreview());
            GlobalState.colorChangedEvent.AddListener((Color color) => UpdatePreview());

            // On release update
            roughnessSlider.onReleaseEvent.AddListener(UpdateMaterial);
            metallicSlider.onReleaseEvent.AddListener(UpdateMaterial);
            GlobalState.colorReleasedEvent.AddListener((Color color) => UpdateMaterial());

            SetTooltips();
        }

        public override void SetTooltips()
        {
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Pick Material");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Joystick, Tooltips.Action.HoldHorizontal, "Brush Size");
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Secondary, false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            colorOp = ColorOp.Colorize;
            uiInitialized = false;
        }

        protected override void DoUpdate()
        {
            // Alt button
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primaryButton, () =>
            {
                previousColorOp = colorOp;
                colorOp = ColorOp.Pick;
                uiInitialized = false;
            }, () =>
            {
                colorOp = previousColorOp;
                uiInitialized = false;
            });

            // Update UI
            if (!uiInitialized)
            {
                uiInitialized = true;
                switch (colorOp)
                {
                    case ColorOp.Colorize: OnSetColorize(); break;
                    case ColorOp.UpdateSelection: OnSetUpdateSelection(); break;
                    case ColorOp.Pick: OnSetPick(); break;
                }
                UpdatePreview();
            }

            // Clear selection: only when triggering on nothing with the ColorOp.UpdateSelection
            if (ColorOp.UpdateSelection == colorOp)
            {
                VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, () =>
                {
                    selectionHasChanged = false;
                }, () =>
                {
                    if (!selectionHasChanged && ColorOp.UpdateSelection == colorOp)
                    {
                        CommandRemoveFromSelection command = new CommandRemoveFromSelection(Selection.SelectedObjects.ToList());
                        command.Redo();
                        command.Submit();
                    }
                });
            }
        }

        private void UpdateMaterial()
        {
            if (!gameObject.activeSelf) { return; }

            UpdatePreview();

            if (colorOp == ColorOp.UpdateSelection)
            {
                ColorizeObjects(Selection.SelectedObjects.ToList());
            }
        }

        private void UpdatePreview()
        {
            if (!gameObject.activeSelf) { return; }

            previewRenderer.material.SetColor("_BaseColor", GlobalState.CurrentColor);
            previewRenderer.material.SetFloat("_Smoothness", 1f - roughnessSlider.Value);
            previewRenderer.material.SetFloat("_Metallic", metallicSlider.Value);
        }

        // Called by ColorizeTrigger script
        public void ProcessObjects(List<GameObject> gobjects)
        {
            switch (colorOp)
            {
                case ColorOp.Colorize: ColorizeObjects(gobjects); break;
                case ColorOp.Pick: PickObjects(gobjects); break;
                case ColorOp.UpdateSelection: SelectObjects(gobjects); break;
            }
        }

        private void ColorizeObjects(List<GameObject> gobjects)
        {
            CommandMaterial command = new CommandMaterial(gobjects, GlobalState.CurrentColor, roughnessSlider.Value, metallicSlider.Value);
            command.Submit();
            VRInput.SendHaptic(VRInput.primaryController, 0.1f, 0.2f);
        }

        private void PickObjects(List<GameObject> gobjects)
        {
            MeshRenderer renderer = gobjects[0].GetComponentInChildren<MeshRenderer>();
            GlobalState.CurrentColor = renderer.material.GetColor("_BaseColor");
            if (renderer.material.HasProperty("_Smoothness")) { roughnessSlider.Value = 1f - renderer.material.GetFloat("_Smoothness"); }
            else { roughnessSlider.Value = renderer.material.GetFloat("_Roughness"); }
            metallicSlider.Value = renderer.material.GetFloat("_Metallic");
            UpdatePreview();
            VRInput.SendHaptic(VRInput.primaryController, 0.1f, 0.2f);
        }

        private void SelectObjects(List<GameObject> gobjects)
        {
            bool primaryState = VRInput.GetValue(VRInput.primaryController, CommonUsages.primaryButton);
            ICommand command;
            if (!primaryState) { command = new CommandAddToSelection(gobjects); }
            else { command = new CommandRemoveFromSelection(gobjects); }
            command.Redo();
            command.Submit();
            selectionHasChanged = true;
        }

        // Buttons callbacks
        public void OnSetColorize()
        {
            colorOp = ColorOp.Colorize;
            colorizeButton.Checked = true;
            pickButton.Checked = false;
            updateSelectionButton.Checked = false;
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Trigger, Tooltips.Action.Push, "Set Material");
        }

        public void OnSetPick()
        {
            colorOp = ColorOp.Pick;
            colorizeButton.Checked = false;
            pickButton.Checked = true;
            updateSelectionButton.Checked = false;
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Trigger, Tooltips.Action.Push, "Pick Material");
        }

        public void OnSetUpdateSelection()
        {
            colorOp = ColorOp.UpdateSelection;
            colorizeButton.Checked = false;
            pickButton.Checked = false;
            updateSelectionButton.Checked = true;
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Trigger, Tooltips.Action.Push, "Select Object");
            UpdateMaterial();
        }
    }
}
