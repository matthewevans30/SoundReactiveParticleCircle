using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioAnalysis : MonoBehaviour {

    public AudioSource audiosource;
    public float[] samples = new float[512];        // 0 to 20,000 hz compacted into 128 samples
    public float[] freqBand = new float[8];

    public float[] bandBuffer = new float[8];
    public float[] freqBandHighest = new float[8]; //keep track of highest values of each freqband to normalize
    float[] bufferDecrease = new float[8];

    float highest = 0;

    //band chosen to drive reactions
    public int FocusBand;

    // Update is called once per frame
    void Update() {
        if (audiosource != null) {
            //Debug.Log("UpdatingAudioAnalysis");
            GetSpectrumAudioSource();
            MakeIndividualFrequencyBand();
            IndividualBandBuffer();
            NormalizeValue();
        }

    }

    void GetSpectrumAudioSource() {
        audiosource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
    }

    void BandBuffer() {
        for (int i = 0; i < freqBand.Length; i++) {
            //freqBand is lower than buffer band, just copy bandbuffer and reset decreaseAmt
            if (freqBand[i] > bandBuffer[i]) {
                bandBuffer[i] = freqBand[i];
                bufferDecrease[i] = 0.005f;
            }

            //freqBand is below buffer - decrease buffer by decreaseAmt until equal again^
            if (freqBand[i] < bandBuffer[i]) {
                bandBuffer[i] -= bufferDecrease[i];
                bufferDecrease[i] *= 1.2f;
            }
        }
    }

    //developer reference function to find highest value 
    void FindHighest() {
        if (bandBuffer[FocusBand] > highest) {
            highest = bandBuffer[FocusBand];
            Debug.Log(highest);
        }



    }

    void IndividualBandBuffer() {
        //freqBand is lower than buffer band, just copy bandbuffer and reset decreaseAmt
        if (freqBand[FocusBand] > bandBuffer[FocusBand]) {
            bandBuffer[FocusBand] = freqBand[FocusBand];
            bufferDecrease[FocusBand] = 0.005f;
        }

        //freqBand is below buffer - decrease buffer by decreaseAmt until equal again^
        if (freqBand[FocusBand] < bandBuffer[FocusBand]) {
            bandBuffer[FocusBand] -= bufferDecrease[FocusBand];
            bufferDecrease[FocusBand] *= 1.2f;
        }
    }

    //rather than normalizing values on the fly, a full run through of the songs using
    //findHighest() yields an avg highest value of 3
    void NormalizeValue() {
        freqBand[FocusBand] = freqBand[FocusBand] / 3f;
        bandBuffer[FocusBand] = bandBuffer[FocusBand] / 3f;
    }

    //remove audio source when not playing to stop update functions
    void RemoveAudioSource() {
        audiosource = null;
    }

    void MakeFrequencyBands() {
        /*
         * 22050 / 512 = 43 hertz per sample
         * 
         * 20 - 60 hz - sub bass
         * 60 - 250 hz - bass 
         * 250 - 500 hz - low midrange
         * 500 - 2000 hz - midrange
         * 2000 - 4000 hz - upper midrange
         * 4000 - 6000 hz - presence
         * 6000 - 20000 hz - brilliance
         * 
         * 
         * 0 - 2 * 43 = 86
         * 1 - 4 * 63 = 172 or 87 - 258 hz
         * 2 - 8 * 43 = 344 or 259 - 602 hz
         * 3 - 16 * 43 = 688 or 603 - 1290 hz
         * 4 - 32 * 43 = 1376 or 1291 - 2666 hz
         * 5 - 64 * 43 = 2752 or 2667 - 5418 hz
         * 6 - 128 * 42 = 5504 or 5419 - 10922 hz
         * 7 - 256 * 43 = 11008 or 10923 - 21940 hz
         * 
         */

        int count = 0;

        for (int i = 0; i < freqBand.Length; i++) {
            float average = 0;

            //get upper limit
            int sampleCount = (int)Mathf.Pow(2, i + 1);

            //add two samples on to end to make 512 total
            if (i == 7)
                sampleCount += 2;

            //get average of freqbands in range count - sampleCount
            for (int j = 0; j < sampleCount; j++) {
                average += samples[count] * (count + 1);
                count++;
            }

            //calculate average of this range and add to freqBand[]
            average /= sampleCount;
            freqBand[i] = average * 10;
        }
    }

    void MakeIndividualFrequencyBand() {
        int sampleStart = (int)Mathf.Pow(2, FocusBand);
        int sampleEnd = (int)Mathf.Pow(2, FocusBand + 1);

        //edge cases
        if (FocusBand == 0)
            sampleStart = 0;

        if (FocusBand == 7)
            sampleEnd += 2;

        float average = 0;

        for (int i = sampleStart; i < sampleEnd; i++) {
            average += samples[i];
        }

        average /= (sampleEnd - sampleStart);
        freqBand[FocusBand] = average * 10;
    }
}