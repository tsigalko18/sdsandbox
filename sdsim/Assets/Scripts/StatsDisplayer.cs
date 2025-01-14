﻿using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class StatsDisplayer : MonoBehaviour
{
    // Flag to see if car started
    public static bool carStarted;

    // Script start time
    public static DateTime scriptStartTime;

    // Log informations / histories per Lap
    public static bool writeLog = true;
    private string frameLogPath;
    public static List<float> timesHistory;
    public static List<float> offTrackHistory;
    public static List<float> xteAvgHistory;
    public static List<float> xteVarHistory;
    public static List<float> maxXteHistory;
    public static List<float> steersAvgHistory;
    public static List<float> steersVarsHistory;
    public static List<float> speedAvgHistory;
    public static List<float> speedVarHistory;
    public static List<int> crashesHistory;

    // Displayable Stats
    public static int lap = 1;
    public static int lapCrashes = 0;
    public static float lapTime;
    public static float prevLapTime;
    public static int offTrackCounter;
    public static float xte;
    public static float maxXte;
    public static float lastLapSteerVar;

    // Lap frames
    public static List<float> lapXtes;
    public static List<float> lapSteers;
    public static List<float> lapSpeeds;

    // Parameters
    public float offTrackXTE; // 1.4
    private bool isOffTrack = false;
    public static float startTime;

    // Labels to update
    private Text lapLabel;
    private Text timeLabel;
    private Text prevTimeLabel;
    private Text outOfTrackLabel;
    private Text xteLabel;
    private Text xteMaxLabel;
    private Text steerVarLabel;
    private Text currWaypointLabel;

    // Path manager for XTE
    private static PathManager pm;
    private static int currentWaypoint = 0;

    // Car
    private static double epsilon = 0.01;
    private Vector3 startingCarPosition;
    public GameObject carObj;
    private Car car;

    // Start is called before the first frame update
    void Start()
    {
        // Updating script start time
        scriptStartTime = DateTime.Now;

        // Initializing stats
        lapTime = 0;
        prevLapTime = 0;
        offTrackCounter = 0;
        xte = 0;
        maxXte = 0;
        lastLapSteerVar = 0;

        lapXtes = new List<float>();
        lapSteers = new List<float>();
        lapSpeeds = new List<float>();

        // Initializing histories
        if (writeLog)
        {
            timesHistory = new List<float>();
            offTrackHistory = new List<float>();
            xteAvgHistory = new List<float>();
            xteVarHistory = new List<float>();
            maxXteHistory = new List<float>();
            steersAvgHistory = new List<float>();
            steersVarsHistory = new List<float>();
            speedAvgHistory = new List<float>();
            speedVarHistory = new List<float>();
            crashesHistory = new List<int>();

            string filename = "Frames - " + getFileName();
            frameLogPath = Application.dataPath + "/Testing/" + filename + ".csv";
            System.IO.File.AppendAllLines(frameLogPath, new string[] { "Lap;XTE;Steering;Throttle;Velocity;Acceleration"});
        }

        // Initializing PM
        pm = FindObjectOfType<PathManager>();

        // Initializing Car
        car = (Car)Utilities.tryGetCar("DonkeyCar");
        if (car != null)
            startingCarPosition = car.transform.position;

        // Getting labels to update
        GameObject statsPanel = GameObject.Find("StatsPanel");
        if (statsPanel != null)
        {
            Text[] labels = statsPanel.GetComponentsInChildren<Text>();

            if (labels.Length >= 7)
            {
                lapLabel = labels[0];
                timeLabel = labels[1];
                outOfTrackLabel = labels[2];
                xteLabel = labels[3];
                prevTimeLabel = labels[4];
                xteMaxLabel = labels[5];
                steerVarLabel = labels[6];
                currWaypointLabel = labels[7];
            }
        }

        carStarted = checkCarStarted();
    }

    // Update is called once per frame
    void Update()
    {
        if (car == null)
        {
            car = (Car)Utilities.tryGetCar("DonkeyCar");
            if (car != null)
                startingCarPosition = car.transform.position;
        }

        if (!carStarted)
        {
            carStarted = checkCarStarted();
        }

        if (carStarted)
        {
            getUpdatedStats();
            displayStats();
        }

        if (writeLog && carStarted)
        {
            writeFrameStats();
        }
    }

    private void OnDestroy()
    {
        if (writeLog)
        {
            string hour = "" + scriptStartTime.Hour;
            string minute = "" + scriptStartTime.Minute;

            if(scriptStartTime.Hour < 10)
            {
                hour = "0" + hour;
            }

            if(scriptStartTime.Minute < 10)
            {
                minute = "0" + minute;
            }

            // Writing laps (MacOS)
            string filename = "Laps - " + getFileName();
            string filepath = Application.dataPath + "/Testing/" + filename + ".csv";

            string text = "Lap time;Off-track;Max XTE;XTE avg;XTE var;Steer avg;Steer var;Speed avg;Speed var;Crashes;\n";
            for(int i = 0; i<timesHistory.Count; i++)
            {
                float t = timesHistory[i];
                float o = offTrackHistory[i];
                float m = maxXteHistory[i];
                float xa = xteAvgHistory[i];
                float xv = xteVarHistory[i];
                float sta = steersAvgHistory[i];
                float stv = steersVarsHistory[i];
                float spa = speedAvgHistory[i];
                float spv = speedVarHistory[i];
                float c = crashesHistory[i];

                text += t + ";" + o + ";" + m + ";" + xa + ";" + xv + ";" + sta + ";" + stv + ";" + spa + ";" + spv + ";" + c +";\n";
            }

            System.IO.File.WriteAllText(filepath, text);
            Debug.Log("Log file written to " + filepath);
        }
    }

    private bool checkCarStarted()
    {
        if (carStarted)
        {
            return true;
        }

        bool areClose(double a, double b)
        {
            if (Math.Abs(a - b) < epsilon) {
                return true;
            };

            return false;
        }

        if(car == null)
        {
            return false;
        }

        if(
            areClose(car.transform.position.x, startingCarPosition.x) &&
            areClose(car.transform.position.z, startingCarPosition.z)
            )
        {
            return false;
        }

        startTime = Time.time;
        return true;
    }


    private void getUpdatedStats()
    {
        if(car == null)
        {
            Utilities.tryGetCar("DonkeyCar");

            if(car == null) { return; }
        }

        // Updating time
        lapTime = Time.time - startTime;

        // Updating frame infos
        lapSteers.Add(Math.Abs(car.GetSteering()));
        lapSpeeds.Add(Math.Abs(car.GetVelocity().magnitude));


        if (pm != null)
        {
            // Updating current way point
            if (pm.path.iActiveSpan + 1 > currentWaypoint)
                currentWaypoint = pm.path.iActiveSpan + 1;

            // Checking if lap finished
            if (pm.path.iActiveSpan == 1 && currentWaypoint >= pm.path.nodes.Count - 2)
            {
                endOfLapUpdates();
            }

            // Updating XTE
            if (!pm.path.GetCrossTrackErr(car.GetTransform(), ref xte))
            {
                
                if (car.GetLastCollision() != null)
                {
                    // Car crashed
                    lapCrashes += 1;
                    car.ClearLastCollision();
                } else {
                    if (Utilities.carIsGoingForward(car))
                    {
                        // Lap finished, looping
                        pm.path.ResetActiveSpan();
                        endOfLapUpdates();
                    }
                }
            };

            // Updating xte infos
            lapXtes.Add(Math.Abs(xte));

            // Updating Out-of-track counter
            if (Math.Abs(xte) > Math.Abs(offTrackXTE))
            {
                if (!isOffTrack)
                {
                    offTrackCounter += 1;
                    isOffTrack = true;
                }
            }
            else
            {
                isOffTrack = false;
            }
        }

        // Updating max XTE
        if(Math.Abs(xte) > maxXte)
        {
            maxXte = Math.Abs(xte);
        }
    }

    public static void endOfLapUpdates()
    {
        // Updating current waypoint
        currentWaypoint = pm.path.iActiveSpan + 1;

        // Counting lap
        lap += 1;

        // Updating prev. lap time and current starting time
        float finishTime = Time.time;
        prevLapTime = finishTime - startTime;
        startTime = finishTime;


        // Updating log infos
        if (writeLog)
        {
            timesHistory.Add(prevLapTime);
            offTrackHistory.Add(offTrackCounter);

            // XTE
            float lapxteavg = Utilities.getMean(lapXtes);
            xteAvgHistory.Add(lapxteavg);
            xteVarHistory.Add(Utilities.getVar(lapXtes, lapxteavg));
            maxXteHistory.Add(maxXte);

            // Steer
            float lapsteeravg = Utilities.getMean(lapSteers);
            steersAvgHistory.Add(lapsteeravg);
            lastLapSteerVar = Utilities.getVar(lapSteers, lapsteeravg);
            steersVarsHistory.Add(lastLapSteerVar);

            // Speed
            float lapspeedavg = Utilities.getMean(lapSpeeds);
            speedAvgHistory.Add(lapspeedavg);
            speedVarHistory.Add(Utilities.getVar(lapSpeeds, lapspeedavg));

            // Crashes
            crashesHistory.Add(lapCrashes);
        }

        // Clear lap frames
        lapXtes.Clear();
        lapSteers.Clear();
        lapSpeeds.Clear();

        // Resetting max XTE and off-track counter for this lap
        maxXte = 0;
        offTrackCounter = 0;
        lapCrashes = 0;
    }

    private void displayStats()
    {
        lapLabel.text = lapLabel.text.Split(new string[] { ": " }, StringSplitOptions.None)[0] + ": " + lap;
        timeLabel.text = timeLabel.text.Split(new string[] { ": " }, StringSplitOptions.None)[0] + ": " + lapTime;
        prevTimeLabel.text = prevTimeLabel.text.Split(new string[] { ": " }, StringSplitOptions.None)[0] + ": " + prevLapTime;
        outOfTrackLabel.text = outOfTrackLabel.text.Split(new string[] { ": " }, StringSplitOptions.None)[0] + ": " + offTrackCounter;
        xteLabel.text = xteLabel.text.Split(new string[] { ": " }, StringSplitOptions.None)[0] + ": " + xte;
        xteMaxLabel.text = xteMaxLabel.text.Split(new string[] { ": " }, StringSplitOptions.None)[0] + ": " + maxXte;
        steerVarLabel.text = steerVarLabel.text.Split(new string[] { ": "}, StringSplitOptions.None)[0] + ": " + lastLapSteerVar;
        currWaypointLabel.text = currWaypointLabel.text.Split(new string[] { ": " }, StringSplitOptions.None)[0] + ": " + (currentWaypoint) + "/" + (pm.path.nodes.Count - 1);
    }

    private void writeFrameStats()
    {
        string frameStats = lap + ";" + xte + ";" + car.GetSteering() + ";" + car.GetThrottle() + ";" + car.GetVelocity().magnitude + ";" + car.GetAccel().magnitude;
        System.IO.File.AppendAllLines(frameLogPath, new string[] { frameStats });
    }

    private string getFileName()
    {
        string hour = "" + scriptStartTime.Hour;
        string minute = "" + scriptStartTime.Minute;

        if (scriptStartTime.Hour < 10)
        {
            hour = "0" + hour;
        }

        if (scriptStartTime.Minute < 10)
        {
            minute = "0" + minute;
        }

        return scriptStartTime.Year + "-" + scriptStartTime.Month + "-" + scriptStartTime.Day + "-" + hour + "h_" + minute + "m";
    }
}
