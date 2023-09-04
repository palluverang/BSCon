using System;
using UnityEngine;

namespace BSCon
{
    [Serializable]
    public class BlendShapeProxy
    {
        [SerializeField] public string name;
        [SerializeField] public string id;
        [SerializeField] public SkinnedMeshRenderer mesh;
        [SerializeField] public int index;
        [SerializeField] public float multiplier = 100f;
        [SerializeField] private float _value;

        public Guid guid => new(id);

        public void BlendShapeControl(float weight)
        {
            _value = weight * multiplier;
            if (mesh == null)
                return;

            mesh.SetBlendShapeWeight(index, _value);
        }

        public float BlendShapeControl()
        {
            return mesh.GetBlendShapeWeight(index);
        }
    }
}
