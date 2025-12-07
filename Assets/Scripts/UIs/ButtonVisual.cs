using UnityEngine;

public class ButtonVisual : MonoBehaviour
{
    public GameObject ButtonDefaultVisual;
    public GameObject ButtonPressedVisual;
    public GameObject ButtonPressedIndicator;
    public bool IsPressed = false;

    private void Start()
    {
        setPressedVisual(false);
    }

    public void setPressedVisual(bool state)
    {
        IsPressed = state;
        if (ButtonPressedVisual != null)
        {
            ButtonPressedVisual.SetActive(state);
        }
        if (ButtonDefaultVisual != null)
        {
            ButtonDefaultVisual.SetActive(!state);
        }
        if (ButtonPressedIndicator != null)
        {
            ButtonPressedIndicator.SetActive(state);
        }
    }

    // private IEnumerator visualIndicatorCoroutine(float duration = 0.2f){

    //     yield return new WaitForSeconds(duration);
    //     if(ButtonPressedIndicator != null){
    //         ButtonPressedIndicator.SetActive(true);
    //     }
    // }
}
