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
	internal GameObject editSphere;
	internal List<GameObject> menuItems;
	void Start()
		{
		leftHand = SteamVR_Input_Sources.LeftHand;
		rightHand = SteamVR_Input_Sources.RightHand;
		leftHandGo = GameObject.Find("Controller (left)");
		rightHandGo = GameObject.Find("Controller (right)");
		sceneRoot = GameObject.Find("SceneElemRoot");
		editSphere = GameObject.Find("EditSphere");
		editSphereRad = editSphere.transform.localScale.x / 2f;

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
		checkMenuInteraction();
		updateShaderParams();
		checkDragMove();
		checkResize();
		}

	internal bool grabbingRight = false;
	internal bool grabbingLeft = false;
	internal float scaleDist;
	internal bool checkFirstResize = true;
	internal bool triggerMenuInteraction = false;
	internal void checkMenuInteraction()
		{
		//bool triggerPressDown = SteamVR_Input.GetStateDown("GrabPinch", rightHand);
		//bool triggerPress = SteamVR_Input.GetState("GrabPinch", rightHand);
		}

	internal void checkResize()
		{
		if (checkFirstResize)
			{
			checkFirstResize = false;
			Debug.Log("Check first resize");
			}
		Vector2 axis;
		if (!grabbingLeft && !grabbingRight)
			{
			axis = SteamVR_Input.GetVector2("joystick", leftHand);
			if (axis.sqrMagnitude > 0)
				{
				StartScript.sceneRootScale += axis.x * 0.002f;
				sceneRoot.transform.localScale += Vector3.one * StartScript.sceneRootScale;
				}
			}

		axis = SteamVR_Input.GetVector2("joystick", rightHand);
		if (axis.sqrMagnitude > 0)
			{
			editSphereRad += axis.x * 0.0015f;
			if (editSphereRad < 0.015f) editSphereRad = 0.015f;
			else if (editSphereRad > 0.15f) editSphereRad = 0.15f;
			editSphere.transform.localScale = Vector3.one * editSphereRad * 2; // Diameter is radius * 2
			}

		var grabPinch = SteamVR_Input.GetState("GrabPinch", leftHand);
		}
	internal Vector3 lastGrabPos;
	internal Quaternion lastGrabRot;
	internal void checkDragMove()
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

		if ((grabbingLeft && !grabLeft) || (grabbingRight && !grabRight))
			sceneRoot.transform.parent = null;

		grabbingLeft = grabLeft;
		grabbingRight = grabRight;
		}

	internal void updateShaderParams()
		{
		if (StartScript.display == null) return;
		var newEditPos = StartScript.display.transform.InverseTransformPoint(editSphere.transform.position);
		EventHandler.registerEvent(EventHandler.events.seteditpos, newEditPos);
		EventHandler.registerEvent(EventHandler.events.seteditradius, editSphereRad);
		EventHandler.registerEvent(EventHandler.events.settriggerpress, SteamVR_Input.GetState("GrabPinch", rightHand));
		}
	}

