using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum WorkoutStatus
{
    Settings,
    GetReady,
    Workout,
    Rest,
    Done
}

public class WorkoutSettings
{
    public TimeSpan GetReadySpan { get; set; }
    public TimeSpan WorkSpan { get; set; }
    public TimeSpan RestSpan { get; set; }
    public int Sets { get; set; }
}

public class Timer : MonoBehaviour
{

    [Header("Settings text inputs")]
    public InputField SetsText;
    public InputField WorkMinutes;
    public InputField WorkSeconds;
    public InputField RestMinutes;
    public InputField RestSeconds;

    [Header("Audioclips")]
    public AudioClip[] ShortBeeps;
    public AudioClip[] LongBeeps;
    public Dropdown ShortBeepsDropdown;
    public Dropdown LongBeepsDropdown;
    private AudioClip _selectedShortBeep;
    private AudioClip _selectedLongBeep;

    [Header("Workout")]
    public Text TimerText;
    public Text StatusText;
    public Text SetsLeftText;
    public Image WorkoutBackground;
    [Range(1, 10)]
    public int GetReadySec = 5;
    public Color GetReadyBGColor;
    public Color WorkoutBGColor;
    public Color RestBGColor;
    public Color DoneBGColor;
    public GameObject PlayButton;
    public GameObject PauseButton;
    public GameObject BackButton;

    private AudioSource source;
    private int _sets;
    private int _workSeconds;
    private TimeSpan _workSpan;
    private int _restSeconds;
    private TimeSpan _restSpan;
    private WorkoutStatus _status;
    private bool _isPaused = false;
    private System.Timers.Timer _timer;
    private System.Diagnostics.Stopwatch _stopwatch;
    private TimeSpan _pausedTime;
    private WorkoutSettings _currentSettings;
    private string _statusText;
    private Color _nextBgColor;
    private bool _fireBgAnimation = false;
    private int _lastFullSecond = int.MaxValue;
    private WorkoutStatus _lastStatus;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // Use this for initialization
    void Start()
    {
        _sets = 1;
        SetsText.text = _sets.ToString();

        SetWork(TimeSpan.FromSeconds(20));
        SetRest(TimeSpan.FromSeconds(40));

        _status = WorkoutStatus.Settings;
        _statusText = "GET READY";
        WorkoutBackground.color = GetReadyBGColor;

        if (ShortBeeps == null || ShortBeeps.Length == 0)
            throw new Exception("No short beeps assigned!");

        _selectedShortBeep = ShortBeeps[0];
        ShortBeepsDropdown.AddOptions(ShortBeeps.Select(sb => sb.name).ToList());

        if (LongBeeps == null || LongBeeps.Length == 0)
            throw new Exception("No long beeps assigned!");

        _selectedLongBeep = LongBeeps[0];
        LongBeepsDropdown.AddOptions(LongBeeps.Select(lb => lb.name).ToList());
    }

    // Update is called once per frame
    void Update()
    {
        if (_currentSettings == null)
            return;

        StatusText.text = _statusText;
        SetsLeftText.text = _currentSettings.Sets.ToString();

        if (_isPaused || _status == WorkoutStatus.Settings)
            return;

        if (_fireBgAnimation)
        {
            StartCoroutine(BackgroundTransition(WorkoutBackground.color, _nextBgColor));
            _fireBgAnimation = false;
        }

        TimeSpan left = TimeSpan.FromSeconds(0);
        if (_status == WorkoutStatus.GetReady)
        {
            left = _currentSettings.GetReadySpan - _stopwatch.Elapsed;
        }
        else if (_status == WorkoutStatus.Workout)
        {
            left = _currentSettings.WorkSpan - _stopwatch.Elapsed;
        }
        else if (_status == WorkoutStatus.Rest)
        {
            left = _currentSettings.RestSpan - _stopwatch.Elapsed;
        }

        int sec = (int)left.TotalSeconds;
        if (sec == 0 && _status == WorkoutStatus.Done && _status != _lastStatus)
        {
            PauseButton.SetActive(false);
            PlayButton.SetActive(false);
            BackButton.SetActive(true);
        }
        if (sec != _lastFullSecond)
        {
            // short beep
            if (sec >= 1 && sec <= 3)
            {
                source.PlayOneShot(_selectedShortBeep);
            }
            // long beep
            else if (sec == 0)
            {
                source.PlayOneShot(_selectedLongBeep);
            }
        }
        _lastFullSecond = (int)left.TotalSeconds;

        if (left.TotalSeconds < 0 || _status == WorkoutStatus.Done)
            return;

        TimerText.text = string.Format("{0:D2}:{1:D2}", left.Minutes, left.Seconds);
    }

