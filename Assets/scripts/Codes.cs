// All the libraries needed for the code
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

public class Codes : MonoBehaviour
{
	public GameObject floor;  // The gameobject including the floor of the hanger where the aircraft and decoating units drive on

	public Button buttonOptAlloc;  // The button for optimizing the

	int m_Index = 0;  // Index of section
	public GameObject itemTemplate;  // List item template
	public GameObject content;  // Content of a list item

	// AGV parametes
	[FormerlySerializedAs("m_AGV")]
	public GameObject AGV;  // The workspace of AGV represented as cube
	public InputField agvH, agvL, agvReach;  // AGV dimensions
	float m_agvH = 8f, m_agvL=0f, m_agvReach=3f;  // Initializing AGV dimensions
	public Toggle toggleAGV;  // Check AGV reachability toggle
	bool m_boolAGV;  // AGV toggle boolean

	// Crane parameters
	[FormerlySerializedAs("m_Crane")]
	public GameObject Crane;  // A sphere representing the crane
	public InputField craneR; //, craneReach;  // Parameters of the crane
	float m_craneR = 50f; //, m_craneReach = 3;  //
	public Toggle toggleCrane;  // Reachability of crane toggle
	bool m_boolCrane;  // Is reachability of crane toggle active

	// scene camera parameters
	Camera m_Cam;
	Vector3 m_AnchorPoint;
	Quaternion m_AnchorRot;

	int m_IDCounter;  // counter of number of sections

	TransformData m_Td;  // Transform data used for camera reset
	bool m_IsDownward = true, m_GORight = false;  // Camera paramerters
	[FormerlySerializedAs("m_normal")]
	public GameObject normal;  // Normal for drwa mode
	[FormerlySerializedAs("m_sectionNormal")]
	public GameObject sectionNormal;  // section normal
	public float distanceBetweenBorderNormals = 0.2f;  // In the draw mode, the distance between each normal placed on te surface to the next one

	// UI elemets
	public Button buttonWrite, buttonUndo, buttonResetCam, buttonFinishSection, buttonAutoSec, buttonRemoveAll, buttonQuit;
	public Toggle toggleRectangleMode, toggleDoubleCamMode, toggleDeleteBySelectionMode, toggleRegionMode;
	public InputField inputHeight, inputWidth, inputLength, inputCamHeight, inputCamWidth, inputCamLength, inputCamOverlap, inputCamDoubleAngle;
	public InputField inputRegionHeight, inputRegionWidth, inputRegionLength;
	public bool rectangleMode = true, deleteSectionBySelectionMode = false, doubleCamMode, regionMode=false;
	public GameObject rectangle, pyramid, region;
	[FormerlySerializedAs("m_collisionDistance")]
	public float collisionDistance = 0.2f;

	Color m_NewColor;  // Color used to change section color
	private Order m_Order = new Order();  // GA to find the shortest path between sections
	private List<GameObject> m_CurrentNormals = new List<GameObject>();
	bool m_IsRectangleOnPlane = false;  // Check if rectangle of the section mouse movement is not on the plane

	// Section, camera and region dimension
	float m_HeightRectangle=2f, m_WidthRectangle=2f, m_LengthRectangle=2f, m_HeigthCam=0.7f, m_WidthCam=0.7f, m_LengthCam=0.7f, m_OverlapCam = 1f, m_DoubleAngleCam = 10f;
	float m_HeightRegion = 4f, m_WidthRegion = 15f, m_LengthRegion = 15f;

	// Definition of the section
	public struct Section
	{
		public GameObject rectangle;  // cube of the section
		public List<GameObject> borders;  // borders used in draw mode, a list of normals
		public GameObject normal;  // Section's normal
		public List<Transform> camPoses;  // List of cam poses of the section
		public int id;  // ID of the section
		public Color defaultColor;  // Color of the section
		public bool agvReachable;  // If it's reachable by the AGV
		public bool craneReachable;  // If it's reachable by crane

		public void setAgvReachable(bool state)  // Sets the state of the section is reachable by AGV
		{
			agvReachable = state;
		}
		public void setCraneReachable(bool state)  // Sets the state of the section is reachable by Crane
		{
			craneReachable = state;
		}
	}

