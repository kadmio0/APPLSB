﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace LSB
{
    public class ResponseHandler : MonoBehaviour
    {
        [Serializable] public class ResultHandler : UnityEvent<ExpressionList> { }
        public ResultHandler OnResult;

        [Serializable] public class ErrorHandler : UnityEvent<string> { }
        public ErrorHandler OnError;

        public void OnResponse(UnityWebRequest request, string word)
        {
            Debug.Log("LLEGO AQUI RESPONSE");
            Debug.Log("Palabra: " + word);
            ExpressionList expressionList = JsonUtility.FromJson<ExpressionList>(request.downloadHandler.text);
            Debug.Log("EXPRESSION: ");
            Debug.Log(expressionList.getexpressions());
            if (request.responseCode == 200 && OnResult != null)
                OnResult.Invoke(expressionList);
            if (request.responseCode != 200 && OnError != null)
                OnError.Invoke(word);
        }
    }
}
