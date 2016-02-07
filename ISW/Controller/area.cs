﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ISaveWater
{
    class Area
    {
        public Area(string id, List<Zone> zones, Flow flow, OverCurrent over_current, Func<string, int> alert_callback)
        {
            _id = id;
            _zones = zones;
            _flow = flow;
            _over_current = over_current;
            _alert_callback = alert_callback;

            foreach (var valve in _zones)
            {
                valve.AddAlertCallback(AlertCallback);
            }
            _flow.AddAlertCallback(AlertCallback);
            _over_current.AddAlertCallback(AlertCallback);
            _state = INACTIVE_STATE;
        }

        public void Start()
        {
            // Start the flow object to calculate any water flow
            _flow.Start();

            // Start the sample task to detect abnormal flow rates and execute the schedule
            Task.Run(() => Sample());
        }

        private async Task Sample()
        {
            while (true)
            {
                if (_state == ACTIVE_STATE)
                {
                    var flow_rate = _flow.Rate();
                    if (flow_rate < ACTIVE_MIN_THRESHOLD)
                    {
                        _alert_callback("alert:" + _id + ":" + _flow.Id() + ":" + flow_rate.ToString("F1") + ":blocked");
                    }
                }

                if (_state == INACTIVE_STATE)
                {
                    var flow_rate = _flow.Rate();
                    if (flow_rate > INACTIVE_MAX_THRESHOLD)
                    {
                        _alert_callback("alert:" + _id + ":" + _flow.Id() + ":" + flow_rate.ToString("F1") + ":leak");
                    }
                }

                // execute watering schedule
                // How to find the next watering event?
                // What should the thread do in the mean time?
                //   - find the next watering event.  The event will contains the following information:
                //       1. the valve or valves to be enabled
                //       2. the duration of the watering
                //   - start a timer which when it expires will
                //       1. enable the relevant valves
                //       2. start a timer for the specified duration which upon expiry will turn off the valves

                await Task.Delay(1000);
            }
        }

        public string Id()
        {
            return _id;
        }

        public void Activate()
        {
            Debug.WriteLine("Activating Area " + _id);

            _state = ACTIVE_STATE;

            foreach (var zone in _zones)
            {
                zone.Enable();
            }
        }

        public void Deactivate()
        {
            Debug.WriteLine("Deactivating Area " + _id);

            _state = INACTIVE_STATE;

            foreach (var zone in _zones)
            {
                zone.Disable();
            }
        }

        public string Status()
        {
            /* I think a json string would be good here */
            /* <area id>/<zone 1 id>:<state>, <zone 2 id>:<state>/<flow id>:<flow>/<health id>:<state> */
            string status = "status:";

            foreach (var zone in _zones)
            {
                status += zone.Id() + ":" + zone.State() + ",";
            }

            status += _flow.Id() + ":" + _flow.Rate().ToString("F1") + ",";
            status += _over_current.Id() + ":" + _over_current.State();

            return status;
        }

        public void AddScheduleEvent(string sch_event)
        {
            // Insert the event into the schedule
        }

        private int AlertCallback(string value)
        {
            return _alert_callback("alert:" + _id + ":" + value);
        }

        private const string ACTIVE_STATE = "ACTIVE";
        private const string INACTIVE_STATE = "INACTIVE";

        private double INACTIVE_MAX_THRESHOLD = 2.0;
        private double ACTIVE_MIN_THRESHOLD = 3.0;

        private string _id;
        private List<Zone> _zones;
        private Flow _flow;
        private OverCurrent _over_current;
        private Func<string, int> _alert_callback;
        private string _state;

    }

}
