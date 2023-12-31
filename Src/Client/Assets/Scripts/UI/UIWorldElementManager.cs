﻿using Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWorldElementManager : MonoSingleton<UIWorldElementManager> {

    public GameObject nameBarPrefab;
    public GameObject NpcStatusPrefab;
    public GameObject PopupTextPrefab;


    private readonly Dictionary<Transform, GameObject> elementStatus = new Dictionary<Transform, GameObject>();
    private readonly Dictionary<Transform, GameObject> elementNames = new Dictionary<Transform, GameObject>();

    protected override void OnStart()
    {
        nameBarPrefab.SetActive(false);
        PopupTextPrefab.SetActive(false);
    }

    public void AddCharacterNameBar(Transform owner, Character character)
    {
        GameObject goNameBar = Instantiate(nameBarPrefab, this.transform);
        goNameBar.name = "NameBar" + character.entityId;
        goNameBar.GetComponent<UIWorldElement>().owner = owner;
        goNameBar.GetComponent<UINameBar>().Character = character;
        goNameBar.SetActive(true);
        this.elementNames[owner] = goNameBar;
    }

    public void RemoveCharacterNameBar(Transform owner)
    {
        if (this.elementNames.ContainsKey(owner))
        {
            Destroy(this.elementNames[owner]);
            this.elementNames.Remove(owner);
        }
    }
    public void AddNpcQuestStatus(Transform owner, NpcQuestStatus status)
    {
        if (this.elementStatus.ContainsKey(owner))
        {
           elementStatus[owner].GetComponent<UIQuestStatus>().SetQuestStatus(status);
        }
        else
        {
            var go = Instantiate(NpcStatusPrefab, this.transform);
            go.name = "NpcQuestStatus" + owner.name;
            go.GetComponent<UIWorldElement>().owner = owner;
            go.GetComponent<UIQuestStatus>().SetQuestStatus(status);
            go.SetActive(true);
            this.elementStatus[owner] = go;
        }
        
    }

    public void RemoveNpcQuestStatus(Transform owner)
    {
        if (this.elementStatus.ContainsKey(owner))
        {
            Destroy(this.elementStatus[owner]);
            this.elementStatus.Remove(owner);
        }
    }

    public void ShowPopupText(PopupType type, Vector3 position, float damage, bool isCrit)
    {
        GameObject goPoup = Instantiate(PopupTextPrefab, position, Quaternion.identity, this.transform);
        goPoup.name = "Popup";
        goPoup.GetComponent<UIPopupText>().InitPopup(type,damage,isCrit);
        goPoup.SetActive(true);
    }
}
