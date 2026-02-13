using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class Timer : MonoBehaviour
{
    public static Timer Instance {set; get;}
    private int white = 1;
    private int black = 1;
    int team;
    public bool gameIsRunning = true;


    [SerializeField] TMP_Text player1;
    [SerializeField] TMP_Text player2;
    [SerializeField] public int localTeam = 0;
    public Action<int> Timeout;

    private void Awake()
    {
        Instance = this;
        ResetTimer(600);
    }


    public void ProcessMove(bool isWhitesTurn)
    {
        if (gameIsRunning)
        {
            if (isWhitesTurn)
            {
                team = 0;
                if(localTeam == 0)
                {
                    StartCoroutine(StartWhite(player1));
                }
                else
                {
                    StartCoroutine(StartWhite(player2));
                }
            
            }
            else
            {
                team = 1;
                if(localTeam == 1)
                {
                    StartCoroutine(StartBlack(player1));
                }
                else
                {
                    StartCoroutine(StartBlack(player2));
                }
            }
        }
        
    }
    public void ResetTimer(int time)
    {
        team = -1;
        white = time;
        black = time;
        UpdateText(player1, time);
        UpdateText(player2, time);
        team = 0;
    }


    IEnumerator StartWhite(TMP_Text player)
    {
        while(team == 0 && gameIsRunning)
        {
            white--;
            yield return new WaitForSeconds(1f);
            UpdateText(player, white);
            if(white == 0)
            {
                //Signify checkmate scenario
                Timeout?.Invoke(3);
                Debug.Log("Timeout reached, ending timer and signifying checkmate");
                break;
            }
            Debug.Log("Local white team timer is running");
        }
        Debug.Log("Team Change detected, exiting timer for white team");
    }
    IEnumerator StartBlack(TMP_Text player)
    {
        while(team == 1 && gameIsRunning)
        {
            black--;
            yield return new WaitForSeconds(1f);
            UpdateText(player, black);
            if(black == 0)
            {
                //Signify checkmate scenario
                Timeout?.Invoke(2);
                Debug.Log("Timeout reached, ending timer and signifying checkmate");
                break;
            }
            Debug.Log("Local black team timer is running");
        }
        Debug.Log("Team Change detected, exiting timer for black team");
    }
    private void UpdateText(TMP_Text text, int time)
    {
        text.text = Mathf.FloorToInt(time/60) + ":" + (time%60 < 10? "0" + time%60 : time%60);
    }



}
