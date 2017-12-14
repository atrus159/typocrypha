﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// manages battle sequences
public class BattleManager : MonoBehaviour {
	public static BattleManager main = null; // static instance accessible globally
	public GameObject spellDict; // spell dictionary object
	public GameObject enemy_prefab; // prefab for enemy object
	public EnemyChargeBars charge_bars; // creates and mananges charge bars
	public CooldownList cooldown_list; // creates and manages player's cooldowns
	public Transform target_ret; // shows where target is
	public float enemy_spacing; // space between enemies
	public bool pause; // is battle paused?
	public Enemy[] enemy_arr; // array of Enemy components (size 3)
    public int target_ind; // index of currently targeted enemy
    public ICaster[] player_arr = { null, Player.main, null }; // array of Player and allies (size 3)
    public int player_ind = 1;
	public int enemy_count; // number of enemies in battle

	void Awake() {
		if (main == null) main = this;
		pause = false;
	}

	// start battle scene
	public void startBattle(BattleScene scene) {
		Debug.Log ("Battle!");
		enemy_arr = new Enemy[3];
		enemy_count = scene.enemy_stats.Length;
		charge_bars.initChargeBars ();
		for (int i = 0; i < scene.enemy_stats.Length; i++) {
			GameObject new_enemy = GameObject.Instantiate (enemy_prefab, transform);
			new_enemy.transform.localScale = new Vector3 (1, 1, 1);
			new_enemy.transform.localPosition = new Vector3 (i * enemy_spacing, 0, 0);
			enemy_arr [i] = new_enemy.GetComponent<Enemy> ();
			enemy_arr [i].setStats (scene.enemy_stats [i]);
            enemy_arr [i].field = this; //Give enemey access to field (for calling spellcasts
            enemy_arr [i].position = i;      //Log enemy position in field
            enemy_arr[i].bars = charge_bars; //Give enemy access to charge_bars
			Vector3 bar_pos = new_enemy.transform.position;
			bar_pos.Set (bar_pos.x, bar_pos.y + 1, bar_pos.z);
			charge_bars.makeChargeMeter(i, bar_pos);
		}
		pause = false;
		target_ind = 0;
		AudioPlayer.main.playMusic (MusicType.BATTLE, scene.music_tracks[0]);
	}

	// check if player switches targets or attacks
	void Update() {
		if (Input.GetKeyDown (KeyCode.BackQuote)) // toggle pause
			pause = !pause;
		if (pause) return;
		int old_ind = target_ind;
		// move target left or right
		if (Input.GetKeyDown (KeyCode.LeftArrow)) --target_ind;
		if (Input.GetKeyDown (KeyCode.RightArrow)) ++target_ind;
		// fix if target is out of bounds
		if (target_ind < 0) target_ind = 0;
		if (target_ind > 2) target_ind = 2;
		// move target reticule
		target_ret.localPosition = new Vector3 (target_ind * enemy_spacing, -1, 0);
		// play effect sound if target was moved
		if (old_ind != target_ind) AudioPlayer.main.playSFX(0, SFXType.UI, "sfx_enemy_select");
	}

	// attack currently targeted enemy with spell
	public void attackCurrent(string spell) {
        //Can attack dead enemies now, just wont cast spell at them
		StartCoroutine (pauseAttackCurrent (spell));
    }

	// pause for player attack, play animations, unpause
	IEnumerator pauseAttackCurrent(string spell){
		pause = true;
		BattleEffects.main.setDim (true, enemy_arr[target_ind].enemy_sprite);

        //BEGIN PAUSE//

        yield return new WaitForSeconds (1.5f);

        //MOVE TO CAST AND PROCESS WHEN POSSIBLE/IF YOU KNOW HOW
        AudioPlayer.main.playSFX(1, SFXType.SPELL, "Cutting_SFX");
        AnimationPlayer.main.playAnimation(AnimationType.SPELL, "cut", enemy_arr[target_ind].transform.position, false);

        //SPELLCASTING AND CASTDATA PROCESSING HERE//

        SpellData s;
        SpellDictionary d = spellDict.GetComponent<SpellDictionary>();
		//Send spell, Enemy state, and target index to parser and caster
        CastStatus status = d.parse(spell.ToLower(),  out s);
        //Set last_cast
        ((Player)player_arr[player_ind]).Last_cast = s.ToString();
        //Cast/Botch/Cooldown/Fizzle, with associated effects and processing
        playerCast(d, s, status);

        //END SPELLCASTING AND CASTDATA PROCESSING//

        yield return new WaitForSeconds (1f);

        //END PAUSE//

		BattleEffects.main.setDim (false, enemy_arr [target_ind].enemy_sprite);
        updateEnemies();
		pause = false;
	}

