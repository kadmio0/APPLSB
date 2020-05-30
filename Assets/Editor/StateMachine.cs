﻿using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using CreationStates;
using System.Linq;
// Create a menu item that causes a new controller and statemachine to be created.
public class StateMachine : MonoBehaviour
{
    static string resourcesFolder = "Assets/Resources/";
    static string categoryPath = Path.Combine(resourcesFolder, @"DataBase\config\categories.json");
    static string configPath = Path.Combine(resourcesFolder, @"DataBase\config\data.json");
    static Database dataBase;
    static List<object> expressionCodes = new List<object>();/*{
        "#47101",
        "#47102",
        "#47103",
        "#47104",
        "#47105",
        "#47106",
        "#47107",
        "#47108",
        "#47109",
        "#47110",
        "#00106"
    };*/
    static AnimatorState state = new AnimatorState();
    static float positionStateX = 1;
    static float positionStateY = 1;
    static AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Default.controller");
    
    static List<AnimatorStateTransition> animatorStateTransitionList = new List<AnimatorStateTransition>();

    [MenuItem("Menu Controller LSB/Create new Controller")]
    static void CreateController()
    {
        try
        {
            dataBase = new Database(categoryPath, configPath);
            expressionCodes = dataBase.expressions;
            if (controller==null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Default.controller"); 
                // Creates the controller
                
                // Add parameters
                controller.AddParameter("currentSign", AnimatorControllerParameterType.Int);
                controller.AddParameter("animationSpeed", AnimatorControllerParameterType.Float); 

                var rootStateMachine = controller.layers[0].stateMachine;
                controller.layers[0].stateMachine.name = "Base Layer";

                // Add States  
                //SetState("MainIdle");
                AnimatorState newState = GetNewState(rootStateMachine, "MainIdle");
                newState.motion = getMainIdleAnimation(); 
                Debug.Log("MaindIdle state created"); 
                AddTransitionToList(newState);
            }
            else
            {
                Debug.Log("Animator controller "+controller.name+" already exist");
            }
        }
        catch (Exception error)
        {
            Debug.Log("CreateController ERROR: " + error.Message+" -- "+error.Source);
        }
    }

    static AnimationClip getMainIdleAnimation()
    {
        var animationClips = Resources.LoadAll(@"Animations");  
        foreach (AnimationClip clip in animationClips)
        {
            if (clip.name.Contains("MainIdle"))
            {
                return clip;
            }
        }
        return null;
    }

    static void SetNewPositionState(float posX,float posY)
    {
        positionStateX = positionStateX +(3 * posX);
        positionStateY = positionStateY +(3 * posY);
    }

    static void SetState(string code)
    {
        state.name = code;
        state.speedParameterActive = true;
        state.speedParameter = "animationSpeed";
    } 
    
    static bool IsAlreadyState(string stateName)
    {
        try
        {
            var rootStateMachine = controller.layers[0].stateMachine; 
            List <ChildAnimatorState> animatorStates = new List<ChildAnimatorState>(rootStateMachine.states); 
            foreach (var animatorState in animatorStates)
            {
                if (animatorState.state.name == stateName)
                {
                    return true;
                }
            }
        }
        catch(Exception error)
        {
            Debug.Log("isAlreadyState ERROR: " + error.Message + " -- " + error.Source);
        } 
        return false;
    }

    [MenuItem("Menu Controller LSB/1. Load States")]
    static void LoadAllStates()
    { 
        var rootStateMachine = controller.layers[0].stateMachine;
        dataBase = new Database(categoryPath, configPath);
        expressionCodes = dataBase.expressions;
        foreach (ExpressionData expression in expressionCodes)
        {     
            if (!IsAlreadyState(expression.Code[0]))
            {
                AnimatorState newState = GetNewState(rootStateMachine, expression.Code[0]); 
                AddTransitionToList(newState);
            }
            else
            { 
                Debug.Log("State: " + expression.Code[0] + " has already created");
            } 
        }
        addStatesToAnimatorControllerStatesFile();
    }

    private static void addStatesToAnimatorControllerStatesFile()
    {
        var rootStateMachine = controller.layers[0].stateMachine;
        List<ChildAnimatorState> animatorStates = new List<ChildAnimatorState>(rootStateMachine.states);
        List<string> stateCodes = new List<string>();
        foreach (var animatorState in animatorStates)
        {
            stateCodes.Add(animatorState.state.name);
        } 
        File.WriteAllLines("Assets/Resources/Codes.txt", stateCodes,System.Text.Encoding.UTF8); 
    }