	// Definition of order of section
	private struct Order
	{
		public List<Section> sections;  // List of sections
	}

	// Main list of all sections
	public List<Section> allSections = new List<Section>();

	// Use this for initialization
	void Start ()
	{
		Physics.autoSyncTransforms = true;
		rectPosesParent = new GameObject("parent rectangles");
		rectPosesParent.transform.position = Vector3.zero;
		m_IDCounter = 0;

		// UI elements initialization
		buttonOptAlloc.onClick.AddListener(OptAlloc);
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

		// cam poses
		inputCamHeight.onValueChanged.AddListener(ChangeCamHeight);
		inputCamWidth.onValueChanged.AddListener(ChangeCamWidth);
		inputCamLength.onValueChanged.AddListener(ChangeCamLength);
		inputCamOverlap.onValueChanged.AddListener(ChangeCamOverlap);
		inputCamDoubleAngle.onValueChanged.AddListener(ChangeCamDoubleAngle);
		m_Td = transform.Clone();
		rectangleMode = true;

		// AGV
		agvH.onValueChanged.AddListener(ChangeAgvH);
		agvL.onValueChanged.AddListener(ChangeAgvL);
		agvReach.onValueChanged.AddListener(ChangeAgvReach);
		toggleAGV.onValueChanged.AddListener(AGVProcess);

		// Crane
		craneR.onValueChanged.AddListener(ChangeCraneR);
		toggleCrane.onValueChanged.AddListener(CraneProcess);
	}

	private void Awake()
	{
		m_Cam = GetComponent<Camera>();
	}

	// Check if the section is reachable by the Crane
	bool CanReachCrane(Section section)
	{
		// First the physics collider is activated, because otherwise raycast wont work
		section.normal.gameObject.GetComponent<BoxCollider>().enabled = true;
		var normalCenter = section.normal.transform.position;
		var craneCenter = Crane.transform.position;

		Vector3 dir = normalCenter - craneCenter;  // Direction of the ray cast to shoot and check if the section is reachable by the crane
		RaycastHit hit;
		Ray ray = new Ray(craneCenter,dir);
		var result = Physics.Raycast(ray, out hit, 100.0f);  // Result of the raycast

		// Disable the physics collider to avoid unwanted physics elements in the scene
		section.normal.GetComponent<BoxCollider>().enabled = false;

		if (result && hit.transform.gameObject == section.normal)
		{
			// The result hits the section under study
			return true;
		}
		return false;
	}

	// Go through all the sections and check if they are reachable by the crane
	void CraneProcess(bool boolCrane)
	{
		m_boolCrane = boolCrane;
		var collider = Crane.GetComponent<SphereCollider>();  // get the crane boolean
		if (boolCrane)
		{
			Crane.transform.localScale = new Vector3(m_craneR,m_craneR, m_craneR);
			Crane.transform.position = new Vector3(Crane.transform.position.x, 0.0f, Crane.transform.position.z);

			for (int i=0; i < allSections.Count; i++)
			{
				var dist = (allSections[i].normal.transform.position - Crane.transform.position).magnitude;
				if (dist < m_craneR/2)
				{
					if (CanReachCrane(allSections[i]))
					{
						HighlightSection(allSections[i], Color.green, 3f);
						allSections[i].setCraneReachable(true);
					}
				}
			}
			collider.enabled = false;  // Disable physics collider to avoid unwanted collisions
		}
		else
		{
			collider.enabled = true;
			Crane.transform.position = new Vector3(Crane.transform.position.x, 2000f, Crane.transform.position.z);
		}
	}

	// Change Crane radius
	void ChangeCraneR(string r)
	{
		m_craneR = Convert.ToSingle(r);
		if (m_boolCrane)
		{
			Crane.transform.localScale = new Vector3(m_craneR,m_craneR, m_craneR);
			Crane.transform.position = new Vector3(Crane.transform.position.x, 0.0f, Crane.transform.position.z);
		}
	}

