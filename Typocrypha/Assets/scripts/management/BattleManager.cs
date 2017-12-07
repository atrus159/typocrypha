﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// manages battle sequences
public class BattleManager : MonoBehaviour {
	public StateManager state_manager; // manages global state/scenes
	public GameObject enemy_prefab; // prefab for enemy object
	public int target_ind; // index of currently targeted enemy
	public Transform target_ret; // shows where target is
	Enemy[] enemy_arr; // array of Enemy components (size 3)

	// start battle scene
	public void startBattle(BattleScene scene) {
		Debug.Log ("Battle! (goes on infinitely)");
		enemy_arr = new Enemy[3];
		for (int i = 0; i < scene.enemy_stats.Length; i++) {
			GameObject new_enemy = GameObject.Instantiate (enemy_prefab, transform);
			new_enemy.transform.localScale = new Vector3 (1, 1, 1);
			new_enemy.transform.localPosition = new Vector3 (i * 3, 0, 0);
			enemy_arr [i] = new_enemy.GetComponent<Enemy> ();
			enemy_arr [i].setStats (scene.enemy_stats [i]);
		}
		target_ind = 0;
	}

	// check if player switches targets or attacks
	void Update() {
		// move target left or right
		if (Input.GetKeyDown (KeyCode.LeftArrow)) --target_ind;
		if (Input.GetKeyDown (KeyCode.RightArrow)) ++target_ind;
		// fix if target is out of bounds
		if (target_ind < 0) target_ind = 0;
		if (target_ind > 2) target_ind = 2;
		// move target reticule
		target_ret.localPosition = new Vector3 (target_ind * 3, -1, 0);
	}

	// attack currently targeted enemy with spell
	public void attackCurrent(string spell) {
		// TEMP: just get 'attacked' by whatever player typed
		enemy_arr[target_ind].beAttacked(spell); 
	}
}
