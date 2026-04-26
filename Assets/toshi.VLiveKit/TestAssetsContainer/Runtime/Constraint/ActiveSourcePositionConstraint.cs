using UnityEngine;
using UnityEngine.Animations;

[ExecuteInEditMode]
[RequireComponent(typeof(PositionConstraint))]
public class ActiveSourcePositionConstraint : MonoBehaviour
{
    private PositionConstraint _posConstraint;

    private void Awake()
    {
        // 同じGameObjectにアタッチされているPositionConstraintを取得
        _posConstraint = GetComponent<PositionConstraint>();
    }

    private void Update()
    {
        // PositionConstraintに設定されているソース数だけループ
        for (int i = 0; i < _posConstraint.sourceCount; i++)
        {
            // i番目のソース情報を取得
            ConstraintSource src = _posConstraint.GetSource(i);

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
            _posConstraint.SetSource(i, src);
        }
    }
}
