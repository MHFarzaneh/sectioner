using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class import : MonoBehaviour
{
	public String meshPath;
	//public GameObject emptyPrefabWithMeshRenderer;
    // Start is called before the first frame update
    void Start()
    {
	    Mesh holderMesh = new Mesh();
	    FastObjImporter newMesh = new FastObjImporter();
	    holderMesh = newMesh.ImportFile(meshPath);

	    MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
	    MeshFilter filter = gameObject.AddComponent<MeshFilter>();
	    filter.mesh = holderMesh;
    }

    // Update is called once per frame
    void Update()
    {

    }
}


