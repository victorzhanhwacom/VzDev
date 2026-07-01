using UnityEngine;
using UnityEngine.UI;

public class CircleProgressBar : MonoBehaviour
{
  #region Fields
 /*  [Foldout()] public UnityEvent<float> onProgressChanged;
  [] public UnityEvent<float> onProgressChanged01;
  [] public UnityEvent onComplete; */
  [SerializeField] private Image progressBar;
    #endregion

    public void SetProgress(float progress)
    {
        // progressBar.fillAmount = progress;
    }

    public void SetProgress01(float progress01)
    {
        // progressBar.fillAmount = Mathf.Clamp01(progress);
    }

    public void SetMaxValue(float maxValue)
    {
    }
    
    public void SetValue(float value)
    {
    }
    

}
