using System.IO;
using UnityEngine;

public class LasFile
	{
	public string fullFileName;
	public LasHeader header;
	public LasPoint[] points;
	}

// LAS header las version 1.2
// Specification in http://www.asprs.org/wp-content/uploads/2010/12/asprs_las_format_v12.pdf
// Uses byte instead of char as char is 2 bytes long
public struct LasHeader
	{
	public byte[] fileSignature; // 0
	public ushort fileSourceId; // 4
	public ushort globalEncoding; // 6
	public uint projectId1; // 8
	public ushort projectId2; // 12
	public ushort projectId3; // 14
	public byte[] projectId4; // 16
	public byte versionMajor; // 24
	public byte versionMinor; // 25
	public byte[] systemIdentifier; // 26
	public byte[] generatingSoftware; // 58
	public ushort fileCreationDay; // 90
	public ushort fileCreationYear; // 92
	public ushort headerSize; // 94
	public uint offsetToData; // 96
	public uint numVariableLengthRecords; // 100
	public byte pointDataFormat; // 104
	public ushort pointDataLength; // 105
	public uint numPoints; // 107
	public uint[] numPointsByReturn; // 111
	public double xScaleFactor; // 128
	public double yScaleFactor; // 136
	public double zScaleFactor; // 144
	public double xOffset; // 152
	public double yOffset; // 160
	public double zOffset; // 168
	public double maxX; // 176
	public double minX; // 184
	public double maxY; // 192
	public double minY; // 200
	public double maxZ; // 208
	public double minZ; // 216
	}

public struct LasPoint
	{
	public Vector3 xyz;
	public Color col;
	public ushort instensity;
	public byte returnNumber; // 3 bits
	public byte numberOfReturns; // 3 bits
	public byte scanDirectionFlag; // bit
	public byte edgeOfFlightLine; // bit
	public byte scanAngleRank; // -90 to + 90, should be an uchar
	public byte classification;
	public byte userData;
	public ushort pointSourceId;
	public double GPSTime;
	}
