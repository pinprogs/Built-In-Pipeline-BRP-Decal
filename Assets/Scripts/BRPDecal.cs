using UnityEngine;

namespace SingleCMD
{
    public class BRPDecal : MonoBehaviour
    {
        public Color color;
        public Matrix4x4 worldToLocalMatrix => transform.worldToLocalMatrix;

        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;


        private void OnEnable()
        {
            BRPDecalManager.Instance.Register(this);
            CacheTransform();
        }

        private void OnDisable()
        {
            BRPDecalManager.Instance.Unregister(this);
        }

        void Update()
        {
            if (HasTransformChanged())
            {
                CacheTransform();
                BRPDecalManager.Instance.RequestRebuild();
            }
        }

        /// <summary>
        /// 기존 Transform 정보 캐싱
        /// </summary>
        void CacheTransform()
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastScale = transform.localScale;
        }

        /// <summary>
        /// Transform 정보가 바뀌었는지 체크
        /// </summary>
        /// <returns></returns>
        bool HasTransformChanged()
        {
            return transform.position != lastPosition ||
                   transform.rotation != lastRotation ||
                   transform.localScale != lastScale;
        }


#if UNITY_EDITOR
        private BRPDecalGizmos gizmos;

        private void OnDrawGizmos()
        {
            if (gizmos == null)
                gizmos = new BRPDecalGizmos();
            gizmos.Draw(transform);
        }
#endif
    }
}