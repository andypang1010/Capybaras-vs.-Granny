using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StartCountingUI : MonoBehaviour
{
    public bool IsGranny = false;
    public RawImage Image4;
    public RawImage Image3;
    public RawImage Image2;
    public RawImage Image1;
    public RawImage ImageStart;
    public AudioSource CountdownSound;

    private void Start()
    {
        HideAllImages();
    }

    private void HideAllImages()
    {
        if (Image4)
            Image4.gameObject.SetActive(false);
        if (Image3)
            Image3.gameObject.SetActive(false);
        if (Image2)
            Image2.gameObject.SetActive(false);
        if (Image1)
            Image1.gameObject.SetActive(false);
        if (ImageStart)
            ImageStart.gameObject.SetActive(false);
    }

    public void StartCountdown()
    {
        StartCoroutine(countdownCoroutine());
    }

    private IEnumerator countdownSFXCoroutine()
    {
        if (CountdownSound)
        {
            CountdownSound.Play();
        }
        yield return null;
    }

    private IEnumerator countdownCoroutine()
    {
        //HideAllImages();

        if (Image4)
        {
            Debug.Log("Showing Image 4");
            Image4.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            Image4.gameObject.SetActive(false);
        }
        if (IsGranny)
            StartCoroutine(countdownSFXCoroutine());
        if (Image3)
        {
            Debug.Log("Showing Image 3");
            Image3.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            Image3.gameObject.SetActive(false);
        }

        if (Image2)
        {
            Image2.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            Image2.gameObject.SetActive(false);
        }

        if (Image1)
        {
            Image1.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            Image1.gameObject.SetActive(false);
        }

        if (ImageStart)
        {
            ImageStart.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            ImageStart.gameObject.SetActive(false);
        }

        OnCountdownComplete();
    }

    private void OnCountdownComplete() { }
}
