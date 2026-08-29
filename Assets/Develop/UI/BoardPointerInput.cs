using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NeonSeven.UI
{
    public sealed class BoardPointerInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _rectTransform;
        private int _size;
        private Action<int> _aimed;
        private Action<int> _dropped;
        private bool _isLocked;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        public void Initialize(int size, Action<int> aimed, Action<int> dropped)
        {
            _size = size;
            _aimed = aimed;
            _dropped = dropped;
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isLocked)
                return;

            _aimed?.Invoke(ColumnFrom(eventData));
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isLocked)
                return;

            _aimed?.Invoke(ColumnFrom(eventData));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isLocked)
                return;

            int column = ColumnFrom(eventData);
            _aimed?.Invoke(column);
            _dropped?.Invoke(column);
        }

        private int ColumnFrom(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out var local);
            float normalized = Mathf.InverseLerp(_rectTransform.rect.xMin, _rectTransform.rect.xMax, local.x);
            return Mathf.Clamp(Mathf.FloorToInt(normalized * _size), 0, _size - 1);
        }
    }
}