	// Checks if the section can find any way to reach the ground within a given radius
	bool CanReachGround(float radius, Section section)
	{
		var trs = section.normal.transform;

		for (double i = 0; i < 360; i = i + 10)
		{
			Vector3 v = trs.position + Quaternion.AngleAxis((float)i, Vector3.up)*Vector3.left*radius;
			Debug.DrawLine(trs.position, v, Color.green, 50);
			RaycastHit hit;
			Ray ray = new Ray(v+Vector3.up*2,Vector3.down);
			if (Physics.Raycast(ray, out hit, 100.0f))
			{
				if(hit.transform.gameObject.CompareTag("floor"))
					return true;
			}
		}
		return false;
	}

	// Go through all the sections and check if they are reachable by the AGV (similar to crane)
	void AGVProcess(bool boolAGV)
	{
		floor.GetComponent<BoxCollider>().enabled = true;

		m_boolAGV = boolAGV;
		var collider = AGV.GetComponent<BoxCollider>();
		if (boolAGV)
		{
			AGV.transform.localScale = new Vector3(AGV.transform.localScale.x,m_agvH - m_agvL, AGV.transform.localScale.z);
			AGV.transform.position = new Vector3(AGV.transform.position.x, m_agvL+(m_agvH - m_agvL)/2, AGV.transform.position.z);

			for (int i=0; i < allSections.Count; i++)
			{
				if (collider.bounds.Contains(allSections[i].normal.transform.position))
				{
					if (CanReachGround(m_agvReach, allSections[i]))
					{
						HighlightSection(allSections[i], Color.blue, 3f);
						allSections[i].setAgvReachable(true);
					}
				}
			}
			collider.enabled = false;
		}
		else
		{
			collider.enabled = true;
			AGV.transform.position = new Vector3(AGV.transform.position.x, 2000f, AGV.transform.position.z);
		}

		floor.GetComponent<BoxCollider>().enabled = false;
	}

	// Change AGV height
	void ChangeAgvH(string h)
	{
		m_agvH = Convert.ToSingle(h);
		if (m_boolAGV)
		{
			AGV.transform.localScale = new Vector3(AGV.transform.localScale.x,m_agvH - m_agvL, AGV.transform.localScale.z);
			AGV.transform.position = new Vector3(AGV.transform.position.x, m_agvL+(m_agvH - m_agvL)/2, AGV.transform.position.z);
		}
	}

	// Change AGV length
	void ChangeAgvL(string l)
	{
		if (m_boolAGV)
		{
			AGV.transform.localScale = new Vector3(AGV.transform.localScale.x,m_agvH - m_agvL, AGV.transform.localScale.z);
			AGV.transform.position = new Vector3(AGV.transform.position.x, m_agvL, AGV.transform.position.z);
		}
		m_agvL = Convert.ToSingle(l);
	}

	// Change AGV reach radius
	void ChangeAgvReach(string r)
	{
		m_agvReach = Convert.ToSingle(r);
	}

	// Change Camera pose length
	void ChangeCamLength(string l)
	{
		//pyramid.transform.localScale =new Vector3(pyramid.transform.localScale.x,pyramid.transform.localScale.y,Convert.ToSingle(l));
		m_LengthCam = Convert.ToSingle(l);
	}

	// Change Camera pose Height
	void ChangeCamHeight(string h)
	{
		//pyramid.transform.localScale = new Vector3(pyramid.transform.localScale.x,Convert.ToSingle(h),pyramid.transform.localScale.z);
		m_HeigthCam = Convert.ToSingle(h);
	}

	// Change Camera pose Width
	void ChangeCamWidth(string w)
	{
		//pyramid.transform.localScale = new Vector3(Convert.ToSingle(w),pyramid.transform.localScale.y,pyramid.transform.localScale.z);
		m_WidthCam = Convert.ToSingle(w);
	}

	// Change Camera pose overlap
	void ChangeCamOverlap(string o)
	{
		m_OverlapCam = Convert.ToSingle(o);
	}

	// Change Camera pose double angle to cover appandages
	void ChangeCamDoubleAngle(string a)
	{
		m_DoubleAngleCam = Convert.ToSingle(a);
	}

	// Change section cube length
	void ChangeRectangleLength(string l)
	{
		rectangle.transform.localScale =new Vector3(rectangle.transform.localScale.x,rectangle.transform.localScale.y,Convert.ToSingle(l));
		m_LengthRectangle = Convert.ToSingle(l);
	}

