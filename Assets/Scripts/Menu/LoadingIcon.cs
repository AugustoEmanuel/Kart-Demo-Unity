using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingIcon : MonoBehaviour
{
    [SerializeField] private Image loadingIcon;
    [SerializeField] private List<Sprite> loadingIconFrames;
    private int loadingIconFrameCounter;
    private float loadingIconTimer = 0f;

    // Update is called once per frame
    void Update()
    {
        loadingIconTimer += Time.deltaTime;
        if (loadingIconTimer > .1f)
        {
            loadingIconFrameCounter = (int)(loadingIconFrameCounter + 1) % loadingIconFrames.Count;
            loadingIcon.sprite = loadingIconFrames[loadingIconFrameCounter];
            loadingIconTimer = 0;
        }
    }

    public void ToggleLoadingIcon(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