    public void AlterWork(int amount)
    {
        _workSeconds = int.Parse(WorkMinutes.text) * 60 + int.Parse(WorkSeconds.text);
        if (amount > 0 && _workSeconds == 1)
        {
            amount = 4; // changing 5 to 4 to avoid 1, 6, 11.. -> 1, 5, 10.. instead
        }

        _workSeconds = Mathf.Clamp(_workSeconds + amount, 1, 3599); // 1h = 3600 sec, max 59:59
        SetWork(TimeSpan.FromSeconds(_workSeconds));
    }

    public void SetWork(TimeSpan ts)
    {
        _workSpan = ts;
        WorkMinutes.text = _workSpan.Minutes.ToString();
        WorkSeconds.text = string.Format("{0:D2}", _workSpan.Seconds);
    }

    public void AlterRest(int amount)
    {
        _restSeconds = int.Parse(RestMinutes.text) * 60 + int.Parse(RestSeconds.text);
        _restSeconds = Mathf.Clamp(_restSeconds + amount, 0, 3599); // 1h = 3600 sec, max 59:59
        SetRest(TimeSpan.FromSeconds(_restSeconds));
    }

    public void SetRest(TimeSpan ts)
    {
        _restSpan = ts;
        RestMinutes.text = _restSpan.Minutes.ToString();
        RestSeconds.text = string.Format("{0:D2}", _restSpan.Seconds);
    }

    public void AlterSets(int amount)
    {
        _sets = int.Parse(SetsText.text);
        _sets = Mathf.Clamp(_sets + amount, 1, 99);
        SetsText.text = _sets.ToString();
    }

    public void ChangeShortBeep(int option)
    {
        _selectedShortBeep = ShortBeeps[option];
        source.PlayOneShot(_selectedShortBeep);
    }

    public void ChangeLongBeep(int option)
    {
        _selectedLongBeep = LongBeeps[option];
        source.PlayOneShot(_selectedLongBeep);
    }

    public void StartWorkout()
    {
        GetSettings(); // user might have changed it by on-screen keyboard

        if (_timer != null)
            _timer.Elapsed -= TimerFinished;

        _timer = new System.Timers.Timer();
        _timer.AutoReset = false;
        _timer.Elapsed += TimerFinished;

        if (_stopwatch != null)
            _stopwatch.Stop();

        _stopwatch = new System.Diagnostics.Stopwatch();
        _status = WorkoutStatus.GetReady;
        _isPaused = false;

        SetTimers();
        SetUIMode();
    }

    private void TimerFinished(object sender, System.Timers.ElapsedEventArgs e)
    {
        Debug.Log($"Timer finished, old status: {_status}");
        WorkoutStatus oldStatus = _status;

        // pick next status
        if (_status == WorkoutStatus.GetReady)
        {
            _status = WorkoutStatus.Workout;
        }
        else if (_status == WorkoutStatus.Workout)
        {
            if (_currentSettings.RestSpan.Seconds == 0) // no rest
            {
                _currentSettings.Sets--;
                if (_currentSettings.Sets == 0)
                {
                    _status = WorkoutStatus.Done;
                } // else no change, still workout mode
            }
            else if (_currentSettings.Sets <= 1) // in last set no need to rest
            {
                _status = WorkoutStatus.Done;
                _currentSettings.Sets = 0;
            }
            else
            {
                _status = WorkoutStatus.Rest;
            }
        }
        else if (_status == WorkoutStatus.Rest)
        {
            _currentSettings.Sets--;
            if (_currentSettings.Sets == 0)
            {
                _status = WorkoutStatus.Done;
            }
            else
            {
                _status = WorkoutStatus.Workout;
            }
        }
        Debug.Log($"New status: {_status}");

        SetTimers();
        SetUIMode();
    }

