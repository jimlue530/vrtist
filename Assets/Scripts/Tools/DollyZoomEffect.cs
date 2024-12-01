using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;

//算FOV但不APPLY，算出FOCAL再用cameracontroller.focal = ComputeFocal

namespace VRtist
{
    public class DollyZoomEffect : MonoBehaviour
    {
        [SerializeField] public Transform target;
        [SerializeField] public bool LockWidth = false;
        [SerializeField] public float Width = -1; 
        //int index = 1;
        Transform Gtransform;
        Transform Ttransform;
        CameraController cameracontroller;
        LookAtConstraint lookatconstraint = null;
        // Start is called before the first frame update
        void Start()
        {
            Gtransform = this.gameObject.GetComponent<Transform>();
            cameracontroller = this.gameObject.GetComponent<CameraController>();
            lookatconstraint = this.gameObject.GetComponent<LookAtConstraint>();
        }

        // Update is called once per frame
        void Update()
        {
            Vector2 joystickAxis = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
            if (joystickAxis != Vector2.zero) { print(joystickAxis); };
            if (lookatconstraint != null)
            {
                target = lookatconstraint.GetSource(0).sourceTransform;
                Ttransform = target.GetComponent<Transform>();
                float currentDistance = Vector3.Distance(Gtransform.position, Ttransform.position); //just want to see it change while moving the camera
                if (LockWidth)
                {
                    if (Width == -1) //還沒算值
                    {
                        Width = ComputeWidth(cameracontroller.ValueOfFOV(), currentDistance);
                    }
                    float fov = ComputeFieldOfView(Width, currentDistance);
                    cameracontroller.Focal = cameracontroller.ComputeFocalByFOV(fov);
                    CameraTool.SendCameraParams(cameracontroller.gameObject);
                }
                else
                {
                    LockWidth = false;
                    Width = -1;
                }
                return;
            }
            else
            {
                if (LockWidth)
                {
                    print("Needs camera to look at an object!");
                }
                else
                {
                    LockWidth = false;
                    Width = -1;
                }
            }
            lookatconstraint = this.gameObject.GetComponent<LookAtConstraint>();
        }

        public bool LookAt()
        {
            if (lookatconstraint != null) return true;
            else return false;
        }

        private void SetLookAtConstraints()
        {
            if (lookatconstraint != null)
            {
                target = lookatconstraint.GetSource(0).sourceTransform;
            }
            else
            {
                lookatconstraint = this.gameObject.GetComponent<LookAtConstraint>();
            }
        }

        private float ComputeWidth(float fov, float distance)
        {
            return (2.0f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
        }

        private float ComputeFieldOfView(float width, float distance)
        {
            return (2.0f * Mathf.Atan(width * 0.5f / distance) * Mathf.Rad2Deg);
        }
    }
}