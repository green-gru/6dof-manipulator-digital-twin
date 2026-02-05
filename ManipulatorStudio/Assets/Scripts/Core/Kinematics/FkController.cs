using UnityEngine;

/// <summary>
/// 6축 매니퓰레이터의 FK를 적용합니다. | Applies FK for a 6-axis manipulator.
/// </summary>
public sealed class FkController : MonoBehaviour
{
    private const int JOINT_COUNT = 6;
    private const float AXIS_EPSILON = 1e-6f;
    private const float DIRTY_EPSILON_DEG = 0.0001f;

    [Header("Joint Transforms")]
    [SerializeField] private Transform _j1;
    [SerializeField] private Transform _j2;
    [SerializeField] private Transform _j3;
    [SerializeField] private Transform _j4;
    [SerializeField] private Transform _j5;
    [SerializeField] private Transform _j6;

    [Header("Joint Angles (Deg)")]
    [SerializeField] private float _j1Deg;
    [SerializeField] private float _j2Deg;
    [SerializeField] private float _j3Deg;
    [SerializeField] private float _j4Deg;
    [SerializeField] private float _j5Deg;
    [SerializeField] private float _j6Deg;

    [Header("Joint Limits (Deg)")]
    [SerializeField] private bool _useLimits = true;
    [SerializeField] private float _j1MinDeg = -180f;
    [SerializeField] private float _j1MaxDeg = 180f;
    [SerializeField] private float _j2MinDeg = -180f;
    [SerializeField] private float _j2MaxDeg = 180f;
    [SerializeField] private float _j3MinDeg = -180f;
    [SerializeField] private float _j3MaxDeg = 180f;
    [SerializeField] private float _j4MinDeg = -180f;
    [SerializeField] private float _j4MaxDeg = 180f;
    [SerializeField] private float _j5MinDeg = -180f;
    [SerializeField] private float _j5MaxDeg = 180f;
    [SerializeField] private float _j6MinDeg = -180f;
    [SerializeField] private float _j6MaxDeg = 180f;

    [Header("Joint Axes (Local)")]
    [SerializeField] private Vector3 _j1Axis = Vector3.right;
    [SerializeField] private Vector3 _j2Axis = Vector3.right;
    [SerializeField] private Vector3 _j3Axis = Vector3.right;
    [SerializeField] private Vector3 _j4Axis = Vector3.right;
    [SerializeField] private Vector3 _j5Axis = Vector3.right;
    [SerializeField] private Vector3 _j6Axis = Vector3.right;

    private readonly Quaternion[] _baseLocalRotations = new Quaternion[JOINT_COUNT];
    private readonly float[] _lastAppliedDeg = new float[JOINT_COUNT];
    private readonly bool[] _missingWarnings = new bool[JOINT_COUNT];
    private readonly bool[] _axisWarnings = new bool[JOINT_COUNT];

    private bool _hasBaseRotations;
    private bool _forceApply;

    /// <summary>
    /// 초기 로컬 회전을 저장합니다. | Caches initial local rotations.
    /// </summary>
    private void Awake()
    {
        CacheBaseRotations();
        _forceApply = true;
    }

    /// <summary>
    /// 활성화 시 베이스 회전을 다시 저장합니다. | Recaches base rotations on enable.
    /// </summary>
    private void OnEnable()
    {
        CacheBaseRotations();
        _forceApply = true;
    }

    /// <summary>
    /// FK를 LateUpdate에서 적용합니다. | Applies FK in LateUpdate.
    /// </summary>
    private void LateUpdate()
    {
        if (!AreJointsValid())
        {
            return;
        }

        if (!_hasBaseRotations)
        {
            CacheBaseRotations();
        }

        if (!_forceApply && !IsDirty())
        {
            return;
        }

        ApplyFk();
        _forceApply = false;
    }

    /// <summary>
    /// 조인트 로컬 회전을 캐시합니다. | Caches joint local rotations.
    /// </summary>
    private void CacheBaseRotations()
    {
        _hasBaseRotations = false;

        for (int i = 0; i < JOINT_COUNT; i++)
        {
            Transform joint = GetJointByIndex(i);
            if (joint == null)
            {
                continue;
            }

            _baseLocalRotations[i] = joint.localRotation;
        }

        _hasBaseRotations = true;
    }

    /// <summary>
    /// FK를 적용합니다. | Applies FK rotations.
    /// </summary>
    private void ApplyFk()
    {
        for (int i = 0; i < JOINT_COUNT; i++)
        {
            Transform joint = GetJointByIndex(i);
            if (joint == null)
            {
                continue;
            }

            if (!TryGetClampedAngle(i, out float clampedDeg))
            {
                return;
            }

            Vector3 axis = GetNormalizedAxis(i);
            Quaternion rotation = Quaternion.AngleAxis(clampedDeg, axis);
            joint.localRotation = _baseLocalRotations[i] * rotation;
            _lastAppliedDeg[i] = clampedDeg;
        }
    }

