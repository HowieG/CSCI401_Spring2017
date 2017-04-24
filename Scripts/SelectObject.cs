using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using Leap;

[RequireComponent (typeof(PhotonView))]
public class SelectObject : Photon.MonoBehaviour
{
	//public class SelectObject : MonoBehaviour {

	public static GameObject selectedObject;

	List<GameObject> selectableObjects = new List<GameObject> ();
	//	public GameObject obj1;
	//	public GameObject obj2;
	//	public GameObject obj3;
	//	public GameObject obj4;


	HandModel hand_model;
	Hand leap_hand;
	Controller LEAPcontroller;
	private Frame oldFrame;

	FingerModel indexFinger;
	FingerModel middleFinger;
	FingerModel ringFinger;

	//SELECT
	private bool isHit;
	private bool alreadyHit = false;

	private Color previousColour;

	LineRenderer line;

	//MOVE
	private float originalZPos;


	//SCALE
	private float targetScale;
	private float shrinkSpeed;
	private Vector3 v3Scale;


	void Start ()
	{

		//initialize Leap Motion
		hand_model = GetComponent<HandModel> ();
		leap_hand = hand_model.GetLeapHand ();
		if (leap_hand == null)
			Debug.LogError ("No leap_hand founded");
		LEAPcontroller = new Controller ();
		LEAPcontroller.EnableGesture (Gesture.GestureType.TYPE_CIRCLE);
		LEAPcontroller.EnableGesture (Gesture.GestureType.TYPE_SWIPE);

		indexFinger = hand_model.fingers [1];
		middleFinger = hand_model.fingers [2];
		ringFinger = hand_model.fingers [3];

		initSelect ();
		initScale ();
	}


	void Update ()
	{
		Frame frame = leap_hand.Frame;

//		printFrame ();

		//don't neet to select an object when one is already selected
		if (!selectedObject) {
			selectObject ();
		} else {

			if (!photonView.isMine) {
				photonView.RequestOwnership ();
			}
			if (!photonView.isMine) {
//				return;
			}

			moveObject (oldFrame);
//			checkForGestures ();
			rotateObject ();
		}
		oldFrame = frame;
	}



	//SELECT

	private void initSelect ()
	{
		//initialize laser
		line = gameObject.GetComponent<LineRenderer> ();
		line.enabled = false;
		line.material = new Material (Shader.Find ("Particles/Additive"));
		//TODO: highlight
		//		Shader shader1 = Shader.Find( "Diffuse" );
		//		Shader shader2 = Shader.Find( "Self-Illumin/Diffuse" );
	}

	IEnumerator FireLaser (FingerModel finger)
	{
		line.enabled = true;

		//TODO: only raycast when selectedObject == null
//		if (selectedObject == null) {
		Ray ray = new Ray (finger.GetTipPosition (), finger.GetRay ().direction);
		line.SetPosition (0, ray.origin);
		line.SetPosition (1, ray.GetPoint (100));
		yield return null;
//		}

		line.enabled = false;
	}

	IEnumerator MoveLaser (FingerModel finger)
	{
		line.enabled = true;

		float rayLength = Vector3.Distance (finger.GetTipPosition (), selectedObject.transform.position);

		Ray ray = new Ray (finger.GetTipPosition (), finger.GetRay ().direction);
		line.SetPosition (0, ray.origin);
		line.SetPosition (1, ray.GetPoint (rayLength));
		yield return null;

		line.enabled = false;
	}

	private void selectObject ()
	{
		if (!leap_hand.IsRight) {
			return;
		}

		line.SetColors (Color.red, Color.white);

		StopCoroutine ("FireLaser");
		StartCoroutine ("FireLaser", indexFinger);

		Debug.DrawRay (indexFinger.GetTipPosition (), indexFinger.GetRay ().direction, Color.red);
		RaycastHit indexHit;

		isHit = Physics.Raycast (indexFinger.GetTipPosition (), indexFinger.GetRay ().direction, out indexHit);

		if (isHit) {
			//can only select desired objects

			Debug.Log (indexHit.collider);

			if (indexHit.transform.parent.name == "Objects") {
				selectedObject = indexHit.transform.gameObject;
				originalZPos = selectedObject.transform.position.z;
				return;
			} else if (indexHit.transform.parent.name == "Satellite") {
				selectedObject = indexHit.transform.parent.gameObject;
				originalZPos = selectedObject.transform.position.z;
				return;
			}

			selectedObject = null;
			return;
		}
	}





