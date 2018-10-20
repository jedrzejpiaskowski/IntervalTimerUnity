using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour {

    [Header("Text inputs")]
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


    private AudioSource source;
    private int _sets;
    private int _workSeconds;
    private TimeSpan _workSpan;
    private int _restSeconds;
    private TimeSpan _restSpan;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }


    // Use this for initialization
    void Start ()
    {
        _sets = 1;
        SetsText.text = _sets.ToString();

        SetWork(TimeSpan.FromSeconds(20));
        SetRest(TimeSpan.FromSeconds(40));

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
	void Update () {
		
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
        _restSeconds = Mathf.Clamp(_restSeconds + amount, 1, 3599); // 1h = 3600 sec, max 59:59
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

    }
}
