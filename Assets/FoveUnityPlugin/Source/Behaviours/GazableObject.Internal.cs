
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fove.Unity
{
    public partial class GazableObject : MonoBehaviour
    {
        private static int instanceCount = 0;
        private static readonly Dictionary<int, GazableObject> idToGazableObject = new Dictionary<int, GazableObject>();

        protected int id = Fove.GazableObject.IdInvalid;

        protected bool isIdValid { get { return id != Fove.GazableObject.IdInvalid; } }

        protected ObjectPose pose;

        //To measure velocity of the object
        protected Rigidbody rigidbodyForVelocitySource;
        protected Vector3 lastPosition;
        protected float positionShiftTime;
        protected Vector3 positionShift;

        protected class UnityMeshCollider : MeshColliderBase<Vector3, int>
        {
            protected override int GetIndexCount()
            {
                return indices.Length / 3;
            }
            protected override NativeCollider GetNativeCollider()
            {
                if (indices.Length % 3 != 0)
                    throw new ArgumentException("Indices array size should be a multiple of 3");

                return base.GetNativeCollider();
            }
        }
        
        internal static GazableObject FindGazableObject(int id)
        {
            GazableObject obj;
            idToGazableObject.TryGetValue(id, out obj);
            return obj;
        }

        private static void CreateFromColliders(Collider[] colliders)
        {
            foreach (var collider in colliders)
            {
                if (!IsColliderSupported(collider))
                {
                    Debug.LogWarning("Encountered an unsupported collider type '" + collider.GetType().Name +
                        "'. Gazable objects won't be created for this collider.");
                }
                else if (collider.GetComponent<GazableObject>() == null)
                {
                    collider.gameObject.AddComponent<GazableObject>();
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (!isIdValid)
                Initialize();

            UpdatePose();

            var result = FoveManager.Headset.UpdateGazableObject(id, ref pose);
            if (result.Failed)
                Debug.LogError("Failed to update GazableObject. Error: " + result.error);
        }

        protected virtual void FixedUpdate()
        {
            if (velocitySource == VelocitySource.FixedUpdate)
            {
                positionShiftTime += Time.fixedDeltaTime;
                positionShift += (transform.position - lastPosition);
                lastPosition = transform.position;
            }
        }

        protected virtual void UpdatePose()
        {
            pose.position = Utils.ToVec3(transform.position);
            pose.rotation = Utils.ToQuat(transform.rotation);
            pose.scale = Utils.ToVec3(transform.lossyScale);

            if (velocitySource == VelocitySource.LateUpdate)
            {
                pose.velocity = Utils.ToVec3((transform.position - lastPosition) / Time.deltaTime);
                lastPosition = transform.position;
            }
            else if (velocitySource == VelocitySource.FixedUpdate)
            {
                if (positionShiftTime != 0)
                    pose.velocity = Utils.ToVec3(positionShift / positionShiftTime);
                positionShift = Vector3.zero;
                positionShiftTime = 0;
            }
            else if (velocitySource == VelocitySource.Rigidbody)
            {
                pose.velocity = Utils.ToVec3(rigidbodyForVelocitySource.velocity);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            // Do not remove the gazable object when the application quits.
            // Unfortunately Unity do not waranty any order of destruction for the objects and
            // this generates issues with the FoveManager singleton that can already be already destroyed
            id = Fove.GazableObject.IdInvalid;
        }

        protected virtual void OnDisable()
        {
            DestroyGazableObject();
        }

        protected virtual void DestroyGazableObject()
        {
            if (isIdValid)
            {
                var result = FoveManager.Headset.RemoveGazableObject(id);
                if (result.Failed)
                    Debug.LogError("GazableObject: remove gazable object returned error: " + result.error);

                idToGazableObject.Remove(id);
            }

            id = Fove.GazableObject.IdInvalid;
        }

        protected virtual void Initialize()
        {
            id = instanceCount++;
            idToGazableObject[id] = this;

            OnValidate();
            if (velocitySource == VelocitySource.Rigidbody)
            {
                rigidbodyForVelocitySource = GetComponent<Rigidbody>();
                if (rigidbodyForVelocitySource == null)
                    rigidbodyForVelocitySource = GetComponentInParent<Rigidbody>();
            }
            lastPosition = transform.position;
            UpdatePose();

            var colliders = new List<ObjectCollider>();

            foreach (var collider in GetComponents<Collider>())
            {
                if (!collider.enabled)
                    continue;

                var boxCollider = collider as BoxCollider;
                if (boxCollider != null)
                {
                    colliders.Add(new CubeCollider
                    {
                        center = Utils.ToVec3(boxCollider.center),
                        size = Utils.ToVec3(boxCollider.size)
                    });
                }

                var sphereCollider = collider as UnityEngine.SphereCollider;
                if (sphereCollider != null)
                {
                    colliders.Add(new SphereCollider
                    {
                        center = Utils.ToVec3(sphereCollider.center),
                        radius = sphereCollider.radius
                    });
                }

                var meshCollider = collider as UnityEngine.MeshCollider;
                if (meshCollider != null)
                {
                    var mesh = meshCollider.sharedMesh;
                    colliders.Add(new UnityMeshCollider
                    {
                        center = new Vec3(),
                        indices = mesh.triangles,
                        vertices = mesh.vertices,
                        boundingBox = Utils.ToBoundingBox(mesh.bounds)
                    });
                }
            }

            var gazableObj = new Fove.GazableObject
            {
                id = id,
                group = (ObjectGroup)(1 << gameObject.layer),
                colliders = colliders,
                pose = pose,
            };

            var result = FoveManager.Headset.RegisterGazableObject(gazableObj);
            if (result.Failed)
                Debug.LogError("Failed to create GazableObject. Error: " + result.error);
        }

        private static bool IsColliderSupported(Collider collider)
        {
            return collider is BoxCollider || collider is UnityEngine.SphereCollider || collider is UnityEngine.MeshCollider;
        }

        private void OnValidate()
        {
            bool rigidBodyFound = GetComponent<Rigidbody>() != null || GetComponentInParent<Rigidbody>() != null;
            if (velocitySource == VelocitySource.Rigidbody && !rigidBodyFound)
            {
                Debug.LogError("'" + gameObject.name + "': can't set velocity source to 'rigitBody', no Rigidbody component exists on this object or its parent. Setting velocity source to 'LateUpdate'.", this);
                velocitySource = VelocitySource.LateUpdate;
            }
            if (rigidBodyFound && velocitySource != VelocitySource.Rigidbody)
            {
                Debug.LogWarning("'" + gameObject.name + "': a Rigidbody component was found. Changing the velocity source to 'Rigidbody'.", this);
                velocitySource = VelocitySource.Rigidbody;
            }
        }
    }
}
