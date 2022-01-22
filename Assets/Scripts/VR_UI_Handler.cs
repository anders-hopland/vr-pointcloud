using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Valve.VR.Extras;

public class VR_UI_Handler : MonoBehaviour
	{
	private SteamVR_LaserPointer steamVrLaserPointer;

	private void Awake()
		{
		var go = GameObject.Find("Controller (right)");
		steamVrLaserPointer = go.GetComponent<SteamVR_LaserPointer>();
		steamVrLaserPointer.PointerIn += OnPointerIn;
		steamVrLaserPointer.PointerOut += OnPointerOut;
		steamVrLaserPointer.PointerClick += OnPointerClick;
		}

	private void OnPointerClick(object sender, PointerEventArgs e)
		{
		IPointerClickHandler clickHandler = e.target.GetComponent<IPointerClickHandler>();
		if (clickHandler == null)
			{
			return;
			}

		// Need to have colliders on newly created dropdown gameobjects
		// Maybe make a cleaner way to do this later
		if (e.target.gameObject.GetComponent<Dropdown>() != null)
			{
			StartScript.ui.vrAddColliders(e.target.gameObject);
			}

		clickHandler.OnPointerClick(new PointerEventData(EventSystem.current));
		}

	private void OnPointerOut(object sender, PointerEventArgs e)
		{
		IPointerExitHandler pointerExitHandler = e.target.GetComponent<IPointerExitHandler>();
		if (pointerExitHandler == null)
			{
			return;
			}

		pointerExitHandler.OnPointerExit(new PointerEventData(EventSystem.current));
		}

	private void OnPointerIn(object sender, PointerEventArgs e)
		{
		IPointerEnterHandler pointerEnterHandler = e.target.GetComponent<IPointerEnterHandler>();
		if (pointerEnterHandler == null)
			{
			return;
			}

		pointerEnterHandler.OnPointerEnter(new PointerEventData(EventSystem.current));
		}
	}