	//MOVE
	private void moveObject (Frame oldFrame)
	{

		line.SetColors (Color.blue, Color.white);

//		float originalZ = selectedObject.transform.position.z;

		if (!leap_hand.IsRight) {
			return;
		}

		//debugging
		StopCoroutine ("MoveLaser");
		StartCoroutine ("MoveLaser", middleFinger);


		RaycastHit indexHit;
		RaycastHit middleHit;
		bool indexRayIntersectingObject = Physics.Raycast (indexFinger.GetTipPosition (), indexFinger.GetRay ().direction, out indexHit);
		bool middleRayIntersectingObject = Physics.Raycast (middleFinger.GetTipPosition (), middleFinger.GetRay ().direction, out middleHit);

//		//must be pointing at selected object
		if ((indexHit.transform.gameObject != selectedObject) && (indexHit.transform.parent.gameObject != selectedObject)) {
			return;
		}
//
//		float middle_index = Vector3.Distance (indexFinger.GetTipPosition (), middleFinger.GetTipPosition ());
//
//		//both middle and index finger must point to the same object
//		if ((middleHit.transform.gameObject != selectedObject) && (indexHit.transform.parent.gameObject != selectedObject)) {
//			return;
//		}

		float middle_index = Vector3.Distance (indexFinger.GetTipPosition (), middleFinger.GetTipPosition ());

		//both fingers must be pointing at the object
		if (middle_index > 0.13) {
			return;
		}

//
		Vector deltaZ = leap_hand.Translation (oldFrame);
		Debug.Log (deltaZ);

//		float averageX = (indexHit.point.x + middleHit.point.x)/2;
//		float averageY = (indexHit.point.y + middleHit.point.y)/2;

		float zPos = Mathf.Exp (3 * (indexFinger.GetTipPosition ().z + 5)) + originalZPos;

		Debug.Log (zPos);

		//only move along x and y axis
		Vector3 newPos = new Vector3 (middleHit.point.x, middleHit.point.y, zPos);

		//debug
//		float averageZ = (indexHit.point.z + middleHit.point.z)/2;
//		Vector3 debugPos = new Vector3 (averageX, averageY, averageZ);
//		Debug.DrawRay (indexFinger.GetTipPosition(), debugPos, Color.red);
//		Debug.DrawRay (middleFinger.GetTipPosition(), debugPos, Color.red);


//		Debug.Log ("index pos: " + indexFinger.GetTipPosition().z);
//
//		float distance = Vector3.Distance (indexFinger.GetTipPosition (), selectedObject.transform.position);
//
//		float zPos = Mathf.Exp(5 * (indexFinger.GetTipPosition ().z + 5));
//
//		Debug.Log (zPos);
//
		selectedObject.transform.position = newPos;

	}

	//GESTURES

	//SCALE
	public void initScale ()
	{
//		targetScale = Mathf.Min (selectedObject.transform.localScale.x, selectedObject.transform.localScale.y, selectedObject.transform.localScale.z);
		shrinkSpeed = 1.0f;
		//TODO: update targetScale for each object selected
		targetScale = 0.002f;
		Debug.Log ("initializing scale to " + targetScale);
	}

	void checkForGestures ()
	{

		if (!leap_hand.IsLeft) {
			return;
		}

		Debug.Log (selectedObject);

		Frame frame = LEAPcontroller.Frame ();

		GestureList gesturesInFrame = frame.Gestures ();
		if (!gesturesInFrame.IsEmpty) {
			foreach (Gesture gesture in gesturesInFrame) {
				switch (gesture.Type) {

				//CIRCLE = GROW/SHRINK
				case Gesture.GestureType.TYPECIRCLE:
					CircleGesture circleGesture = new CircleGesture (gesture);
					float turns = circleGesture.Progress / 1000;

					//grow if rotating clockwise
					if (circleGesture.Pointable.Direction.AngleTo (circleGesture.Normal) <= Mathf.PI / 2) {
						if (targetScale <= 10.0f) {
							targetScale += turns;
						}
					} else {
						if (targetScale >= 1.0f) {
							targetScale -= turns;
						}
					}

					v3Scale = new Vector3 (targetScale, targetScale, targetScale);
					selectedObject.transform.localScale = Vector3.Lerp (selectedObject.transform.localScale, v3Scale, Time.deltaTime * shrinkSpeed);

					break;

				case Gesture.GestureType.TYPESWIPE:
					SwipeGesture swipe = new SwipeGesture (gesture);
					Vector swipeDirection = swipe.Direction;
					//left swipe
					if (swipeDirection.x > 0) {
						Debug.Log ("rightSwipe");
						selectedObject.transform.GetComponent<MeshRenderer> ().material.color = previousColour;
						selectedObject = null;
					}
					break;

				}
			}
		}
	}

