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
		bool triggerPressDown = SteamVR_Input.GetStateDown("GrabPinch", rightHand);
		bool triggerPress = SteamVR_Input.GetState("GrabPinch", rightHand);

		bool menuClick = false;
		// Check for hover on button
		foreach (var go in menuItems)
			{
			if (editSphere.GetComponent < MeshRenderer>().bounds.Intersects(go.GetComponent<MeshRenderer>().bounds))
				{
				go.GetComponent<MeshRenderer>().material.color = Color.white;
				// Problem with bounds, so multiple buttons can be hit simultanously
				// Must thus have make sure that we only interact with first hit
				if (triggerPressDown && !menuClick) 
					{
					if (go.name == "ButtonLayer1")
						{
						StartScript.setCurLayer(Color.yellow);
						menuClick = true;
						}
					else if (go.name == "ButtonLayer2")
						{
						StartScript.setCurLayer(Color.cyan);
						menuClick = true;
						}
					else if (go.name == "nextFrame")
						{
						StartScript.nextPointCloud ();
						menuClick = true;
						}
					else if (go.name == "prevFrame")
						{
						StartScript.prevPointCloud();
						menuClick = true;
						}
					}
				}
			else
				go.GetComponent<MeshRenderer>().material.color = Color.blue;
			}
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
				sceneRoot.transform.localScale += Vector3.one * axis.x * 0.002f;
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
		if (StartScript.selectedObj == null) return;
		if (StartScript.selectedObjMat == null) return;
		StartScript.selectedObjMat.SetFloat("_EditRadius", editSphereRad * (1 / sceneRoot.transform.localScale.x));
		var newEditPos = StartScript.selectedObj.transform.InverseTransformPoint(editSphere.transform.position);
		StartScript.selectedObjMat.SetVector("_EditPos", newEditPos);
		StartScript.selectedObjMat.SetInt ("_TriggerPress", SteamVR_Input.GetState("GrabPinch", rightHand) ? 1 : 0);
		}
	}

