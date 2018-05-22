#region using

using System.Collections.Generic;
using System.Collections.ObjectModel;
using ClientDataModel;
using DataTable;
using EventSystem;

#endregion

public static class ActivityDataModelExtension
{
    public static void Clone(this ActivityCellDataModel dataModel, ActivityCellDataModel otherModel)
    {
        dataModel.iconId = otherModel.iconId;
        dataModel.name = otherModel.name;
        dataModel.desc = otherModel.desc;
        dataModel.openDesc = otherModel.openDesc;
        dataModel.openedDesc = otherModel.openedDesc;
        dataModel.Time = otherModel.Time;
        dataModel.isShowTime = true;
        dataModel.isShowTime = false;
        dataModel.isShowTime = otherModel.isShowTime;

        dataModel.isShowOPendDesc = true;
        dataModel.isShowOPendDesc = false;
        dataModel.isShowOPendDesc = otherModel.isShowOPendDesc;

        dataModel.enterCount = otherModel.enterCount;
        dataModel.maxCount = otherModel.maxCount;
        dataModel.type = otherModel.type;
        dataModel.fuBenId = otherModel.fuBenId;
        dataModel.order = otherModel.order;
        dataModel.timeState = otherModel.timeState;
        dataModel.QueueState = otherModel.QueueState;
        dataModel.isShowYuYueBtn = otherModel.isShowYuYueBtn;
        dataModel.tableId = otherModel.tableId;
        dataModel.TiliValue = otherModel.TiliValue;
        dataModel.TiliMaxValue = otherModel.TiliMaxValue;
        dataModel.TiliPercent = otherModel.TiliPercent;
        dataModel.TiliBuyCount = otherModel.TiliBuyCount;
        dataModel.TIliBuyMaxCount = otherModel.TIliBuyMaxCount;
        dataModel.LeiJiExp = otherModel.LeiJiExp;
        dataModel.NeedDiamond = otherModel.NeedDiamond;
        dataModel.IsShowExp = otherModel.IsShowExp;
        dataModel.LeftTime = string.Empty;
        dataModel.LeftTime = otherModel.LeftTime;
        dataModel.isGrey = otherModel.isGrey;

        dataModel.worldMosnterBtns.ModelId = otherModel.worldMosnterBtns.ModelId;
        dataModel.worldMosnterBtns.CurBtn.Index = otherModel.worldMosnterBtns.CurBtn.Index;
        dataModel.worldMosnterBtns.CurBtn.TableId = otherModel.worldMosnterBtns.CurBtn.TableId;
        dataModel.worldMosnterBtns.CurBtn.Selected = otherModel.worldMosnterBtns.CurBtn.Selected;
        dataModel.worldMosnterBtns.CurBtn.Enabled = otherModel.worldMosnterBtns.CurBtn.Enabled;
        dataModel.worldMosnterBtns.Btns.Clear();
        foreach (var btn in otherModel.worldMosnterBtns.Btns)
        {
            BtnState temp = new BtnState();
            temp.Index = btn.Index;
            temp.TableId = btn.TableId;
            temp.Selected = btn.Selected;
            temp.Enabled = btn.Enabled;
            dataModel.worldMosnterBtns.Btns.Add(temp);
        }
        EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityModelChangeEvent());
    }
}