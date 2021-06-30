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
	public Button buttonWrite, buttonUndo, buttonResetCam, buttonFinishSection, buttonQuit;
	public Toggle toggleRectangleMode, toggleDoubleCamMode;
	public InputField inputHeight, inputWidth, inputLength, inputCamHeight, inputCamWidth, inputCamLength, inputCamOverlap, inputCamDoubleAngle;
	public bool rectangleMode = true, doubleCamMode;
	public GameObject rectangle;
	public GameObject pyramid;
	public float m_collisionDistance = 0.2f;
	//IEnumerator coroutine;

	Color m_newColor;
	private List<GameObject> m_currentNormals = new List<GameObject>();
	bool isRectangleOnPlane = false;
	float heigthRectangle=1f, widthRectangle=1f, lengthRectangle=1f, heigthCam=0.5f, widthCam=0.3f, lengthCam=0.3f, overlapCam = 1f, doubleAngleCam = 10f;
	private struct Section
	{
		public GameObject rectangle;
		public List<GameObject> borders;
		public GameObject normal;
		public List<Transform> camPoses;
	}

	private List<Section> m_AllSections = new List<Section>();

	// Use this for initialization
	void Start () {
		m_newColor = new Color(Random.value, Random.value, Random.value, 0.1f);
		buttonWrite.onClick.AddListener(WriteToFile);
		buttonFinishSection.onClick.AddListener(CloseSection);
		buttonUndo.onClick.AddListener(RemovePreviousSection);
		buttonResetCam.onClick.AddListener(ResetCamera);
		buttonQuit.onClick.AddListener(Quit);
		toggleRectangleMode.onValueChanged.AddListener(ChangeRectangleMode);
		toggleDoubleCamMode.onValueChanged.AddListener(ChangeDoubleCamMode);
		inputHeight.onValueChanged.AddListener(ChangeRectangleHeight);
		inputWidth.onValueChanged.AddListener(ChangeRectangleWidth);
		inputLength.onValueChanged.AddListener(ChangeRectangleLength);
		inputCamHeight.onValueChanged.AddListener(ChangeCamHeight);
		inputCamWidth.onValueChanged.AddListener(ChangeCamWidth);
		inputCamLength.onValueChanged.AddListener(ChangeCamLength);
		inputCamOverlap.onValueChanged.AddListener(ChangeCamOverlap);
		inputCamDoubleAngle.onValueChanged.AddListener(ChangeCamDoubleAngle);
		td = transform.Clone();
		rectangleMode = true;
	}

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	void ChangeCamLength(string l)
	{
		//pyramid.transform.localScale =new Vector3(pyramid.transform.localScale.x,pyramid.transform.localScale.y,Convert.ToSingle(l));
		lengthCam = Convert.ToSingle(l);
	}
	void ChangeCamHeight(string h)
	{
		//pyramid.transform.localScale = new Vector3(pyramid.transform.localScale.x,Convert.ToSingle(h),pyramid.transform.localScale.z);
		heigthCam = Convert.ToSingle(h);
	}

	void ChangeCamWidth(string w)
	{
		//pyramid.transform.localScale = new Vector3(Convert.ToSingle(w),pyramid.transform.localScale.y,pyramid.transform.localScale.z);
		widthCam = Convert.ToSingle(w);
	}

	void ChangeCamOverlap(string o)
	{
		overlapCam = Convert.ToSingle(o);
	}

	void ChangeCamDoubleAngle(string a)
	{
		doubleAngleCam = Convert.ToSingle(a);
	}

	void ChangeRectangleLength(string l)
	{
		rectangle.transform.localScale =new Vector3(rectangle.transform.localScale.x,rectangle.transform.localScale.y,Convert.ToSingle(l));
		lengthRectangle = Convert.ToSingle(l);
	}
	void ChangeRectangleHeight(string h)
	{
		rectangle.transform.localScale = new Vector3(rectangle.transform.localScale.x,Convert.ToSingle(h),rectangle.transform.localScale.z);
		heigthRectangle = Convert.ToSingle(h);
	}

	void ChangeRectangleWidth(string w)
	{
		rectangle.transform.localScale = new Vector3(Convert.ToSingle(w),rectangle.transform.localScale.y,rectangle.transform.localScale.z);
		widthRectangle = Convert.ToSingle(w);
	}

	void RotatePlus()
	{
		rectangle.transform.Rotate(Vector3.up * 5);
	}


	void RotateMinus()
	{
		rectangle.transform.Rotate(Vector3.up * -5);
	}

	void ChangeRectangleMode(bool mode)
	{
		rectangleMode = mode;
	}

	void ChangeDoubleCamMode(bool mode)
	{
		doubleCamMode = mode;
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

	private void RectangleUpdate()
	{
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if ( Physics.Raycast (ray,out hit,100.0f))
		{
			rectangle.transform.position = hit.point;
			rectangle.transform.rotation = Quaternion.FromToRotation(rectangle.transform.up, hit.normal) * rectangle.transform.rotation;
			if (Input.GetKeyUp(KeyCode.Alpha1)) RotatePlus();
			if (Input.GetKeyUp(KeyCode.Alpha2)) RotateMinus();

			isRectangleOnPlane = true;
		}
		else
		{
			rectangle.transform.position = Vector3.zero;
			isRectangleOnPlane = false;
		}
	}

	void GenerateCamPoses(Section s)
	{
		//pyramid.transform.localScale = new Vector3(lengthCam, widthCam, heigthCam);
		var recScale = s.rectangle.transform.lossyScale;
		for (float l = -lengthRectangle / (recScale.z*2f); l < lengthRectangle / (recScale.z*2f); l = l + (lengthCam/recScale.z)/overlapCam)
		{
			for (float w = -widthRectangle / (recScale.x*2f); w < widthRectangle / (recScale.x*2f); w = w + (widthCam/recScale.x)/overlapCam)
			{
				var camPose = Instantiate(pyramid, s.rectangle.transform);
				camPose.transform.localPosition = new Vector3(w, 0,l);
				var camScale = camPose.transform.localScale;
				camPose.transform.localScale = new Vector3((camScale.x*widthCam)/(recScale.x*0.3f),
					(camScale.y*heigthCam)/(recScale.y*0.5f), (camScale.z*lengthCam)/(recScale.z*0.3f));
				camPose.GetComponentInChildren<Renderer>().material.color = m_newColor;

				// Double cam mode
				if (doubleCamMode)
				{
					camPose.transform.rotation *= Quaternion.AngleAxis(doubleAngleCam, camPose.transform.up);

					var camPose2 = Instantiate(pyramid, s.rectangle.transform);
					camPose2.transform.localPosition = new Vector3(w, 0,l);
					var camScale2 = camPose2.transform.localScale;
					camPose2.transform.localScale = new Vector3((camScale.x*widthCam)/(recScale.x*0.3f),
						(camScale.y*heigthCam)/(recScale.y*0.5f), (camScale.z*lengthCam)/(recScale.z*0.3f));
					camPose2.GetComponentInChildren<Renderer>().material.color = m_newColor;

					camPose2.transform.rotation *= Quaternion.AngleAxis(-doubleAngleCam, camPose2.transform.up);

					// Check collision
					var sphereCenter2 = camPose2.transform.position+camPose2.transform.up*heigthCam;
					//Debug.DrawLine(sphereCenter,camPose.transform.position, Color.green, 500f);
					Collider[] hitColliders2 = Physics.OverlapSphere(sphereCenter2, heigthCam*0.8f);
					if (hitColliders2.Length>0)
					{
						Destroy(camPose2);
					}
					else // check if cam is generated on empty space
					{
						hitColliders2 = Physics.OverlapSphere(sphereCenter2, heigthCam*1.1f);
						if (hitColliders2.Length==0)
						{
							Destroy(camPose);
						}
					}
				}
				// Check collision
				var sphereCenter = camPose.transform.position+camPose.transform.up*heigthCam;
				//Debug.DrawLine(sphereCenter,camPose.transform.position, Color.green, 500f);
				Collider[] hitColliders = Physics.OverlapSphere(sphereCenter, heigthCam*0.8f);
				if (hitColliders.Length>0)
				{
					Destroy(camPose);
				}
				else // check if cam is generated on empty space
				{
					hitColliders = Physics.OverlapSphere(sphereCenter, heigthCam*1.1f);
					if (hitColliders.Length==0)
					{
						Destroy(camPose);
					}
				}
			}
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
        GenerateCamPoses(s);

        m_AllSections.Add(s);

        m_currentNormals.Clear();

        // pick a random color
        m_newColor = new Color(Random.value, Random.value, Random.value, 1.0f);

	}

	private void CloseRectangleSection()
	{
		// pick a random color
		m_newColor = new Color(Random.value, Random.value, Random.value,0.1f);

		if (!isRectangleOnPlane) return;
		var dummyRectangle = new GameObject();

		dummyRectangle.transform.position = rectangle.transform.position;
		dummyRectangle.transform.up = rectangle.transform.up;

		var normal = Instantiate(m_sectionNormal,dummyRectangle.transform);
		var rectangleInstance = Instantiate(rectangle);

		normal.GetComponentInChildren<Renderer>().material.color = m_newColor;
		rectangleInstance.GetComponentInChildren<Renderer>().material.color = m_newColor;

		Section s = new Section();
		s.normal = normal;
		s.borders = new List<GameObject>(m_currentNormals);
		s.rectangle = rectangleInstance;
		GenerateCamPoses(s);

		m_AllSections.Add(s);

		m_currentNormals.Clear();
	}

	void RemovePreviousSection()
	{
		var section = m_AllSections.ElementAt(m_AllSections.Count-1);
		foreach (var b in section.borders)
		{
			Destroy(b);
		}

		Destroy(section.normal);
		Destroy(section.rectangle);
		m_AllSections.RemoveAt(m_AllSections.Count - 1);
	}

	void ResetCamera()
	{
		print("Reset camera to home");
		transform.position = td.position;
		transform.rotation = td.rotation;
	}

	void Quit()
	{
		Application.Quit();
	}

	void Update()
	{
		if (Input.GetKey("escape"))
		{
			Application.Quit();
		}

		if (rectangleMode)
		{
			// update rectangle on body
			RectangleUpdate();

			// create section
			if (Input.GetMouseButtonUp(0))
			{
				CloseRectangleSection();
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

