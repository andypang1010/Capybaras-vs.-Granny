using System;
using TMPro;
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public string tutorialMessage;
    public TMP_Text tutorialText;

    void OnTriggerStay(Collider other)
    {
        if (
            other.transform.root.gameObject.layer == LayerMask.NameToLayer("Capybara")
            || other.transform.root.gameObject.layer == LayerMask.NameToLayer("Granny")
        )
        {
            tutorialText.gameObject.SetActive(true);
            tutorialText.text = tutorialMessage;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (
            other.transform.root.gameObject.layer == LayerMask.NameToLayer("Capybara")
            || other.transform.root.gameObject.layer == LayerMask.NameToLayer("Granny")
        )
        {
            tutorialText.gameObject.SetActive(false);
            tutorialText.text = "";
        }
    }
}
