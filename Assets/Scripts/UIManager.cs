using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIManager
	{
	public UIManager()
		{
		registerButtonCallbacks();
		}

	internal void registerButtonCallbacks()
		{
		var go = GameObject.Find("loadPointCloudBtn");
		if (go != null)
			{
			var btn = go.GetComponent<Button>();
			btn.onClick.AddListener(() => openFileLoadDialog());
			}
		}

	internal void openFileLoadDialog()
		{
		// Open file with filter
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
	}
