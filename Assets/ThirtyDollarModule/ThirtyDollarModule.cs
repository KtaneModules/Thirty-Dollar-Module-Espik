using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class ThirtyDollarModule : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] KeyboardPanels;
    public KMSelectable[] DisplayPanels;
    public KMSelectable[] ScrollButtons;
    public KMSelectable SubmitButton;
    public KMSelectable ClearButton;

    public Renderer[] KeyboardEmojis;
    public Renderer[] DisplayEmojis;

    public Renderer[] DisplayBackings;
    public Material[] PanelMaterials;

    public Texture[] Emojis;
    public TextAsset AllSounds;

    public Renderer[] LightBases;
    public Light[] Lights;
    public Material[] LightMaterials;
    public Color[] LightColors;

    public Transform[] DisplayPositions;
    public Transform[] KeyboardPositions;

    // Variables for solving
    private TDSound[] sounds = new TDSound[204];

    private TDSound[] displaySounds = new TDSound[5];
    private TDSound[] submittedSounds = new TDSound[5];
    private int[] correctSounds = new int[5];

    private int[] keyboardIds = new int[12];
    private int keyboardPage = 0;

    private int selectedPanel = -1;

    private bool canPress = false;
    private bool lightsOn = false;
    private bool submitting = false;
    private bool moduleFailure = false;

    private readonly float[] displayXCoords = { -0.044f, -0.022f, 0.0f, 0.022f, 0.044f };
    private readonly float[] keyboardXCoords = { -0.055f, -0.033f, -0.011f, 0.011f, 0.033f, 0.055f };
    private readonly float[] keyboardZCoords = { -0.018f, -0.042f };

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;

    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;
        
        for (int i = 0; i < KeyboardPanels.Length; i++) {
            int j = i;
            KeyboardPanels[i].OnInteract += delegate () {
                PressKeyboardButton(j);
                return false;
            };
        }

        for (int i = 0; i < DisplayPanels.Length; i++) {
            int j = i;
            DisplayPanels[i].OnInteract += delegate () {
                PressDisplayButton(j);
                return false;
            };
        }

        ScrollButtons[0].OnInteract += delegate () {
            ScrollButtons[0].AddInteractionPunch(0.5f);
            PressScrollButton(false);
            return false;
        };

        ScrollButtons[1].OnInteract += delegate () {
            ScrollButtons[1].AddInteractionPunch(0.5f);
            PressScrollButton(true);
            return false;
        };

        SubmitButton.OnInteract += delegate () {
            SubmitButton.AddInteractionPunch(0.5f);
            PressSubmitButton();
            return false;
        };

        ClearButton.OnInteract += delegate () {
            ClearButton.AddInteractionPunch(0.5f);
            PressClearButton();
            return false;
        };
	}

    // Gets information and scales the lights with bomb size
    private void Start() {
        InitSounds();

        if (!moduleFailure) {

            ChooseSounds();
            TurnOffLights();

            keyboardPage = UnityEngine.Random.Range(0, 17);
            Debug.LogFormat("<Thirty Dollar Module #{0}> Keyboard starting at page {1}.", moduleId, keyboardPage);
            StartCoroutine(UpdateKeyboard());
        }

        float scalar = transform.lossyScale.x;
        for (var i = 0; i < Lights.Length; i++)
            Lights[i].range *= scalar;
    }

    // Loads all the sounds into the module
    private void InitSounds() {
        /* This is a temporary solution because I couldn't get loading from the CSV file to work.
         * If anyone else has a solution to this, please make a pull request on GitHub.
         */

        sounds = AddAllSounds.AddSounds();

        /*try {
            string[] lines = AllSounds.text.Split('\n');

            for (int i = 0; i < lines.Length; i++) {
                string[] values = lines[i].Split('\t');
                int index = int.Parse(values[0]);

                sounds[index] = new TDSound(index, values[1], values[2]);
            }
        }

        catch(FormatException) {
            moduleFailure = true;
            canPress = true;
            Debug.LogFormat("[Thirty Dollar Module #{0}] Whoops! Looks like something went wrong. Press any button to solve the module. Please contact Espik about this.", moduleId);
        }*/
    }


    // Chooses the sounds for the module
    private void ChooseSounds() {
        bool isHoriz = UnityEngine.Random.Range(0, 2) == 1;
        int[][] coords = { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] }; // [X,Y]

        int vertLine = 0, horizLine = 0;
        int vertDiag = 0, horizDiag = 0;
        int vertRange = 0, horizRange = 0;

        // Draw vertical hexagon
        if (!isHoriz) {
            coords[0][0] = UnityEngine.Random.Range(1, 11);
            coords[0][1] = UnityEngine.Random.Range(0, 14);

            vertLine = UnityEngine.Random.Range(1, 15 - coords[0][1]);
            vertDiag = UnityEngine.Random.Range(2, 17 - coords[0][1] - vertLine) / 2;
            
            horizRange = Math.Min(coords[0][0], 11 - coords[0][0]);
            horizDiag = UnityEngine.Random.Range(1, horizRange + 1);

            coords = DrawHexagon(coords, false, 0, vertDiag, horizDiag, vertLine);
        }

        // Draw horizontal hexagon
        else {
            coords[0][0] = UnityEngine.Random.Range(0, 9);
            coords[0][1] = UnityEngine.Random.Range(1, 16);

            horizLine = UnityEngine.Random.Range(1, 10 - coords[0][0]);
            horizDiag = UnityEngine.Random.Range(2, 12 - coords[0][0] - horizLine) / 2;

            vertRange = Math.Min(coords[0][1], 16 - coords[0][1]);
            vertDiag = UnityEngine.Random.Range(1, vertRange + 1);

            coords = DrawHexagon(coords, true, 0, vertDiag, horizDiag, horizLine);
        }

        int missingCoord = UnityEngine.Random.Range(0, 6);

        // Randomizes the order the coordinates for the display
        int[] chosenCoords = new int[5];
        int[] displayCoords = new int[5];

        for (int i = 0; i < chosenCoords.Length; i++)
            chosenCoords[i] = i >= missingCoord ? i + 1 : i;

        for (int i = 0; i < displayCoords.Length; i++) {
            int index = UnityEngine.Random.Range(0, chosenCoords.Length - i);
            displayCoords[i] = chosenCoords[index];

            for (int j = index; j < chosenCoords.Length - i - 1; j++)
                chosenCoords[j] = chosenCoords[j + 1];
        }

        for (int i = 0; i < displaySounds.Length; i++) {
            int id = GetCoordID(coords[displayCoords[i]]);

            displaySounds[i] = sounds[id];
            Debug.LogFormat("[Thirty Dollar Module #{0}] Display {1} is playing the sound: {2}.", moduleId, i + 1, displaySounds[i].GetName());

            submittedSounds[i] = sounds[0];
        }

        // Creates a new hexagon for the solution
        Debug.LogFormat("[Thirty Dollar Module #{0}] The missing emoji in the hexagon is: {1}.", moduleId, sounds[GetCoordID(coords[missingCoord])].GetName());

        int compCoord = (missingCoord + 3) % 6;
        coords[compCoord] = coords[missingCoord];

        if (!isHoriz)
            coords = DrawHexagon(coords, false, compCoord, vertDiag, horizDiag, vertLine);

        else
            coords = DrawHexagon(coords, true, compCoord, vertDiag, horizDiag, horizLine);

        // Organizes the hexagon in order for the solution
        for (int i = 0; i < correctSounds.Length; i++) {
            correctSounds[i] = GetCoordID(coords[(compCoord + i + 1) % 6]);
            Debug.LogFormat("[Thirty Dollar Module #{0}] Display {1} should be set to: {2}.", moduleId, i + 1, sounds[correctSounds[i]].GetName());
        }
    }

    // Draws the hexagon
    private int[][] DrawHexagon(int[][] coords, bool isHoriz, int start, int vertDiag, int horizDiag, int line) {
        int i = (start + 1) % 6;

        if (!isHoriz) {
            while (i != start) {
                switch (i) {
                    case 1:
                        coords[1][0] = coords[0][0] + horizDiag;
                        coords[1][1] = coords[0][1] + vertDiag;
                        break;
                    case 2:
                        coords[2][0] = coords[1][0];
                        coords[2][1] = coords[1][1] + line;
                        break;
                    case 3:
                        coords[3][0] = coords[2][0] - horizDiag;
                        coords[3][1] = coords[2][1] + vertDiag;
                        break;
                    case 4:
                        coords[4][0] = coords[3][0] - horizDiag;
                        coords[4][1] = coords[3][1] - vertDiag;
                        break;
                    case 5:
                        coords[5][0] = coords[4][0];
                        coords[5][1] = coords[4][1] - line;
                        break;
                    default:
                        coords[0][0] = coords[5][0] + horizDiag;
                        coords[0][1] = coords[5][1] - vertDiag;
                        break;
                }

                i = (i + 1) % 6;
            }
        }

        else {
            while (i != start) {
                switch (i) {
                    case 1:
                        coords[1][0] = coords[0][0] + horizDiag;
                        coords[1][1] = coords[0][1] - vertDiag;
                        break;
                    case 2:
                        coords[2][0] = coords[1][0] + line;
                        coords[2][1] = coords[1][1];
                        break;
                    case 3:
                        coords[3][0] = coords[2][0] + horizDiag;
                        coords[3][1] = coords[2][1] + vertDiag;
                        break;
                    case 4:
                        coords[4][0] = coords[3][0] - horizDiag;
                        coords[4][1] = coords[3][1] + vertDiag;
                        break;
                    case 5:
                        coords[5][0] = coords[4][0] - line;
                        coords[5][1] = coords[4][1];
                        break;
                    default:
                        coords[0][0] = coords[5][0] - horizDiag;
                        coords[0][1] = coords[5][1] - vertDiag;
                        break;
                }

                i = (i + 1) % 6;
            }
        }

        return coords;
    }

    // Gets the numeric ID from a coordinate on the grid
    private int GetCoordID(int[] coord) {
        int x = coord[0], y = coord[1];

        while (x < 0)
            x += 12;

        while (y < 0)
            y += 17;

        x %= 12;
        y %= 17;

        return y * 12 + x;
    }


    // Sets the keyboard buttons to their correct value
    private IEnumerator UpdateKeyboard() {
        for (int i = 0; i < KeyboardPanels.Length; i++) {
            keyboardIds[i] = keyboardPage * 12 + i;
            KeyboardEmojis[i].material.mainTexture = Emojis[keyboardPage * 12 + i];
            yield return new WaitForSeconds(0.02f);
        }

        canPress = true;
    }

    // Clears the keyboard displays
    private IEnumerator ClearKeyboard() {
        for (int i = 0; i < KeyboardPanels.Length; i++) {
            KeyboardEmojis[i].material.mainTexture = Emojis[0];
            yield return new WaitForSeconds(0.02f);
        }

        StartCoroutine(UpdateKeyboard());
    }


    // Press keyboard button
    private void PressKeyboardButton(int i) {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform);

        if (moduleFailure && !moduleSolved)
            Solve();

        else if (canPress && !moduleSolved) {
            if (selectedPanel != -1) {
                if (lightsOn)
                    TurnOffLights();

                int newId = keyboardIds[i];
                submittedSounds[selectedPanel] = sounds[newId];
                DisplayEmojis[selectedPanel].material.mainTexture = Emojis[newId];

                StartCoroutine(AnimatePress(i, false));
            }

            selectedPanel++;
            ShowSelectedPanel();
        }
    }

    // Press display button
    private void PressDisplayButton(int i) {
        if (moduleFailure && !moduleSolved)
            Solve();

        else if (canPress && !moduleSolved) {
            selectedPanel = i;
            ShowSelectedPanel();

            StartCoroutine(AnimatePress(i, true));
            Audio.PlaySoundAtTransform(displaySounds[i].GetSound(), transform);
        }
    }

    // Changes the color of the displayed panel
    private void ShowSelectedPanel() {
        for (int i = 0; i < DisplayBackings.Length; i++)
            DisplayBackings[i].material = i == selectedPanel ? PanelMaterials[1] : PanelMaterials[0];

        selectedPanel = selectedPanel > 4 ? -1 : selectedPanel;
    }


    // Press scroll button
    private void PressScrollButton(bool isRight) {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);

        if (moduleFailure && !moduleSolved)
            Solve();

        else if (canPress && !moduleSolved) {
            canPress = false;

            keyboardPage = isRight ? keyboardPage += 1  : keyboardPage -= 1;
            keyboardPage %= 17;

            if (keyboardPage < 0)
                keyboardPage += 17;

            StartCoroutine(ClearKeyboard());
        }
    }

    // Press clear button
    private void PressClearButton() {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);

        if (moduleFailure && !moduleSolved)
            Solve();

        else if (canPress && !moduleSolved) {
            canPress = false;

            selectedPanel = -1;
            ShowSelectedPanel();

            if (lightsOn)
                TurnOffLights();

            StartCoroutine(ClearDisplay());
        }
    }

    // Clears the display panels
    private IEnumerator ClearDisplay() {
        for (int i = 0; i < DisplayPanels.Length; i++) {
            DisplayEmojis[i].material.mainTexture = Emojis[0];
            submittedSounds[i] = sounds[0];
            yield return new WaitForSeconds(0.02f);
        }

        canPress = true;
    }

    // Press submit button
    private void PressSubmitButton() {
        if (moduleFailure && !moduleSolved) {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);
            Solve();
        }

        else if (canPress && !moduleSolved) {
            canPress = false;
           
            selectedPanel = -1;
            ShowSelectedPanel();

            if (lightsOn)
                TurnOffLights();
            
            StartCoroutine(Submit());
        }

        else
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);
    }

    // Checks if the answer is correct
    private IEnumerator Submit() {
        submitting = true;
        Debug.LogFormat("[Thirty Dollar Module #{0}] Don't you lecture me with your thirty dollar module!", moduleId);
        Audio.PlaySoundAtTransform("tdm_intro", transform);
        yield return new WaitForSeconds(3.25f);

        bool willStrike = false;
        lightsOn = true;

        for (int i = 0; i < submittedSounds.Length; i++) {
            Debug.LogFormat("[Thirty Dollar Module #{0}] Display {1} - You submitted: {2}", moduleId, i + 1, submittedSounds[i].GetName());

            if (submittedSounds[i].GetId() == correctSounds[i]) {
                Debug.LogFormat("[Thirty Dollar Module #{0}] That was correct!", moduleId);

                LightBases[i].material = LightMaterials[1];
                Lights[i].enabled = true;
                Lights[i].color = LightColors[1];
            }

            else {
                willStrike = true;
                Debug.LogFormat("[Thirty Dollar Module #{0}] That was incorrect! The correct answer was: {1}", moduleId, sounds[correctSounds[i]].GetName());

                LightBases[i].material = LightMaterials[2];
                Lights[i].enabled = true;
                Lights[i].color = LightColors[2];
            }

            StartCoroutine(AnimatePress(i, true));
            Audio.PlaySoundAtTransform(submittedSounds[i].GetSound(), transform);
            yield return new WaitForSeconds(0.5f);
        }

        if (willStrike) {
            Debug.LogFormat("[Thirty Dollar Module #{0}] Strike!", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
            submitting = false;
            canPress = true;
        }

        else
            Solve();
    }


    // Animates the tile press
    private IEnumerator AnimatePress(int index, bool isDisplay) {
        canPress = false;

        float xCoord, zCoord;
        Transform tile;

        if (isDisplay) {
            tile = DisplayPositions[index];
            xCoord = displayXCoords[index];
            zCoord = 0.03f;
        }

        else {
            tile = KeyboardPositions[index];
            xCoord = keyboardXCoords[index % 6];
            zCoord = keyboardZCoords[index / 6];
        }

        for (int i = -5; i < 7; i++) {
            tile.localPosition = new Vector3(xCoord, 0.01f, zCoord + 0.004f - 0.004f / 6 * Math.Abs(i));
            yield return new WaitForSeconds(0.02f);
        }

        if (!submitting)
            canPress = true;
    }

    // Turns off all the lights
    private void TurnOffLights() {
        lightsOn = false;

        for (int i = 0; i < LightBases.Length; i++) {
            LightBases[i].material = LightMaterials[0];
            Lights[i].color = LightColors[0];
            Lights[i].enabled = false;
        }
    }


    // Module solves
    private void Solve() {
        canPress = false;
        moduleSolved = true;
        Debug.LogFormat("[Thirty Dollar Module #{0}] Module solved!", moduleId);
        GetComponent<KMBombModule>().HandlePass();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
    }

    // Twitch Plays command handler - by eXish
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} upper/lower <#> [Presses the specified upper/lower tile '#' in reading order] | !{0} left/right (#) [Presses the specified button at the bottom of the module (optionally '#' times)] | !{0} play [Presses the play button] | !{0} clear [Presses the clear button] | Commands are chainable with semicolons";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(';');
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].EqualsIgnoreCase("play") || parameters[i].EqualsIgnoreCase("clear") || parameters[i].EqualsIgnoreCase("left") || parameters[i].EqualsIgnoreCase("right"))
                continue;
            string[] subparams = parameters[i].Split(' ');
            if (subparams.Length == 2)
            {
                int val;
                if (subparams[0].EqualsIgnoreCase("left") || subparams[0].EqualsIgnoreCase("right"))
                {
                    if (int.TryParse(subparams[1], out val) && val > 0)
                        continue;
                }
                else if (subparams[0].EqualsIgnoreCase("upper"))
                {
                    if (int.TryParse(subparams[1], out val) && val > 0 && val < 6)
                        continue;
                }
                else if (subparams[0].EqualsIgnoreCase("lower"))
                {
                    if (int.TryParse(subparams[1], out val) && val > 0 && val < 13)
                        continue;
                }
            }
            if (parameters.Length > 1)
                yield return "sendtochaterror!f The specified subcommand '" + parameters[i] + "' is invalid!";
            yield break;
        }
        yield return null;
        for (int i = 0; i < parameters.Length; i++)
        {
            while (!canPress) yield return "trycancel";
            string[] subparams = parameters[i].Split(' ');
            if (parameters[i].EqualsIgnoreCase("left"))
                ScrollButtons[0].OnInteract();
            else if (parameters[i].EqualsIgnoreCase("right"))
                ScrollButtons[1].OnInteract();
            else if (parameters[i].EqualsIgnoreCase("play"))
            {
                SubmitButton.OnInteract();
                bool strike = false;
                for (int j = 0; j < submittedSounds.Length; j++)
                {
                    if (submittedSounds[j].GetId() != correctSounds[j])
                    {
                        strike = true;
                        yield return "strike";
                        break;
                    }
                }
                if (!strike)
                    yield return "solve";
                break;
            }
            else if (parameters[i].EqualsIgnoreCase("clear"))
                ClearButton.OnInteract();
            else if (subparams[0].EqualsIgnoreCase("left"))
            {
                for (int j = 0; j < int.Parse(subparams[1]); j++)
                {
                    if (j > 0)
                        while (!canPress) yield return "trycancel";
                    ScrollButtons[0].OnInteract();
                }
            }
            else if (subparams[0].EqualsIgnoreCase("right"))
            {
                for (int j = 0; j < int.Parse(subparams[1]); j++)
                {
                    if (j > 0)
                        while (!canPress) yield return "trycancel";
                    ScrollButtons[1].OnInteract();
                }
            }
            else if (subparams[0].EqualsIgnoreCase("upper"))
                DisplayPanels[int.Parse(subparams[1]) - 1].OnInteract();
            else if (subparams[0].EqualsIgnoreCase("lower"))
                KeyboardPanels[int.Parse(subparams[1]) - 1].OnInteract();
        }
    }

    // Twitch Plays autosolver - by eXish
    IEnumerator TwitchHandleForcedSolve()
    {
        if (moduleFailure)
        {
            SubmitButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            if (submitting)
            {
                for (int j = 0; j < submittedSounds.Length; j++)
                {
                    if (submittedSounds[j].GetId() != correctSounds[j])
                    {
                        StopAllCoroutines();
                        moduleSolved = true;
                        Module.HandlePass();
                        yield break;
                    }
                }
            }
            else
            {
                int index = selectedPanel != -1 ? selectedPanel : 0;
                for (int j = 0; j < submittedSounds.Length; j++)
                {
                    if (submittedSounds[index].GetId() != correctSounds[index])
                    {
                        if (selectedPanel != index)
                        {
                            while (!canPress) yield return true;
                            DisplayPanels[index].OnInteract();
                        }
                        int targetPage = -1;
                        for (int i = 0; i < 17; i++)
                        {
                            for (int k = 0; k < 12; k++)
                            {
                                if (correctSounds[index] == (i * 12 + k))
                                {
                                    targetPage = i;
                                    i = 17;
                                    k = 12;
                                }
                            }
                        }
                        int checker1 = keyboardPage;
                        int checker2 = keyboardPage;
                        while (checker1 != targetPage && checker2 != targetPage)
                        {
                            checker1 -= 1;
                            if (checker1 < 0)
                                checker1 += 17;
                            checker2 += 1;
                            if (checker2 > 16)
                                checker2 -= 17;
                        }
                        if (checker1 == targetPage)
                        {
                            while (keyboardPage != targetPage)
                            {
                                while (!canPress) yield return true;
                                ScrollButtons[0].OnInteract();
                            }
                        }
                        else
                        {
                            while (keyboardPage != targetPage)
                            {
                                while (!canPress) yield return true;
                                ScrollButtons[1].OnInteract();
                            }
                        }
                        while (!canPress) yield return true;
                        KeyboardPanels[correctSounds[index] % 12].OnInteract();
                    }
                    index++;
                    index %= 5;
                }
                while (!canPress) yield return true;
                SubmitButton.OnInteract();
            }
        }
        while (!moduleSolved) yield return true;
    }
}