    private static AnimatorState GetNewState(AnimatorStateMachine rootStateMachine, string code)
    {
        AnimatorState newState = rootStateMachine.AddState(code);
        newState.speedParameterActive = true;
        newState.speedParameter = "animationSpeed";
        return newState;
    }

    static void AddTransitionToList(AnimatorState destinationState)
    {  
        animatorStateTransitionList.Add(GetAnimatorStateTransition(destinationState)); 
    }

    static AnimatorStateTransition GetAnimatorStateTransition(AnimatorState destinationAnimatorState){
        try
        {
            AnimatorStateTransition transition = new AnimatorStateTransition
            {
                destinationState = destinationAnimatorState,
                hasExitTime = false,
                exitTime = float.Parse("0,75"),
                hasFixedDuration = true,
                duration = float.Parse("0,5"),
                canTransitionToSelf = false
            };
            String currentSign = "";
            if (destinationAnimatorState.name == "MainIdle")
            {
                currentSign = "MainIdle";
                transition.AddCondition(AnimatorConditionMode.Equals, 0, "currentSign");
            }
            else
            {
                currentSign = destinationAnimatorState.name.Substring(1);
                transition.AddCondition(AnimatorConditionMode.Equals, int.Parse(currentSign), "currentSign");
            }
            return transition;
        }
        catch (Exception error)
        {
            Debug.Log("LoadAnimations ERROR: " + error.Message + " -- " + error.Source);
        }
        return null;
    }


    [MenuItem("Menu Controller LSB/2. Load Transitions")]
    static void LoadAllTransitions()
    {
        try
        {   
            var rootStateMachine = controller.layers[0].stateMachine;
            ChildAnimatorState[] animatorStatesList = rootStateMachine.states;
            if (rootStateMachine.anyStateTransitions.Length<1)
            {
                reloadAllTransitions(animatorStatesList);
            }
            else
            {
                List<AnimatorStateTransition> oldTransitions = rootStateMachine.anyStateTransitions.OfType<AnimatorStateTransition>().ToList();
                oldTransitions.AddRange(animatorStateTransitionList);
                rootStateMachine.anyStateTransitions= oldTransitions.ToArray();
            }
            rootStateMachine.anyStateTransitions = animatorStateTransitionList.ToArray();
            Debug.Log("Transitions reloaded");
        }
        catch(Exception error)
        {
            Debug.Log("loadAllTransitions ERROR: " + error.Message + " -- " + error.Source);
        }
    }

    static void reloadAllTransitions(ChildAnimatorState[] animatorStatesList)
    {
        animatorStateTransitionList = new List<AnimatorStateTransition>();
        foreach (var animatorState in animatorStatesList)
        {
            AddTransitionToList(animatorState.state);
        } 
    }

    static void AddTransitionFromAnyStateToDestination(AnimatorState destinationState)
    {
        try
        {
            var rootStateMachine = controller.layers[0].stateMachine;
            AnimatorStateTransition st = GetAnimatorStateTransition(destinationState);
            
        }
        catch (Exception error)
        {
            Debug.Log("AddTransitionFromAnyStateToDestination ERROR: " + error.Message + " -- " + error.Source);
        }
    }

    [MenuItem("Menu Controller LSB/3. Load all Animations")]
    static void LoadAnimations()
    {
        dataBase = new Database(categoryPath, configPath);
        expressionCodes = dataBase.expressions;
        var animationClips = Resources.LoadAll(@"Animations");
            Debug.Log("Clips Founded: " + animationClips.Length);
            var rootStateMachine = controller.layers[0].stateMachine; 
            ChildAnimatorState[] animatorStatesList = rootStateMachine.states;
            foreach (AnimationClip clip in animationClips)
            {
                if (clip.name.Contains("_"))
                {
                    String clipCode = clip.name.Substring(clip.name.Length - 5);
                    //if exist code clip in data base
                    bool firstFlag = false; bool secondFlag = false; 
                    foreach (ExpressionData expression in expressionCodes) {
                        if (expression.Code[0].Contains("#" + clipCode))
                        {
                            firstFlag = true;
                            foreach (var animatorState in animatorStatesList)
                            {
                                if ("#" + clipCode == animatorState.state.name)
                                {
                                    secondFlag = true;
                                    animatorState.state.motion = clip;
                                    break;
                                } 
                            }
                            if (secondFlag == false)
                            {
                                Debug.Log("Clip name " + clipCode + " doesn't have state");
                            }
                            break;
                        }
                    }
                    if(firstFlag == false) 
                    {
                        Debug.Log("Clip name " + clipCode + " doesn't exist in database");
                    }
                }
                else
                { 
                    Debug.Log("Clip has errors in name: " + clip.name); 
                }
            } 
        
    } 
}
