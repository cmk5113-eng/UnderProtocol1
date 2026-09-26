using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Dialog2 : MonoBehaviour, IPointerDownHandler
{
    public Text ScriptText_dialogue;
    public string[] dialogue
        = { "�丮�� �װ� �� usb ���� �� ������ �ôٴ��� �����̾�??",
            "�� �� ������ ����..",
            "�ʵ� �� �����ָ� ���� �� �����ٰ�",
            "�� �����ָ� �ɱ�??",
            "�� �翡 �ִ� ����� �ʹ� ���Ƽ� �� ������ �𸣰ھ�..!",
            "�׷� �Ŷ�� ��������! ���� ����� ������ �� ���ָ� �Ǵ� ����??",
            "���������� ������, Ȧ������ ¦�������� �˷��ָ� ��!",
            "�̹� ������ Ȧ¦ �����̾� �� �˰� ����??"
        };
    public int dialogue_count = 0;

    public void OnPointerDown(PointerEventData data)
    {
        dialogue_count++;
        Debug.Log(dialogue_count);

        if (dialogue_count == 8)
        {
            Debug.Log("��ȭ ����");
            dialogue_count = 0;
        }

        ScriptText_dialogue.text = dialogue[dialogue_count];
    }
}