using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
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
		addListenerVrDesktop("LowerPointRadBtn", "Button", () => EventHandler.registerEvent(EventHandler.events.decreasedisplayrad));
		addListenerVrDesktop("HigherPointRadBtn", "Button", () => EventHandler.registerEvent(EventHandler.events.increasedisplayrad));
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

	internal TextMeshProUGUI statisticsObjectNameVr;
	internal TextMeshProUGUI statisticsPointCountVr;
	internal TextMeshProUGUI statisticsPointCountDesktop;
	internal TextMeshProUGUI statisticsObjectNameDesktop;
	internal void updateStatistics(string curObjectName, int numPoints)
		{
		if (statisticsPointCountVr == null)
			statisticsPointCountVr = Helpers.findGameObjectComponent(vrMenuRoot, "TextMeshProUGUI", "StatisticsPointCount") as TextMeshProUGUI;
		if (statisticsPointCountDesktop == null)
			statisticsPointCountDesktop = Helpers.findGameObjectComponent(desktopMenuRoot, "TextMeshProUGUI", "StatisticsPointCount") as TextMeshProUGUI;
		
		if (statisticsObjectNameVr == null)
			statisticsObjectNameVr = Helpers.findGameObjectComponent(vrMenuRoot, "TextMeshProUGUI", "StatisticsObjectName") as TextMeshProUGUI;
		if (statisticsObjectNameDesktop == null)
			statisticsObjectNameDesktop = Helpers.findGameObjectComponent(desktopMenuRoot, "TextMeshProUGUI", "StatisticsObjectName") as TextMeshProUGUI;

		if (statisticsPointCountVr != null) statisticsPointCountVr.text = numPoints.ToString();
		if (statisticsObjectNameVr != null) statisticsObjectNameVr.text = curObjectName;

		if (statisticsPointCountDesktop != null) statisticsPointCountDesktop.text = numPoints.ToString();
		if (statisticsObjectNameDesktop != null) statisticsObjectNameDesktop.text = curObjectName;
		}

	internal void toggleVrMenu()
		{
		vrMenuRoot.gameObject.SetActive(!vrMenuRoot.gameObject.activeSelf);
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

	internal void quitApplicationCallback()
		{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
		}
	}
