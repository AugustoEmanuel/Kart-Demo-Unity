using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonCustom : MonoBehaviour, ISelectHandler
{
    private AudioSource source;
    public AudioClip audioClipSelected;
    public AudioClip audioClipClicked;
    private bool mute = false;
    private float initialWidth;
    private float initialHeight;
    RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialWidth = rectTransform.sizeDelta.x;
        initialHeight = rectTransform.sizeDelta.y;
        source = GameObject.Find("SCREEN_MANAGER").GetComponent<AudioSource>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (mute) return;
        source.PlayOneShot(audioClipSelected);
    }

    public void OnDeselect(BaseEventData eventData)
    {
    }
    private void OnDisable()
    {
        rectTransform.sizeDelta = new Vector2(initialWidth, rectTransform.sizeDelta.y);
    }

    public void OnClick()
    {
        if (mute) return;
        source.PlayOneShot(audioClipClicked);
    }

    public void OnClick(UnityAction action)
    {
        GetComponent<Button>().onClick.AddListener(() => {
            source.PlayOneShot(audioClipClicked);
            action?.Invoke();
        });
    }

    public void Select()
    {
        mute = true;
        EventSystem.current.SetSelectedGameObject(gameObject);
        mute = false;
    }

    private void Update()
    {
        bool selected = EventSystem.current.currentSelectedGameObject.Equals(gameObject);
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(Mathf.Lerp(rectTransform.sizeDelta.x, initialWidth * (selected ? 1.1f : 1f), 4 * Time.deltaTime * (selected ? 1 : 2)), Mathf.Lerp(rectTransform.sizeDelta.y, initialHeight * (selected ? 1.1f : 1f), 4 * Time.deltaTime * (selected ? 1 : 2)));
    }
}
