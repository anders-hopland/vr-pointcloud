using System.IO;
using System;
using System.Globalization;

public class LasReader
	{
	/// <summary>
	/// Returns a struct with all relevant data from las file
	/// </summary>
	/// <param name="fileName"></param>
	/// <returns></returns>
	public static LasFile readLASFile(string fileName)
		{
		LasFile lasFile = new LasFile();
		if (!File.Exists(fileName)) return lasFile;
		FileStream fs = File.Open(fileName, FileMode.Open);
		lasFile.fullFileName = fileName;
		LasHeader header = readLasHeader(fs);
		LasPoint[] lasPoints = readLasPoints(fs, header);

		lasFile.header = header;
		lasFile.points = lasPoints;
		fs.Close();

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

		return header;
		}

	private static LasPoint[] readLasPoints(FileStream file, LasHeader header)
		{
		LasPoint[] lasPoints = new LasPoint[header.numPoints];
		int lasFormat = header.pointDataFormat;

		byte[] bytebuf;
		int numToRead = 500;
		if (lasPoints.Length > numToRead)
			bytebuf = new byte[header.pointDataLength * numToRead];
		else
			bytebuf = new byte[header.pointDataLength * lasPoints.Length];
		file.Position = header.offsetToData;

		byte[] bytes = new byte[header.pointDataLength];

		for (int i = 0; i < lasPoints.Length; i++)
			{
			if (i % numToRead == 0)
				file.Read(bytebuf, 0, Math.Min(numToRead, lasPoints.Length - i) * header.pointDataLength);

			Array.Copy(bytebuf, header.pointDataLength * (i % numToRead), bytes, 0, header.pointDataLength);

			if (lasFormat == 0) lasPoints[i] = getLasPointV0(bytes);
			else if (lasFormat == 1) lasPoints[i] = getLasPointV1(bytes);
			else if (lasFormat == 2) lasPoints[i] = getLasPointV2(bytes);
			else if (lasFormat == 3) lasPoints[i] = getLasPointV3(bytes);
			}

		for (int i = 0; i < lasPoints.Length; i++)
			{
			lasPoints[i].xyz.x *= (float)header.xScaleFactor;
			lasPoints[i].xyz.x += (float)header.xOffset;
			lasPoints[i].xyz.y *= (float)header.yScaleFactor;
			lasPoints[i].xyz.y += (float)header.yOffset;
			lasPoints[i].xyz.z *= (float)header.zScaleFactor;
			lasPoints[i].xyz.z += (float)header.zOffset;
			}

		return lasPoints;
		}

	private static LasPoint getLasPointV0(byte[] bytes)
		{
		LasPoint point = new LasPoint();
		point.xyz = new UnityEngine.Vector3(
			BitConverter.ToInt32(bytes, 0),
			BitConverter.ToInt32(bytes, 4),
			BitConverter.ToInt32(bytes, 8)
			);
		point.instensity = BitConverter.ToUInt16(bytes, 12);
		point.returnNumber = (byte)((int)bytes[14] & 0x7);
		point.numberOfReturns = (byte)(((int)bytes[14] >> 3) & 0x7);
		point.scanDirectionFlag = (byte)(((int)bytes[14] >> 6) & 0x1);
		point.edgeOfFlightLine = (byte)(((int)bytes[14] >> 7) & 0x1);
		point.classification = bytes[15];
		point.scanAngleRank = bytes[16];
		point.userData = bytes[17];
		point.pointSourceId = BitConverter.ToUInt16(bytes, 18);

		return point;
		}

	private static LasPoint getLasPointV1(byte[] bytes)
		{
		LasPoint point = new LasPoint();
		point.xyz = new UnityEngine.Vector3(
			BitConverter.ToInt32(bytes, 0),
			BitConverter.ToInt32(bytes, 4),
			BitConverter.ToInt32(bytes, 8)
			);
		point.instensity = BitConverter.ToUInt16(bytes, 12);
		point.returnNumber = (byte)((int)bytes[14] & 0x7);
		point.numberOfReturns = (byte)(((int)bytes[14] >> 3) & 0x7);
		point.scanDirectionFlag = (byte)(((int)bytes[14] >> 6) & 0x1);
		point.edgeOfFlightLine = (byte)(((int)bytes[14] >> 7) & 0x1);
		point.classification = bytes[15];
		point.scanAngleRank = bytes[16];
		point.userData = bytes[17];
		point.pointSourceId = BitConverter.ToUInt16(bytes, 18);
		point.GPSTime = BitConverter.ToDouble(bytes, 20);

		return point;
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
		point.returnNumber = (byte)((int)bytes[14] & 0x7);
		point.numberOfReturns = (byte)(((int)bytes[14] >> 3) & 0x7);
		point.scanDirectionFlag = (byte)(((int)bytes[14] >> 6) & 0x1);
		point.edgeOfFlightLine = (byte)(((int)bytes[14] >> 7) & 0x1);
		point.classification = bytes[15];
		point.scanAngleRank = bytes[16];
		point.userData = bytes[17];
		point.pointSourceId = BitConverter.ToUInt16(bytes, 18);

		point.col = new UnityEngine.Color(
			BitConverter.ToUInt16(bytes, 20) / (256f * 256f),
			BitConverter.ToUInt16(bytes, 22) / (256f * 256f),
			BitConverter.ToUInt16(bytes, 24) / (256f * 256f)
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
		point.returnNumber = (byte)((int)bytes[14] & 0x7);
		point.numberOfReturns = (byte)(((int)bytes[14] >> 3) & 0x7);
		point.scanDirectionFlag = (byte)(((int)bytes[14] >> 6) & 0x1);
		point.edgeOfFlightLine = (byte)(((int)bytes[14] >> 7) & 0x1);
		point.classification = bytes[15];
		point.scanAngleRank = bytes[16];
		point.userData = bytes[17];
		point.pointSourceId = BitConverter.ToUInt16(bytes, 18);
		point.GPSTime = BitConverter.ToDouble(bytes, 20);


		point.col = new UnityEngine.Color(
			BitConverter.ToUInt16(bytes, 28) / (256f * 256f),
			BitConverter.ToUInt16(bytes, 30) / (256f * 256f),
			BitConverter.ToUInt16(bytes, 32) / (256f * 256f)
			);

		return point;
		}
	}
