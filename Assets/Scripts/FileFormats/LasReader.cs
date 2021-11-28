using System.IO;
using System;
using System.Globalization;

public class LasReader
	{
	public static LasStruct readLASFile(string fileName)
		{
		LasStruct lasFile = new LasStruct();
		if (!File.Exists(fileName)) return lasFile;
		FileStream fs = File.Open(fileName, FileMode.Open);
		LasHeader header = readLasHeader(fs);
		LasPoint[] lasPoints = readLasPoints(fs, header);

		lasFile.header = header;
		lasFile.points = lasPoints;
		return lasFile;
		}

	private static LasHeader readLasHeader(FileStream file)
		{
		LasHeader header = new LasHeader();
		byte[] bytes = new byte[235]; // Size of header
		file.Read(bytes, 0, bytes.Length);

		header.fileSignature = new byte[4];
		for (int i = 0; i < 4; i++)
			header.fileSignature[i] = bytes[i];

		header.fileSourceId = BitConverter.ToUInt16(bytes, 4);
		header.globalEncoding = BitConverter.ToUInt16(bytes, 6);
		header.projectId1 = BitConverter.ToUInt32(bytes, 8);
		header.projectId2 = BitConverter.ToUInt16(bytes, 12);
		header.projectId4 = new byte[8];
		for (int i = 0; i < 8; i++)
			header.projectId4[i] = bytes[i + 16];

		header.versionMajor = bytes[24];
		header.versionMinor = bytes[25];

		header.systemIdentifier = new byte[32];
		for (int i = 0; i < 32; i++)
			header.systemIdentifier[i] = bytes[i + 26];

		header.generatingSoftware = new byte[32];
		for (int i = 0; i < 32; i++)
			header.generatingSoftware[i] = bytes[i + 58];

		header.fileCreationDay = BitConverter.ToUInt16(bytes, 90);
		header.fileCreationYear = BitConverter.ToUInt16(bytes, 92);
		header.headerSize = BitConverter.ToUInt16(bytes, 94);
		header.offsetToData = BitConverter.ToUInt32(bytes, 96);
		header.numVariableLengthRecords = BitConverter.ToUInt32(bytes, 100);
		header.pointDataFormat = bytes[104];
		header.pointDataLength = BitConverter.ToUInt16(bytes, 105);
		header.numPoints = BitConverter.ToUInt32(bytes, 107);

		header.numPointsByReturn = new uint[5];
		for (int i = 0; i < 5; i++)
			header.numPointsByReturn[i] = BitConverter.ToUInt32(bytes, i * 4 + 111);

		header.xScaleFactor = BitConverter.ToDouble(bytes, 131);
		header.yScaleFactor = BitConverter.ToDouble(bytes, 139);
		header.zScaleFactor = BitConverter.ToDouble(bytes, 147);
		header.xOffset = BitConverter.ToDouble(bytes, 155);
		header.yOffset = BitConverter.ToDouble(bytes, 163);
		header.zOffset = BitConverter.ToDouble(bytes, 171);
		header.maxX = BitConverter.ToDouble(bytes, 179);
		header.minX = BitConverter.ToDouble(bytes, 187);
		header.maxY = BitConverter.ToDouble(bytes, 195);
		header.minY = BitConverter.ToDouble(bytes, 203);
		header.maxZ = BitConverter.ToDouble(bytes, 211);
		header.minZ = BitConverter.ToDouble(bytes, 219);
		header.startOfWaveformDataPacket = BitConverter.ToUInt64(bytes, 227);

		return header;
		}

	private static LasPoint[] readLasPoints(FileStream file, LasHeader header)
		{
		LasPoint[] lasPoints = new LasPoint[header.numPoints];
		int lasFormat = header.pointDataFormat;

		byte[] bytes = new byte[header.pointDataLength];
		file.Position = header.offsetToData;

		// TODO: Chunk reading into e.g 4KB per read

		for (int i = 0; i < lasPoints.Length; i++)
			{
			file.Read(bytes, 0, (int)header.pointDataLength);
			if (lasFormat == 0) lasPoints[i] = getLasPointV0(bytes);
			else if (lasFormat == 1) lasPoints[i] = getLasPointV1(bytes);
			else if (lasFormat == 2) lasPoints[i] = getLasPointV2(bytes);
			else if (lasFormat == 3) lasPoints[i] = getLasPointV3(bytes);
			}

		for (int i = 0; i < lasPoints.Length; i++)
			{
			lasPoints[i].xyz.x *= (float)header.xScaleFactor;
			lasPoints[i].xyz.y *= (float)header.yScaleFactor;
			lasPoints[i].xyz.z *= (float)header.zScaleFactor;
			}

		return lasPoints;
		}

	private static LasPoint getLasPointV0(byte[] bytes)
		{
		LasPoint ret = new LasPoint();
		ret.xyz = new UnityEngine.Vector3(
			BitConverter.ToInt32(bytes, 0),
			BitConverter.ToInt32(bytes, 4),
			BitConverter.ToInt32(bytes, 8)
			);
		ret.instensity = BitConverter.ToUInt16(bytes, 12);

		ret.classification = bytes[15];
		ret.scanAngleRank = bytes[16];
		ret.userData = bytes[17];
		ret.pointSourceId = BitConverter.ToUInt16(bytes, 18);

		return ret;
		}

	private static LasPoint getLasPointV1(byte[] bytes)
		{
		LasPoint ret = new LasPoint();
		ret.xyz = new UnityEngine.Vector3(
			BitConverter.ToInt32(bytes, 0),
			BitConverter.ToInt32(bytes, 4),
			BitConverter.ToInt32(bytes, 8)
			);
		ret.instensity = BitConverter.ToUInt16(bytes, 12);

		ret.classification = bytes[15];
		ret.scanAngleRank = bytes[16];
		ret.userData = bytes[17];
		ret.pointSourceId = BitConverter.ToUInt16(bytes, 18);
		ret.GPSTime = BitConverter.ToDouble(bytes, 20);

		return ret;
		}

	private static LasPoint getLasPointV2(byte[] bytes)
		{
		LasPoint point = new LasPoint();
		point.xyz = new UnityEngine.Vector3(
			BitConverter.ToInt32(bytes, 0),
			BitConverter.ToInt32(bytes, 4),
			BitConverter.ToInt32(bytes, 8)
			);
		point.instensity = BitConverter.ToUInt16(bytes, 12);

		point.classification = bytes[15];
		point.scanAngleRank = bytes[16];
		point.userData = bytes[17];
		point.pointSourceId = BitConverter.ToUInt16(bytes, 18);

		point.col = new UnityEngine.Color(
			BitConverter.ToUInt16(bytes, 20),
			BitConverter.ToUInt16(bytes, 22),
			BitConverter.ToUInt16(bytes, 24)
			);

		return point;
		}

	private static LasPoint getLasPointV3(byte[] bytes)
		{
		LasPoint point = new LasPoint();
		point.xyz = new UnityEngine.Vector3(
			BitConverter.ToInt32(bytes, 0),
			BitConverter.ToInt32(bytes, 4),
			BitConverter.ToInt32(bytes, 8)
			);
		point.instensity = BitConverter.ToUInt16(bytes, 12);

		point.classification = bytes[15];
		point.scanAngleRank = bytes[16];
		point.userData = bytes[17];
		point.pointSourceId = BitConverter.ToUInt16(bytes, 18);
		point.GPSTime = BitConverter.ToDouble(bytes, 20);

		point.col = new UnityEngine.Color(
			BitConverter.ToUInt16(bytes, 28),
			BitConverter.ToUInt16(bytes, 30),
			BitConverter.ToUInt16(bytes, 32)
			);

		return point;
		}
	}
