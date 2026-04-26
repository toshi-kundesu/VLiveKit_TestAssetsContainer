using UnityEngine;
using UnityEngine.Animations;

[ExecuteInEditMode]
[RequireComponent(typeof(RotationConstraint))]
public class ActiveSourceRotationConstraint : MonoBehaviour
{
    private RotationConstraint _rotConstraint;

    private void Awake()
    {
        // 同じGameObjectにアタッチされているRotationConstraintを取得
        _rotConstraint = GetComponent<RotationConstraint>();
    }

    private void Update()
    {
        // RotationConstraintに設定されているソース数だけループ
        for (int i = 0; i < _rotConstraint.sourceCount; i++)
        {
            // i番目のソース情報を取得
            ConstraintSource src = _rotConstraint.GetSource(i);

            // ソースのTransformが存在し、かつアクティブかどうかをチェック
            if (src.sourceTransform != null && src.sourceTransform.gameObject.activeInHierarchy)
            {
                // アクティブならWeightを1にする（Constraint有効）
                src.weight = 1f;
            }
            else
            {
                // 非アクティブならWeightを0にする（Constraint無効）
                src.weight = 0f;
            }

            // 変更したWeightをConstraintに反映
            _rotConstraint.SetSource(i, src);
        }
    }
}
