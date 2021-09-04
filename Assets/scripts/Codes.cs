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
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

//using Random = System.Random;

public class Codes : MonoBehaviour
{
	[SerializeField] float speed = 0.5f;
	[SerializeField] float sensitivity = 1.0f;

	int m_Index = 0;
	public GameObject itemTemplate;
	public GameObject content;

	Camera m_Cam;
	Vector3 m_AnchorPoint;
	Quaternion m_AnchorRot;

	int m_IDCounter;

	TransformData m_Td;
	bool m_IsDownward = true, m_GORight = false;
	[FormerlySerializedAs("m_canvas")]
	public GameObject canvas;
	[FormerlySerializedAs("m_mark")]
	public GameObject mark;
	[FormerlySerializedAs("m_normal")]
	public GameObject normal;
	[FormerlySerializedAs("m_sectionNormal")]
	public GameObject sectionNormal;
	public float distanceBetweenBorderNormals = 0.2f;
	public Button buttonWrite, buttonUndo, buttonResetCam, buttonFinishSection, buttonAutoSec, buttonRemoveAll, buttonQuit;
	public Toggle toggleRectangleMode, toggleDoubleCamMode, toggleDeleteBySelectionMode;
	public InputField inputHeight, inputWidth, inputLength, inputCamHeight, inputCamWidth, inputCamLength, inputCamOverlap, inputCamDoubleAngle;
	public bool rectangleMode = true, deleteSectionByselectionMode = false, doubleCamMode;
	public GameObject rectangle;
	public GameObject pyramid;
	[FormerlySerializedAs("m_collisionDistance")]
	public float collisionDistance = 0.2f;
	//IEnumerator coroutine;

	Color m_NewColor;
	private Order m_Order = new Order();
	private List<GameObject> m_CurrentNormals = new List<GameObject>();
	bool m_IsRectangleOnPlane = false;
	float m_HeigthRectangle=1f, m_WidthRectangle=1f, m_LengthRectangle=1f, m_HeigthCam=0.5f, m_WidthCam=0.3f, m_LengthCam=0.3f, m_OverlapCam = 1f, m_DoubleAngleCam = 10f;
	public struct Section
	{
		public GameObject rectangle;
		public List<GameObject> borders;
		public GameObject normal;
		public List<Transform> camPoses;
		public int id;
	}

	private struct Order
	{
		public List<Section> sections;
	}

	public List<Section> allSections = new List<Section>();

	// Use this for initialization
	void Start ()
	{
		m_IDCounter = 0;
		m_NewColor = new Color(Random.value, Random.value, Random.value, 0.1f);
		buttonWrite.onClick.AddListener(WriteToFile);
		buttonFinishSection.onClick.AddListener(CloseSection);
		buttonUndo.onClick.AddListener(RemovePreviousSection);
		buttonResetCam.onClick.AddListener(ResetCamera);
		buttonQuit.onClick.AddListener(Quit);
		buttonRemoveAll.onClick.AddListener(RemoveAll);
		buttonAutoSec.onClick.AddListener(AutoSec);
		toggleRectangleMode.onValueChanged.AddListener(ChangeRectangleMode);
		toggleDeleteBySelectionMode.onValueChanged.AddListener(ChangeDeletBySelectionMode);
		toggleDoubleCamMode.onValueChanged.AddListener(ChangeDoubleCamMode);
		inputHeight.onValueChanged.AddListener(ChangeRectangleHeight);
		inputWidth.onValueChanged.AddListener(ChangeRectangleWidth);
		inputLength.onValueChanged.AddListener(ChangeRectangleLength);
		inputCamHeight.onValueChanged.AddListener(ChangeCamHeight);
		inputCamWidth.onValueChanged.AddListener(ChangeCamWidth);
		inputCamLength.onValueChanged.AddListener(ChangeCamLength);
		inputCamOverlap.onValueChanged.AddListener(ChangeCamOverlap);
		inputCamDoubleAngle.onValueChanged.AddListener(ChangeCamDoubleAngle);
		m_Td = transform.Clone();
		rectangleMode = true;
	}

	private void Awake()
	{
		m_Cam = GetComponent<Camera>();
	}

	void ChangeCamLength(string l)
	{
		//pyramid.transform.localScale =new Vector3(pyramid.transform.localScale.x,pyramid.transform.localScale.y,Convert.ToSingle(l));
		m_LengthCam = Convert.ToSingle(l);
	}
	void ChangeCamHeight(string h)
	{
		//pyramid.transform.localScale = new Vector3(pyramid.transform.localScale.x,Convert.ToSingle(h),pyramid.transform.localScale.z);
		m_HeigthCam = Convert.ToSingle(h);
	}

