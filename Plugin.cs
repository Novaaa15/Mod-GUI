using BepInEx;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Valve.VR;

namespace GorillaMod
{
    [BepInPlugin("com.nova.modui", "Mod GUI", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Rect guiRect = new Rect(20, 30, 300, 330);
        private bool GUIToggled = true;
        private string RoomCode;
        private string Name;

        private bool ExcelFly;
        private bool TFly;
        private bool LongArms;
        private float LongArmAmmount = 1f;

        private bool Chams;
        private bool Tracers;
        private bool NameTags;

        private string Credits = "none right now";

        private void OnGUI()
        {
            if (!GUIToggled)
                CreateLabel("Press F4 To Open GUI");
            else
                CreateLabel("Press F4 To Close GUI");

            guiRect = GUILayout.Window(0, guiRect, MainGUI, "Mod UI | V1.0");
        }

        private void MainGUI(int windowid)
        {
            if (GUIToggled)
            {
                Name = CreateText(Name);
                CreateButton("Change Name", () => { ChangeName(Name); });
                Space(5);
                CreateButton("Disconnect", () => { PhotonNetwork.Disconnect(); });
                CreateButton("Join Random", () => { PhotonNetworkController.Instance.AttemptToJoinPublicRoom(PhotonNetworkController.Instance.currentJoinTrigger ?? GorillaComputer.instance.GetJoinTriggerForZone("forest"), JoinType.Solo); });
                Space(5);
                RoomCode = CreateText(RoomCode);
                CreateButton("Join Room", () => { PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(RoomCode, JoinType.Solo); });
                Space(5);

                ExcelFly = CreateToggle("Excel Fly", ExcelFly);
                TFly = CreateToggle("Fly [Right Trigger]", TFly);
                //CreateLabel("Long Arm Ammount: " + LongArmAmmount.ToString("F1"));
                //LongArmAmmount = CreateSlider(false, LongArmAmmount, 1, 5);
                LongArms = CreateToggle("Long Arms", LongArms);

                Space(5);
                Chams = CreateToggle("Chams", Chams);
                Tracers = CreateToggle("Tracers", Tracers);
                NameTags = CreateToggle("Name Tags", NameTags);

                Space(2);
                CreateLabel("More Coming Soon");

                Space(7);
                CreateLabel("Credits: " + Credits);

                GUI.DragWindow();
            }
        }

        private void Update()
        {
            if (Keyboard.current.f4Key.wasPressedThisFrame)
                GUIToggled = !GUIToggled;

            if (ExcelFly)
            {
                if (Controls.RightPrimary())
                    GTPlayer.Instance.bodyCollider.attachedRigidbody.linearVelocity += GTPlayer.Instance.rightControllerTransform.right;
                if (Controls.LeftPrimary())
                    GTPlayer.Instance.bodyCollider.attachedRigidbody.linearVelocity += -GTPlayer.Instance.leftControllerTransform.right;
            }

            if (TFly)
            {
                if (Controls.RightTrigger())
                {
                    GorillaTagger.Instance.GetComponent<Rigidbody>().transform.position += GTPlayer.Instance.headCollider.transform.forward * Time.deltaTime * 7f;
                    GorillaTagger.Instance.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                }
            }

            if (LongArms)
                GTPlayer.Instance.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            else
                GTPlayer.Instance.transform.localScale = new Vector3(1, 1, 1);

            if (PhotonNetwork.InRoom)
            {
                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    if (rig != VRRig.LocalRig)
                    {
                        if (Chams)
                        {
                            if (rig.mainSkin.material.shader != Shader.Find("GUI/Text Shader"))
                                rig.mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                            rig.mainSkin.material.color = rig.mainSkin.name.Contains("fected") ? Color.red : Color.cyan;
                        }
                        else
                        {
                            if (rig.mainSkin.material.shader != Shader.Find("GorillaTag/UberShader"))
                                rig.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                            rig.mainSkin.material.color = rig.playerColor;
                        }

                        if (Tracers)
                        {
                            GameObject tracerObj = new GameObject();
                            LineRenderer tracer = tracerObj.AddComponent<LineRenderer>();
                            tracer.useWorldSpace = true;
                            tracer.material.shader = Shader.Find("GUI/Text Shader");
                            tracer.startWidth = 0.02f;
                            tracer.endWidth = 0.02f;
                            tracer.startColor = rig.mainSkin.material.name.Contains("fected") ? Color.red : Color.cyan;
                            tracer.positionCount = 2;
                            tracer.SetPosition(0, GTPlayer.Instance.rightControllerTransform.position);
                            tracer.SetPosition(1, rig.transform.position);
                            GameObject.Destroy(tracerObj, Time.deltaTime);
                        }

                        if (NameTags)
                        {
                            GameObject nameTagHolder = new GameObject();
                            nameTagHolder.transform.position = rig.transform.position;
                            TextMeshPro NameTag = nameTagHolder.AddComponent<TextMeshPro>();
                            NameTag.font = GorillaTagger.Instance.offlineVRRig.playerText1.font;
                            NameTag.alignment = TextAlignmentOptions.Center;
                            NameTag.transform.localPosition = rig.headMesh.transform.position + new Vector3(0, 0.7f, 0);
                            NameTag.fontSize = 1;
                            NameTag.fontStyle = FontStyles.Bold;
                            string NameTagText = $"{rig.OwningNetPlayer.NickName}\nFPS: {rig.fps}\nUID: {rig.OwningNetPlayer.UserId}\nPlatform: {(rig.concatStringOfCosmeticsAllowed.Contains("S. FIRST LOGIN") ? "[STEAM]" : "[QUEST]")}";
                            NameTag.text = NameTagText;
                            nameTagHolder.transform.LookAt(Camera.main.transform.position);
                            nameTagHolder.transform.Rotate(0, 180f, 0);
                            GameObject.Destroy(nameTagHolder, Time.deltaTime);
                        }
                    }
                }
            }
        }

        private void ChangeName(string newName)
        {
            GorillaComputer.instance.currentName = newName;
            PhotonNetwork.LocalPlayer.NickName = newName;
            PlayerPrefs.SetString("playerName", newName);
            PlayerPrefs.Save();
        }

        private void Space(int space) => GUILayout.Space(space);

        private void CreateLabel(string labelText) => GUILayout.Label(labelText);

        private float CreateSlider(bool Vertical, float value, float min, float max)
        {
            if (Vertical)
                return GUILayout.VerticalSlider(value, min, max);
            else
                return GUILayout.HorizontalSlider(value, min, max);
        }

        private string CreateText(string text)
        {
            return GUILayout.TextArea(text);
        }

        private bool CreateToggle(string toggleText, bool toggle)
        {
            return GUILayout.Toggle(toggle, toggleText);
        }

        private void CreateButton(string buttonText, Action action)
        {
            if (string.IsNullOrEmpty(buttonText))
                return;

            if (GUILayout.Button(buttonText))
            {
                if (action != null)
                    action.Invoke();
            }
        }
    }

    public class Controls
    {
        public static bool LeftPrimary() => ControllerInputPoller.instance.leftControllerPrimaryButton;
        public static bool RightPrimary() => ControllerInputPoller.instance.rightControllerPrimaryButton;
        public static bool LeftSecondary() => ControllerInputPoller.instance.leftControllerSecondaryButton;
        public static bool RightSecondary() => ControllerInputPoller.instance.rightControllerSecondaryButton;
        public static bool LeftGrip() => ControllerInputPoller.instance.leftGrab;
        public static bool RightGrip() => ControllerInputPoller.instance.rightGrab;
        public static bool LeftTrigger() => ControllerInputPoller.instance.leftControllerIndexFloat > 0.6f;
        public static bool RightTrigger() => ControllerInputPoller.instance.rightControllerIndexFloat > 0.6f;
        public static bool LeftJoystick() => SteamVR_Actions.gorillaTag_LeftJoystickClick.state;
        public static bool RightJoystick() => SteamVR_Actions.gorillaTag_RightJoystickClick.state;
    }
}
