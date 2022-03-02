using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ComputeBufferManager
	{
	internal static ComputeBuffer vertBuffer;
	internal static ComputeBuffer normBuffer;
	internal static ComputeBuffer colBuffer;

	internal static int updateCounter = -1;
	internal static int numSlots = 3;
	internal static int numElemsPerSlot = 200000;
	internal class bufferSlot
		{
		public PointCloudObject pc;
		public int lastUpdate;
		public int startIx;

		public bufferSlot(PointCloudObject pc, int lastUpdate, int startIx)
			{
			this.pc = pc;
			this.lastUpdate = lastUpdate;
			this.startIx = startIx;
			}
		}

	internal static List<bufferSlot> slots;


	private static bool initialized;
	internal static void init()
		{
		if (initialized) return;

		vertBuffer = new ComputeBuffer(numSlots * numElemsPerSlot, 4 * 3); // 4 bytes per float, 3 floats		
		Graphics.SetRandomWriteTarget(1, vertBuffer);

		normBuffer = new ComputeBuffer(numSlots * numElemsPerSlot, 4 * 3);
		Graphics.SetRandomWriteTarget(2, normBuffer);

		colBuffer = new ComputeBuffer(numSlots * numElemsPerSlot, 4 * 4);
		Graphics.SetRandomWriteTarget(3, colBuffer);

		slots = new List<bufferSlot>();
		slots.Add(new bufferSlot(null, updateCounter, 0));
		slots.Add(new bufferSlot(null, updateCounter, colBuffer.count / 3));
		slots.Add(new bufferSlot(null, updateCounter, (colBuffer.count / 3) * 2));

		initialized = true;
		}

	internal static int setPointCloud(PointCloudObject pc, bool displayNormals)
		{
		bool isOnGpu = false;
		bufferSlot curSlot = null;
		int cbOffset = 0;
		foreach (var s in slots)
			if (s.pc == pc)
				{ isOnGpu = true; curSlot = s; break; }

		if (isOnGpu)
			{
			curSlot.lastUpdate = ++updateCounter;
			cbOffset = curSlot.startIx;
			}
		else
			{
			int smallestUpdateCounter = int.MaxValue;
			foreach (var s in slots)
				if (s.lastUpdate < smallestUpdateCounter)
					smallestUpdateCounter = s.lastUpdate;

			foreach (var s in slots)
				if (s.lastUpdate == smallestUpdateCounter)
					{
					if (s.pc != null)
						downloadPointCloudAsync(s);

					s.pc = pc;
					s.lastUpdate = ++updateCounter;
					uploadPointCloud(s, displayNormals);
					cbOffset = s.startIx;
					break;
					}
			}

		return cbOffset;
		}

	internal static void downloadPointCloudAsync(bufferSlot slot)
		{
		slot.pc.downloading = true;
		AsyncGPUReadback.Request(colBuffer, colBuffer.count / numSlots, slot.startIx, slot.pc.downloadPointCloudCallback);
		}

	internal static void uploadPointCloud(bufferSlot slot, bool displayNormals)
		{
		vertBuffer.SetData(slot.pc.points, 0, slot.startIx, slot.pc.points.Length);
		colBuffer.SetData(slot.pc.classification, 0, slot.startIx, slot.pc.classification.Length);
		if (displayNormals)
			normBuffer.SetData(slot.pc.normals, 0, slot.startIx, slot.pc.normals.Length);
		}

	internal static void dispose()
		{
		if (vertBuffer != null)
			vertBuffer.Release();
		if (normBuffer != null)
			normBuffer.Release();
		if (colBuffer != null)
			colBuffer.Release();
		}
	}
