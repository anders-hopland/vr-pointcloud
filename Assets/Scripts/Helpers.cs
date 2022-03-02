using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helpers
	{
	/// <summary>
	/// Returns first gameobject it finds with name
	/// Starts search from rootnode
	/// </summary>
	/// <param name="rootNode"></param>
	/// <returns></returns>
	public static GameObject findGameObject(GameObject rootNode, string name)
		{
		GameObject ret = null;
		if (rootNode.name == "name") return rootNode;

		int childCount = rootNode.transform.childCount;
		for (int i = 0; i < childCount; i++)
			{
			if (rootNode.transform.GetChild(i).gameObject.name == name) 
				{ ret = rootNode.transform.GetChild(i).gameObject; break; }
			else
				{
				var temp = findGameObject(rootNode.transform.GetChild(i).gameObject, name);
				if (temp != null)
					{ ret = temp; break; }
				}
			}

		return ret;
		}

	/// <summary>
	/// Returns component of type @param type of first gameobject it first
	/// finds with name @param name. Start search from rootNode
	/// </summary>
	/// <param name="rootNode">Search from</param>
	/// <param name="type">Type of component</param>
	/// <param name="name">Name of gameobject</param>
	/// <returns></returns>
	public static Component findGameObjectComponent(GameObject rootNode, string type, string name)
		{
		Component ret = null;
		if (rootNode.name == "name") return null;

		int childCount = rootNode.transform.childCount;
		for (int i = 0; i < childCount; i++)
			{
			if (rootNode.transform.GetChild(i).gameObject.name == name)
				{
				ret = rootNode.transform.GetChild(i).gameObject.GetComponent(type); 
				break;
				}
			else
				{
				var temp = findGameObjectComponent(rootNode.transform.GetChild(i).gameObject, type, name);
				if (temp != null)
					{ ret = temp; break; }
				}
			}

		return ret;
		}
	}
