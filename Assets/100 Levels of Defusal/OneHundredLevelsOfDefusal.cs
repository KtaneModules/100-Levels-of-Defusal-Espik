using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class OneHundredLevelsOfDefusal : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] Letters;
    public TextMesh[] LetterTexts;
    public Renderer[] LetterButtons;

    public TextMesh LevelText;
    public Renderer[] ProgressBar;
    public Material[] ProgressBarColors;

    public Color[] TextColors;

    public KMSelectable SubmitBtn;
    public TextMesh SubmitText;
    public Renderer SubmitBtnModel;

    public KMSelectable ToggleBtn;
    public TextMesh ToggleText;
    public Renderer ToggleBtnModel;

    // Solving info
    private int level = 0;
    private double progress = 0;
    private int progressBars = 0;
    private bool canFlashNext = true;

    private double solves = 0;
    private double solvesNeeded = 0;
    private int moduleCount = 0;

    private int letters = 0;
    private bool levelFound = true;
    private bool solvesReached = false;

    private readonly string[] LETTERS = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    private readonly int[] FIRSTVALUE = { 0, 1, 1, 1, 0, 1, 1, 1, 0, 2, 2, 2, 2, 2, 0, 3, 3, 3, 3, 4, 0, 4, 4, 5, 5, 6 };
    private readonly int[] SECNDVALUE = { 1, 1, 2, 3, 3, 4, 5, 6, 4, 2, 3, 4, 5, 6, 5, 3, 4, 5, 6, 4, 6, 5, 6, 5, 6, 6 };

    private int[] letterIndexes = new int[33];
    private string[] letterDisplays = new string[33];

    private int[] lettersUsed = new int[12];
    private int letterSlotsUsed = 6;

    private string screenDisplay;
    private string correctMessage;
    private bool lockButtons = true;

    private char[] displayedLetters = new char[12]; // Used for Souvenir

    private bool direction = true;

    private int moduleStrikes = 0;

    // Testing variables
    private readonly int FIXLETTERS = 6; // 6
    private readonly int FIXLEVEL = 15; // 15

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;

    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        for (int i = 0; i < Letters.Length; i++) {
            int j = i;
            Letters[i].OnInteract += delegate () { LetterPressed(j); return false; };
        }

        SubmitBtn.OnInteract += delegate () { SubmitButtonPressed(); return false; };
        ToggleBtn.OnInteract += delegate () { ToggleButtonPressed(); return false; };

        Module.OnActivate += OnActivate;
    }

    // Starts the module
    private void Start() {
        DisableAll();
        DetermineLevel();
    }

    // Bomb lights turn on
    private void OnActivate() {
        if (levelFound == false)
            StartCoroutine(PendingText());

        else
            StartCoroutine(LevelFound());
    }

    


    // Disables all letters and selectables
    private void DisableAll() {
        for (int i = 0; i < Letters.Length; i++) {
            letterIndexes[i] = 0;
            letterDisplays[i] = "A";
            Letters[i].gameObject.SetActive(false);
            LetterTexts[i].text = "";
            LetterButtons[i].enabled = false;
        }

        SubmitBtn.gameObject.SetActive(false);
        SubmitText.text = "";
        SubmitBtnModel.enabled = false;
        ToggleBtn.gameObject.SetActive(false);
        ToggleText.text = "";
        ToggleBtnModel.enabled = false;
    }


    // Tracking solve count
    private void Update() {
        if (levelFound == true)
            solves = Bomb.GetSolvedModuleNames().Count();

        progress = solves / solvesNeeded * 100;

        // Shows progress
        if (progressBars < 10 && progressBars < (int)Math.Floor(progress / 10) && canFlashNext == true) {
            StartCoroutine(ShowProgress());
        }

        // Progress bar fills
        if (progressBars >= 10 && solvesReached == false) {
            solvesReached = true;

            if (levelFound == false) {
                Debug.LogFormat("[100 Levels of Defusal #{0}] Module solved!", moduleId);
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
            }

            else {
                Audio.PlaySoundAtTransform("100Levels_ProgressFilled", transform);
                Debug.LogFormat("[100 Levels of Defusal #{0}] Progress bar filled! Generating cipher...", moduleId);
                StartCoroutine(DelayGeneration());
            }
        }
    }

    // Displays progress bars
    private IEnumerator ShowProgress() {
        canFlashNext = false;
        progressBars++;

        for (int i = 1; i <= progressBars && i <= 10; i++) {
            ProgressBar[i - 1].material = ProgressBarColors[1];
        }

        yield return new WaitForSeconds(0.25f);
        canFlashNext = true;
    }

    // Delays cipher generation
    private IEnumerator DelayGeneration() {
        yield return new WaitForSeconds(0.8f);
        GenerateCipher();
    }


    // Letter is pressed
    private void LetterPressed(int index) {
        Letters[index].AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Letters[index].transform);

        if (lockButtons == false)
            Increment(index);
    }

    // Increments the letter
    private void Increment(int index) {
        if (direction == true) {
            letterIndexes[index]++;

            if (letterIndexes[index] == 26)
                letterIndexes[index] = 0;
        }

        else {
            letterIndexes[index]--;

            if (letterIndexes[index] == -1)
                letterIndexes[index] = 25;
        }

        letterDisplays[index] = LETTERS[letterIndexes[index]];
        LetterTexts[index].text = letterDisplays[index];
    }

    // Submit button pressed
    private void SubmitButtonPressed() {
        SubmitBtn.AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitBtn.transform);

        if (lockButtons == false) {
            string str = "";
            for (int i = 0; i < letterSlotsUsed; i++)
                str += LETTERS[letterIndexes[lettersUsed[i]]];

            Debug.LogFormat("[100 Levels of Defusal #{0}] You submitted: {1}", moduleId, str);

            // Turns the buttons off
            lockButtons = true;
            SubmitBtn.gameObject.SetActive(false);
            SubmitText.text = "";
            SubmitBtnModel.enabled = false;
            ToggleBtn.gameObject.SetActive(false);
            ToggleText.text = "";
            ToggleBtnModel.enabled = false;


            // Correct answer
            if (str == correctMessage) {
                StartCoroutine(CorrectAnswer());
                StartCoroutine(ShowSolveText());

                if (solvesReached == false)
                    solves++;

                else {
                    Debug.LogFormat("[100 Levels of Defusal #{0}] Module solved!", moduleId);
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                }
            }

            else {
                Debug.LogFormat("[100 Levels of Defusal #{0}] That was wrong...", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
                moduleStrikes++;
                StartCoroutine(IncorrectAnswer());
            }
        }
    }

    // Toggle button pressed
    private void ToggleButtonPressed() {
        ToggleBtn.AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ToggleBtn.transform);

        if (lockButtons == false) {
            if (direction == true)
                direction = false;

            else
                direction = true;
        }
    }


    // Pending text
    private IEnumerator PendingText() {
        LevelText.text = "PENDING.";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING..";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING...";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING.";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING..";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING...";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING.";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING..";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING...";
        float randomWait = UnityEngine.Random.Range(0.2f, 0.6f);
        yield return new WaitForSeconds(randomWait);
        Audio.PlaySoundAtTransform("100Levels_LevelNotFound", transform);
        yield return new WaitForSeconds(0.04f);
        LevelText.text = "ERROR";
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = "ERROR";
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = "ERROR";
        yield return new WaitForSeconds(0.25f);
        Debug.LogFormat("[100 Levels of Defusal #{0}] No level found. Generating cipher...", moduleId);
        GenerateCipher();
        yield return new WaitForSeconds(0.55f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = "LEVEL #??";
    }

    // Level found
    private IEnumerator LevelFound() {
        LevelText.text = "";
        string txtDisplayed;

        if (level < 10)
            txtDisplayed = "LEVEL #0" + level;

        else
            txtDisplayed = "LEVEL #" + level;

        yield return new WaitForSeconds(1.0f);
        LevelText.text = txtDisplayed;
        PlayIntroSound();
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = txtDisplayed;
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = txtDisplayed;
    }

    // Plays the intro sound according to the level
    private void PlayIntroSound() {
        switch (letters) {
        case 4: Audio.PlaySoundAtTransform("100Levels_Stage2", transform); break;
        case 5: Audio.PlaySoundAtTransform("100Levels_Stage3", transform); break;
        case 6: Audio.PlaySoundAtTransform("100Levels_Stage4", transform); break;
        case 7: Audio.PlaySoundAtTransform("100Levels_Stage5", transform); break;
        case 8: Audio.PlaySoundAtTransform("100Levels_Stage6", transform); break;
        case 9: Audio.PlaySoundAtTransform("100Levels_Stage7", transform); break;
        case 10: Audio.PlaySoundAtTransform("100Levels_Stage8", transform); break;
        case 11: Audio.PlaySoundAtTransform("100Levels_Stage9", transform); break;
        case 12: Audio.PlaySoundAtTransform("100Levels_Stage10", transform); break;
        default: Audio.PlaySoundAtTransform("100Levels_Stage1", transform); break;
        }
    }


    // Runs a cipher
    private void GenerateCipher() {
        DisableAll();
        lockButtons = true;
        ColorText(3);

        if (Bomb.GetStrikes() > moduleStrikes)
            letterSlotsUsed = letters - Bomb.GetStrikes();

        else if (Bomb.GetStrikes() < moduleStrikes)
            letterSlotsUsed = letters - moduleStrikes;

        else
            letterSlotsUsed = letters - Bomb.GetStrikes();

        if (letterSlotsUsed < 2)
            letterSlotsUsed = 2;


        // Determines which slots are used
        switch (letterSlotsUsed) {
        case 3:
        lettersUsed[0] = 24; lettersUsed[1] = 25; lettersUsed[2] = 26; lettersUsed[3] = -1; lettersUsed[4] = -1; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 4:
        lettersUsed[0] = 7; lettersUsed[1] = 8; lettersUsed[2] = 9; lettersUsed[3] = 10; lettersUsed[4] = -1; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 5:
        lettersUsed[0] = 23; lettersUsed[1] = 24; lettersUsed[2] = 25; lettersUsed[3] = 26; lettersUsed[4] = 27; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 6:
        lettersUsed[0] = 6; lettersUsed[1] = 7; lettersUsed[2] = 8; lettersUsed[3] = 9; lettersUsed[4] = 10; lettersUsed[5] = 11;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 7:
        lettersUsed[0] = 1; lettersUsed[1] = 2; lettersUsed[2] = 3; lettersUsed[3] = 4; lettersUsed[4] = 29; lettersUsed[5] = 30;
        lettersUsed[6] = 31; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 8:
        lettersUsed[0] = 1; lettersUsed[1] = 2; lettersUsed[2] = 3; lettersUsed[3] = 4; lettersUsed[4] = 13; lettersUsed[5] = 14;
        lettersUsed[6] = 15; lettersUsed[7] = 16; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 9:
        lettersUsed[0] = 18; lettersUsed[1] = 19; lettersUsed[2] = 20; lettersUsed[3] = 21; lettersUsed[4] = 22; lettersUsed[5] = 13;
        lettersUsed[6] = 14; lettersUsed[7] = 15; lettersUsed[8] = 16; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 10:
        lettersUsed[0] = 18; lettersUsed[1] = 19; lettersUsed[2] = 20; lettersUsed[3] = 21; lettersUsed[4] = 22; lettersUsed[5] = 28;
        lettersUsed[6] = 29; lettersUsed[7] = 30; lettersUsed[8] = 31; lettersUsed[9] = 32; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 11:
        lettersUsed[0] = 0; lettersUsed[1] = 1; lettersUsed[2] = 2; lettersUsed[3] = 3; lettersUsed[4] = 4; lettersUsed[5] = 5;
        lettersUsed[6] = 28; lettersUsed[7] = 29; lettersUsed[8] = 30; lettersUsed[9] = 31; lettersUsed[10] = 32; lettersUsed[11] = -1; break;

        case 12:
        lettersUsed[0] = 0; lettersUsed[1] = 1; lettersUsed[2] = 2; lettersUsed[3] = 3; lettersUsed[4] = 4; lettersUsed[5] = 5;
        lettersUsed[6] = 12; lettersUsed[7] = 13; lettersUsed[8] = 14; lettersUsed[9] = 15; lettersUsed[10] = 16; lettersUsed[11] = 17; break;

        default:
        lettersUsed[0] = 8; lettersUsed[1] = 9; lettersUsed[2] = -1; lettersUsed[3] = -1; lettersUsed[4] = -1; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;
        }


        // Generates the letters
        screenDisplay = "";
        for (int i = 0; i < displayedLetters.Length; i++) {
            displayedLetters[i] = '.';
        }

        int[] availableLetters = { 1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25 };

        for (int i = 0; i < letterSlotsUsed; i++) {
            letterIndexes[lettersUsed[i]] = availableLetters[UnityEngine.Random.Range(0, availableLetters.Length)];
            letterDisplays[lettersUsed[i]] = LETTERS[letterIndexes[lettersUsed[i]]];
            screenDisplay += LETTERS[letterIndexes[lettersUsed[i]]];
            displayedLetters[i] = screenDisplay.ToCharArray()[i];
        }

        Debug.LogFormat("[100 Levels of Defusal #{0}] The cipher is {1} letters long, and the display is: {2}", moduleId, letterSlotsUsed, screenDisplay);

        // Turns the letters into the number
        int firstPairSum = 0;
        int secndPairSum = 0;

        for (int i = 0; i < letterSlotsUsed - 1; i++) {
            for (int j = i + 1; j < letterSlotsUsed; j++) {
                firstPairSum += FIRSTVALUE[letterIndexes[lettersUsed[i]]] * FIRSTVALUE[letterIndexes[lettersUsed[j]]];
                secndPairSum += SECNDVALUE[letterIndexes[lettersUsed[i]]] * SECNDVALUE[letterIndexes[lettersUsed[j]]];
            }
        }

        Debug.LogFormat("[100 Levels of Defusal #{0}] Value A is {1}, and Value B is {2}.", moduleId, firstPairSum, secndPairSum);

        int calculatedValue = firstPairSum + secndPairSum;

        if (levelFound == false)
            calculatedValue *= FIXLEVEL;

        else
            calculatedValue *= level;


        // Modifies the number into the number to decrypt
        string convertedNumber = BaseConvert(calculatedValue);
        Debug.LogFormat("[100 Levels of Defusal #{0}] The calculated value is {1}, which converts to {2} in base 6.", moduleId, calculatedValue, convertedNumber);

        string usedNumber = DetermineUsedNumber(convertedNumber.ToCharArray());
        Debug.LogFormat("[100 Levels of Defusal #{0}] After further modifcations, the number used in calculations is {1}.", moduleId, usedNumber);


        // Converts the number back to letters
        correctMessage = "";
        char[] usedNumberArray = usedNumber.ToCharArray();

        for (int i = usedNumberArray.Length - 1; i > 0; i--) {
            switch (usedNumberArray[i].ToString() + usedNumberArray[i - 1].ToString()) {
            case "01": correctMessage = "A" + correctMessage; break;
            case "02": correctMessage = "E" + correctMessage; break;
            case "03": correctMessage = "E" + correctMessage; break;
            case "04": correctMessage = "I" + correctMessage; break;
            case "05": correctMessage = "O" + correctMessage; break;
            case "06": correctMessage = "U" + correctMessage; break;
            case "10": correctMessage = "A" + correctMessage; break;
            case "11": correctMessage = "B" + correctMessage; break;
            case "12": correctMessage = "C" + correctMessage; break;
            case "13": correctMessage = "D" + correctMessage; break;
            case "14": correctMessage = "F" + correctMessage; break;
            case "15": correctMessage = "G" + correctMessage; break;
            case "16": correctMessage = "H" + correctMessage; break;
            case "20": correctMessage = "E" + correctMessage; break;
            case "21": correctMessage = "C" + correctMessage; break;
            case "22": correctMessage = "J" + correctMessage; break;
            case "23": correctMessage = "K" + correctMessage; break;
            case "24": correctMessage = "L" + correctMessage; break;
            case "25": correctMessage = "M" + correctMessage; break;
            case "26": correctMessage = "N" + correctMessage; break;
            case "30": correctMessage = "E" + correctMessage; break;
            case "31": correctMessage = "D" + correctMessage; break;
            case "32": correctMessage = "K" + correctMessage; break;
            case "33": correctMessage = "P" + correctMessage; break;
            case "34": correctMessage = "Q" + correctMessage; break;
            case "35": correctMessage = "R" + correctMessage; break;
            case "36": correctMessage = "S" + correctMessage; break;
            case "40": correctMessage = "I" + correctMessage; break;
            case "41": correctMessage = "F" + correctMessage; break;
            case "42": correctMessage = "L" + correctMessage; break;
            case "43": correctMessage = "Q" + correctMessage; break;
            case "44": correctMessage = "T" + correctMessage; break;
            case "45": correctMessage = "V" + correctMessage; break;
            case "46": correctMessage = "W" + correctMessage; break;
            case "50": correctMessage = "O" + correctMessage; break;
            case "51": correctMessage = "G" + correctMessage; break;
            case "52": correctMessage = "M" + correctMessage; break;
            case "53": correctMessage = "R" + correctMessage; break;
            case "54": correctMessage = "V" + correctMessage; break;
            case "55": correctMessage = "X" + correctMessage; break;
            case "56": correctMessage = "Y" + correctMessage; break;
            case "60": correctMessage = "U" + correctMessage; break;
            case "61": correctMessage = "H" + correctMessage; break;
            case "62": correctMessage = "N" + correctMessage; break;
            case "63": correctMessage = "S" + correctMessage; break;
            case "64": correctMessage = "W" + correctMessage; break;
            case "65": correctMessage = "Y" + correctMessage; break;
            case "66": correctMessage = "Z" + correctMessage; break;
            default: correctMessage = "A" + correctMessage; break;
            }
        }

        Debug.LogFormat("[100 Levels of Defusal #{0}] The answer to submit is: {1}", moduleId, correctMessage);

        // Starts the screen display
        StartCoroutine(ShowLetters(true));
    }


    // Converts from base 10 to base 6
    private string BaseConvert(int no) {
        string str = "";
        int pos = 7;

        while (pos >= 0) {
            int counter = 0;

            while (no >= Math.Pow(6, pos)) {
                no -= (int)Math.Pow(6, pos);
                counter++;
            }

            str += counter;
            pos--;
        }

        // Removes leading zeros
        char[] noArray = str.ToCharArray();
        str = "";
        bool foundStart = false;

        for (int i = 0; i < noArray.Length; i++) {
            if (foundStart == false) {
                if (noArray[i] != '0')
                    foundStart = true;
            }

            if (foundStart == true)
                str += noArray[i];
        }

        return str;
    }

    // Calculates the number that will be used for the calculation
    private string DetermineUsedNumber(char[] noArray) {
        string str = "";
        int counter = 0;

        for (int i = noArray.Length - 1; i >= 0 && counter < letterSlotsUsed + 1; i--) {
            if (noArray[i] == '0')
                str = 6 + str;

            else
                str = noArray[i] + str;

            counter++;
        }

        while (counter < letterSlotsUsed + 1) {
            str = 0 + str;
            counter++;
        }

        return str;
    }


    // Obfuscates the next
    private IEnumerator Obfuscate(int index, int func, bool sound) {
        int originalLetter = letterIndexes[index];
        float waitTime = 1.5f / 52.0f;

        if (sound == true)
            Audio.PlaySoundAtTransform("100Levels_Letter", transform);

        for (int i = 0; i < 52; i++) {
            Increment(index);
            yield return new WaitForSeconds(waitTime);
        }

        if (sound == true && func != 1)
            Audio.PlaySoundAtTransform("100Levels_LetterStop", transform);

        /* 0 = Do nothing
         * 1 = Disappear
         * 2 = Show correct letter
         */

        // Removes the letter
        if (func == 1) {
            letterIndexes[index] = 0;
            letterDisplays[index] = "A";
            Letters[index].gameObject.SetActive(false);
            LetterTexts[index].text = "";
            LetterButtons[index].enabled = false;
        }

        // Shows the correct letter
        else if (func == 2) {
            ColorText(2);

            char[] correctAnsArray = correctMessage.ToCharArray();
            for (int i = 0; i < letterSlotsUsed; i++)
                LetterTexts[lettersUsed[i]].text = correctAnsArray[i].ToString();
        }

        else if (func == 0) {
            letterIndexes[index] = originalLetter;
            letterDisplays[index] = LETTERS[letterIndexes[index]];
            LetterTexts[index].text = letterDisplays[index];
        }
    }

    // Colors each text
    private void ColorText(int colorIndex) {
        /* 0 = White
         * 1 = Red
         * 2 = Orange
         * 3 = Yellow
         * 4 = Green
         * 5 = Cyan
         */

        for (int i = 0; i < LetterTexts.Length; i++) {
            LetterTexts[i].color = TextColors[colorIndex];
        }
    }

    // Shows the letters on the screen one by one
    private IEnumerator ShowLetters(bool unlock) {
        float waitTime = 3.0f / letterSlotsUsed;

        for (int i = 0; i < letterSlotsUsed; i++) {
            LetterTexts[lettersUsed[i]].text = letterDisplays[lettersUsed[i]];
            Letters[lettersUsed[i]].gameObject.SetActive(true);
            LetterButtons[lettersUsed[i]].enabled = true;
            StartCoroutine(Obfuscate(lettersUsed[i], 0, true));
            yield return new WaitForSeconds(waitTime);
        }

        if (unlock == true) {
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(UnlockButtons());
        }
    }

    // Unlocks the buttons
    private IEnumerator UnlockButtons() {
        Audio.PlaySoundAtTransform("100Levels_Switch", transform);
        SubmitBtn.gameObject.SetActive(true);
        SubmitText.text = "SUBMIT";
        SubmitBtnModel.enabled = true;
        ToggleBtn.gameObject.SetActive(true);
        ToggleText.text = "TOGGLE";
        ToggleBtnModel.enabled = true;

        ColorText(5);
        yield return new WaitForSeconds(0.1f);
        ColorText(0);
        yield return new WaitForSeconds(0.1f);
        ColorText(5);
        yield return new WaitForSeconds(0.1f);
        ColorText(0);
        yield return new WaitForSeconds(0.1f);
        ColorText(5);
        yield return new WaitForSeconds(0.1f);
        ColorText(0);
        lockButtons = false;
    }


    // Correct answer
    private IEnumerator CorrectAnswer() {
        ColorText(4);
        Debug.LogFormat("[100 Levels of Defusal #{0}] That was correct!", moduleId);
        yield return new WaitForSeconds(3.0f);
        for (int i = 0; i < letterSlotsUsed; i++) {
            if (i == 0)
                StartCoroutine(Obfuscate(lettersUsed[i], 1, true));

            else
                StartCoroutine(Obfuscate(lettersUsed[i], 1, false));
        }
    }

    // Incorrect answer
    private IEnumerator IncorrectAnswer() {
        ColorText(1);
        yield return new WaitForSeconds(1.5f);
        for (int i = 0; i < letterSlotsUsed; i++) {
            if (i == 0)
                StartCoroutine(Obfuscate(lettersUsed[i], 2, true));

            else
                StartCoroutine(Obfuscate(lettersUsed[i], 2, false));
        }

        yield return new WaitForSeconds(5.5f);
        for (int i = 0; i < letterSlotsUsed; i++) {
            if (i == 0)
                StartCoroutine(Obfuscate(lettersUsed[i], 1, true));

            else
                StartCoroutine(Obfuscate(lettersUsed[i], 1, false));
        }

        yield return new WaitForSeconds(2.0f);
        Debug.LogFormat("[100 Levels of Defusal #{0}] Generating new cipher...", moduleId);
        GenerateCipher();
    }

    // Delayed generation
    private IEnumerator Delay() {
        yield return new WaitForSeconds(2.5f);
        GenerateCipher();
    }

    // Displays "SOLVED" on the screen
    private IEnumerator ShowSolveText() {
        yield return new WaitForSeconds(5.0f);
        letterSlotsUsed = 6;

        lettersUsed[0] = 6; lettersUsed[1] = 7; lettersUsed[2] = 8; lettersUsed[3] = 9; lettersUsed[4] = 10; lettersUsed[5] = 11;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1;

        letterIndexes[6] = 18; letterIndexes[7] = 14; letterIndexes[8] = 11; letterIndexes[9] = 21; letterIndexes[10] = 4; letterIndexes[11] = 3;

        for (int i = 0; i < letterSlotsUsed; i++)
            letterDisplays[lettersUsed[i]] = LETTERS[letterIndexes[lettersUsed[i]]];

        StartCoroutine(ShowLetters(false));
    }


    // Determine level
    private void DetermineLevel() {
        //Debug.LogFormat("[100 Levels of Defusal #{0}] Mission ID: {1}", moduleId, GetMissionID());

        switch (GetMissionID()) {
        case "mod_100LevelsOfDefusalMissions_level1": level = 1; break;
        case "mod_100LevelsOfDefusalMissions_level2": level = 2; break;
        case "mod_100LevelsOfDefusalMissions_level3": level = 3; break;
        case "mod_100LevelsOfDefusalMissions_level4": level = 4; break;
        case "mod_100LevelsOfDefusalMissions_level5": level = 5; break;
        case "mod_100LevelsOfDefusalMissions_level6": level = 6; break;
        case "mod_100LevelsOfDefusalMissions_level7": level = 7; break;
        case "mod_100LevelsOfDefusalMissions_level8": level = 8; break;
        case "mod_100LevelsOfDefusalMissions_level9": level = 9; break;
        case "mod_100LevelsOfDefusalMissions_level10": level = 10; break;
        case "mod_100LevelsOfDefusalMissions_level11": level = 11; break;
        case "mod_100LevelsOfDefusalMissions_level12": level = 12; break;
        case "mod_100LevelsOfDefusalMissions_level13": level = 13; break;
        case "mod_100LevelsOfDefusalMissions_level14": level = 14; break;
        case "mod_100LevelsOfDefusalMissions_level15": level = 15; break;
        case "mod_100LevelsOfDefusalMissions_level16": level = 16; break;
        case "mod_100LevelsOfDefusalMissions_level17": level = 17; break;
        case "mod_100LevelsOfDefusalMissions_level18": level = 18; break;
        case "mod_100LevelsOfDefusalMissions_level19": level = 19; break;
        case "mod_100LevelsOfDefusalMissions_level20": level = 20; break;
        case "mod_100LevelsOfDefusalMissions_level21": level = 21; break;
        case "mod_100LevelsOfDefusalMissions_level22": level = 22; break;
        case "mod_100LevelsOfDefusalMissions_level23": level = 23; break;
        case "mod_100LevelsOfDefusalMissions_level24": level = 24; break;
        case "mod_100LevelsOfDefusalMissions_level25": level = 25; break;
        case "mod_100LevelsOfDefusalMissions_level26": level = 26; break;
        case "mod_100LevelsOfDefusalMissions_level27": level = 27; break;
        case "mod_100LevelsOfDefusalMissions_level28": level = 28; break;
        case "mod_100LevelsOfDefusalMissions_level29": level = 29; break;
        case "mod_100LevelsOfDefusalMissions_level30": level = 30; break;
        case "mod_100LevelsOfDefusalMissions_level31": level = 31; break;
        case "mod_100LevelsOfDefusalMissions_level32": level = 32; break;
        case "mod_100LevelsOfDefusalMissions_level33": level = 33; break;
        case "mod_100LevelsOfDefusalMissions_level34": level = 34; break;
        case "mod_100LevelsOfDefusalMissions_level35": level = 35; break;
        case "mod_100LevelsOfDefusalMissions_level36": level = 36; break;
        case "mod_100LevelsOfDefusalMissions_level37": level = 37; break;
        case "mod_100LevelsOfDefusalMissions_level38": level = 38; break;
        case "mod_100LevelsOfDefusalMissions_level39": level = 39; break;
        case "mod_100LevelsOfDefusalMissions_level40": level = 40; break;
        case "mod_100LevelsOfDefusalMissions_level41": level = 41; break;
        case "mod_100LevelsOfDefusalMissions_level42": level = 42; break;
        case "mod_100LevelsOfDefusalMissions_level43": level = 43; break;
        case "mod_100LevelsOfDefusalMissions_level44": level = 44; break;
        case "mod_100LevelsOfDefusalMissions_level45": level = 45; break;
        case "mod_100LevelsOfDefusalMissions_level46": level = 46; break;
        case "mod_100LevelsOfDefusalMissions_level47": level = 47; break;
        case "mod_100LevelsOfDefusalMissions_level48": level = 48; break;
        case "mod_100LevelsOfDefusalMissions_level49": level = 49; break;
        case "mod_100LevelsOfDefusalMissions_level50": level = 50; break;
        case "mod_100LevelsOfDefusalMissions_level51": level = 51; break;
        case "mod_100LevelsOfDefusalMissions_level52": level = 52; break;
        case "mod_100LevelsOfDefusalMissions_level53": level = 53; break;
        case "mod_100LevelsOfDefusalMissions_level54": level = 54; break;
        case "mod_100LevelsOfDefusalMissions_level55": level = 55; break;
        case "mod_100LevelsOfDefusalMissions_level56": level = 56; break;
        case "mod_100LevelsOfDefusalMissions_level57": level = 57; break;
        case "mod_100LevelsOfDefusalMissions_level58": level = 58; break;
        case "mod_100LevelsOfDefusalMissions_level59": level = 59; break;
        case "mod_100LevelsOfDefusalMissions_level60": level = 60; break;
        case "mod_100LevelsOfDefusalMissions_level61": level = 61; break;
        case "mod_100LevelsOfDefusalMissions_level62": level = 62; break;
        case "mod_100LevelsOfDefusalMissions_level63": level = 63; break;
        case "mod_100LevelsOfDefusalMissions_level64": level = 64; break;
        case "mod_100LevelsOfDefusalMissions_level65": level = 65; break;
        case "mod_100LevelsOfDefusalMissions_level66": level = 66; break;
        case "mod_100LevelsOfDefusalMissions_level67": level = 67; break;
        case "mod_100LevelsOfDefusalMissions_level68": level = 68; break;
        case "mod_100LevelsOfDefusalMissions_level69": level = 69; break;
        case "mod_100LevelsOfDefusalMissions_level70": level = 70; break;
        case "mod_100LevelsOfDefusalMissions_level71": level = 71; break;
        case "mod_100LevelsOfDefusalMissions_level72": level = 72; break;
        case "mod_100LevelsOfDefusalMissions_level73": level = 73; break;
        case "mod_100LevelsOfDefusalMissions_level74": level = 74; break;
        case "mod_100LevelsOfDefusalMissions_level75": level = 75; break;
        case "mod_100LevelsOfDefusalMissions_level76": level = 76; break;
        case "mod_100LevelsOfDefusalMissions_level77": level = 77; break;
        case "mod_100LevelsOfDefusalMissions_level78": level = 78; break;
        case "mod_100LevelsOfDefusalMissions_level79": level = 79; break;
        case "mod_100LevelsOfDefusalMissions_level80": level = 80; break;
        case "mod_100LevelsOfDefusalMissions_level81": level = 81; break;
        case "mod_100LevelsOfDefusalMissions_level82": level = 82; break;
        case "mod_100LevelsOfDefusalMissions_level83": level = 83; break;
        case "mod_100LevelsOfDefusalMissions_level84": level = 84; break;
        case "mod_100LevelsOfDefusalMissions_level85": level = 85; break;
        case "mod_100LevelsOfDefusalMissions_level86": level = 86; break;
        case "mod_100LevelsOfDefusalMissions_level87": level = 87; break;
        case "mod_100LevelsOfDefusalMissions_level88": level = 88; break;
        case "mod_100LevelsOfDefusalMissions_level89": level = 89; break;
        case "mod_100LevelsOfDefusalMissions_level90": level = 90; break;
        case "mod_100LevelsOfDefusalMissions_level91": level = 91; break;
        case "mod_100LevelsOfDefusalMissions_level92": level = 92; break;
        case "mod_100LevelsOfDefusalMissions_level93": level = 93; break;
        case "mod_100LevelsOfDefusalMissions_level94": level = 94; break;
        case "mod_100LevelsOfDefusalMissions_level95": level = 95; break;
        case "mod_100LevelsOfDefusalMissions_level96": level = 96; break;
        case "mod_100LevelsOfDefusalMissions_level97": level = 97; break;
        case "mod_100LevelsOfDefusalMissions_level98": level = 98; break;
        case "mod_100LevelsOfDefusalMissions_level99": level = 99; break;
        case "mod_100LevelsOfDefusalMissions_level100": level = 100; break;
        }

        // Determines the number of solves needed and the letters used in the cipher
        switch (level) {
        case 1: solvesNeeded = 1; letters = 3; break;
        case 2: solvesNeeded = 2; letters = 3; break;
        case 3: solvesNeeded = 3; letters = 3; break;
        case 4: solvesNeeded = 4; letters = 4; break;
        case 5: solvesNeeded = 5; letters = 4; break;
        case 6: solvesNeeded = 6; letters = 4; break;
        case 7: solvesNeeded = 7; letters = 4; break;
        case 8: solvesNeeded = 8; letters = 5; break;
        case 9: solvesNeeded = 8; letters = 5; break;
        case 10: solvesNeeded = 9; letters = 5; break;
        case 11: solvesNeeded = 9; letters = 5; break;
        case 12: solvesNeeded = 10; letters = 5; break;
        case 13: solvesNeeded = 10; letters = 6; break;
        case 14: solvesNeeded = 11; letters = 6; break;
        case 15: solvesNeeded = 11; letters = 6; break;
        case 16: solvesNeeded = 12; letters = 6; break;
        case 17: solvesNeeded = 12; letters = 6; break;
        case 18: solvesNeeded = 13; letters = 6; break;
        case 19: solvesNeeded = 13; letters = 6; break;
        case 20: solvesNeeded = 14; letters = 6; break;
        case 21: solvesNeeded = 14; letters = 7; break;
        case 22: solvesNeeded = 15; letters = 7; break;
        case 23: solvesNeeded = 15; letters = 7; break;
        case 24: solvesNeeded = 15; letters = 7; break;
        case 25: solvesNeeded = 16; letters = 7; break;
        case 26: solvesNeeded = 16; letters = 7; break;
        case 27: solvesNeeded = 16; letters = 7; break;
        case 28: solvesNeeded = 16; letters = 7; break;
        case 29: solvesNeeded = 17; letters = 7; break;
        case 30: solvesNeeded = 17; letters = 8; break;
        case 31: solvesNeeded = 18; letters = 8; break;
        case 32: solvesNeeded = 18; letters = 8; break;
        case 33: solvesNeeded = 18; letters = 8; break;
        case 34: solvesNeeded = 19; letters = 8; break;
        case 35: solvesNeeded = 19; letters = 8; break;
        case 36: solvesNeeded = 19; letters = 8; break;
        case 37: solvesNeeded = 20; letters = 8; break;
        case 38: solvesNeeded = 20; letters = 8; break;
        case 39: solvesNeeded = 21; letters = 8; break;
        case 40: solvesNeeded = 21; letters = 8; break;
        case 41: solvesNeeded = 22; letters = 8; break;
        case 42: solvesNeeded = 22; letters = 8; break;
        case 43: solvesNeeded = 22; letters = 8; break;
        case 44: solvesNeeded = 23; letters = 9; break;
        case 45: solvesNeeded = 23; letters = 9; break;
        case 46: solvesNeeded = 23; letters = 9; break;
        case 47: solvesNeeded = 24; letters = 9; break;
        case 48: solvesNeeded = 24; letters = 9; break;
        case 49: solvesNeeded = 24; letters = 9; break;
        case 50: solvesNeeded = 24; letters = 9; break;
        case 51: solvesNeeded = 24; letters = 9; break;
        case 52: solvesNeeded = 24; letters = 9; break;
        case 53: solvesNeeded = 24; letters = 9; break;
        case 54: solvesNeeded = 24; letters = 9; break;
        case 55: solvesNeeded = 25; letters = 9; break;
        case 56: solvesNeeded = 25; letters = 9; break;
        case 57: solvesNeeded = 25; letters = 9; break;
        case 58: solvesNeeded = 25; letters = 9; break;
        case 59: solvesNeeded = 25; letters = 9; break;
        case 60: solvesNeeded = 25; letters = 9; break;
        case 61: solvesNeeded = 25; letters = 10; break;
        case 62: solvesNeeded = 25; letters = 10; break;
        case 63: solvesNeeded = 25; letters = 10; break;
        case 64: solvesNeeded = 25; letters = 10; break;
        case 65: solvesNeeded = 26; letters = 10; break;
        case 66: solvesNeeded = 26; letters = 10; break;
        case 67: solvesNeeded = 26; letters = 10; break;
        case 68: solvesNeeded = 26; letters = 10; break;
        case 69: solvesNeeded = 26; letters = 10; break;
        case 70: solvesNeeded = 26; letters = 10; break;
        case 71: solvesNeeded = 26; letters = 10; break;
        case 72: solvesNeeded = 26; letters = 10; break;
        case 73: solvesNeeded = 26; letters = 10; break;
        case 74: solvesNeeded = 26; letters = 10; break;
        case 75: solvesNeeded = 26; letters = 10; break;
        case 76: solvesNeeded = 26; letters = 10; break;
        case 77: solvesNeeded = 26; letters = 10; break;
        case 78: solvesNeeded = 26; letters = 10; break;
        case 79: solvesNeeded = 27; letters = 10; break;
        case 80: solvesNeeded = 27; letters = 11; break;
        case 81: solvesNeeded = 27; letters = 11; break;
        case 82: solvesNeeded = 27; letters = 11; break;
        case 83: solvesNeeded = 28; letters = 11; break;
        case 84: solvesNeeded = 28; letters = 11; break;
        case 85: solvesNeeded = 28; letters = 11; break;
        case 86: solvesNeeded = 28; letters = 11; break;
        case 87: solvesNeeded = 29; letters = 11; break;
        case 88: solvesNeeded = 29; letters = 11; break;
        case 89: solvesNeeded = 29; letters = 11; break;
        case 90: solvesNeeded = 29; letters = 11; break;
        case 91: solvesNeeded = 30; letters = 11; break;
        case 92: solvesNeeded = 30; letters = 11; break;
        case 93: solvesNeeded = 30; letters = 11; break;
        case 94: solvesNeeded = 30; letters = 11; break;
        case 95: solvesNeeded = 30; letters = 11; break;
        case 96: solvesNeeded = 30; letters = 11; break;
        case 97: solvesNeeded = 30; letters = 12; break;
        case 98: solvesNeeded = 30; letters = 12; break;
        case 99: solvesNeeded = 30; letters = 12; break;
        case 100: solvesNeeded = 30; letters = 12; break;
        default: solvesNeeded = 1; letters = FIXLETTERS; levelFound = false; break; // This doesn't actually require 1 solve
        }

        if (levelFound == true)
            Debug.LogFormat("[100 Levels of Defusal #{0}] Initiating Level {1}. Number of solves needed to unlock cipher: {2}", moduleId, level, solvesNeeded);
    }

    // Gets the mission ID - Thanks to S.
    private string GetMissionID() {
        try {
            Component gameplayState = GameObject.Find("GameplayState(Clone)").GetComponent("GameplayState");
            Type type = gameplayState.GetType();
            FieldInfo fieldMission = type.GetField("MissionToLoad", BindingFlags.Public | BindingFlags.Static);
            return fieldMission.GetValue(gameplayState).ToString();
        }

        catch (NullReferenceException) {
            return "undefined";
        }
    }


    // Twitch Plays Support - Thanks to eXish


    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit <ans> [Submits an answer of 'ans'] | Valid answers have only letters";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (!solvesReached && levelFound)
            {
                yield return "sendtochaterror The module must unlock before an answer can be submitted!";
                yield break;
            }
            if (lockButtons)
            {
                yield return "sendtochaterror An answer cannot be submitted right now!";
                yield break;
            }
            if (letterSlotsUsed != parameters[1].Length)
            {
                yield return "sendtochaterror An answer of length '" + parameters[1].Length + "' cannot be submitted!";
                yield break;
            }
            for (int i = 0; i < parameters[1].Length; i++)
            {
                if (!LETTERS.Contains(parameters[1][i].ToString().ToUpper()))
                {
                    yield return "sendtochaterror The specified answer to submit '" + parameters[1] + "' is invalid!";
                    yield break;
                }
            }
            for (int i = 0; i < parameters[1].Length; i++)
            {
                int forct = 0;
                int backct = 0;
                int counter = letterIndexes[lettersUsed[i]];
                while (counter != Array.IndexOf(LETTERS, parameters[1][i].ToString().ToUpper()))
                {
                    counter++;
                    if (counter == 26)
                        counter = 0;
                    forct++;
                }
                counter = letterIndexes[lettersUsed[i]];
                while (counter != Array.IndexOf(LETTERS, parameters[1][i].ToString().ToUpper()))
                {
                    counter--;
                    if (counter == -1)
                        counter = 25;
                    backct++;
                }
                if (forct > backct)
                {
                    if (direction == true)
                    {
                        ToggleBtn.OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                    for (int j = 0; j < backct; j++)
                    {
                        Letters[lettersUsed[i]].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                else if (forct < backct)
                {
                    if (direction == false)
                    {
                        ToggleBtn.OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                    for (int j = 0; j < forct; j++)
                    {
                        Letters[lettersUsed[i]].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                else
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        ToggleBtn.OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                    for (int j = 0; j < forct; j++)
                    {
                        Letters[lettersUsed[i]].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                }
            }

            // Dynamic scoring
            string str = "";
            for (int i = 0; i < letterSlotsUsed; i++)
                str += LETTERS[letterIndexes[lettersUsed[i]]];

            if (str == correctMessage) {
                if (!levelFound)
                    yield return "solve";
                yield return "awardpointsonsolve " + Math.Floor((double) letterSlotsUsed * 1.5);
            }

            SubmitBtn.OnInteract();
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (levelFound)
        {
            while (!solvesReached) { yield return true; yield return new WaitForSeconds(0.1f); }
        }
        while (lockButtons) { yield return true; yield return new WaitForSeconds(0.1f); }
        yield return ProcessTwitchCommand("submit " + correctMessage);
        while (!moduleSolved) { yield return true; yield return new WaitForSeconds(0.1f); }
    }
}