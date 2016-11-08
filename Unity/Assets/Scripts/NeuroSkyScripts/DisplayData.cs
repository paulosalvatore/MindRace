using UnityEngine;
using System.Collections;

public class DisplayData : MonoBehaviour
{
	public Texture2D[] signalIcons;
	public bool esconderGUI;

	private int indexSignalIcons = 1;

	TGCConnectionController controller;

	private int poorSignal;
	private int attention;
	private int meditation;

	private float delta;

	void Start()
	{
		controller = GameObject.Find("NeuroSkyTGCController").GetComponent<TGCConnectionController>();

		controller.UpdatePoorSignalEvent += OnUpdatePoorSignal;
		controller.UpdateAttentionEvent += OnUpdateAttention;
		controller.UpdateMeditationEvent += OnUpdateMeditation;

		controller.UpdateDeltaEvent += OnUpdateDelta;
	}

	void OnUpdatePoorSignal(int value)
	{
		poorSignal = value;
		if (value < 25)
			indexSignalIcons = 0;
		else if (value >= 25 && value < 51)
			indexSignalIcons = 4;
		else if (value >= 51 && value < 78)
			indexSignalIcons = 3;
		else if (value >= 78 && value < 107)
			indexSignalIcons = 2;
		else if (value >= 107)
			indexSignalIcons = 1;
	}

	void OnUpdateAttention(int value)
	{
		attention = value;
	}

	void OnUpdateMeditation(int value)
	{
		meditation = value;
	}

	void OnUpdateDelta(float value)
	{
		delta = value;
	}
	
	void OnGUI()
	{
		if (esconderGUI)
			return;

		GUILayout.BeginHorizontal();
		
		if (GUILayout.Button("Connect"))
		{
			controller.Connect();
		}
		if (GUILayout.Button("DisConnect"))
		{
			controller.Disconnect();
			indexSignalIcons = 1;
		}

		GUILayout.Space(Screen.width - 250);
		GUILayout.Label(signalIcons[indexSignalIcons]);

		GUILayout.EndHorizontal();
		
		GUILayout.Label("PoorSignal1:" + poorSignal);
		GUILayout.Label("Attention1:" + attention);
		GUILayout.Label("Meditation1:" + meditation);
		GUILayout.Label("Delta:" + delta);
	}
}
