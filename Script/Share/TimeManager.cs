#region using

using System;
using System.Collections.Generic;
using System.Linq;
using C5;

#endregion

namespace Shared
{

    #region 触发器

    public class Trigger : IComparable<Trigger>
    {
        public Trigger(DateTime tt, ulong triggerId, Action func, int autoInterval)
        {
            DueTime = tt;
            TriggerId = triggerId;
            Func = func;
            AutoInterval = autoInterval;
        }

        public int AutoInterval; //循环触发器的间隔(毫秒)
        public DateTime DueTime; //触发器的下次生效时间
        private readonly Action Func; //回调函数
        public IPriorityQueueHandle<Trigger> Handle;
        public bool bDolready { get; set; } //在一次Updata中保证只会触发一次
        public ulong TriggerId { get; private set; } //触发器的唯一ID
        //执行触发后的效果
        public void DoFunction()
        {
            if (bDolready)
            {
                Logger.Warn("Trigger::DoFunction  bDolready is True!");
                return; //保证只会触发修改一遍
            }
            bDolready = true;
            //执行lambda表达式
            try
            {
                Func();
            }
            catch (Exception ex)
            {
                Logger.Error("Trigger::DoFunction Do Func Fatal!TriggerId=" + TriggerId + "  Time=" + DueTime +
                             "Exception " + ex);
                AutoInterval = 0;
            }
        }

        //获取下次生效时间
        public DateTime GetNextTime()
        {
            return DueTime;
        }

        public int CompareTo(Trigger other)
        {
            if (DueTime < other.DueTime)
            {
                return -1;
            }
            if (DueTime == other.DueTime)
            {
                return 0;
            }
            return 1;
        }
    }

    #endregion

    //触发器管理器
    public class TimeManager
    {
        private static TimeManager _instance;

        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TimeManager();
                }
                return _instance;
            }
        }

        public void CleanUp()
        {
            mTimers = new IntervalHeap<Trigger>();
        }

        #region 心跳

        //心跳
        public void Updata()
        {
            try
            {
                if (mTimers.Count <= 0)
                {
                    return;
                }

                var maxLoop = 1000;

                var trigger = mTimers.FindMin();
                while (trigger.DueTime < Game.Instance.ServerTime)
                {
                    maxLoop--;
                    if (maxLoop <= 0)
                    {
                        Logger.Warn("TimeManager update loop over 1000 times.");
                        break;
                    }

                    try
                    {
                        mCurrentTrigger = trigger;
                        if (DoTrigger(trigger))
                        {
                            mCurrentTrigger = null;
                            DeleteTrigger(trigger);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Trigger DoFunction has exception.", ex);
                    }

                    if (mTimers.Count <= 0)
                    {
                        return;
                    }
                    trigger = mTimers.FindMin();
                }
            }
            catch (ArgumentNullException argumentNullException)
            {
                Logger.Error("TimeManager::Updata  TriggerListByTime.Keys.Min  is null", argumentNullException);
            }
            catch (KeyNotFoundException keyNotFoundException)
            {
                Logger.Error("TimeManager::Updata  keyNotFoundException ", keyNotFoundException);
            }
            catch (Exception ex)
            {
                Logger.Error("TimeManager::Updata   ", ex);
            }
        }

        #endregion

        #region 数据结构

        private ulong NextTriggerId = 1;
//         private readonly Dictionary<ulong, Trigger> TriggerListById = new Dictionary<ulong, Trigger>();
//         private readonly Dictionary<DateTime, List<ulong>> TriggerListByTime = new Dictionary<DateTime, List<ulong>>();
//         private readonly Dictionary<ulong, Trigger> dolist = new Dictionary<ulong, Trigger>();

        private IntervalHeap<Trigger> mTimers = new IntervalHeap<Trigger>();
        private Trigger mCurrentTrigger;

        #endregion

        #region 对外接口(增删改查)

        //创建触发器
        public object CreateTrigger(DateTime triggerTime,
                                    Action act,
                                    int autoInterval = 0,
                                    string filename = "",
                                    string member =
                                        "",
                                    int line = 0)
        {
            if (act == null)
            {
                Logger.Error("TimeManager::CreateTrigger act == null, called from {0} at {1}:{2}.", member, filename,
                    line);
                return null;
            }

            var getid = GetNextId();
            var trigger = new Trigger(triggerTime, getid, act, autoInterval);
            Logger.Info("TimeManager::CreateTrigger Id={0} Time={1} ", getid, triggerTime);
            PushTrigger(getid, trigger);
            return trigger;
        }

        //删除触发器(直接删 或者标记将要删)

        public void DeleteTrigger(object obj,
                                  string filename = "",
                                  string member =
                                      "",
                                  int line = 0)
        {
            if (obj == null)
            {
                Logger.Error("TimeManager::DeleteTrigger  obj == null, called from {0} at {1}:{2}.", member, filename,
                    line);
                return;
            }

            var trigger = (Trigger) obj;
            if (mCurrentTrigger != trigger && mTimers.Contains(trigger))
            {
                mTimers.Delete(trigger.Handle);
            }
            trigger.AutoInterval = 0;
        }

        //修改触发器时间
        public void ChangeTime(object obj,
                               DateTime newTime,
                               int autoInterval = -1,
                               string filename = "",
                               string member =
                                   "",
                               int line = 0)
        {
            if (obj == null)
            {
                Logger.Warn("ChangeTime obj == null, called from {0} at {1}:{2}.", member, filename, line);
                return;
            }

            var trigger = (Trigger) obj;
            mTimers.Delete(trigger.Handle);
            trigger.DueTime = newTime;
            if (autoInterval > -1)
            {
                trigger.AutoInterval = autoInterval;
            }
            PushTrigger(trigger.TriggerId, trigger);
        }

        //获取下次生效时间
        public DateTime GetNextTime(object obj)
        {
            var trigger = (Trigger) obj;
            if (trigger == null)
            {
                return Game.Instance.ServerTime;
            }
            return trigger.GetNextTime();
        }

        #endregion

        #region 私有(Private)

        //删除触发器(删数据)
        private void DeleteTrigger(Trigger value)
        {
            mTimers.Delete(value.Handle);
            value.AutoInterval = 0;
        }

        //执行触发器
        private bool DoTrigger(Trigger value)
        {
            //Logger.Trace("DoTrigger id={0}", value.TriggerId);
            value.DoFunction();
            if (value.AutoInterval > 0)
            {
                value.bDolready = false;
#if DEBUG
                var temp = Game.Instance.ServerTime;
#else
                DateTime temp = value.DueTime;
#endif
                var temp2 = temp.AddMilliseconds(value.AutoInterval);
                ChangeTime(value, temp2);
                return false;
            }
            return true;
        }

        //触发器压栈
        private void PushTrigger(ulong id, Trigger trigger)
        {
            if (mTimers.Add(ref trigger.Handle, trigger))
            {
                //Logger.Info("PushTrigger id:{0}", id);
                return;
            }

            Logger.Error("PushTrigger id:{0}", id);
        }

        //触发器ID管理
        private ulong GetNextId()
        {
            return NextTriggerId++;
        }

        #endregion
    }
}