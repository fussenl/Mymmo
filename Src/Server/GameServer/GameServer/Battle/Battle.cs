﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Core;
using GameServer.Entities;
using GameServer.Managers;
using GameServer.Models;
using Network;
using SkillBridge.Message;

namespace GameServer.Battle
{
    public class Battle
    {
        public Map Map;

        /// <summary>
        /// 所有参加战斗的单位
        /// </summary>
        private Dictionary<int, Creature> AllUnits = new Dictionary<int, Creature>();

        private Queue<NSkillCastInfo> Actions = new Queue<NSkillCastInfo>();

        /// <summary>
        /// 死亡的单位
        /// </summary>
        private List<Creature> DeahPool = new List<Creature>();

        private List<NSkillHitInfo> Hits = new List<NSkillHitInfo>();
        private List<NBuffInfo> BuffActions = new List<NBuffInfo>();
        private List<NSkillCastInfo> CastSkills = new List<NSkillCastInfo>();


        public Battle(Map map)
        {
            this.Map = map;
        }

        public void ProcessBattleMessage(NetConnection<NetSession> sender, SkillCastRequest request)
        {
            Character character = sender.Session.Character;
            if (request.castInfo != null)
            {
                if (character.entityId != request.castInfo.casterId)
                    return;
                this.Actions.Enqueue(request.castInfo);
            }
        }

        public void Update()
        {
            this.CastSkills.Clear();
            this.Hits.Clear();
            this.BuffActions.Clear();
            if (this.Actions.Count > 0)
            {
                NSkillCastInfo skillCast = this.Actions.Dequeue();
                this.ExecuteAction(skillCast);
            }

            this.UpdateUnits();

            this.BroadcastHitsMessage();
        }

        private void ExecuteAction(NSkillCastInfo cast)
        {
            BattleContext context = new BattleContext(this);
            context.Caster = EntityManager.Instance.GetCreature(cast.casterId);
            context.Target = EntityManager.Instance.GetCreature(cast.targetId);
            context.CastSkill = cast;
            if (context.Caster != null)
                this.JoinBattle(context.Caster);
            if (context.Target != null)
                this.JoinBattle(context.Target);

            context.Caster.CastSkill(context, cast.skillId);

           /*弃用 NetMessageResponse message = new NetMessageResponse();
            message.skillCast = new SkillCastResponse();
            message.skillCast.castInfoes = context.CastSkill;
            message.skillCast.Result = context.Result == SkillResult.Ok ? Result.Success : Result.Failed;
            message.skillCast.Errormsg = context.Result.ToString();
            this.Map.BroadcasrBattleResponse(message);*/

        }

        public void JoinBattle(Creature unit)
        {
            this.AllUnits[unit.entityId] = unit;
        }

        public void LeaveBattle(Creature unit)
        {
            this.AllUnits.Remove(unit.entityId);
        }

        public List<Creature> FindUnitsInRange(Vector3Int pos, float range)
        {
            List<Creature> result = new List<Creature>();
            foreach (var unit in this.AllUnits)
            {
                if (unit.Value.Distance(pos) < range)
                {
                    result.Add(unit.Value);
                }
            }

            return result;
        }

        public List<Creature> FindUnitsInMapRange(Vector3Int pos, int range)
        {
            return EntityManager.Instance.GetMapEntitiesInRange<Creature>(this.Map.ID, pos, range);
        }

        public void AddCastSkillInfo(NSkillCastInfo cast)
        {
            this.CastSkills.Add(cast);
        }
        public void AddHitInfo(NSkillHitInfo hitInfo)
        {
            this.Hits.Add(hitInfo);
        }
        private void BroadcastHitsMessage()
        {
            if (this.Hits.Count == 0 && this.BuffActions.Count == 0&&this.CastSkills.Count==0) return;
            NetMessageResponse message = new NetMessageResponse();
            if (this.CastSkills.Count > 0)
            {
                message.skillCast = new SkillCastResponse();
                message.skillCast.castInfoes.AddRange(CastSkills);
                message.skillCast.Result = Result.Success;
                message.skillCast.Errormsg = "";
            }


            if (this.Hits.Count > 0)
            {
                message.skillHits = new SkillHitResponse();
                message.skillHits.Hits.AddRange(Hits);
                message.skillHits.Result = Result.Success;
                message.skillHits.Errormsg = "";
            }
            if (this.BuffActions.Count > 0)
            {
                message.buffRes = new BuffResponse();
                message.buffRes.Buffs.AddRange(this.BuffActions);
                message.buffRes.Result = Result.Success;
                message.buffRes.Errormsg = "";
            }


            this.Map.BroadcasrBattleResponse(message);

        }

        private void UpdateUnits()
        {
            this.DeahPool.Clear();
            foreach (var kv in this.AllUnits)
            {
                kv.Value.Update();
                if (kv.Value.IsDeath)
                {
                    this.DeahPool.Add(kv.Value);
                }
            }
            //更新完从战斗上清除
            foreach (var unit in this.DeahPool)
            {
                this.LeaveBattle(unit);
            }

        }
        public void AddBuffAction(NBuffInfo buff)
        {
            this.BuffActions.Add(buff);
        }

    }
}