	// Change section cube height
	void ChangeRectangleHeight(string h)
	{
		rectangle.transform.localScale = new Vector3(rectangle.transform.localScale.x,Convert.ToSingle(h),rectangle.transform.localScale.z);
		m_HeightRectangle = Convert.ToSingle(h);
	}

	// Change section cube width
	void ChangeRectangleWidth(string w)
	{
		rectangle.transform.localScale = new Vector3(Convert.ToSingle(w),rectangle.transform.localScale.y,rectangle.transform.localScale.z);
		m_WidthRectangle = Convert.ToSingle(w);
	}

	// Change region cube length
	void ChangeRegionLength(string l)
	{
		region.transform.localScale =new Vector3(region.transform.localScale.x,region.transform.localScale.y,Convert.ToSingle(l));
		m_LengthRegion = Convert.ToSingle(l);
	}

	// Change region cube height
	void ChangeRegionHeight(string h)
	{
		region.transform.localScale = new Vector3(region.transform.localScale.x,Convert.ToSingle(h),region.transform.localScale.z);
		m_HeightRegion = Convert.ToSingle(h);
	}

	// Change region cube width
	void ChangeRegionWidth(string w)
	{
		region.transform.localScale = new Vector3(Convert.ToSingle(w),region.transform.localScale.y,region.transform.localScale.z);
		m_WidthRegion = Convert.ToSingle(w);
	}

	// Rotate the section clockwise
	void RotatePlus(GameObject obj)
	{
		obj.transform.Rotate(Vector3.up * 5);
		//region.transform.Rotate(Vector3.up * 5);
	}

	// Rotate the section counterclockwise
	void RotateMinus(GameObject obj)
	{
		obj.transform.Rotate(Vector3.up * -5);
		//region.transform.Rotate(Vector3.up * -5);
	}

	// Toggle between rectangle mode or draw mode
	void ChangeRectangleMode(bool mode)
	{
		rectangleMode = mode;
	}

	// Switch to region mode
	void ChangeRegionMode(bool mode)
	{
		regionMode = mode;
		rectangle.transform.rotation = Quaternion.identity;
		region.transform.rotation = Quaternion.identity;
	}

	// Delete by selection mode
	void ChangeDeletBySelectionMode(bool mode)
	{
		deleteSectionBySelectionMode = mode;
		// add collider to all sections
		if (mode)
		{
			// If activated, add physics so it can be deleted by mouse click
			foreach (var s in allSections)
			{
				s.rectangle.AddComponent<BoxCollider>();
			}
		}
		else
		{
			// otherwise, remove the physics to avoid unwanted collision
			foreach (var s in allSections)
			{
				Destroy(s.rectangle.GetComponent<BoxCollider>());
			}
		}
	}

	// Switch the double camera mode on and off
	void ChangeDoubleCamMode(bool mode)
	{
		doubleCamMode = mode;
	}

	// Refresh the List of sections shown on the left side.
	// This function is called in many places every time a list refresh is needed.
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
					HighlightSection(section1, Color.red);
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

	// Swap two enteries in the section list
	public static void Swap<T>(IList<T> list, int indexA, int indexB)
	{
		T tmp = list[indexA];
		list[indexA] = list[indexB];
		list[indexB] = tmp;
	}

	// Move up an entry in the section list
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

	// Move down an entry in the lit of section
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

	// Highlight a section, used in many places
	UnityAction HighlightSection(Section section, Color color, float sec =1f)
	{
		var initColor = section.rectangle.GetComponent<MeshRenderer>().material.color;
		StartCoroutine(ChangeColorCoroutine(section, sec));
		section.rectangle.GetComponent<MeshRenderer>().material.color = color;

		Debug.Log("number "+section.id.ToString());
		return null;
	}

	// highlight a section when clicked on its entry in the list
	IEnumerator ChangeColorCoroutine(Section section, float sec = 1f)
	{
		var initColor = section.defaultColor;
		//section.rectangle.GetComponent<MeshRenderer>().material.color = Color.red;
		yield return new WaitForSeconds(sec);
		section.rectangle.GetComponent<MeshRenderer>().material.color = initColor;
	}

