using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class starstruck : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable button;
    public TextMesh mainStar;
    public GameObject buttonCap;
    public Transform[] hatches;
    public Renderer space;
    public Renderer led;
    public Material[] ledMats;

    private int timesOpened;
    private int clusterUsed;
    private string[] clueStars = new string[3];
    private int[] cluePieces = new int[3];
    private bool[] clueNegations = new bool[3];

    private bool hatchMoving;
    private bool hatchOpen;
    private bool submissionPhase;
    private Coroutine buttonCounter;
    private Coroutine starRotation;
    private float buttonCountElapsed;

    private static readonly string[] clusters = new[] { "███q██████w███████ert█y█ui█opasdfghjklz██xcvbn███mQW█E████R█████", "██TYUI█O███P██AS████DFGH███JK██LZ██XC███VBNM████12██3███4█5678██", @"████9█████0█!@███#$█%^&██████*()█-=_█+,█./█;█:<█>?\|█[███]█{███}" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { PressButton(); return false; };
        button.OnInteractEnded += delegate () { ReleaseButton(); };
    }

    private void Start()
    {
        StartCoroutine(SpaceMovement());
        clusterUsed = rnd.Range(0, 3);
        clusterUsed = 0;
        var clusterArray = clusters[clusterUsed].ToCharArray().Select(ch => ch.ToString()).ToArray();

        var tries = 0;
    tryAgain:
        tries++;
        var cluedStars = new string[3] { "", "", "" };
        var deductions = new string[3] { "", "", "" };
        for (int i = 0; i < 3; i++)
        {
            clueStars[i] = clusters[clusterUsed].Where(ch => ch != '█').PickRandom().ToString();
            cluePieces[i] = rnd.Range(0, 5);
            clueNegations[i] = rnd.Range(0, 2) == 0;
            switch (cluePieces[i])
            {
                case 0:
                    cluedStars[i] = chessMoves.rookMove(clusterArray, clueStars[i], clueNegations[i]);
                    break;
                case 1:
                    cluedStars[i] = chessMoves.knightMove(clusterArray, clueStars[i], clueNegations[i]);
                    break;
                case 2:
                    cluedStars[i] = chessMoves.kingMove(clusterArray, clueStars[i], clueNegations[i]);
                    break;
                case 3:
                    cluedStars[i] = chessMoves.bishopMove(clusterArray, clueStars[i], clueNegations[i]);
                    break;
                case 4:
                    cluedStars[i] = chessMoves.queenMove(clusterArray, clueStars[i], clueNegations[i]);
                    break;
            }

            if (i == 0)
                deductions[0] = new string(cluedStars[0].OrderBy(ch => ch).ToArray());
            if (i == 1)
            {
                deductions[1] = clusterArray.Where(s => deductions[0].Contains(s) && cluedStars[1].Contains(s)).OrderBy(s => s).Join("");
                if (deductions[0] == deductions[1])
                    goto tryAgain;
            }
            // Uncommenting this hangs the game, which means you don't always need 3 stars. I'm leaving it here incase it's salvagable.
            /*else
            {
                deductions[2] = clusterArray.Where(s => deductions[1].Contains(s) && cluedStars[2].Contains(s)).OrderBy(s => s).Join("");
               if (deductions[2] == deductions[1])
                   goto tryAgain;
            }*/
        }
        if (deductions[2].Length != 1)
            goto tryAgain;
        Debug.Log(deductions[2][0]);

        Debug.Log("Tries: " + tries);
        var pieceNames = new[] { "rook", "knight", "king", "bishop", "queen" };
        Debug.Log(cluedStars.Join(", "));
        Debug.Log(deductions.Join(", "));
        for (int i = 0; i < 3; i++)
        {
            Debug.Log(clueStars[i] + ", " + pieceNames[cluePieces[i]] + ", " + (clueNegations[i] ? "INVERT" : "NORMAL"));
        }
        Debug.LogFormat("[Starstruck #{0}] We are in the {1}.", moduleId, new[] { "Faulty Butterfly Cluster", "Whirlboolean Galaxy", "The Anametaxies" }[clusterUsed]);
    }

    private void PressButton()
    {
        StartCoroutine(AnimateButton(0f, -.0076f));
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (moduleSolved || hatchMoving)
            return;
        buttonCounter = StartCoroutine(CountUp());
    }

    private void ReleaseButton()
    {
        StartCoroutine(AnimateButton(-.0076f, 0f));
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (moduleSolved || hatchMoving)
            return;
        StopCoroutine(buttonCounter);
        if (buttonCountElapsed >= 2f)
        {
            //> reset module
        }
        else
        {
            if (submissionPhase)
                return;
            if (!hatchOpen)
            {
                timesOpened++;
                starRotation = StartCoroutine(RotateStar());
                StartCoroutine(OpenHatches());
            }
            else
            {
                if (timesOpened == 3)
                    submissionPhase = true;
                StartCoroutine(CloseHatches());
            }
        }
    }

    private IEnumerator CountUp()
    {
        buttonCountElapsed = 0f;
        while (true)
        {
            yield return null;
            buttonCountElapsed += Time.deltaTime;
        }
    }

    private IEnumerator OpenHatches()
    {
        var elapsed = 0f;
        var duration = 1f;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, space.transform);
        led.material = ledMats[1];
        hatchMoving = true;
        while (elapsed < duration)
        {
            hatches[0].localPosition = new Vector3(0f, .0109f, Easing.OutQuad(elapsed, .0173f, .0442f, duration));
            hatches[1].localPosition = new Vector3(0f, .0109f, Easing.OutQuad(elapsed, -.0173f, -.0442f, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        hatches[0].localPosition = new Vector3(0f, .0109f, .0442f);
        hatches[1].localPosition = new Vector3(0f, .0109f, -.0442f);
        led.material = ledMats[0];
        hatchMoving = false;
        hatchOpen = true;
    }

    private IEnumerator CloseHatches()
    {
        var elapsed = 0f;
        var duration = 1f;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, space.transform);
        led.material = ledMats[1];
        hatchMoving = true;
        while (elapsed < duration)
        {
            hatches[0].localPosition = new Vector3(0f, .0109f, Easing.OutQuad(elapsed, .0442f, .0173f, duration));
            hatches[1].localPosition = new Vector3(0f, .0109f, Easing.OutQuad(elapsed, -.0442f, -.0173f, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        hatches[0].localPosition = new Vector3(0f, .0109f, .0173f);
        hatches[1].localPosition = new Vector3(0f, .0109f, -.0173f);
        StopCoroutine(starRotation);
        mainStar.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        led.material = ledMats[0];
        hatchMoving = false;
        hatchOpen = false;
        if (submissionPhase)
            StartCoroutine(SubmissionPhaseAnimation());
    }

    private IEnumerator SubmissionPhaseAnimation()
    {
        mainStar.text = "";
        //> cycling gray stars
        yield return new WaitForSeconds(1.25f);
        StartCoroutine(OpenHatches());
    }

    private IEnumerator SpaceMovement()
    {
        var starScrollSpeed = .02f;
        while (true)
        {
            var offset = Time.time * starScrollSpeed;
            space.material.mainTextureOffset = new Vector2(-offset, 0f);
            yield return null;
        }
    }

    private IEnumerator RotateStar()
    {
        var direction = rnd.Range(0, 2) == 0;
        while (true)
        {
            var framerate = 1f / Time.deltaTime;
            var rotation = 5f / framerate;
            if (direction)
                rotation *= -1;
            var y = mainStar.transform.localEulerAngles.y;
            y += rotation;
            mainStar.transform.localEulerAngles = new Vector3(90f, y, 0f);
            yield return null;
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            buttonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        buttonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} ";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        yield return null;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
