using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

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

	private List<GameObject> m_currentNormals = new List<GameObject>();

	// Use this for initialization
	void Start () {
		td = transform.Clone();
	}

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	void Update()
	{
		if ( Input.GetMouseButtonDown (0)){
        	RaycastHit hit;
        	Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        	if ( Physics.Raycast (ray,out hit,100.0f)) {
        		//Instantiate(m_mark, tran, Quaternion.identity);
                var normal =Instantiate(m_normal, hit.point, Quaternion.identity);
                m_currentNormals.Add(normal);
                normal.transform.up = hit.normal;

                //Debug.DrawRay(hit.point, -hit.normal,Color.green,5f);
            }
        }

		// Rest scene view
        if (Input.GetKey(KeyCode.Space))
        {
        	print("Reset to home");
        	transform.position = td.position;
        	transform.rotation = td.rotation;
        }

		// Close section
		if (Input.GetKeyUp(KeyCode.Return))
		{
			print("Close section");
			var sectionPosition = new Vector3(0,0,0);
			var sectionUp = new Vector3(0,0,0);
			float i = 0;
			foreach (var n in m_currentNormals)
			{
				sectionPosition += n.transform.position;
				sectionUp += n.transform.up;
				Debug.Log("n position"+ n.transform.position);
				Debug.Log("n up"+ n.transform.up);
				i++;
			}

			Debug.Log("section position"+ sectionPosition);
			Debug.Log("section up"+ sectionUp);

			sectionPosition /= i;
			sectionUp /= i;
			sectionUp.Normalize();

			Debug.Log("section position"+ sectionPosition);
			Debug.Log("section up"+ sectionUp);

			GameObject tf = new GameObject();
			tf.transform.position = sectionPosition;
			tf.transform.up = sectionUp;

			Instantiate(m_sectionNormal, tf.transform);

			m_currentNormals.Clear();
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
