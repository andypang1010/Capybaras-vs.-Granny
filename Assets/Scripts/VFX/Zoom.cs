using UnityEngine;

public class Zoom : MonoBehaviour
{
    Renderer _renderer;
    Camera _cam;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _cam = Camera.main;
    }

    void Update()
    {
        Vector3 scrnPt = _cam.WorldToScreenPoint(transform.position);
        scrnPt.x = scrnPt.x / Screen.width;
        scrnPt.y = scrnPt.y / Screen.height;
        _renderer.material.SetVector("_ObjectScreenPos", scrnPt);
    }
}
