using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class VR_Actions : MonoBehaviour
	{
	internal SteamVR_Input_Sources leftHand;
	internal SteamVR_Input_Sources rightHand;
	internal GameObject leftHandGo;
	internal GameObject rightHandGo;
	internal GameObject sceneRoot;
	internal GameObject labelSphere;
	internal List<GameObject> menuItems;

	internal bool grabbingRight;
	internal bool grabbingLeft;
	internal float scaleDist;

	// Grab resize
	internal bool resizing;
	internal float initialResizeDist;
	internal float origResizeScale;
	internal Vector3 startRightHandPos;
	internal Vector3 startLeftHandPos;
	internal Quaternion initialSceneRot;
	internal float initialSceneScale;
	internal Vector3 intitialSceneDir;
	internal Vector3 startDirBetweenHands;


	void Start()
		{
		leftHand = SteamVR_Input_Sources.LeftHand;
		rightHand = SteamVR_Input_Sources.RightHand;
		leftHandGo = GameObject.Find("Controller (left)");
		rightHandGo = GameObject.Find("Controller (right)");
		sceneRoot = GameObject.Find("SceneElemRoot");
		labelSphere = GameObject.Find("LabelSphere");
		editSphereRad = labelSphere.transform.localScale.x / 2f;

		menuItems = new List<GameObject>();
		GameObject go = GameObject.Find("ButtonLayer1");
		menuItems.Add(go);
		go = GameObject.Find("ButtonLayer2");
		menuItems.Add(go);
		go = GameObject.Find("nextFrame");
		menuItems.Add(go);
		go = GameObject.Find("prevFrame");
		menuItems.Add(go);
		}

	internal float editSphereRad = 1f;
	void Update()
		{
		checkJoystickResize();
		checkGrabMove();
		checkGrabResize();
		updateShaderParams();

		if (SteamVR_Input.GetStateDown("GrabPinch", leftHand)) // Left trigger
			{
			StartScript.ui.toggleVrMenu();
			}
		}

	internal void checkJoystickResize()
		{
		var axis = SteamVR_Input.GetVector2("joystick", rightHand);
		if (axis.sqrMagnitude > 0)
			{
			float radIncrease = axis.x * 0.0015f;
			if (editSphereRad + radIncrease <= 0.015f) radIncrease = 0.015f - editSphereRad;
			if (editSphereRad + radIncrease >= 0.15f) radIncrease = 0.15f - editSphereRad;
			editSphereRad += radIncrease;

			//if (editSphereRad < 0.015f) { editSphereRad = 0.015f; }
			//else if (editSphereRad > 0.15f) { editSphereRad = 0.15f; }
			labelSphere.transform.localScale = Vector3.one * editSphereRad * 2; // Diameter is radius * 2
			labelSphere.transform.localPosition += new Vector3(0, radIncrease, radIncrease);
			}
		}

	internal void checkGrabResize()
		{
		if (grabbingLeft && grabbingRight)
			{
			if (!resizing)
				{
				startRightHandPos = rightHandGo.transform.position;
				startLeftHandPos = leftHandGo.transform.position;
				initialSceneRot = StartScript.sceneRoot.transform.rotation;
				initialSceneScale = StartScript.sceneRoot.transform.localScale.x;
				intitialSceneDir = StartScript.sceneRoot.transform.position - (startRightHandPos + startLeftHandPos) * 0.5f;
				startDirBetweenHands = (startRightHandPos - startLeftHandPos).normalized;

				resizing = true;
				}
			else
				{
				Vector3 curRightHandPos = rightHandGo.transform.position;
				Vector3 curLeftHandPos = leftHandGo.transform.position;

				Vector3 curDirBetweenHands = (curRightHandPos - curLeftHandPos).normalized;

				Quaternion rot = Quaternion.FromToRotation(startDirBetweenHands, curDirBetweenHands);

				float currentGrabDistance = (curRightHandPos - curLeftHandPos).magnitude;
				float initialGrabDistance = (startRightHandPos - startLeftHandPos).magnitude;
				float distChange = (currentGrabDistance / initialGrabDistance);

				StartScript.sceneRootScale = initialSceneScale * distChange;
				StartScript.sceneRoot.transform.rotation = rot * initialSceneRot;
				StartScript.sceneRoot.transform.localScale = Vector3.one * StartScript.sceneRootScale;
				StartScript.sceneRoot.transform.position = (0.5f * (curRightHandPos + curLeftHandPos)) + (rot * (intitialSceneDir * distChange));
				}
			}
		else
			{
			resizing = false;
			}
		}
	internal Vector3 lastGrabPos;
	internal Quaternion lastGrabRot;
	internal void checkGrabMove()
		{
		bool grabRight = SteamVR_Input.GetState("GrabGrip", rightHand);
		bool grabLeft = SteamVR_Input.GetState("GrabGrip", leftHand);

		if (grabLeft != grabRight)
			{
			if (grabLeft && !grabbingLeft) // Start grab
				{
				sceneRoot.transform.parent = leftHandGo.transform;
				}
			if (grabRight && !grabbingRight) // Start grab
				{
				sceneRoot.transform.parent = rightHandGo.transform;
				}
			}

		if ((grabbingLeft && !grabLeft) || (grabbingRight && !grabRight) || (grabLeft && grabRight))
			sceneRoot.transform.parent = null;


		grabbingLeft = grabLeft;
		grabbingRight = grabRight;
		}

	internal void updateShaderParams()
		{
		if (StartScript.display == null) return;
		var newEditPos = StartScript.display.transform.InverseTransformPoint(labelSphere.transform.position);
		EventHandler.registerEvent(EventHandler.events.seteditpos, newEditPos);
		EventHandler.registerEvent(EventHandler.events.seteditradius, editSphereRad);
		EventHandler.registerEvent(EventHandler.events.updatedisplayrad);
		EventHandler.registerEvent(EventHandler.events.settriggerpress, SteamVR_Input.GetState("GrabPinch", rightHand));
		}
	}

