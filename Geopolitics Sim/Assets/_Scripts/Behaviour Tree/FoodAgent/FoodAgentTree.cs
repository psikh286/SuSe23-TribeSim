﻿using System;
using System.Collections;
using System.Collections.Generic;
using BehaviorTree;
using UnityEngine;
using Object = UnityEngine.Object;

public class FoodAgentTree : BTree
{
    public static Action<int, int> OnAgentSpawn;

    private int _colorIndex;
    
    private IEnumerator Start()
    {
        StartCoroutine(Death());
        
        yield return new WaitForSeconds(1f);
        var t = (float)GetData("speed");
        var c = Mathf.InverseLerp(GlobalSettings.MinSpeed, GlobalSettings.MaxSpeed, t);


        var mat = GetComponent<MeshRenderer>().material;
        switch (c)
        {
            case < 0.3f:
                mat.color = Color.red;
                _colorIndex = 0;
                break;
            case < 0.6f:
                mat.color = Color.yellow;
                _colorIndex = 1;
                break;
            default:
                mat.color = Color.green;
                _colorIndex = 2;
                break;
        }
        
        OnAgentSpawn?.Invoke(1, _colorIndex);
    }

    private IEnumerator Death()
    {
        for (var j = 0; j < 2; j++)
        {
            yield return new WaitForSeconds(5f);
            var i = (int)GetData("foodCount");
            if (i <= 0) continue;
            SetData("foodCount", i - 1);
            StartCoroutine(Death());
            yield break;
        }
        
        OnAgentSpawn?.Invoke(-1, _colorIndex);
        Destroy(gameObject);
    }

    private void Update()
    {
        OnTick(true);
    }

    protected override Node SetupTree()
    {
        var node = 
            new Selector(this, new List<Node>
            {
                //WAIT FOR A MATE
                new CheckWaitingForMate(this)
                ,//REPRODUCE
                new Sequence(this, new List<Node>
                {
                    new CheckReadyToReproduce(this),
                    new CheckMateInDoRange(this),
                    new TaskReproduce(this)
                })
                ,//WALK TO MATE
                new Sequence(this, new List<Node>
                {
                    new CheckReadyToReproduce(this),
                    new CheckMateInFOVRange(this),
                    new TaskGoToTarget(this)
                })
                ,//EAT FOOD
                new Sequence(this, new List<Node>
                {
                    new CheckNotEnoughFood(this),
                    new CheckFoodInEatingRange(this),
                    new TaskEatFood(this)
                })
               , //WALK TO FOOD
                new Sequence(this, new List<Node>
                {
                    new CheckNotEnoughFood(this),
                    new CheckFoodInFOVRange(this),
                    new TaskGoToTarget(this)
                })
                , //WALK AROUND
                new TaskWander(this)
            });
        
        SetData("speed", 2f);
        SetData("foodCount", 0);
        
        
        
        return node;
    }
    
    public bool RequestMate(FoodAgentTree male)
    {
        if (new CheckReadyToReproduce(this).Evaluate() != NodeState.SUCCESS) return false;
        if ((Object)GetData("mate") != null) return false;
        
        SetData("mate", male);
        SetData("target", transform);
        
        return true;
    }
    public void Impregnate()
    {
        ClearData("mate");
        ClearData("target");
        SetData("foodCount", (int)GetData("foodCount") - 2);
        
    }
}