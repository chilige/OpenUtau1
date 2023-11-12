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

        public override void SetUpPhoneDictAndRhy() {
            LoadSingerRhythmizer("CNM3");
            GetRealPhnDict("ds_CNM3.txt");
            GetRhyMap();
        }

        protected override void ProcessPart(Note[][] phrase) {
            ProcessByRhyMap(phrase);
        }

        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseChinesePhonemizer.Romanize(lyrics);
        }
    }

}
