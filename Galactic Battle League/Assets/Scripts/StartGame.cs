﻿using UnityEngine;
using System.Collections;

public class StartGame : MonoBehaviour 
{

	public void BeginCharacterSelect () 
	{
		Application.LoadLevel ("CharacterSelect");
	}

	public void BeginLevelSelect () 
	{
		Application.LoadLevel ("LevelSelectMenu");
	}

	public void BeginMainStage () 
	{
		PlayerPrefs.SetString ("Level", "arena");
		Application.LoadLevel ("LoadScreen");
	}

	public void BeginAltitude () 
	{
		PlayerPrefs.SetString ("Level", "Altitude");
		Application.LoadLevel ("LoadScreen");
	}

	public void BeginCityscape ()
	{
		PlayerPrefs.SetString ("Level", "arena04");
		Application.LoadLevel ("LoadScreen");
	}

	public void BeginCrucible ()
	{
		PlayerPrefs.SetString ("Level", "Crucible");
		Application.LoadLevel ("LoadScreen");
	}

	public void BeginMainMenu () 
	{
		GameObject music = GameObject.Find("MenuMusic");
		if (music) {
			GameObject.Destroy(music);
		}
		Application.LoadLevel ("MainMenu");
	}

	public void BeginControls ()
	{
		Application.LoadLevel ("Controls");
	}

	public void BeginWinScreen()
	{
		Application.LoadLevel ("WinScreen");
	}

	public void BeginWinScoreboard()
	{
		Application.LoadLevel ("WinScoreboard");
	}

	public void BeginCredits()
	{
		Application.LoadLevel ("Credits");
	}

	public void EndGame()
	{
		Application.Quit();
	}
}

