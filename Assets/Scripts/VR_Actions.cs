using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class VR_Actions : MonoBehaviour
	{
	SteamVR_Input_Sources leftHand;
	SteamVR_Input_Sources rightHand;
	void Start()
		{
		}
	void Update()
		{
		Debug.Log(SteamVR_Input.GetStateDown("A", rightHand));
		}
	}

