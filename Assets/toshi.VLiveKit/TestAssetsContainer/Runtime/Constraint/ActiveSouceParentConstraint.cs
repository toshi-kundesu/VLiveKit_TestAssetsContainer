using UnityEngine;
using UnityEngine.Animations;

[ExecuteInEditMode]
[RequireComponent(typeof(ParentConstraint))]
public class ActiveSourceParentConstraint : MonoBehaviour
{
    private ParentConstraint _parentConstraint;

    private void Awake()
    {
        // 同じGameObjectにアタッチされているParentConstraintを取得
        _parentConstraint = GetComponent<ParentConstraint>();
    }

    private void Update()
    {
        // ParentConstraintに設定されているソース数だけループ
        for (int i = 0; i < _parentConstraint.sourceCount; i++)
        {
            // i番目のソース情報を取得
            ConstraintSource src = _parentConstraint.GetSource(i);

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
            _parentConstraint.SetSource(i, src);
        }
    }
}