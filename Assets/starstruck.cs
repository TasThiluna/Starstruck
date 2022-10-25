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
        var tries = 0;
        tryAgain:
        tries++;
        clusterUsed = rnd.Range(0, 3);

        var chars = clusters[clusterUsed].Where(ch => ch != '█').OrderBy(c => c).ToArray();
        var pieces = Enumerable.Range(0, 3).Select(i => rnd.Range(0, 10)).ToArray();
        int ofs1 = rnd.Range(0, chars.Length);
        int ofs2 = rnd.Range(0, chars.Length);
        int ofs3 = rnd.Range(0, chars.Length);

        char? answer = null;
        char[] piecePositions = null;

        for (var ir1 = 0; ir1 < chars.Length; ir1++)
        {
            var i1 = (ir1 + ofs1) % chars.Length;
            var cluedStars1 = chessMoves.move(pieces[0], clusters[clusterUsed], chars[i1]);
            for (var ir2 = 0; ir2 < chars.Length; ir2++)
            {
                var i2 = (ir2 + ofs2) % chars.Length;
                if (i2 == i1)
                    continue;
                var cluedStars2 = chessMoves.move(pieces[1], clusters[clusterUsed], chars[i2]);
                if (cluedStars1.Intersect(cluedStars2).Count() < 2)
                    continue;
                for (var ir3 = 0; ir3 < chars.Length; ir3++)
                {
                    var i3 = (ir3 + ofs3) % chars.Length;
                    if (i3 == i1 || i3 == i2)
                        continue;
                    var cluedStars3 = chessMoves.move(pieces[2], clusters[clusterUsed], chars[i3]);
                    if (cluedStars1.Intersect(cluedStars3).Count() < 2 || cluedStars2.Intersect(cluedStars3).Count() < 2)
                        continue;
                    var result = cluedStars1.Intersect(cluedStars2).Intersect(cluedStars3).ToArray();
                    if (result.Length != 1)
                        continue;
                    answer = result[0];
                    piecePositions = new char[] { chars[i1], chars[i2], chars[i3] };
                    goto found;
                }
            }
        }
        found:
        if (answer == null)
        {
            Debug.LogFormat("There seems to be no way to place pieces {0}, {1}, {2} on cluster {3}.", pieces[0], pieces[1], pieces[2], clusterUsed);
            goto tryAgain;
        }

        Debug.LogFormat("Cluster: {6}; pieces: {0} on {1}, {2} on {3}, {4} on {5}; answer: {7} ({8} tries).", pieceName(pieces[0]), piecePositions[0], pieceName(pieces[1]), piecePositions[1], pieceName(pieces[2]), piecePositions[2], clusterUsed, answer, tries);
        StartCoroutine(SpaceMovement());
    }

    private string pieceName(int pieceInfo)
    {
        return string.Format("{0} {1}", "rook,knight,king,bishop,queen".Split(',')[pieceInfo >> 1], (pieceInfo & 1) != 0 ? "inverted" : "normal");
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
