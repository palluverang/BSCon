using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BSCon.Editor
{
    [CustomEditor(typeof(BSCon))]
    public class BSConEditor : UnityEditor.Editor
    {
        private SerializedProperty sp_asset;
        private SerializedProperty sp_actionMapIndex;
        private SerializedProperty sp_deviceIndex;
        private SerializedProperty sp_inputActions;
        private SerializedProperty sp_proxies;
        private SerializedProperty sp_characterRoot;

        private void OnEnable()
        {
            sp_asset = serializedObject.FindProperty("_asset");
            sp_actionMapIndex = serializedObject.FindProperty("_actionMapIndex");
            sp_deviceIndex = serializedObject.FindProperty("_deviceIndex");
            sp_inputActions = serializedObject.FindProperty("_inputActions");
            sp_proxies = serializedObject.FindProperty("_proxies");
            sp_characterRoot = serializedObject.FindProperty("_characterRoot");
        }

        public override void OnInspectorGUI()
        {
            var bscon = target as BSCon;
            if (bscon == null)
                return;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(sp_asset);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                bscon.Enable();
            }

            var asset = sp_asset.objectReferenceValue as InputActionAsset;
            if (asset == null)
            {

            }
            else
            {
                var actionMapNames = asset
                    .actionMaps
                    .Select(map => map.name)
                    .ToArray();

                EditorGUI.BeginChangeCheck();
                sp_actionMapIndex.intValue = EditorGUILayout.Popup("Action Map", sp_actionMapIndex.intValue, actionMapNames);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(bscon, "Change Action Map");
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(bscon);
                }
            }

            if (sp_deviceIndex.intValue < 0)
            {
                var deviceNames = InputSystem
                    .devices
                    .Select(device => device.name.Replace('/', ' '))
                    .Prepend("default")
                    .ToArray();

                EditorGUI.BeginChangeCheck();
                var index = EditorGUILayout.Popup("Device", 0, deviceNames);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(bscon, "Change Device");
                    EditorUtility.SetDirty(bscon);
                }

                bscon.UpdateDevice(index switch
                {
                    0 => default,
                    _ => InputSystem.devices[index - 1].description
                });
            }
            else
            {
                var deviceNames = InputSystem
                    .devices
                    .Select(device => device.name.Replace('/', ' '))
                    .ToArray();

                EditorGUI.BeginChangeCheck();
                var index = EditorGUILayout.Popup("Device", sp_deviceIndex.intValue, deviceNames);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(bscon, "Change Device");
                    EditorUtility.SetDirty(bscon);
                }

                bscon.UpdateDevice(InputSystem.devices[index].description);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(sp_characterRoot);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                bscon.Enable();
            }

            var characterRoot = sp_characterRoot.objectReferenceValue as GameObject;
            if (characterRoot == null)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var meshes = characterRoot
                .GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
                .Where(mesh => mesh.sharedMesh.blendShapeCount > 0)
                .ToArray();
            var blendShapeNames = meshes
                .Select(mesh =>
                {
                    var path = GetTransformPath(mesh.transform, characterRoot.transform);
                    return Enumerable
                        .Range(0, mesh.sharedMesh.blendShapeCount)
                        .Select(index => path + '/' + mesh.sharedMesh.GetBlendShapeName(index));
                })
                .SelectMany(paths => paths)
                .ToArray();

            EditorGUILayout.Space();

            for (int i = 0; i < sp_inputActions.arraySize; i++)
            {
                var sp_index = sp_proxies.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(BlendShapeProxy.index));
                var sp_mesh = sp_proxies.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(BlendShapeProxy.mesh));
                var sp_name = sp_proxies.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(BlendShapeProxy.name));
                var sp_multiplier = sp_proxies.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(BlendShapeProxy.multiplier));
                var sp_value = sp_proxies.GetArrayElementAtIndex(i).FindPropertyRelative("_value");
                var sp_actionName = sp_inputActions.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name");

                EditorGUILayout.Space();

                EditorGUILayout.LabelField(sp_actionName.stringValue, EditorStyles.boldLabel);

                var offset = 0;
                foreach (var mesh in meshes)
                {
                    if (mesh == sp_mesh.objectReferenceValue)
                        break;

                    offset += mesh.sharedMesh.blendShapeCount;
                }
                var blendShapeOffset = EditorGUILayout.Popup("Blend Shape", offset + sp_index.intValue, blendShapeNames);

                var count = 0;
                foreach (var mesh in meshes)
                {
                    var c = count + mesh.sharedMesh.blendShapeCount;
                    if (blendShapeOffset >= c)
                    {
                        count = c;
                        continue;
                    }

                    sp_index.intValue = blendShapeOffset - count;
                    sp_mesh.objectReferenceValue = mesh;
                    sp_name.stringValue = blendShapeNames[blendShapeOffset];
                    break;
                }

                EditorGUILayout.PropertyField(sp_multiplier);

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Slider("Wight", sp_value.floatValue, 0, 100);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static string GetTransformPath(in Transform transform, in Transform parent)
        {
            if (transform.parent == parent)
                return transform.name;

            return GetTransformPath(transform.parent, parent) + '/' + transform.name;
        }
    }
}
