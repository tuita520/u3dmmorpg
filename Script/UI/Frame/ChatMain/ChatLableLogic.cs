using ScriptManager;
using System.Collections;
using System;
#region using
using EventSystem;
using ClientDataModel;
using DataTable;
using UnityEngine;

#endregion

public class ChatLableLogic : MonoBehaviour
{
    public CharInfoNode InfoNode { get; set; }
    public int param { get; set; }
    public void OnClickLinkText()
    {
        if (param == -1)
        {                   
            EventDispatcher.Instance.DispatchEvent(new ChatMainOperate(10));
            return;
        }
        if (InfoNode.LinkType == eChatLinkType.Equip)
        {
            var bagItemData = new BagItemDataModel();
            bagItemData.ItemId = InfoNode.NodeData.Id;
            //if (bagItemData.ItemId >= 600000 && bagItemData.ItemId <= 600102)
            //{
            //    PlayerDataManager.Instance.NoticeData.GongGaoMaYaWeapon = true;
            //}
            bagItemData.Exdata.InstallData(InfoNode.NodeData.ExData);
            GameUtils.ShowItemDataTip(bagItemData);
        }
        else if (InfoNode.LinkType == eChatLinkType.Postion)
        {
            var sceneId = InfoNode.NodeData.ExData[0];
            var tbScene = Table.GetScene(sceneId);
            if (tbScene == null)
            {
                return;
            }
            var lv = PlayerDataManager.Instance.GetLevel();
            if (lv < tbScene.LevelLimit)
            {
//等级不足，无法进入此地图
                GameUtils.ShowHintTip(210207);
                return;
            }

            var mieShiForceGo = false;
            if (SceneManager.Instance.IsMieShiFuben(sceneId))
            { // 灭世副本,默认同时只存在一个
                if (SceneManager.Instance.isInMieshiFuben())
                {
                    mieShiForceGo = true;
                }
                else
                {
                    GameUtils.ShowHintTip(300000109);
                    return;
                }
            }

            if (!mieShiForceGo)
            {
                if (PlayerDataManager.Instance.IsInFubenScnen())
                {
                    //副本中无法传送
                    GameUtils.ShowHintTip(210208);
                    return;
                }
            }
            
            GameControl.Executer.Stop();
            var x = InfoNode.NodeData.ExData[1];
            var y = InfoNode.NodeData.ExData[2];
            var command = GameControl.GoToCommand(sceneId, x/100.0f, y/100.0f, 1.0f);
            GameControl.Executer.PushCommand(command);
        }
        else if (InfoNode.LinkType == eChatLinkType.Character)
        {
            if (InfoNode.CharacterId == PlayerDataManager.Instance.GetGuid())
            {
                return;
            }
            var worldPos = UICamera.currentCamera.ScreenToWorldPoint(UICamera.lastTouchPosition);
            var localPos = transform.root.InverseTransformPoint(worldPos);
            localPos.z = 0;
            UIConfig.OperationList.Loction = localPos;
            PlayerDataManager.Instance.ShowCharacterPopMenu(InfoNode.CharacterId, InfoNode.InfoContent, 10);
        }
        else if (InfoNode.LinkType == eChatLinkType.Team)
        {
            if (InfoNode.CharacterId == PlayerDataManager.Instance.GetGuid())
            {
                return;
            }
            var tbFuben = Table.GetFuben(InfoNode.NodeData.Id);
            if (tbFuben == null)
                return;

            



            var characterId = InfoNode.NodeData.ExData64[0];
            var teamId = InfoNode.NodeData.ExData64[1];
            EventDispatcher.Instance.DispatchEvent(new Event_TeamApplyOtherTeam(characterId));




        }
        else if (InfoNode.LinkType == eChatLinkType.NewTeam)
        {
            if (InfoNode.CharacterId == PlayerDataManager.Instance.GetGuid())
            {
                return;
            }
            if (InfoNode.NodeData.ExData.Count > 1)
            {
                int minLv = InfoNode.NodeData.ExData[1];
                int maxLv = InfoNode.NodeData.ExData[2];
                if (PlayerDataManager.Instance.GetLevel() < minLv || PlayerDataManager.Instance.GetLevel() > maxLv)
                {
                    GameUtils.ShowHintTip(100001473);
                    return;
                }
            }
            var characterId = InfoNode.NodeData.ExData64[0];
            var teamId = InfoNode.NodeData.ExData64[1];
          
            EventDispatcher.Instance.DispatchEvent(new Event_TeamApplyOtherTeam(characterId));
        }
    }

    private void Start()
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

}