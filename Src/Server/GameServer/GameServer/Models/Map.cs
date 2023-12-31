﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Data;
using GameServer.Entities;
using GameServer.Managers;
using GameServer.Services;
using Network;
using SkillBridge.Message;

namespace GameServer.Models
{
    public class Map
    {
        internal class MapCharacter
        {
            public NetConnection<NetSession> Connection;
            public Character character;

            public MapCharacter(NetConnection<NetSession> conn, Character cha)
            {
                this.Connection = conn;
                this.character = cha;
            }
        }

        public int ID
        {
            get { return this.Define.ID; }
        }

        public int InstanceID { get; set; }
        internal MapDefine Define;

        /// <summary>
        /// 地图中的角色，以CharacterId为Key
        /// </summary>
        Dictionary<int, MapCharacter> MapCharacters = new Dictionary<int, MapCharacter>();


        private SpawnManager spawnManager = new SpawnManager();

        public Battle.Battle Battle;

        public MonsterManager MonsterManager = new MonsterManager();


        internal Map(MapDefine define, int instanceId)
        {
            this.Define = define;
            this.InstanceID = instanceId;
            this.spawnManager.Init(this);
            this.MonsterManager.Init(this);
            this.Battle = new Battle.Battle(this);
        }

        internal void Update()
        {
            spawnManager.Update();
            Battle.Update();
        }

        /// <summary>
        /// 角色进入地图
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="character"></param>
        internal void CharacterEnter(NetConnection<NetSession> conn, Character character)
        {
            Log.InfoFormat("CharacterEnter:Map:{0} characterId:{1}", this.Define.ID, character.Id);

            this. AddCharacter(conn, character);

            conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;

            foreach (var kv in this.MapCharacters)
            {
                conn.Session.Response.mapCharacterEnter.Characters.Add(kv.Value.character.Info);
                if (kv.Value.character != character)
                {
                    this.AddCharacterEnterMap(kv.Value.Connection, character.Info);
                }
            }
            foreach (var kv in this.MonsterManager.Monsters)
            {
                conn.Session.Response.mapCharacterEnter.Characters.Add(kv.Value.Info);
            }
            conn.SendResponse();

        }

        public void AddCharacter(NetConnection<NetSession> conn, Character character)
        {
            Log.InfoFormat("AddCharacter:Map:{0} characterId:{1}", this.Define.ID, character.Id);
            character.OnEnterMap(this);
            character.Info.mapId = this.ID;
            if (!this.MapCharacters.ContainsKey(character.Id))
            {
                this.MapCharacters[character.Id] = new MapCharacter(conn, character);
            }
        }

        internal void CharacterLeave(Character cha)
        {
            cha.OnLeaveMap(this);
            Log.InfoFormat("CharacterLeave:Map:{0} characterId:{1}", this.Define.ID, cha.Id);
            foreach (var kv in this.MapCharacters)
            {
                this.SendCharacterLeaveMap(kv.Value.Connection, cha);
            }
            this.MapCharacters.Remove(cha.Id);
        }
        void AddCharacterEnterMap(NetConnection<NetSession> conn, NCharacterInfo character)
        {
            if (conn.Session.Response.mapCharacterEnter == null)
            {
                conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
                conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;
            }

            conn.Session.Response.mapCharacterEnter.Characters.Add(character);
            conn.SendResponse();
        }

        void SendCharacterLeaveMap(NetConnection<NetSession> conn, Character character)
        {
            Log.InfoFormat($"SendCharacterLeaveMap to {{0}}:{{1}} : Map:{{2}}Character:{{3}}:{{4}}", conn.Session.Character.Id, conn.Session.Character.Info.Name, this.Define.ID, character.Id, character.Info.Name);

            conn.Session.Response.mapCharacterLeave = new MapCharacterLeaveResponse();
            conn.Session.Response.mapCharacterLeave.entityId = character.entityId;

            conn.SendResponse();
        }


        /// <summary>
        /// 同步
        /// </summary>
        internal void UpdateEntity(NEntitySync entity)
        {
            foreach (var kv in this.MapCharacters)
            {
                if (kv.Value.character.entityId == entity.Id)
                {
                    kv.Value.character.Position = entity.Entity.Position;
                    kv.Value.character.Direction = entity.Entity.Direction;
                    kv.Value.character.Speed = entity.Entity.Speed;
                    if (entity.Event == EntityEvent.Ride)
                    {
                        kv.Value.character.Ride = entity.Param;
                    }
                }
                else
                {
                    MapService.Instance.SendEntityUpdate(kv.Value.Connection, entity);
                }
            }
        }
        //怪物进入
        internal void MonsterEnter(Monster monster)
        {
            Log.InfoFormat("MonsterEnter:Map:{0} monsterId:{1}", this.Define.ID, monster.Id);
            monster.OnEnterMap(this);
            foreach (var kv in this.MapCharacters)
            {
                this.AddCharacterEnterMap(kv.Value.Connection, monster.Info);
            }
        }

        internal void BroadcasrBattleResponse(NetMessageResponse response)
        {
            foreach (var kv in MapCharacters)
            {
                if(response.skillCast!=null)
                    kv.Value.Connection.Session.Response.skillCast = response.skillCast;
                if(response.skillHits!=null)
                    kv.Value.Connection.Session.Response.skillHits = response.skillHits;
                if (response.buffRes != null)
                    kv.Value.Connection.Session.Response.buffRes = response.buffRes;

                kv.Value.Connection.SendResponse();
            }
        }


    }
}
