﻿using UnityEngine;

namespace VRtist
{
    public class UIShotManager : MonoBehaviour
    {
        public UIDynamicList shotList;
        public UILabel activeShotCountLabel;

        void Start()
        {
            ShotManager.Instance.ShotsChangedEvent.AddListener(OnShotManagerChanged);
            ShotManager.Instance.CurrentShotChangedEvent.AddListener(OnCurrentShotChanged);
            shotList.ItemClickedEvent += OnListItemClicked;
        }

        public ShotItem GetShotItem(int index)
        {
            return shotList.GetItems()[index].GetComponent<ShotItem>();
        }

        // Update UI: set current shot
        void OnCurrentShotChanged()
        {
            int itemIndex = 0;
            int currentShotIndex = ShotManager.Instance.CurrentShot;
            foreach (UIDynamicListItem item in shotList.GetItems())
            {
                ShotItem shotItem = item.Content.GetComponent<ShotItem>();
                shotItem.cameraButton.Checked = (itemIndex++ == currentShotIndex);
            }
        }

        // Update UI: rebuild shots list
        void OnShotManagerChanged()
        {
            shotList.Clear();
            ShotManager sm = ShotManager.Instance;
            int activeShotCount = 0;
            foreach (Shot shot in sm.shots)
            {
                ShotItem shotItem = ShotItem.GenerateShotItem(shot);
                shotItem.AddListeners(OnUpdateShotName, OnUpdateShotStart, OnUpdateShotEnd, OnUpdateShotCameraName, OnUpdateShotColor, OnUpdateShotEnabled, OnSetCamera);
                shotList.AddItem(shotItem.transform);

                if (shot.enabled)
                    activeShotCount++;
            }
            shotList.CurrentIndex = sm.CurrentShot;
            activeShotCountLabel.Text = activeShotCount.ToString() + "/" + sm.shots.Count.ToString();
        }

        void OnListItemClicked(object sender, IndexedGameObjectArgs args)
        {
            ShotManager.Instance.SetCurrentShot(args.index);
        }

        // Update data: add a shot
        public void OnAddShot()
        {
            ShotManager sm = ShotManager.Instance;

            // Take the current shot to find the start frame of the new shot
            // Keep same camera as the current shot if anyone
            int start = 0;
            GameObject camera = null;
            if (sm.CurrentShot != -1)
            {
                Shot selectedShot = sm.shots[sm.CurrentShot];
                start = selectedShot.end + 1;
                camera = selectedShot.camera;
            }
            int end = start + 50;  // arbitrary duration

            // Look at all the shots to find a name for the new shot
            string name = sm.GetUniqueShotName();

            Shot shot = new Shot { name = name, camera = camera, enabled = true, end = end, start = start };
            int shotIndex = sm.CurrentShot;

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.AddShot,
                cameraName = null != camera ? camera.name : "",
                shotIndex = shotIndex,
                shotName = shot.name,
                shotStart = start,
                shotEnd = end,
                shotColor = Color.blue  // TODO: find a unique color
            };
            new CommandShotManager(info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);

            // Add the shot to ShotManager singleton
            shotIndex++;
            sm.InsertShot(shotIndex, shot);
            sm.SetCurrentShot(shotIndex);

            // Rebuild UI
            OnShotManagerChanged();
        }

        // Update data: delete shot
        public void OnDeleteShot()
        {
            ShotManager sm = ShotManager.Instance;

            // Delete the current shot
            int shotIndex = sm.CurrentShot;
            Shot shot = sm.shots[shotIndex];

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.DeleteShot,
                shotName = shot.name,
                cameraName = shot.camera ? shot.camera.name : "",
                shotStart = shot.start,
                shotEnd = shot.end,
                shotColor = shot.color,
                shotEnabled = shot.enabled ? 1 : 0,
                shotIndex = shotIndex
            };

            new CommandShotManager(info).Submit();

            sm.RemoveShot(shotIndex);
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);

            // Rebuild UI
            OnShotManagerChanged();
        }

        // Update data: move shot
        public void OnMoveShot(int offset)
        {
            ShotManager sm = ShotManager.Instance;

            int shotIndex = sm.CurrentShot;
            if (offset < 0 && shotIndex <= 0)
                return;
            if (offset > 0 && shotIndex >= (sm.shots.Count - 1))
                return;
            sm.MoveCurrentShot(offset);

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.MoveShot,
                shotIndex = shotIndex,
                moveOffset = offset
            };
            new CommandShotManager(info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);

            // Rebuild UI
            OnShotManagerChanged();
        }

        public void OnUpdateShotStart(float value)
        {
            ShotManager sm = ShotManager.Instance;

            int intValue = Mathf.FloorToInt(value);
            Shot shot = sm.shots[sm.CurrentShot];
            int endValue = shot.end;
            if (intValue > endValue)
                intValue = endValue;

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.CurrentShot,
                shotStart = shot.start
            };
            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotStart = intValue;
            sm.SetCurrentShotStart(intValue);

            new CommandShotManager(oldInfo, info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }

        public void OnUpdateShotEnd(float value)
        {
            ShotManager sm = ShotManager.Instance;

            int intValue = Mathf.FloorToInt(value);
            Shot shot = sm.shots[sm.CurrentShot];
            int startValue = shot.start;
            if (intValue < startValue)
                intValue = startValue;
            
            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.CurrentShot,
                shotEnd = shot.end,
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotEnd = intValue;
            sm.SetCurrentShotEnd(intValue);

            new CommandShotManager(oldInfo, info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }

        public void OnUpdateShotCameraName(string value)
        {
            ShotManager sm = ShotManager.Instance;
            Shot shot = sm.shots[sm.CurrentShot];

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.CurrentShot,
                cameraName = shot.camera.name
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.cameraName = value;            
            sm.SetCurrentShotCamera(value);

            new CommandShotManager(oldInfo, info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }

        public void OnUpdateShotName(string value)
        {
            ShotManager sm = ShotManager.Instance;
            Shot shot = sm.shots[sm.CurrentShot];

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.CurrentShot,
                shotName = shot.name
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotName = value;
            sm.SetCurrentShotName(value);

            new CommandShotManager(oldInfo, info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }

        public void OnUpdateShotColor(Color value)
        {
            ShotManager sm = ShotManager.Instance;
            Shot shot = sm.shots[sm.CurrentShot];

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.CurrentShot,
                shotColor = shot.color
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotColor = value;
            sm.SetCurrentShotColor(value);

            new CommandShotManager(oldInfo, info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);

        }

        public void OnUpdateShotEnabled(bool value)
        {
            ShotManager sm = ShotManager.Instance;
            Shot shot = sm.shots[sm.CurrentShot];

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.CurrentShot,
                shotEnabled = shot.enabled ? 1 : 0
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotEnabled = value ? 1 : 0;
            sm.SetCurrentShotEnabled(value);

            new CommandShotManager(oldInfo, info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }

        public void OnSetCamera()
        {
            ShotManager sm = ShotManager.Instance;
            Shot currentShot = sm.shots[sm.CurrentShot];

            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.CurrentShot,
                cameraName = currentShot.camera == null ? "" : currentShot.camera.name
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.cameraName = Selection.activeCamera == null ? "" : Selection.activeCamera.name;
            currentShot.camera = Selection.activeCamera;

            new CommandShotManager(oldInfo, info).Submit();
            NetworkClient.GetInstance().SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }
    }
}
