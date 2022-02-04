using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class UIManager
	{

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

	internal Dropdown layerDropdownDesktop;
	internal Dropdown layerDropdownVr;
	internal GameObject vrMenuRoot;
	internal GameObject desktopMenuRoot;
	public UIManager()
		{
		vrMenuRoot = GameObject.Find("VR menu");
		desktopMenuRoot = GameObject.Find("Desktop menu");
		registerCallBacks();
		registerVariables();
		addUiColliders(vrMenuRoot);
		}

	internal void registerVariables()
		{
		layerDropdownDesktop = (Dropdown)Helpers.findGameObjectComponent(desktopMenuRoot, "Dropdown", "LayerDropdown");
		layerDropdownVr = (Dropdown)Helpers.findGameObjectComponent(vrMenuRoot, "Dropdown", "LayerDropdown");
		}
	internal void registerCallBacks()
		{
		addListenerVrDesktop("ExitProgramButton", "Button", () => quitApplicationCallback());
		addListenerVrDesktop("loadPointCloudBtn", "Button", () => openFileLoadDialog());
		addListenerVrDesktop("PrevPointcloudBtn", "Button", () => EventHandler.registerEvent(EventHandler.events.prev));
		addListenerVrDesktop("NextPointcloudBtn", "Button", () => EventHandler.registerEvent(EventHandler.events.next));
		addListenerVrDesktop("LayerDropdown", "Dropdown", (int newIndex) => layerDropdownCallback(newIndex));
		addListenerVrDesktop("NormalsToggle", "Toggle", (bool val) => { displayNormalsToggleCallback(val); });
		addListenerVrDesktop("ChangePointRadButton", "Button", changePointRadCallback);
		}

	private void addListenerVrDesktop(string name, string type, Action func)
		{
		var component = Helpers.findGameObjectComponent(vrMenuRoot, type, name);
		if (component != null)
			{
			if (type == "Button") (component as Button).onClick.AddListener(() => func());
			}

		component = Helpers.findGameObjectComponent(desktopMenuRoot, type, name);
		if (component != null)
			{
			if (type == "Button") (component as Button).onClick.AddListener(() => func());
			}
		}

	private void addListenerVrDesktop(string name, string type, Action<int> func)
		{
		var component = Helpers.findGameObjectComponent(vrMenuRoot, type, name);
		if (component != null)
			{
			if (type == "Dropdown") (component as Dropdown).onValueChanged.AddListener((int val) => func(val));
			}

		component = Helpers.findGameObjectComponent(desktopMenuRoot, type, name);
		if (component != null)
			{
			if (type == "Dropdown") (component as Dropdown).onValueChanged.AddListener((int val) => func(val));
			}
		}

	private void addListenerVrDesktop(string name, string type, Action<bool> func)
		{
		var component = Helpers.findGameObjectComponent(vrMenuRoot, type, name);
		if (component != null)
			{
			if (type == "Toggle") (component as Toggle).onValueChanged.AddListener((bool val) => func(val));
			}

		component = Helpers.findGameObjectComponent(desktopMenuRoot, type, name);
		if (component != null)
			{
			if (type == "Toggle") (component as Toggle).onValueChanged.AddListener((bool val) => func(val));
			}
		}

	internal void addUiColliders(GameObject root)
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
			if (go.transform.childCount > 0) addUiColliders(go);
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
