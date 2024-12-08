using System.Collections.Generic;
using Unity.Linq;
using UnityEngine;

namespace LHC.Core
{
    class LHCWindow : MonoBehaviour
    {
        private bool _isOpen;
        private int _windowId = 9123;
        private Rect _windowPosition;
        private ParticleSystem[] _systems;
        private ParticleSystem _selectedIndex;
        private Vector2 _listScroll = new Vector2();

        private List<FocusType> _focusTypes = new List<FocusType>()
        {
            FocusType.Keyboard,
            FocusType.Keyboard,
            FocusType.Passive,
            FocusType.Keyboard
        };
        private void Update()
        {
            if (!Init.OpenWindowKey.Value.IsUp()) return;
            _systems = GetComponentsInChildren<ParticleSystem>();
            _isOpen = true;
        }

        private void OnGUI()
        {
            if (!_isOpen)
                return;
            _windowPosition = GUI.Window(_windowId, _windowPosition, WindowFunction, "Lewd Horny Collider");
        }

        private void WindowFunction(int id)
        {
            var main = _selectedIndex.main;
        }

    }
}