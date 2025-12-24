using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools.DevTasks
{
    public class AnimationHandler
    {
        /// <summary>
        /// Endlessly pulse the size of the visual element if icon animation is enabled
        /// </summary>
        /// <param name="myElement"></param>
        /// <param name="scaleSpeed"></param>
        /// <param name="maxScale"></param>
        public void Animate_Scale(VisualElement myElement, float scaleSpeed = 0.05f, float maxScale = 1.2f)
        {
            if (myElement == null)
                return;

            if (!Settings.instance.enableIconAnimations)
                return;

            float minScale = 1.0f;
            bool increasing = true;

            myElement.schedule.Execute(() =>
            {
                Vector2 currentScale = myElement.resolvedStyle.scale.value;

                if (increasing)
                {
                    currentScale += new Vector2(scaleSpeed, scaleSpeed);
                    if (currentScale.x >= maxScale)
                    {
                        currentScale = new Vector2(maxScale, maxScale);
                        increasing = false;
                    }
                }
                else
                {
                    currentScale -= new Vector2(scaleSpeed, scaleSpeed);
                    if (currentScale.x <= minScale)
                    {
                        currentScale = new Vector2(minScale, minScale);
                        increasing = true;
                    }
                }

                myElement.style.scale = new Scale(currentScale);
            }).Every(100);
            //}).Every(100).Until(() => !increasing); 
        }

        /// <summary>
        /// Endlessly rotate the visual element if icon animation is enabled
        /// </summary>
        /// <param name="myElement"></param>
        /// <param name="speed"></param>
        public void Animate_Rotation(VisualElement myElement, float speed = 10)
        {
            if (myElement == null) return;

            if (!Settings.instance.enableIconAnimations) return;

            float rotationAngle = 0.0f;
            myElement.schedule.Execute(() =>
            {
                rotationAngle += speed; // Increase the rotation angle
                myElement.style.rotate = new Rotate(new Angle(rotationAngle));
            }).Every(8);
        }

        /// <summary>
        /// Fade in the visual element if icon animation is enabled
        /// </summary>
        /// <param name="myElement"></param>
        /// <param name="fadeInSpeed"></param>
        /// <param name="after"></param>
        public void Animate_Fade(VisualElement myElement, float fadeInSpeed = 0.01f, long after = 0)
        {
            if (myElement == null) return;

            if (!Settings.instance.enableIconAnimations) return;

            myElement.style.opacity = 0.0f; // Start fully transparent

            myElement.schedule.Execute(() =>
            {
                float currentOpacity = myElement.resolvedStyle.opacity;
                currentOpacity += fadeInSpeed;

                myElement.style.opacity = currentOpacity;
            }).Every(10).Until(() => myElement.resolvedStyle.opacity >= 1).ExecuteLater(after);
        }
    }
}