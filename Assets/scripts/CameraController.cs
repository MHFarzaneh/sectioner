//using System;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using UnityEngine.UI;

//using Random = System.Random;

public class CameraController : MonoBehaviour
{
	[SerializeField] float speed = 0.5f;
	[SerializeField] float sensitivity = 1.0f;

	Camera cam;
	Vector3 anchorPoint;
	Quaternion anchorRot;

	TransformData td;
	public GameObject m_mark;
	public GameObject m_normal;
	public GameObject m_sectionNormal;
	public float distanceBetweenBorderNormals = 0.2f;
	public Button buttonWrite, buttonUndo, buttonResetCam, buttonFinishSection;
	public bool pyramidMode = false;
	public GameObject pyramid;
	//IEnumerator coroutine;

	Color m_newColor;
	private List<GameObject> m_currentNormals = new List<GameObject>();
	private struct Section
	{
		public GameObject pyramid;
		public List<GameObject> borders;
		public GameObject normal;
	}

	private List<Section> m_AllSections = new List<Section>();

	// Use this for initialization
	void Start () {
		m_newColor = new Color(Random.value, Random.value, Random.value, 0.1f);
		buttonWrite.onClick.AddListener(WriteToFile);
		buttonFinishSection.onClick.AddListener(CloseSection);
		buttonUndo.onClick.AddListener(RemovePreviousSection);
		buttonResetCam.onClick.AddListener(ResetCamera);
		td = transform.Clone();
	}

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	void WriteToFile()
	{
		// Write the string array to a new file named "WriteLines.txt".
		using (StreamWriter outputFile = new StreamWriter( "WriteLines.txt"))
		{
			foreach (var s in m_AllSections)
			{
				var p= s.normal.transform.position;
				string line = p.x +" "+ p.y+" "+p.z;
				outputFile.WriteLine(line);
			}
		}
	}

	private void DrawSectionBorder()
	{
		RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if ( Physics.Raycast (ray,out hit,100.0f)) {
	        if (!m_currentNormals.Any() || Vector3.Distance(m_currentNormals.Last().transform.position,hit.point)>distanceBetweenBorderNormals)
	        {
		        //Instantiate(m_mark, tran, Quaternion.identity);
                var normal =Instantiate(m_normal, hit.point, Quaternion.identity);
                normal.GetComponentInChildren<Renderer>().material.color = m_newColor;
                m_currentNormals.Add(normal);
                normal.transform.up = hit.normal;
	        }
        }
	}

	private void PyramidUpdate()
	{
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if ( Physics.Raycast (ray,out hit,100.0f))
		{
			pyramid.transform.position = hit.point;
			pyramid.transform.up = hit.normal;
		}
	}

	private void CloseSection()
	{
		var sectionPosition = new Vector3(0,0,0);
        var sectionUp = new Vector3(0,0,0);
        float i = 0;
        foreach (var n in m_currentNormals)
        {
        	sectionPosition += n.transform.position;
        	sectionUp += n.transform.up;
            i++;
        }

        sectionPosition /= i;
        sectionUp /= i;
        sectionUp.Normalize();

        GameObject tf = new GameObject();
        tf.transform.position = sectionPosition;
        tf.transform.up = sectionUp;

        var normal = Instantiate(m_sectionNormal, tf.transform);
        normal.GetComponentInChildren<Renderer>().material.color = m_newColor;

        Section s = new Section();
        s.normal = normal;
        s.borders = new List<GameObject>(m_currentNormals);

        m_AllSections.Add(s);

        m_currentNormals.Clear();

        // pick a random color
        m_newColor = new Color(Random.value, Random.value, Random.value, 1.0f);
	}


	private void ClosePyramidSection()
	{
		var dummyPyramid = new GameObject();

		dummyPyramid.transform.position = pyramid.transform.position;
		dummyPyramid.transform.up = pyramid.transform.up;

		var normal = Instantiate(m_sectionNormal,dummyPyramid.transform);
		var pyramidInstance = Instantiate(pyramid);

		normal.GetComponentInChildren<Renderer>().material.color = m_newColor;
		pyramidInstance.GetComponentInChildren<Renderer>().material.color = m_newColor;

		Section s = new Section();
		s.normal = normal;
		s.borders = new List<GameObject>(m_currentNormals);
		s.pyramid = pyramidInstance;

		m_AllSections.Add(s);

		m_currentNormals.Clear();

		// pick a random color
		m_newColor = new Color(Random.value, Random.value, Random.value,0.1f);
	}

	void RemovePreviousSection()
	{
		var section = m_AllSections.ElementAt(m_AllSections.Count-1);
		foreach (var b in section.borders)
		{
			Destroy(b);
		}

		Destroy(section.normal);
		m_AllSections.RemoveAt(m_AllSections.Count - 1);
	}

	void ResetCamera()
	{
		print("Reset camera to home");
		transform.position = td.position;
		transform.rotation = td.rotation;
	}

	void Update()
	{
		if (pyramidMode)
		{
			// update pyramid on body
			PyramidUpdate();

			// create section
			if (Input.GetMouseButtonUp(0))
			{
				ClosePyramidSection();
			}
		}
		else
		{
			// add border normal on click
			if (Input.GetMouseButton(0))
            {
            	DrawSectionBorder();
            }

			// Close section
            if (Input.GetKeyUp(KeyCode.Space))
            {
            	CloseSection();
            }
		}

		// Rest scene view
        if (Input.GetKey(KeyCode.R))
        {
        	ResetCamera();
        }

        // remove previous section
		if (Input.GetKeyUp(KeyCode.F))
		{
			RemovePreviousSection();
		}

		// write to file
		if (Input.GetKeyUp(KeyCode.O))
		{
			WriteToFile();
		}
	}

	void FixedUpdate()
	{
		Vector3 move = Vector3.zero;
		if(Input.GetKey(KeyCode.W))
			move += Vector3.forward * speed;
		if (Input.GetKey(KeyCode.S))
			move -= Vector3.forward * speed;
		if (Input.GetKey(KeyCode.D))
			move += Vector3.right * speed;
		if (Input.GetKey(KeyCode.A))
			move -= Vector3.right * speed;
		if (Input.GetKey(KeyCode.E))
			move += Vector3.up * speed;
		if (Input.GetKey(KeyCode.Q))
			move -= Vector3.up * speed;
		transform.Translate(move);

		if (Input.GetMouseButtonDown(1))
		{
			anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
			anchorRot = transform.rotation;
		}
		if (Input.GetMouseButton(1))
		{
			Quaternion rot = anchorRot;
			Vector3 dif = anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
			rot.eulerAngles += dif * sensitivity;
			transform.rotation = rot;
		}
	}
}

public struct TransformData
{
	public Vector3 position;
	public Quaternion rotation;

	public Vector3 localPosition;
	public Vector3 localScale;
	public Quaternion localRotation;

	public Transform parent;
}

public static class TransformUtils {

	public static TransformData Clone( this Transform transform )
	{
		TransformData td = new TransformData();

		td.position = transform.position;
		td.rotation = transform.rotation;
		td.localPosition = transform.localPosition;

		td.rotation = transform.rotation;
		td.localRotation = transform.localRotation;

		td.localScale = transform.localScale;

		td.parent = transform.parent;

		return td;
	}

}

