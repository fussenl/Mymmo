﻿using Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Battle;
using Common.Battle;
using Managers;
using Models;
using SkillBridge.Message;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISkillSlot : MonoBehaviour, IPointerClickHandler
{

    public Image icon;
    public Image overlay;
    public Text cdText;
    private Skill skill;

    void Start()
    {
        overlay.enabled = false;
        cdText.enabled = false;
    }

    void Update()
    {
        if (this.skill == null) return;

        if (this.skill.CD > 0)
        {
            if (!overlay.enabled)
            {
                overlay.enabled = true;
            }
            if (!cdText.enabled)
            {
                cdText.enabled = true;
            }

            overlay.fillAmount = this.skill.CD / this.skill.Define.CD;
            this.cdText.text = ((int)Math.Ceiling(this.skill.CD)).ToString();
        }
        else
        {
            if (overlay.enabled)
            {
                overlay.enabled = false;
            }

            if (this.cdText.enabled)
            {
                this.cdText.enabled = false;
            }
        }
    }

    public void OnPositionSelected(Vector3 pos)
    {
        BattleManager.Instance.CurrentPosition = GameObjectTool.WorldToLogicN(pos);
        this.CanSkill();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (skill.Define.CastTarget==TargetType.Position)
        {
            TargetSelector.ShowSelector(User.Instance.CurrentCharacter.position, this.skill.Define.CastRange, this.skill.Define.AOERange,OnPositionSelected);
            return;
        }
        CanSkill();
    }
    private void CanSkill()
    {
        SkillResult result = this.skill.CanCast(BattleManager.Instance.CurrentTarget);

        switch (result)
        {
            case SkillResult.InvalidTarget:
                MessageBox.Show("技能" + this.skill.Define.Name + "目标无效");
                return;
            case SkillResult.CoolDown:
                MessageBox.Show("技能" + this.skill.Define.Name + "正在冷却");
                return;
            case SkillResult.OutOFRANGE:
                MessageBox.Show("技能" + this.skill.Define.Name + "释放范围不足");
                return;

            case SkillResult.OutOfMp:
                MessageBox.Show("技能" + this.skill.Define.Name + "MP不足");
                return;

        }
        BattleManager.Instance.CastSkill(this.skill);
    }


    internal void SetSkill(Skill value)
    {
        this.skill = value;
        if (this.icon != null)
        {
            this.icon.overrideSprite = Resloader.Load<Sprite>(this.skill.Define.Icon);
            this.icon.SetAllDirty();
        }
    }

}
