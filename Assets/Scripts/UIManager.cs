using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class UIManager
	{
	internal Dropdown layerDropdown;
	internal GameObject vrMenuRoot;
	public UIManager()
		{
		vrMenuRoot = GameObject.Find("VR menu");
		registerLoadPointCloudBtnCallback();
		registerExitProgramButton();
		registerLayerSelectorCallback();
		vrAddColliders(vrMenuRoot);
		}

	internal void registerLoadPointCloudBtnCallback()
		{
		var go = GameObject.Find("loadPointCloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => openFileLoadDialog());
			}
		}
	internal void registerExitProgramButton()
		{
		var go = GameObject.Find("ExitProgramButton");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => quitApplicationCallback());
			}
		}

	internal void registerLayerSelectorCallback()
		{
		var go = GameObject.Find("LayerDropdown");
		if (go != null)
			{
			layerDropdown = go.GetComponent<Dropdown>();
			layerDropdown.onValueChanged.AddListener((int newIndex) => layerDropdownCallback(newIndex));
			}
		}

	internal void vrAddColliders(GameObject root)
		{
		int numChildren = root.transform.childCount;
		for (int i = 0; i < numChildren; i++)
			{
			var go = root.transform.GetChild(i).gameObject;
			if (go.GetComponent<Dropdown>() != null 
				|| go.GetComponent<Button>() != null
				|| go.GetComponent<Toggle>() != null)
				{
				if (go.GetComponent<BoxCollider>() == null) // Could already have a boxcollider
					{
					go.AddComponent<BoxCollider>();
					var collider = go.GetComponent<BoxCollider>();
					var rectTrans = go.GetComponent<RectTransform>();
					collider.isTrigger = true;
					float width = rectTrans.offsetMax.x - rectTrans.offsetMin.x;
					float height = rectTrans.offsetMax.y - rectTrans.offsetMin.y;
					if (width == 0) width = 180;
					if (height == 0) height = 20;
					collider.size = new Vector3(width, height, 1);
					}
				}
			if (go.transform.childCount > 0) vrAddColliders(go);
			}
		}

	internal void openFileLoadDialog()
		{
		// Open file dialog with filter
		var extensions = new[] {
		    new ExtensionFilter("Point Cloud", "las"),
		};

		var fileNames = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);
		if (fileNames.Length == 0) return;
		fileNames = fileNames.Where(x => x.EndsWith(".las")).ToArray();

		// Sorting on filename, filenames should be on the form filnamefilter<1> ... filenameFilter<N>
		Array.Sort(fileNames);

		var lasFiles = new LasStruct[fileNames.Length];
		for (int i = 0; i < fileNames.Length; i++)
			lasFiles[i] = LasReader.readLASFile(fileNames[i]);

		StartScript.display = PointCloudObject.newPointCloudObject(lasFiles);
		}

	internal static Color[] layerColors = new Color[]
		{
		Color.blue,
		Color.red,
		Color.white,
		Color.yellow,
		Color.green,
		Color.gray,
		Color.magenta
		};
	internal void layerDropdownCallback(int newIndex)
		{
		EventHandler.registerEvent(EventHandler.events.setlayer, layerColors[newIndex]);
		}
	internal void quitApplicationCallback()
		{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
		}
	}
