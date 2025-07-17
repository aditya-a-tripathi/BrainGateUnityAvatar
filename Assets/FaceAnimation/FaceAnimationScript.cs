using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class TextAnimator : MonoBehaviour
{
    private Animator animator;
    public Button convertButton;
    public TMP_InputField inputField;
    // Can test out different values of these public variables to work out most natural speech animation 
    public float defaultHoldTime = 0.2f; // How long each phoneme facial pose should hold 
    public float wordPauseDuration = 0.1f; // Pause time between each word in the text input to make it more natural 

    [System.Serializable]
    public struct AnimationTiming
    {
        public float holdTime;
    }

    // Struct that links blendValue with its specific timing info - can customize this for different phonemes to better reflect natural speech
    [System.Serializable]
    public struct BlendValueTiming
    {
        public float blendValue;
        public AnimationTiming timing;
    }

    // Can edit this in inspector to override the default blend values + associated timings
    public BlendValueTiming[] blendValueTimings;

    private Dictionary<string, BlendValueTiming> charToBlendValueTiming = new Dictionary<string, BlendValueTiming>();

    void Start()
    {
        animator = GetComponent<Animator>();
        convertButton.onClick.AddListener(PlayAnimation);
        InitializeCharToBlendValueTiming();
    }

    void InitializeCharToBlendValueTiming()
    {
        // Initialize with default timings - constrcut a dictionary that maps each character with associated blendValue used by the Animator, IDLE state is just closed mouth
        string[] characters = { "IDLE", "A", "C", "D", "G","H", "Y", "Z", "K", "N", "CH", "SH", "E", "F", "V", "I", "L", "M", "B", "P", "O", "R", "S", "T", "TH", "U", "W", "Q" };
        float[] blendValues = { 0f, 0.2f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.6f, 0.6f, 0.8f, 1f, 1f, 1.2f, 1.2f, 1.4f, 1.4f, 1.4f, 1.6f, 1.8f, 1.8f, 1.8f, 2f, 2.2f, 2.4f, 2.4f };
        for (int i = 0; i < characters.Length; i++)
        {
            charToBlendValueTiming[characters[i]] = new BlendValueTiming
            {
                blendValue = blendValues[i],
                timing = new AnimationTiming { holdTime = defaultHoldTime }
            };
        }

        // Override the default blend settings with custom mappings that can be set in the inspector window
        foreach (var timing in blendValueTimings)
        {
            // Identify which character(s) match the blend value
            var keysToUpdate = charToBlendValueTiming
                .Where(kvp => Mathf.Approximately(kvp.Value.blendValue, timing.blendValue))
                .Select(kvp => kvp.Key)
                .ToList();

            // Update those specific characters with the new custom timing 
            foreach (var key in keysToUpdate)
            {
                charToBlendValueTiming[key] = timing;
            }
        }

    }

    void PlayAnimation()
    {
        string text = inputField.text.ToUpper();
        StartCoroutine(AnimateLetters(text));
    }

    private IEnumerator AnimateLetters(string text)
    {
        // Splits sentence into words 
        string[] words = text.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
        {
            string word = words[wordIndex];

            for (int i = 0; i < word.Length; i++)
            {
                string currentChar = word[i].ToString(); 
                // Check for two-letter digraph phoneme
                string digraph = (i < word.Length - 1) ? word.Substring(i, 2) : "";
                
                // Animate the digraph and skip the second character as its part of digraph
                if (charToBlendValueTiming.TryGetValue(digraph, out BlendValueTiming digraphTiming))
                {
                    yield return StartCoroutine(AnimateToBlendValue(digraphTiming));
                    i++;
                }
                else if (i < word.Length - 1 && currentChar == word[i + 1].ToString())
                {
                    // Handle any double letters (ex: LL)
                    if (charToBlendValueTiming.TryGetValue(currentChar, out BlendValueTiming doubleTiming))
                    {
                        // Increase hold time for double letters - can remove if deemed unnecessary 
                        BlendValueTiming adjustedTiming = doubleTiming;
                        adjustedTiming.timing.holdTime *= 1.5f;
                        yield return StartCoroutine(AnimateToBlendValue(adjustedTiming));
                        i++; // Skip second letter
                    }
                    else
                    {
                        yield return StartCoroutine(AnimateToBlendValue(charToBlendValueTiming["IDLE"]));
                    }
                }
                //Handles normal single character phoneme animations
                else if (charToBlendValueTiming.TryGetValue(currentChar, out BlendValueTiming charTiming))
                {
                    yield return StartCoroutine(AnimateToBlendValue(charTiming));
                }
                else
                {
                    // If character is not found, then use the IDLE animation 
                    yield return StartCoroutine(AnimateToBlendValue(charToBlendValueTiming["IDLE"]));
                }
            }

            // Add pause after each word as long as it's not the last word
            if (wordIndex < words.Length - 1)
            {
                yield return new WaitForSeconds(wordPauseDuration);
            }
        }

        // Transition back to the IDLE state once the sentence ends
        yield return StartCoroutine(AnimateToBlendValue(charToBlendValueTiming["IDLE"]));
    }

    // Adjusting the blend parameter in the Animator to play appropriate animations 
    private IEnumerator AnimateToBlendValue(BlendValueTiming blendValueTiming)
    {
        float startTime = Time.time;
        float startBlend = animator.GetFloat("BlendParameter");
        float targetBlend = blendValueTiming.blendValue;
        // Set the Animator float parameter to the target blend value
        animator.SetFloat("BlendParameter", targetBlend);
        // Hold the pose for the defined time
        yield return new WaitForSeconds(blendValueTiming.timing.holdTime);
    }
}