    //Casts from an ally position at target enemy_arr[target]: calls processCast on results
    public void NPC_Cast(SpellDictionary dict, SpellData s, int position, int target)
    {
        List<CastData> data = dict.cast(s, enemy_arr, target, player_arr, position);
        processCast(data, s);
    }
    //Casts from an enemy position: calls processCast on results
    public void enemyCast(SpellDictionary dict, SpellData s, int position)
    {
        List<CastData> data = dict.cast(s, player_arr, player_ind, enemy_arr, position);
        processCast(data, s);
    }
    //Cast/Botch/Cooldown/Fizzle, with associated effects and processing
    //all animation and attack effects should be processed here
    //ONLY CALL FOR A PLAYER CAST
    //Pre: CastStatus is generated by dict.Parse()
    private void playerCast(SpellDictionary dict, SpellData s, CastStatus status)
    {
        List<CastData> data;
        switch (status)//Switched based on caststatus
        {
            case CastStatus.SUCCESS:
                dict.startCooldown(s, (Player)player_arr[player_ind]);
                data = dict.cast(s, enemy_arr, target_ind, player_arr, player_ind);
                processCast(data, s);
                break;
            case CastStatus.BOTCH:
                data = dict.botch(s, enemy_arr, target_ind, player_arr, player_ind);
                Debug.Log("Botched cast: " + s.ToString());
                //Process the data here
                break;
            case CastStatus.ONCOOLDOWN:
                //Handle effects
                Debug.Log("Cast failed: " + s.root.ToUpper() + " is on cooldown for " + dict.getTimeLeft(s) + " seconds");
                break;
            case CastStatus.COOLDOWNFULL:
                //Handle effects
                Debug.Log("Cast failed: cooldownList is full!");
                break;
            case CastStatus.FIZZLE:
                //Handle effects
                break;
        }
    }
    //Method for processing CastData (where all the effects happen)
    //Called by Cast in the SUCCESS CastStatus case, possibly on BOTCH in the future
    //Can be used to process the cast of an enemy or ally, if implemented (put the AI loop in battlemanager)
    private void processCast(List<CastData> data, SpellData s)
    {
        //Process the data here
        foreach (CastData d in data)
        {
            if (d.Target.CasterType == ICasterType.PLAYER)
            {
                //Implement stuff for when player hits themselves
                if (d.isHit == false)//Spell misses
                {
                    Debug.Log(d.Caster.Stats.name + " missed " + d.Target.Stats.name + "!");
                    //Process miss graphics
                }
                else//Spell hits
                {
                    //Process hit graphics

                    if (d.isCrit)//Spell is crit
                    {
                        Debug.Log(d.Caster.Stats.name + " scores a critical with " + s.ToString() + " on " + d.Target.Stats.name);
                        //process crit graphics
                    }
                    Debug.Log(d.Target.Stats.name + " was hit for " + d.damageInflicted + " " + Elements.toString(d.element) + " damage x" + d.Target.Stats.vsElement[d.element]);
                    //Process elemental wk/resist/absorb/reflect graphics
                    //Process damage graphics
                }
            }
            else if (d.Target.CasterType == ICasterType.NPC_ALLY)
            {
                Debug.Log("ALLY " + d.Target.Stats.name + " has been cast at!");
                //Implement Graphics for cast at NPC_ALLY (NOT YET POSSIBLE)
            }
            else//Target is an ENEMY
            {
                Enemy e = (Enemy)d.Target;
                if (d.isHit == false)//Spell misses
                {
                    Debug.Log(d.Caster.Stats.name + " missed " + d.Target.Stats.name + "!");
                    //Process miss graphics
                }
                else//Spell hits
                {
                    //Process hit graphics

                    if (d.isCrit)//Spell is crit
                    {
                        Debug.Log(d.Caster.Stats.name + " scores a critical with " + s.ToString() + " on " + d.Target.Stats.name);
                        //process crit graphics
                    }
                    if (d.isStun)
                    {
                        //Process stun graphics
                        Debug.Log(d.Caster.Stats.name + " stuns " + d.Target.Stats.name);
                        AudioPlayer.main.playSFX(2, SFXType.BATTLE, "sfx_stagger");
                        charge_bars.Charge_bars[e.position].gameObject.transform.GetChild(0).GetComponent<Image>().color = new Color(1, 0.5F, 0);
                    }
                    Debug.Log(d.Target.Stats.name + " was hit for " + d.damageInflicted + " " + Elements.toString(d.element) + " damage x" + d.Target.Stats.vsElement[d.element]);
                    //Process elemental wk/resist/absorb/reflect graphics
                    //Process damage graphics
                }
            }
        }
    }

    //Updates death and opacity of enemies after pause in puaseAttackCurrent
    private void updateEnemies()
    {
		int curr_dead = 0;
        for(int i = 0; i < enemy_arr.Length; i++)
        {
            if(!enemy_arr[i].Is_dead)
			    enemy_arr [i].updateCondition ();
			else
                ++curr_dead;
        }
		if (curr_dead == enemy_count) // end battle if all enemies dead
		{
			Debug.Log("you win!");
			cooldown_list.removeAll ();
			StartCoroutine(StateManager.main.nextSceneDelayed(2.0f));
		}
    }
	//removes all enemies and charge bars
	public void stopBattle() {
		pause = true;
		foreach (Enemy enemy in enemy_arr) {
			if (enemy != null) GameObject.Destroy (enemy.gameObject);
		}
		enemy_arr = null;
		cooldown_list.removeAll ();
		charge_bars.removeAll ();
	}
}
