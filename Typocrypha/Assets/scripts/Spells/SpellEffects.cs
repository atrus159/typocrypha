﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellEffects : MonoBehaviour {
    public Popper popp; //holds popper script component
	public EnemyHealthBars enemy_hp_bars; // holds enemy HP bars component
    const float POP_TIMER = 1.5f; //pop-ups last this many seconds long
    Vector3 DMGNUM_OFFSET = new Vector3(0, 0.375f, 0); //where the damage number should be
    Vector3 UNDER_OFFSET = new Vector3(0, -0.75f, 0); //where something under the damage num should be
    Vector3 OVER_OFFSET = new Vector3(0, 1.5f, 0); //where something over the damage num should be
    Vector3 ELEM_OFFSET = new Vector3(-0.75f, 1.6f, 0);
    const float shakeIntensityMod = 0.1f; //The amount to scale each shake by (per keyword)

    // Use this for initialization
    void Start () {
		
	}

    public IEnumerator playEffects(CastData d, SpellData s)
    {
        if (d.isHit == false)//Spell misses
        {
            Debug.Log(d.Caster.Stats.name + " missed " + d.Target.Stats.name + "!");
            //Process miss graphics
            popp.spawnSprite("popup_miss", POP_TIMER, d.Target.Transform.position + UNDER_OFFSET);
            AudioPlayer.main.playSFX("sfx_spell_miss");
            BattleEffects.main.spriteShift(d.Target.Transform, 0.3f, 0.1f); // sprite moves to the right as a dodge
            yield break;
        }
        else if (d.Target.CasterType == ICasterType.ENEMY)// when enemy is hit
        {
            //learn their strength/weaknesses against the element used
            EnemyIntel.main.learnIntel(d.Target.Stats.name, d.element);
        }
        else if(d.Caster.CasterType == ICasterType.ENEMY && d.Target.CasterType == ICasterType.PLAYER && d.repel)
        {
            //learn their strength/weaknesses against the element used
            EnemyIntel.main.learnIntel(d.Caster.Stats.name, d.element);
        }

		//Process repel
		if (d.repel)
		{
			spawnElementPopup(d.element, Elements.vsElement.REPEL, d.Caster.Transform);
			AnimationPlayer.main.playAnimation("anim_element_reflect", d.Caster.Transform.position, 2f);
			yield return new WaitForSeconds(0.333F);
		}
        float shakeIntensity = 0;
		//Process hit graphics (if not a repelled attack)
		for (int i = 0; i < d.animData.Length; ++i)
		{
			if (d.animData[i] != null)
			{
				AudioPlayer.main.setSFX(AudioPlayer.channel_spell_sfx, d.sfxData[i]);
				AudioPlayer.main.playSFX(AudioPlayer.channel_spell_sfx);
				//AnimationPlayer.main.playAnimation(d.animData[i], d.Target.Transform.position, 1);
				AnimationPlayer.main.playAnimation(d.animData[i], d.Target.Transform.position, 2f);
                if(d.Target.CasterType == ICasterType.PLAYER)
                   BattleEffects.main.screenShake(0.15f + shakeIntensity/8, 0.05f + shakeIntensity);
                shakeIntensity += shakeIntensityMod;
				if (d.repel) {
					popp.spawnText (s.root.ToUpper()+"!", POP_TIMER, d.Caster.Transform.position, Color.black, new Color(1,111f/255f,1));
				} else {
					popp.spawnText (s.root.ToUpper()+"!", POP_TIMER, d.Caster.Transform.position, new Color(1,111f/255f,1), Color.white);
				}
                yield return new WaitForSeconds(0.333F);
			}
		}
        if (d.isCrit && d.elementalData != Elements.vsElement.BLOCK) {//Spell is crit
			Debug.Log (d.Caster.Stats.name + " scores a critical with " + s.ToString () + " on " + d.Target.Stats.name);
			if (d.Target.CasterType == ICasterType.ENEMY)
				AudioPlayer.main.playSFX ("sfx_enemy_weakcrit_dmg");
			else if (d.Target.CasterType == ICasterType.PLAYER || d.Target.CasterType == ICasterType.NPC_ALLY)
				AudioPlayer.main.playSFX ("sfx_party_weakcrit_dmg");
			//process crit graphics
			popp.spawnSprite ("popup_critical", POP_TIMER, d.Target.Transform.position + UNDER_OFFSET);
		} else if ((d.elementalData == Elements.vsElement.WEAK || d.elementalData == Elements.vsElement.SUPERWEAK) && d.damageInflicted > 0){
			if (d.Target.CasterType == ICasterType.ENEMY)
				AudioPlayer.main.playSFX ("sfx_enemy_weakcrit_dmg");
			else if (d.Target.CasterType == ICasterType.PLAYER || d.Target.CasterType == ICasterType.NPC_ALLY)
				AudioPlayer.main.playSFX ("sfx_party_weakcrit_dmg");
		}
        if (d.isStun)
        {
            //Process stun graphics
            Debug.Log(d.Caster.Stats.name + " stuns " + d.Target.Stats.name);
            AudioPlayer.main.playSFX("sfx_stagger");
        }

        Debug.Log(d.Target.Stats.name + " was hit for " + d.damageInflicted + " " + Elements.toString(d.element) + " damage x" + d.Target.Stats.getFloatVsElement(d.Target.BuffDebuff, d.element));

        //Process elemental wk/resist/drain/repel graphics
		if (!((d.elementalData == Elements.vsElement.WEAK || d.elementalData == Elements.vsElement.SUPERWEAK) && d.damageInflicted <= 0)) {
			spawnElementPopup(d.element, d.elementalData, d.Target.Transform);
		}

		//Play block/reflect/drain animations if necessary
		if (d.elementalData == Elements.vsElement.BLOCK) {
			AnimationPlayer.main.playAnimation("anim_element_block", d.Target.Transform.position, 2f);
			yield return new WaitForSeconds(0.333F);
		}
		else if (d.elementalData == Elements.vsElement.DRAIN) {
			AnimationPlayer.main.playAnimation("anim_element_drain", d.Target.Transform.position, 2f);
			yield return new WaitForSeconds(0.333F);
		}

        //Process damage graphics
		if (d.damageInflicted > 0) {
			BattleEffects.main.spriteShake (d.Target.Transform, 0.3f, 0.1f);
			AudioPlayer.main.playSFX ("sfx_spell_hit");
			Color dmgNumColor = Color.white;
			Color dmgNumColorTop = new Color(255f/255f, 0, 25f/255f);
			Color dmgNumColorBottom = new Color(255f/255f, 85f/255f, 85f/255f);
			if (d.Target.CasterType == ICasterType.PLAYER) {
				if (d.repel)
					dmgNumColor = new Color (255, 0, 255);//new Color(220, 86, 249);
				BattleEffects.main.screenShake (0.15f + shakeIntensity / 2, shakeIntensity + 0.3f);
			} else if (d.Target.CasterType == ICasterType.ENEMY) {
				// Gradually lower enemy HP gauge displays
				BattleManagerS.main.uiManager.setEnabledGauges(true);
				StartCoroutine (enemy_hp_bars.gradualUpdateDamage (d.Target.Position,d.damageInflicted));
			}
			// Set damage text size based on amount of damage ratios
			string sizeTagOpen = "<size=";
			string sizeTagClose = "</size>";
			int sizeValueMin = 28;
			int sizeValueMax = 72;
			int sizeValueDiff = sizeValueMax - sizeValueMin;
			float sizeRatio = (float)d.damageInflicted / (float)d.Target.Stats.max_hp;
			Debug.Log ("damage ratio to max hp: " + sizeRatio);
			int sizeValueCurr = sizeValueMin + Mathf.RoundToInt (((float)sizeValueDiff)*sizeRatio);
			string damageText = sizeTagOpen + sizeValueCurr + ">" + d.damageInflicted.ToString () + sizeTagClose;
			popp.spawnText ("-" + damageText + "<size=24>HP</size>", POP_TIMER, d.Target.Transform.position + DMGNUM_OFFSET, dmgNumColorTop, dmgNumColorBottom);
			if ((d.Target.Curr_hp <= 0) && (d.damageInflicted > (d.Target.Stats.max_hp))) {
				Vector3 ko_offset = new Vector3(0.5f, -0.5f, 0);
				popp.spawnText ("<size=28>OVERKILL!</size>", POP_TIMER, d.Target.Transform.position + DMGNUM_OFFSET + ko_offset, Color.yellow, Color.white);
			}
			else if (d.Target.Curr_hp <= 0) {
				Vector3 ko_offset = new Vector3(0.5f, -0.5f, 0);
				popp.spawnText ("<size=28>K.O.</size>", POP_TIMER, d.Target.Transform.position + DMGNUM_OFFSET + ko_offset, dmgNumColorTop, dmgNumColorBottom);
			}
		} else if (d.damageInflicted < 0) {
			string heal = "+" + (-1 * (d.damageInflicted)).ToString () + "<size=32>HP</size>";
			AudioPlayer.main.playSFX ("sfx_heal");
			popp.spawnText (heal, POP_TIMER, d.Target.Transform.position + DMGNUM_OFFSET, new Color(27f/255f, 195f/255f, 43f/255f));
		} else {
			if (d.elementalData == Elements.vsElement.BLOCK) {
				popp.spawnText ("NULL", POP_TIMER, d.Target.Transform.position + DMGNUM_OFFSET, Color.gray);
			} else {
				popp.spawnText (d.damageInflicted.ToString (), POP_TIMER, d.Target.Transform.position + DMGNUM_OFFSET);
			}
		}
		//Debug.Log (d.Target.Transform.position);
    }

    //Spawns elemental popup with proper icon
    private void spawnElementPopup(int element, Elements.vsElement vElem, Transform pos)
    {
        switch (vElem)
        {
            case Elements.vsElement.REPEL:
                popp.spawnSprite("popup_reflect", POP_TIMER, pos.position + OVER_OFFSET);
				AudioPlayer.main.playSFX ("sfx_spell_reflect");
                break;
            case Elements.vsElement.DRAIN:
                popp.spawnSprite("popup_absorb", POP_TIMER, pos.position + OVER_OFFSET);
				AudioPlayer.main.playSFX ("sfx_spell_drain");
                break;
            case Elements.vsElement.BLOCK:
                popp.spawnSprite("popup_nullify", POP_TIMER, pos.position + OVER_OFFSET);
				AudioPlayer.main.playSFX ("sfx_spell_block");
                break;
            case Elements.vsElement.RESIST:
                popp.spawnSprite("popup_resistant", POP_TIMER, pos.position + OVER_OFFSET);
				AudioPlayer.main.playSFX ("sfx_spell_resist");
                break;
            case Elements.vsElement.WEAK:
				popp.spawnSprite("popup_weak", POP_TIMER, pos.position + OVER_OFFSET);
                break;
            case Elements.vsElement.SUPERWEAK:
                popp.spawnSprite("popup_superweak", POP_TIMER, pos.position + OVER_OFFSET);
                break;
        }
        if (vElem != Elements.vsElement.NEUTRAL)
        {
            switch (element)
            {
                case 0:
                    popp.spawnSprite("popup_slash", POP_TIMER, pos.position + ELEM_OFFSET);
                    break;
                case 1:
                    popp.spawnSprite("popup_fire", POP_TIMER, pos.position + ELEM_OFFSET);
                    break;
                case 2:
                    popp.spawnSprite("popup_ice", POP_TIMER, pos.position + ELEM_OFFSET);
                    break;
                case 3:
                    popp.spawnSprite("popup_bolt", POP_TIMER, pos.position + ELEM_OFFSET);
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}
