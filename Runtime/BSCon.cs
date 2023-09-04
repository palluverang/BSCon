using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace BSCon
{
    [ExecuteAlways]
    public class BSCon : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _asset;
        [SerializeField] private int _actionMapIndex;
        public string actionMapName => _asset.actionMaps[_actionMapIndex].name;

        [SerializeField] private int _deviceIndex = -1;
        [SerializeField] private InputDeviceDescription _deviceDescription = default;
        [SerializeField] private InputDevice _device = null;

        [SerializeField] private GameObject _characterRoot;

        [SerializeField] private InputAction[] _inputActions;
        [SerializeField] private BlendShapeProxy[] _proxies;

        private void OnEnable()
        {
            Enable();
            InputSystem.onDeviceChange += onDeviceChange;
        }

        private void OnDisable()
        {
            Disable();
            InputSystem.onDeviceChange -= onDeviceChange;
        }

        private void onDeviceChange(InputDevice device, InputDeviceChange inputDeviceChange)
        {
            UpdateDevice(_deviceDescription);
        }

        public void Enable()
        {
            if (_asset == null)
                return;
            if (_asset.actionMaps.Count == 0)
                return;

            if (string.IsNullOrEmpty(actionMapName))
                _actionMapIndex = 0;

            var map = _asset.FindActionMap(actionMapName);
            if (map == null)
                return;

            _inputActions = map
                .actions
                .Select(action => map.FindAction(action.id))
                .ToArray();

            _proxies = _inputActions
                .Select(action => action.id)
                .Select(guid =>
                {
                    return _proxies.FirstOrDefault(proxy => proxy.guid == guid)
                        ?? new BlendShapeProxy { id = guid.ToString() };
                })
                .ToArray();

            foreach (var action in _inputActions)
            {
                action.started += Control;
                action.performed += Control;
                action.canceled += Control;

                action.Enable();
            }
        }

        public void Disable()
        {
            if (_inputActions == null)
                return;

            foreach (var action in _inputActions)
            {
                action.started -= Control;
                action.performed -= Control;
                action.canceled -= Control;
            }
        }

        public void UpdateDevice(InputDeviceDescription description)
        {
            for (_deviceIndex = 0; _deviceIndex < InputSystem.devices.Count; _deviceIndex++)
            {
                var device = InputSystem.devices[_deviceIndex];
                if (device.description == description)
                {
                    _device = device;
                    _deviceDescription = description;
                    break;
                }
            }

            if (InputSystem.devices.Count == _deviceIndex)
            {
                _deviceIndex = -1;
                _device = null;
            }
        }

        private void Control(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device != _device)
                return;

            var proxy = _proxies.FirstOrDefault(proxy => proxy.guid == ctx.action.id);
            if (proxy == null)
                return;

            proxy.BlendShapeControl(ctx.ReadValue<float>());
        }
    }
}