	void rotateObject ()
	{

		if (!leap_hand.IsLeft) {
			return;
		}

		float middle_index = Vector3.Distance (indexFinger.GetTipPosition (), middleFinger.GetTipPosition ());
		float ring_middle = Vector3.Distance (middleFinger.GetTipPosition (), ringFinger.GetTipPosition ());

		Debug.Log ("middle_index: " + middle_index);
		Debug.Log ("ring_middle: " + ring_middle);

		if ((middle_index < .13) && (ring_middle < .13)) {
			Debug.Log ("here 1");
			float zPos = Mathf.Exp ((middleFinger.GetTipPosition ().z + 5));
			Quaternion rotz = Quaternion.AngleAxis (zPos, Vector3.forward);
			selectedObject.transform.rotation = rotz * selectedObject.transform.rotation;
			return;
		} else if (middle_index < .13) {
			Debug.Log ("here 2");
			float yPos = Mathf.Exp ((middleFinger.GetTipPosition ().y));
			Quaternion roty = Quaternion.AngleAxis (yPos, Vector3.down);
			selectedObject.transform.rotation = roty * selectedObject.transform.rotation;
			return;
		} else {
			Debug.Log ("here 3");
			float xPos = Mathf.Exp ((indexFinger.GetTipPosition ().x));
			Quaternion rotx = Quaternion.AngleAxis (xPos, Vector3.right);
			selectedObject.transform.rotation = rotx * selectedObject.transform.rotation;
			return;
		}






//		/////
//
//		float xPos = Mathf.Exp((indexFinger.GetTipPosition ().x));
//		float yPos = Mathf.Exp((indexFinger.GetTipPosition ().y));
//		float zPos = Mathf.Exp((indexFinger.GetTipPosition ().z + 5));
//	
//		selectedObject.transform.Rotate (xPos, yPos, zPos);


	}

	void printFrame ()
	{

		Frame frame = LEAPcontroller.Frame ();

		Debug.Log ("Frame id: " + frame.Id
		+ ", timestamp: " + frame.Timestamp
		+ ", hands: " + frame.Hands.Count
		+ ", fingers: " + frame.Fingers.Count
		+ ", tools: " + frame.Tools.Count
		+ ", gestures: " + frame.Gestures ().Count);

		foreach (Hand hand in frame.Hands) {
			Debug.Log ("  Hand id: " + hand.Id
			+ ", palm position: " + hand.PalmPosition);
			// Get the hand's normal vector and direction
			Vector normal = hand.PalmNormal;
			Vector direction = hand.Direction;

			// Calculate the hand's pitch, roll, and yaw angles
			Debug.Log ("  Hand pitch: " + direction.Pitch * 180.0f / (float)Mathf.PI + " degrees, "
			+ "roll: " + normal.Roll * 180.0f / (float)Mathf.PI + " degrees, "
			+ "yaw: " + direction.Yaw * 180.0f / (float)Mathf.PI + " degrees");

			// Get the Arm bone
			Arm arm = hand.Arm;
			Debug.Log ("  Arm direction: " + arm.Direction
			+ ", wrist position: " + arm.WristPosition
			+ ", elbow position: " + arm.ElbowPosition);

			// Get fingers
			foreach (Finger finger in hand.Fingers) {
				Debug.Log ("    Finger id: " + finger.Id
				+ ", " + finger.Type ()
				+ ", length: " + finger.Length
				+ "mm, width: " + finger.Width + "mm"
				+ "position: " + finger.TipPosition);

				// Get finger bones
//				Bone bone;
//				foreach (Bone.BoneType boneType in (Bone.BoneType[]) Enum.GetValues(typeof(Bone.BoneType)))
//				{
//					bone = finger.Bone(boneType);
//					Debug.Log("  Bone: " + boneType
//						+ ", start: " + bone.PrevJoint
//						+ ", end: " + bone.NextJoint
//						+ ", direction: " + bone.Direction);
//				}
			}

		}
	}
}
