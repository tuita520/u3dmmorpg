using UnityEngine;
using System.Collections;

namespace GameUI
{
    public class SkillBallDrawMove : MonoBehaviour
    {

        private void OnDrag(Vector2 delta)
        {
            if (null != SkillBallDrawMoveSpinning.m_Instance)
            {
                SkillBallDrawMoveSpinning.m_Instance.OnDrag(delta);
            }
        }

        private void OnPress(bool press)
        {
            if (null != SkillBallDrawMoveSpinning.m_Instance)
            {
                  SkillBallDrawMoveSpinning.m_Instance.OnPress(press);
            }
        }
    }
}
