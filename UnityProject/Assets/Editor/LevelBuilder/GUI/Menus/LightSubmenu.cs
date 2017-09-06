﻿using UnityEditor;
using UnityEngine;
using GeneratorNS;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Serializers;
using MapFeatures;

class LightSubmenu : IMenu
{		
	public LightData lightData = new LightData(new Vector2(0.0f, 0.0f));
	public bool addLight = false;

	private string lightX = "0";
	private string lightY = "0";

	public void Display()
	{
		addLight = EditorGUILayout.Toggle("Add light to your sprite", addLight);
		if (addLight) 
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label ("Relative position in the sprite");

			GUILayout.Label ("X: ");
			lightX = GUILayout.TextField(lightX, GUILayout.MaxWidth(30));
			GUILayout.Label ("Y: ");
			lightY = GUILayout.TextField(lightY, GUILayout.MaxWidth(30));

			GUILayout.EndHorizontal();
		
			try 
			{
				lightData.x = float.Parse(lightX);
				lightData.y = float.Parse(lightY);
			}
			catch (FormatException e)
			{
				Debug.Log (
					"Light x/y format exception." +
					"Please make sure you write an integer.");
			}
		}
	}
}
