﻿using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AnimateControllerButtons : MonoBehaviour
    {
        private Transform gripTransform = null;
        private float gripRotationAmplitude = 15.0f;
        private Quaternion initGripRotation = Quaternion.identity;

        private Transform triggerTransform = null;
        private float triggerRotationAmplitude = 15.0f;
        private Quaternion initTriggerRotation = Quaternion.identity;

        private Transform joystickTransform = null;
        private float joystickRotationAmplitude = 15.0f;
        private Quaternion initJoystickRotation = Quaternion.identity;

        private Transform primaryTransform = null;
        private float primaryTranslationAmplitude = -0.0016f;
        private Vector3 initPrimaryTranslation = Vector3.zero;

        private Transform secondaryTransform = null;
        private float secondaryTranslationAmplitude = -0.0016f;
        private Vector3 initSecondaryTranslation = Vector3.zero;

        private Transform systemTransform = null;
        private float systemTranslationAmplitude = -0.001f;
        private Vector3 initSystemTranslation = Vector3.zero;

        public bool rightHand = true;
        private InputDevice device;

        public float gripDirection = 1.0f;

        // Start is called before the first frame update
        void Start()
        {
            CaptureController();
            CaptureInitialTransforms();
        }

        private void CaptureController()
        {
            if (rightHand)
            {
                device = VRInput.primaryController;
            }
            else
            {
                device = VRInput.secondaryController;
            }
        }

        private void CaptureInitialTransforms()
        {
            gripTransform = transform.Find("GripButtonPivot/GripButton");
            if (null != gripTransform)
            {
                initGripRotation = gripTransform.localRotation;
            }

            triggerTransform = transform.Find("TriggerButtonPivot/TriggerButton");
            if (null != triggerTransform)
            {
                initTriggerRotation = triggerTransform.localRotation;
            }

            joystickTransform = transform.Find("PrimaryAxisPivot/PrimaryAxis");
            if (null != joystickTransform)
            {
                initJoystickRotation = joystickTransform.localRotation;
            }

            primaryTransform = transform.Find("PrimaryButtonPivot/PrimaryButton");
            if (null != primaryTransform)
            {
                initPrimaryTranslation = primaryTransform.localPosition;
            }

            secondaryTransform = transform.Find("SecondaryButtonPivot/SecondaryButton");
            if (null != secondaryTransform)
            {
                initSecondaryTranslation = secondaryTransform.localPosition;
            }

            systemTransform = transform.Find("SystemButtonPivot/SystemButton");
            if (null != systemTransform)
            {
                initSystemTranslation = systemTransform.localPosition;
            }
        }

        public void OnRightHanded(bool isRightHanded)
        {
            // TODO: handle what needs to be handled when we change hands.

            //gripDirection = isRightHanded ? 1.0f : -1.0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (!device.isValid)
            {
                CaptureController();
                CaptureInitialTransforms();
            }

            // GRIP
            if (null != gripTransform)
            {
                float gripAmount = VRInput.GetValue(device, CommonUsages.grip);
                gripTransform.localRotation = initGripRotation * Quaternion.Euler(0, gripAmount * gripRotationAmplitude * gripDirection, 0);
                gripTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", gripAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
            }

            // TRIGGER
            if (null != triggerTransform)
            {
                float triggerAmount = VRInput.GetValue(device, CommonUsages.trigger);
                triggerTransform.localRotation = initTriggerRotation * Quaternion.Euler(triggerAmount * triggerRotationAmplitude, 0, 0);
                triggerTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", triggerAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
            }

            // JOYSTICK
            if (null != joystickTransform)
            {
                Vector2 joystick = VRInput.GetValue(device, CommonUsages.primary2DAxis);
                joystickTransform.localRotation = initJoystickRotation * Quaternion.Euler(joystick.y * joystickRotationAmplitude, 0, joystick.x * -joystickRotationAmplitude);
                joystickTransform.gameObject.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", joystick.magnitude > 0.05f ? UIOptions.SelectedColor : Color.black);
            }

            // PRIMARY
            if (null != primaryTransform)
            {
                bool primaryState = VRInput.GetValue(device, CommonUsages.primaryButton);
                primaryTransform.localPosition = initPrimaryTranslation;
                primaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", primaryState ? UIOptions.SelectedColor : Color.black);
                if (primaryState)
                {
                    primaryTransform.localPosition += new Vector3(0, 0, primaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
                }
            }

            // SECONDARY
            if (null != secondaryTransform)
            {
                bool secondaryState = VRInput.GetValue(device, CommonUsages.secondaryButton);
                secondaryTransform.localPosition = initSecondaryTranslation;
                secondaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", secondaryState ? UIOptions.SelectedColor : Color.black);
                if (secondaryState)
                {
                    secondaryTransform.localPosition += new Vector3(0, 0, secondaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
                }
            }

            // SYSTEM
            if (null != systemTransform)
            {
                ////bool systemState = VRInput.GetValue(device, CommonUsages.menuButton);
                ////systemTransform.localPosition = initSystemTranslation;
                ////systemTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", systemState ? UIOptions.SelectedColor : Color.black);
                ////if (systemState)
                ////{
                ////    systemTransform.localPosition += new Vector3(0, 0, systemTranslationAmplitude); // TODO: quick anim? CoRoutine.
                ////}
            }
        }
    }
}
