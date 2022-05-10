using DataStructures.ViliWonka.KDTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PointCloudManager : MonoBehaviour
    {
    internal MeshFilter mf;
    internal MeshRenderer mr;
    internal PointCloudObject[] files;
    internal int curtFileIx;
    internal bool displayNormals;
    internal bool displayRoundPoints;
    internal float displayRadius = 0.1f;
    internal float imageCylRad = -1f;
    internal bool initialized;

    void Start ()
        {
        init ();
        }

    void Update ()
        {

        }

    public static PointCloudManager newPointCloudObject (PointCloudObject[] pointClouds)
        {
        var go = new GameObject ();
        go.name = "PointCloud";
        go.AddComponent<PointCloudManager> ();
        go.AddComponent<MeshFilter> ();
        go.AddComponent<MeshRenderer> ();
        go.GetComponent<MeshRenderer> ().material = Resources.Load ("Materials/PointCloudMat") as Material;
        go.transform.parent = StartScript.sceneRoot.transform;
        var display = go.GetComponent<PointCloudManager> ();
        display.displayNormals = StartScript.displayNormals;
        display.setFile (pointClouds);

        StartScript.sceneFloor.transform.position = new Vector3 (
            (float)(pointClouds[0].file.header.maxX + pointClouds[0].file.header.minX) / 2f,
            (float)(pointClouds[0].file.header.maxY + pointClouds[0].file.header.minY) / 2f,
            (float)pointClouds[0].file.header.minZ
            );

        return display;
        }

    internal void init ()
        {
        if (initialized) return;

        mf = this.GetComponent<MeshFilter> ();
        mr = this.GetComponent<MeshRenderer> ();

        initialized = true;
        }

    public void setFile (PointCloudObject[] files)
        {
        if (files.Length == 0)
            {
            throw new System.Exception ("setfile length is 0");
            }

        this.files = files;
        setPointCloud (this.files[0]);
        }

    // Work arrays for pushing data to GPU
    internal static Vector3[] verts;
    internal static int[] tris;
    internal static int[] indices;
    internal void setPointCloud (PointCloudObject pc)
        {
        init ();

        verts = new Vector3[pc.file.points.Length * 4];
        tris = new int[pc.file.points.Length * 6];

        for (int i = 0; i < pc.file.points.Length; i++)
            {
            verts[i * 4] = pc.file.points[i].xyz;
            verts[i * 4 + 1] = pc.file.points[i].xyz;
            verts[i * 4 + 2] = pc.file.points[i].xyz;
            verts[i * 4 + 3] = pc.file.points[i].xyz;
            }

        for (int i = 0; i < pc.file.points.Length; i++)
            {
            tris[i * 6] = i * 4 + 1;
            tris[i * 6 + 1] = i * 4 + 2;
            tris[i * 6 + 2] = i * 4 + 0;
            tris[i * 6 + 3] = i * 4 + 1;
            tris[i * 6 + 4] = i * 4 + 3;
            tris[i * 6 + 5] = i * 4 + 2;
            }

        setMatDefaults ();
        mf.mesh.indexFormat = IndexFormat.UInt32;
        mf.mesh.SetVertices (verts, 0, pc.file.points.Length * 4);
        mf.mesh.SetTriangles (tris, 0);

        StartScript.imageCyl.transform.parent = this.transform;
        StartScript.imageCyl.transform.localPosition = new Vector3(0, 0, -1);
        StartScript.imageCyl.transform.localRotation = Quaternion.identity;
        if (pc.imageTex != null)
            {
            StartScript.imageCyl.GetComponent<MeshRenderer> ().material.mainTexture = pc.imageTex;
            }
        else
            StartScript.imageCyl.SetActive (false);

        int cbOffset = ComputeBufferManager.setPointCloud (pc, displayNormals);
        setMatCbOffset (cbOffset);

        StartScript.ui.updateStatistics (Path.GetFileName (pc.file.fullFileName), pc.file.points.Length);
        }

    internal void setMatDefaults ()
        {
        setMatDisplayRad (displayRadius);
        setMatDisplayNormals (displayNormals);
        setMatEditLayer (0);
        }

    internal void uploadPointNormals()
        {
        if (files[curtFileIx].normals == null)
            files[curtFileIx].calcNormals ();

        ComputeBufferManager.uploadNormal (files[curtFileIx]);
        }

    /// <summary>
    /// Sets offset for compute buffers
    /// </summary>
    /// <param name="offset"></param>
    internal void setMatCbOffset (int offset)
        {
        if (mr == null) return;
        mr.material.SetFloat ("_CbOffset", offset);
        }
    internal void setMatEditLayer (int layer)
        {
        if (mr == null) return;
        mr.material.SetInt ("_CurLabel", layer);
        }
    internal void setMatDisplayMode (int displaymode)
        {
        if (mr == null) return;
        mr.material.SetInt ("_Displaymode", displaymode);
        }
    internal void setMatEditPos (Vector3 pos)
        {
        if (mr == null) return;
        mr.material.SetVector ("_EditPos", pos);
        }
    internal void setMatEditRad (float rad)
        {
        if (mr == null) return;
        mr.material.SetFloat ("_EditRadius", rad * (1f / StartScript.sceneRootScale));
        }
    internal void setMatDisplayRad (float rad)
        {
        if (mr == null) return;
        displayRadius = rad;
        mr.material.SetFloat ("_DisplayRadius", displayRadius * (1 / StartScript.sceneRootScale));
        }
    internal void updateMatDisplayRad ()
        {
        if (mr == null) return;
        mr.material.SetFloat ("_DisplayRadius", displayRadius * StartScript.sceneRootScale);
        }

    private const float displayRadStepSize = 1.16f;
    internal void increaseDisplayRad ()
        {
        if (mr == null) return;
        displayRadius *= displayRadStepSize;
        mr.material.SetFloat ("_DisplayRadius", displayRadius);
        }
    internal void decreaseDisplayRad ()
        {
        if (imageCylRad < 0) imageCylRad = StartScript.imageCyl.transform.localScale.x;
        displayRadius *= (1f / displayRadStepSize);
        mr.material.SetFloat ("_DisplayRadius", displayRadius);
        }
    internal void increaseImageRad ()
        {
        if (imageCylRad < 0) imageCylRad = StartScript.imageCyl.transform.localScale.x;
        float prevRad = imageCylRad;
        imageCylRad *= displayRadStepSize;
        float radChange = imageCylRad - prevRad;
        StartScript.imageCyl.transform.localScale *= displayRadStepSize;
        var newImageCylPos = StartScript.imageCyl.transform.localPosition;
        newImageCylPos.z += radChange * 0.01f;
        StartScript.imageCyl.transform.localPosition = newImageCylPos;
        }
    internal void decreaseImageRad ()
        {
        if (imageCylRad < 0) imageCylRad = StartScript.imageCyl.transform.localScale.x;
        float prevRad = imageCylRad;
        imageCylRad *= (1f / displayRadStepSize);
        float radChange = prevRad - imageCylRad;
        StartScript.imageCyl.transform.localScale *= (1f / displayRadStepSize);
        var newImageCylPos = StartScript.imageCyl.transform.localPosition;
        newImageCylPos.z -= radChange * 0.01f;
        StartScript.imageCyl.transform.localPosition = newImageCylPos;
        }
    internal void setMatDisplayNormals (bool displayNormals)
        {
        if (mr == null) return;
        this.displayNormals = displayNormals;
        mr.material.SetFloat ("_DisplayNormals", displayNormals ? 1 : 0);
        }

    internal void setMatDisplayRoundPoints (bool displayRoundPoints)
        {
        if (mr == null) return;
        this.displayRoundPoints = displayRoundPoints;
        mr.material.SetFloat ("_DisplayRoundPoints", displayRoundPoints ? 1 : 0);
        }

    internal void setMatTriggerPress (bool press)
        {
        if (mr == null) return;
        mr.material.SetInt ("_TriggerPress", press ? 1 : 0);
        }

    internal void nextPointCloud ()
        {
        StartScript.ui.updateStatistics ("Point Cloud", files[curtFileIx].file.points.Length);
        if (curtFileIx >= files.Length - 1) return;
        curtFileIx++;
        setPointCloud (files[curtFileIx]);
        }

    internal void prevPointCloud ()
        {
        StartScript.ui.updateStatistics ("Point Cloud", files[curtFileIx].file.points.Length);
        if (curtFileIx <= 0) return;
        curtFileIx--;
        setPointCloud (files[curtFileIx]);
        }
    }
