using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public Camera camToLookAt;

    private void Update()
    {
        transform.forward = camToLookAt.transform.position - transform.position;
    }
}
