﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO;
using UnitySocketIO.Events;
using SimpleJSON;

public class WorldControls : MonoBehaviour
{
	public bool debug;

	private const float ProcessingInterval = 0.75f;

	private float startTime;
	private Queue<JSONNode> dataQueue;

	public SocketIOController io;
	private int userId = 1;

	void Start()
	{
		if (Application.platform == RuntimePlatform.WebGLPlayer) 
		{
			// Ask the browsers for setup calls.
			// (See unity.html for clarifications.)
			Debug.Log("Sending message to WebGLPlayer.");
			Application.ExternalCall("SendAllConnect");
		}
		else 
		{
			// TEMPORARY: Otherwise connect directly. Just for testing.
			EstablishConnection();
		} 

		startTime = Time.time;
		dataQueue = new Queue<JSONNode>();

		WorldInit();
	}

	void Update()
	{
		float step = Time.time - startTime;

		if (dataQueue == null)
			return;

		if (step >= ProcessingInterval && dataQueue.Count > 0)
			ProcessUpdate();
	}

	// Socket setup.
	public void SetGameURL(string url)
	{
		io.settings.url = url;
	}

	public void SetGamePort(int port)
	{
		io.settings.port = port;
	}

	public void SetUserId(int userId)
	{
		this.userId = userId;
	}

	// The backend calls this function to open a socket connection.
	// Once this happens, the game starts.
	public void EstablishConnection()
	{
		io.ResetSettings();

		io.On("connect", (SocketIOEvent e) => 
		{
			Debug.Log("SocketIO Connected.");
		});

		io.Connect();

		io.On("world-init", (SocketIOEvent e) => 
		{
			Debug.Log("World init.");

			// So that the server knows that requests have started
			// being processed.
			io.Emit("client-ready", Convert.ToString(userId));

			Debug.Log("Emitted response.");
		});

		io.On("world-update", (SocketIOEvent e) => 
		{
			WorldUpdate(e.data);
		});
	}

	// Handle initial request and build the map.
	void WorldInit()
	{
		// TEMPORARY! For now we'll just have a massive plane. 
		GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
		floor.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
		floor.transform.localScale = new Vector3(10.0f, 10.0f, 10.0f);
		floor.GetComponent<Renderer>().material.color = Color.gray;
	}

	// Receive updates from the backend, parse them and delegate to the 
	// classes in charge of creating, deleting and updating game objects.
	void WorldUpdate(string updatesString)
	{
		JSONNode updates = JSON.Parse(updatesString);

		// TEMPORARY. We only subscribe to the relevant updates.
		if (userId == updates["main_player"].AsInt)
			dataQueue.Enqueue(updates);
	}

	// Manage the changes in the scene.
	void ProcessUpdate()
	{
		startTime = Time.time;
		JSONNode updates = dataQueue.Dequeue();

		// Players updates.
		JSONNode players = updates["players"];

		foreach (JSONNode player in players["create"].AsArray) 
			Player.Create(player["id"].AsInt, new PlayerData(player), debug);

		foreach (JSONNode player in players["delete"].AsArray)
			Player.Delete(player["id"].AsInt);

		foreach (JSONNode player in players["update"].AsArray)
			Player.Update(player["id"].AsInt, new PlayerData(player));

		// Map features updates.
		JSONNode mapFeatures = updates["map_features"];
		JSONNode healthPoints = mapFeatures["health_point"];
		JSONNode scorePoints = mapFeatures["score_point"];
		JSONNode pickups = mapFeatures ["pickup"];
		JSONNode obstacles = mapFeatures ["obstacle"];

		// Health points.
		foreach (JSONNode healthPoint in healthPoints["create"].AsArray)
			HealthPoint.Create(
				healthPoint["id"], 
				healthPoint["x"].AsFloat, 
				healthPoint["y"].AsFloat);

		foreach (JSONNode healthPoint in healthPoints["delete"].AsArray)
			HealthPoint.Delete(healthPoint["id"]);

		// Score points.
		foreach (JSONNode scorePoint in scorePoints["create"].AsArray) {
			ScorePoint.Create (
				scorePoint["id"], 
				scorePoint["x"].AsFloat, 
				scorePoint["y"].AsFloat);
		}

		foreach (JSONNode scorePoint in scorePoints["delete"].AsArray)
			ScorePoint.Delete(scorePoint["id"]);

		// Pickups.
		foreach (JSONNode pickup in pickups["create"].AsArray)
			Pickup.Create(
				pickup["id"], 
				pickup["x"].AsFloat, 
				pickup["y"].AsFloat);

		foreach (JSONNode pickup in pickups["delete"].AsArray)
			Pickup.Delete(pickup["id"]);

		// Obstacles.
		foreach (JSONNode obstacle in obstacles["create"].AsArray)
			Obstacle.Create(
				obstacle["id"], 
				obstacle["x"].AsFloat, 
				obstacle["y"].AsFloat);

		foreach (JSONNode obstacle in obstacles["delete"].AsArray)
			Obstacle.Delete(obstacle["id"]);
	}

	// Tell the server when we get out of the game.
	void OnApplicationQuit()
	{
		io.Emit("exit-game", Convert.ToString(userId));
	}

	// Detect focus. If there is focus, process everything fast.
	/*void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus && dataQueue != null) 
			while (dataQueue.Count > 1)
				ProcessUpdate();
	}*/
}