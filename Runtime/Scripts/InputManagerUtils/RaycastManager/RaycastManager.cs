using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.RaycastUtils
{
    public class RaycastManager : MonoBehaviour
    {
        #region Events 
        [Foldout("[Event]-Click"), Tooltip("Hit&Click到的第一個物件")] public UnityEvent<Transform> OnClickTargetAction;
        [Foldout("[Event]-Click"), Tooltip("Hit&Click到的物件列表")] public UnityEvent<List<Transform>> OnClickTargetsAction;
        [Foldout("[Event]-Hover"), Tooltip("Hit到的第一個物件")] public UnityEvent<Transform> OnHoverTargetAction;
        [Foldout("[Event]-Hover"), Tooltip("Hit到的物件列表")] public UnityEvent<List<Transform>> OnHoverTargetsAction;
        #endregion


        #region Fields
        [Foldout("[Settings]"), SerializeField] private int maxHitObjects = 10;
        [Foldout("[Settings]"), SerializeField] private float rayDistance = 100f;
        [Foldout("[Settings]"), SerializeField] private LayerMask targetLayerMask = ~0;
        [Foldout("[Settings]"), SerializeField] private Camera mainCamera;
        [Foldout("[HitResult]"), SerializeField] private List<Transform> clickedObjects = new List<Transform>();
        [Foldout("[HitResult]"), SerializeField] private List<Transform> hoveredObjects = new List<Transform>();

        private Transform _lastHoveredObject;
        private Vector3 _hitPoint;
        #endregion


        private void OnValidate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Update()
        {
            if (mainCamera == null) return;
            RaycastHit[] hits = HandleRaycastHit();
            DrawDebugRay(hits);
            HandleMouseOverExit(hits);
            if (Input.GetMouseButtonDown(0))
            {
                HandleClickEvents(hits);
            }
        }



        private RaycastHit[] HandleRaycastHit()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, targetLayerMask);

            // Sort hits by distance
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Limit the number of hit objects
            if (hits.Length > maxHitObjects)
            {
                Array.Resize(ref hits, maxHitObjects);
            }
            return hits;
        }

        private void DrawDebugRay(RaycastHit[] hits)
        {
            if (hits.Length > 0)
            {
                Debug.DrawLine(mainCamera.transform.position, hits[0].point, Color.red);
            }
            else
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);
            }
        }

        private void HandleClickEvents(RaycastHit[] hits)
        {
            if (hits.Length > 0)
            {
                clickedObjects = new List<Transform>();
                foreach (var hit in hits)
                {
                    clickedObjects.Add(hit.transform);
                }
                OnClickTargetAction?.Invoke(clickedObjects[0]);
                OnClickTargetsAction?.Invoke(clickedObjects);
            }
        }

        private void HandleMouseOverExit(RaycastHit[] hits)
        {
            Transform currentHoveredObject = hits.Length > 0 ? hits[0].transform : null;

            if (_lastHoveredObject != currentHoveredObject)
            {
                // Mouse exit event
                if (_lastHoveredObject != null)
                {
                    OnHoverTargetAction?.Invoke(null);
                    OnHoverTargetsAction?.Invoke(new List<Transform>());
                }

                // Mouse over event
                if (currentHoveredObject != null)
                {
                    hoveredObjects = new List<Transform>();
                    foreach (var hit in hits)
                    {
                        hoveredObjects.Add(hit.transform);
                    }

                    OnHoverTargetAction?.Invoke(currentHoveredObject);
                    OnHoverTargetsAction?.Invoke(hoveredObjects);
                }

                _lastHoveredObject = currentHoveredObject;
            }
        }
    }
}