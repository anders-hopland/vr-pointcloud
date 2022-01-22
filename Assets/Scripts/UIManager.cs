using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class UIManager
	{
	internal Dropdown layerDropdownDesktop;
	internal Dropdown layerDropdownVr;
	internal GameObject vrMenuRoot;
	internal GameObject desktopMenuRoot;
	public UIManager()
		{
		vrMenuRoot = GameObject.Find("VR menu");
		desktopMenuRoot = GameObject.Find("Desktop menu");
		registerLoadPointCloudBtnCallback();
		registerExitProgramButton();
		registerLayerSelectorCallback();
		registerPrevPointcloudBtn();
		registerNextPointcloudBtn();
		registerDisplayNormalsCallback();
		registerChangePointRadCallback();
		vrAddColliders(vrMenuRoot);
		}

	internal void registerLoadPointCloudBtnCallback()
		{
		var go = Helpers.findGameObject(vrMenuRoot, "loadPointCloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => openFileLoadDialog());
			}

		go = Helpers.findGameObject(desktopMenuRoot, "loadPointCloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => openFileLoadDialog());
			}
		}
	internal void registerExitProgramButton()
		{
		var go = Helpers.findGameObject(vrMenuRoot, "ExitProgramButton");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => quitApplicationCallback());
			}

		go = Helpers.findGameObject(desktopMenuRoot, "ExitProgramButton");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => quitApplicationCallback());
			}
		}

	internal void registerPrevPointcloudBtn()
		{
		var go = Helpers.findGameObject(vrMenuRoot, "PrevPointcloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => EventHandler.registerEvent(EventHandler.events.prev));
			}

		go = Helpers.findGameObject(desktopMenuRoot, "PrevPointcloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => EventHandler.registerEvent(EventHandler.events.prev));
			}
		}

	internal void registerNextPointcloudBtn()
		{
		var go = Helpers.findGameObject(vrMenuRoot, "NextPointcloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => EventHandler.registerEvent(EventHandler.events.next));
			}

		go = Helpers.findGameObject(desktopMenuRoot, "NextPointcloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => EventHandler.registerEvent(EventHandler.events.next));
			}
		}

	internal void registerLayerSelectorCallback()
		{
		var go = Helpers.findGameObject(vrMenuRoot, "LayerDropdown");
		if (go != null)
			{
			layerDropdownVr = go.GetComponent<Dropdown>();
			layerDropdownVr.onValueChanged.AddListener((int newIndex) => layerDropdownCallback(newIndex));
			}

		go = Helpers.findGameObject(desktopMenuRoot, "LayerDropdown");
		if (go != null)
			{
			layerDropdownDesktop = go.GetComponent<Dropdown>();
			layerDropdownDesktop.onValueChanged.AddListener((int newIndex) => layerDropdownCallback(newIndex));
			}
		}

	internal void registerDisplayNormalsCallback()
		{
		var go = Helpers.findGameObject(vrMenuRoot, "NormalsToggle");
		if (go != null)
			{
			var btn = go.GetComponent<Toggle>();
			btn.onValueChanged.AddListener((bool val) => displayNormalsToggleCallback(val));
			}

		go = Helpers.findGameObject(desktopMenuRoot, "NormalsToggle");
		if (go != null)
			{
			var btn = go.GetComponent<Toggle>();
			btn.onValueChanged.AddListener((bool val) => displayNormalsToggleCallback(val));
			}
		}

	internal void registerChangePointRadCallback()
		{
		var go = Helpers.findGameObject(vrMenuRoot, "ChangePointRadButton");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => changePointRadCallback());
			}

		go = Helpers.findGameObject(desktopMenuRoot, "ChangePointRadButton");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => changePointRadCallback());
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
		// Sync both menu elements
		layerDropdownDesktop.value = newIndex;
		layerDropdownVr.value = newIndex;

		EventHandler.registerEvent(EventHandler.events.setlayer, layerColors[newIndex]);
		}

	internal void displayNormalsToggleCallback(bool val)
		{
		StartScript.displayNormals = val;
		}

	internal void changePointRadCallback()
		{
		var input = Helpers.findGameObject(desktopMenuRoot, "PointRadInput");
		var textGo = Helpers.findGameObject(input, "Text");
		var text = textGo.GetComponent<Text>();
		float pointRad = 0.01f;
		float.TryParse(text.text, out pointRad);
		if (pointRad <= 0) pointRad = 0.01f;
		StartScript.display.setMatDisplayRad(pointRad);
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
