using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DataContract;
using DataTable;

public class IconEffect : MonoBehaviour
{
    public UISprite mSprite;
    private List<string> _path = new List<string>();
    private float _frameRate;
    private bool _isLoop;
    private string _atlas;
    private float _fps;
    private int _index;

    private int _id;
    public int _ItemId
    {
        get { return _id; }
        set
        {
            if (_id == value)
                return;
            _id = value;
            Init();
        }
    }

    void Init()
    {
        _path.Clear();
        var tbItem = Table.GetItemBase(_ItemId);
        bool bActive = false;
        do
        {
            if (tbItem == null)
                break;
            if (tbItem.EffectID <= 0)
            {
                break;
            }
            var tbEffect2D = Table.GetEffect2D(tbItem.EffectID);
            if (tbEffect2D != null)
            {
                foreach (var iconId in tbEffect2D.EveryIcon)
                {
                    var tbIcon = Table.GetIcon(iconId);
                    {
                        _atlas = tbIcon.Atlas;
                        _path.Add(tbIcon.Sprite);
                    }
                }
                //mSprite.atlas.name = _atlas;
                _index = 0;
                _isLoop = tbEffect2D.IsLoop>0;
                _frameRate = tbEffect2D.FrameRate;
                bActive = true;
            }            
        } while (false);
        gameObject.SetActive(bActive);

    }
    void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    void Update()
    {
#if !UNITY_EDITOR
try
{
#endif

        _fps += Time.deltaTime*1000;
        if (_fps >= _frameRate)
        {
            NextFrame();
            _fps -= _frameRate;
        }
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    void NextFrame()
    {
        if (++_index >= _path.Count)
        {
            if (_isLoop == true)
            {
                _index = 0;
            }
            else
            {
                gameObject.SetActive(false);
                return;
            }
        }
        mSprite.spriteName = _path[_index];

    }

}
