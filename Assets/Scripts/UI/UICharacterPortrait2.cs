using System;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class UICharacterPortrait2 : MonoBehaviour
    {
        #region Fields
        public RectTransform Rect;
        public Image Image;
        #endregion

        #region Methods
        public void SetAlpha(float alpha)
        {
            this.Image.color = Utils.ColourAlpha(Color.white, alpha);
        }

        public void SetRotation(float yRotation)
        {
            this.Rect.localRotation = Quaternion.Euler(0, yRotation, 0);
        }
        #endregion
    }
}