using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public enum MenuState
    {
        MENU,
        TUTORIAL,
    }

    public static MenuManager instance;
    public MenuState currentState = MenuState.MENU;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (Display.displays.Length > 1)
            Display.displays[1].Activate();
    }

    void Update()
    {
        switch (currentState)
        {
            case MenuState.MENU:
                UpdateMenuState();
                break;
            case MenuState.TUTORIAL:
                UpdateTutorialState();
                break;
        }
    }

    private void UpdateMenuState()
    {
        UpdateMenuStateUI();
        if (IsEveryPlayerReady())
        {
            SetCurrentState(MenuState.TUTORIAL);
        }
    }

    private void UpdateMenuStateUI()
    {
        throw new NotImplementedException();
    }

    private void UpdateTutorialState()
    {
        if (IsEveryPlayerReady())
        {
            SceneManager.LoadScene("GAME");
        }
    }

    bool IsPlayerReady(int playerIndex)
    {
        return InputManager.Instance.GetCapybaraAbilityDown(playerIndex);
    }

    bool IsEveryPlayerReady()
    {
        bool allReady = true;

        for (int i = 1; i <= 4; i++)
        {
            allReady &= IsPlayerReady(i);
        }
        return allReady;
    }

    void SetCurrentState(MenuState newState)
    {
        currentState = newState;
    }
}