	void ChangeCamWidth(string w)
	{
		//pyramid.transform.localScale = new Vector3(Convert.ToSingle(w),pyramid.transform.localScale.y,pyramid.transform.localScale.z);
		m_WidthCam = Convert.ToSingle(w);
	}

	void ChangeCamOverlap(string o)
	{
		m_OverlapCam = Convert.ToSingle(o);
	}

	void ChangeCamDoubleAngle(string a)
	{
		m_DoubleAngleCam = Convert.ToSingle(a);
	}

	void ChangeRectangleLength(string l)
	{
		rectangle.transform.localScale =new Vector3(rectangle.transform.localScale.x,rectangle.transform.localScale.y,Convert.ToSingle(l));
		m_LengthRectangle = Convert.ToSingle(l);
	}
	void ChangeRectangleHeight(string h)
	{
		rectangle.transform.localScale = new Vector3(rectangle.transform.localScale.x,Convert.ToSingle(h),rectangle.transform.localScale.z);
		m_HeigthRectangle = Convert.ToSingle(h);
	}

	void ChangeRectangleWidth(string w)
	{
		rectangle.transform.localScale = new Vector3(Convert.ToSingle(w),rectangle.transform.localScale.y,rectangle.transform.localScale.z);
		m_WidthRectangle = Convert.ToSingle(w);
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

	void ChangeDeletBySelectionMode(bool mode)
	{
		deleteSectionByselectionMode = mode;
		// add collider to all sections
		if (mode)
		{
			foreach (var s in allSections)
			{
				s.rectangle.AddComponent<BoxCollider>();
			}
		}
		else
		{
			foreach (var s in allSections)
			{
				Destroy(s.rectangle.GetComponent<BoxCollider>());
			}
		}
	}

	void ChangeDoubleCamMode(bool mode)
	{
		doubleCamMode = mode;
	}

	void AddToList(ref Section section)
	{
		var copy = Instantiate(itemTemplate);
		copy.transform.parent = content.transform;

		copy.GetComponentInChildren<Text>().text = section.id.ToString();
		int copyOfIndex = section.id;
		var section1 = section;
		copy.GetComponent<Button>().onClick.AddListener(
			() =>
			{
				HighlightSection(section1);
			});
	}

	UnityAction HighlightSection(Section section)
	{
		var initColor = section.rectangle.GetComponent<MeshRenderer>().material.color;
		StartCoroutine(ChangeColorCoroutine(section));
		section.rectangle.GetComponent<MeshRenderer>().material.color = Color.blue;

		Debug.Log("number "+section.id.ToString());
		return null;
	}

	IEnumerator ChangeColorCoroutine(Section section)
	{
		var initColor = section.rectangle.GetComponent<MeshRenderer>().material.color;
		section.rectangle.GetComponent<MeshRenderer>().material.color = Color.red;
		yield return new WaitForSeconds(1);
		section.rectangle.GetComponent<MeshRenderer>().material.color = initColor;
	}

	void WriteToFile()
	{
		// Write the string array to a new file named "WriteLines.txt".
		using (StreamWriter outputFile = new StreamWriter( "WriteLines.txt"))
		{
			foreach (var s in allSections)
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
	        if (!m_CurrentNormals.Any() || Vector3.Distance(m_CurrentNormals.Last().transform.position,hit.point)>distanceBetweenBorderNormals)
	        {
		        //Instantiate(m_mark, tran, Quaternion.identity);
                var normal =Instantiate(this.normal, hit.point, Quaternion.identity);
                normal.GetComponentInChildren<Renderer>().material.color = m_NewColor;
                m_CurrentNormals.Add(normal);
                normal.transform.up = hit.normal;
	        }
        }
	}

	private void RectangleUpdate(Ray ray)
	{
		RaycastHit hit;
		if ( Physics.Raycast (ray,out hit,100.0f))
		{
			rectangle.transform.position = hit.point;
			rectangle.transform.rotation = Quaternion.FromToRotation(rectangle.transform.up, hit.normal) * rectangle.transform.rotation;
			if (Input.GetKeyUp(KeyCode.Alpha1)) RotatePlus();
			if (Input.GetKeyUp(KeyCode.Alpha2)) RotateMinus();

			m_IsRectangleOnPlane = true;
		}
		else
		{
			rectangle.transform.position = Vector3.zero;
			m_IsRectangleOnPlane = false;
		}
	}

	void GenerateCamPoses(Section s)
	{
		//pyramid.transform.localScale = new Vector3(lengthCam, widthCam, heigthCam);
		var recScale = s.rectangle.transform.lossyScale;
		for (float l = -m_LengthRectangle / (recScale.z*2f); l < m_LengthRectangle / (recScale.z*2f); l = l + (m_LengthCam/recScale.z)/m_OverlapCam)
		{
			for (float w = -m_WidthRectangle / (recScale.x*2f); w < m_WidthRectangle / (recScale.x*2f); w = w + (m_WidthCam/recScale.x)/m_OverlapCam)
			{
				var camPose = Instantiate(pyramid, s.rectangle.transform);
				camPose.transform.localPosition = new Vector3(w, 0,l);
				var camScale = camPose.transform.localScale;
				camPose.transform.localScale = new Vector3((camScale.x*m_WidthCam)/(recScale.x*0.3f),
					(camScale.y*m_HeigthCam)/(recScale.y*0.5f), (camScale.z*m_LengthCam)/(recScale.z*0.3f));
				camPose.GetComponentInChildren<Renderer>().material.color = m_NewColor;

				// Double cam mode
				if (doubleCamMode)
				{
					camPose.transform.rotation *= Quaternion.AngleAxis(m_DoubleAngleCam, camPose.transform.up);

					var camPose2 = Instantiate(pyramid, s.rectangle.transform);
					camPose2.transform.localPosition = new Vector3(w, 0,l);
					var camScale2 = camPose2.transform.localScale;
					camPose2.transform.localScale = new Vector3((camScale.x*m_WidthCam)/(recScale.x*0.3f),
						(camScale.y*m_HeigthCam)/(recScale.y*0.5f), (camScale.z*m_LengthCam)/(recScale.z*0.3f));
					camPose2.GetComponentInChildren<Renderer>().material.color = m_NewColor;

					camPose2.transform.rotation *= Quaternion.AngleAxis(-m_DoubleAngleCam, camPose2.transform.up);

					// Check collision
					var sphereCenter2 = camPose2.transform.position+camPose2.transform.up*m_HeigthCam;
					//Debug.DrawLine(sphereCenter,camPose.transform.position, Color.green, 500f);
					Collider[] hitColliders2 = Physics.OverlapSphere(sphereCenter2, m_HeigthCam*0.8f);
					if (hitColliders2.Length>0)
					{
						Destroy(camPose2);
					}
					else // check if cam is generated on empty space
					{
						hitColliders2 = Physics.OverlapSphere(sphereCenter2, m_HeigthCam*1.3f);
						if (hitColliders2.Length==0)
						{
							Destroy(camPose);
						}
					}
				}
				// Check collision
				var sphereCenter = camPose.transform.position+camPose.transform.up*m_HeigthCam;
				//Debug.DrawLine(sphereCenter,camPose.transform.position, Color.green, 500f);
				Collider[] hitColliders = Physics.OverlapSphere(sphereCenter, m_HeigthCam*0.8f);
				if (hitColliders.Length>0)
				{
					Destroy(camPose);
				}
				else // check if cam is generated on empty space
				{
					hitColliders = Physics.OverlapSphere(sphereCenter, m_HeigthCam*1.3f);
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
        foreach (var n in m_CurrentNormals)
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

        var normal = Instantiate(sectionNormal, tf.transform);
        normal.GetComponentInChildren<Renderer>().material.color = m_NewColor;

        Section s = new Section();
        s.normal = normal;
        s.borders = new List<GameObject>(m_CurrentNormals);
        GenerateCamPoses(s);

        allSections.Add(s);

        m_CurrentNormals.Clear();

        // pick a random color
        m_NewColor = new Color(Random.value, Random.value, Random.value, 1.0f);

	}

	private void CloseRectangleSection()
	{
		// pick a random color
		m_NewColor = new Color(Random.value, Random.value, Random.value,0.1f);

		if (!m_IsRectangleOnPlane) return;
		var dummyRectangle = new GameObject();

		dummyRectangle.transform.position = rectangle.transform.position;
		dummyRectangle.transform.up = rectangle.transform.up;

		var normal = Instantiate(sectionNormal,dummyRectangle.transform);
		var rectangleInstance = Instantiate(rectangle);


		normal.GetComponentInChildren<Renderer>().material.color = m_NewColor;
		rectangleInstance.GetComponentInChildren<Renderer>().material.color = m_NewColor;

		Section s = new Section();
		s.normal = normal;
		s.borders = new List<GameObject>(m_CurrentNormals);
		s.rectangle = rectangleInstance;
		GenerateCamPoses(s);
		//s.rectangle.AddComponent<BoxCollider>();
		m_IDCounter++;
		s.id = m_IDCounter;
		//Debug.Log(m_IDCounter);

		allSections.Add(s);

		m_CurrentNormals.Clear();

		AddToList(ref s);
	}

	void RemovePreviousSection()
	{
		var section = allSections.ElementAt(allSections.Count-1);
		foreach (var b in section.borders)
		{
			Destroy(b);
		}

		Destroy(section.normal);
		Destroy(section.rectangle);
		allSections.RemoveAt(allSections.Count - 1);
	}

	void RemoveSection(Section section)
	{
		var sectionIndex = allSections.FindIndex(s => s.id == section.id);
		Debug.Log(sectionIndex);
		foreach (var b in allSections[sectionIndex].borders)
		{
			Destroy(b);
		}

		Destroy(allSections[sectionIndex].normal);
		Destroy(allSections[sectionIndex].rectangle);
		allSections.RemoveAt(sectionIndex);
	}

	void ResetCamera()
	{
		print("Reset camera to home");
		transform.position = m_Td.position;
		transform.rotation = m_Td.rotation;
	}

	void Quit()
	{
		Application.Quit();
	}

	void RemoveAll()
	{
		while (allSections.Count != 0)
		{
			RemovePreviousSection();
		}
	}

	void AutoSec()
	{
		if (allSections.Count == 0)
		{
			return;
		}
		var lastSection = allSections.Last();
		var direction = lastSection.normal.transform.forward;
		if (m_IsDownward) direction *= -1f;
		var oldHit = lastSection.normal.transform.position;
		var oldNormal = lastSection.normal.transform.up;
		var newHitPre = oldHit + direction * m_HeigthRectangle + oldNormal*5f;
		if (m_GORight)
		{
			var rightDir = lastSection.normal.transform.right;
			newHitPre = oldHit + rightDir * m_HeigthRectangle + oldNormal*5f;
		}

		Debug.DrawLine(newHitPre, newHitPre+(-oldNormal*10f),Color.green, 100f);

		RectangleUpdate(new Ray(newHitPre, -oldNormal));
		if (m_IsRectangleOnPlane)
		{
			CloseRectangleSection();
		}
		else
		{
			Debug.DrawLine(newHitPre, newHitPre+10f*-oldNormal,Color.red, 100f);
			Debug.Log("not on plane");
		}

		lastSection = allSections.Last();
		var projToYZ = new Vector3(0, lastSection.normal.transform.up.y, lastSection.normal.transform.up.z);
		float angleToY = Vector3.Angle(projToYZ, Vector3.up);
	//Debug.Log(angleToY);

	float angleThreshold = 10f * (m_HeigthRectangle + m_LengthRectangle + m_WidthRectangle) / 3f;
	Debug.Log(angleThreshold);

	// End of column, move one to right and go upward
		if ((Mathf.Abs(angleToY) < angleThreshold || Mathf.Abs(angleToY) > 180f-angleThreshold) && allSections.Count < 300 && m_IsRectangleOnPlane && !m_GORight)
		{
			Debug.Log("switch");
			m_IsDownward = !m_IsDownward;
			m_GORight = true;
			AutoSec();

		}
		else if (allSections.Count < 300 && m_IsRectangleOnPlane)
		{
			if (m_GORight) m_GORight = false;
			AutoSec();
		}
		//Debug.Log("out");


	}

	void DeleteSectionBySelection()
	{
		if (Input.GetMouseButtonUp(0))
		{
			//Debug.Log("here");
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if ( Physics.Raycast (ray,out hit,100.0f))
			{
				var gameObject = hit.transform.gameObject;
				//Debug.Log(gameObject.name);
				//Debug.Log(rectangle.name);
				if (gameObject.name != rectangle.name+"(Clone)")
				{
					return;
				}
				var id = gameObject.GetInstanceID();
				//Debug.Log(id);
				var sectionToDelete = allSections.Find(section => section.rectangle.GetInstanceID() == id);
				//Debug.Log(sectionToDelete);
				//Debug.Log(sectionToDelete.id);
				RemoveSection(sectionToDelete);
			}
		}
	}

	void Update()
	{
		//TODO
		if (EventSystem.current.IsPointerOverGameObject(0))    // is the touch on the GUI
		{
			// GUI Action
			return;
		}

		if (Input.GetKey("escape"))
		{
			Application.Quit();
		}

		if (deleteSectionByselectionMode)
		{
			DeleteSectionBySelection();
		}
		else if (rectangleMode)
		{
			// update rectangle on body
			RectangleUpdate(Camera.main.ScreenPointToRay(Input.mousePosition));

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

	/*
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
	}*/
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

