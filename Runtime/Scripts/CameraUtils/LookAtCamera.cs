using System;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.CameraUtils
{
    public class LookAtCamera : MonoBehaviour
    {
       [Foldout("[組件]"), SerializeField] private Camera mainCamera;

       private void Awake()
       {
           if (mainCamera == null) FindComponents();
       }

       private void Update()
       {
           Vector3 dir = mainCamera.transform.position - transform.position;
           transform.rotation = Quaternion.LookRotation(dir * -1, Vector3.up);
       }

       private void Reset() => FindComponents();

       [Button]
       private void FindComponents() => mainCamera = Camera.main;
       
    }
}
