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

namespace OpenUtau.Core.DiffSinger {

    [Phonemizer("DiffSinger Rhythmizer Japanese Phonemizer", "DIFFS JPN-RHY", "BaiTang", language: "JA")]
    public class DiffSingerRhythmizerJPNPhonemizer : DiffSingerRhythmizerBasePhonemizer {

        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
            if (this.singer == null) {
                return;
            }
            //Load rhythmizer model
            try {
                //if rhythmizer is packed within the voicebank
                var packedRhythmizerPath = Path.Combine(singer.Location, "rhythmizer");
                if (Directory.Exists(packedRhythmizerPath)) {
                    rhythmizer = new DsBaseRhythmizer(packedRhythmizerPath);
                } else {
                    string rhythmizerName;
                    string rhythmizerYamlPath = Path.Combine(singer.Location, "dsrhythmizer.yaml");
                    if (File.Exists(rhythmizerYamlPath)) {
                        rhythmizerName = Core.Yaml.DefaultDeserializer.Deserialize<DsBaseRhythmizerYaml>(
                            File.ReadAllText(rhythmizerYamlPath, singer.TextFileEncoding)).rhythmizer;
                    } else {
                        rhythmizerName = DsBaseRhythmizer.DefaultRhythmizer;
                    }
                    rhythmizer = DsBaseRhythmizer.FromName(rhythmizerName);
                }
                //Load pinyin to phoneme dictionary from rhythmizer package
                phoneDict = rhythmizer.phoneDict;
            } catch (Exception ex) {
                return;
            }

            //LoadJpnPhoneDict
            try {
                string path = Path.Combine(singer.Location, "ds_JPN.txt");
                this.realPhnDict = LoadDsDict(path);
            } catch (Exception ex) {
                Log.Warning(ex.ToString());
                return;
            }

            //LoadRhyMap
            try {
                string path = Path.Combine(singer.Location, "rhy_map.txt");
                this.rhyMapDict = LoadRhyMap(path);
            } catch (Exception ex) {
                Log.Warning(ex.ToString());
                return;
            }
        }

        //Run timing model for a sentence
        //Slur notes are merged into the lyrical note before it to prevent shortening of consonants due to short slur
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

                    if (realPhnDict.ContainsKey(lyric)) {
                        realNotePhonemes = realPhnDict[lyric];
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

            //Call Diffsinger phoneme timing model
            //ph_dur = session.run(['ph_dur'], {'tokens': tokens, 'midi': midi, 'midi_dur': midi_dur, 'is_slur': is_slur})[0]
            //error phonemes are replaced with SP
            Log.Information(phonemes.ToString());
            Log.Information(realPhonemes.ToString());
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
                    noteResult.Add(Tuple.Create(phonemes[phIndex], timeAxis.TicksBetweenMsPos(
                       notePos, positions[phIndex] * 1000)));
                }
                partResult[group[0].position] = noteResult;
            }
        }

        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseJapanesePhonemizer.Romanize(lyrics);
        }
    }

}
