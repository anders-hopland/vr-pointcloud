using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventHandler
	{
	internal enum events
		{
		prev,
		next,
		setlayer,
		settriggerpress,
		seteditpos,
		seteditradius,
		updatedisplayrad,
		increasedisplayrad,
		decreasedisplayrad
		}
	internal static void registerEvent(events e, params object[] args)
		{
		if (StartScript.display == null) return;
		if (e == events.prev)
			{
			StartScript.display.prevPointCloud();
			}
		else if (e == events.next)
			{
			StartScript.display.nextPointCloud();
			}
		else if (e == events.setlayer)
			{
			if (args.Length == 1)
				{ StartScript.display.setMatEditCol((Color)args[0]); }
			}
		else if (e == events.seteditpos)
			{
			if (args.Length == 1)
				{ StartScript.display.setMatEditPos((Vector3)args[0]); }
			}
		else if (e == events.seteditradius)
			{
			if (args.Length == 1)
				{ StartScript.display.setMatEditRad((float)args[0]); }
			}
		else if (e == events.updatedisplayrad)
			{
			StartScript.display.updateMatDisplayRad();
			}
		else if (e == events.increasedisplayrad)
			{
			StartScript.display.increaseDisplayRad();
			}
		else if (e == events.decreasedisplayrad)
			{
			StartScript.display.decreaseDisplayRad();
			}
		else if (e == events.settriggerpress)
			{
			if (args.Length == 1)
				{ StartScript.display.setMatTriggerPress((bool)args[0]); }
			}
		}


	}
