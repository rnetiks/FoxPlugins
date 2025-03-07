using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CTRLZDoesntWork.KK
{
    [RequireComponent(typeof(MeshFilter))]
    class MeshModifier : MonoBehaviour
    {
        private Mesh _originalMesh;
        private Mesh _modifiedMesh;
        private Transform _originalTransform;

        List<BaseModifier> _activeModifiers = new List<BaseModifier>();

        private void Awake()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _originalMesh = meshFilter.sharedMesh;
            _modifiedMesh = Instantiate(_originalMesh);
            meshFilter.mesh = _modifiedMesh;
            _originalTransform = transform;
        }

        private void Update()
        {
            foreach (var baseModifier in _activeModifiers)
            {
                baseModifier.Update(gameObject);
            }
        }

        public void AddModifier(BaseModifier modifier)
        {
            _activeModifiers.Add(modifier);
            ApplyModifiers();
        }

        public void RemoveModifier(BaseModifier modifier)
        {
            _activeModifiers.Remove(modifier);
            ApplyModifiers();
        }

        private void ApplyModifiers()
        {
            _modifiedMesh.vertices = _originalMesh.vertices.Clone() as Vector3[];

            foreach (var meshModifier in _activeModifiers)
            {
                meshModifier.Apply(_modifiedMesh);
            }

            _modifiedMesh.RecalculateNormals();
        }

        private void OnDisable()
        {
            GetComponent<MeshFilter>().mesh = _originalMesh;
            foreach (var activeModifier in _activeModifiers)
            {
                activeModifier.SpecialDestroy(gameObject.transform);
            }
        }
    }
}