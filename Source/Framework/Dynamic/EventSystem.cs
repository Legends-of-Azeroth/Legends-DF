﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Dynamic
{
    public class EventSystem
    {
        readonly List<double> _removeKeys = new List<double>();
        public EventSystem()
        {
            m_time = 0;
        }

        public void Update(uint p_time)
        {
            // update time
            m_time += p_time;
            KeyValuePair<double, BasicEvent> i = default;
            
            // main event loop
            lock (m_events)
                if (m_events.Count > 0)
                    while ((i = m_events.KeyValueList().FirstOrDefault()).Value != null && i.Key <= m_time)
                    {
                        // sorted dictionart will stop looping at the first time that does not meet the while condition
                        var Event = i.Value;
                        m_events.Remove(i);

                        if (Event.IsRunning())
                        {
                            Event.Execute(m_time, p_time);
                            continue;
                        }

                        if (Event.IsAbortScheduled())
                        {
                            Event.Abort(m_time);
                            // Mark the event as aborted
                            Event.SetAborted();
                        }

                        if (Event.IsDeletable())
                            continue;

                        // Reschedule non deletable events to be checked at
                        // the next update tick
                        InternalAddEvent(Event, CalculateTime(TimeSpan.FromMilliseconds(1)), false);
                    }
        }

        public void KillAllEvents(bool force)
        {
            lock (m_events)
                m_events.RemoveIfMatching((pair) =>
                {
                    // Abort events which weren't aborted already
                    if (!pair.Value.IsAborted())
                    {
                        pair.Value.SetAborted();
                        pair.Value.Abort(m_time);
                    }

                    // Skip non-deletable events when we are
                    // not forcing the event cancellation.
                    if (!force && !pair.Value.IsDeletable())
                        return false;

                    if (!force)
                        return true;

                    return false;
                });

            // fast clear event list (in force case)
            if (force)
                lock (m_events)
                    m_events.Clear();
        }

        public void AddEvent(BasicEvent Event, TimeSpan e_time, bool set_addtime = true)
        {
            lock (m_events)
                InternalAddEvent(Event, e_time, set_addtime);
        }

        public EventSystem AddRepeatEvent(Func<TimeSpan> func, TimeSpan offset)
        {
            AddEvent(new RepeatEvent(this, func), offset);
            return this;
        }


        public EventSystem AddEvent(Action action, TimeSpan e_time, bool set_addtime = true) { AddEvent(new LambdaBasicEvent(action), e_time, set_addtime); return this; }
        
        public EventSystem AddEventAtOffset(BasicEvent Event, TimeSpan offset) { AddEvent(Event, CalculateTime(offset)); return this; }

        public EventSystem AddEventAtOffset(BasicEvent Event, TimeSpan offset, TimeSpan offset2) { AddEvent(Event, CalculateTime(RandomHelper.RandTime(offset, offset2))); return this; }

        public EventSystem AddEventAtOffset(Action action, TimeSpan offset) { AddEventAtOffset(new LambdaBasicEvent(action), offset); return this; }

        public EventSystem AddRepeatEventAtOffset(Func<TimeSpan> func, TimeSpan offset)
        {
            AddEventAtOffset(new RepeatEvent(this, func), offset); 
            return this;
        }

        public void ModifyEventTime(BasicEvent Event, TimeSpan newTime)
        {
            lock (m_events)
            if(m_events.RemoveFirstMatching((pair) =>
            {
                if (pair.Value != Event)
                    return false;

                Event.m_execTime = newTime.TotalMilliseconds;
                return true;
            }, out var foundVal))
            {
                m_events.Add(newTime.TotalMilliseconds, Event);
            }
        }

        public TimeSpan CalculateTime(TimeSpan t_offset)
        {
            return TimeSpan.FromMilliseconds(m_time) + t_offset;
        }

        public void ScheduleAbortOnAllMatchingEvents(Func<BasicEvent, bool> func)
        {
            lock (m_events)
                foreach (var l in m_events.Values)
                    foreach (var e in l)
                        if (func(e))
                            e.ScheduleAbort();
        }

        public void ScheduleAbortOnFirstMatchingEvent(Func<BasicEvent, bool> func)
        {
            lock (m_events)
                foreach (var l in m_events.Values)
                    foreach (var e in l)
                        if (func(e))
                        {
                            e.ScheduleAbort();
                            break;
                        }
        }

        private void InternalAddEvent(BasicEvent Event, TimeSpan e_time, bool set_addtime = true)
        {
            if (set_addtime)
                Event.m_addTime = m_time;

            Event.m_execTime = e_time.TotalMilliseconds;

            m_events.Add(e_time.TotalMilliseconds, Event);
        }

        ulong m_time;
        readonly SortedDictionary<double, List<BasicEvent>> m_events = new();
    }

    public class BasicEvent
    {
        public BasicEvent() { m_abortState = AbortState.Running; }

        public void ScheduleAbort()
        {
            Cypher.Assert(IsRunning(), "Tried to scheduled the abortion of an event twice!");
            m_abortState = AbortState.Scheduled;
        }

        public void SetAborted()
        {
            Cypher.Assert(!IsAborted(), "Tried to abort an already aborted event!");
            m_abortState = AbortState.Aborted;
        }

        // this method executes when the event is triggered
        // return false if event does not want to be deleted
        // e_time is execution time, p_time is update interval
        public virtual bool Execute(ulong etime, uint pTime) { return true; }

        public virtual bool IsDeletable() { return true; }   // this event can be safely deleted

        public virtual void Abort(ulong e_time) { } // this method executes when the event is aborted

        public bool IsRunning() { return m_abortState == AbortState.Running; }
        public bool IsAbortScheduled() { return m_abortState == AbortState.Scheduled; }
        public bool IsAborted() { return m_abortState == AbortState.Aborted; }

        AbortState m_abortState; // set by externals when the event is aborted, aborted events don't execute
        public ulong m_addTime; // time when the event was added to queue, filled by event handler
        public double m_execTime; // planned time of next execution, filled by event handler
    }

    public class LambdaBasicEvent : BasicEvent
    {
        readonly Action _callback;

        public LambdaBasicEvent(Action callback) : base()
        {
            _callback = callback;
        }

        public override bool Execute(ulong etime, uint pTime)
        {
            _callback();
            return true;
        }
    }
    
    enum AbortState
    {
        Running,
        Scheduled,
        Aborted
    }

    public class RepeatEvent : BasicEvent 
    {
        readonly Func<TimeSpan> _event;
        readonly EventSystem _eventSystem;

        public RepeatEvent(EventSystem eventSystem, Func<TimeSpan> func) : base()
        {
            _event = func;
            _eventSystem = eventSystem;
        }
        public override bool Execute(ulong etime, uint pTime)
        {
            var ts = _event.Invoke();
            if (ts != default)
                _eventSystem.AddEventAtOffset(this, ts);
            return true;
        }
    }
}
