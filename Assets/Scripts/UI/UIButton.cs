using System;
using UnityEngine;
using UnityEngine.EventSystems;

#nullable enable

namespace Orbits
{
    public enum UIButtonType
    {
        Click, Confirmation
    }

    public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        #region Fields
        public UIButtonType ButtonType = UIButtonType.Click;
        #endregion

        #region Unity Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            var sounds = this.ButtonType == UIButtonType.Confirmation ? AudioManager.Instance.UIConfirmation : AudioManager.Instance.UIClick;
            AudioManager.PlayOneOffUI(sounds, this.transform.position);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            var anyDown = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
            if (!anyDown)
            {
                AudioManager.PlayOneOffUI(AudioManager.Instance.UIHover, this.transform.position);
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}