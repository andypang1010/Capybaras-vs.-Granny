using UnityEngine.UI;
using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private Image progressBarImage;
    //[SerializeField] private float totalProgress = 100f; // Total progress value
    [SerializeField] private float currentProgress = 1f; // Target fill amount (0 to 1)
    public bool IsFull = true;

    void Start()
    {
        if (progressBarImage == null)
        {
            progressBarImage = GetComponent<Image>();
        }
        if (IsFull)
            SetProgress(1f); // Initialize to full progress
        else
            SetProgress(0f); // Initialize to empty progress
    }
    
    public void SetProgress(float progress)
    {
        progressBarImage.fillAmount = progress;
        currentProgress = progress;
    }
}