	// TODO: Section allocation for those sections shared between
	void OptAlloc()
	{
		foreach (var s in allSections)
		{
			if(s.agvReachable && s.craneReachable)
				HighlightSection(s, Color.red, 3f);
			else if(s.craneReachable)
				HighlightSection(s, Color.green, 3f);
			else if (s.agvReachable)
				HighlightSection(s, Color.blue, 3f);
		}
	}

	// Output the result of sectioning and mission planing in a text file
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

	// Draw mode to create the section
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

	// A very frequently called function that is find the location of the mouse cursor on the aircraft body.
	// This also takes care of rotation of the section/region or basically any object passed in
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

	// Main process of the region mode
	void RegionMode(bool justViz = false)
	{
		GameObject[] rects;  // an empty array of the rectangles
		rects = GameObject.FindGameObjectsWithTag("rect");  // Find the rectangle game object
		foreach (GameObject rect in rects)
		{
			Destroy(rect);
		}

		/*foreach (Transform child in rectPosesParent.transform) {
			GameObject.Destroy(child.gameObject);
		}*/

		//pyramid.transform.localScale = new Vector3(lengthCam, widthCam, heigthCam);
		var regScale = region.transform.lossyScale;

		// Simulate a grid where sections are created, respecting the region size
		// Side-by-side section are created to fill the region
		for (float l = -m_LengthRegion /(regScale.z*2f) ; l < m_LengthRegion/(regScale.z*2f) ; l = l + (m_LengthRectangle/regScale.z))
		{
			for (float w = -m_WidthRegion/ (regScale.x*2f) ; w < m_WidthRegion/ (regScale.x*2f) ; w = w + (m_WidthRectangle/regScale.x))
			{
				var rectPose = Instantiate(rectangle, region.transform);  // Create a enw game object of type rectangle witht he region as its parent
				rectPose.transform.localPosition = new Vector3(w, 0,l);  // Relocate the section to the designed location
				//rectPose.transform.SetParent(rectPosesParent.transform);
				rectPose.tag = "rect";  // add a tag to the game object
				//var origin = region.transform.position + new Vector3(w, m_HeightRegion,l);

				// Check if the section is on the aircraft body or not
				// If the section is floating in the air, it needs to be removed
				// also the section has to align with the surface
				// To shoot a raycast, we create the start point to be the origin, where it is slightly above the enter of the section
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

	// This function creates the cam poses within a section
	// Similar to the region mode that creates sections, this method creates a grid and places a pyramid (representing the camera view)
	// Also it can create a double cam pose if this option is selected by the user
	// A double cam pose is when instead of one, two camera poses with an adjustable angle are created, in order to more accurately scan the surface
	void GenerateCamPoses(Section s)
	{
		var recScale = s.rectangle.transform.lossyScale;

		// Create the grid
		for (float l = -m_LengthRectangle / (recScale.z*2f); l < m_LengthRectangle / (recScale.z*2f); l = l + (m_LengthCam/recScale.z)/m_OverlapCam)
		{
			for (float w = -m_WidthRectangle / (recScale.x*2f); w < m_WidthRectangle / (recScale.x*2f); w = w + (m_WidthCam/recScale.x)/m_OverlapCam)
			{
				// Create a new pyramid object
				var camPose = Instantiate(pyramid, s.rectangle.transform);
				// Moe it to the right spot
				camPose.transform.localPosition = new Vector3(w, 0,l);
				// Store the scale of the pyramid
				var camScale = camPose.transform.localScale;
				// Scale the pyramid based on the cam size given b ythe user
				camPose.transform.localScale = new Vector3((camScale.x*m_WidthCam)/(recScale.x*0.3f),
					(camScale.y*m_HeigthCam)/(recScale.y*0.5f), (camScale.z*m_LengthCam)/(recScale.z*0.3f));
				// Get the color to change later
				camPose.GetComponentInChildren<Renderer>().material.color = m_NewColor;

				// Double cam mode
				if (doubleCamMode)
				{
					camPose.transform.rotation *= Quaternion.AngleAxis(m_DoubleAngleCam, camPose.transform.up);

					// Create a second pyramid as the double cam
					var camPose2 = Instantiate(pyramid, s.rectangle.transform);
					// Same process as the first pyramid few lines above
					camPose2.transform.localPosition = new Vector3(w, 0,l);
					var camScale2 = camPose2.transform.localScale;
					camPose2.transform.localScale = new Vector3((camScale.x*m_WidthCam)/(recScale.x*0.3f),
						(camScale.y*m_HeigthCam)/(recScale.y*0.5f), (camScale.z*m_LengthCam)/(recScale.z*0.3f));
					camPose2.GetComponentInChildren<Renderer>().material.color = m_NewColor;

					camPose2.transform.rotation *= Quaternion.AngleAxis(-m_DoubleAngleCam, camPose2.transform.up);

					// Check collision
					var sphereCenter2 = camPose2.transform.position+camPose2.transform.up*m_HeigthCam;
					// Remove the cam pose if the distance from the pyramid apex is not far enough from the surface
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
				// Remove the cam pose if the distance from the pyramid apex is not far enough from the surface
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

	// In draw mode, a section closed either by pressing space bar or GUI button
	private void CloseSection()
	{
		// Create empty vector to fill later
		var sectionPosition = new Vector3(0,0,0);
        var sectionUp = new Vector3(0,0,0);
        float i = 0;

        // Go through all the drawn normals and Find the average for them
        foreach (var n in m_CurrentNormals)
        {
        	sectionPosition += n.transform.position;
        	sectionUp += n.transform.up;
            i++;
        }

        sectionPosition /= i;
        sectionUp /= i;
        sectionUp.Normalize();

        // Create an empty transform and fill it up with the calculated average
        GameObject tf = new GameObject();
        tf.transform.position = sectionPosition;
        tf.transform.up = sectionUp;

        // Create the normal of the section
        var normal = Instantiate(sectionNormal, tf.transform);
        normal.GetComponentInChildren<Renderer>().material.color = m_NewColor;

        // Create an empty section and fill it up with useful info, then add it to the list of all sections
        Section s = new Section();
        s.normal = normal;
        s.borders = new List<GameObject>(m_CurrentNormals);
        GenerateCamPoses(s);

        allSections.Add(s);

        m_CurrentNormals.Clear();

        // pick a random color to be used for next section
        m_NewColor = new Color(Random.value, Random.value, Random.value, 1.0f);

	}

	// In the rectangle mode, create the rectangular section
	private void CloseRectangleSection(Transform tf, bool withRefresh=true)
	{
		// pick a random color
		m_NewColor = new Color(Random.value, Random.value, Random.value,0.1f);

		// Check if at this moment the rectangle is placed on the aircraft surface
		if (!m_IsRectangleOnPlane) return;
		var dummyRectangle = new GameObject();

		// Create a dummy rectangle to place in the right place and fill up with correct info
		dummyRectangle.transform.position = tf.position;
		dummyRectangle.transform.up = tf.up;
		dummyRectangle.transform.rotation = tf.rotation;

		var normal = Instantiate(sectionNormal,dummyRectangle.transform);
		GameObject rectangleInstance;

		// If the section is being created within the region mode, its parent needs to be the region
		rectangleInstance = regionMode ? Instantiate(rectangle, dummyRectangle.transform) : Instantiate(rectangle);

		normal.GetComponentInChildren<Renderer>().material.color = m_NewColor;
		rectangleInstance.GetComponentInChildren<Renderer>().material.color = m_NewColor;

		// Create an empty section to fill with the rectangle and other info about the section, then add it to the list of all section
		Section s = new Section();
		s.normal = normal;
		s.borders = new List<GameObject>(m_CurrentNormals);
		s.rectangle = rectangleInstance;
		s.defaultColor = m_NewColor;
		GenerateCamPoses(s);

		m_IDCounter++;
		s.id = m_IDCounter;

		allSections.Add(s);

		m_CurrentNormals.Clear();

		// If refresh list is active, refresh the list
		if (withRefresh)
			RefreshList();
	}

	// This is the undo function that removes the previously created function
	void RemovePreviousSection()
	{
		// Get the last section
		var section = allSections.ElementAt(allSections.Count-1);

		// Delete all the draw-mode cylinders associated to the section border
		foreach (var b in section.borders)
		{
			Destroy(b);
		}

		// Delete the normal
		Destroy(section.normal);
		// Delete the rectangular section
		Destroy(section.rectangle);
		// Delete the section itself
		allSections.RemoveAt(allSections.Count - 1);
	}

	// Remove the given section. This is used in the remove by click
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

	// Reset the camera pose to the one at the start of the app
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

	// The call to the auto section function to create the sections on the surface of the fuselage
	void CallAutoSec()
	{
		if (allSections.Count == 0)
		{
			return;
		}
		AutoSec();
		RefreshList();
	}

	// Create the sections on the surface of the fuselage
	// The way this works is that from the last section in the list of sections, we go down (m_IsDownward==true) for the distance of one section,
	// then using the raycast, find the contact point on the surface to place the new section.
	// This continues until we reach the belly of the aircraft, where we need to switch to upward mode (m_IsDownward==false)
	// Where the same process is continued in the opposite direction
	// This function is call within itself recursively. The number of automatically placed sections is hard coded for now
	void AutoSec()
	{
		// Query the last section and its normal
		var lastSection = allSections.Last();
		var direction = lastSection.normal.transform.forward;

		// Downward mode
		if (m_IsDownward) direction *= -1f;
		var oldHit = lastSection.normal.transform.position;
		var oldNormal = lastSection.normal.transform.up;
		var newHitPre = oldHit + direction * m_HeightRectangle + oldNormal*5f;

		// If we have reached the belly of the aircraft
		if (m_GORight)
		{
			var rightDir = lastSection.normal.transform.right;
			newHitPre = oldHit + rightDir * m_HeightRectangle + oldNormal*5f;
		}

		RayCastUpdate(rectangle, new Ray(newHitPre, -oldNormal));
		if (m_IsRectangleOnPlane)
		{
			CloseRectangleSection(rectangle.transform, false);
		}
		else
		{
			Debug.Log("not on plane");
		}

		lastSection = allSections.Last();
		var projToYZ = new Vector3(0, lastSection.normal.transform.up.y, lastSection.normal.transform.up.z);
		float angleToY = Vector3.Angle(projToYZ, Vector3.up);

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

			// Recursive call
			AutoSec();
		}
	}

	// This function is called when the user selects the delete by selection mode
	void DeleteSectionBySelection()
	{
		// When left mouse is clicked
		if (Input.GetMouseButtonUp(0))
		{
			// Create an empty raycast
			RaycastHit hit;
			// Find hit position in the scene
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			// If there is a hit detected
			if ( Physics.Raycast (ray,out hit,100.0f))
			{
				var gameObject = hit.transform.gameObject;

				// If the hit object is not a section, return because raycast has hit something else
				if (gameObject.name != rectangle.name+"(Clone)")
				{
					return;
				}

				// Query the ID of the hit object
				var id = gameObject.GetInstanceID();

				// Find the section within the all sections by its ID
				var sectionToDelete = allSections.Find(section => section.rectangle.GetInstanceID() == id);

				RemoveSection(sectionToDelete);
			}
		}
	}

	// The main update loop of the simulation software.
	// Every frame of the simulation this function is called
	// For example 30 time per second for 30 FPS simulation
	void Update()
	{
		//TODO: If there is a mouse click on the GUI, there should not be any sections placed on the aircraft
		if (EventSystem.current.IsPointerOverGameObject(0))    // is the touch on the GUI
		{
			// GUI Action
			return;
		}

		// If 'escape' button is pressed, quit the app
		if (Input.GetKey("escape"))
		{
			Application.Quit();
		}

		// If the region mode check box is ticked
		if (regionMode)
		{
			// PLace the region where the mouse cursor is
			RayCastUpdate(region, Camera.main.ScreenPointToRay(Input.mousePosition));

			// When left mouse button clicked
			if (Input.GetMouseButtonUp(0) && m_IsRectangleOnPlane)
			{
				// Place the region and the sections and the camera poses in it
				RegionMode();
			}
			else
			{
				// When the left mouse button is not clicked, only display the potential region
				RegionMode(true);
			}
		}
		// Else if the delete by selection option is clicked
		else if (deleteSectionBySelectionMode)
		{
			DeleteSectionBySelection();
		}
		// Else if we are in the rectangle mode
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
		// If none of the above it means we are in the draw mode
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
}

// The below section is to control the viewport camera motion in the scene
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

