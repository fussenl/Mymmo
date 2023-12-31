﻿using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Data;
using Entities;
using Models;
using UnityEngine;
using Managers;

namespace Services
{
    class MapService : Singleton<MapService>, IDisposable
    {
        public MapService()
        {
            MessageDistributer.Instance.Subscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Subscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);

            MessageDistributer.Instance.Subscribe<MapEntitySyncResponse>(this.OnEntitySync);

            SceneManager.Instance.onSceneLoadDone += OnLoadDone;
        }

        public int CurrentMapId { get; set; }
        private bool loadingDone = true;
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Unsubscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            SceneManager.Instance.onSceneLoadDone -= OnLoadDone;

            //MessageDistributer.Instance.Unsubscribe<MapEntitySyncResponse>(this.OnEntitySync);
        }

        public void Init()
        {

        }
        void OnMapCharacterEnter(object sender, MapCharacterEnterResponse response)
        {
            Debug.LogFormat("OnMapCharacterEnter:Map:{0} Count:{1}", response.mapId, response.Characters.Count);
            foreach (var cha in response.Characters)
            {
                if (User.Instance.CurrentCharacterInfo == null || (cha.Type == CharacterType.Player && User.Instance.CurrentCharacterInfo.Id == cha.Id))
                {
                    //当前角色切换地图成功
                    User.Instance.CurrentCharacterInfo = cha;
                    if (User.Instance.CurrentCharacter == null)
                        User.Instance.CurrentCharacter = new Character(cha);
                    else //不是第一次进游戏，而是换地图就更新角色信息
                        User.Instance.CurrentCharacter.UpdateInfo(cha);

                    User.Instance.CurrentCharacter.ready = false;
                    User.Instance.CharacterInited();
                    CharacterManager.Instance.AddCharacter(User.Instance.CurrentCharacter);

                    if (CurrentMapId != response.mapId)
                    {
                        this.EnterMap(response.mapId);
                        this.CurrentMapId = response.mapId;
                    }

                    continue;
                }

                CharacterManager.Instance.AddCharacter(new Character(cha));
            }

        }

        void OnMapCharacterLeave(object sender, MapCharacterLeaveResponse response)
        {
            Debug.LogFormat("OnMapCharacterLeave:CharID:{0}", response.entityId);
            if (response.entityId != User.Instance.CurrentCharacterInfo.EntityId)
            {
                CharacterManager.Instance.RemoveCharacter(response.entityId);
            }
            else
            {
                if (User.Instance.CurrentCharacterObject != null)
                {
                    User.Instance.CurrentCharacterObject.OnLeavelevel();
                }
                CharacterManager.Instance.Clear();
            }
        }

        void EnterMap(int mapId)
        {
            if (DataManager.Instance.Maps.ContainsKey(mapId))
            {
                loadingDone = false;
                MapDefine map = DataManager.Instance.Maps[mapId];
                User.Instance.CurrentMapData = map;
                SceneManager.Instance.LoadScene(map.Resource);
                SoundManager.Instance.PlayMusic(map.Music);
            }
            else
                Debug.LogErrorFormat("EnterMap: Map {0} not existed", mapId);
        }

        public void SendMapEntitySync(EntityEvent entityEvent, NEntity entity, int param)
        {
            if(!loadingDone)return;
            Debug.LogFormat("SendMapEntitySync :EntityID{0} POS:{1} DIR:{2} SPD:{3}", entity.Id, entity.Position.String(), entity.Direction.String(), entity.Speed);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapEntitySync = new MapEntitySyncRequest();
            message.Request.mapEntitySync.entitySync = new NEntitySync()
            {
                Id = entity.Id,
                Event = entityEvent,
                Entity = entity,
                Param = param
            };
            NetClient.Instance.SendMessage(message);
        }

        void OnEntitySync(object sender, MapEntitySyncResponse response)
        {
            if (!loadingDone) return;
            System.Text.StringBuilder sb = new StringBuilder();
            sb.AppendFormat("OnEntitySync: Entitys:{0}", response.entitySyncs.Count);
            sb.AppendLine();
            foreach (var entity in response.entitySyncs)
            {
                EntityManager.Instance.OnEntitySync(entity);
                sb.AppendFormat("   [{0}]evt:{1} entity:{2}", entity.Id, entity.Event, entity.Entity.String());
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }
        public void SendMapTeleport(int teleporterID)
        {
            Debug.LogFormat("MapTeleportRequest :teleporterID:{0}", teleporterID);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapTeleport = new MapTeleportRequest();
            message.Request.mapTeleport.teleporterId = teleporterID;
            NetClient.Instance.SendMessage(message);
        }


        private void OnLoadDone()
        {
            if (User.Instance.CurrentCharacter != null)
                User.Instance.CurrentCharacter.ready = true;

            if (User.Instance.CurrentCharacterObject != null)
            {
                User.Instance.CurrentCharacterObject.OnEnterlevel();
            }

            loadingDone = true;
        }

    }
}
