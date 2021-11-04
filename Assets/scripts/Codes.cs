//using System;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using System.Numerics;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

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
	public Toggle toggleRectangleMode, toggleDoubleCamMode, toggleDeleteBySelectionMode, toggleRegionMode;
	public InputField inputHeight, inputWidth, inputLength, inputCamHeight, inputCamWidth, inputCamLength, inputCamOverlap, inputCamDoubleAngle;
	public InputField inputRegionHeight, inputRegionWidth, inputRegionLength;
	public bool rectangleMode = true, deleteSectionBySelectionMode = false, doubleCamMode, regionMode=false;
	public GameObject rectangle, pyramid, region;
	[FormerlySerializedAs("m_collisionDistance")]
	public float collisionDistance = 0.2f;
	//IEnumerator coroutine;

	Color m_NewColor;
	private Order m_Order = new Order();
	private List<GameObject> m_CurrentNormals = new List<GameObject>();
	bool m_IsRectangleOnPlane = false;
	float m_HeightRectangle=2f, m_WidthRectangle=2f, m_LengthRectangle=2f, m_HeigthCam=0.7f, m_WidthCam=0.7f, m_LengthCam=0.7f, m_OverlapCam = 1f, m_DoubleAngleCam = 10f;
	float m_HeightRegion = 4f, m_WidthRegion = 15f, m_LengthRegion = 15f;
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
		rectPosesParent = new GameObject("parent rectangles");
		rectPosesParent.transform.position = Vector3.zero;
		m_IDCounter = 0;
		m_NewColor = new Color(Random.value, Random.value, Random.value, 0.1f);
		buttonWrite.onClick.AddListener(WriteToFile);
		buttonFinishSection.onClick.AddListener(CloseSection);
		buttonUndo.onClick.AddListener(RemovePreviousSection);
		buttonResetCam.onClick.AddListener(ResetCamera);
		buttonQuit.onClick.AddListener(Quit);
		buttonRemoveAll.onClick.AddListener(RemoveAll);
		buttonAutoSec.onClick.AddListener(CallAutoSec);
		toggleRectangleMode.onValueChanged.AddListener(ChangeRectangleMode);
		toggleDeleteBySelectionMode.onValueChanged.AddListener(ChangeDeletBySelectionMode);
		toggleRegionMode.onValueChanged.AddListener(ChangeRegionMode);
		toggleDoubleCamMode.onValueChanged.AddListener(ChangeDoubleCamMode);
		inputHeight.onValueChanged.AddListener(ChangeRectangleHeight);
		inputWidth.onValueChanged.AddListener(ChangeRectangleWidth);
		inputLength.onValueChanged.AddListener(ChangeRectangleLength);
		// region
		inputRegionHeight.onValueChanged.AddListener(ChangeRegionHeight);
		inputRegionWidth.onValueChanged.AddListener(ChangeRegionWidth);
		inputRegionLength.onValueChanged.AddListener(ChangeRegionLength);
		//
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
		m_HeightRectangle = Convert.ToSingle(h);
	}

	void ChangeRectangleWidth(string w)
	{
		rectangle.transform.localScale = new Vector3(Convert.ToSingle(w),rectangle.transform.localScale.y,rectangle.transform.localScale.z);
		m_WidthRectangle = Convert.ToSingle(w);
	}

	void ChangeRegionLength(string l)
	{
		region.transform.localScale =new Vector3(region.transform.localScale.x,region.transform.localScale.y,Convert.ToSingle(l));
		m_LengthRegion = Convert.ToSingle(l);
	}
	void ChangeRegionHeight(string h)
	{
		region.transform.localScale = new Vector3(region.transform.localScale.x,Convert.ToSingle(h),region.transform.localScale.z);
		m_HeightRegion = Convert.ToSingle(h);
	}

	void ChangeRegionWidth(string w)
	{
		region.transform.localScale = new Vector3(Convert.ToSingle(w),region.transform.localScale.y,region.transform.localScale.z);
		m_WidthRegion = Convert.ToSingle(w);
	}

	void RotatePlus(GameObject obj)
	{
		obj.transform.Rotate(Vector3.up * 5);
		//region.transform.Rotate(Vector3.up * 5);
	}


	void RotateMinus(GameObject obj)
	{
		obj.transform.Rotate(Vector3.up * -5);
		//region.transform.Rotate(Vector3.up * -5);
	}

	void ChangeRectangleMode(bool mode)
	{
		rectangleMode = mode;
	}

	void ChangeRegionMode(bool mode)
	{
		regionMode = mode;
		rectangle.transform.rotation = Quaternion.identity;
		region.transform.rotation = Quaternion.identity;
	}

	void ChangeDeletBySelectionMode(bool mode)
	{
		deleteSectionBySelectionMode = mode;
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

	void RefreshList()
	{
		// clear all
		foreach (Transform child in content.transform) {
			Destroy(child.gameObject);
		}

		// add all
		foreach (var section in allSections)
		{
			var copy = Instantiate(itemTemplate);
			copy.transform.parent = content.transform;

			copy.GetComponentInChildren<Text>().text = section.id.ToString();
			var section1 = section;
			var buttons = copy.GetComponentsInChildren<Button>();
			// Index button
			buttons[0].onClick.AddListener(
				() =>
				{
					HighlightSection(section1);
				});

			// Delete butoon
			buttons[1].onClick.AddListener(
				() =>
				{
					RemoveSection(section1);
				});

			// MoveUp butoon
			buttons[2].onClick.AddListener(
				() =>
				{
					MoveUp(section1);
				});

			// MoveDown butoon
			buttons[3].onClick.AddListener(
				() =>
				{
					MoveDown(section1);
				});
		}
	}

	public static void Swap<T>(IList<T> list, int indexA, int indexB)
	{
		T tmp = list[indexA];
		list[indexA] = list[indexB];
		list[indexB] = tmp;
	}

	void MoveUp(Section section)
	{
		int a = 0;
		int b = 0;
		for (int s=0; s<allSections.Count; s++)
		{
			if (allSections[s].id == section.id)
			{
				a = s;
				b = s - 1;
			}
		}

		if (b < 0)
			return;

		Swap(allSections, a, b);
		RefreshList();
	}

	void MoveDown(Section section)
	{
		int a = 0;
		int b = 0;
		for (int s=0; s<allSections.Count; s++)
		{
			if (allSections[s].id == section.id)
			{
				a = s;
				b = s + 1;
			}
		}

		if (b > allSections.Count-1)
			return;

		Swap(allSections, a, b);
		RefreshList();
	}

	UnityAction HighlightSection(Section section)
	{
		var initColor = section.rectangle.GetComponent<MeshRenderer>().material.color;
		StartCoroutine(ChangeColorCoroutine(section));
		section.rectangle.GetComponent<MeshRenderer>().material.color = Color.red;

		Debug.Log("number "+section.id.ToString());
		return null;
	}

	IEnumerator ChangeColorCoroutine(Section section)
	{
		var initColor = section.rectangle.GetComponent<MeshRenderer>().material.color;
		//section.rectangle.GetComponent<MeshRenderer>().material.color = Color.red;
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

	private void RayCastUpdate(GameObject obj, Ray ray)
	{
		RaycastHit hit;
		if ( Physics.Raycast (ray,out hit,100.0f))
		{
			obj.transform.position = hit.point;
			obj.transform.rotation = Quaternion.FromToRotation(obj.transform.up, hit.normal) * obj.transform.rotation;
			if (Input.GetKeyUp(KeyCode.Alpha1)) RotatePlus(obj);
			if (Input.GetKeyUp(KeyCode.Alpha2)) RotateMinus(obj);

			m_IsRectangleOnPlane = true;
		}
		else
		{
			obj.transform.position = Vector3.zero;
			m_IsRectangleOnPlane = false;
		}
	}

	GameObject rectPosesParent;

	void RegionMode(bool justViz = false)
	{
		GameObject[] rects;
		rects = GameObject.FindGameObjectsWithTag("rect");
		foreach (GameObject rect in rects)
		{
			Destroy(rect);
		}

		/*foreach (Transform child in rectPosesParent.transform) {
			GameObject.Destroy(child.gameObject);
		}*/

		//pyramid.transform.localScale = new Vector3(lengthCam, widthCam, heigthCam);
		var regScale = region.transform.lossyScale;
		for (float l = -m_LengthRegion /(regScale.z*2f) ; l < m_LengthRegion/(regScale.z*2f) ; l = l + (m_LengthRectangle/regScale.z))
		{
			for (float w = -m_WidthRegion/ (regScale.x*2f) ; w < m_WidthRegion/ (regScale.x*2f) ; w = w + (m_WidthRectangle/regScale.x))
			{
				var rectPose = Instantiate(rectangle, region.transform);
				rectPose.transform.localPosition = new Vector3(w, 0,l);
				//rectPose.transform.SetParent(rectPosesParent.transform);
				rectPose.tag = "rect";
				//var origin = region.transform.position + new Vector3(w, m_HeightRegion,l);
				var origin = rectPose.transform.position + region.transform.up*m_HeightRegion;
				var direction = -region.transform.up;
				Debug.DrawLine(origin, origin+direction*5f);
				RayCastUpdate(rectPose, new Ray(origin, direction));
				var rectScale = rectPose.transform.localScale;
				rectPose.transform.localScale = new Vector3((rectScale.x)/(regScale.x),
					(rectScale.y)/(regScale.y), (rectScale.z)/(regScale.z));
				rectPose.GetComponentInChildren<Renderer>().material.color = m_NewColor;
				if (!justViz)
				{
					CloseRectangleSection(rectPose.transform, false);
					Destroy(rectPose);
				}
			}
		}

		if (!justViz)
		{
			RefreshList();
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

	private void CloseRectangleSection(Transform tf, bool withRefresh=true)
	{
		// pick a random color
		m_NewColor = new Color(Random.value, Random.value, Random.value,0.1f);

		if (!m_IsRectangleOnPlane) return;
		var dummyRectangle = new GameObject();

		dummyRectangle.transform.position = tf.position;
		dummyRectangle.transform.up = tf.up;
		dummyRectangle.transform.rotation = tf.rotation;

		var normal = Instantiate(sectionNormal,dummyRectangle.transform);
		GameObject rectangleInstance;
		if (regionMode)
			rectangleInstance = Instantiate(rectangle, dummyRectangle.transform);
		else
		{
			rectangleInstance = Instantiate(rectangle);
		}


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

		if (withRefresh)
			RefreshList();
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

		RefreshList();
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
		RefreshList();
	}

	void CallAutoSec()
	{
		if (allSections.Count == 0)
		{
			return;
		}
		AutoSec();
		RefreshList();
	}

	void AutoSec()
	{
		var lastSection = allSections.Last();
		var direction = lastSection.normal.transform.forward;
		if (m_IsDownward) direction *= -1f;
		var oldHit = lastSection.normal.transform.position;
		var oldNormal = lastSection.normal.transform.up;
		var newHitPre = oldHit + direction * m_HeightRectangle + oldNormal*5f;
		if (m_GORight)
		{
			var rightDir = lastSection.normal.transform.right;
			newHitPre = oldHit + rightDir * m_HeightRectangle + oldNormal*5f;
		}

		//Debug.DrawLine(newHitPre, newHitPre+(-oldNormal*10f),Color.green, 100f);

		RayCastUpdate(rectangle, new Ray(newHitPre, -oldNormal));
		if (m_IsRectangleOnPlane)
		{
			CloseRectangleSection(rectangle.transform, false);
		}
		else
		{
			//Debug.DrawLine(newHitPre, newHitPre+10f*-oldNormal,Color.red, 100f);
			Debug.Log("not on plane");
		}

		lastSection = allSections.Last();
		var projToYZ = new Vector3(0, lastSection.normal.transform.up.y, lastSection.normal.transform.up.z);
		float angleToY = Vector3.Angle(projToYZ, Vector3.up);
		//Debug.Log(angleToY);

		float angleThreshold = 10f * (m_HeightRectangle + m_LengthRectangle + m_WidthRectangle) / 3f;
		Debug.Log(angleThreshold);

		// End of column, move one to right and go upward
		if ((Mathf.Abs(angleToY) < angleThreshold || Mathf.Abs(angleToY) > 180f-angleThreshold) && allSections.Count < 100 && m_IsRectangleOnPlane && !m_GORight)
		{
			Debug.Log("switch");
			m_IsDownward = !m_IsDownward;
			m_GORight = true;
			AutoSec();

		}
		else if (allSections.Count < 100 && m_IsRectangleOnPlane)
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

		if (regionMode)
		{
			RayCastUpdate(region, Camera.main.ScreenPointToRay(Input.mousePosition));
			// create section
			if (Input.GetMouseButtonUp(0) && m_IsRectangleOnPlane)
			{
				RegionMode();
			}
			else
			{
				RegionMode(true);
			}
		}
		else if (deleteSectionBySelectionMode)
		{
			DeleteSectionBySelection();
		}
		else if (rectangleMode)
		{
			// update rectangle on body
			RayCastUpdate(rectangle, Camera.main.ScreenPointToRay(Input.mousePosition));

			// create section
			if (Input.GetMouseButtonUp(0))
			{
				CloseRectangleSection(rectangle.transform);
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

