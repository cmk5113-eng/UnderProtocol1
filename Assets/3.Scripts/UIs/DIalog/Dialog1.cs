using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class tori_start : MonoBehaviour, IPointerDownHandler
{
    public Text ScriptText_dialogue;
    public string[] dialogue
        = { "토리야 네가 내 usb 훔쳐 간 범인을 봤다던데 정말이야??",
            "나 좀 도와줘 제발..",
            "너도 날 도와주면 나도 널 도와줄게",
            "뭘 도와주면 될까??",
            "내 밭에 있는 당근이 너무 많아서 몇 개인지 모르겠어..!",
            "그런 거라면 문제없지! 내가 당근의 개수를 다 세주면 되는 거지??",
            "개수까지는 괜찮고, 홀수인지 짝수인지만 알려주면 돼!",
            "이번 게임은 홀짝 게임이야 잘 알고 있지??"
        };
    public int dialogue_count = 0;

    public void OnPointerDown(PointerEventData data)
    {
        dialogue_count++;
        Debug.Log(dialogue_count);

        if (dialogue_count == 8)
        {
            Debug.Log("대화 종료");
            dialogue_count = 0;
        }

        ScriptText_dialogue.text = dialogue[dialogue_count];
    }
}