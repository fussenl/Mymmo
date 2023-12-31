﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;
using Managers;
using Network;
using SkillBridge.Message;
using UnityEngine;

namespace Services
{
    public class BattleService : Singleton<BattleService>, IDisposable
    {
        public void Init()
        {

        }

        public BattleService()
        {
            MessageDistributer.Instance.Subscribe<SkillCastResponse>(OnSkillCast);
            MessageDistributer.Instance.Subscribe<SkillHitResponse>(OnSkillHit);
            MessageDistributer.Instance.Subscribe<BuffResponse>(OnBuff);

        }


        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<SkillCastResponse>(OnSkillCast);
            MessageDistributer.Instance.Unsubscribe<SkillHitResponse>(OnSkillHit);
            MessageDistributer.Instance.Unsubscribe<BuffResponse>(OnBuff);

        }

        public void SendSkillCast(int skillId, int casterId, int targetId, NVector3 position)
        {
            if (position == null)
            {
                position = new NVector3();
            }
            Debug.LogFormat($"SendSkillCast: skill: {skillId} caster:{casterId} target:{targetId} pos:{position.ToString()}");
            var message = new NetMessage
            {
                Request = new NetMessageRequest
                {
                    skillCast = new SkillCastRequest
                    {
                        castInfo = new NSkillCastInfo
                        {
                            skillId = skillId,
                            casterId = casterId,
                            targetId = targetId,
                            Position = position
                        }
                    }
                }
            };
            NetClient.Instance.SendMessage(message);
        }

        private void OnSkillCast(object sender, SkillCastResponse message)
        {
            if (message.Result == Result.Success)
            {
                foreach (var castInfo in message.castInfoes)
                {
                    Debug.LogFormat("OnSkillCast: skill:{0} caster:{1} target:{2} pos:{3} result:{4}", castInfo.skillId,
                        castInfo.casterId, castInfo.targetId, castInfo.Position.String(),
                        message.Result);

                    Creature caster = EntityManager.Instance.GetEntity(castInfo.casterId) as Creature;
                    if (caster != null)
                    {
                        Creature target = EntityManager.Instance.GetEntity(castInfo.targetId) as Creature;
                        caster.CastSkill(castInfo.skillId, target, castInfo.Position);
                    }

                }
            }
            else
            {
                ChatManager.Instance.AddSystemMessage(message.Errormsg);
            }
        }


        private void OnSkillHit(object sender, SkillHitResponse message)
        {
            Debug.LogFormat("OnSkillHit: count:{0}",message.Hits.Count);
            if (message.Result==Result.Success)
            {
                foreach (var hit in message.Hits)
                {
                    Creature caster = EntityManager.Instance.GetEntity(hit.casterId) as Creature;
                    if (caster!=null)
                    {
                        caster.DoSkillHit(hit);
                    }
                }
            }
        }

        private void OnBuff(object sender, BuffResponse message)
        {
            Debug.LogFormat("OnBuff: count：{0}",message.Buffs.Count);

            foreach (var buff in message.Buffs)
            {
                Debug.LogFormat("  Buff:{0} :{1} [{2}]",buff.buffId,buff.buffType,buff.Action);
                Creature owner=EntityManager.Instance.GetEntity(buff.ownerId)as Creature;
                if (owner != null)
                {
                    owner.DoBuffAction(buff);
                }
            }
        }

    }
}
