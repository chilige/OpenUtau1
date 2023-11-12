using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.IO;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using System.Text;
using System.Linq;
using Serilog;
using Newtonsoft.Json;

namespace OpenUtau.Core.DiffSinger {

    [Phonemizer("DiffSinger Mandarin 三段式 RHY Phonemizer", "DIFFS CNM-TRI-RHY", "BaiTang", language: "ZH")]
    public class DiffSingerRhythmizerCHNTRIPhonemizer : DiffSingerRhythmizerBasePhonemizer {

        public Dictionary<string, string[]> chn2PhnDict = new Dictionary<string, string[]> { };
        public Dictionary<string, string[]> chn3PhnDict = new Dictionary<string, string[]> { };

        public override void SetUpPhoneDictAndRhy() {
            LoadSingerRhythmizer("CNM3");
            GetRealPhnDict("ds_CNM3.txt");
            this.chn3PhnDict = this.realPhnDict;
            GetRhyMap();
            try {
                string path = Path.Combine(singer.Location, "ds_CNM.txt");
                this.chn2PhnDict = LoadDsDict(path);
            } catch (Exception) {
                return;
            }
        }

        protected override void ProcessPart(Note[][] phrase) {
            float padding = 0.5f;//Padding time for consonants at the beginning of a sentence
            var phonemes = new List<string> { "SP" };
            var realPhonemes = new List<string> { "SP" };
            var midi = new List<long> { 0 };//Phoneme pitch
            var midi_dur = new List<float> { padding };//List of parent note duration for each phoneme
            var is_slur = new List<bool> { false };//Whether the phoneme is slur
            List<double> ph_dur;//Phoneme durations output by the model
            var notePhIndex = new List<int> { 1 };//The position of the first phoneme of each note in the phoneme list
            var phAlignPoints = new List<Tuple<int, double>>();//Phoneme alignment position, s, absolute time
            double offsetMs = timeAxis.TickPosToMsPos(phrase[0][0].position);

            //Convert note list to phoneme list
            foreach (int groupIndex in Enumerable.Range(0, phrase.Length)) {
                string[] notePhonemes;
                string[] realNotePhonemes;
                Note[] group = phrase[groupIndex];
                if (group[0].phoneticHint is null) {
                    var lyric = group[0].lyric;
                    var targetDict = realPhnDict;
                    if (lyric.EndsWith("#3")) {
                        targetDict = chn3PhnDict;
                    } else if (lyric.EndsWith("#2")) {
                        targetDict = chn2PhnDict;
                    }
                    if (targetDict.ContainsKey(lyric)) {
                        realNotePhonemes = targetDict[lyric];
                        notePhonemes = GetSimilarPhonemes(realNotePhonemes);
                    } else {
                        realNotePhonemes = new string[] { lyric };
                        notePhonemes = new string[] { lyric };
                    }
                } else {
                    realNotePhonemes = group[0].phoneticHint.Split(" ");
                    notePhonemes = GetSimilarPhonemes(realNotePhonemes);
                }
                is_slur.AddRange(Enumerable.Repeat(false, notePhonemes.Length));
                phAlignPoints.Add(new Tuple<int, double>(
                    phonemes.Count + (notePhonemes.Length > 1 ? 1 : 0),//TODO
                    timeAxis.TickPosToMsPos(group[0].position) / 1000
                    ));
                phonemes.AddRange(notePhonemes);
                realPhonemes.AddRange(realNotePhonemes);
                midi.AddRange(Enumerable.Repeat((long)group[0].tone, notePhonemes.Length));
                notePhIndex.Add(phonemes.Count);

                midi_dur.AddRange(Enumerable.Repeat((float)timeAxis.MsBetweenTickPos(
                    group[0].position, group[^1].position + group[^1].duration) / 1000, notePhonemes.Length));
            }
            var lastNote = phrase[^1][^1];
            phAlignPoints.Add(new Tuple<int, double>(
                phonemes.Count,
                timeAxis.TickPosToMsPos(lastNote.position + lastNote.duration) / 1000));

            // Log.Information(JsonConvert.SerializeObject(phonemes));
            // Log.Information(JsonConvert.SerializeObject(realPhonemes));

            //Call Diffsinger phoneme timing model
            //ph_dur = session.run(['ph_dur'], {'tokens': tokens, 'midi': midi, 'midi_dur': midi_dur, 'is_slur': is_slur})[0]
            //error phonemes are replaced with SP
            long defaultToken = rhythmizer.phonemes.IndexOf("SP");
            var tokens = phonemes
                .Select(x => (long)(rhythmizer.phonemes.IndexOf(x)))
                .Select(x => x < 0 ? defaultToken : x)
                .ToList();
            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("tokens",
                new DenseTensor<long>(tokens.ToArray(), new int[] { tokens.Count }, false)
                .Reshape(new int[] { 1, tokens.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("midi",
                new DenseTensor<long>(midi.ToArray(), new int[] { midi.Count }, false)
                .Reshape(new int[] { 1, midi.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("midi_dur",
                new DenseTensor<float>(midi_dur.ToArray(), new int[] { midi_dur.Count }, false)
                .Reshape(new int[] { 1, midi_dur.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("is_slur",
                new DenseTensor<bool>(is_slur.ToArray(), new int[] { is_slur.Count }, false)
                .Reshape(new int[] { 1, is_slur.Count })));
            var outputs = rhythmizer.session.Run(inputs);
            ph_dur = outputs.First().AsTensor<float>().Select(x => (double)x).ToList();
            //Align the starting time of vowels to the position of each note, unit: s
            var positions = new List<double>();
            List<double> alignGroup = ph_dur.GetRange(0, phAlignPoints[0].Item1);
            //Starting consonants are not scaled
            positions.AddRange(stretch(alignGroup, 1, phAlignPoints[0].Item2));
            //The other phonemes are scaled according to the ratio of the time difference 
            //between the two alignment points and the duration of the phoneme
            //pairwise(alignGroups)
            foreach (var pair in phAlignPoints.Zip(phAlignPoints.Skip(1), (a, b) => Tuple.Create(a, b))) {
                var currAlignPoint = pair.Item1;
                var nextAlignPoint = pair.Item2;
                alignGroup = ph_dur.GetRange(currAlignPoint.Item1, nextAlignPoint.Item1 - currAlignPoint.Item1);
                double ratio = (nextAlignPoint.Item2 - currAlignPoint.Item2) / alignGroup.Sum();
                positions.AddRange(stretch(alignGroup, ratio, nextAlignPoint.Item2));
            }
            //Convert the position sequence to tick and fill into the result list
            int index = 1;
            foreach (int groupIndex in Enumerable.Range(0, phrase.Length)) {
                Note[] group = phrase[groupIndex];
                var noteResult = new List<Tuple<string, int>>();
                if (group[0].lyric.StartsWith("+")) {
                    continue;
                }
                double notePos = timeAxis.TickPosToMsPos(group[0].position);//音符起点位置，单位ms
                for (int phIndex = notePhIndex[groupIndex]; phIndex < notePhIndex[groupIndex + 1]; ++phIndex) {
                    noteResult.Add(Tuple.Create(realPhonemes[phIndex], timeAxis.TicksBetweenMsPos(
                       notePos, positions[phIndex] * 1000)));
                }
                partResult[group[0].position] = noteResult;
            }
        }
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseChinesePhonemizer.Romanize(lyrics);
        }
    }
}
