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
    public KMSelectable[] sliderButtons;
    public TextMesh[] bigStars;
    public TextMesh[] extraStars;
    public TextMesh colorblindText;
    public Color[] starColors;
    public GameObject buttonCap;
    public Transform[] hatches;
    public Transform[] sliders;
    public Renderer space;
    public Renderer led;
    public Material[] ledMats;
    public Font planetFont;
    public Material planetFontMat;

    private int timesOpened;
    private int clusterUsed;
    private int[] pieces = new int[3];
    private char[] piecePositions = null;
    private int[] solution = new int[3];
    private int[] sliderPositions = new[] { 2, 2, 2 };
    private int[][] viewingTimes = new int[3][];

    private bool hatchMoving;
    private bool hatchOpen;
    private bool submissionPhase;
    private Coroutine buttonCounter;
    private Coroutine submissionPhaseAnimation;
    private Coroutine[] starRotation = new Coroutine[3];
    private Coroutine[] extraStarRotation = new Coroutine[6];
    private Coroutine[] sliderMovements = new Coroutine[3];
    private List<int> activeRotations = new List<int>();
    private bool[] activeSliders = new bool[3];
    private float buttonCountElapsed;
    private bool canPlayResetSound = true;
    private bool starsFlying;

    private static readonly string[] clusters = new[] { "███q██████w███████ert█y█ui█opasdfghjklz██xcvbn███mQW█E████R█████", "██TYUI█O███P██AS████DFGH███JK██LZ██XC███VBNM████12██3███4█5678██", @"████9█████0█!@███#$█%^&██████*()█-=_█+,█./█;█:<█>?\|█[███]█{███}" };
    private static readonly string[] colorNames = new[] { "cyan", "red", "blue", "orange", "purple", "yellow", "pink", "green", "brown", "lime" };
    private string topRow = "0G9R8E7U6N";
    private string bottomRow = "5D4C3T2L1M";
    private string[] sliderNames = new[] { "top", "middle", "bottom" };
    private string[] positionNames = new[] { "outer left", "inner left", "middle", "inner right", "outer right" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { PressButton(); return false; };
        button.OnInteractEnded += delegate () { ReleaseButton(); };
        foreach (KMSelectable slider in sliderButtons)
            slider.OnInteract += delegate () { PressSlider(slider); return false; };
        module.OnActivate += delegate () { audio.PlaySoundAtTransform("start", transform); };
        colorblindText.gameObject.SetActive(GetComponent<KMColorblindMode>().ColorblindModeActive);
    }

    private void Start()
    {
        var tries = 0;
    tryAgain:
        tries++;
        clusterUsed = rnd.Range(0, 3);

        var chars = clusters[clusterUsed].Where(ch => ch != '█').OrderBy(c => c).ToArray();
        pieces = Enumerable.Range(0, 3).Select(i => rnd.Range(0, 10)).ToArray();
        int ofs1 = rnd.Range(0, chars.Length);
        int ofs2 = rnd.Range(0, chars.Length);
        int ofs3 = rnd.Range(0, chars.Length);

        char? answer = null;
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
            Debug.LogFormat("<Starstruck #{0}> There seems to be no way to place pieces {1}, {2}, {3} on cluster {4}.", moduleId, pieces[0], pieces[1], pieces[2], clusterUsed);
            goto tryAgain;
        }
        pieces = pieces.ToArray();
        var solutionPosition = clusters[clusterUsed].IndexOf(answer.Value);

        for (int i = 0; i < 3; i++)
            viewingTimes[i] = new[] { -1, -1, -1 };
        var arraysFilled = 0;
        var serialNumber = bomb.GetSerialNumber();
        var snIx = 0;
        var numLetters = serialNumber.Count(ch => !"0123456789".Contains(ch));
        var numNumbers = serialNumber.Count(ch => "0123456789".Contains(ch));
        var topRowTimes = new[] { 19, 18, 17, 16, 15, 14, 13, 12, 11, 10 };
        var bottomRowTimes = new[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        Debug.LogFormat("[Starstruck #{0}] There are {1} letters and {2} numbers in the serial number.", moduleId, numLetters, numNumbers);
        while (viewingTimes.Any(arr => arr.Contains(-1)))
        {
            var ch = serialNumber[snIx].ToString();
            if (topRow.Contains(ch))
            {
                if (!viewingTimes.Any(arr => arr[0] == topRowTimes[topRow.IndexOf(ch)]))
                {
                    for (int i = 0; i < 3; i++)
                        viewingTimes[arraysFilled][i] = (topRowTimes[topRow.IndexOf(ch)] - i + 20) % 20;
                    Debug.LogFormat("[Starstruck #{0}] An unused time can be found above {1}. A star can be viewed in the timeframes of {2}, {3}, and {4}.", moduleId, ch, viewingTimes[arraysFilled][0], viewingTimes[arraysFilled][1], viewingTimes[arraysFilled][2]);
                    arraysFilled++;
                }
                else
                    Debug.LogFormat("[Starstruck #{0}] No unused time found above {1}.", moduleId, ch);
            }
            if (bottomRow.Contains(ch))
            {
                if (!viewingTimes.Any(arr => arr[0] == bottomRowTimes[bottomRow.IndexOf(ch)]))
                {
                    for (int i = 0; i < 3; i++)
                        viewingTimes[arraysFilled][i] = (bottomRowTimes[bottomRow.IndexOf(ch)] - i + 20) % 20;
                    Debug.LogFormat("[Starstruck #{0}] An unused time can be found above {1}. A star can be viewed in the timeframes of {2}, {3}, and {4}.", moduleId, ch, viewingTimes[arraysFilled][0], viewingTimes[arraysFilled][1], viewingTimes[arraysFilled][2]);
                    arraysFilled++;
                }
                else
                    Debug.LogFormat("[Starstruck #{0}] No unused time found above {1}.", moduleId, ch);
            }
            else
                Debug.LogFormat("[Starstruck #{0}] {1} is not present in either of the rows.", moduleId, ch);
            if (!viewingTimes.Any(arr => arr.Contains(-1)))
                break;
            Debug.LogFormat("[Starstruck #{0}] Shifting rows...", moduleId);
            topRow = shift(topRow, numLetters);
            bottomRow = shift(bottomRow, 10 - numNumbers);
            snIx = (snIx + 1) % 6;
        }

        Debug.LogFormat("[Starstruck #{0}] We are in The {1}.", moduleId, new[] { "Faulty Butterfly Cluster", "Whirlboolean Galaxy", "Anametaxies" }[clusterUsed]);
        for (int i = 0; i < 3; i++)
            Debug.LogFormat("[Starstruck #{0}] The star at {1} is {2}, representing a{4} {3}.", moduleId, coordinate(clusters[clusterUsed].IndexOf(piecePositions[i])), colorNames[pieces[i]], pieceName(pieces[i]), pieceName(pieces[i]).Contains("inverted") ? "n" : "");
        Debug.LogFormat("[Starstruck #{0}] The goal star is at {1}.", moduleId, coordinate(solutionPosition));
        StartCoroutine(SpaceMovement());
        foreach (TextMesh bigStar in bigStars)
            bigStar.text = "";
        var topSliderStuff = "0,1,2,3,8,9,10,11,16,17,18,19,24,25,26,27;4,5,6,7,12,13,14,15,20,21,22,23,28,29,30,31;-1;32,33,34,35,40,41,42,43,48,49,50,51,56,57,58,59;36,37,38,39,44,45,46,47,52,53,54,55,60,61,62,63".Split(';').Select(str => str.Split(',').Select(x => int.Parse(x)).ToArray()).ToArray();
        var middleSliderStuff = "0,1,8,9,4,5,12,13,32,33,40,41,36,37,44,45;2,3,10,11,6,7,14,15,34,35,42,43,38,39,46,47;-1;16,17,24,25,20,21,28,29,48,49,56,57,52,53,60,61;18,19,26,27,22,23,30,31,50,51,58,59,54,55,62,63".Split(';').Select(str => str.Split(',').Select(x => int.Parse(x)).ToArray()).ToArray();
        var bottomSliderStuff = "0,2,4,6,16,18,20,22,32,34,36,38,48,50,52,54;1,3,5,7,17,19,21,23,33,35,37,39,49,51,53,55;-1;8,10,12,14,24,26,28,30,40,42,44,46,56,58,60,62;9,11,13,15,25,27,29,31,41,43,45,47,57,59,61,63".Split(';').Select(str => str.Split(',').Select(x => int.Parse(x)).ToArray()).ToArray();
        var allSliderStuffs = new[] { topSliderStuff, middleSliderStuff, bottomSliderStuff };
        for (int i = 0; i < 3; i++)
        {
            solution[i] = Array.IndexOf(allSliderStuffs[i], allSliderStuffs[i].First(arr => arr.Contains(solutionPosition)));
            Debug.LogFormat("[Starstruck #{0}] Set the {1} slider to the {2} position.", moduleId, sliderNames[i], positionNames[solution[i]]);
        }
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
            StartCoroutine(ResetModule());
        else
        {
            if (submissionPhase)
            {
                for (int i = 0; i < 3; i++)
                    Debug.LogFormat("[Starstruck #{0}] The {1} slider was submitted in the {2} position.", moduleId, sliderNames[i], positionNames[sliderPositions[i]]);
                if (solution.Where((x, ix) => sliderPositions[ix] == x).Count() == 3)
                {
                    Debug.LogFormat("[Starstruck #{0}] All slider positions were correct. Module solved!", moduleId);
                    module.HandlePass();
                    moduleSolved = true;
                    audio.PlaySoundAtTransform("solve", transform);
                    led.material = ledMats[2];
                    StartCoroutine(SolveAnimation());
                }
                else
                {
                    Debug.LogFormat("[Starstruck #{0}] A slider position was incorrect. Module solved!", moduleId);
                    module.HandleStrike();
                }
            }
            else if (!hatchOpen)
            {
                timesOpened++;
                var time = (int)bomb.GetTime() % 20;
                for (int i = 0; i < 3; i++)
                    if (viewingTimes[i].Contains(time))
                        activeRotations.Add(i);
                var usedStarTexts = new TextMesh[] { };
                if (activeRotations.Count == 1)
                    usedStarTexts = new[] { bigStars[1] };
                else if (activeRotations.Count == 2)
                    usedStarTexts = new[] { bigStars[0], bigStars[2] };
                else
                    usedStarTexts = new[] { bigStars[0], bigStars[1], bigStars[2] };
                for (int i = 0; i < activeRotations.Count; i++)
                {
                    starRotation[activeRotations[i]] = StartCoroutine(RotateStar(usedStarTexts[i].transform));
                    usedStarTexts[i].color = starColors[pieces[activeRotations[i]]];
                    usedStarTexts[i].text = piecePositions[activeRotations[i]].ToString();
                }
                colorblindText.text = activeRotations.Select(x => colorNames[pieces[x]]).Join(" ");
                StartCoroutine(OpenHatches(activeRotations.Count == 3));
            }
            else
            {
                if (timesOpened == 3)
                    submissionPhase = true;
                StartCoroutine(CloseHatches());
            }
        }
    }

    private void PressSlider(KMSelectable slider)
    {
        if (!submissionPhase)
            return;
        var ix = Array.IndexOf(sliderButtons, slider);
        var sliderUsed = ix / 2;
        if ((ix % 2 == 0 && sliderPositions[sliderUsed] == 0) || (ix % 2 == 1 && sliderPositions[sliderUsed] == 4))
            return;
        sliderPositions[sliderUsed] += ix % 2 == 0 ? -1 : 1;
        audio.PlaySoundAtTransform("click" + rnd.Range(1, 3), sliders[sliderUsed]);
        if (activeSliders[sliderUsed])
            StopCoroutine(sliderMovements[sliderUsed]);
        sliderMovements[sliderUsed] = StartCoroutine(AnimateSlider(sliders[sliderUsed], sliderUsed));
    }

    private IEnumerator AnimateSlider(Transform slider, int ix)
    {
        activeSliders[ix] = true;
        var elapsed = 0f;
        var duration = .25f;
        var start = slider.localPosition.x;
        var sliderHoriPositions = new[] { -.32f, -.16f, 0f, .17f, .32f };
        var sliderZs = new[] { .357f, -.033f, -.422f };
        while (elapsed < duration)
        {
            slider.localPosition = new Vector3(Easing.OutQuint(elapsed, start, sliderHoriPositions[sliderPositions[ix]], duration), .537f, sliderZs[ix]);
            yield return null;
            elapsed += Time.deltaTime;
        }
        slider.localPosition = new Vector3(sliderHoriPositions[sliderPositions[ix]], .537f, sliderZs[ix]);
        activeSliders[ix] = false;
    }

    private IEnumerator CountUp()
    {
        buttonCountElapsed = 0f;
        while (true)
        {
            yield return null;
            buttonCountElapsed += Time.deltaTime;
            if (buttonCountElapsed >= 2f && canPlayResetSound)
            {
                audio.PlaySoundAtTransform("bloop", transform);
                canPlayResetSound = false;
            }
        }
    }

    private IEnumerator OpenHatches(bool easterEgg = false, bool playDoorSound = true)
    {
        var elapsed = 0f;
        var duration = 1f;
        if (playDoorSound)
            audio.PlaySoundAtTransform("open", space.transform);
        if (!moduleSolved)
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
        if (!moduleSolved)
            led.material = ledMats[0];
        hatchMoving = false;
        hatchOpen = true;
        if (easterEgg)
            audio.PlaySoundAtTransform("easter egg", space.transform);
    }

    private IEnumerator CloseHatches(bool playDoorSound = true)
    {
        colorblindText.text = "";
        var elapsed = 0f;
        var duration = .5f;
        if (playDoorSound)
            audio.PlaySoundAtTransform("close", space.transform);
        if (!moduleSolved)
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
        for (int i = 0; i < 3; i++)
            if (activeRotations.Contains(i))
                StopCoroutine(starRotation[i]);
        activeRotations.Clear();
        foreach (TextMesh bigStar in bigStars)
        {
            bigStar.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            bigStar.text = "";
        }
        if (!moduleSolved)
            led.material = ledMats[0];
        hatchMoving = false;
        hatchOpen = false;
        if (submissionPhase && !moduleSolved)
            submissionPhaseAnimation = StartCoroutine(SubmissionPhaseAnimation());
    }

    private IEnumerator SubmissionPhaseAnimation()
    {
        foreach (TextMesh bigStar in bigStars)
            bigStar.text = "";
        starsFlying = true;
        StartCoroutine(FlyingStars());
        yield return new WaitForSeconds(.25f);
        StartCoroutine(OpenHatches());
    }

    private IEnumerator FlyingStars()
    {
        var firstTime = true;
        while (starsFlying)
        {
            for (int i = 0; i < 6; i++)
            {
                extraStars[i].transform.localPosition = new Vector3(.0671f, .0086f, i % 2 == 0 ? .0134f : -.0134f);
                extraStars[i].text = rnd.Range(0, 500) == 0 ? "ඞ" : "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm12345678@\"90!@#$%^&*()-=_+,./;:<>?\\|[]{}".PickRandom().ToString();
                extraStars[i].color = starColors.PickRandom();
                if (!firstTime)
                    StopCoroutine(extraStarRotation[i]);
                extraStarRotation[i] = StartCoroutine(RotateStar(extraStars[i].transform, rnd.Range(5f, 15f)));
                StartCoroutine(MoveStar(extraStars[i].transform, .0671f, -.0671f, extraStars[i].transform.localPosition.z));
                yield return new WaitForSeconds(rnd.Range(.5f, 1f));
            }
            firstTime = false;
        }
    }

    private IEnumerator MoveStar(Transform star, float start, float end, float z)
    {
        var elapsed = 0f;
        var duration = 3f;
        while (elapsed < duration)
        {
            star.localPosition = new Vector3(Mathf.Lerp(start, end, elapsed / duration), .0086f, z);
            yield return null;
            elapsed += Time.deltaTime;
        }
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

    private IEnumerator RotateStar(Transform bigStar, float rpm = 5f)
    {
        var direction = rnd.Range(0, 2) == 0;
        while (true)
        {
            var framerate = 1f / Time.deltaTime;
            var rotation = rpm / framerate;
            if (direction)
                rotation *= -1;
            var y = bigStar.localEulerAngles.y;
            y += rotation;
            bigStar.localEulerAngles = new Vector3(90f, y, 0f);
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

    private IEnumerator ResetModule()
    {
        if (hatchOpen)
            StartCoroutine(CloseHatches());
        while (hatchMoving)
            yield return null;
        Debug.LogFormat("[Starstruck #{0}] Button was held. Resetting...", moduleId);
        timesOpened = 0;
        activeRotations.Clear();
        if (submissionPhase)
        {
            StopCoroutine(submissionPhaseAnimation);
            foreach (TextMesh star in extraStars)
                star.text = "";
        }
        submissionPhase = false;
        canPlayResetSound = true;
        buttonCountElapsed = 0f;
        Start();
    }

    private IEnumerator SolveAnimation()
    {
        StartCoroutine(CloseHatches(false));
        while (hatchMoving)
            yield return null;
        foreach (TextMesh star in extraStars)
            star.gameObject.SetActive(false);
        StopCoroutine(submissionPhaseAnimation);
        starsFlying = false;
        yield return new WaitForSeconds(.25f);
        bigStars[1].font = planetFont;
        bigStars[1].GetComponent<Renderer>().material = planetFontMat;
        bigStars[1].transform.localPosition = new Vector3(bigStars[1].transform.localPosition.x, bigStars[1].transform.localPosition.y, -.0034f);
        bigStars[1].transform.localScale = new Vector3(.0023f, .0023f, .0023f);
        bigStars[1].text = "BGHIOPRSUVYZbeghivxy0235%&,~".PickRandom().ToString();
        var startColor = 0f;
        float X, Y; // These are never actually used but need to be here because RGBToHSV is incredibly stupid
        Color.RGBToHSV(bigStars[1].color, out startColor, out X, out Y);
        var stopPoint = rnd.Range(0, 100);
        for (int i = 0; i < 100; ++i)
        {
            bigStars[1].color = Color.HSVToRGB((startColor + (i * 0.01f)) % 1.0f, 1.0f, 1.0f);
            if (i == stopPoint)
                break;
        }
        StartCoroutine(OpenHatches(false, false));
    }

    private string pieceName(int pieceInfo)
    {
        return string.Format("{0} {1}", (pieceInfo & 1) != 0 ? "inverted" : "normal", "rook,knight,king,bishop,queen".Split(',')[pieceInfo >> 1]);
    }

    private string coordinate(int x)
    {
        var s1 = "ABCDEFGH"[x % 8].ToString();
        var s2 = (x / 8) + 1;
        return s1 + s2;
    }

    private static string shift(string str, int i)
    {
        return str.Substring(str.Length - i) + str.Substring(0, str.Length - i);
    }


    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press [Presses the button] !{0} press xx [Presses the button when the seconds digits of the timer modulo 20 are xx] !{0} reset [Resets the module] !{0} <top/middle/bottom> <OL/IL/M/IR/OR> [Sets the top, middle, or bottom slider to the outer left, inner left, middle, inner right, or outer right positon] !{0} colorblind [Enables colorblind mode]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Trim().ToLowerInvariant();
        var inputArray = input.Split(' ');
        var tpPosNames = new[] { "ol", "il", "m", "ir", "or" };
        var time = 0;
        if (input == "press")
        {
            yield return null;
            button.OnInteract();
            yield return new WaitForSeconds(.2f);
            button.OnInteractEnded();
        }
        else if (inputArray.Length == 2 && inputArray[0] == "press" && int.TryParse(inputArray[1], out time))
        {
            if (time < 0 || time >= 20)
                yield return "sendtochaterror Invalid time.";
            yield return null;
            while ((int)bomb.GetTime() % 20 != time)
                yield return "trycancel";
            button.OnInteract();
            yield return new WaitForSeconds(.2f);
            button.OnInteractEnded();
        }
        else if (input == "reset")
        {
            yield return null;
            button.OnInteract();
            yield return new WaitForSeconds(2.5f);
            button.OnInteractEnded();
        }
        else if (inputArray.Length == 2 && sliderNames.Contains(inputArray[0]) && tpPosNames.Contains(inputArray[1]))
        {
            if (!submissionPhase)
                yield return "sendtochaterror The sliders are not usable yet.";
            var sliderUsed = Array.IndexOf(sliderNames, inputArray[0]);
            var desiredPosition = Array.IndexOf(tpPosNames, inputArray[1]);
            while (sliderPositions[sliderUsed] != desiredPosition)
            {
                sliderButtons[(2 * sliderUsed) + (desiredPosition < sliderPositions[sliderUsed] ? 0 : 1)].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        else if (input == "colorblind" || input == "cb" || input == "colourblind")
            colorblindText.gameObject.SetActive(true);
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        var tpPosNames = new[] { "ol", "il", "m", "ir", "or" };
        while (!submissionPhase)
        {
            yield return new WaitUntil(() => !hatchMoving);
            button.OnInteract();
            yield return new WaitForSeconds(.2f);
            button.OnInteractEnded();
        }
        for (int i = 0; i < 3; i++)
        {
            yield return ProcessTwitchCommand(sliderNames[i] + " " + tpPosNames[solution[i]]);
            yield return new WaitForSeconds(.2f);
        }
        yield return new WaitForSeconds(2f);
        button.OnInteract();
        yield return new WaitForSeconds(.2f);
        button.OnInteractEnded();
    }
}