    /// <summary>
    /// 조인트 유효성을 확인합니다. | Validates joint references.
    /// </summary>
    private bool AreJointsValid()
    {
        bool allValid = true;

        for (int i = 0; i < JOINT_COUNT; i++)
        {
            if (GetJointByIndex(i) == null)
            {
                if (!_missingWarnings[i])
                {
                    Debug.LogWarning($"조인트 누락: J{i + 1} | Missing joint: J{i + 1}", this);
                    _missingWarnings[i] = true;
                }
                allValid = false;
            }
        }

        return allValid;
    }

    /// <summary>
    /// 값 변경 여부를 체크합니다. | Checks if values changed.
    /// </summary>
    private bool IsDirty()
    {
        return Mathf.Abs(_j1Deg - _lastAppliedDeg[0]) > DIRTY_EPSILON_DEG
            || Mathf.Abs(_j2Deg - _lastAppliedDeg[1]) > DIRTY_EPSILON_DEG
            || Mathf.Abs(_j3Deg - _lastAppliedDeg[2]) > DIRTY_EPSILON_DEG
            || Mathf.Abs(_j4Deg - _lastAppliedDeg[3]) > DIRTY_EPSILON_DEG
            || Mathf.Abs(_j5Deg - _lastAppliedDeg[4]) > DIRTY_EPSILON_DEG
            || Mathf.Abs(_j6Deg - _lastAppliedDeg[5]) > DIRTY_EPSILON_DEG;
    }

    /// <summary>
    /// 각도를 클램프하고 검증합니다. | Clamps and validates angle.
    /// </summary>
    private bool TryGetClampedAngle(int index, out float clampedDeg)
    {
        clampedDeg = GetAngleByIndex(index);
        if (float.IsNaN(clampedDeg) || float.IsInfinity(clampedDeg))
        {
            Debug.LogWarning($"유효하지 않은 각도: J{index + 1} | Invalid angle: J{index + 1}", this);
            return false;
        }

        if (!_useLimits)
        {
            return true;
        }

        GetLimitsByIndex(index, out float minDeg, out float maxDeg);
        clampedDeg = Mathf.Clamp(clampedDeg, minDeg, maxDeg);
        return true;
    }

    /// <summary>
    /// 로컬 축을 정규화해 반환합니다. | Returns a normalized local axis.
    /// </summary>
    private Vector3 GetNormalizedAxis(int index)
    {
        Vector3 axis = GetAxisByIndex(index);
        if (axis.sqrMagnitude < AXIS_EPSILON)
        {
            if (!_axisWarnings[index])
            {
                Debug.LogWarning($"조인트 축이 0에 가깝습니다: J{index + 1} | Joint axis near zero: J{index + 1}", this);
                _axisWarnings[index] = true;
            }
            return Vector3.right;
        }

        return axis.normalized;
    }

    /// <summary>
    /// 인덱스로 조인트를 가져옵니다. | Gets joint by index.
    /// </summary>
    private Transform GetJointByIndex(int index)
    {
        return index switch
        {
            0 => _j1,
            1 => _j2,
            2 => _j3,
            3 => _j4,
            4 => _j5,
            5 => _j6,
            _ => null
        };
    }

    /// <summary>
    /// 인덱스로 각도를 가져옵니다. | Gets angle by index.
    /// </summary>
    private float GetAngleByIndex(int index)
    {
        return index switch
        {
            0 => _j1Deg,
            1 => _j2Deg,
            2 => _j3Deg,
            3 => _j4Deg,
            4 => _j5Deg,
            5 => _j6Deg,
            _ => 0f
        };
    }

    /// <summary>
    /// 인덱스로 축을 가져옵니다. | Gets axis by index.
    /// </summary>
    private Vector3 GetAxisByIndex(int index)
    {
        return index switch
        {
            0 => _j1Axis,
            1 => _j2Axis,
            2 => _j3Axis,
            3 => _j4Axis,
            4 => _j5Axis,
            5 => _j6Axis,
            _ => Vector3.right
        };
    }

    /// <summary>
    /// 인덱스로 제한각을 가져옵니다. | Gets limits by index.
    /// </summary>
    private void GetLimitsByIndex(int index, out float minDeg, out float maxDeg)
    {
        switch (index)
        {
            case 0:
                minDeg = _j1MinDeg;
                maxDeg = _j1MaxDeg;
                break;
            case 1:
                minDeg = _j2MinDeg;
                maxDeg = _j2MaxDeg;
                break;
            case 2:
                minDeg = _j3MinDeg;
                maxDeg = _j3MaxDeg;
                break;
            case 3:
                minDeg = _j4MinDeg;
                maxDeg = _j4MaxDeg;
                break;
            case 4:
                minDeg = _j5MinDeg;
                maxDeg = _j5MaxDeg;
                break;
            case 5:
                minDeg = _j6MinDeg;
                maxDeg = _j6MaxDeg;
                break;
            default:
                minDeg = -180f;
                maxDeg = 180f;
                break;
        }
    }
}