    private void SetUIMode()
    {
        if (_status == WorkoutStatus.GetReady)
        {
            _statusText = "GET READY";
            PauseButton.SetActive(true);
            PlayButton.SetActive(false);
            BackButton.SetActive(false);
        }
        else if (_status == WorkoutStatus.Workout)
        {
            _statusText = "WORK!";
        }
        else if (_status == WorkoutStatus.Rest)
        {
            _statusText = "REST...";
        }
        else if (_status == WorkoutStatus.Done)
        {
            _statusText = "DONE!";
        }

        _fireBgAnimation = true;
        _nextBgColor = PickColorForStatus(_status);
    }

    private Color PickColorForStatus(WorkoutStatus status)
    {
        switch (status)
        {
            case WorkoutStatus.Done:
                return DoneBGColor;

            case WorkoutStatus.GetReady:
                return GetReadyBGColor;

            case WorkoutStatus.Rest:
                return RestBGColor;

            case WorkoutStatus.Workout:
                return WorkoutBGColor;
        }
        return Color.white;
    }

    IEnumerator BackgroundTransition(Color startColor, Color changeTo)
    {
        float t = 0;
        while (t <= 1.0f)
        {
            t += Time.fixedDeltaTime; // Goes from 0 to 1, incrementing by step each time
            WorkoutBackground.color = Color.Lerp(startColor, changeTo, t);
            yield return new WaitForFixedUpdate();         // Leave the routine and return here in the next frame
        }
        WorkoutBackground.color = changeTo;
    }


    public void SetPause(bool pause)
    {
        _isPaused = pause;

        if (_isPaused)
        {
            _timer.Stop();
            _stopwatch.Stop();
            _pausedTime = _stopwatch.Elapsed;
        }
        else
        {
            TimeSpan left = TimeSpan.FromSeconds(0);

            switch (_status)
            {
                case WorkoutStatus.GetReady:
                    left = _currentSettings.GetReadySpan - _pausedTime;
                    break;

                case WorkoutStatus.Workout:
                    left = _currentSettings.WorkSpan - _pausedTime;
                    break;

                case WorkoutStatus.Rest:
                    left = _currentSettings.RestSpan - _pausedTime;
                    break;

                case WorkoutStatus.Settings:
                    return;
            }

            if (left.TotalMilliseconds <= 0)
                return;

            _stopwatch.Start();
            _timer.Interval = left.TotalMilliseconds;
            _timer.Start();
        }
    }

    private void SetTimers()
    {
        _timer.Stop();
        _stopwatch.Reset();
        switch (_status)
        {
            case WorkoutStatus.GetReady:
                _timer.Interval = (double)_currentSettings.GetReadySpan.TotalMilliseconds;
                break;

            case WorkoutStatus.Workout:
                _timer.Interval = (double)_currentSettings.WorkSpan.TotalMilliseconds;
                break;

            case WorkoutStatus.Rest:
                _timer.Interval = (double)_currentSettings.RestSpan.TotalMilliseconds;
                break;

            case WorkoutStatus.Settings:
                return;
        }
        Debug.Log("Timer interval: " + _timer.Interval);
        _timer.Start();
        _stopwatch.Start();
        _isPaused = false;
    }

    private void GetSettings()
    {
        _currentSettings = new WorkoutSettings()
        {
            GetReadySpan = TimeSpan.FromSeconds(GetReadySec),
            WorkSpan = TimeSpan.FromSeconds(int.Parse(WorkMinutes.text) * 60 + int.Parse(WorkSeconds.text)),
            RestSpan = TimeSpan.FromSeconds(int.Parse(RestMinutes.text) * 60 + int.Parse(RestSeconds.text)),
            Sets = int.Parse(SetsText.text)
        };
    }
}
